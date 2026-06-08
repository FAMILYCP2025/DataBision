<#
.SYNOPSIS
    End-to-end validation script for Native BI backend endpoints (Sprint 6A-6L).

.DESCRIPTION
    Tests all 17+ Native BI endpoints against a running DataBision API.
    In DEV mode (no JWT token provided), uses ?companyId query param.
    In PROD mode, sends Authorization: Bearer header.
    Validates meta fields on paginated responses.
    Exits with code 0 if all pass, 1 if any fail.

.PARAMETER BaseUrl
    API base URL. Default: http://localhost:5103

.PARAMETER CompanyId
    Company slug to query (e.g. "company-dev-001"). Used as ?companyId in DEV mode.

.PARAMETER BearerToken
    Optional JWT bearer token. When provided, sent as Authorization header.
    The CompanyId is resolved from the JWT claim — query param is ignored by the API.
    NOTE: token value is never printed to console or output file.

.PARAMETER OutputPath
    Optional path to save results (e.g. "docs/e2e-results/run-20260607.txt").
    Parent directory is created automatically if it does not exist.

.EXAMPLE
    .\test-native-bi-endpoints.ps1
    .\test-native-bi-endpoints.ps1 -BaseUrl http://localhost:5000 -CompanyId acme-corp
    .\test-native-bi-endpoints.ps1 -BearerToken "eyJ..." -OutputPath "docs/e2e-results/run.txt"
#>
param(
    [string]$BaseUrl     = "http://localhost:5103",
    [string]$CompanyId   = "company-dev-001",
    [string]$BearerToken = "",
    [string]$OutputPath  = ""
)

$ErrorActionPreference = "Continue"
$StartTime = Get-Date

# ── Output capture ────────────────────────────────────────────────────────────

$OutputLines = [System.Collections.Generic.List[string]]::new()

function Write-Out {
    param([string]$Text, [string]$Color = "White")
    Write-Host $Text -ForegroundColor $Color
    $script:OutputLines.Add($Text)
}

# ── Counters ──────────────────────────────────────────────────────────────────

$PassCount = 0
$FailCount = 0
$Results   = [System.Collections.Generic.List[PSCustomObject]]::new()

# ── Helpers ───────────────────────────────────────────────────────────────────

function Invoke-Endpoint {
    param(
        [string]$Label,
        [string]$Url,
        [string]$ExpectField    = "",
        [switch]$ExpectPaged    = $false
    )

    $headers = @{ "Accept" = "application/json" }
    if ($BearerToken -ne "") {
        $headers["Authorization"] = "Bearer $BearerToken"
    }

    try {
        $response = Invoke-WebRequest -Uri $Url -Headers $headers -UseBasicParsing -ErrorAction Stop
        $status   = $response.StatusCode
        $body     = $response.Content | ConvertFrom-Json -ErrorAction SilentlyContinue

        $detail = ""

        if ($ExpectField -ne "" -and $body) {
            $val = $body.$ExpectField
            if ($val -is [array]) { $detail = "count=$($val.Count)" }
            elseif ($null -ne $val) { $detail = "present" }
            else { $detail = "null" }
        }

        # Validate paginated meta fields
        if ($ExpectPaged -and $body) {
            $meta = $body.meta
            if ($null -eq $meta) {
                $script:FailCount++
                Write-Out "  [FAIL] $Label  HTTP $status — missing meta field" "Red"
                $script:Results.Add([PSCustomObject]@{ Endpoint=$Label; Status=$status; Result="FAIL"; Detail="missing meta" })
                return
            }
            $missingMeta = @()
            if ($null -eq $meta.limit)   { $missingMeta += "limit" }
            if ($null -eq $meta.offset)  { $missingMeta += "offset" }
            if ($null -eq $meta.count)   { $missingMeta += "count" }
            if ($null -eq $meta.hasMore) { $missingMeta += "hasMore" }
            if ($missingMeta.Count -gt 0) {
                $script:FailCount++
                Write-Out "  [FAIL] $Label  HTTP $status — meta missing: $($missingMeta -join ', ')" "Red"
                $script:Results.Add([PSCustomObject]@{ Endpoint=$Label; Status=$status; Result="FAIL"; Detail="meta missing: $($missingMeta -join ', ')" })
                return
            }
            $detail = "$detail meta(limit=$($meta.limit),offset=$($meta.offset),count=$($meta.count),hasMore=$($meta.hasMore))"
        }

        $script:PassCount++
        Write-Out "  [PASS] $Label  HTTP $status  $detail" "Green"
        $script:Results.Add([PSCustomObject]@{ Endpoint=$Label; Status=$status; Result="PASS"; Detail=$detail })
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        $msg = $_.Exception.Message.Split([char]10)[0]
        $script:FailCount++
        Write-Out "  [FAIL] $Label  HTTP $status  $msg" "Red"
        $script:Results.Add([PSCustomObject]@{ Endpoint=$Label; Status=$status; Result="FAIL"; Detail=$msg })
    }
}

function Invoke-ExpectError {
    param(
        [string]$Label,
        [string]$Url,
        [int]   $ExpectStatus
    )
    $headers = @{ "Accept" = "application/json" }
    if ($BearerToken -ne "") { $headers["Authorization"] = "Bearer $BearerToken" }

    try {
        $response = Invoke-WebRequest -Uri $Url -Headers $headers -UseBasicParsing -ErrorAction Stop
        $script:FailCount++
        Write-Out "  [FAIL] $Label  Expected HTTP $ExpectStatus but got $($response.StatusCode)" "Red"
        $script:Results.Add([PSCustomObject]@{ Endpoint=$Label; Status=$response.StatusCode; Result="FAIL"; Detail="Expected $ExpectStatus" })
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        if ($status -eq $ExpectStatus) {
            $script:PassCount++
            Write-Out "  [PASS] $Label  HTTP $status (expected)" "Green"
            $script:Results.Add([PSCustomObject]@{ Endpoint=$Label; Status=$status; Result="PASS"; Detail="Correct $ExpectStatus" })
        } else {
            $script:FailCount++
            Write-Out "  [FAIL] $Label  Expected $ExpectStatus got $status" "Red"
            $script:Results.Add([PSCustomObject]@{ Endpoint=$Label; Status=$status; Result="FAIL"; Detail="Expected $ExpectStatus" })
        }
    }
}

# ── Build query string prefix ─────────────────────────────────────────────────

$Q      = if ($BearerToken -eq "") { "?companyId=$CompanyId" } else { "?" }
$Amp    = if ($BearerToken -eq "") { "&" } else { "" }

# ── Header ────────────────────────────────────────────────────────────────────

Write-Out ""
Write-Out "DataBision Native BI — E2E Endpoint Validation" "Cyan"
Write-Out "================================================" "Cyan"
Write-Out "  BaseUrl   : $BaseUrl"
Write-Out "  CompanyId : $CompanyId"
Write-Out "  Auth mode : $(if ($BearerToken -eq '') { 'DEV (query param)' } else { 'JWT Bearer (token hidden)' })"
Write-Out "  Started   : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Out ""

# ── 1. Dashboard ──────────────────────────────────────────────────────────────

Write-Out "1. Dashboard endpoints" "Yellow"

Invoke-Endpoint "GET /dashboard/summary"       "$BaseUrl/api/client/dashboard/summary$Q"                                     -ExpectField "data"
Invoke-Endpoint "GET /dashboard/sales-daily"   "$BaseUrl/api/client/dashboard/sales-daily${Q}${Amp}days=30"                  -ExpectField "data"
Invoke-Endpoint "GET /dashboard/sales-monthly" "$BaseUrl/api/client/dashboard/sales-monthly${Q}${Amp}months=12"              -ExpectField "data"
Invoke-Endpoint "GET /dashboard/top-customers" "$BaseUrl/api/client/dashboard/top-customers${Q}${Amp}limit=10"               -ExpectField "data" -ExpectPaged
Invoke-Endpoint "GET /dashboard/top-items"     "$BaseUrl/api/client/dashboard/top-items${Q}${Amp}limit=10"                   -ExpectField "data" -ExpectPaged
Invoke-Endpoint "GET /dashboard/salespersons"  "$BaseUrl/api/client/dashboard/salespersons${Q}${Amp}limit=20"                -ExpectField "data" -ExpectPaged

# ── 2. Sales ──────────────────────────────────────────────────────────────────

Write-Out ""
Write-Out "2. Sales endpoints" "Yellow"

Invoke-Endpoint "GET /sales/overview"     "$BaseUrl/api/client/sales/overview$Q"                                               -ExpectField "data"
Invoke-Endpoint "GET /sales/daily"        "$BaseUrl/api/client/sales/daily${Q}${Amp}dateFrom=2026-01-01&dateTo=2026-12-31"      -ExpectField "data"
Invoke-Endpoint "GET /sales/monthly"      "$BaseUrl/api/client/sales/monthly${Q}${Amp}dateFrom=2026-01-01&dateTo=2026-12-31"    -ExpectField "data"
Invoke-Endpoint "GET /sales/customers"    "$BaseUrl/api/client/sales/customers${Q}${Amp}limit=20"                              -ExpectField "data" -ExpectPaged
Invoke-Endpoint "GET /sales/items"        "$BaseUrl/api/client/sales/items${Q}${Amp}limit=20"                                  -ExpectField "data" -ExpectPaged
Invoke-Endpoint "GET /sales/salespersons" "$BaseUrl/api/client/sales/salespersons${Q}${Amp}limit=20"                           -ExpectField "data" -ExpectPaged

# ── 3. Sync ───────────────────────────────────────────────────────────────────

Write-Out ""
Write-Out "3. Sync endpoints" "Yellow"

Invoke-Endpoint "GET /sync/status"           "$BaseUrl/api/client/sync/status$Q"           -ExpectField "data"
Invoke-Endpoint "GET /sync/objects"          "$BaseUrl/api/client/sync/objects$Q"           -ExpectField "data"
Invoke-Endpoint "GET /sync/transform-status" "$BaseUrl/api/client/sync/transform-status$Q"  -ExpectField "data"

# ── 4. Diagnostics ────────────────────────────────────────────────────────────

Write-Out ""
Write-Out "4. Diagnostics endpoints" "Yellow"

Invoke-Endpoint "GET /diagnostics/native-bi"        "$BaseUrl/api/client/diagnostics/native-bi$Q"         -ExpectField "data"
Invoke-Endpoint "GET /diagnostics/native-bi/tables" "$BaseUrl/api/client/diagnostics/native-bi/tables$Q"  -ExpectField "data"

# ── 5. Pagination validation ──────────────────────────────────────────────────

Write-Out ""
Write-Out "5. Pagination + sort validation" "Yellow"

Invoke-Endpoint "GET /dashboard/top-customers offset=10"   "$BaseUrl/api/client/dashboard/top-customers${Q}${Amp}limit=5&offset=10"             -ExpectField "data" -ExpectPaged
Invoke-Endpoint "GET /sales/customers sortBy=cardCode asc" "$BaseUrl/api/client/sales/customers${Q}${Amp}limit=5&sortBy=cardCode&sortDir=asc"   -ExpectField "data" -ExpectPaged

# ── 6. Error validation (DEV mode only) ──────────────────────────────────────

if ($BearerToken -eq "") {
    Write-Out ""
    Write-Out "6. Error validation (DEV mode)" "Yellow"

    Invoke-ExpectError "Missing companyId → 400"    "$BaseUrl/api/client/dashboard/summary"                                    -ExpectStatus 400
    Invoke-ExpectError "Invalid days=0 → 400"       "$BaseUrl/api/client/dashboard/sales-daily${Q}${Amp}days=0"               -ExpectStatus 400
    Invoke-ExpectError "Invalid months=0 → 400"     "$BaseUrl/api/client/dashboard/sales-monthly${Q}${Amp}months=0"           -ExpectStatus 400
    Invoke-ExpectError "Invalid sortBy → 400"       "$BaseUrl/api/client/dashboard/top-customers${Q}${Amp}sortBy=badField"    -ExpectStatus 400
    Invoke-ExpectError "Invalid sortDir → 400"      "$BaseUrl/api/client/dashboard/top-customers${Q}${Amp}sortDir=random"     -ExpectStatus 400
    Invoke-ExpectError "Invalid offset=-1 → 400"    "$BaseUrl/api/client/dashboard/top-customers${Q}${Amp}offset=-1"          -ExpectStatus 400
    Invoke-ExpectError "Invalid limit=0 → 400"      "$BaseUrl/api/client/dashboard/top-customers${Q}${Amp}limit=0"            -ExpectStatus 400
    Invoke-ExpectError "Invalid dateFrom → 400"     "$BaseUrl/api/client/sales/overview${Q}${Amp}dateFrom=not-a-date"         -ExpectStatus 400
}

# ── Summary ───────────────────────────────────────────────────────────────────

$Duration = [math]::Round(((Get-Date) - $StartTime).TotalSeconds, 1)
$Total    = $PassCount + $FailCount

Write-Out ""
Write-Out "================================================" "Cyan"
Write-Out "  Endpoints : $Total"
Write-Out "  Passed    : $PassCount" $(if ($FailCount -eq 0) { "Green" } else { "White" })
Write-Out "  Failed    : $FailCount" $(if ($FailCount -gt 0) { "Red" } else { "White" })
Write-Out "  Duration  : ${Duration}s"
Write-Out ""

if ($FailCount -gt 0) {
    Write-Out "Failed endpoints:" "Red"
    $Results | Where-Object { $_.Result -eq "FAIL" } | ForEach-Object {
        Write-Out "  - $($_.Endpoint) (HTTP $($_.Status): $($_.Detail))" "Red"
    }
    Write-Out ""
}

# ── Save to file ──────────────────────────────────────────────────────────────

if ($OutputPath -ne "") {
    $dir = Split-Path -Parent $OutputPath
    if ($dir -ne "" -and !(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    $OutputLines | Out-File -FilePath $OutputPath -Encoding utf8
    Write-Host "Results saved to: $OutputPath" -ForegroundColor Cyan
}

# ── Exit code ─────────────────────────────────────────────────────────────────

if ($FailCount -gt 0) { exit 1 } else { exit 0 }
