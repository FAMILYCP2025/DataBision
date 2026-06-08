<#
.SYNOPSIS
    End-to-end validation script for Native BI backend endpoints (Sprint 6A-6H).

.DESCRIPTION
    Tests all 15+ Native BI endpoints against a running DataBision API.
    In DEV mode (no JWT token provided), uses ?companyId query param.
    In PROD mode, sends Authorization: Bearer header.

.PARAMETER BaseUrl
    API base URL. Default: http://localhost:5103

.PARAMETER CompanyId
    Company slug to query (e.g. "company-dev-001"). Used as ?companyId in DEV mode.

.PARAMETER BearerToken
    Optional JWT bearer token. When provided, sent as Authorization header.
    The CompanyId is resolved from the JWT claim — query param is ignored by the API.

.EXAMPLE
    .\test-native-bi-endpoints.ps1
    .\test-native-bi-endpoints.ps1 -BaseUrl http://localhost:5000 -CompanyId acme-corp
    .\test-native-bi-endpoints.ps1 -BearerToken "eyJ..."
#>
param(
    [string]$BaseUrl    = "http://localhost:5103",
    [string]$CompanyId  = "company-dev-001",
    [string]$BearerToken = ""
)

$ErrorActionPreference = "Continue"

# ── Helpers ────────────────────────────────────────────────────────────────────

$PassCount = 0
$FailCount = 0
$Results   = @()

function Invoke-Endpoint {
    param(
        [string]$Label,
        [string]$Url,
        [string]$ExpectField = ""
    )

    $headers = @{ "Accept" = "application/json" }
    if ($BearerToken -ne "") {
        $headers["Authorization"] = "Bearer $BearerToken"
    }

    try {
        $response = Invoke-WebRequest -Uri $Url -Headers $headers -UseBasicParsing -ErrorAction Stop
        $status   = $response.StatusCode
        $body     = $response.Content | ConvertFrom-Json -ErrorAction SilentlyContinue

        $count = ""
        if ($ExpectField -ne "" -and $body) {
            $val = $body.$ExpectField
            if ($val -is [array]) { $count = "count=$($val.Count)" }
            elseif ($null -ne $val) { $count = "present" }
            else { $count = "null" }
        }

        $status_text = if ($status -eq 200) { "OK" } else { "HTTP $status" }
        $script:PassCount++
        $line = "  [PASS] $Label  $status_text  $count"
        Write-Host $line -ForegroundColor Green
        $script:Results += [PSCustomObject]@{ Endpoint=$Label; Status=$status; Result="PASS"; Detail=$count }
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        $script:FailCount++
        $line = "  [FAIL] $Label  HTTP $status  $($_.Exception.Message.Split([char]10)[0])"
        Write-Host $line -ForegroundColor Red
        $script:Results += [PSCustomObject]@{ Endpoint=$Label; Status=$status; Result="FAIL"; Detail=$_.Exception.Message.Split([char]10)[0] }
    }
}

function Invoke-ExpectError {
    param(
        [string]$Label,
        [string]$Url,
        [int]$ExpectStatus
    )
    $headers = @{ "Accept" = "application/json" }
    if ($BearerToken -ne "") { $headers["Authorization"] = "Bearer $BearerToken" }

    try {
        $response = Invoke-WebRequest -Uri $Url -Headers $headers -UseBasicParsing -ErrorAction Stop
        # Should NOT succeed
        $script:FailCount++
        Write-Host "  [FAIL] $Label  Expected HTTP $ExpectStatus but got $($response.StatusCode)" -ForegroundColor Red
        $script:Results += [PSCustomObject]@{ Endpoint=$Label; Status=$response.StatusCode; Result="FAIL"; Detail="Expected $ExpectStatus" }
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        if ($status -eq $ExpectStatus) {
            $script:PassCount++
            Write-Host "  [PASS] $Label  HTTP $status (expected)" -ForegroundColor Green
            $script:Results += [PSCustomObject]@{ Endpoint=$Label; Status=$status; Result="PASS"; Detail="Correct $ExpectStatus" }
        } else {
            $script:FailCount++
            Write-Host "  [FAIL] $Label  Expected $ExpectStatus got $status" -ForegroundColor Red
            $script:Results += [PSCustomObject]@{ Endpoint=$Label; Status=$status; Result="FAIL"; Detail="Expected $ExpectStatus" }
        }
    }
}

# ── Build base query string ────────────────────────────────────────────────────

# In DEV mode (no token), pass companyId as query param.
# In production, the API ignores companyId query param and uses JWT claim.
$Q = if ($BearerToken -eq "") { "?companyId=$CompanyId" } else { "?" }
$Amp = if ($BearerToken -eq "") { "&" } else { "" }
$QPrefix = if ($BearerToken -eq "") { "?companyId=$CompanyId&" } else { "?" }

# ── Header ────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "DataBision Native BI — E2E Endpoint Validation" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  BaseUrl   : $BaseUrl"
Write-Host "  CompanyId : $CompanyId"
Write-Host "  Auth mode : $(if ($BearerToken -eq '') { 'DEV (query param)' } else { 'JWT Bearer (token hidden)' })"
Write-Host ""

# ── 1. Dashboard ──────────────────────────────────────────────────────────────

Write-Host "1. Dashboard endpoints" -ForegroundColor Yellow

Invoke-Endpoint "GET /dashboard/summary"       "$BaseUrl/api/client/dashboard/summary$Q"                                      -ExpectField "data"
Invoke-Endpoint "GET /dashboard/sales-daily"   "$BaseUrl/api/client/dashboard/sales-daily${Q}${Amp}days=30"                    -ExpectField "data"
Invoke-Endpoint "GET /dashboard/sales-monthly" "$BaseUrl/api/client/dashboard/sales-monthly${Q}${Amp}months=12"               -ExpectField "data"
Invoke-Endpoint "GET /dashboard/top-customers" "$BaseUrl/api/client/dashboard/top-customers${Q}${Amp}limit=10"                -ExpectField "data"
Invoke-Endpoint "GET /dashboard/top-items"     "$BaseUrl/api/client/dashboard/top-items${Q}${Amp}limit=10"                    -ExpectField "data"
Invoke-Endpoint "GET /dashboard/salespersons"  "$BaseUrl/api/client/dashboard/salespersons${Q}${Amp}limit=20"                 -ExpectField "data"

# ── 2. Sales ──────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "2. Sales endpoints" -ForegroundColor Yellow

Invoke-Endpoint "GET /sales/overview"          "$BaseUrl/api/client/sales/overview$Q"                                         -ExpectField "data"
Invoke-Endpoint "GET /sales/daily"             "$BaseUrl/api/client/sales/daily${Q}${Amp}dateFrom=2026-01-01&dateTo=2026-12-31" -ExpectField "data"
Invoke-Endpoint "GET /sales/monthly"           "$BaseUrl/api/client/sales/monthly${Q}${Amp}dateFrom=2026-01-01&dateTo=2026-12-31" -ExpectField "data"
Invoke-Endpoint "GET /sales/customers"         "$BaseUrl/api/client/sales/customers${Q}${Amp}limit=20"                        -ExpectField "data"
Invoke-Endpoint "GET /sales/items"             "$BaseUrl/api/client/sales/items${Q}${Amp}limit=20"                            -ExpectField "data"
Invoke-Endpoint "GET /sales/salespersons"      "$BaseUrl/api/client/sales/salespersons${Q}${Amp}limit=20"                     -ExpectField "data"

# ── 3. Sync ───────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "3. Sync endpoints" -ForegroundColor Yellow

Invoke-Endpoint "GET /sync/status"             "$BaseUrl/api/client/sync/status$Q"                                            -ExpectField "data"
Invoke-Endpoint "GET /sync/objects"            "$BaseUrl/api/client/sync/objects$Q"                                           -ExpectField "data"
Invoke-Endpoint "GET /sync/transform-status"   "$BaseUrl/api/client/sync/transform-status$Q"                                  -ExpectField "data"

# ── 4. Diagnostics ────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "4. Diagnostics endpoints" -ForegroundColor Yellow

Invoke-Endpoint "GET /diagnostics/native-bi"        "$BaseUrl/api/client/diagnostics/native-bi$Q"              -ExpectField "status"
Invoke-Endpoint "GET /diagnostics/native-bi/tables" "$BaseUrl/api/client/diagnostics/native-bi/tables$Q"       -ExpectField "tables"

# ── 5. Pagination validation ──────────────────────────────────────────────────

Write-Host ""
Write-Host "5. Pagination + error validation" -ForegroundColor Yellow

Invoke-Endpoint "GET /dashboard/top-customers offset=10" "$BaseUrl/api/client/dashboard/top-customers${Q}${Amp}limit=5&offset=10"      -ExpectField "data"
Invoke-Endpoint "GET /sales/customers sortBy=cardCode"   "$BaseUrl/api/client/sales/customers${Q}${Amp}limit=5&sortBy=cardCode&sortDir=asc" -ExpectField "data"

# Error cases (DEV mode only — in JWT mode missing companyId returns 401)
if ($BearerToken -eq "") {
    Write-Host ""
    Write-Host "5b. Error validation (DEV mode)" -ForegroundColor Yellow
    Invoke-ExpectError "Missing companyId → 400"  "$BaseUrl/api/client/dashboard/summary"                                   -ExpectStatus 400
    Invoke-ExpectError "Invalid days=0 → 400"     "$BaseUrl/api/client/dashboard/sales-daily${Q}${Amp}days=0"              -ExpectStatus 400
    Invoke-ExpectError "Invalid sortBy → 400"     "$BaseUrl/api/client/dashboard/top-customers${Q}${Amp}sortBy=badField"   -ExpectStatus 400
    Invoke-ExpectError "Invalid offset=-1 → 400"  "$BaseUrl/api/client/dashboard/top-customers${Q}${Amp}offset=-1"         -ExpectStatus 400
    Invoke-ExpectError "Invalid dateFrom → 400"   "$BaseUrl/api/client/sales/overview${Q}${Amp}dateFrom=not-a-date"        -ExpectStatus 400
}

# ── 6. Summary ───────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Results: $PassCount passed, $FailCount failed" -ForegroundColor $(if ($FailCount -eq 0) { "Green" } else { "Red" })
Write-Host ""

if ($FailCount -gt 0) {
    Write-Host "Failed endpoints:" -ForegroundColor Red
    $Results | Where-Object { $_.Result -eq "FAIL" } | ForEach-Object {
        Write-Host "  - $($_.Endpoint) (HTTP $($_.Status))" -ForegroundColor Red
    }
    Write-Host ""
}

Write-Host "Tip: to save results, run:" -ForegroundColor Gray
Write-Host '  .\test-native-bi-endpoints.ps1 | Tee-Object -FilePath results.txt' -ForegroundColor Gray
