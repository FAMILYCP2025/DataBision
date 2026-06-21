# run-nativebi-finance-refresh.ps1
# DataBision Native BI Finance — Scheduled Extraction + Refresh
#
# Order: OACT (full) → OJDT (incremental) → refresh_accounting_all
# Logs to: [script dir]\logs\finance-refresh-YYYYMMDD.log
#
# Usage:
#   .\run-nativebi-finance-refresh.ps1
#   .\run-nativebi-finance-refresh.ps1 -ExtractorPath "C:\DataBision\Extractor"
#   .\run-nativebi-finance-refresh.ps1 -CompanyId "company-abc-001" -SkipOact
#
# Schedule: Register via Windows Task Scheduler
#   See: docs/operations/native-bi-scheduler-windows-task.md

param(
    [string]$ExtractorPath = $PSScriptRoot,
    [string]$CompanyId     = "",
    [switch]$SkipOact,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Setup ─────────────────────────────────────────────────────────────────────

$LogDir  = Join-Path $ExtractorPath "logs"
$Date    = Get-Date -Format "yyyyMMdd"
$LogFile = Join-Path $LogDir "finance-refresh-$Date.log"
$Exe     = Join-Path $ExtractorPath "DataBision.Extractor.exe"

if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Force $LogDir | Out-Null
}

function Write-Log {
    param([string]$Level, [string]$Message)
    $ts  = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "[$ts $Level] $Message"
    Write-Host $line
    Add-Content -Path $LogFile -Value $line -Encoding UTF8
}

function Invoke-Extractor {
    param([string[]]$Args)
    if ($DryRun) {
        Write-Log "INF" "DRY-RUN: $Exe $($Args -join ' ')"
        return 0
    }
    & $Exe @Args
    return $LASTEXITCODE
}

# ── Validate ──────────────────────────────────────────────────────────────────

Write-Log "INF" "=== DataBision Finance Refresh START ==="
Write-Log "INF" "ExtractorPath: $ExtractorPath"
Write-Log "INF" "CompanyId:     $CompanyId"
Write-Log "INF" "SkipOact:      $SkipOact"
Write-Log "INF" "DryRun:        $DryRun"

if (-not (Test-Path $Exe)) {
    Write-Log "ERR" "Extractor not found: $Exe"
    exit 1
}

$StartTime = Get-Date
$ExitCode  = 0

# ── Step 1: OACT (full refresh) ───────────────────────────────────────────────

if (-not $SkipOact) {
    Write-Log "INF" "Step 1/3: OACT extraction (full refresh)"
    $code = Invoke-Extractor @("--object", "OACT", "--send")
    if ($code -ne 0) {
        Write-Log "ERR" "OACT extraction failed (exit=$code). Aborting."
        exit $code
    }
    Write-Log "INF" "Step 1/3: OACT OK"
} else {
    Write-Log "INF" "Step 1/3: OACT skipped (--SkipOact)"
}

# ── Step 2: OJDT (incremental) ────────────────────────────────────────────────

Write-Log "INF" "Step 2/3: OJDT extraction (incremental)"
$code = Invoke-Extractor @("--object", "OJDT", "--send")
if ($code -ne 0) {
    Write-Log "WRN" "OJDT extraction failed (exit=$code) — refresh_accounting_all skipped."
    $ExitCode = $code
} else {
    Write-Log "INF" "Step 2/3: OJDT OK"

    # ── Step 3: refresh_accounting_all ────────────────────────────────────────

    Write-Log "INF" "Step 3/3: refresh_accounting_all"

    $TransformArgs = @("--transform-mart")
    if ($CompanyId -ne "") {
        $TransformArgs += @("--company", $CompanyId)
    }

    $code = Invoke-Extractor $TransformArgs
    if ($code -ne 0) {
        Write-Log "ERR" "Transform failed (exit=$code)"
        $ExitCode = $code
    } else {
        Write-Log "INF" "Step 3/3: transform OK"
    }
}

# ── Summary ───────────────────────────────────────────────────────────────────

$Duration = ((Get-Date) - $StartTime).TotalSeconds
$Status   = if ($ExitCode -eq 0) { "SUCCESS" } else { "FAILED" }

Write-Log "INF" "=== DataBision Finance Refresh $Status (${Duration}s, exit=$ExitCode) ==="

exit $ExitCode
