# Native BI Accounting — Production Readiness Checklist

Sprint 14F — 2026-06-18

Use this checklist when onboarding a new client to the accounting module or promoting from DEV → TST → PRD.

---

## Environment Promotion Checklist

### DEV (local development)

- [ ] Migrations applied: `dotnet ef database update --context AppDbContext`
- [ ] Migrations applied: `dotnet ef database update --context StagingDbContext`
- [ ] `StagingConnection` in `appsettings.Development.json` points to DEV Supabase project
- [ ] `AnalyticsCompanyId` set for at least one test company
- [ ] At least one test extraction completed (OACT + OJDT — JDT1 lines are embedded in OJDT extraction)
- [ ] `mart.refresh_accounting_all()` ran without errors
- [ ] `GET /api/client/bi/finance/readiness` returns `readinessStatus = "ready"` for test company
- [ ] Finance dashboard tabs load without error (Resultados, Balance, EBITDA, Cuentas)
- [ ] Validation tab shows `healthScore >= 80`
- [ ] `npm run build` passes (0 TypeScript errors)
- [ ] `dotnet build` passes (0 errors, 0 warnings)

### TST (staging environment)

- [ ] AppDbContext migrations applied to TST SQL Server
- [ ] StagingDbContext migrations applied to TST Supabase project
- [ ] TST company record has `AnalyticsCompanyId` set
- [ ] Classification rules configured for TST company
- [ ] Full extraction completed: OACT + OJDT (at least 12 months — JDT1 lines are embedded in OJDT)
- [ ] MART refresh completed: `mart.refresh_accounting_all('tst-company-id')`
- [ ] Readiness endpoint: `blocked_reasons: []`
- [ ] Validations endpoint: `healthScore >= 80`, `criticalIssues = 0`
- [ ] Balance cuadra: `isBalanced = true`
- [ ] Revenue is positive in income statement
- [ ] EBITDA trend shows expected direction
- [ ] Filter config wiring tested (label overrides, advanced filters)
- [ ] Demo script walkthrough completed with internal team

### PRD (production)

**Pre-deployment:**
- [ ] TST checklist 100% complete
- [ ] Client accountant has reviewed and approved classification rules
- [ ] Client CFO has reviewed income statement + EBITDA (at least 3 months)
- [ ] Balance cuadra confirmed by client accounting team
- [ ] Merge freeze confirmed (no pending migrations)
- [ ] Smoke test SQL executed: `docs/sql/accounting-deployment-smoke-test.sql`
- [ ] Operations runbook reviewed by on-call team

**Deployment:**
- [ ] AppDbContext migrations applied to PRD
- [ ] StagingDbContext migrations applied to PRD Supabase (MANUAL — not auto-run at startup)
- [ ] PRD company record has `AnalyticsCompanyId` set
- [ ] Classification rules exported from TST → imported in PRD
- [ ] Full extraction from PRD SAP: OACT + OJDT (JDT1 lines embedded in OJDT)
- [ ] MART refresh: `mart.refresh_accounting_all('prd-company-id')`
- [ ] Readiness endpoint passes in PRD
- [ ] Validations endpoint: `healthScore >= 80`
- [ ] Client demo completed in PRD

**Post-deployment:**
- [ ] Daily extractor schedule confirmed (OJDT — includes JDT1 lines automatically)
- [ ] Weekly MART refresh schedule confirmed
- [ ] Alerting configured for extractor failures
- [ ] Client training completed (how to interpret Validaciones tab)

---

## Smoke Test Queries (PRD)

Run `docs/sql/accounting-deployment-smoke-test.sql` against the PRD Supabase instance.
Replace `YOUR_COMPANY_ID` with the client's `AnalyticsCompanyId`.

Critical checks:
1. All 12 accounting tables have rows > 0
2. No functions missing
3. `unclassified_accounts = 0` (or < 5 with justification)
4. Balance imbalance = 0 or < 0.01
5. Revenue positive in last 3 periods

---

## Known Risks

| Risk | Impact | Mitigation |
|---|---|---|
| PgBouncer transaction pooler (port 6543) | Migrations fail if run via API startup | Always run migrations manually with `dotnet ef database update` |
| OACT sign convention varies by SAP version | Revenue appears negative | Verify `mart.refresh_income_statement` applies correct sign for client |
| Unclassified accounts grow over time | MART data degrades | Monthly review with `mart.gl_accounts WHERE statement_line = 'unclassified'` |
| Balance imbalance after re-classification | CFO visibility issue | Always re-run full MART refresh after changing classification rules |
| Watermark drift | Duplicate or missing journal lines | Monitor `ctl.ingest_checkpoint` after each extraction |
| Large JDT1 table | First extraction slow (hours) | Run first extraction off-hours; subsequent runs are incremental |

---

## Classification Rule Export/Import (TST → PRD)

```sql
-- Export from TST (run in TST Supabase)
SELECT company_id, account_code, format_code, statement_line
FROM cfg.account_classification_rules
WHERE company_id = 'tst-company-id';

-- Import to PRD (change company_id to PRD value)
INSERT INTO cfg.account_classification_rules
    (company_id, account_code, format_code, statement_line, created_at, updated_at)
VALUES
    ('prd-company-id', 'account_code_here', NULL, 'revenue', NOW(), NOW()),
    -- ... more rows
ON CONFLICT (company_id, account_code) DO UPDATE
    SET statement_line = EXCLUDED.statement_line, updated_at = NOW();
```

---

## Support Reference

- Operations Runbook: `docs/native-bi-accounting-operations-runbook.md`
- Deployment Checklist: `docs/native-bi-accounting-deployment-checklist.md`
- Account Classification Guide: `docs/native-bi-account-classification.md`
- Finance Demo Script: `docs/commercial/native-bi-finance-demo-script.md`
- Smoke Test SQL: `docs/sql/accounting-deployment-smoke-test.sql`
