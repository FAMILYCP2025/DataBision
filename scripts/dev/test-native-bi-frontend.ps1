<#
.SYNOPSIS
    Manual smoke-test script for the Native BI frontend routes.
    Does NOT require credentials — only validates that routes return HTTP 200
    and that the frontend build is reachable.

.PARAMETER FrontendUrl
    Base URL of the frontend dev server. Default: http://localhost:5173

.PARAMETER ApiUrl
    Base URL of the backend API. Default: http://localhost:5000

.PARAMETER CompanySlug
    Tenant slug appended as ?tenant= query param for local testing. Default: demo

.EXAMPLE
    .\test-native-bi-frontend.ps1
    .\test-native-bi-frontend.ps1 -FrontendUrl http://localhost:5174 -CompanySlug acme
#>

param(
    [string]$FrontendUrl = "http://localhost:5173",
    [string]$ApiUrl      = "http://localhost:5000",
    [string]$CompanySlug = "demo"
)

$ErrorActionPreference = "Continue"

$pass = 0
$fail = 0
$results = @()

function Test-Route {
    param([string]$Label, [string]$Url, [int]$ExpectedStatus = 200)

    try {
        $resp = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
        $status = $resp.StatusCode
    } catch {
        $status = $_.Exception.Response.StatusCode.value__
        if (-not $status) { $status = 0 }
    }

    $ok = ($status -eq $ExpectedStatus)
    $icon = if ($ok) { "[PASS]" } else { "[FAIL]" }
    $script:pass += if ($ok) { 1 } else { 0 }
    $script:fail += if ($ok) { 0 } else { 1 }
    $script:results += [PSCustomObject]@{ Result=$icon; Label=$Label; Status=$status; Url=$Url }
    Write-Host "$icon  $Label  ($status)"
}

Write-Host ""
Write-Host "=== Native BI Frontend Smoke Tests ==="
Write-Host "Frontend : $FrontendUrl"
Write-Host "API      : $ApiUrl"
Write-Host "Tenant   : $CompanySlug"
Write-Host ""

# ── Frontend routes ──────────────────────────────────────────────────────────
Write-Host "-- Frontend routes (expect 200 — Vite serves index.html for all SPA paths) --"
$slug = "?tenant=$CompanySlug"
Test-Route "Root"                   "$FrontendUrl/$slug"
Test-Route "NativeBi Dashboard"     "$FrontendUrl/client/bi/dashboard$slug"
Test-Route "NativeBi Sales"         "$FrontendUrl/client/bi/sales$slug"
Test-Route "NativeBi Diagnostics"   "$FrontendUrl/client/bi/diagnostics$slug"

# ── API health endpoints (no auth required) ──────────────────────────────────
Write-Host ""
Write-Host "-- API health (no auth) --"
Test-Route "API health"             "$ApiUrl/health"
Test-Route "Tenant config (public)" "$ApiUrl/api/tenant/config" -ExpectedStatus 200

# ── API BI endpoints (expect 401 without token — confirms route is registered) ──
Write-Host ""
Write-Host "-- API BI endpoints (expect 401 = route exists, no auth provided) --"
Test-Route "Dashboard summary"      "$ApiUrl/api/client/bi/dashboard/summary"   -ExpectedStatus 401
Test-Route "Dashboard sales-daily"  "$ApiUrl/api/client/bi/dashboard/sales-daily" -ExpectedStatus 401
Test-Route "Dashboard top-customers" "$ApiUrl/api/client/bi/dashboard/top-customers" -ExpectedStatus 401
Test-Route "Sales overview"         "$ApiUrl/api/client/bi/sales/overview"       -ExpectedStatus 401
Test-Route "Sales customers"        "$ApiUrl/api/client/bi/sales/customers"      -ExpectedStatus 401
Test-Route "Sales items"            "$ApiUrl/api/client/bi/sales/items"          -ExpectedStatus 401
Test-Route "Sales salespersons"     "$ApiUrl/api/client/bi/sales/salespersons"   -ExpectedStatus 401
Test-Route "Diagnostics"            "$ApiUrl/api/client/bi/diagnostics"          -ExpectedStatus 401
Test-Route "Table counts"           "$ApiUrl/api/client/bi/diagnostics/table-counts" -ExpectedStatus 401

# ── Summary ──────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "=== Results: $pass passed, $fail failed ==="
if ($fail -gt 0) {
    Write-Host ""
    Write-Host "Failed tests:"
    $results | Where-Object { $_.Result -eq "[FAIL]" } | ForEach-Object {
        Write-Host "  $($_.Label) — got $($_.Status) from $($_.Url)"
    }
    exit 1
} else {
    Write-Host "All tests passed."
    exit 0
}
