# Native BI Finance — Informe Go/No-Go Piloto TST (24F)

**Sprint:** 24F  
**Fecha generación:** 2026-06-21  
**Empresa:** ksdepor / CLTSTKSDEPOR  
**Tipo:** Piloto interno TST (validación técnica, no demo comercial)

---

## 1. Resumen ejecutivo

_(Completar al finalizar la ejecución)_

Este informe documenta el primer piloto real controlado de DataBision Native BI Finance sobre la base de datos TST de ksdepor (CLTSTKSDEPOR). El objetivo es validar el flujo completo desde la configuración del perfil de conexión hasta la visualización de dashboards financieros, sin tocar datos de producción.

---

## 2. Alcance probado

| Componente | Incluido | Resultado |
|---|---|---|
| Connection Profile en Admin UI | ✅ | ___ |
| Test connection (API → SAP SL) | ✅ | ___ |
| Extractor `--profile tst` | ✅ | ___ |
| Dry-run con resolución de perfil | ✅ | ___ |
| Extracción OACT (Chart of Accounts) | ✅ | ___ |
| Extracción OJDT (Journal Entry headers) | ✅ | ___ |
| Extracción JDT1 (Journal Entry lines vía individual GET) | ✅ | ___ |
| MART refresh (`refresh_accounting_all`) | ✅ | ___ |
| Endpoint /readiness | ✅ | ___ |
| Endpoint /income-statement | ✅ | ___ |
| Endpoint /balance-sheet | ✅ | ___ |
| Endpoint /ebitda | ✅ | ___ |
| Endpoint /chart-of-accounts | ✅ | ___ |
| Endpoint /refresh-status | ✅ | ___ |

---

## 3. Datos SAP usados

| Dato | Valor |
|---|---|
| SAP B1 DB | `CLTSTKSDEPOR` (TST — no productivo) |
| Service Layer URL | `https://[SL-HOST]:50000/b1s/v1` (enmascarado) |
| Usuario SAP | `dgoto` |
| Certificado SSL | IgnoreSSL=true (autofirmado TST) |
| Plan de cuentas | PCGE Perú |

---

## 4. Connection Profile

| Campo | Valor |
|---|---|
| ProfileName | `tst` |
| SecretRef | `env:SAP_PASSWORD_KSDEPOR` (no plaintext) |
| IsActive | true |
| FetchConcurrency | 3 |
| TimeoutSeconds | 60 |
| AnalyticsCompanyId empresa | `company-dev-001` |
| API key | `dev-key-001` (dev) |

---

## 5. Test Connection

| Métrica | Valor |
|---|---|
| Fecha/hora test | ___ |
| `success` | ___ |
| `latencyMs` | ___ |
| `loginOk` | ___ |
| `chartOfAccountsOk` | ___ |
| `journalEntriesOk` | ___ |
| `message` | ___ |

---

## 6. Dry-Run

| Check | Resultado |
|---|---|
| Profile resolved | ☐ OK / ☐ FAIL |
| DB mostrado en log | ☐ OK (`CLTSTKSDEPOR`) |
| Password NO en logs | ☐ OK / ☐ FAIL |
| Configuration OK | ☐ OK / ☐ FAIL |
| Exit code | ___ |

---

## 7. Extracción OACT / OJDT / JDT1

| Objeto | Rows SAP | Inserted Supabase | Exit Code |
|---|---|---|---|
| OACT (Chart of Accounts) | | | |
| OJDT (Journal Entries headers) | | | |
| JDT1 (Journal Entry lines) | | | |

| Métrica OJDT | Valor |
|---|---|
| Estrategia líneas | `individual GET` / `$expand` / `top-level` |
| Propiedad líneas | `JournalEntryLines` / `Lines` |
| Failed GETs (17C) | |
| ReferenceDate desde–hasta | |

```sql
-- Evidencia (conteos Supabase, ejecutar y pegar resultado):
SELECT 
  'raw.sap_oact'  AS t, COUNT(*) FROM raw.sap_oact  WHERE company_id='company-dev-001'
UNION ALL SELECT 
  'raw.sap_ojdt'  AS t, COUNT(*) FROM raw.sap_ojdt  WHERE company_id='company-dev-001'
UNION ALL SELECT 
  'raw.sap_jdt1'  AS t, COUNT(*) FROM raw.sap_jdt1  WHERE company_id='company-dev-001';
```

_Resultado:_
```
(pegar aquí)
```

---

## 8. MART Financiero

| Tabla MART | Filas | Estado |
|---|---|---|
| mart.gl_accounts | | ☐ > 0 |
| mart.account_balances | | ☐ > 0 |
| mart.income_statement_summary | | ☐ > 0 |
| mart.balance_sheet_summary | | ☐ > 0 |
| mart.ebitda_summary | | ☐ > 0 |

| Métrica contable | Valor |
|---|---|
| Clasificación unclassified | ___ cuentas |
| Balance cuadra (tolerancia 1%) | ☐ Sí / ☐ No (dif: ___) |
| healthScore | ___ |
| Períodos con datos | ___ |

---

## 9. Dashboard Endpoints

| Endpoint | HTTP | Estado |
|---|---|---|
| `GET /api/client/bi/finance/readiness?companyId=ksdepor` | ___ | ☐ OK |
| `GET /api/client/bi/finance/validations?companyId=ksdepor` | ___ | ☐ OK |
| `GET /api/client/bi/finance/income-statement?companyId=ksdepor` | ___ | ☐ OK |
| `GET /api/client/bi/finance/balance-sheet?companyId=ksdepor` | ___ | ☐ OK |
| `GET /api/client/bi/finance/ebitda?companyId=ksdepor` | ___ | ☐ OK |
| `GET /api/client/bi/finance/chart-of-accounts?companyId=ksdepor` | ___ | ☐ OK |
| `GET /api/client/bi/finance/refresh-status?companyId=ksdepor` | ___ | ☐ OK |

**Score:** ___/7 HTTP 200

---

## 10. Riesgos

| Riesgo | Severidad | Estado |
|---|---|---|
| ENV var `SAP_PASSWORD_KSDEPOR` no persistida entre reinicios de API | Alta | Documentado — configurar como variable del sistema |
| `IgnoreSslErrors=true` en TST | Media | Aceptable para TST — requiere SSL válido en prod |
| JDT1 vacío si SL no expone `JournalEntryLines` | Media | Documentado en extraction results |
| Cuentas unclassified afectan P&L | Media | Requiere ajuste de `cfg.account_classification_rules` |
| Balance no cuadra en TST | Baja | Estructural en TST — documentar vs. prod |
| azure-kv:// no implementado | Baja | Solo `env:` soportado — suficiente para piloto |

---

## 11. Issues Abiertos

| # | Issue | Prioridad | Sprint |
|---|---|---|---|
| 1 | | | |
| 2 | | | |

---

## 12. Decisión Go/No-Go

### Criterios GO (todos deben cumplirse)

| Criterio | Cumplido |
|---|---|
| Test connection exitoso (`success=true`) | ☐ |
| Dry-run resuelve profile correctamente | ☐ |
| `raw.sap_oact > 0` | ☐ |
| `raw.sap_ojdt > 0` | ☐ |
| `mart.income_statement_summary > 0` | ☐ |
| `mart.balance_sheet_summary > 0` | ☐ |
| `mart.ebitda_summary > 0` | ☐ |
| Endpoints 5/7 HTTP 200 (mínimo) | ☐ |
| Sin secretos en logs (password, B1SESSION, JWT) | ☐ |

### Criterios NO-GO (cualquiera bloquea)

| Criterio | Presente |
|---|---|
| Login SAP falla | ☐ |
| ApiKey company mismatch | ☐ |
| OACT = 0 (sin plan de cuentas) | ☐ |
| OJDT = 0 (sin asientos) | ☐ |
| `mart.refresh_all` falla con error | ☐ |
| Secretos detectados en logs | ☐ |

---

## 13. Decisión Final

**☐ GO** — El flujo completo funciona en TST. Listo para:
- Preparar demo con cliente real
- Configurar perfil en ambiente de producción del cliente
- Iniciar Sprint 25 (piloto comercial)

**☐ NO-GO** — Bloqueado por:
- ___ (describir causa)
- Próximos pasos: ___

---

## 14. Próximos Pasos Comerciales

### Si GO

1. **Sprint 25A** — Onboarding del primer cliente real:
   - Crear empresa en AppDB producción
   - Configurar `env:SAP_PASSWORD_{SLUG}` en servidor API producción
   - Crear perfil desde Admin UI con datos SAP productivos del cliente
   - Test connection en producción
   - Primera extracción con `--profile produccion --object OACT --send`

2. **Sprint 25B** — Validación financiera con contador del cliente:
   - Comparar P&L vs. reportes del cliente
   - Ajustar `cfg.account_classification_rules`
   - Balance cuadra dentro del 5%

3. **Sprint 25C** — Go-live y suscripción:
   - Configurar scheduler (Windows Task Scheduler o cron)
   - Capacitación al cliente (2h)
   - Firma de contrato de suscripción

### Si NO-GO

- Revisar issues listados en sección 11
- Re-ejecutar Sprint 24 con fixes aplicados

---

## Metadatos

| Campo | Valor |
|---|---|
| Sprint | 24F |
| Fecha generación | 2026-06-21 |
| Preparado por | DataBision Engineering |
| Revisión | Pendiente |
