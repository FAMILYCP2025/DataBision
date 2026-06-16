# Demo KSDEPOR — Checklist Pre-Demo

Sprint 8L — Junio 2026

Ejecutar antes de cada sesión de demo. Marcar cada ítem antes de llamar al cliente.

---

## Backend

- [ ] API levanta en `http://localhost:5103`
  - Comando: `dotnet run --project src\DataBision.Api --no-launch-profile`
  - Verificar: ver `Now listening on: http://localhost:5103`

- [ ] Swagger responde
  - Comando: `Invoke-WebRequest http://localhost:5103/swagger -UseBasicParsing`
  - Verificar: `StatusCode: 200`

- [ ] validate-staging ALL PASS
  - Comando: `dotnet run --project src\DataBision.Extractor --configuration Debug -- --validate-staging`
  - Verificar: `=== --validate-staging: ALL PASS ===`

- [ ] validate-ops DONE sin errores nuevos
  - Comando: `dotnet run --project src\DataBision.Extractor --configuration Debug -- --validate-ops --company company-dev-001`
  - Verificar: `=== --validate-ops: DONE ===`

- [ ] Transform ejecutado (datos frescos)
  - Comando: `dotnet run --project src\DataBision.Extractor --configuration Debug -- --transform --include-mart --company company-dev-001`
  - Verificar: `Transform complete`

---

## Frontend

- [ ] `npm run build` sin errores TypeScript
  - Desde `databision-frontend/`: `npm run build`
  - Verificar: `✓ built in Xms`, `0 Error(s)`

- [ ] `npm run dev` corriendo
  - Desde `databision-frontend/`: `npm run dev`
  - Verificar: `Local: http://localhost:5173/`

- [ ] Login demo OK
  - URL: `http://localhost:5173/client/login?tenant=ksdepor`
  - Verificar: pantalla de login carga, login exitoso

- [ ] Sidebar muestra sección "Análisis" con todos los dashboards
  - Verificar: Dashboard, Ventas, Compras, Inventario, Finanzas, Operaciones

- [ ] `/client/bi/sales` abre y muestra KPIs
- [ ] `/client/bi/purchasing` abre y muestra KPIs
- [ ] `/client/bi/inventory` abre y muestra KPIs
- [ ] `/client/bi/finance` abre y muestra KPIs
- [ ] `/client/bi/operations` abre y muestra health score

---

## Datos

- [ ] `sales_fulfillment_dashboard` — tabla con registros > 0
- [ ] `purchase_executive_daily` — tabla con registros > 0
- [ ] `purchase_supplier_dashboard` — tabla con registros > 0
- [ ] `purchase_receiving_dashboard` — tabla con registros > 0 *(si OPDN estuvo OK)*
- [ ] `inventory_warehouse_dashboard` — tabla con registros > 0
- [ ] `finance_ar_aging_dashboard` — tabla con registros > 0
- [ ] Operations health score visible y > 0

---

## Ambiente

- [ ] Browser en modo presentación (sin notificaciones, pantalla completa)
- [ ] Resolución de pantalla adecuada (recomendado 1920×1080 o superior)
- [ ] Pestaña de browser abierta en la ruta inicial de demo
- [ ] No hay otras pestañas o aplicaciones que puedan interrumpir

---

## Contingencia Preparada

- [ ] Conocer las limitaciones conocidas del documento `demo-ksdepor-known-limitations.md`
- [ ] Tener abierto el documento de guion comercial por si se necesita improvisar

---

## Contingencia — Qué hacer si algo falla

### Si la API no responde

1. Verificar que el proceso `dotnet run` sigue corriendo en la terminal.
2. Verificar puerto: `netstat -an | findstr 5103`
3. Reiniciar: `dotnet run --project src\DataBision.Api --no-launch-profile`
4. Si persiste: mostrar el swagger.json estático o los datos de validación ya ejecutados.

### Si el frontend no carga

1. Verificar que `npm run dev` sigue corriendo.
2. Verificar puerto: `netstat -an | findstr 5173`
3. Reiniciar: `npm run dev`
4. En último caso: usar screenshots de la interfaz para la demo.

### Si un endpoint devuelve 500

1. Revisar logs en la terminal de la API.
2. Error más probable: timeout de Supabase o query sin datos.
3. Pasar a otra sección de la demo mientras se estabiliza.
4. Mencionar al cliente que el ambiente es de desarrollo y puede tener latencia.

### Si no hay datos en alguna página

1. Ejecutar: `dotnet run --project src\DataBision.Extractor --configuration Debug -- --transform --include-mart --company company-dev-001`
2. Esperar a que termine y recargar la página.
3. Si la tabla del MART está vacía, mencionar que ese módulo se alimenta de datos históricos que se cargan en una extracción completa programada.

### Si Service Layer SAP no responde

1. Esto solo afecta extracción nueva, no los datos ya en MART.
2. Los dashboards seguirán mostrando los datos del último run exitoso.
3. Mencionar: "La extracción es asíncrona y se programa en horarios controlados. El portal siempre muestra los últimos datos disponibles."
