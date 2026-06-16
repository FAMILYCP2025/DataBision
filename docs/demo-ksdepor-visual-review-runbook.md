# Demo KSDEPOR — Visual Review Runbook

Sprint 8O — Junio 2026

Guía paso a paso para realizar la revisión visual completa del portal en browser antes de una demo con KSDEPOR.

---

## Prerequisitos

Antes de empezar la revisión visual, el ambiente debe estar levantado:

```powershell
# Terminal 1 — API backend
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5103"
dotnet run --project src\DataBision.Api --no-launch-profile

# Terminal 2 — Frontend dev server
cd databision-frontend
npm run dev
```

Confirmar:
- API en `http://localhost:5103/swagger` → responde 200
- Frontend en `http://localhost:5173` → carga sin error en consola

---

## Paso 1 — Login

URL: `http://localhost:5173/client/login?tenant=ksdepor`

**Qué verificar:**
- [ ] Pantalla de login carga sin errores en consola
- [ ] Logo/título de la plataforma visible (DataBision o nombre del tenant)
- [ ] Campos email y contraseña visibles
- [ ] Botón de login activo
- [ ] Fondo con color correcto (blanco o brand secundario)

**Credenciales de demo:**
> Ver `docs/demo-ksdepor-local-runbook.md` — sección Credenciales.

**Acción:** Iniciar sesión. Debe redirigir al portal con el sidebar completo.

---

## Paso 2 — Ventas (`/client/bi/sales?tenant=ksdepor`)

**Qué verificar:**
- [ ] Header: título "Ventas", descripción "Análisis de ventas por rango de fechas", badge "Native BI"
- [ ] DateRangePicker visible en el header (derecha)
- [ ] 4 KPI cards: Ventas netas, Ventas brutas, Facturas, Ticket promedio
- [ ] KPI cards con valores numéricos formateados en español (CLP con `.` de miles)
- [ ] Tab bar con 4 tabs: Clientes, Productos, Vendedores, Fulfillment
- [ ] Tab "Clientes" activa por defecto con tabla de clientes
- [ ] Tabla con columnas: Cliente, Ventas netas, Facturas, Ticket prom., Última factura
- [ ] Paginación visible si hay más de 20 filas
- [ ] Tab "Fulfillment" muestra tabla de tasa de cumplimiento por período
- [ ] Columna "Tasa cumplimiento" colorea en naranja si < 80%, verde si ≥ 80%
- [ ] Sin errores en consola del browser

---

## Paso 3 — Compras (`/client/bi/purchasing?tenant=ksdepor`)

**Qué verificar:**
- [ ] Header: título "Compras", descripción "Órdenes de compra, proveedores y recepciones — últimos 30 días", badge "Native BI"
- [ ] 4 KPI cards: Órdenes de compra, Monto OC, Monto recibido, Proveedores activos
- [ ] Tab bar con 2 tabs: Proveedores, Recepciones
- [ ] Tab "Proveedores" activa por defecto con tabla
- [ ] Tabla de proveedores: Proveedor (nombre + código), OC, Monto OC, Recibido, Prom. OC, Última OC
- [ ] Tab "Recepciones" muestra tabla de recepciones por proveedor
- [ ] Si alguna tabla está vacía: empty state con "Sin datos de ... en el período analizado."
- [ ] Sin errores en consola

---

## Paso 4 — Inventario (`/client/bi/inventory?tenant=ksdepor`)

**Qué verificar:**
- [ ] Header: título "Inventario", descripción "Rotación de artículos y movimientos por almacén", badge "Native BI"
- [ ] 4 KPI cards: Alta rotación, Rotación normal, Baja rotación, Sin movimiento
- [ ] Tab bar con 2 tabs: Rotación, Almacenes
- [ ] Tab "Rotación" activa con tabla de artículos
- [ ] Columna "Rotación" muestra badge de color: verde=Alta, azul=Normal, naranja=Baja, gris=Sin movimiento
- [ ] Tab "Almacenes" muestra tabla de movimientos por almacén
- [ ] Si almacenes vacío: "Stock por almacén pendiente de habilitar según endpoint disponible en Service Layer..."
- [ ] Sin errores en consola

---

## Paso 5 — Finanzas (`/client/bi/finance?tenant=ksdepor`)

**Qué verificar:**
- [ ] Header: título "Finanzas", descripción "Cuentas por cobrar y por pagar — vencimientos y aging", badge "Native BI"
- [ ] 4 KPI cards: AR vencido, % vencido (último período), Facturas emitidas (30d), Monto facturado (30d)
- [ ] Tab bar: Cuentas por cobrar (AR), Cuentas por pagar (AP)
- [ ] Tab AR activa por defecto con tabla de clientes con saldo vencido
- [ ] Montos vencidos en rojo cuando > 0
- [ ] Columna "+90d" en rojo cuando > 0
- [ ] Tab AP: si vacío → "Sin datos de cuentas por pagar en el ambiente de demo..."
- [ ] Sin errores en consola

---

## Paso 6 — Operaciones (`/client/bi/operations?tenant=ksdepor`)

**Qué verificar:**
- [ ] Header: título "Operaciones", descripción "Salud del pipeline de datos, alertas activas y calidad de datos", badge "Native BI"
- [ ] 4 KPI cards: Health score, Alertas activas, Objetos extraídos, Errores DQ sin resolver
- [ ] Bloque de estado: Extractor (status + timestamp) y Transform (status + timestamp)
- [ ] Puntos de estado correctos: verde=ok, naranja=warning, rojo=error
- [ ] Tab bar: Alertas, Calidad de datos
- [ ] Tab "Alertas": si sin alertas → "Sin alertas activas. El pipeline no presenta alertas pendientes."
- [ ] Si hay alertas: badge rojo con conteo en el tab
- [ ] Tab "Calidad de datos": si sin problemas → "Sin problemas de calidad de datos detectados."
- [ ] Sin errores en consola

---

## Paso 7 — Sidebar y navegación

**Qué verificar:**
- [ ] Sidebar oscuro (`#0F172A`) visible en todas las rutas
- [ ] Sección "Análisis" con 5 items: Ventas, Compras, Inventario, Finanzas, Operaciones
- [ ] Item activo con fondo `#1E293B` claramente diferenciado
- [ ] Transición suave al cambiar de ruta
- [ ] Header superior muestra nombre de empresa (izquierda) y usuario (derecha)
- [ ] Sin desbordamiento ni elementos cortados en viewport 1280×800

---

## Criterios de PASS/FAIL

| Pantalla | PASS | FAIL |
|---|---|---|
| Login | Carga y autentica | Error de CORS o JWT |
| Ventas | Datos y 4 tabs | Tabla en blanco sin empty state |
| Compras | Datos y 2 tabs | Error 500 en consola |
| Inventario | Badges de rotación | Página en blanco |
| Finanzas | AR con datos | Error de carga |
| Operaciones | Health score visible | Sin datos de salud |

---

## Notas para el presentador

- Si el ambiente se levantó hace más de 10 minutos antes de la demo, hacer refresh del browser para que los tokens de sesión estén frescos.
- Tener la ventana del browser en modo 1280px de ancho mínimo (laptop estándar).
- No mostrar la URL con `?tenant=ksdepor` — aclarar que en producción el portal se accede por subdominio (`ksdepor.databision.app`).
