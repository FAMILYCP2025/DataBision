# Native BI Finance — Release Notes Sprint 15

Sprint 15 — 2026-06-18

---

## Sprint 15A — Corrección de nombres de funciones ETL

**Problema:** Los documentos de operaciones (runbook y checklist) referenciaban funciones PostgreSQL inexistentes (`stg.load_oact`, `mart.build_income_statement`, etc.) y listaban `--object JDT1` como un comando del extractor separado que no existe.

**Cambios:**

| Antes (incorrecto) | Ahora (correcto) |
|---|---|
| `stg.load_oact(company_id)` | `stg.refresh_gl_accounts(p_company_id)` |
| `stg.load_ojdt(company_id)` | `stg.refresh_journal_entries(p_company_id)` |
| `stg.load_jdt1(company_id)` | *(embebido en `stg.refresh_journal_entries`)* |
| `mart.build_gl_accounts(company_id)` | `mart.refresh_gl_accounts(p_company_id)` |
| `mart.build_income_statement(company_id)` | `mart.refresh_income_statement(p_company_id)` |
| `mart.build_balance_sheet(company_id)` | `mart.refresh_balance_sheet(p_company_id)` |
| `mart.build_ebitda(company_id)` | `mart.refresh_ebitda(p_company_id)` |
| `--object JDT1` (CLI) | *(no existe — usar `--object OJDT`)* |

**Archivos actualizados:**
- `docs/native-bi-accounting-operations-runbook.md`
- `docs/native-bi-accounting-production-checklist.md`
- `docs/commercial/native-bi-finance-demo-script.md`

**Archivo nuevo:**
- `docs/native-bi-accounting-function-map.md` — mapa completo de funciones ETL, tablas RAW/STG/MART y sus relaciones

---

## Sprint 15B — Protocolo de validación RAW

**Nuevo:** `docs/native-bi-accounting-raw-validation.md`

Documento con el protocolo completo de validación de la capa RAW post-extracción:
- Estado de configuración del extractor para KSDEPOR
- Procedimiento de 3 etapas: dry-run → read-only → send
- Queries de validación de forma (AccountType, FormatCode, Debit/Credit)
- Checklist completo con estado actual

**Hallazgo:** La configuración del extractor (`appsettings.Development.json`) ya apunta correctamente a `company-dev-001` como `CompanyId`, coincidiendo con el `AnalyticsCompanyId` de KSDEPOR. No se requieren cambios de configuración.

---

## Sprint 15C — Protocolo de validación STG/MART

**Nuevo:** `docs/native-bi-accounting-mart-validation-results.md`

Documento con el protocolo completo de validación post-MART refresh:
- Verificación de todos los 7 pasos de `mart.refresh_accounting_all`
- Queries de validación por capa (STG, MART GL, Account Balances, Income Statement, Balance Sheet, EBITDA)
- Pruebas de ecuación contable (Activos = Pasivos + Patrimonio)
- Validación de sign convention (revenue positivo)
- Validación de endpoints API y tabs del frontend

---

## Sprint 15D — Clasificación contable KSDEPOR

**Hallazgo crítico:** `AnalyticsCompanyId` para KSDEPOR y demo es `"company-dev-001"`, configurado en `NativeBi:CompanySlugMap` del archivo `appsettings.json`.

**Nuevos archivos:**
- `sql/native-bi/accounting-classification-demo-ksdepor.sql` — reglas de clasificación demo por format-code prefix para `company-dev-001`
- `docs/native-bi-ksdepor-account-classification-plan.md` — plan completo de clasificación con checklist de validación con contador

**Reglas incluidas (template — requieren validación con contador):**
- Activos corrientes: format_code prefixes 11–14
- Activos no corrientes: 15–18
- Pasivos corrientes: 21–25
- Pasivos no corrientes: 26–28
- Patrimonio: 31–34
- Revenue: 4x
- COGS: 5x
- OPEX + Depreciación: 6x (67 → `depreciation`)
- Otros ingresos: 7x
- Financiero: 8x
- Impuesto: 9x

---

## Sprint 15E — Protocolo de validación financiera

**Nuevo:** `docs/native-bi-finance-real-data-validation.md`

Protocolo completo de validación para los 6 endpoints de finanzas:
- Sign convention documentada por statement_line
- Fórmulas EBITDA y P&L verificadas
- Balance equation tests
- Validación de 6 tabs del frontend Finance dashboard
- Conocida limitación: balance sheet es flow-based, no acumulativo

---

## Sprint 15F — Documentación comercial

**Nuevos archivos:**
- `docs/commercial/native-bi-finance-one-pager.md` — one-pager para CFO/Gerencia
- `docs/commercial/native-bi-finance-demo-checklist.md` — checklist pre-demo con alertas y guión de 5 minutos
- `docs/commercial/native-bi-finance-client-proposal.md` — propuesta comercial con plan de implementación 4 fases
- `docs/commercial/native-bi-finance-demo-script.md` — actualizado: corrección de pre-requisitos técnicos

---

## Resumen de archivos del Sprint 15

### Archivos nuevos
| Archivo | Tipo | Propósito |
|---|---|---|
| `docs/native-bi-accounting-function-map.md` | Técnico | Mapa de funciones ETL reales |
| `docs/native-bi-accounting-raw-validation.md` | Técnico | Protocolo validación RAW |
| `docs/native-bi-accounting-mart-validation-results.md` | Técnico | Protocolo validación MART |
| `sql/native-bi/accounting-classification-demo-ksdepor.sql` | SQL | Reglas clasificación KSDEPOR |
| `docs/native-bi-ksdepor-account-classification-plan.md` | Técnico | Plan clasificación KSDEPOR |
| `docs/native-bi-finance-real-data-validation.md` | Técnico | Protocolo validación endpoints |
| `docs/commercial/native-bi-finance-one-pager.md` | Comercial | One-pager CFO |
| `docs/commercial/native-bi-finance-demo-checklist.md` | Comercial | Checklist pre-demo |
| `docs/commercial/native-bi-finance-client-proposal.md` | Comercial | Propuesta comercial |

### Archivos actualizados
| Archivo | Cambio |
|---|---|
| `docs/native-bi-accounting-operations-runbook.md` | Corregidos 5 errores: función names, JDT1 como separado |
| `docs/native-bi-accounting-production-checklist.md` | Corregidos 4 errores: JDT1 como separado, mart.build_income_statement |
| `docs/commercial/native-bi-finance-demo-script.md` | Corregido pre-requisito JDT1 + función refresh |

---

## Estado de ejecución (pendiente confirmación)

| Tarea | Estado | Bloqueador |
|---|---|---|
| Extracción OACT de CLTSTKSDEPOR | ⏳ Pendiente | Ejecutar manualmente |
| Extracción OJDT de CLTSTKSDEPOR | ⏳ Pendiente | Ejecutar manualmente |
| Aplicar `accounting-classification-demo-ksdepor.sql` | ⏳ Pendiente | Requiere OACT extraído primero |
| `mart.refresh_accounting_all('company-dev-001')` | ⏳ Pendiente | Requiere extracción completa |
| Validación endpoints API | ⏳ Pendiente | Requiere MART refresh |
| Demo con CFO KSDEPOR | ⏳ Pendiente | Requiere validación completa |
