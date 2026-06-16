# Demo KSDEPOR — Smoke Test Técnico

Sprint 8M — Junio 2026

Secuencia mínima de validación antes de cada demo. Ejecutar en orden. Cada paso debe pasar antes de continuar.

---

## Paso 1 — Validar API

```powershell
Invoke-WebRequest http://localhost:5103/swagger -UseBasicParsing
```

**Resultado esperado:** `StatusCode: 200`

**Si falla:** La API no está corriendo. Levantarla primero:
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5103"
dotnet run --project src\DataBision.Api --no-launch-profile
```

---

## Paso 2 — Validate Staging

```powershell
dotnet run --project src\DataBision.Extractor --configuration Debug -- --validate-staging
```

**Resultado esperado:**
```
[VS-01] PASS — Supabase connection open
[VS-02] Schemas present: cfg, ctl, mart, ops, raw, stg
[VS-03] cfg.process=5, cfg.dashboard=20, cfg.company_process_enabled=5
[VS-04] ops.alert_rule=8 (expected 8)
[VS-05] Tables (32): ...
=== --validate-staging: ALL PASS ===
```

**Si falla:** Verificar conexión a Supabase y que las variables de entorno están disponibles.

---

## Paso 3 — Validate OPS

```powershell
dotnet run --project src\DataBision.Extractor --configuration Debug -- --validate-ops --company company-dev-001
```

**Resultado esperado:**
```
[OPS-01] Connection open
[OPS-02] extractor_run: total=N, errors=E
[OPS-03] extractor_page_log: N pages logged
[OPS-04] transform_run: N runs
[OPS-05] alert_event: N events fired
  run: obj=OPDN status=SUCCESS ...
=== --validate-ops: DONE ===
```

**Atención:** El `status` del último run de cada objeto relevante debe ser SUCCESS. Si OPDN muestra ERROR, ver sección de contingencia.

**Contingencia OPDN ERROR:** Ejecutar con API activa:
```powershell
dotnet run --project src\DataBision.Extractor --configuration Debug -- --object OPDN --send --page-size 10 --max-pages 2
```

---

## Paso 4 — Transform STG+MART

```powershell
dotnet run --project src\DataBision.Extractor --configuration Debug -- --transform --include-mart --company company-dev-001
```

**Resultado esperado:**
```
Transform complete
```

Duración estimada: 30–90 segundos.

---

## Paso 5 — Frontend Build

```powershell
cd databision-frontend
npm run build
```

**Resultado esperado:**
```
✓ built in Xms
0 Error(s)
```

La advertencia `INEFFECTIVE_DYNAMIC_IMPORT` es pre-existente y no bloqueante.

---

## Paso 6 — Login en Browser

```
http://localhost:5173/client/login?tenant=ksdepor
```

**Resultado esperado:** Pantalla de login carga. Login exitoso lleva al portal con sidebar completo.

---

## Criterios de PASS/FAIL

| Check | Pass | Fail |
|---|---|---|
| Swagger responde 200 | ✅ | Levantar API |
| validate-staging ALL PASS | ✅ | Revisar Supabase |
| validate-ops DONE, OPDN SUCCESS | ✅ | Re-run OPDN con API activa |
| Frontend build 0 errores | ✅ | Revisar TypeScript |
| Login OK | ✅ | Revisar credenciales o API |
| Todos los dashboards cargan | ✅ | Ejecutar transform antes de demo |

---

## Notas de Diagnóstico Rápido

- **OPDN ERROR en validate-ops**: Siempre es porque la API no estaba activa durante la extracción. No es un problema de SAP ni del extractor. SAP extrae correctamente las filas; solo falla el envío al endpoint de ingest.
- **alert_event no crece**: Esperado tras sprint 8M. La deduplicación en `ops.evaluate_alert_rules` impide insertar alertas repetidas para la misma regla activa.
- **extractor_page_log > 0**: Confirma que el logging de páginas está activo. Debe crecer con cada run exitoso paginado.
