# Native BI — Operational Runbook

Reference guide for operating and debugging the Native BI backend pipeline.

---

## Architecture recap

```
SAP B1 Service Layer (HTTPS :50000)
    └── Extractor (dotnet run -- --run-once --company <id>)
            └── POST /api/ingest/sap-b1/*  (API key auth)
                    └── raw.o_inv, raw.inv_1, raw.o_rin, raw.rin_1, raw.o_itm, raw.o_slp, raw.o_crd
                            └── Extractor --transform --company <id>
                                    └── stg.sales_invoice, stg.credit_memo, stg.customer, stg.item, stg.salesperson
                                            └── Extractor --transform-mart --company <id>
                                                    └── mart.sales_daily, mart.sales_monthly, mart.customer_sales,
                                                        mart.item_sales, mart.salesperson_sales, mart.sales_kpi_summary
                                                                └── GET /api/client/dashboard/*
                                                                └── GET /api/client/sales/*
                                                                └── GET /api/client/sync/*
                                                                └── GET /api/client/diagnostics/*
```

---

## 1. Validate API is running

```powershell
Invoke-RestMethod http://localhost:5103/api/health
# Expected: { status: "ok", timestamp: "..." }
```

---

## 2. Validate extractor

```powershell
# Check last run result (look for "Extraction complete" or error)
dotnet run --project src\DataBision.Extractor -- --run-once --company company-dev-001 2>&1 | Select-String "complete|error|Error"
```

Check if checkpoints advanced:
```sql
SELECT sap_object, watermark_date, total_rows_ingested, last_updated_at
FROM ctl.ingest_checkpoint
WHERE company_id = 'company-dev-001'
ORDER BY sap_object;
```

---

## 3. Validate STG transform

```powershell
dotnet run --project src\DataBision.Extractor -- --transform --company company-dev-001
```

Verify:
```sql
SELECT COUNT(*) FROM stg.sales_invoice WHERE company_id = 'company-dev-001';
SELECT COUNT(*) FROM stg.credit_memo   WHERE company_id = 'company-dev-001';
```

---

## 4. Validate MART transform

```powershell
dotnet run --project src\DataBision.Extractor -- --transform-mart --company company-dev-001
```

Or full pipeline (STG + MART):
```powershell
dotnet run --project src\DataBision.Extractor -- --transform --include-mart --company company-dev-001
```

Verify row counts:
```sql
SELECT 'sales_daily'       AS t, COUNT(*) FROM mart.sales_daily       WHERE company_id = 'company-dev-001'
UNION ALL
SELECT 'sales_monthly',    COUNT(*) FROM mart.sales_monthly    WHERE company_id = 'company-dev-001'
UNION ALL
SELECT 'customer_sales',   COUNT(*) FROM mart.customer_sales   WHERE company_id = 'company-dev-001'
UNION ALL
SELECT 'item_sales',       COUNT(*) FROM mart.item_sales       WHERE company_id = 'company-dev-001'
UNION ALL
SELECT 'salesperson_sales',COUNT(*) FROM mart.salesperson_sales WHERE company_id = 'company-dev-001'
UNION ALL
SELECT 'sales_kpi_summary',COUNT(*) FROM mart.sales_kpi_summary WHERE company_id = 'company-dev-001';
```

---

## 5. Run E2E endpoint validation

```powershell
# DEV mode (no JWT)
.\scripts\dev\test-native-bi-endpoints.ps1 -BaseUrl http://localhost:5103 -CompanyId company-dev-001

# Save results
.\scripts\dev\test-native-bi-endpoints.ps1 | Tee-Object docs\validation-run-$(Get-Date -Format 'yyyyMMdd').txt
```

---

## 6. Diagnostics endpoints

```powershell
# Overall health
Invoke-RestMethod "http://localhost:5103/api/client/diagnostics/native-bi?companyId=company-dev-001" | ConvertTo-Json -Depth 5

# Table row counts
Invoke-RestMethod "http://localhost:5103/api/client/diagnostics/native-bi/tables?companyId=company-dev-001" | ConvertTo-Json -Depth 5
```

---

## 7. Review logs

```powershell
# API logs (if running in foreground)
dotnet run --project src\DataBision.Api 2>&1 | Select-String "Error|error|Exception"

# Extractor logs
dotnet run --project src\DataBision.Extractor -- --run-once 2>&1 | Select-String "error|warn|rows|complete"
```

---

## 8. Common errors and fixes

### StagingConnection empty / not configured

**Symptom:** `/api/client/dashboard/*` returns 503 or DI resolution error.  
**Fix:** Set `ConnectionStrings:StagingConnection` in `appsettings.Development.json`.  
**Check:** `dotnet run --project src\DataBision.Api` logs "StagingConnection not configured".

---

### MART tables empty / no data

**Symptom:** `/dashboard/summary` returns `{ "data": null }`.  
**Cause:** Transform hasn't run, or ran for a different `company_id`.  
**Fix:** `dotnet run -- --transform --include-mart --company <id>`

---

### Checkpoint not advancing

**Symptom:** Watermark date stays the same after multiple runs.  
**Cause:** SAP B1 returns no new records (watermark-based filtering).  
**Check:** Query SAP B1 directly for new records since watermark date.

---

### Data older than 24h (status = "warning" or "error")

**Symptom:** `/diagnostics/native-bi` returns `status = "warning"`.  
**Fix:** Run extractor + transform cycle.  
**Schedule:** Consider a Windows Task Scheduler job (see `docs/extractor-windows-service-installation.md`).

---

### JWT / company_id missing from token

**Symptom:** `/api/client/dashboard/*` returns 403 `forbidden_no_company`.  
**Cause:** User is authenticated but JWT has no `company_slug` claim (SuperAdmin token, or old token).  
**Fix:** Log out and log in again from the correct tenant subdomain.

---

### Supabase timeout / connection refused

**Symptom:** Diagnostics `staging_connection` check = "error".  
**Causes:** Supabase project paused, VPN required, wrong connection string.  
**Fix:** Check Supabase project status at supabase.com. Verify `StagingConnection` string.

---

### `ctl.extraction_run` table missing

**Symptom:** `GetLastExtractionRunAsync` throws `relation "ctl.extraction_run" does not exist`.  
**Fix:** Run migrations: `dotnet ef database update --context StagingDbContext`.

---

## 9. Security reminder

- In production, `companyId` query param is **ignored** — resolved from JWT `company_slug` claim.
- `[AllowAnonymous]` on Native BI controllers is intentional: ASP.NET must not block the pipeline so `CompanyContextResolver` can enforce security manually.
- Never expose `StagingConnection` string in API responses or logs.
- Diagnostics endpoints do NOT return connection strings or raw SQL.
