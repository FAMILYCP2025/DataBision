#!/usr/bin/env bash
# run-nativebi-finance-refresh.sh
# DataBision Native BI Finance — Scheduled Extraction + Refresh
#
# Order: OACT (full) → OJDT (incremental) → refresh_accounting_all
# Logs to: [script dir]/logs/finance-refresh-YYYYMMDD.log
#
# Usage:
#   ./run-nativebi-finance-refresh.sh
#   ./run-nativebi-finance-refresh.sh --company company-abc-001
#   ./run-nativebi-finance-refresh.sh --skip-oact
#   ./run-nativebi-finance-refresh.sh --dry-run
#
# Schedule: Register via cron
#   See: docs/operations/native-bi-scheduler-linux-cron.md

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
EXTRACTOR_PATH="${EXTRACTOR_PATH:-$SCRIPT_DIR}"
COMPANY_ID=""
SKIP_OACT=false
DRY_RUN=false

# ── Parse args ────────────────────────────────────────────────────────────────

while [[ $# -gt 0 ]]; do
    case "$1" in
        --company)    COMPANY_ID="$2"; shift 2 ;;
        --skip-oact)  SKIP_OACT=true; shift ;;
        --dry-run)    DRY_RUN=true; shift ;;
        --extractor)  EXTRACTOR_PATH="$2"; shift 2 ;;
        *) echo "Unknown arg: $1" >&2; exit 1 ;;
    esac
done

# ── Setup ─────────────────────────────────────────────────────────────────────

LOG_DIR="$EXTRACTOR_PATH/logs"
DATE="$(date +%Y%m%d)"
LOG_FILE="$LOG_DIR/finance-refresh-$DATE.log"
EXE="$EXTRACTOR_PATH/DataBision.Extractor"

mkdir -p "$LOG_DIR"

log() {
    local level="$1"; shift
    local msg="$*"
    local ts
    ts="$(date '+%Y-%m-%d %H:%M:%S')"
    local line="[$ts $level] $msg"
    echo "$line"
    echo "$line" >> "$LOG_FILE"
}

run_extractor() {
    if [ "$DRY_RUN" = true ]; then
        log "INF" "DRY-RUN: $EXE $*"
        return 0
    fi
    "$EXE" "$@"
}

# ── Lock ──────────────────────────────────────────────────────────────────────

LOCK_DIR="$EXTRACTOR_PATH/locks"
LOCK_FILE="$LOCK_DIR/finance-refresh.lock"

mkdir -p "$LOCK_DIR"

if [ -f "$LOCK_FILE" ]; then
    log "WRN" "Lock file exists: $LOCK_FILE. Previous run still active or crashed. Exiting."
    exit 1
fi

touch "$LOCK_FILE"
trap 'rm -f "$LOCK_FILE"' EXIT

# ── Validate ──────────────────────────────────────────────────────────────────

log "INF" "=== DataBision Finance Refresh START ==="
log "INF" "ExtractorPath: $EXTRACTOR_PATH"
log "INF" "CompanyId:     ${COMPANY_ID:-'(from config)'}"
log "INF" "SkipOact:      $SKIP_OACT"
log "INF" "DryRun:        $DRY_RUN"

if [ ! -f "$EXE" ]; then
    log "ERR" "Extractor not found: $EXE"
    exit 1
fi

START_TIME="$(date +%s)"
EXIT_CODE=0

# ── Step 1: OACT ─────────────────────────────────────────────────────────────

if [ "$SKIP_OACT" = false ]; then
    log "INF" "Step 1/3: OACT extraction (full refresh)"
    if ! run_extractor --object OACT --send; then
        log "ERR" "OACT extraction failed. Aborting."
        exit 1
    fi
    log "INF" "Step 1/3: OACT OK"
else
    log "INF" "Step 1/3: OACT skipped (--skip-oact)"
fi

# ── Step 2: OJDT ─────────────────────────────────────────────────────────────

log "INF" "Step 2/3: OJDT extraction (incremental)"
if ! run_extractor --object OJDT --send; then
    log "WRN" "OJDT extraction failed — refresh_accounting_all skipped."
    EXIT_CODE=4
else
    log "INF" "Step 2/3: OJDT OK"

    # ── Step 3: transform ─────────────────────────────────────────────────────

    log "INF" "Step 3/3: refresh_accounting_all"

    TRANSFORM_ARGS=(--transform-mart)
    if [ -n "$COMPANY_ID" ]; then
        TRANSFORM_ARGS+=(--company "$COMPANY_ID")
    fi

    if ! run_extractor "${TRANSFORM_ARGS[@]}"; then
        log "ERR" "Transform failed."
        EXIT_CODE=5
    else
        log "INF" "Step 3/3: transform OK"
    fi
fi

# ── Summary ───────────────────────────────────────────────────────────────────

END_TIME="$(date +%s)"
DURATION="$((END_TIME - START_TIME))"
STATUS="$([ "$EXIT_CODE" -eq 0 ] && echo 'SUCCESS' || echo 'FAILED')"

log "INF" "=== DataBision Finance Refresh $STATUS (${DURATION}s, exit=$EXIT_CODE) ==="

exit "$EXIT_CODE"
