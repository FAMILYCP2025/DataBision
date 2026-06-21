# DataBision Native BI Finance — Runbook Piloto Cliente Real (5 días)

**Versión:** 1.0  
**Sprint:** 21A  
**Fecha:** 2026-06-20  
**Audiencia:** Consultor DataBision (ejecutor)

---

## Resumen del piloto de 5 días

| Día | Foco | Salida |
|---|---|---|
| Día 0 | Preparación y acceso | Credenciales validadas, entorno listo |
| Día 1 | Extracción inicial y PCGE | Datos en pipeline, cuentas clasificadas |
| Día 2 | Validación financiera | Reportes revisados con contador |
| Día 3 | Ajustes y refinamiento | Clasificaciones ajustadas, dashboard limpio |
| Día 4 | Entrega y capacitación | Cliente opera el dashboard solo |
| Día 5 | Soporte post-entrega | Preguntas resueltas, scheduler configurado |

---

## Día 0 — Preparación (antes de iniciar con el cliente)

### D0-1 Reunión kickoff de información (30 min con TI del cliente)

Solicitar y confirmar:
- [ ] URL SAP Service Layer + puerto
- [ ] Base de datos de producción (`CompanyDB`)
- [ ] Usuario SAP con permisos de lectura (crear usuario dedicado si es posible)
- [ ] Ventana de mantenimiento disponible (para primera extracción si el cliente lo prefiere fuera de horario)

### D0-2 Validar conectividad

```powershell
# Desde servidor DataBision Extractor
curl -k "https://[SAP_HOST]:50000/b1s/v1/$metadata" -o /dev/null -w "%{http_code}"
# Esperado: 200
```

### D0-3 Configurar extractor

1. Crear directorio de trabajo para el cliente:
   ```
   C:\DataBision\Extractors\[SLUG]\
   ```

2. Copiar ejecutable extractor y `appsettings.json`

3. Crear `appsettings.Production.json` con datos del cliente (nunca `appsettings.json` base):
   ```json
   {
     "SapServiceLayer": {
       "BaseUrl": "https://[HOST]:50000/b1s/v1",
       "CompanyDB": "[COMPANYDB]",
       "UserName": "[USER]",
       "Password": "[PASSWORD]",
       "IgnoreSslCertificateErrors": false,
       "TimeoutSeconds": 90
     },
     "Extractor": {
       "TenantId": "[TENANT_ID]",
       "CompanyId": "[ANALYTICS_COMPANY_ID]",
       "Mode": "INCREMENTAL",
       "JournalEntryLineFetchConcurrency": 3
     }
   }
   ```

4. Validar config:
   ```
   dotnet DataBision.Extractor.exe --dry-run
   dotnet DataBision.Extractor.exe --validate
   ```

### D0-4 Crear empresa en SuperAdmin

- [ ] Empresa creada con slug correcto
- [ ] `AnalyticsCompanyId` configurado
- [ ] Módulo `native_bi_finance` habilitado
- [ ] Reglas PCGE base importadas (según país del cliente)

**Tiempo estimado Día 0:** 2-3 horas

---

## Día 1 — Extracción inicial y clasificación PCGE

### D1-1 Primera extracción OACT

```powershell
dotnet DataBision.Extractor.exe --object OACT --send
```

Verificar en logs:
```
OACT: [N] accounts in [Ms]ms
OACT sent: inserted=[N], updated=0, skipped=0
```

Validar en Supabase:
```sql
SELECT COUNT(*) FROM raw.sap_oact WHERE company_id = '[ANALYTICS_COMPANY_ID]';
-- Esperado: [N] cuentas del cliente
```

### D1-2 Primera extracción OJDT

```powershell
dotnet DataBision.Extractor.exe --object OJDT --send
```

Verificar en logs:
```
OJDT-17C: extracted [N] lines from [M]/[M] entries (0 failed) in [Ms]ms
```

Validar en Supabase:
```sql
SELECT COUNT(*) FROM raw.sap_ojdt WHERE company_id = '[ANALYTICS_COMPANY_ID]';
SELECT COUNT(*) FROM raw.sap_jdt1 WHERE company_id = '[ANALYTICS_COMPANY_ID]';
```

### D1-3 Ejecutar pipeline STG + MART

```powershell
dotnet DataBision.Extractor.exe --transform --include-mart --company [ANALYTICS_COMPANY_ID]
```

O desde Supabase SQL Editor:
```sql
SELECT * FROM mart.refresh_accounting_all('[ANALYTICS_COMPANY_ID]');
-- Esperado: 8 filas, todas con status 'OK'
```

### D1-4 Revisar clasificación inicial

```sql
SELECT statement_line, COUNT(*) as cuentas
FROM mart.gl_accounts
WHERE company_id = '[ANALYTICS_COMPANY_ID]'
GROUP BY statement_line ORDER BY cuentas DESC;
```

Si hay `unclassified` > 0:
- Ir a SuperAdmin → Empresa → Clasificación de cuentas
- Ejecutar "Sugerencias desde OACT"
- Clasificar manualmente las cuentas no reconocidas con el contador

**Criterio de éxito Día 1:** `mart.gl_accounts` con ≤ 5% unclassified

---

## Día 2 — Validación financiera con el contador

### D2-1 Revisión readiness

```
GET /api/client/bi/finance/readiness?companyId=[SLUG]
GET /api/client/bi/finance/validations?companyId=[SLUG]
```

Compartir con el cliente el `healthScore`. Si < 70: identificar y corregir issues antes de continuar.

### D2-2 Revisión Estado de Resultados

Con el contador del cliente:
- ¿Los ingresos (cuentas 70-79 PCGE) están clasificados correctamente?
- ¿El COGS (cuentas 60-69) coincide con lo que espera el contador?
- ¿Hay cuentas de OPEX mal clasificadas?
- Exportar CSV y comparar contra el ES manual del cliente del mismo período

### D2-3 Revisión Balance General

Con el contador:
- ¿Activos corrientes / no corrientes coherentes?
- ¿Pasivos corrientes / no corrientes coherentes?
- ¿La ecuación A = P + Patrimonio cuadra?
  - Si no cuadra → confirmar si hay asientos de cierre de ejercicio en SAP
  - Si la empresa no cierra el ejercicio contablemente en SAP, el balance no cuadrará (comportamiento esperado)

### D2-4 Documentar discrepancias

Para cada discrepancia encontrada:
- Cuenta SAP involucrada
- Clasificación actual en DataBision
- Clasificación correcta según contador
- Acción: ajustar regla en AdminPanel

**Criterio de éxito Día 2:** Lista de ajustes documentada y acordada con el contador

---

## Día 3 — Ajustes, refinamiento y re-validación

### D3-1 Aplicar ajustes de clasificación

Para cada cuenta con clasificación incorrecta identificada en Día 2:
```
SuperAdmin → Empresa [N] → Native BI → Clasificación de Cuentas → Editar regla
```

Después de cada ajuste, re-ejecutar pipeline:
```sql
SELECT * FROM mart.refresh_accounting_all('[ANALYTICS_COMPANY_ID]');
```

### D3-2 Segunda extracción OJDT (incremental)

```powershell
dotnet DataBision.Extractor.exe --object OJDT --send
```

Verificar que la extracción incremental captura solo los asientos nuevos (no re-extrae todo).

### D3-3 Validación final con el cliente

- [ ] Estado de Resultados: exportar CSV, comparar con reporte contable del cliente
- [ ] healthScore ≥ 80
- [ ] 0 cuentas unclassified (o documentar las pendientes)
- [ ] Balance: explicar cualquier diferencia documentada

**Criterio de éxito Día 3:** healthScore ≥ 80, exportación CSV aprobada por el contador

---

## Día 4 — Entrega y capacitación

### D4-1 Capacitación usuario final (45 min)

Guía de uso para el CFO / Controller:
1. Acceder al dashboard: `https://[SLUG].databision.app`
2. Tab Validaciones: cómo leer el healthScore
3. Tab Estado de Resultados: filtrar por período, exportar CSV
4. Tab EBITDA: tendencias, exportar
5. Tab Balance: fecha de snapshot
6. Tab Plan de Cuentas: buscar cuentas, exportar

### D4-2 Capacitación admin DataBision (30 min)

Para el responsable TI del cliente (si aplica):
1. Panel SuperAdmin: ver empresa y módulos
2. Clasificación de cuentas: cómo ajustar reglas
3. Entender que los datos se actualizan manualmente (Sprint 21C: futuro auto-scheduler)

### D4-3 Entrega documentación

Entregar al cliente:
- [ ] Checklist de onboarding completado (este documento)
- [ ] Acceso al dashboard
- [ ] Contacto de soporte DataBision
- [ ] SLA de actualización de datos (hasta Sprint 21C: manual; futuro: automático)

### D4-4 Configurar scheduler provisional

Hasta implementar scheduler automático (Sprint 21C):

**Windows (PowerShell manual semanal):**
```powershell
# Crear tarea en Task Scheduler (ejecutar semanalmente)
# Ref: docs/operations/native-bi-scheduler-windows-task.md
```

**Criterio de éxito Día 4:** Cliente accede al dashboard y navega sin ayuda

---

## Día 5 — Soporte post-entrega

### D5-1 Monitoreo post-entrega

Verificar que los datos del cliente están correctos 24h después de entrega:
```
GET /api/client/bi/finance/readiness?companyId=[SLUG]
```

### D5-2 Responder preguntas

Preguntas frecuentes del primer día:
- "¿Por qué el balance no cuadra?" → explicar cierre de ejercicio SAP
- "¿Puedo agregar más períodos?" → sí, ejecutando más extracciones OJDT
- "¿Puedo exportar todo?" → sí, 4 CSVs disponibles
- "¿Cuándo se actualiza solo?" → Sprint 21C (scheduler automático próximamente)

### D5-3 Documentar feedback

- [ ] Cuentas adicionales que el contador quiere clasificar diferente
- [ ] Features que el cliente pidió (para backlog)
- [ ] Problemas encontrados y resolución

**Criterio de éxito Día 5:** Cliente satisfecho, sin errores críticos, próxima actualización manual agendada

---

## Comandos de referencia rápida

```powershell
# Validar conectividad SAP
dotnet DataBision.Extractor.exe --validate

# Extracción completa (en orden)
dotnet DataBision.Extractor.exe --object OACT --send
dotnet DataBision.Extractor.exe --object OJDT --send

# Refresh pipeline
dotnet DataBision.Extractor.exe --transform-mart --company [ANALYTICS_COMPANY_ID]

# Validar staging
dotnet DataBision.Extractor.exe --validate-staging

# Ver últimas ejecuciones
dotnet DataBision.Extractor.exe --validate-ops --company [ANALYTICS_COMPANY_ID]
```

```sql
-- Verificar pipeline completo
SELECT * FROM mart.refresh_accounting_all('[ANALYTICS_COMPANY_ID]');

-- Ver clasificación de cuentas
SELECT statement_line, COUNT(*) FROM mart.gl_accounts
WHERE company_id = '[ANALYTICS_COMPANY_ID]' GROUP BY statement_line;

-- Health check rápido
SELECT readiness_status, health_score FROM mart.finance_readiness_view
WHERE company_id = '[ANALYTICS_COMPANY_ID]';
```

---

## Escalación

| Problema | Acción |
|---|---|
| No conecta SAP SL | Verificar firewall con TI cliente → si persiste, escalar a soporte SAP |
| refresh_accounting_all falla | Ver logs específicos del paso que falló → contactar equipo DataBision |
| healthScore < 50 | Revisar clasificación PCGE con contador → ajustar reglas |
| Balance no cuadra en producción | Confirmar si SAP tiene asientos de cierre → documentar y explicar |
| Cliente no puede acceder al dashboard | Verificar slug, módulos habilitados, auth |
