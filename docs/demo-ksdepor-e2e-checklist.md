# Demo KSDEPOR — Checklist End-to-End

Sprint 8L — Junio 2026

Seguir este checklist paso a paso durante la sesión de demo. Cada paso tiene un resultado esperado y una nota de contingencia.

---

## Fase 1: Preparación Técnica (antes de que el cliente entre)

### Paso 1 — Levantar API

```powershell
cd C:\Users\user\Documents\Claude_dev\DataBision
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5103"
dotnet run --project src\DataBision.Api --no-launch-profile
```

- [ ] **Resultado esperado:** `Now listening on: http://localhost:5103`
- **Contingencia:** Si no arranca, verificar puerto libre: `netstat -an | findstr 5103`

---

### Paso 2 — Validar Swagger

```powershell
Invoke-WebRequest http://localhost:5103/swagger -UseBasicParsing
```

- [ ] **Resultado esperado:** `StatusCode: 200`
- [ ] O abrir en browser: `http://localhost:5103/swagger`
- **Contingencia:** Si devuelve error, verificar que la API completó el startup (esperar 10–15 seg más).

---

### Paso 3 — Ejecutar validate-staging

```powershell
dotnet run --project src\DataBision.Extractor --configuration Debug -- --validate-staging
```

- [ ] **Resultado esperado:** `=== --validate-staging: ALL PASS ===`
- **Contingencia:** Si falla algún VS-XX, verificar conexión a Supabase y que las variables de entorno están disponibles.

---

### Paso 4 — Ejecutar validate-ops

```powershell
dotnet run --project src\DataBision.Extractor --configuration Debug -- --validate-ops --company company-dev-001
```

- [ ] **Resultado esperado:** `=== --validate-ops: DONE ===` con transform_run > 0
- **Contingencia:** Si extractor_run errors es alto, no es bloqueante — es histórico.

---

### Paso 5 — Ejecutar Transform STG+MART

```powershell
dotnet run --project src\DataBision.Extractor --configuration Debug -- --transform --include-mart --company company-dev-001
```

- [ ] **Resultado esperado:** `Transform complete`
- [ ] Duración: 30–90 segundos
- **Contingencia:** Si falla, verificar logs de la ejecución. Los datos del run anterior siguen disponibles.

---

### Paso 6 — Levantar Frontend

En terminal separada:

```powershell
cd C:\Users\user\Documents\Claude_dev\DataBision\databision-frontend
npm run dev
```

- [ ] **Resultado esperado:** `Local: http://localhost:5173/`
- **Contingencia:** Si el puerto está ocupado, `npm run dev -- --port 5174`

---

## Fase 2: Demo con Cliente

### Paso 7 — Entrar al Portal

Abrir en browser:
```
http://localhost:5173/client/login?tenant=ksdepor
```

- [ ] **Resultado esperado:** Pantalla de login con branding DataBision
- [ ] Hacer login con credenciales demo
- [ ] **Resultado esperado:** Llegar al portal con sidebar visible

---

### Paso 8 — Mostrar Ventas

Navegar a `/client/bi/sales`

- [ ] KPIs de cabecera visibles (Ventas netas, Ventas brutas, Facturas, Ticket promedio)
- [ ] Tab "Clientes" — tabla con datos de clientes
- [ ] Tab "Productos" — tabla con ítems vendidos
- [ ] Tab "Vendedores" — tabla con performance de vendedores
- [ ] Tab "Fulfillment" — tabla con tasa de cumplimiento por período

**Qué decir:** "Esto viene de OINV, ORIN, ORDR y ODLN de SAP — directamente de sus datos."

---

### Paso 9 — Mostrar Compras

Navegar a `/client/bi/purchasing`

- [ ] KPIs visibles (Total OC, Monto, Recepciones, Proveedores)
- [ ] Tab "Proveedores" — tabla paginada con ranking de proveedores
- [ ] Tab "Recepciones" — tabla con recepciones de mercadería

**Qué decir:** "Esto viene de OPOR (órdenes de compra) y OPDN (recepciones) de SAP."

---

### Paso 10 — Mostrar Inventario

Navegar a `/client/bi/inventory`

- [ ] KPIs visibles (Fast, Normal, Slow, No Movement item counts)
- [ ] Tab "Rotación" — tabla con badge de estado por ítem (FAST/NORMAL/SLOW/NO_MOVEMENT)
- [ ] Tab "Almacenes" — lista de almacenes con stock

**Qué decir:** "La rotación clasifica automáticamente cada ítem según su actividad de movimiento."

---

### Paso 11 — Mostrar Finanzas

Navegar a `/client/bi/finance`

- [ ] KPIs visibles (AR vencido, % vencido, Facturas, Monto facturado)
- [ ] Tab "Cuentas por cobrar (AR)" — tabla con aging por cliente, montos vencidos en rojo
- [ ] Tab "Cuentas por pagar (AP)" — mostrar (puede estar vacío — ver limitaciones conocidas)

**Qué decir:** "El aging de cuentas por cobrar muestra qué clientes tienen deuda vencida y de qué antigüedad."

---

### Paso 12 — Mostrar Operaciones

Navegar a `/client/bi/operations`

- [ ] Health score visible (valor sobre 100)
- [ ] Estado del Extractor (OK/warning/error) con timestamp del último run
- [ ] Estado del Transform con timestamp
- [ ] Tab "Alertas" — tabla de alertas activas con severidad
- [ ] Tab "Calidad de datos" — tabla de problemas detectados

**Qué decir:** "Este módulo es para el equipo técnico. Muestra si el pipeline de datos está funcionando bien, cuándo fue la última extracción y si hay problemas de calidad en los datos de SAP."

---

### Paso 13 — Explicar Trazabilidad

> Abrir `ops.extractor_run` o mostrar el panel de operaciones

**Qué decir:** "Cada dato que ven aquí tiene trazabilidad completa: qué objeto SAP se leyó, cuántas páginas se procesaron, cuántas filas se recibieron, en cuánto tiempo, y si hubo algún error. Nada es una caja negra."

---

### Paso 14 — Explicar Arquitectura

Mostrar el diagrama del documento `databision-client-proposal-summary.md` o explicar verbalmente:

```
SAP B1 → Service Layer → Extractor → PostgreSQL → API → Portal
```

**Qué decir:** "SAP no se toca. Solo lectura. Los datos son una copia procesada. El portal siempre muestra los últimos datos disponibles."

---

### Paso 15 — Cerrar con Propuesta Comercial

**Qué decir:** "Lo que vieron es DataBision conectado a datos reales de su empresa en ambiente de desarrollo. El siguiente paso es definir cuándo hacemos la prueba en producción."

**Opciones de cierre:**
- Propuesta formal con precio mensual.
- Reunión técnica con consultor SAP.
- Fecha para primera extracción en producción.

---

## Post-Demo

- [ ] Enviar resumen de la sesión al cliente.
- [ ] Enviar documento de propuesta: `databision-client-proposal-summary.md`.
- [ ] Agendar siguiente reunión.
- [ ] Documentar cualquier feedback o pregunta que no se pudo responder en el momento.
