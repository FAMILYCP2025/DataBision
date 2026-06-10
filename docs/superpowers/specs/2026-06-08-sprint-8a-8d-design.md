# Sprint 8A–8D — Design Spec

**Date:** 2026-06-08  
**Status:** Approved  
**Author:** Product + Architecture

---

## Context

DataBision has a working MVP with SAP B1 Service Layer extractor, Supabase PostgreSQL staging pipeline (RAW → STG → MART), and a Native BI frontend portal (Dashboard, Sales, Diagnostics). KSDEPOR has given green light as the first real customer.

Sprints 8A–8D convert the MVP into a multi-process product with a proper catalog, production-grade extractor pagination, expanded MART schemas, and operational monitoring.

---

## Architecture

```
SAP B1 HANA (source)
    ↓ Service Layer HTTP
.NET Extractor (DataBision.Extractor)
    ↓ Ingest API → Dapper upserts
Supabase PostgreSQL
    ├── ctl.*    EF-managed control tables (checkpoints, audit) — existing
    ├── stg.*    Raw staging tables (Dapper) — existing
    ├── mart.*   Analytics tables + PL/pgSQL functions — existing + new
    ├── cfg.*    Product catalog (SQL seed + 1 EF entity) — NEW Sprint 8A
    └── ops.*    Operational monitoring (SQL only) — NEW Sprint 8D
    ↓
DataBision API (.NET 8)
    ↓
Portal Web (React/TypeScript)
```

**Two EF Core contexts:**
- `AppDbContext` (SQLite): SaaS tenancy — Companies, Users, Modules, Reports, Auth
- `StagingDbContext` (PostgreSQL/Supabase): ETL pipeline — `ctl.*` entities + new `cfg.company_process_enabled`

**Pattern for Staging entities:**
- File: `src/DataBision.Infrastructure/Data/Staging/Entities/<Entity>.cs`
- Attributes: `[Table("table_name", Schema = "schema")]`, `[Key]`, `[Required]`, `[MaxLength]`
- Config class: `src/DataBision.Infrastructure/Data/Staging/Configurations/<Entity>Configuration.cs` (implements `IEntityTypeConfiguration<T>`) — auto-discovered by `ApplyConfigurationsFromAssembly` since namespace contains "Staging"
- Migration history: `ctl.__EFMigrationsHistory` (snake_case naming convention)

---

## Sprint 8A — cfg Schema (Supabase PostgreSQL)

### Goal
Create the product catalog that defines processes, dashboards, KPIs, SAP objects, and per-company enablement. Powers the future "what modules should I show this company?" query.

### Migration
**File:** `20260608_XXXXXX_AddCfgSchema.cs` (StagingDbContext)

### Tables (SQL only, no EF entities)

```sql
cfg.process            -- 5 rows: SALES, PURCHASING, INVENTORY, FINANCE, OPERATIONS
cfg.dashboard          -- 20 rows: 4 dashboards per process
cfg.kpi                -- ~30 rows: KPIs per process with human formula + format_type
cfg.kpi_formula        -- SQL formula + dependencies per KPI
cfg.dashboard_widget   -- widgets per dashboard (KPI_CARD, BAR_CHART, TABLE, etc.)
cfg.sap_object_catalog -- ~30 SAP objects mapped to processes
```

### EF Entity (one only)

```csharp
// src/DataBision.Infrastructure/Data/Staging/Entities/CompanyProcessEnabled.cs
[Table("company_process_enabled", Schema = "cfg")]
public sealed class CompanyProcessEnabled {
    [Key, Column(Order = 0)] public string CompanyId { get; set; }
    [Key, Column(Order = 1)] public string ProcessCode { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime EnabledAtUtc { get; set; }
}

// src/DataBision.Infrastructure/Data/Staging/Configurations/CompanyProcessEnabledConfiguration.cs
// IEntityTypeConfiguration<CompanyProcessEnabled> — configures composite PK
```

**DbSet in StagingDbContext:**
```csharp
public DbSet<CompanyProcessEnabled> CompanyProcessesEnabled => Set<CompanyProcessEnabled>();
```

### Seeds (SQL in migration)

**cfg.process:**
| process_code | process_name | display_order |
|---|---|---|
| SALES | Ventas | 1 |
| PURCHASING | Compras | 2 |
| INVENTORY | Inventario | 3 |
| FINANCE | Finanzas | 4 |
| OPERATIONS | Operaciones | 5 |

**cfg.dashboard (20 rows):**
| process_code | dashboard_code | dashboard_type |
|---|---|---|
| SALES | SALES_EXECUTIVE | EXECUTIVE |
| SALES | SALES_CUSTOMERS | ANALYTICAL |
| SALES | SALES_ITEMS_MARGIN | ANALYTICAL |
| SALES | SALES_ORDER_FULFILLMENT | OPERATIONAL |
| PURCHASING | PURCHASING_EXECUTIVE | EXECUTIVE |
| PURCHASING | PURCHASING_SUPPLIERS | ANALYTICAL |
| PURCHASING | PURCHASING_RECEIVING | OPERATIONAL |
| PURCHASING | PURCHASING_PRICE_VARIATION | CONTROL |
| INVENTORY | INVENTORY_EXECUTIVE | EXECUTIVE |
| INVENTORY | INVENTORY_STOCK_VALUE | ANALYTICAL |
| INVENTORY | INVENTORY_ROTATION_COVERAGE | ANALYTICAL |
| INVENTORY | INVENTORY_WAREHOUSE_TRANSFERS | OPERATIONAL |
| FINANCE | FINANCE_EXECUTIVE | EXECUTIVE |
| FINANCE | FINANCE_AR_AGING | CONTROL |
| FINANCE | FINANCE_AP_AGING | CONTROL |
| FINANCE | FINANCE_CASHFLOW_CONTROL | CONTROL |
| OPERATIONS | OPERATIONS_EXECUTIVE | EXECUTIVE |
| OPERATIONS | OPERATIONS_PIPELINE_HEALTH | OPERATIONAL |
| OPERATIONS | OPERATIONS_DATA_QUALITY | CONTROL |
| OPERATIONS | OPERATIONS_ALERTS | CONTROL |

**cfg.sap_object_catalog (~30 objects):**
SALES: OINV, INV1, ORIN, RIN1, ORDR, RDR1, ODLN, DLN1, OCRD, OSLP, OITM  
PURCHASING: OPOR, POR1, OPDN, PDN1, OPCH, PCH1, OCRD  
INVENTORY: OITM, OITW, OINM, OWHS, OWTR, WTR1  
FINANCE: OJDT, JDT1, ORCT, OVPM, OINV, OPCH  

**cfg.company_process_enabled seeds:**
- `company-dev-001` → all 5 processes enabled
- KSDEPOR: **not seeded** — company_id not confirmed in codebase. Document in `docs/ksdepor-onboarding.md`.

### Docs
- `docs/databision-process-kpi-catalog.md`

---

## Sprint 8B — Extractor Real Pagination

### Goal
Replace single-page extraction with proper multi-page pagination, page-level logging, and retry. No new objects activated — only safe refactor of existing jobs.

### Current state
Every job fetches exactly `$top=PageSize` and stops. No `$skip`, no `@odata.nextLink`, no retry per page.

### New class: `ServiceLayerPaginator`

**File:** `src/DataBision.Extractor/ServiceLayer/ServiceLayerPaginator.cs`

```csharp
public sealed class ServiceLayerPaginator(IServiceLayerClient sl, ILogger<ServiceLayerPaginator> log)
{
    public async Task<PaginationResult> PaginateAsync(
        string sapObject, string entity, string baseQuery,
        int pageSize, int maxPages, CancellationToken ct)
    // Returns: PaginationResult { AllRows, Logs, HitMaxPages, LastError }
}
```

**Pagination algorithm:**
1. Build URL: `{entity}?{baseQuery}&$top={pageSize}&$skip={skip}`
2. Execute with 2-retry on transient errors (HTTP 429, 503, timeout)
3. If response contains `@odata.nextLink` → use it directly for next page
4. Else → `skip += pageSize`
5. Stop when: `rows.Count < pageSize` OR `pageNumber >= maxPages`
6. Log each page: `PageLog { SapObject, PageNumber, Skip, Top, RowsReceived, ElapsedMs, Status, ErrorCode, ErrorMessage }`

**New models (same file or `ServiceLayer/Models/`):**
```csharp
record PageLog(string SapObject, int PageNumber, int Skip, int Top,
               int RowsReceived, long ElapsedMs, string Status,
               string? ErrorCode, string? ErrorMessage);

record PaginationResult(JsonArray AllRows, List<PageLog> Logs,
                        bool HitMaxPages, string? LastError);
```

### ExtractorOptions changes

Add:
```csharp
/// <summary>Safety cap: max pages per object per run. Default 500 (= 50k rows at PageSize 100).</summary>
public int MaxPages { get; init; } = 500;
```

### Jobs refactored (7 jobs)

All use `ServiceLayerPaginator` instead of inline `_sl.GetAsync(...)`:
- `OcrdExtractorJob` — OCRD
- `OitmExtractorJob` — OITM
- `OinvExtractorJob` — OINV
- `OrinExtractorJob` — ORIN
- `OslpExtractorJob` — OSLP
- `Inv1ExtractorJob` — INV1 (DocumentLines via `$expand` if supported, else separate endpoint)
- `Rin1ExtractorJob` — RIN1

**Checkpoint advance rule:** watermark advances only after ALL pages complete successfully. If any page fails permanently, run is marked WARNING/ERROR, checkpoint not advanced.

### Object catalog in code

New constants class `SapObjectRegistry`:
```csharp
// Objetos activos (extraen hoy)
public static readonly IReadOnlyList<string> ActiveObjects = ["OCRD","OITM","OSLP","OINV","ORIN","INV1","RIN1"];

// Objetos preparados, IsActive = false
public static readonly IReadOnlyList<string> PreparedObjects = ["ORDR","RDR1","ODLN","DLN1","OPOR","POR1","OPDN","PDN1","OPCH","PCH1","OITW","OWHS","OWTR","WTR1"];
```

### Unit tests

**Project:** `tests/DataBision.Application.Tests` (or new `tests/DataBision.Extractor.Tests`)

Test cases for `ServiceLayerPaginator`:
1. Single page (rows < pageSize → stops after page 1)
2. Multi-page via `$skip` (2-3 pages)
3. Multi-page via `@odata.nextLink`
4. `maxPages` cap stops execution with `HitMaxPages = true`
5. Retry succeeds on 2nd attempt after transient 503
6. Permanent error (400) → no retry, `LastError` set
7. Empty first page → returns immediately, 0 rows

### Docs
- `docs/service-layer-pagination-strategy.md`
- `docs/ksdepor-extraction-object-plan.md`

---

## Sprint 8C — MART Process Schemas (Supabase PostgreSQL)

### Goal
Expand MART with purchasing, inventory, and finance tables. Supplement SALES with missing views/tables. Create per-process refresh orchestrators.

### Migration
**File:** `20260608_XXXXXX_AddMartProcessSchemas.cs` (StagingDbContext, SQL only)

### SALES — no duplication

Existing: `sales_daily`, `sales_monthly`, `customer_sales`, `item_sales`, `salesperson_sales`, `sales_kpi_summary`

**New only if truly different:**
- `mart.sales_customer_dashboard` — extends `customer_sales` with `group_name`, `days_since_last_purchase` (computed from `CURRENT_DATE - last_invoice_date`)
- `mart.sales_item_dashboard` — extends `item_sales` with `avg_price`, `item_group_name`, `estimated_margin_pct NULLABLE`
- `mart.sales_fulfillment_dashboard` — **entirely nullable** until stg.order/stg.delivery exist. Refresh function inserts zeros.

**NOT created (already covered by existing tables):**
- `sales_executive_daily` → identical to `sales_daily` → use a VIEW instead if needed

### PURCHASING (new — all tables)

All fields populated from `stg.purchase_order`, `stg.purchase_invoice` — these STG tables **do not exist yet**. Columns that depend on them are `NULLABLE DEFAULT NULL`. Refresh functions insert safely with `COALESCE(..., 0)`.

Tables:
- `mart.purchase_executive_daily`
- `mart.purchase_supplier_dashboard`
- `mart.purchase_receiving_dashboard`

### INVENTORY (new — all tables)

Depends on `stg.item_warehouse` (OITW — not yet extracted) and `stg.warehouse` (OWHS). Fields dependent on OITW are NULLABLE.  
AR rotation depends on `stg.item` (OITM) + `stg.sales_invoice_line` — both exist ✓.

Tables:
- `mart.inventory_stock_dashboard`
- `mart.inventory_rotation_dashboard` (rotation_status computed: FAST/NORMAL/SLOW/NO_MOVEMENT/STOCKOUT)
- `mart.inventory_warehouse_dashboard`

### FINANCE (new — all tables)

- `mart.finance_ar_aging_dashboard` — **fully functional now**: uses `stg.sales_invoice` (OINV extracted ✓). Computes aging buckets from `doc_due_date`.
- `mart.finance_ap_aging_dashboard` — depends on `stg.purchase_invoice` (OPCH — not yet extracted). All balance fields NULLABLE. Refresh inserts zeros safely.
- `mart.finance_executive_daily` — AR side functional, AP side zeros until OPCH extracted.

### Functions (5 orchestrators)

```sql
mart.refresh_sales_process(company_id)
    -- calls: refresh_sales_daily, refresh_sales_monthly, refresh_customer_sales,
    --         refresh_item_sales, refresh_salesperson_sales, refresh_sales_kpi_summary,
    --         refresh_sales_customer_dashboard, refresh_sales_item_dashboard,
    --         refresh_sales_fulfillment_dashboard
    -- returns TABLE(object_name TEXT, rows_affected INT)

mart.refresh_purchasing_process(company_id)
    -- defensive: checks IF EXISTS stg.purchase_order before any INSERT

mart.refresh_inventory_process(company_id)
    -- AR rotation functional, stock fields COALESCE to 0 if OITW missing

mart.refresh_finance_process(company_id)
    -- AR aging functional, AP aging inserts zeros until OPCH in STG

mart.refresh_all_processes(company_id)
    -- calls all 4 refresh_*_process; returns combined rows_affected
    -- wraps each in BEGIN/EXCEPTION so one failing process doesn't abort others
```

**Defensive pattern (applied to all purchasing/inventory/AP functions):**
```sql
IF EXISTS (SELECT FROM information_schema.tables
           WHERE table_schema = 'stg' AND table_name = 'purchase_order') THEN
    -- actual INSERT/UPDATE
ELSE
    -- INSERT zeros / skip
END IF;
```

### Key formulas

| KPI | Formula |
|---|---|
| `net_sales_amount` | `gross_sales_amount - credit_memo_amount` |
| `avg_ticket_amount` | `gross_sales_amount / NULLIF(invoice_count, 0)` |
| `days_since_last_purchase` | `CURRENT_DATE - last_invoice_date` |
| `fill_rate_pct` | `delivered_qty / NULLIF(ordered_qty, 0)` |
| `coverage_days` | `on_hand / NULLIF(avg_daily_sales_qty, 0)` |
| `ar_overdue_amount` | `SUM WHERE doc_due_date < CURRENT_DATE` |
| `overdue_risk_pct` | `overdue_amount / NULLIF(ar_total, 0)` |
| `estimated_margin_pct` | `estimated_margin_amount / NULLIF(net_sales_amount, 0)` — NULLABLE |

### Docs
- Updates `docs/databision-process-kpi-catalog.md`
- New `docs/supabase-mart-process-model.md`
- New `docs/ksdepor-kpi-formulas.md`

---

## Sprint 8D — ops Schema (Supabase PostgreSQL)

### Goal
Operational monitoring: track every extractor run, page log, transform run, data quality issues, alert rules, and pipeline health — all append-only, no EF entities.

### Migration
**File:** `20260608_XXXXXX_AddOpsSchema.cs` (StagingDbContext, SQL only)

### Tables (7)

```sql
ops.extractor_run       PK: run_id UUID, status: RUNNING|SUCCESS|WARNING|ERROR
ops.extractor_page_log  PK: page_log_id UUID, FK: run_id
ops.transform_run       PK: transform_run_id UUID, per process_code
ops.data_quality_issue  PK: issue_id UUID, severity: INFO|WARNING|ERROR|CRITICAL
ops.alert_rule          PK: alert_rule_id UUID, is_active bool
ops.alert_event         PK: alert_event_id UUID, FK: alert_rule_id, status: OPEN|ACKNOWLEDGED|RESOLVED
ops.pipeline_health     PK: company_id TEXT, health_status: OK|WARNING|ERROR|UNKNOWN
```

### Alert rules seeds (8)

| alert_code | process_code | severity | condition |
|---|---|---|---|
| EXTRACTOR_NOT_RUN_RECENTLY | OPERATIONS | WARNING | last_extractor_run_at > 2h ago |
| MART_EMPTY | OPERATIONS | ERROR | mart tables have 0 rows |
| STG_EMPTY | OPERATIONS | ERROR | stg tables have 0 rows |
| SALES_DROP_DAILY | SALES | WARNING | today's sales < 30% of 7d avg |
| STOCKOUT_ITEMS | INVENTORY | WARNING | inventory_stock_dashboard has STOCKOUT rows |
| AR_OVERDUE_HIGH | FINANCE | WARNING | overdue_risk_pct > 0.20 |
| DATA_QUALITY_ERRORS | OPERATIONS | ERROR | unresolved data_quality_issue with severity ERROR |
| TRANSFORM_FAILED | OPERATIONS | ERROR | transform_run with status ERROR in last 24h |

### Functions (3)

```sql
ops.refresh_pipeline_health(p_company_id TEXT)
    -- UPSERT into ops.pipeline_health based on last extractor/transform runs
    -- Sets health_status: OK if last run < 2h ago and no errors

ops.evaluate_alert_rules(p_company_id TEXT)
    -- Checks all active alert_rules for the company
    -- INSERTs into ops.alert_event when condition is met
    -- Does NOT duplicate: skips if OPEN event already exists for same rule

ops.log_data_quality_issue(
    p_company_id TEXT, p_process_code TEXT, p_severity TEXT,
    p_issue_code TEXT, p_issue_message TEXT,
    p_source_table TEXT DEFAULT NULL, p_source_key TEXT DEFAULT NULL)
    -- Simple INSERT helper; callable from any MART refresh function
```

### Docs
- `docs/databision-monitoring-alerts.md`
- `docs/ksdepor-production-readiness-plan.md`

---

## Implementation Order

```
8A (cfg schema + entity)
    → build + test ✓
8B (paginator + job refactors + tests)
    → build + test ✓
8C (mart process schemas)
    → build + test ✓
8D (ops schema)
    → build + test ✓
Final: git status --short
```

Each sprint gates on `dotnet build DataBision.sln && dotnet test DataBision.sln --no-build`. Stop on failure.

---

## Constraints

- No `dotnet ef database update`
- No frontend changes
- No `appsettings.Development.json` changes
- No secrets printed
- No full load against SAP (MaxPages default capped, PageSize low for tests)
- No git add / commit / push

---

## Risks & Open Items

| Risk | Mitigation |
|---|---|
| `@odata.nextLink` format varies by SL version | Paginator falls back to `$skip` if no `nextLink` in response |
| PURCHASING/INVENTORY STG tables don't exist | All refresh functions are defensive (check table existence) |
| `coverage_days` = 0/NULL for items with no sales history | `NULLIF(avg_daily_sales_qty, 0)` → NULL, not divide-by-zero |
| KSDEPOR company_id unknown | Documented in `docs/ksdepor-onboarding.md`, not seeded |
| StagingDbContext migration snapshot drift | Run `dotnet ef migrations add` properly via `--project` and `--startup-project` flags |
| cfg.company_process_enabled composite PK in EF | Use `HasKey(e => new { e.CompanyId, e.ProcessCode })` in config class |

---

*Spec status: Approved. Next step: writing-plans.*
