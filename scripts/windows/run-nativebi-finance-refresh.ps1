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

function Invoke-ExtractorProcess {
    param([string[]]$ExtractorArgs)
    if ($DryRun) {
        Write-Log "INF" "DRY-RUN: $Exe $($ExtractorArgs -join ' ')"
        return 0
    }
    & $Exe @ExtractorArgs
    return $LASTEXITCODE
}

# ── Lock ──────────────────────────────────────────────────────────────────────

$LockDir  = Join-Path $ExtractorPath "locks"
$LockFile = Join-Path $LockDir "finance-refresh.lock"

if (-not (Test-Path $LockDir)) {
    New-Item -ItemType Directory -Force $LockDir | Out-Null
}

if (Test-Path $LockFile) {
    Write-Log "WRN" "Lock file exists: $LockFile. Previous run still active or crashed. Exiting."
    exit 1
}

New-Item -ItemType File -Path $LockFile -Force | Out-Null

$ExitCode = 0

try {

# ── Validate ──────────────────────────────────────────────────────────────────

Write-Log "INF" "=== DataBision Finance Refresh START ==="
Write-Log "INF" "ExtractorPath: $ExtractorPath"
Write-Log "INF" "CompanyId:     $CompanyId"
Write-Log "INF" "SkipOact:      $SkipOact"
Write-Log "INF" "DryRun:        $DryRun"

if (-not (Test-Path $Exe)) {
    Write-Log "ERR" "Extractor not found: $Exe"
    $ExitCode = 1
    return
}

$StartTime = Get-Date

# ── Step 1: OACT (full refresh) ───────────────────────────────────────────────

if (-not $SkipOact) {
    Write-Log "INF" "Step 1/3: OACT extraction (full refresh)"
    $code = Invoke-ExtractorProcess @("--object", "OACT", "--send")
    if ($code -ne 0) {
        Write-Log "ERR" "OACT extraction failed (exit=$code). Aborting."
        $ExitCode = $code
        return
    }
    Write-Log "INF" "Step 1/3: OACT OK"
} else {
    Write-Log "INF" "Step 1/3: OACT skipped (--SkipOact)"
}

# ── Step 2: OJDT (incremental) ────────────────────────────────────────────────

Write-Log "INF" "Step 2/3: OJDT extraction (incremental)"
$code = Invoke-ExtractorProcess @("--object", "OJDT", "--send")
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

    $code = Invoke-ExtractorProcess $TransformArgs
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

} finally {
    Remove-Item -Path $LockFile -Force -ErrorAction SilentlyContinue
}

exit $ExitCode
