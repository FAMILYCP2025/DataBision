# Demo KSDEPOR — Visual Polish

Sprint 8N — Junio 2026

Registro de todos los cambios visuales y de texto aplicados en el sprint de pulido para la demo comercial de KSDEPOR.

---

## Objetivo

Asegurar que todas las pantallas del portal de BI nativo presenten descripciones claras y comercialmente comprensibles, y que los mensajes de estado vacío sean informativos y profesionales.

---

## Cambios aplicados

### 1. Operaciones — Descripción de página

**Archivo:** `databision-frontend/src/client/pages/OperationsDashboardPage.tsx`

| Campo | Antes | Después |
|---|---|---|
| `description` del header | "Estado del pipeline de extracción y transformación" | "Salud del pipeline de datos, alertas activas y calidad de datos" |

**Razón:** La descripción original era demasiado técnica para un contexto comercial. La nueva descripción comunica el valor al decisor (salud del proceso, alertas, calidad) sin exponer detalles de implementación.

---

### 2. Operaciones — Empty state de alertas

**Archivo:** `databision-frontend/src/client/pages/OperationsDashboardPage.tsx`

| Campo | Antes | Después |
|---|---|---|
| `NbEmptyState message` (tab Alertas) | "Sin alertas activas." | "Sin alertas activas. El pipeline no presenta alertas pendientes." |

**Razón:** El mensaje original era demasiado corto. El nuevo mensaje refuerza que el pipeline está saludable, lo cual es un punto comercial positivo durante la demo.

---

### 3. Inventario — Empty state de rotación

**Archivo:** `databision-frontend/src/client/pages/InventoryDashboardPage.tsx`

| Campo | Antes | Después |
|---|---|---|
| `NbEmptyState message` (tab Rotación) | "Sin datos de rotación." | "Sin datos de rotación de artículos en el período analizado." |

**Razón:** El mensaje genérico no daba contexto. El nuevo mensaje indica que el vacío es relativo al período, lo cual es más preciso e informativo.

---

### 4. Finanzas — Empty state de AR

**Archivo:** `databision-frontend/src/client/pages/FinanceDashboardPage.tsx`

| Campo | Antes | Después |
|---|---|---|
| `NbEmptyState message` (tab AR) | "Sin datos de cuentas por cobrar." | "Sin datos de cuentas por cobrar en el período analizado." |

**Razón:** Mismo patrón que Inventario — el contexto del período hace el mensaje más preciso.

---

### 5. Compras — Empty states de proveedores y recepciones

**Archivo:** `databision-frontend/src/client/pages/PurchasingDashboardPage.tsx`

| Campo | Antes | Después |
|---|---|---|
| `NbEmptyState message` (tab Proveedores) | "Sin datos de proveedores." | "Sin datos de proveedores en el período analizado." |
| `NbEmptyState message` (tab Recepciones) | "Sin datos de recepciones." | "Sin datos de recepciones en el período analizado." |

**Razón:** Mismo patrón de contextualización por período.

---

## Cambios NO realizados (ya estaban correctos)

| Pantalla | Campo | Estado | Mensaje actual |
|---|---|---|---|
| Operaciones | DQ empty state | ✅ Correcto | "Sin problemas de calidad de datos detectados." |
| Inventario | Almacenes empty state | ✅ Ya actualizado en 8M | "Stock por almacén pendiente de habilitar según endpoint..." |
| Finanzas | AP empty state | ✅ Ya actualizado en 8M | "Sin datos de cuentas por pagar en el ambiente de demo..." |
| Ventas | Descripción | ✅ Correcto | "Análisis de ventas por rango de fechas" |
| Compras | Descripción | ✅ Correcto | "Órdenes de compra, proveedores y recepciones — últimos 30 días" |
| Inventario | Descripción | ✅ Correcto | "Rotación de artículos y movimientos por almacén" |
| Finanzas | Descripción | ✅ Correcto | "Cuentas por cobrar y por pagar — vencimientos y aging" |

---

## Branding

El portal usa colores por defecto (`--brand-primary: #2563EB`) ya que el tenant KSDEPOR en el ambiente de desarrollo no tiene una configuración personalizada cargada via `GET /api/tenant/config`.

- El componente `BrandingLoader` está activo y aplicaría branding personalizado automáticamente si KSDEPOR tuviera configuración en la tabla `tenant_config`.
- Para la demo: mencionar la capacidad de personalización de marca sin intentar demostrarla visualmente en el ambiente actual.
- En producción: el logo, colores y nombre de empresa se configuran desde el panel de SuperAdmin.

**El sidebar usa `#0F172A` (color fijo de plataforma) y el color de acento activo es el brand primary.** Esto es correcto y no debe cambiarse.

---

## Convenciones visuales verificadas

| Elemento | Estado |
|---|---|
| Sidebar oscuro con íconos y labels claros | ✅ |
| Tabs con indicador `border-bottom brand-primary` | ✅ Consistente en todas las páginas |
| KPI cards con label + valor + subLabel opcional | ✅ |
| Empty states con ícono + mensaje descriptivo | ✅ Actualizado |
| Tabla con `fontVariantNumeric: tabular-nums` en valores numéricos | ✅ |
| Colores semánticos: verde=ok, naranja=warning, rojo=error | ✅ |
| `fmtDate` con `es-CL` locale en todas las páginas | ✅ |
| `fmtAmt` con `es-CL` locale en todas las páginas | ✅ |

---

## Build

`npm run build` ejecutado tras estos cambios — ver resultado en la sesión de Sprint 8N.
