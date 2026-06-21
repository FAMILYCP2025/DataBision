# Native BI Finance Demo v2 — Pre-Demo Checklist

**Date:** 2026-06-20  
**Version:** 2.0 (post Sprint 19 productization)

## T-30 min — Environment

- [ ] SAP B1 CLTSTKSDEPOR accessible (Service Layer `https://161.153.200.53:50000`)
- [ ] Supabase connection confirmed (run readiness check below)
- [ ] API started: `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5103 dotnet run --project src/DataBision.Api --no-launch-profile`
- [ ] Frontend started: `npm run dev` from `databision-frontend/`
- [ ] Browser open to portal URL with `?tenant=ksdepor`

## T-10 min — Data Validation

Run all 6 API checks:

```bash
# All should return HTTP 200
curl -s -o /dev/null -w "%{http_code}" "http://localhost:5103/api/client/bi/finance/readiness?companyId=ksdepor"
curl -s -o /dev/null -w "%{http_code}" "http://localhost:5103/api/client/bi/finance/validations?companyId=ksdepor"
curl -s -o /dev/null -w "%{http_code}" "http://localhost:5103/api/client/bi/finance/income-statement?companyId=ksdepor"
curl -s -o /dev/null -w "%{http_code}" "http://localhost:5103/api/client/bi/finance/balance-sheet?companyId=ksdepor"
curl -s -o /dev/null -w "%{http_code}" "http://localhost:5103/api/client/bi/finance/ebitda?companyId=ksdepor"
curl -s -o /dev/null -w "%{http_code}" "http://localhost:5103/api/client/bi/finance/chart-of-accounts?companyId=ksdepor"
```

- [ ] All 6 return 200
- [ ] readiness.readinessStatus = "ready"
- [ ] validations.healthScore = 100
- [ ] income-statement has at least 1 period with data
- [ ] ebitda.cogs > 0 (should be 128,474.80)
- [ ] chart-of-accounts has 55 accounts

## T-5 min — Known Limitations to Address Proactively

Be ready to explain:

| What prospect will see | What to say |
|---|---|
| Revenue = 201.19 (very small) | "Base de datos de prueba TST con actividad limitada. En producción verían sus cifras reales." |
| COGS >> Revenue | "Mismo motivo — TST tiene muchas compras pero pocas ventas registradas." |
| Imbalance en Balance Sheet | "En TST no hay asientos patrimoniales. En producción, balance cuadra." |
| unclassified en income_statement | "Cuentas auxiliares sin clasificar — mínimo impacto. En producción con PCGE completo, desaparecen." |

## T-0 — Demo Flow

1. ✅ Readiness → mostrar readinessStatus="ready", 84 reglas
2. ✅ Validations → mostrar healthScore=100
3. ✅ Income Statement → destacar PCGE automático, cogs separado correctamente
4. ✅ EBITDA → destacar gross_profit, net_income, financial_result
5. ✅ Balance Sheet → mostrar activos/pasivos
6. ✅ Chart of Accounts → destacar clasificación automática, 0 sin clasificar

## Post-Demo — Refresh Data (if asked)

If client wants to see data update live:
1. `SELECT * FROM mart.refresh_accounting_all('company-dev-001');` — runs in Supabase
2. Reload any finance page — data updates immediately from MART

## Emergency Fallback

If API is down:
- Show screenshots in `docs/native-bi-sprint-19-finance-productization-summary.md`
- Income statement numbers: revenue=201.19, cogs=128,474.80, net_income=-130,921.40
- healthScore=100, 6/6 endpoints 200
