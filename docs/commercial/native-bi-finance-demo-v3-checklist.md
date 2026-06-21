# Native BI Finance Demo v3 — Pre-Demo Checklist

**Date:** 2026-06-20  
**Version:** 3.0 (post Sprint 20)

---

## T-30 min — Entorno

- [ ] SAP B1 CLTSTKSDEPOR accesible (Service Layer `https://161.153.200.53:50000`)
- [ ] Supabase connection OK
- [ ] API iniciada:
  ```bash
  ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5103 \
    dotnet run --project src/DataBision.Api --no-launch-profile
  ```
- [ ] Frontend iniciado: `npm run dev` desde `databision-frontend/`
- [ ] Browser abierto al portal con `?tenant=ksdepor`

---

## T-10 min — Validación datos

```bash
# Todos deben retornar HTTP 200
curl -s -o /dev/null -w "%{http_code}" "http://localhost:5103/api/client/bi/finance/readiness?companyId=ksdepor"
curl -s -o /dev/null -w "%{http_code}" "http://localhost:5103/api/client/bi/finance/validations?companyId=ksdepor"
curl -s -o /dev/null -w "%{http_code}" "http://localhost:5103/api/client/bi/finance/income-statement?companyId=ksdepor"
curl -s -o /dev/null -w "%{http_code}" "http://localhost:5103/api/client/bi/finance/balance-sheet?companyId=ksdepor"
curl -s -o /dev/null -w "%{http_code}" "http://localhost:5103/api/client/bi/finance/ebitda?companyId=ksdepor"
curl -s -o /dev/null -w "%{http_code}" "http://localhost:5103/api/client/bi/finance/chart-of-accounts?companyId=ksdepor"
```

**Checks:**
- [ ] 6/6 retornan 200
- [ ] readiness.readinessStatus = "ready"
- [ ] validations.healthScore = 100
- [ ] income-statement tiene período Enero 2026 con revenue=201.19
- [ ] income-statement **NO tiene filas `unclassified`** (fix Sprint 20A ✅)
- [ ] balance-sheet **NO tiene categoría `unclassified`** (fix Sprint 20B ✅)
- [ ] ebitda Jan: cogs=128,474.80, net_income=-130,921.40
- [ ] chart-of-accounts: 55 cuentas

---

## T-5 min — Preparar talking points

| Limitación TST | Qué decir proactivamente |
|---|---|
| Revenue muy bajo (201.19) | "TST con actividad limitada. En producción: cifras reales del cliente." |
| COGS >> Revenue | "TST tiene muchas compras de prueba pero pocas ventas registradas." |
| Balance no cuadra | "TST no tiene asientos patrimoniales. En producción el balance cuadra." |
| Feb muestra cero IS | "Febrero solo tiene movimientos de balance (activos/pasivos sin P&L)." |
| Depreciation = 0 | "No hay asientos de depreciación en TST. En producción se clasifican en account_classification_rules." |

---

## T-0 — Flujo demo

1. **Tab Validaciones** → readiness="ready", healthScore=100
2. **Tab Estado de Resultados** → mostrar P&L, destacar PCGE automático, **exportar CSV**
3. **Tab EBITDA** → mostrar gross_profit, net_income, financial, **exportar CSV**
4. **Tab Balance General** → mostrar activos/pasivos, explicar imbalance, **exportar CSV**
5. **Tab Plan de Cuentas** → 55 cuentas, 0 sin clasificar, **exportar CSV**
6. **Admin panel** → mostrar AccountClassification con 84 reglas, sugerencias OACT

---

## Post-Demo — Actualizar datos en vivo (si el cliente lo pide)

```sql
-- En Supabase SQL Editor
SELECT * FROM mart.refresh_accounting_all('company-dev-001');
-- 8 filas, todas 'OK' → recargar el dashboard
```

---

## Fallback de emergencia

Si la API está caída:
- Mostrar screenshots en `docs/native-bi-sprint-20-finance-readiness-summary.md`
- Números clave: revenue=201.19, cogs=128,474.80, healthScore=100, 6/6 endpoints 200
- Mostrar los docs de arquitectura técnica

---

## Sprint 20 improvements (vs v2)

| Feature | v2 | v3 |
|---|---|---|
| Unclassified en IS | ❌ Presente | ✅ Eliminado (DELETE+INSERT) |
| Unclassified en BS | ❌ Presente | ✅ Eliminado (DELETE+INSERT) |
| Export CSV | ❌ No disponible | ✅ P&L, Balance, EBITDA, Cuentas |
| OJDT concurrency | ❌ Sequential | ✅ SemaphoreSlim (default 3) |
| Admin clasificación | ✅ Ya existía | ✅ Mantenido |
