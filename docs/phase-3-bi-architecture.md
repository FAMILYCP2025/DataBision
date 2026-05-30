# DataBision — Phase 3 BI Architecture

Status: **Design only — no implementation.** Brutally honest evaluation of the architecture required to take DataBision from stub to enterprise-grade Power BI Embedded multi-tenant SaaS.

> **⚠️ SUPERSEDED (2026-05-29):** Power BI Embedded ya no es el núcleo del producto. DataBision Native BI (React + ECharts) es el motor de visualización principal. Ver [ADR-002](adr/ADR-002-bi-layer.md) y [ADR-006](adr/ADR-006-native-bi-vs-powerbi.md). Este documento se conserva como referencia para cuando se ofrezca Power BI como add-on opcional o se evalúe Embedded en Fase 4 (30+ clientes).

---

## 1. Executive summary

**Recommended target architecture:** Centralized warehouse + single Power BI workspace + shared dataset + RLS by `CompanyId`. Premium per User licensing for the first 5–20 customers, migrate to Embedded reserved capacity (A2+) once concurrent named users exceed ~50.

**Top 3 blockers** that must be resolved before any real customer:
1. **Service Principal not provisioned** — nothing real moves until Azure AD app + workspace grant is done.
2. **No ETL story for SAP B1** — the hardest, most under-estimated part of the stack. SAP B1 customers vary wildly (HANA / SQL Server, on-prem / cloud, with/without Service Layer).
3. **No automated RLS test** — RLS is the single security boundary in shared-dataset mode. One misconfigured DAX rule leaks data across tenants.

**Cost reality check:**
- Premium per User: $10/user/month → cheap for the first 50–100 named users.
- Embedded A1: $735/month → roughly 5–10 active customers with moderate refresh.
- Embedded A4: $5,889/month → ~50–100 customers.
- Egress (warehouse → PBI): trivial at small scale, watch at >100 GB/month.

**Hard truth:** This is a 12–18 month roadmap before "production-grade enterprise" is real. The current backend/frontend foundation is solid; the BI/ETL stack from the warehouse down is the long pole.

---

## 2. Current state assessment

| Layer | State | Gap |
|---|---|---|
| Auth / Multi-tenant | ✓ JWT RS256, tenant via subdomain, audit | Embed endpoint missing rate limit |
| Permissions | ✓ Module + Report level via `UserPermission` | OK for MVP |
| Power BI stub | ✓ `IPowerBIService` + `embed-config` endpoint | Token generation is `NotImplementedException` |
| Frontend embed container | ✓ 5 states (loading/forbidden/not_configured/error/ready) | No real `powerbi-client` integration |
| Audit | ✓ `REPORT_VIEWED`, `EMBED_CONFIG_REQUESTED/DENIED` | Missing refresh, export, duration, dataset events |
| Warehouse | ✗ does not exist | Required for shared-dataset architecture |
| ETL | ✗ does not exist | Hardest single piece |
| RLS in dataset | ✗ does not exist | Defined here, not implemented |
| Service Principal | ✗ not provisioned | **BLOCKER** |

---

## 3. Workspace strategy — option analysis

### Option A — Workspace per company

| Dimension | Verdict |
|---|---|
| Cost | High. Per-workspace refresh consumes capacity-minutes linearly. |
| Security | High isolation — RLS not required for cross-tenant protection. |
| Maintenance | **Painful.** One pbix change × N tenants = deployment hell. Need automated deployment pipeline. |
| Performance | Good. Each tenant has dedicated capacity allocation. |
| Onboarding | Slow. Workspace provisioning, pbix deployment, gateway setup per customer. |
| Support | Complex. Per-tenant support knowledge. |
| Licensing | Each workspace needs Premium-backed capacity. |

### Option B — Shared workspace + RLS

| Dimension | Verdict |
|---|---|
| Cost | Low. One dataset refresh feeds all tenants. |
| Security | **Medium-High RISK.** RLS is the single boundary. One bad DAX rule = leak. |
| Maintenance | Excellent. One pbix → one update for everyone. |
| Performance | Risk: large tenant degrades shared dataset (RLS evaluated per query). |
| Onboarding | Instant: add `CompanyId` row, done. |
| Support | Simple. One stack to know. |
| Licensing | One workspace, one capacity allocation. |

### Option C — Hybrid (template + per-tenant workspace deployed from CI)

| Dimension | Verdict |
|---|---|
| Cost | Medium-High. |
| Security | High. |
| Maintenance | Medium. CI/CD does the multiplication. |
| Performance | Good. |
| Onboarding | Automated but multi-step. |
| Support | Medium. |
| Licensing | Same as Option A. |

### Decision matrix

| Factor (weight) | A: per-tenant | B: shared+RLS | C: hybrid |
|---|---|---|---|
| Cost (3) | 2 | **5** | 3 |
| Security (5) | 5 | 3 | **5** |
| Maintenance (4) | 1 | **5** | 3 |
| Performance (3) | **4** | 3 | **4** |
| Onboarding (3) | 1 | **5** | 3 |
| Support (2) | 2 | **5** | 3 |
| Licensing flex (2) | 3 | **4** | 3 |
| **Weighted total** | 50 | **84** | 71 |

### Recommendation

**Option B (Shared workspace + RLS by CompanyId).** Adopt aggressively for MVP/Beta. Move premium-tier customers to Option C (their own workspace cloned from template) only when they ask for custom KPIs or have data-residency requirements.

Why not C across the board: at 100 customers, you have 100 workspaces to monitor, 100 refresh schedules, 100 deployment targets. That is a full-time job, not an architecture.

**Caveat:** If even one customer has a hard data-isolation contract (banking, healthcare), they go to a dedicated workspace. Make that an explicit tier.

---

## 4. RLS model (design — not implemented yet)

### Tables in the semantic model

```
dim_Company
  CompanyId       INT  PK
  CompanySlug     NVARCHAR(50)  UNIQUE   ← matches JWT company_slug claim
  CompanyName     NVARCHAR(200)
  IsActive        BIT

dim_User
  UserId          INT  PK
  CompanyId       INT  FK → dim_Company
  Email           NVARCHAR(200)
  IsActive        BIT

fact_*  (every fact table has CompanyId column, indexed)
  CompanyId       INT  FK
  ...
```

### DAX role

```dax
// Role: CompanyRole
[CompanySlug] = USERPRINCIPALNAME()
```

Filter propagates from `dim_Company` to all fact tables via the model relationships. **Every fact table must join to `dim_Company` with cross-filter direction Single (dim → fact).** Bi-directional cross-filter on `dim_Company` is forbidden — it breaks RLS isolation.

### EffectiveIdentity in the embed token

```json
{
  "username": "demo",           // matches dim_Company.CompanySlug
  "roles": ["CompanyRole"],
  "datasets": ["<dataset-id>"]
}
```

`username` is the **tenant slug**, not the end-user email. This is deliberate: all users of the same tenant see the same data, and tenant-internal permissions are enforced by DataBision's `UserPermission` table at the report-visibility level.

### Validation strategy (mandatory before second customer)

1. **Unit test (DAX):** for each fact table, assert that `CALCULATETABLE(<fact>, REMOVEFILTERS(dim_Company)) > <fact>` filtered by company.
2. **Integration test:** generate embed token for tenant A, query DAX `EVALUATE TOPN(10, fact_Sales)`, count rows, assert `MAX(CompanyId) == tenant A's id`.
3. **RLS smoke test in CI:** run automated test that requests tenant A's report with tenant B's identity → expect 0 rows.
4. **Manual cross-tenant attempt** before every dataset deployment.

This is non-negotiable. Without it, you have a one-DAX-typo away from a data breach.

---

## 5. Embed token architecture

### Lifecycle

```
[Client opens report]
      │
      ▼
[GET /api/reports/{id}/embed-config]
      │
      ▼
[Check cache (Redis): key = userId:reportId:tenant]
   │ hit (>5min remaining)        │ miss
   ▼                              ▼
[Return cached token]    [Call PBI GenerateTokenInGroup]
                                  │
                                  ▼
                            [Cache with TTL=55min]
                                  │
                                  ▼
                            [Return token]
```

### Specifications

| Property | Value | Justification |
|---|---|---|
| Token lifetime (PBI default) | 60 min | Max allowed by PBI; trade-off vs renewal frequency. |
| Server cache TTL | 55 min | 5 min buffer before expiry. |
| Frontend auto-refresh | 5 min before expiry | Power BI SDK supports `report.refresh()` with new token. |
| Cache key | `tenant:userId:reportId` | Per-user (audit traceability), per-report (RLS context). |
| Cache backend | Redis (production) / IMemoryCache (MVP) | Move to Redis when going multi-instance. |
| Eviction triggers | Logout, permission revoked, tenant changed | Use `qc.clear()`-equivalent on backend. |
| Telemetry per generation | userId, reportId, tenant, latency, success | Goes to AuditLog + Prometheus/AppInsights. |

### Rate limits

- **Backend:** 30 token requests/min/user (defense against compromised JWT).
- **Power BI API ceiling:** 60 req/min/workspace — must not exceed. Cache + dedup + queue.
- **Per-tenant cap:** 200 req/min/tenant (a single tenant cannot DoS the workspace).

### What is NOT possible

- **Token invalidation.** Once issued, a token is valid until expiry. Mitigate via short TTL + cache eviction (so it's not reused).
- **Replay protection by PBI.** Sub-tenant or sub-report scoping is via `EffectiveIdentity` only; PBI doesn't validate request fingerprint. Pair with HTTPS + httpOnly cookies + same-origin checks.

---

## 6. Dataset strategy

### Three options

| Strategy | When | Cost | Complexity |
|---|---|---|---|
| **Dataset per company** | Tenant-isolation contract; tenant manages own data | High (N × refresh) | High |
| **Shared dataset with RLS** | Standard SaaS path | Low | Medium (RLS) |
| **Centralized semantic model + multiple datasets** | Fabric era; reuse KPI defs | Medium | High |

### Recommendation by phase

- **MVP / Beta:** Shared dataset, single source of truth, RLS by CompanyId. Star schema.
- **Production:** Same shared dataset. Add per-tenant materialized views in the warehouse for hot data.
- **Enterprise:** Migrate to Fabric semantic model. Premium customers get dedicated dataset cloned from semantic model.

### Star schema sketch (Gold layer)

```
dim_Date
dim_Company    ← RLS anchor
dim_Customer
dim_Product
dim_Warehouse
dim_Salesperson
dim_GLAccount
dim_FiscalPeriod

fact_Sales            (CompanyId, DateKey, CustomerKey, ProductKey, ...)
fact_Purchases        (CompanyId, DateKey, VendorKey, ProductKey, ...)
fact_Inventory        (CompanyId, DateKey, WarehouseKey, ProductKey, Qty, Value)
fact_GLTransactions   (CompanyId, DateKey, AccountKey, Amount)
fact_ARAging          (CompanyId, SnapshotDateKey, CustomerKey, Bucket0_30, ...)
fact_APAging          (CompanyId, SnapshotDateKey, VendorKey, ...)
```

Every fact joins `dim_Company` with single-direction cross-filter (RLS prerequisite).

---

## 7. ETL architecture (design — not implemented)

### Source connection matrix

| SAP B1 deployment | Recommended connector |
|---|---|
| HANA, cloud-hosted | Service Layer (OData REST) — preferred |
| HANA, on-prem | HANA connector via Power BI Gateway |
| SQL Server, on-prem | ODBC via Gateway, or extract via .NET worker |
| SQL Server, Azure | Direct connection with managed identity |

**Service Layer is the cloud-friendly path.** Always use it when available. SQL access is faster but couples you to SAP B1 internals.

### Medallion layers

```
Bronze (raw)            Silver (cleaned)         Gold (dimensional)
─────────────────       ─────────────────        ─────────────────
OINV.json (raw)    →    inv_header (typed) →    fact_Sales
INV1.json          →    inv_lines             dim_Customer
OCRD.json          →    customers             dim_Product
OITM.json          →    items                 ...

Partition: CompanyId/Year/Month     Conformed dimensions     Star schema
Storage: ADLS Gen2 Parquet           SQL/Delta                Power BI dataset
```

### Incremental refresh

- **Watermark:** SAP B1 has `UpdateDate` on most transactional tables. Use it.
- **Per-table metadata in warehouse:** `etl_watermarks(company_id, table_name, last_extracted_at)`.
- **Idempotent loads:** upsert by natural key (`DocEntry` for SAP B1 docs).
- **Late-arriving data:** re-extract last 7 days every run to catch backdated changes.

### Scheduler

- **MVP:** Azure Function timer trigger, sequential per customer.
- **Beta:** Azure Data Factory with per-customer pipeline parameters.
- **Production:** Fabric Pipelines + control table for SLA-based prioritization.

### Frequency by tier

| Customer tier | Bronze | Silver | Gold | Power BI refresh |
|---|---|---|---|---|
| Standard | Daily | Daily | Daily | Daily 6am |
| Pro | 4×/day | 4×/day | 4×/day | 4×/day |
| Enterprise | Hourly | Hourly | Hourly | Hourly (DirectQuery on hot tables) |

### Tools by phase

- **MVP / POC:** .NET worker service + Azure Storage Queue (cheap, full control).
- **Beta:** Azure Data Factory (visual, monitored, costs slightly more).
- **Production:** Stay on ADF or move to Fabric Pipelines.

---

## 8. SAP Business One specifics

### Critical tables

| Table | Purpose |
|---|---|
| `OINV` / `INV1` | A/R invoices header + lines |
| `ORDR` / `RDR1` | Sales orders |
| `OPCH` / `PCH1` | A/P invoices |
| `ORIN` / `RIN1` | Credit memos |
| `OITM` / `OITW` | Items + per-warehouse stock |
| `OCRD` | Business partners (customers + vendors) |
| `JDT1` / `OJDT` | Journal entries header + lines |
| `OACT` | Chart of accounts |
| `OWHS` | Warehouses |
| `OINM` | Inventory transactions (movements) |
| `OSLP` | Salespersons |
| `OUSR` | SAP users (for source-side audit if needed) |

### Pitfalls

- **Multi-currency.** Every monetary column has FC / SC / LC variants. Standardize on LC (local currency) for fact tables; preserve SC (system currency) for consolidated reporting.
- **UDF/UDT.** Customer-specific fields. Don't replicate by default — onboarding step to map customer's UDFs.
- **Year-end closing.** Generates massive JDT1 deltas. Plan for refresh spikes in January.
- **Inventory snapshots.** Use `OINM` for movements; reconstruct point-in-time stock via running sum, not `OITW` (which is current state).
- **Posting periods.** Tie all dates to `OFPR` (fiscal periods) for accounting reports.
- **Soft deletes.** SAP B1 doesn't physically delete documents. Watch for `Canceled = 'Y'` and `DocStatus = 'C'`.
- **Tax detail.** Tax breakdown lives in `INV4` and `INV5` linked tables. Lines table alone is incomplete for tax reporting.

### UDO (User-Defined Objects)

Customers may have UDOs for custom processes (quality control, projects). These are stored in custom tables with `@` prefix. **Onboarding question:** does the customer use UDOs? If yes, scope work for that customer separately.

---

## 9. BI audit architecture

### Event catalog (proposed)

| Event | When | Source |
|---|---|---|
| `EMBED_CONFIG_REQUESTED` | Endpoint hit | Backend ✓ |
| `EMBED_CONFIG_DENIED` | 403/501 | Backend ✓ |
| `REPORT_VIEWED` | 200 from embed-config | Backend ✓ |
| `REPORT_VIEW_DURATION` | Frontend `unload` / tab close | Frontend → backend |
| `REPORT_REFRESHED` | User clicks refresh in embed | Frontend → backend |
| `REPORT_EXPORTED` | Export-to-PDF or PPTX | Frontend → backend |
| `REPORT_ERROR` | `report.on('error')` callback | Frontend → backend |
| `BOOKMARK_CREATED` / `_APPLIED` | Bookmarks API | Frontend → backend |
| `FILTER_APPLIED` | Filter changes (debounced) | Frontend → backend |
| `DATASET_REFRESH_STARTED` | Power BI activity log | Backend pull from PBI |
| `DATASET_REFRESH_COMPLETED` | Power BI activity log | Backend pull from PBI |
| `DATASET_REFRESH_FAILED` | Power BI activity log | Backend pull from PBI |

### Storage tier

| Tier | Retention | Backend | Query pattern |
|---|---|---|---|
| Hot | 90 days | Main SQL DB | Operational (admin dashboards) |
| Warm | 1 year | Azure Blob Parquet | Analytical (cohort, trends) |
| Cold | 1–7 years | Azure Blob archive | Compliance only |

### Per-tenant metrics (computed nightly)

- DAU / MAU per tenant
- Reports viewed per tenant per day
- Avg session length per tenant
- Most-viewed reports per tenant
- Error rate per tenant per report
- p95 embed latency per tenant

### Telemetry stack

- **Logs:** Serilog → Azure Log Analytics
- **Metrics:** Prometheus or Application Insights
- **Audit:** SQL DB (transactional) → nightly export to Parquet
- **Tracing:** OpenTelemetry → AI / Jaeger

---

## 10. Frontend enterprise features

### Capability matrix (powerbi-client-react)

| Feature | API | Effort | Phase |
|---|---|---|---|
| Embed report | `<PowerBIEmbed type="report" />` | XS | MVP |
| Fullscreen | `report.fullscreen()` | XS | MVP |
| Responsive layout | `settings.layoutType: MobilePortrait` | S | Beta |
| Bookmarks | `report.bookmarksManager.capture()/apply()` | M | Beta |
| Filters / deep linking | `report.setFilters([...])` | M | Beta |
| Print / Export | `report.print()` / export REST API | M | Beta |
| Refresh | `report.refresh()` | XS | MVP |
| Tab navigation | `report.getPages()` / `setPage()` | S | MVP |
| Lazy loading | Don't embed until tab active | S | Beta |
| Favorites | Per-user table in main DB | S | Beta |
| Recent reports | Per-user table | S | Beta |
| Pre-warm token on hover | `prefetchQuery` on link hover | S | Production |

### Memory leak watch

`powerbi-client` does not always tear down the iframe. On unmount:

```typescript
useEffect(() => () => {
  if (reportRef.current) {
    reportRef.current.off('loaded')
    reportRef.current.off('rendered')
    reportRef.current.off('error')
  }
  pbiService.reset(containerRef.current)
}, [])
```

Without this, navigating between reports leaks iframes — observable in DevTools memory snapshots after 10+ navigations.

---

## 11. Roadmap

### MVP — weeks 1–8

Goal: one paying customer, real Power BI embed.

- [ ] Provision Azure AD app + Service Principal
- [ ] Grant SP `Workspace Contributor` on dev workspace
- [ ] Add `Microsoft.PowerBI.Api` + `Microsoft.Identity.Client` NuGet packages
- [ ] Implement `PowerBIService.GenerateEmbedTokenAsync` (real)
- [ ] Set `PowerBI:Enabled=true` in dev
- [ ] One demo pbix with sample data + RLS role `CompanyRole`
- [ ] Frontend: install `powerbi-client-react`, replace `EmbedReady` stub
- [ ] Add rate limiting to `/embed-config` (30/min/user)
- [ ] Add embed token caching (IMemoryCache, 55min TTL)
- [ ] First customer pilot: manual data load (CSV → Azure SQL → dataset)
- [ ] Choose licensing: **Premium per User** for first 5 customers

### Beta — months 3–6

Goal: 5–10 paying customers, repeatable onboarding.

- [ ] Automate workspace + pbix deployment (if Option C ever needed)
- [ ] Automated RLS smoke test in CI (mandatory)
- [ ] ETL POC for one customer (one fact table, one source)
- [ ] Audit pipeline: extended event set (refresh, export, duration, error)
- [ ] Power BI Gateway for on-prem SAP B1 refresh
- [ ] Admin dashboard: per-tenant refresh status, audit log viewer
- [ ] Bookmarks, filters, fullscreen in frontend
- [ ] Per-customer cost attribution (capacity-minutes tracked)
- [ ] First DR test (restore workspace from backup)

### Production — months 6–12

Goal: 20–50 customers, SLA-backed.

- [ ] Centralized warehouse (Azure SQL or Synapse Serverless)
- [ ] Multi-tenant ETL with ADF (one pipeline per source type, parameterized by customer)
- [ ] Migrate pilots to shared dataset + RLS
- [ ] Capacity scaling plan: A1 → A2 → A4 with monitoring thresholds
- [ ] SLA: 99.5% uptime, refresh within 4hr, embed latency p95 < 3s
- [ ] DR plan tested quarterly
- [ ] Audit data archival pipeline (hot → warm → cold)
- [ ] Premium-tier customers on dedicated workspace (Option C)

### Enterprise — year 2+

- [ ] Fabric semantic model migration
- [ ] Copilot for Power BI integration
- [ ] Sensitivity labels + IRM
- [ ] Multi-region deployment (EU, NA)
- [ ] Custom KPI builder for premium tier
- [ ] Cross-tenant benchmarking (aggregated, anonymized)

---

## 12. Risk register

### Cost risks

| Risk | Impact | Mitigation |
|---|---|---|
| A1 capacity saturation at ~10 customers | $5k+/mo unplanned upgrade | Monitor capacity utilization; alert at 70% |
| Refresh hours multiply per tenant | Capacity exhaustion at scale | Shared dataset (Option B) reduces refresh load 10× |
| Embedded vs Premium per User mispricing | Up to 5× overspend | Calculate breakeven at named-user count — decide explicitly |
| Egress at scale | $0.087/GB outbound > $1k/mo at 10 TB | Co-locate warehouse + PBI in same region |

### Technical risks

| Risk | Impact | Mitigation |
|---|---|---|
| A1 = 3 GB dataset RAM cap | Large tenants break shared dataset | Per-tenant row limits; archive >2 yr; consider DirectQuery |
| 6 concurrent refresh cap on A1 | Refresh queue stalls | Stagger schedules; upgrade to A2 |
| PBI API rate limit (60/min/workspace) | Throttling at peak | Token cache + queue + backoff |
| DirectQuery latency | Poor UX on hot tables | Aggregations table; import for hot, DQ for cold |
| Large tenant degrades shared dataset | All tenants slowed | Move large tenant to dedicated workspace |

### Security risks

| Risk | Impact | Mitigation |
|---|---|---|
| **RLS misconfiguration** | Cross-tenant data leak (existential) | Automated RLS test, mandatory before deploy |
| Embed token interception | Time-bounded user impersonation | HTTPS, httpOnly cookies, short TTL, IP binding consideration |
| Service Principal credential leak | Full workspace access | Key Vault, quarterly rotation, scoped permissions |
| Refresh credentials in PBI | Source data compromise | Service Principal for source where possible; OAuth instead of basic auth |
| Audit log tampering | Compliance failure | Append-only table; backup to immutable storage |

### Operational risks

| Risk | Impact | Mitigation |
|---|---|---|
| Customer's SAP B1 schema variant | Onboarding stalls | Standard onboarding questionnaire; per-customer schema mapping |
| Refresh cascade failures | Multiple tenants affected | Isolated per-tenant Bronze; shared Silver/Gold only |
| PBI service outage (~3–4/yr, 30min) | User-visible downtime | Accept SLA; show "PBI service status" link on error |
| Customer-side gateway failure | Refresh fails for that tenant | Customer-owned but documented; status page |
| pbix versioning chaos | Production drift | Use `.pbip` format + Git from day 1 |

### Token abuse / memory

| Risk | Mitigation |
|---|---|
| User shares embed URL+token | Short TTL (60min); origin checks; identity claim required |
| Server cache leak | Encrypt at rest if in Redis; evict on logout |
| Frontend iframe leak | Mandatory cleanup on unmount (see §10) |
| Cache memory growth | TTL + max entries per process; Redis at multi-instance |

---

## 13. Executable recommendations

### URGENT (this week)

1. **Provision Azure AD Service Principal.** Nothing else in Phase 3 moves until done. Block on this.
2. **Decide capacity model.** I recommend Premium per User ($10/user/mo) for first 5 customers; move to Embedded A2 when active named users > 50.
3. **Add rate limit to `/embed-config`.** 30 req/min/user. Defense against compromised token.
4. **Decide pbix versioning.** Use `.pbip` (Power BI Project format) + Git. Standard since 2024.
5. **Lock the workspace strategy:** Shared workspace + RLS by `CompanyId`. Document the decision.

### IMPORTANT (next 1–3 months)

6. **Embed token cache** (IMemoryCache → Redis when multi-instance).
7. **Extended audit events:** `REPORT_VIEW_DURATION`, `REPORT_EXPORTED`, `REPORT_REFRESHED`, `DATASET_REFRESH_*`.
8. **Automated RLS test in CI.** Non-negotiable before second customer.
9. **ETL POC.** One customer, one fact table, one source. Validate end-to-end latency and cost.
10. **Audit log archival.** Schema + job for hot → warm → cold tiering.
11. **Capacity monitoring.** Dashboard showing capacity utilization, refresh queue depth, embed token rate.

### OPTIONAL (months 3–6)

12. Workspace provisioning automation (if Option C ever needed for premium tier).
13. Admin dashboard for per-tenant refresh trigger + status.
14. Customer-facing usage metrics (DAU, top reports).
15. `.pbip` deployment automation via Azure DevOps.

### FUTURE (year 2+)

16. Fabric semantic model migration.
17. Copilot / AI integration.
18. Sensitivity labels + IRM.
19. Multi-region deployment.
20. Aggregated cross-tenant benchmarking (anonymized).

---

## 14. Brutally honest assessment

### What's solid

- Clean Architecture layering is correct; the Phase 2 stub respects it.
- Multi-tenancy is modeled correctly (subdomain → tenant claim → company filter).
- Auth/session hardening from Phases 1.5 and 1.6 is mature.
- Frontend `ReportEmbedContainer` with 5 states is a good base; replacement to real embed is a swap, not a rewrite.

### What's weak

- **No ETL story.** This is the hardest part of the stack and there is currently zero design beyond this document. Plan 2–3 engineer-months for a real first version.
- **No automated RLS test.** The single biggest data-leak risk and there is no test in CI.
- **Service Principal not provisioned.** Real demo is blocked.
- **pbix versioning is undefined.** Will become a problem at second customer.
- **Audit log will balloon.** No partitioning or archival plan in code yet.
- **Embed token rate limiting is missing.** Compromised JWT → cheap DoS on the workspace.

### What scares me

- **RLS as the only security boundary.** I cannot overstate this. Build the RLS test first, then build the rest. A leak at customer #3 ends the company.
- **SAP B1 onboarding heterogeneity.** Every customer is different. Plan 1–2 weeks per customer in Beta. Don't promise faster.
- **Capacity SKU cost curve.** A1 is generous for 5 customers, painful at 20. Have the A2/A4 migration plan documented before customer #10.
- **Premium per User vs Embedded.** Different cost shapes. With <100 named users, Premium per User wins by a lot. With many concurrent users, Embedded wins. Pick deliberately, not by default.

### What I'd do differently

- **Don't pre-build for workspace-per-tenant.** It's an operational nightmare. Default to shared + RLS. Move customers off shared only on explicit contract requirement.
- **Use Premium per User for first 5 customers.** Cheaper, simpler, lets you delay Embedded capacity decisions until you have real usage data.
- **Start the ETL POC immediately in parallel with the embed implementation.** The embed work is small; ETL is large. Don't sequence them.
- **Treat the demo .pbix as production code from day 1.** `.pbip` + Git + reviewed PRs.
- **Pair-program the first DAX role with two engineers.** RLS bugs are subtle. Four eyes minimum.

### Honest timeline

- **Real customer demo with embedded PBI:** 2–4 weeks (assuming Service Principal is unblocked).
- **First customer in production with real ETL:** 3–4 months.
- **5 customers in production with shared dataset + RLS:** 6–9 months.
- **20 customers, SLA-backed:** 12–18 months.
- **Enterprise (Fabric, AI, multi-region):** 24+ months.

Anything faster than this requires either cutting scope (no ETL — manual loads) or growing the team (2–3 engineers minimum for the warehouse + ETL track).
