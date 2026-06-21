# Native BI Finance Dashboard — Demo Script v2

**Date:** 2026-06-20  
**Version:** 2.0 (post Sprint 19 productization)  
**Duration:** ~15 minutes  
**Audience:** SAP B1 customer / CFO / Finance Director

## Pre-Demo Setup

1. API running: `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5103 dotnet run --project src/DataBision.Api --no-launch-profile`
2. Verify readiness: `curl http://localhost:5103/api/client/bi/finance/readiness?companyId=ksdepor`
   - Should return `readinessStatus: "ready"`
3. Frontend: `npm run dev` from `databision-frontend/`
4. Open browser to `http://localhost:5174?tenant=ksdepor` (or configured dev URL)

## Act 1 — Problema del cliente (2 min)

*"Hoy, para obtener estos números necesitas: exportar desde SAP, abrir Excel, consolidar manualmente, y recién entonces tienes el P&L del mes. ¿Cuánto demora ese proceso?"*

→ Pausa para respuesta del cliente.

*"DataBision conecta directamente a SAP B1 vía Service Layer. No Excel, no exportación manual. Los datos siempre están actualizados."*

## Act 2 — Dashboard de Preparación (2 min)

Navegar a: Finance → Readiness

**Mostrar:**
```
Estado: LISTO
OACT: 20 cuentas | OJDT: 50 asientos | JDT1: 122 líneas
Cuentas MART: 55 | Reglas: 84 | Sin clasificar: 0
```

*"Esto confirma que los datos de SAP están sincronizados y el MART financiero está listo. 84 reglas de clasificación PCGE Peru aplicadas automáticamente."*

## Act 3 — Estado de Salud (1 min)

Navegar a: Finance → Validations

**Mostrar:**
```
Health Score: 100/100
Issues críticos: 0 | Warnings: 0
```

*"Score 100. El motor detecta automáticamente asientos sin contrapartida, cuentas sin clasificar, imbalances. En este caso, todo limpio."*

## Act 4 — Estado de Resultados (4 min)

Navegar a: Finance → Income Statement

**Mostrar (Jan 2026):**
- Revenue: 201.19
- COGS: 128,474.80
- Gross Profit: -128,273.61
- OPEX: 2,650.00
- Financial Income: 2.21
- Net Income: -130,921.40

*"El Estado de Resultados viene clasificado automáticamente por PCGE Peru. Ingresos, costo de ventas, gastos operativos — todo separado sin intervención manual. Las cuentas 60xxx (Compras) y 69xxx (Costo de ventas) están correctamente agrupadas como COGS."*

*"En este ambiente de prueba, el revenue es bajo porque la base de datos TST tiene actividad limitada. En un ambiente productivo con datos reales, verías las cifras del negocio real."*

## Act 5 — EBITDA (3 min)

Navegar a: Finance → EBITDA

**Mostrar (Jan 2026):**
- Revenue: 201.19
- COGS: 128,474.80
- Gross Profit: -128,273.61
- EBITDA: -130,923.61
- Financial Result: 2.21
- Net Income: -130,921.40

*"El EBITDA se calcula automáticamente desde el Estado de Resultados. Aquí ves el margen EBITDA, el resultado financiero neto (ingresos vs gastos financieros), y el ingreso neto final."*

*"Hoy esto requeriría un analista financiero varias horas. Con DataBision, se actualiza en segundos."*

## Act 6 — Plan de Cuentas (2 min)

Navegar a: Finance → Chart of Accounts

**Mostrar:**
- 55 cuentas clasificadas
- Filtro por statement_line: revenue (1), cogs (12), current_assets (20)
- Cuentas JDT1 identificadas con prefijo

*"DataBision clasifica automáticamente cada cuenta contable. Si una cuenta no está en el catálogo OACT de SAP pero aparece en asientos, igual la detectamos y clasificamos por PCGE."*

## Act 7 — Balance General (1 min)

Navegar a: Finance → Balance Sheet

**Mostrar:**
- Total Activos: 124,289.50
- Total Pasivos: 81,055.05

*"El Balance General se construye desde los mismos asientos JDT1 de SAP. En un ambiente de producción con asientos patrimoniales completos, verías el balance cuadrado."*

## Closing

*"Todo lo que viste viene directamente de SAP B1 — cero intervención manual, cero Excel, cero riesgo de error humano. El pipeline se ejecuta automáticamente: extracción, clasificación PCGE, y visualización."*

*"¿Qué áreas del análisis financiero son hoy más dolorosas para tu equipo?"*
