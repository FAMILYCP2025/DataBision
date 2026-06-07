# Production Readiness Checklist

Pre-launch checklist for DataBision. Complete all items before going live with a production tenant.

---

## Infrastructure

- [ ] Azure SQL (or Supabase PostgreSQL) provisioned in production region
- [ ] Database migrations applied (`dotnet ef database update`)
- [ ] `raw`, `stg`, `mart`, `ctl`, `audit` schemas exist and are populated correctly
- [ ] Connection strings use production credentials (not dev)
- [ ] Azure Blob Storage account provisioned for logo uploads
- [ ] SSL certificates valid for `*.databision.app` and `admin.databision.app`
- [ ] DNS wildcard `*.databision.app` → load balancer / App Service
- [ ] `admin.databision.app` DNS → admin App Service

## API (`DataBision.Api`)

- [ ] `dotnet build` 0 errors, 0 warnings
- [ ] All 34 tests pass (`dotnet test --no-build`)
- [ ] RSA key pair generated for JWT signing (`Jwt__PrivateKey`, `Jwt__PublicKey`)
- [ ] Keys stored in Azure Key Vault or equivalent — not in appsettings.json
- [ ] `App__BaseDomain` = `databision.app`
- [ ] `ASPNETCORE_ENVIRONMENT` = `Production`
- [ ] CORS policy allows only `*.databision.app` and `admin.databision.app`
- [ ] HTTPS enforced (HSTS enabled)
- [ ] Rate limiting configured on `/api/auth/login` and `/api/auth/refresh`
- [ ] Health check endpoint responds `200 OK`
- [ ] Audit log writes verified (`AuditService` logs `VIEW_REPORT`, `LOGIN`, write events)

## Power BI Embedded

- [ ] Service Principal created in Azure AD (not Master User)
- [ ] Service Principal added to Power BI workspace as Member
- [ ] `PowerBI__TenantId`, `PowerBI__ClientId`, `PowerBI__ClientSecret` set in production config
- [ ] `PowerBI__WorkspaceId` points to production workspace
- [ ] RLS role `CompanyRole` exists in every published report
- [ ] RLS filter: `[company_slug] = USERPRINCIPALNAME()` (or equivalent)
- [ ] Embed token generation tested for at least one company
- [ ] Embed token expiry handled (5 min auto-refresh in frontend)
- [ ] Power BI dataset connected to production Supabase (not dev)

## Extractor (`DataBision.Extractor`)

- [ ] Installed as Windows Service (`sc create ...` or NSSM) — see `docs/extractor-windows-service-installation.md`
- [ ] Service runs under a dedicated service account (not LocalSystem)
- [ ] `appsettings.Production.json` (or env vars) set for production SAP + Supabase
- [ ] SAP B1 credentials stored securely (not plaintext in appsettings)
- [ ] `--schedule` mode enabled with appropriate frequency (e.g., every 60 min)
- [ ] Post-extraction transform auto-triggered (Task Scheduler or service hook)
- [ ] Extraction logs written to `C:\ProgramData\DataBision\logs\` (or equivalent)
- [ ] Log retention policy set (rotate weekly, keep 30 days)
- [ ] Alerting configured for extraction failures (email or monitoring tool)
- [ ] `ctl.source_object_config` rows seeded for all SAP objects

## Multi-tenancy

- [ ] `TenantMiddleware` resolves `company_id` from `Host` header (not query param) in production
- [ ] Every data query has explicit `company_id` filter (no cross-tenant leakage)
- [ ] SuperAdmin JWT has `company_id = null` — verified in prod
- [ ] First production company created via SuperAdmin panel
- [ ] Company slug matches `*.databision.app` subdomain

## Frontend

- [ ] `npm run build` succeeds, 0 errors
- [ ] `npm run lint` passes
- [ ] `VITE_API_BASE_URL` points to production API
- [ ] Tenant config cached in `localStorage` (no color flash on load)
- [ ] Power BI embed renders correctly for at least one company in production browser
- [ ] Auth flow: login → JWT → refresh token rotation works end-to-end
- [ ] 401 interceptor auto-refreshes token before expiry

## Security

- [ ] No secrets committed to git (scan with `git log --all -- "*.json"`)
- [ ] No `appsettings.Development.json` deployed to production
- [ ] Refresh tokens stored **hashed** in DB (not plaintext)
- [ ] Refresh token rotation: old token invalidated on use
- [ ] SQL injection: no string interpolation in `FromSqlRaw` calls
- [ ] No `any` TypeScript in frontend (strict mode)
- [ ] OWASP top 10 review completed

## Data quality

- [ ] KPI validation queries pass (0 delta) — see `docs/sql/kpi-validation-queries.sql`
- [ ] MART row counts reasonable (non-zero for active company)
- [ ] `mart.sales_kpi_summary` has exactly 1 row per company
- [ ] No cancelled invoices in STG tables
- [ ] STG `transformed_at_utc` < 24h old at go-live

## Monitoring & operations

- [ ] Application Insights (or equivalent) wired up to API
- [ ] Structured logs (Serilog JSON sink) forwarded to log aggregator
- [ ] Uptime monitor on `https://api.databision.app/health`
- [ ] Runbook documented: how to restart extractor service, re-run failed extraction
- [ ] Backup strategy: database nightly backup, retention ≥ 30 days
- [ ] Disaster recovery: RTO and RPO defined

## Go-live sign-off

| Item | Owner | Status |
|---|---|---|
| Infrastructure provisioned | | |
| API deployed and tested | | |
| Extractor installed and running | | |
| Power BI dataset connected | | |
| First tenant onboarded | | |
| KPI validation passed | | |
| Security review | | |
