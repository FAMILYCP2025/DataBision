# SAP B1 Service Layer Extractor — Architecture

**Version:** 1.0  
**Date:** 2026-05-31  
**Status:** Architecture approved — pending implementation  
**Mode:** Service Layer Polling (Mode C)

> **Relationship to existing docs:**
> - `docs/dedicated-extractor-design.md` — Mode A (HANA SQL direct). Requires HANA port access.
> - `docs/service-layer-delta-design.md` — Mode B (PTN + UDT Queue delta). Requires SAP B1 ≥ 9.3 with PTN.
> - **This document — Mode C (Service Layer Polling).** Works with any SAP B1 version. Only requires HTTP access to Service Layer port. Simplest to deploy. Use when HANA SQL access is unavailable or Mode B is not yet configured.

---

## 1. Context and Problem

DataBision's staging layer (`raw.sap_*` in Supabase PostgreSQL) is ready to receive data via the Ingest API. The missing piece is the **extractor** — the component that reads data from SAP B1 and pushes it to DataBision.

Mode A requires a direct TCP connection to SAP HANA port 30015 and the HANA ODBC driver — unavailable on SAP B1 Cloud and restricted in many on-premises environments.

Mode B requires PTN configuration (SAP B1 ≥ 9.3, partner access to register subscriptions) and UDT creation — significant onboarding complexity.

**Mode C solves both constraints.** SAP B1 Service Layer is available to any client with a valid user account. It runs on HTTP/HTTPS port 50000, accessible from any network-adjacent machine. It requires zero HANA access and zero SAP B1 configuration.

### Constraints accepted in Mode C

| Constraint | Impact | Mitigation |
|---|---|---|
| Service Layer throughput: ~200–500 rows/s | Slower initial load vs HANA SQL | Batch size tuning + off-hours initial load |
| `$skip`-based pagination degrades on large datasets | Initial load of >50k rows is slow | Keyset pagination by `DocEntry` where supported |
| No per-second granularity in SAP B1 OData `$filter` on some versions | Possible re-extraction on same day | Lookback window + idempotent upsert handles duplicates |
| Session expiry after 30 min of inactivity | Need session management | Auto-renew before expiry |
| Rate limiting by SAP B1 per session | HTTP 429 or degraded response | Configurable rate limit + Polly |

---

## 2. Architecture Overview

```
┌─────────────────── CUSTOMER PREMISES ──────────────────────────┐
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │   DataBision SAP Extractor  (.NET 8 Worker Service)        │ │
│  │                                                            │ │
│  │  ┌─────────────┐   ┌────────────────┐  ┌───────────────┐  │ │
│  │  │  Scheduler  │──►│ Extract Pipeline│──►│  Ingest Pusher│  │ │
│  │  │ (PeriodicTimer│  │ - read checkpoint│  │ - POST to API│  │ │
│  │  │  per object) │  │ - build OData   │  │ - retry/CB   │  │ │
│  │  └─────────────┘  │   filter        │  │ - offline q  │  │ │
│  │                   │ - paginate SL   │  └───────┬───────┘  │ │
│  │  ┌─────────────┐  │ - map to DTOs   │          │          │ │
│  │  │ Session Mgr │◄─│ - compute hash  │          │          │ │
│  │  │ (B1SESSION  │  └────────────────┘          │          │ │
│  │  │  cookie)    │                               │          │ │
│  │  └──────┬──────┘                               │          │ │
│  │         │                    ┌──────────────────┘          │ │
│  │  ┌──────▼──────────┐   ┌────▼──────────────┐              │ │
│  │  │ SAP B1 SL Client│   │ Local SQLite Store │              │ │
│  │  │ HttpClient +    │   │ - checkpoints      │              │ │
│  │  │ Polly           │   │ - offline queue    │              │ │
│  │  └──────┬──────────┘   │ - run history      │              │ │
│  │         │              └───────────────────-┘              │ │
│  └─────────┼──────────────────────────────────────────────────┘ │
└────────────┼─────────────────────────────────────────────────────┘
             │ HTTPS :50000             │ HTTPS :443
             ▼                         ▼
     ┌───────────────┐       ┌─────────────────────────────────┐
     │ SAP B1        │       │ DataBision Cloud                │
     │ Service Layer │       │                                 │
     │ :50000        │       │  POST /api/ingest/sap-b1/*      │
     │               │       │  → Supabase raw.sap_*           │
     └───────────────┘       │  → ctl.ingest_checkpoint        │
                             └─────────────────────────────────┘
```

---

## 3. SAP B1 Service Layer — Session Management

### 3.1 Login

```
POST {sl_base_url}/Login
Content-Type: application/json

{
  "CompanyDB": "SBO_ACME",
  "UserName":  "databision_reader",
  "Password":  "..."
}
```

**Response (200 OK):**
```json
{
  "odata.metadata": "...",
  "SessionId": "7F3A2B-...",
  "Version": "1000195",
  "SessionTimeout": 30
}
```

Service Layer sets `B1SESSION={SessionId}` as a cookie. All subsequent calls must include this cookie.

### 3.2 Session Lifecycle

| State | Trigger | Action |
|---|---|---|
| **Fresh** | Startup or post-logout | POST /Login, store SessionId + expiry = now + 28 min |
| **Active** | Session age < 28 min | Use existing cookie |
| **Near-expiry** | Session age ≥ 28 min | Renew before next request (POST /Login again) |
| **Expired (401)** | HTTP 401 received | Re-login, replay request once |
| **Invalid (HANA error)** | HTTP 500 with "session" in body | Re-login, replay once |

**Safety margin: 28 minutes** (not 30) — avoids the race where session expires mid-request.

### 3.3 Logout

`POST {sl_base_url}/Logout` on graceful shutdown and after completed extraction run. Frees server-side session resources.

Never log the `SessionId` — treat it as a credential.

### 3.4 SessionManager Interface

```
ISessionManager:
  Task<string> GetCookieAsync(CancellationToken ct)
    → returns current valid "B1SESSION=..." header value
    → transparently renews if near expiry
    → retries login once on 401

  Task InvalidateAsync()
    → forces re-login on next GetCookieAsync call
```

The `SapB1Client` calls `GetCookieAsync()` before every HTTP request. Session management is completely transparent to the extraction pipeline.

---

## 4. OData Endpoints for Initial Objects

| SAP Object | Service Layer Entity | Key Field | Filter Field |
|---|---|---|---|
| OINV (AR Invoice headers) | `/b1s/v1/Invoices` | `DocEntry` | `UpdateDate` |
| OCRD (Business Partners) | `/b1s/v1/BusinessPartners` | `CardCode` | `UpdateDate` |
| OITM (Items) | `/b1s/v1/Items` | `ItemCode` | `UpdateDate` |
| OSLP (Salespersons) | `/b1s/v1/SalesPersons` | `SalesEmployeeCode` | `UpdateDate` |

### 4.1 OData Filter Syntax

SAP B1 Service Layer OData date format: `'YYYY-MM-DD'` (ISO) on versions ≥ 9.3.

**Incremental filter (OINV example):**
```
GET /b1s/v1/Invoices
  ?$filter=UpdateDate ge '2026-01-15'
  &$select=DocEntry,DocNum,CardCode,DocDate,DocDueDate,DocTotal,DocStatus,...
  &$orderby=UpdateDate asc,DocEntry asc
  &$top=500
```

**Note on UpdateTS:** SAP B1 OData does not always expose `UpdateTS` as a filterable field via `$filter`. The filter is on `UpdateDate` only. Within a day, ordering by `DocEntry` provides a stable keyset. The watermark stored is `(UpdateDate, last_DocEntry_on_that_date)`.

### 4.2 Column Selection

Always use `$select` — never request `*`. Reasons: performance, predictable schema, avoid undocumented fields that change between B1 versions.

**OINV `$select`:**
```
DocEntry,DocNum,DocDate,DocDueDate,TaxDate,CardCode,CardName,
DocTotal,DocTotalSy,VatSum,PaidToDate,DocCur,DocStatus,
SalesPersonCode,Comments,Cancelled,CreateDate,UpdateDate,
U_UpdateTS
```

> `U_UpdateTS` is a user field that some partners populate with `UpdateTS` from the underlying HANA table. Include but treat as optional — fall back to `'000000'` if absent.

**OCRD `$select`:**
```
CardCode,CardName,CardType,GroupCode,ContactPerson,
Phone1,Phone2,Currency,SalesPersonCode,VatLiable,
FederalTaxID,FrozenFor,CurrentAccountBalance,CreditLimit,
CreateDate,UpdateDate
```

**OITM `$select`:**
```
ItemCode,ItemName,ItemsGroupCode,QuantityOnStock,
CommittedQuantity,OrderedQuantity,AverageCost,LastPurchasePrice,
CreateDate,UpdateDate
```

**OSLP `$select`:**
```
SalesEmployeeCode,SalesEmployeeName,CreateDate,UpdateDate
```

### 4.3 Field Name Mapping: Service Layer → DTO

SAP B1 Service Layer uses PascalCase with different names than HANA SQL columns. Each object needs an explicit field mapping.

| Service Layer Field | HANA Column | DTO Property |
|---|---|---|
| `DocEntry` | `DocEntry` | `DocEntry` |
| `SalesPersonCode` | `SlpCode` | `SlpCode` |
| `CardName` | `CardName` | `CardName` |
| `QuantityOnStock` | `OnHand` | `OnHand` |
| `ItemsGroupCode` | `ItmsGrpCod` | `ItmsGrpCod` |
| `SalesEmployeeCode` | `SlpCode` | `SlpCode` |
| ... | ... | ... |

Full mapping defined in `SapFieldMapper.cs` per object. Never infer mapping from name similarity.

---

## 5. Incremental Extraction Strategy

### 5.1 Watermark

The watermark is `(UpdateDate, LastKeyOnThatDate)`. Two separate watermarks per object:
- **Update watermark**: last `UpdateDate` + last `DocEntry/CardCode/ItemCode/SlpCode` seen on that date for modified records.
- **Create watermark**: same structure for new records (using `CreateDate`).

For OINV first implementation, start with **Update watermark only**. Add Create watermark in Sprint 2 of extractor.

### 5.2 Lookback Window

| Level | When | Window |
|---|---|---|
| **Normal** | Default (every run) | UpdateDate ≥ watermark - 2 hours |
| **Nightly** | First run after 02:00 local | UpdateDate ≥ today - 1 day |
| **Month-close** | Day 1 of month (1×) | UpdateDate ≥ today - 7 days |

Lookback ensures changes that landed with a slightly older `UpdateDate` (SAP clock drift, batch operations) are not missed.

### 5.3 Pagination

Service Layer pagination uses `$top` + `$skip`. For large initial loads, `$skip` degrades because SAP recalculates from row 0 on each request.

**For initial load (> 50k rows):** Use date-chunked pagination — split the full date range into 30-day windows, process each window fully before advancing. Within a window, use `$top=500&$skip=N`.

**For incremental runs (typically < 5k rows):** `$top=500&$skip=N` is fine — windows are small.

**MVP implementation:** `$top=500` with skip-based pagination. Document the performance limitation. Upgrade to keyset (`$filter=DocEntry gt {last}`) in Sprint 3 of extractor for initial load optimization.

```
Page 1: $filter=UpdateDate ge '...' &$top=500&$skip=0
Page 2: $filter=UpdateDate ge '...' &$top=500&$skip=500
...
Until: response rows < 500 (last page)
```

### 5.4 Watermark Advancement

Watermark advances **only after Ingest API returns 2xx** for a batch. If the API call fails, the watermark stays at its previous value and the batch is retried. This guarantees at-least-once delivery with idempotent upsert on the API side.

After all pages for a run complete, the final watermark is:
```
update_date = MAX(UpdateDate) seen in this run
last_key    = MAX(DocEntry) where UpdateDate = update_date
```

---

## 6. Full Load (Initial)

### Scope: last 24 months

`CreateDate >= today - 730 days` — not `UpdateDate`. For initial load, use `CreateDate` to get all documents created in the window regardless of when they were last modified.

### Order of extraction

1. OSLP (small, no dependencies)
2. OCRD (master, used by reports)
3. OITM (master, used by reports)
4. OINV (transactional — largest volume)

Lines (INV1, RIN1) are added in Sprint 2 of the extractor.

### Initial load mode

Config flag `InitialLoadMode: true` in the extractor. When true:
- Uses `CreateDate` filter instead of `UpdateDate`
- Date range: last 24 months
- Splits into 30-day chunks
- Sends larger batches (up to 1000 rows)

On completion: sets `InitialLoadMode: false`, records `initial_loaded_at`, sets watermark to MAX(UpdateDate) observed.

---

## 7. Component Design

### 7.1 Projects

```
DataBision.Extractor/
├── DataBision.Extractor.Worker/         ← Worker Service, Program.cs, DI
├── DataBision.Extractor.Core/           ← Interfaces, DTOs, models
└── DataBision.Extractor.Infrastructure/ ← SL client, mappers, pusher, SQLite
```

### 7.2 Core Interfaces

```csharp
// Session management
interface ISessionManager
{
    Task<string> GetCookieAsync(CancellationToken ct);
    Task InvalidateAsync();
}

// Service Layer HTTP client
interface ISapB1Client
{
    Task<IReadOnlyList<T>> GetPageAsync<T>(
        string entity, string filter, string select,
        string orderBy, int top, int skip,
        CancellationToken ct);
}

// Per-object extractor
interface IObjectExtractor
{
    string SapObject { get; }
    Task<ExtractResult> ExtractIncrementalAsync(
        Checkpoint watermark, CancellationToken ct);
    Task<ExtractResult> ExtractInitialAsync(
        DateRange range, CancellationToken ct);
}

// Checkpoint persistence
interface ICheckpointStore
{
    Task<Checkpoint?> GetAsync(string sapObject, CancellationToken ct);
    Task SaveAsync(Checkpoint checkpoint, CancellationToken ct);
}

// DataBision API pusher
interface IIngestPusher
{
    Task<PushResult> PushAsync<T>(
        IngestBatchRequest<T> batch, CancellationToken ct)
        where T : IIngestRow;
}

// Local SQLite offline queue
interface IOfflineQueue
{
    Task<int> EnqueueAsync(PendingBatch batch, CancellationToken ct);
    Task<IReadOnlyList<PendingBatch>> DequeueForRetryAsync(
        int maxItems, CancellationToken ct);
    Task MarkSentAsync(int batchId, CancellationToken ct);
    Task MarkDeadAsync(int batchId, string reason, CancellationToken ct);
}
```

### 7.3 Core DTOs

```csharp
record Checkpoint(
    string SapObject,
    DateOnly? UpdateDate,
    string? LastKey,          // DocEntry/CardCode/ItemCode/SlpCode
    DateOnly? CreateDate,
    string? LastCreateKey,
    DateTime? LastRunAt,
    string? LastRunStatus,
    bool InitialLoaded,
    long TotalRowsExtracted
);

record ExtractResult(
    string SapObject,
    int RowsExtracted,
    int BatchesPushed,
    int BatchesFailed,
    Checkpoint NewWatermark,
    TimeSpan Duration,
    string Status  // SUCCESS | PARTIAL | FAILED
);

record PushResult(
    bool Success,
    int StatusCode,
    int RowsInserted,
    int RowsUpdated,
    int RowsSkipped,
    string? Error
);

record PendingBatch(
    int Id,
    string SapObject,
    string PayloadJson,
    int RowCount,
    DateTime CreatedAt,
    int AttemptCount
);
```

### 7.4 Object Extractors (one per SAP object)

Each extractor implements `IObjectExtractor`:

```
OinvExtractor   → maps SL Invoices response → SapOinvRow[]
OcrdExtractor   → maps SL BusinessPartners  → SapOcrdRow[]
OitmExtractor   → maps SL Items             → SapOitmRow[]
OslpExtractor   → maps SL SalesPersons      → SapOslpRow[]
```

Each extractor is responsible for:
1. Building the OData `$filter` from the checkpoint
2. Paging through the results
3. Mapping Service Layer JSON fields → DTO properties
4. Calling `IIngestPusher.PushAsync()` per batch
5. Returning the updated watermark

The field mapping is explicit per object — no generic reflection or convention-based mapping.

### 7.5 Extraction Pipeline

```
ExtractionPipeline.RunAsync(sapObject, CancellationToken):
  1. Acquire advisory lock (SQLite: INSERT INTO run_lock WHERE object = sapObject)
  2. Read checkpoint from ICheckpointStore
  3. Record run start in local SQLite (extraction_run)
  4. Determine lookback level (normal / nightly / month-close)
  5. Call extractor.ExtractIncrementalAsync(checkpoint, ct)
  6. For each page returned:
     a. Map SL response → DTOs
     b. Build IngestBatchRequest (with tenantId, companyId, runId, batchId)
     c. Call IIngestPusher.PushAsync()
     d. On 2xx: advance watermark, log metrics
     e. On error: enqueue to IOfflineQueue, continue (PARTIAL)
  7. Save final checkpoint to ICheckpointStore
  8. Record run end (SUCCESS / PARTIAL / FAILED)
  9. Release lock
```

---

## 8. Configuration

### 8.1 `appsettings.json` (base — committed)

```json
{
  "Extractor": {
    "AgentId": "REPLACE_AT_INSTALL",
    "LogLevel": "Information"
  },
  "Sap": {
    "ServiceLayerUrl": "REPLACE_AT_INSTALL",
    "CompanyDb": "REPLACE_AT_INSTALL",
    "Username": "REPLACE_AT_INSTALL",
    "PasswordSource": "env:DBI_SAP_PASSWORD",
    "SslValidateCertificate": true,
    "SessionTimeoutMin": 30,
    "SessionRenewAtMin": 28,
    "RequestTimeoutSec": 60,
    "PageSize": 500,
    "MaxConcurrentRequests": 2,
    "RateLimitPerMinute": 60
  },
  "DataBision": {
    "IngestApiUrl": "REPLACE_AT_INSTALL",
    "ApiKeySource": "env:DBI_API_KEY",
    "TenantId": "REPLACE_AT_INSTALL",
    "CompanyId": "REPLACE_AT_INSTALL",
    "RequestTimeoutSec": 60,
    "MaxBatchRows": 500
  },
  "Schedule": {
    "OINV": "*/30 * * * *",
    "OCRD": "0 * * * *",
    "OITM": "0 * * * *",
    "OSLP": "0 4 * * *"
  },
  "Lookback": {
    "NormalHours": 2,
    "NightlyHours": 24,
    "NightlyTriggerHourLocal": 2,
    "MonthCloseDays": 7
  },
  "Retry": {
    "MaxAttempts": 3,
    "InitialDelayMs": 1000,
    "BackoffFactor": 2.0,
    "MaxDelayMs": 30000
  },
  "CircuitBreaker": {
    "FailureThreshold": 5,
    "OpenDurationSec": 300
  },
  "OfflineQueue": {
    "MaxSizeBytes": 1073741824,
    "MaxRetryAttempts": 10
  },
  "LocalStore": {
    "DbPath": "data/extractor.db",
    "LogPath": "logs/extractor-.log",
    "LogRetainDays": 90
  }
}
```

### 8.2 Customer Override (`appsettings.Customer.json` — not committed)

```json
{
  "Extractor": {
    "AgentId": "acme-extractor-01"
  },
  "Sap": {
    "ServiceLayerUrl": "https://10.20.30.40:50000/b1s/v1",
    "CompanyDb": "SBO_ACME",
    "Username": "DBI_READER",
    "SslValidateCertificate": false
  },
  "DataBision": {
    "IngestApiUrl": "https://api.databision.app",
    "TenantId": "acme",
    "CompanyId": "acme-001"
  }
}
```

### 8.3 Secrets (environment variables — never in files)

| Variable | Description |
|---|---|
| `DBI_SAP_PASSWORD` | SAP B1 Service Layer password for extractor user |
| `DBI_API_KEY` | DataBision Ingest API key |

**Windows Service:** set via `sc config` or Registry key `HKLM:\SYSTEM\CurrentControlSet\Services\DataBisionExtractor\Environment`.

**Linux systemd:** `EnvironmentFile=/etc/databision/extractor.env` with `chmod 600`.

---

## 9. Windows Service

### 9.1 Packaging

```
dotnet publish -c Release -r win-x64 \
  -p:PublishSingleFile=true \
  -p:SelfContained=true \
  -o publish/
```

Output: single `DataBision.Extractor.exe` (~60 MB self-contained, no .NET runtime required on client).

### 9.2 Disk Layout

```
C:\DataBision\Extractor\
├── DataBision.Extractor.exe
├── appsettings.json
├── appsettings.Customer.json   ← customer-edited, not in git
├── data\
│   └── extractor.db            ← SQLite (checkpoints, queue, runs)
└── logs\
    └── extractor-YYYYMMDD.log  ← daily rotation, 90-day retention
```

### 9.3 Service Installation

```powershell
# Create dedicated low-privilege user
$pw = ConvertTo-SecureString "ComplexP@ss" -AsPlainText -Force
New-LocalUser "DataBisionSvc" -Password $pw -PasswordNeverExpires $true -AccountNeverExpires

# Grant "Log on as a service" via GPO or Local Security Policy
# Grant write access to C:\DataBision\Extractor\data and C:\DataBision\Extractor\logs

# Install service
sc.exe create DataBisionExtractor `
    binPath= "C:\DataBision\Extractor\DataBision.Extractor.exe" `
    start= auto `
    DisplayName= "DataBision SAP Extractor" `
    obj= ".\DataBisionSvc" password= "ComplexP@ss"

sc.exe description DataBisionExtractor "Extracts SAP B1 data to DataBision cloud (incremental, via Service Layer)"

sc.exe failure DataBisionExtractor `
    reset= 86400 `
    actions= restart/60000/restart/60000/restart/300000

# Set environment variables (secrets)
$regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\DataBisionExtractor"
Set-ItemProperty $regPath -Name "Environment" -Value @(
    "DBI_SAP_PASSWORD=<password>",
    "DBI_API_KEY=<api_key>"
)

sc.exe start DataBisionExtractor
```

### 9.4 Firewall Requirements

| Direction | Protocol | Destination | Port | Purpose |
|---|---|---|---|---|
| Outbound | HTTPS/TCP | SAP B1 Server LAN | 50000 | Service Layer |
| Outbound | HTTPS/TCP | api.databision.app | 443 | Ingest API |
| Inbound | None | — | — | No inbound required |
| Local | HTTP | 127.0.0.1 | 8081 | Health check (optional) |

---

## 10. Retry and Resilience

### 10.1 Polly Policies

**Ingest API pusher (IIngestPusher):**
```
RetryPolicy: 3 attempts, exponential backoff 1s/2s/4s + ±20% jitter
  Retry on: IOException, HttpRequestException, HTTP 5xx, HTTP 429
  No retry on: HTTP 4xx (except 408, 429)

CircuitBreaker: 5 consecutive failures in 60s window → open for 300s
  Half-open probe: 1 request to test recovery
```

**Service Layer client (ISapB1Client):**
```
RetryPolicy: 3 attempts, exponential backoff 1s/2s/4s
  Retry on: IOException, HTTP 5xx, HTTP 429
  On HTTP 401: call ISessionManager.InvalidateAsync(), retry once
  No retry on: other HTTP 4xx

Timeout: 60s per request
```

### 10.2 Offline Queue

When `IIngestPusher` fails after all retries:

1. Serialize the batch to JSON
2. Store in local SQLite `offline_batches` table
3. Continue with next page (batch is not lost — extraction run status becomes PARTIAL)
4. Background `OfflineDrainer` service processes the queue on circuit breaker recovery

**OfflineDrainer loop (runs every 60s):**
- Reads pending batches from SQLite (FIFO)
- Replays each to Ingest API
- On success: marks SENT
- On failure: increments attempt_count; after 10 attempts: marks DEAD, alerts

**Queue capacity:** 1 GB max. If exceeded: stops extraction and alerts. Prevents filling customer disk.

### 10.3 SAP Session Resilience

The `SessionManager` maintains a single session per run. The `SapB1Client`:
- Gets cookie from `SessionManager.GetCookieAsync()` before each request
- On HTTP 401: calls `SessionManager.InvalidateAsync()` then retries (once per request)
- Session manager logs all re-login events (not session ID content)

---

## 11. Checkpoints and Local State

### 11.1 SQLite Schema

```sql
-- Run history
CREATE TABLE extraction_run (
    run_id       INTEGER PRIMARY KEY AUTOINCREMENT,
    sap_object   TEXT NOT NULL,
    started_at   TEXT NOT NULL,  -- ISO UTC
    ended_at     TEXT,
    status       TEXT,           -- RUNNING / SUCCESS / PARTIAL / FAILED
    rows_extracted INTEGER DEFAULT 0,
    batches_sent   INTEGER DEFAULT 0,
    batches_failed INTEGER DEFAULT 0,
    error_message  TEXT,
    trigger_type   TEXT          -- SCHEDULE / MANUAL / RECOVERY
);

-- Watermarks
CREATE TABLE checkpoint (
    sap_object      TEXT PRIMARY KEY,
    update_date     TEXT,   -- YYYY-MM-DD
    last_key        TEXT,   -- DocEntry/CardCode/ItemCode/SlpCode as string
    create_date     TEXT,
    last_create_key TEXT,
    last_run_at     TEXT,
    last_run_status TEXT,
    initial_loaded  INTEGER DEFAULT 0,
    initial_loaded_at TEXT,
    total_rows      INTEGER DEFAULT 0
);

-- Offline queue
CREATE TABLE offline_batch (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    sap_object   TEXT NOT NULL,
    payload_json TEXT NOT NULL,
    row_count    INTEGER,
    created_at   TEXT NOT NULL,
    attempt_count INTEGER DEFAULT 0,
    last_attempt_at TEXT,
    status       TEXT DEFAULT 'PENDING'  -- PENDING / SENT / DEAD
);

-- Advisory lock (prevents concurrent extraction of same object)
CREATE TABLE run_lock (
    sap_object   TEXT PRIMARY KEY,
    started_at   TEXT NOT NULL,
    run_id       INTEGER
);
```

### 11.2 Watermark Rules

- Watermark saved only after Ingest API 2xx confirmation.
- On partial run: watermark = last successfully confirmed page's max values.
- Next run picks up from that point.
- Lookback adds padding — partial runs auto-recover without gaps.

### 11.3 Cloud Sync (after each successful run)

After `ExtractResult.Status = SUCCESS`, the extractor records the run summary by POSTing to a lightweight endpoint:

```
POST /api/extractor/heartbeat
{
  "agentId": "acme-extractor-01",
  "sapObject": "OINV",
  "runStatus": "SUCCESS",
  "rowsExtracted": 142,
  "watermarkDate": "2026-01-15",
  "completedAt": "2026-05-31T14:30:00Z"
}
```

This lets DataBision operations monitor extractor health without accessing the customer's server. The endpoint is fire-and-forget — failure does not affect the extraction run.

---

## 12. Rate Limiting

SAP B1 Service Layer may return HTTP 429 or degrade silently under heavy concurrent load.

**Configurable limits (per extractor instance):**

| Parameter | Default | Description |
|---|---|---|
| `MaxConcurrentRequests` | 2 | Max parallel SL calls (only when multiple objects run together) |
| `RateLimitPerMinute` | 60 | Token bucket — max requests per minute |
| `PauseBetweenPagesMs` | 200 | Minimum pause between paginated requests for same object |

**On HTTP 429:**
- Read `Retry-After` header if present
- Otherwise: pause 60s before retry
- Does not consume retry attempts from the Polly policy

---

## 13. Logging

### 13.1 Local Logs (Serilog rolling file)

- Path: `logs/extractor-YYYYMMDD.log`
- Format: JSON Lines (structured, parseable)
- Retention: 90 days
- Level: `Information` (configurable to `Debug` for troubleshooting)

**What is logged:**
- Service start/stop
- Each extraction run: object, watermark-from, watermark-to, rows, pages, duration, status
- Each batch push: status code, rows_inserted, rows_updated, rows_skipped, latency_ms
- Session events: login, renewal, logout (no session ID in logs)
- Retry attempts and circuit breaker state changes
- Offline queue events: enqueue, drain, dead

**What is NOT logged:**
- Field values from extracted rows (contains PII — CardName, amounts, etc.)
- Passwords or API keys (always redacted)
- Full connection strings

### 13.2 Cloud Heartbeat

Periodic `POST /api/extractor/heartbeat` (every 5 min, separate from run heartbeats). Payload:
- Agent version, uptime
- Last run per object: status, watermark lag, rows/run
- Circuit breaker state
- Offline queue depth

Alerts DataBision operations if heartbeat missing for > 30 min.

---

## 14. Health Check

Local HTTP endpoint at `http://127.0.0.1:8081/health`:

```json
{
  "status": "Healthy",
  "agentId": "acme-extractor-01",
  "version": "1.0.0",
  "uptime": "1d 4h 22m",
  "sap": {
    "serviceLayerReachable": true,
    "sessionActive": true,
    "lastLoginAt": "2026-05-31T14:00:00Z"
  },
  "cloud": {
    "ingestApiReachable": true,
    "circuitState": "Closed",
    "offlineQueueDepth": 0
  },
  "checkpoints": [
    {
      "sapObject": "OINV",
      "watermarkDate": "2026-05-31",
      "lastRunStatus": "SUCCESS",
      "lastRunAt": "2026-05-31T14:30:00Z",
      "lagMinutes": 2
    },
    {
      "sapObject": "OCRD",
      "watermarkDate": "2026-05-31",
      "lastRunStatus": "SUCCESS",
      "lastRunAt": "2026-05-31T14:00:00Z",
      "lagMinutes": 32
    },
    { "sapObject": "OITM", "watermarkDate": "2026-05-31", "lastRunStatus": "SUCCESS", "lagMinutes": 32 },
    { "sapObject": "OSLP", "watermarkDate": "2026-05-30", "lastRunStatus": "SUCCESS", "lagMinutes": 1240 }
  ]
}
```

HTTP status codes:
- `200 Healthy` — all systems operational
- `200 Degraded` — running but circuit open or offline queue > threshold
- `503 Unhealthy` — cannot reach SAP SL or config invalid

---

## 15. Onboarding a New Tenant

### Step 1: Create SAP B1 Read-Only User

In SAP B1 Administration:
- Create user `DBI_READER` with minimal permissions
- Required authorizations: full read access to OINV, OCRD, OITM, OSLP (and INV1, RIN1, ORIN for future)
- User type: `Professional User` (needs API access)
- Disable: Change Password on Next Login

### Step 2: Validate Service Layer Connectivity

From the customer's server, verify:
```powershell
# Test Service Layer reachability
Invoke-WebRequest -Uri "https://sap-server:50000/b1s/v1/$metadata" `
    -SkipCertificateCheck
# Should return OData XML metadata

# Test Login
$body = @{CompanyDB="SBO_ACME"; UserName="DBI_READER"; Password="..."} | ConvertTo-Json
Invoke-WebRequest -Uri "https://sap-server:50000/b1s/v1/Login" `
    -Method POST -Body $body -ContentType "application/json" -SkipCertificateCheck
# Should return 200 with SessionId
```

### Step 3: Install Extractor

1. Copy `DataBision.Extractor.exe` + `appsettings.json` to `C:\DataBision\Extractor\`
2. Create and populate `appsettings.Customer.json`
3. Set environment variables (SAP password + API key)
4. Install and start Windows Service

### Step 4: Run Dry-Run Validation

```
DataBision.Extractor.exe --dry-run --object OINV
```

Output: number of rows that would be extracted and pushed. No data written.

### Step 5: Execute Initial Load

```
DataBision.Extractor.exe --initial-load --object OSLP
DataBision.Extractor.exe --initial-load --object OCRD
DataBision.Extractor.exe --initial-load --object OITM
DataBision.Extractor.exe --initial-load --object OINV
```

Each command runs synchronously. Monitor progress via local health check.

### Step 6: Verify in Supabase

After initial load, confirm:
```sql
SELECT sap_object, total_rows_ingested, watermark_date
FROM ctl.ingest_checkpoint
WHERE company_id = 'acme-001'
ORDER BY sap_object;
```

### Step 7: Activate Scheduler

Once initial load complete, start the Windows Service in scheduled mode:
```
sc.exe start DataBisionExtractor
```

---

## 16. Known Limitations and Mitigations

| Limitation | Impact | Mitigation |
|---|---|---|
| `$skip`-based pagination is O(n²) on SAP SL | Initial load > 100k rows is slow | Date-chunked pagination for initial load; Sprint 3: switch to keyset by DocEntry |
| OData filter resolution is day-level only (no time component) | Same-day changes may be re-extracted on lookback runs | Idempotent upsert on Ingest API handles duplicates with zero data corruption |
| SAP B1 `UpdateDate` not always updated by internal jobs/triggers | Background SAP operations might be missed | Nightly 24h lookback catches late-arriving updates; weekly 7-day month-close catches the rest |
| Service Layer throughput ~200–500 rows/s vs HANA SQL 10k+ rows/s | Initial load takes hours for large datasets | Off-hours initial load window; upgrade to Mode A (HANA SQL) for high-volume clients |
| Self-signed SSL certificates common in on-premises SAP B1 | `SslValidateCertificate=false` required in dev/lab | Production: `SslValidateCertificate=true` with `TrustStorePath` pointing to customer's CA. Exception requires written client approval. |
| Service Layer does not expose `UpdateTS` as OData filter field | Intra-day ordering by natural key only | Acceptable for MVP; monitor for edge cases on high-frequency updates |
| Session-level parallelism limited | Cannot run >3 concurrent SL requests without degrading response | `MaxConcurrentRequests=2` default; per-tenant tunable |

---

## 17. Implementation Roadmap

### Sprint E1 — Foundation (Week 1)

**Goal:** One object (OINV) successfully extracting incrementally.

| Task | Scope |
|---|---|
| `DataBision.Extractor` solution skeleton (3 projects) | Worker, Core, Infrastructure |
| `ISessionManager` + `SapB1Client` with login/logout | HttpClient + Polly |
| Local SQLite store with checkpoint/run schema | Dapper or EF Core minimal |
| `OinvExtractor` — first implementation | OData filter + mapping |
| `IIngestPusher` — POST to Ingest API | HttpClient + Polly |
| `ExtractionPipeline` — full run loop | Orchestration |
| `Scheduler` — single object, cron-based | BackgroundService |
| `appsettings.json` + customer override | Config system |
| Windows Service hosting (`UseWindowsService()`) | Program.cs |
| Dry-run mode (`--dry-run`) | CLI arg |
| Serilog local logging | Setup |
| Health check endpoint (`:8081/health`) | ASP.NET minimal |
| Manual integration test against real SAP B1 dev instance | Validation |

**Exit criteria:** `dotnet run --dry-run --object OINV` reports N rows. Manual run inserts rows in `raw.sap_oinv`.

---

### Sprint E2 — All Four Objects + Initial Load (Week 2)

| Task | Scope |
|---|---|
| `OcrdExtractor`, `OitmExtractor`, `OslpExtractor` | Three new extractors |
| Field mappers for all four objects | `SapFieldMapper.cs` per object |
| Initial load mode (`--initial-load`) | Date-range chunking |
| Offline queue (`IOfflineQueue` + SQLite) | Batch persistence |
| OfflineDrainer background service | Queue drain loop |
| Circuit breaker for Ingest API | Polly |
| Heartbeat to `/api/extractor/heartbeat` | Fire-and-forget |
| Multi-object scheduler (concurrent OCRD + OITM) | Configurable concurrency |
| Windows Service installer script (PowerShell) | Onboarding |
| Rate limiter (token bucket against SL) | Polly RateLimiter |
| Lookback levels (normal / nightly / month-close) | Schedule logic |
| Onboarding runbook (dry-run → initial load → validate → schedule) | Ops doc |

**Exit criteria:** All 4 objects extract incrementally. Initial load of 24-month OINV for a customer with 50k invoices completes in < 4 hours.

---

### Sprint E3 — Lines, Reliability, Observability (Week 3)

| Task | Scope |
|---|---|
| `Inv1Extractor` — lines via DocEntry chunks | Follow-OINV schedule |
| `Rin1Extractor` — lines via DocEntry chunks | Follow-ORIN schedule |
| `OrinExtractor` — credit memo headers | ORIN extraction |
| Keyset pagination for large initial loads (`DocEntry gt :last`) | Performance |
| Retry-from-DLQ command (`--retry-dead-queue`) | Operations |
| Structured metrics (rows/run, lag, latency) in heartbeat | Observability |
| Session renewal edge cases (parallel requests near expiry) | Hardening |
| Integration test suite (mock SL server) | Test coverage |
| Linux systemd packaging | Ops |
| Version reporting to cloud (`/api/extractor/version`) | Fleet management |

**Exit criteria:** All 7 objects extracting. Initial load of a full medium customer (500k invoices, 2M lines) completes overnight.

---

### Sprint E4 — Production Hardening (Week 4)

| Task | Scope |
|---|---|
| Self-signed cert support with explicit CA pinning | Security |
| DPAPI credential encryption for Windows | Security |
| Automated daily smoke test (1-row extract + push) | Ops |
| Admin CLI commands: `--status`, `--reset-checkpoint`, `--force-run` | Ops tooling |
| Graceful shutdown (complete current page, logout SL) | Resilience |
| Distributed lock (prevent two extractor instances for same tenant/object) | Safety |
| Alerting integration (email on DLQ events, heartbeat missing) | Ops |
| Customer-facing install guide (PDF) | Onboarding |

---

## 18. Security Constraints

1. **SSL validation in production:** `SslValidateCertificate` must be `true` in production. If the customer uses a self-signed cert, provide the CA certificate path via `TrustStorePath`. `SslValidateCertificate=false` is only permitted in dev/lab with written client approval.

2. **Credentials never in committed files:** `appsettings.Customer.json` must be in `.gitignore`. Passwords and API keys must come from environment variables only.

3. **No PII in logs:** Row content is never logged. Log only counts, timestamps, and status codes.

4. **API key scope:** The DataBision API key used by the extractor is scoped to `ingest` only. It cannot read other tenants' data, access the admin panel, or modify checkpoints.

5. **Minimal SAP permissions:** The `DBI_READER` user in SAP B1 has read-only access to the specific tables needed. No write permissions, no admin access.

6. **Network isolation:** Extractor has no inbound port open (except the optional local health check on `127.0.0.1:8081`). Outbound only: SAP SL and DataBision API.

---

*Architecture v1.0 — DataBision SAP Extractor — Mode C (Service Layer Polling)*  
*Upstream refs: `docs/dedicated-extractor-design.md` (Mode A), `docs/service-layer-delta-design.md` (Mode B)*  
*Implementation start: Sprint E1*
