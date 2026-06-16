# Demo KSDEPOR — Final Readiness Checklist

Sprint 8Q — Junio 2026

Checklist definitivo de preparación antes de la demo comercial con KSDEPOR. Consolida los checks técnicos, visuales, comerciales y de contingencia.

---

## Sección 1: Ambiente técnico

### Backend

- [ ] `dotnet build` sin errores ni warnings
- [ ] API corriendo: `GET http://localhost:5103/swagger` → 200 OK
- [ ] Credenciales de Supabase disponibles en `appsettings.Development.json`

### Base de datos (Staging / Supabase)

- [ ] `--validate-staging ALL PASS`
  - [VS-01] Supabase connection open
  - [VS-02] Schemas: cfg, ctl, mart, ops, raw, stg
  - [VS-03] cfg.process=5, cfg.dashboard=20
  - [VS-04] ops.alert_rule=8
  - [VS-05] Tables (32)
- [ ] `--validate-ops DONE`
  - Último run de OPDN: STATUS = SUCCESS
  - alert_event estable (no crece entre runs)
- [ ] Transform ejecutado recientemente (`--transform --include-mart`)

### Frontend

- [ ] `npm run build` → `✓ built in Xms` sin errores de TypeScript
- [ ] Dev server corriendo: `npm run dev` → `http://localhost:5173`
- [ ] No hay errores en consola del browser (F12 → Console)

---

## Sección 2: Validación visual (5 pantallas)

### Login

- [ ] Pantalla de login carga en `?tenant=ksdepor`
- [ ] Login exitoso con credenciales de demo
- [ ] Redirige al portal con sidebar completo

### Ventas

- [ ] Header: "Ventas" con badge "Native BI"
- [ ] 4 KPI cards con valores numéricos reales
- [ ] Tab Clientes: tabla con filas de clientes
- [ ] Tab Productos: tabla con productos y montos
- [ ] Tab Vendedores: tabla con vendedores
- [ ] Tab Fulfillment: tabla con tasa de cumplimiento coloreada
- [ ] DateRangePicker funcional (cambiar fechas y ver que recarga)

### Compras

- [ ] Header: "Compras" con badge "Native BI"
- [ ] 4 KPI cards visibles
- [ ] Tab Proveedores: tabla con proveedores
- [ ] Tab Recepciones: tabla o empty state descriptivo

### Inventario

- [ ] Header: "Inventario" con badge "Native BI"
- [ ] 4 KPI cards (Alta/Normal/Baja/Sin movimiento con conteos)
- [ ] Tab Rotación: badges de color por categoría
- [ ] Tab Almacenes: tabla o empty state con explicación

### Finanzas

- [ ] Header: "Finanzas" con badge "Native BI"
- [ ] KPI de AR vencido con monto real
- [ ] Tab AR: clientes con montos vencidos (rojos si > 0)
- [ ] Tab AP: tabla o empty state explicativo

### Operaciones

- [ ] Header: "Operaciones" con badge "Native BI"
- [ ] Health score visible y con valor numérico
- [ ] Status Extractor: ok (verde) o estado descriptivo
- [ ] Status Transform: ok (verde) o estado descriptivo
- [ ] Tab Alertas: sin alertas O lista de alertas con severidad
- [ ] Tab Calidad de datos: sin problemas OR lista descriptiva

---

## Sección 3: Documentos comerciales listos

- [ ] `databision-ksdepor-demo-commercial-pack.md` ✅
- [ ] `demo-ksdepor-guided-flow.md` ✅ (script leído antes de la demo)
- [ ] `demo-ksdepor-objection-handling.md` ✅ (revisado antes de la demo)
- [ ] `databision-ksdepor-pilot-scope.md` ✅ (propuesta lista para enviar)
- [ ] `databision-ksdepor-demo-follow-up-email.md` ✅ (email listo para personalizar)
- [ ] `databision-roadmap-post-demo.md` ✅

---

## Sección 4: Logística

- [ ] Sala / videollamada preparada con pantalla compartida lista
- [ ] Browser en pantalla de login (no en otra pestaña)
- [ ] Modo incógnito activado (evitar guardado de contraseñas visible)
- [ ] Zoom del browser: 100% (no 80% ni 125%)
- [ ] Notificaciones del OS desactivadas
- [ ] Auriculares para llamada (si es videollamada)
- [ ] Plan B si el internet falla: screenshots de la demo en `docs/demo-screenshots/`

---

## Sección 5: Contingencias documentadas

| Problema | Solución rápida | Referencia |
|---|---|---|
| API no responde | Levantar con `dotnet run --project src\DataBision.Api --no-launch-profile` | `demo-ksdepor-smoke-test.md` |
| validate-staging FAIL | Verificar conexión Supabase y variables de entorno | `demo-ksdepor-local-runbook.md` |
| OPDN en ERROR | Levantar API y re-ejecutar con `--object OPDN --send` | `demo-ksdepor-known-limitations.md` |
| Pantalla en blanco | Ejecutar `--transform --include-mart` antes de demo | `demo-ksdepor-smoke-test.md` |
| Frontend build falla | Revisar errores TypeScript en `npm run build` | `demo-ksdepor-visual-polish.md` |
| Browser con errores | Limpiar caché, abrir en modo incógnito | — |
| Internet caído | Usar screenshots de `docs/demo-screenshots/` para la presentación | `demo-ksdepor-screenshot-guide.md` |

---

## Resultado final

| Sección | Estado | Bloqueante |
|---|---|---|
| Ambiente técnico | ✅ / ❌ | Sí |
| Validación visual (5 pantallas) | ✅ / ❌ | Sí |
| Documentos comerciales | ✅ / ❌ | No (pueden llevarse en paper) |
| Logística | ✅ / ❌ | Parcialmente |

**Solo proceder con la demo si la sección técnica y visual están en ✅.**

Si hay algún ❌ técnico, resolver antes de la hora de la demo. Ver runbooks en `demo-ksdepor-local-runbook.md`.

---

## Versión del checklist

| Sprint | Fecha | Cambios |
|---|---|---|
| 8M | 2026-06-16 | Versión inicial (técnica) |
| 8Q | 2026-06-16 | Versión completa (técnica + visual + comercial + contingencias) |
