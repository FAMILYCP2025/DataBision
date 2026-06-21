# Native BI Finance — Checklist Primer Piloto Real

**Sprint:** 23F (actualizado desde 22F)  
**Fecha:** 2026-06-21  
**Uso:** Operativo — completar en orden antes y durante el piloto

---

## FASE 0 — Pre-kickoff (interno, días -3 a -1)

### Infraestructura DataBision
- [ ] API de DataBision desplegada y accesible (URL confirmada)
- [ ] `Jwt:PublicKey` configurado en variables de entorno del servidor (NO en appsettings.json)
- [ ] `StagingConnection` (Supabase) configurado en variables de entorno
- [ ] `Ingest:ApiKeys` configurado (API key del cliente, mapea a company_id)
- [ ] Supabase: `company_id` del cliente creado en todas las tablas necesarias
- [ ] `cfg.account_classification_rules` poblado (o copiado desde empresa demo)
- [ ] MART functions de finance presentes (`refresh_accounting_all`)

### Perfil de conexión SAP (Sprint 23 — flujo nuevo)
- [ ] `AnalyticsCompanyId` configurado en Admin → Empresa (requerido para resolución de perfil)
- [ ] Variable de entorno `SAP_PASSWORD_{SLUG}` configurada en el **servidor de la API**
- [ ] NativeBiConnectionProfile creado desde Admin UI → pestaña Native BI → **+ Nuevo perfil**
  - `SecretRef` = `env:SAP_PASSWORD_{SLUG}` (apunta a la variable en el servidor de la API)
- [ ] Test de conexión desde Admin UI → botón **Test** → resultado ✓ OK
- [ ] Resultado del test guardado (captura de pantalla)

### Extractor
- [ ] Extractor desplegado en servidor con acceso de red a SAP Service Layer del cliente
- [ ] `DataBisionApi:BaseUrl` y `DataBisionApi:ApiKey` configurados en el servidor del extractor
- [ ] `Extractor:CompanyId` = analytics company ID de la empresa (ej. `company-client-001`)
- [ ] `--dry-run --profile produccion` ejecutado y retorna "Profile resolved: ... DB=..."
- [ ] `--validate` ejecutado con las credenciales resueltas (login SAP exitoso)
- [ ] `--validate-staging` ejecutado y Supabase accesible
- [ ] **Rollback documentado:** si el resolve falla, usar appsettings con credenciales directas

### Acceso SAP
- [ ] Usuario SAP dedicado para DataBision creado (solo lectura: OACT, OJDT, ChartOfAccounts, JournalEntries)
- [ ] Año fiscal activo confirmado
- [ ] CompanyDB confirmado (exacto, case-sensitive)
- [ ] URL de Service Layer confirmada (formato: `https://host:50000/b1s/v1`)
- [ ] Certificado SSL: válido o `IgnoreSslErrors=true` documentado y aprobado por cliente

---

## FASE 1 — Kickoff (Día 0)

### Reunión inicial (ver agenda en `native-bi-finance-pilot-kickoff-agenda.md`)
- [ ] Presentación de DataBision Native BI Finance
- [ ] Confirmación de alcance y cronograma
- [ ] Presentación del usuario SAP dedicado al cliente
- [ ] Firma de acuerdo de confidencialidad (si aplica)

---

## FASE 2 — Primera extracción (Día 1)

### OACT (Chart of Accounts)
- [ ] `dotnet DataBision.Extractor.exe --profile produccion --object OACT --send`
- [ ] Verificar en Supabase: `SELECT COUNT(*) FROM raw.oact WHERE company_id = '...'`
- [ ] Verificar en MART: `SELECT COUNT(*) FROM mart.gl_accounts WHERE company_id = '...'`
- [ ] Cuentas sin clasificar: `SELECT * FROM mart.gl_accounts WHERE classification = 'unclassified'`
- [ ] Reglas de clasificación ajustadas para el plan de cuentas del cliente

### OJDT (Journal Entries)
- [ ] `dotnet DataBision.Extractor.exe --object OJDT --send`
- [ ] Verificar filas extraídas en log (esperado: depende del historial del cliente)
- [ ] Verificar en Supabase: `SELECT COUNT(*) FROM raw.ojdt WHERE company_id = '...'`

### MART refresh
- [ ] `dotnet DataBision.Extractor.exe --transform-mart --company {company_id}`
- [ ] Verificar: `SELECT * FROM mart.income_statement_summary WHERE company_id = '...'`
- [ ] Verificar: `SELECT * FROM mart.balance_sheet_summary WHERE company_id = '...'`
- [ ] Verificar: `SELECT * FROM mart.ebitda_summary WHERE company_id = '...'`

### Dashboard
- [ ] `GET /api/client/bi/finance/readiness?companyId={slug}` → `overall_status = "ready"`
- [ ] `GET /api/client/bi/finance/refresh-status?companyId={slug}` → `overallStatus = "ok"`

---

## FASE 3 — Validación contable (Día 2)

### Con el contador o gerente financiero del cliente
- [ ] P&L del mes actual coincide con sistema contable del cliente (margen ≤ 5%)
- [ ] Balance General cuadra (Activos = Pasivos + Patrimonio)
- [ ] EBITDA razonable según expectativa del cliente
- [ ] Cuentas sin clasificar = 0 (o aprobadas como "unclassified")
- [ ] Moneda y signos correctos

### Incidencias detectadas
- [ ] (Registrar aquí cualquier discrepancia encontrada y su resolución)

---

## FASE 4 — Ajustes (Día 3)

- [ ] Ajustes de clasificación de cuentas aplicados si es necesario
- [ ] Re-ejecución de MART refresh tras ajustes
- [ ] Segunda validación con contador

---

## FASE 5 — Capacitación y entrega (Día 4)

- [ ] Demostración de dashboard en vivo (P&L, Balance, EBITDA)
- [ ] Explicación de cómo interpretar los reportes
- [ ] Demostración de widget de refresh-status
- [ ] Explicación del scheduler automático (si configurado)
- [ ] Entrega de manual de usuario (1 página)
- [ ] Acuerdo sobre frecuencia de actualización de datos

---

## FASE 6 — Cierre y go/no-go (Día 4-5)

### Go/No-Go
| Criterio | Estado |
|---|---|
| P&L validado por contador | ☐ OK / ☐ Pendiente |
| Balance cuadrado | ☐ OK / ☐ Pendiente |
| EBITDA validado | ☐ OK / ☐ Pendiente |
| 0 cuentas sin clasificar críticas | ☐ OK / ☐ Pendiente |
| Scheduler configurado y funcionando | ☐ OK / ☐ N/A |
| Cliente confirma aceptación | ☐ OK / ☐ Pendiente |

**Decisión final:** ☐ GO (continuar a suscripción mensual) / ☐ NO-GO (definir próximos pasos)

---

## FASE 7 — Post-entrega (Día 5+)

- [ ] Informe final entregado al cliente (ver template en `native-bi-finance-pilot-closing-report-template.md`)
- [ ] Propuesta de suscripción mensual enviada
- [ ] Acceso SuperAdmin al cliente desactivado si solo era piloto
- [ ] Memoria interna de lecciones aprendidas registrada

---

## Contactos clave del piloto

| Rol | Nombre | Contacto |
|---|---|---|
| Consultor DataBision | | |
| Gerente financiero cliente | | |
| Contador cliente | | |
| Responsable TI cliente | | |
| Responsable SAP cliente | | |
