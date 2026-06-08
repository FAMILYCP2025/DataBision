# DataBision — Native BI Screen Specifications

**Versión:** 1.0  
**Fecha:** 2026-06-03  
**Autor:** Lead UX Architect  
**Estado:** Documento de diseño — sin implementación  
**Relacionado:** `frontend-ux-architecture.md` · `dashboard-information-architecture.md`

---

## Índice

1. [Spec 01 — Login por Tenant](#1-spec-01--login-por-tenant)
2. [Spec 02 — Selector de Empresa](#2-spec-02--selector-de-empresa)
3. [Spec 03 — Home Ejecutivo](#3-spec-03--home-ejecutivo)
4. [Spec 04 — Dashboard Ejecutivo](#4-spec-04--dashboard-ejecutivo)
5. [Spec 05 — Módulo Ventas](#5-spec-05--módulo-ventas)
6. [Spec 06 — Módulo Clientes](#6-spec-06--módulo-clientes)
7. [Spec 07 — Módulo Productos](#7-spec-07--módulo-productos)
8. [Spec 08 — Módulo Vendedores](#8-spec-08--módulo-vendedores)
9. [Spec 09 — Módulo Inventario](#9-spec-09--módulo-inventario)
10. [Spec 10 — Sync Center](#10-spec-10--sync-center)
11. [Spec 11 — Alert Center](#11-spec-11--alert-center)
12. [Spec 12 — Configuración](#12-spec-12--configuración)
13. [Estados de Pantalla Universales](#13-estados-de-pantalla-universales)
14. [shadcn/ui — Componentes por Pantalla](#14-shadcnui--componentes-por-pantalla)
15. [ECharts — Gráficos por Pantalla](#15-echarts--gráficos-por-pantalla)
16. [Reglas de Experiencia](#16-reglas-de-experiencia)
17. [Priorización MVP / Fase 2 / Fase 3](#17-priorización-mvp--fase-2--fase-3)
18. [Riesgos UX](#18-riesgos-ux)
19. [Recomendación de Implementación](#19-recomendación-de-implementación)

---

## 1. Spec 01 — Login por Tenant

**URL:** `{slug}.databision.app/login`

### Layout

```
DESKTOP (> 1024px)
┌──────────────────────┬─────────────────────────────┐
│                      │                             │
│   PANEL IZQUIERDO    │    PANEL DERECHO            │
│   60% — brand        │    40% — formulario         │
│                      │                             │
│   [Logo tenant]      │    Bienvenido a [Empresa]   │
│   centrado vertical  │                             │
│                      │    [Label] Correo           │
│   Fondo: brand-      │    [Input email]            │
│   primary sólido     │                             │
│                      │    [Label] Contraseña       │
│   [Tagline]          │    [Input password + 👁]    │
│   blanco/70          │                             │
│                      │    [Btn: Ingresar]          │
│                      │                             │
│                      │    ¿Olvidaste contraseña?   │
│                      │                             │
│                      │    ─────────────────────    │
│                      │    Powered by DataBision    │
└──────────────────────┴─────────────────────────────┘

MOBILE (< 640px)
┌────────────────────────────────────┐
│  [Logo tenant]  fondo brand-primary│
│  [Tagline]      altura: 30vh       │
├────────────────────────────────────┤
│  Bienvenido a [Empresa]            │
│                                    │
│  [Input email]                     │
│  [Input password + 👁]             │
│                                    │
│  [Btn: Ingresar] full width        │
│                                    │
│  ¿Olvidaste tu contraseña?         │
│                                    │
│  Powered by DataBision             │
└────────────────────────────────────┘
```

### Componentes shadcn/ui

- `Input` — email con `type="email"` y `autoFocus` en desktop
- `Input` — password con botón de toggle show/hide (Lucide `Eye`/`EyeOff`)
- `Button` — `variant="default"`, `className="w-full"`, `size="lg"`
- `Label` — asociado explícitamente a cada input
- `Alert` — para errores de autenticación (`variant="destructive"`)

### Estados

| Estado | Comportamiento visual |
|---|---|
| **Default** | Formulario limpio. Email con autofocus en desktop. |
| **Loading** | Botón: spinner reemplaza texto, se deshabilita. Campos permanecen activos. |
| **Error 401** | `Alert` destructivo debajo del formulario: "Email o contraseña incorrectos." Campo email NO se limpia. |
| **Error de red** | `Alert` warning: "No se pudo conectar. Verifica tu conexión." Botón permanece activo. |
| **Error tenant (404)** | Página de error separada: "Portal no encontrado." Logo genérico DataBision. |
| **Primer login** | Formulario normal → post-login redirige a `/change-password` con token temporal. |
| **Cargando tenant** | Splash centrado con spinner en `var(--brand-primary)` antes de mostrar el formulario. |

### Reglas de seguridad a mostrar

Mensaje discreto bajo el formulario (text-xs, muted): "Esta sesión expira en 15 minutos de inactividad. Los datos de tu empresa están protegidos con cifrado TLS."

No es una alerta — es texto informativo que da confianza sin alarmar.

### Recuperación de contraseña

Al hacer click en el link:
- Aparece un `Dialog` (modal) en la misma página.
- Campo: Email de recuperación.
- Botón: "Enviar instrucciones".
- Estado de éxito: "Si el correo existe, recibirás las instrucciones en los próximos minutos."
- No navegar a otra página — la recuperación es un flujo secundario.

---

## 2. Spec 02 — Selector de Empresa

**URL:** `{slug}.databision.app/select-company`

**Cuándo aparece:** Solo si el usuario autenticado tiene acceso a más de una empresa (multi-empresa). En la mayoría de los casos esta pantalla no existe.

### Layout

```
┌────────────────────────────────────────────────┐
│                                                │
│   Logo DataBision (pequeño, centro)            │
│                                                │
│   Selecciona la empresa a consultar            │
│   (subtítulo: sesión activa como [email])      │
│                                                │
│  ┌────────────────────────────────────────┐    │
│  │  [Logo empresa]  Comercial Torres S.A. │    │
│  │  Base: Torres_PRD  ● Productivo        │    │
│  └────────────────────────────────────────┘    │
│                                                │
│  ┌────────────────────────────────────────┐    │
│  │  [Logo empresa]  Holding Torres Norte  │    │
│  │  Base: Torres_HLD  ● Productivo        │    │
│  └────────────────────────────────────────┘    │
│                                                │
│  ┌────────────────────────────────────────┐    │
│  │  [Logo empresa]  Torres Demo           │    │
│  │  Base: Torres_SBX  ○ Sandbox           │    │
│  └────────────────────────────────────────┘    │
│                                                │
└────────────────────────────────────────────────┘
```

### Componentes

- `Card` clickeable para cada empresa (`hover:bg-muted`, `cursor-pointer`)
- Badge de ambiente: `● Productivo` verde / `○ Sandbox` amarillo / `▲ Staging` naranja
- Si hay > 5 empresas: campo de búsqueda sobre las cards

### Comportamiento

- Click en una empresa → guarda `company_id` en `tenantStore` → aplica branding → redirige a `/`.
- La última empresa seleccionada se recuerda en `localStorage`. En el próximo login, la empresa recordada es la primera de la lista y tiene un chip "Último acceso".
- Si hay exactamente una empresa: esta pantalla se salta automáticamente.

### Diferenciación de ambiente

El ambiente es informativo — el usuario no puede cambiar de productivo a sandbox desde aquí. Eso se gestiona desde Settings de admin. La distinción visual (color del badge) previene confusión de datos de sandbox vs producción.

---

## 3. Spec 03 — Home Ejecutivo

**URL:** `{slug}.databision.app/`

### Layout desktop

```
PAGE HEADER
[DataFreshnessTag]  [Período: HOY | MTD | QTD | YTD | 12M | Custom]

─────────────────────────────────────────────────────────────────
KPI STRIP (6 KpiCards en fila)
[Ventas] [Margen%] [Pedidos pend.] [CxC vencida] [Rotación] [Clientes]
─────────────────────────────────────────────────────────────────

ROW 1 (60% / 40%)
┌─────────────────────────────────────┬─────────────────────────┐
│  Ventas del período                 │  Estado Operacional     │
│  RevenueLineChart (actual vs ant.)  │  4 contadores Cockpit   │
│  Altura: 220px                      │  con colores semánticos │
└─────────────────────────────────────┴─────────────────────────┘

ROW 2 (33% / 33% / 33%)
┌─────────────────┬─────────────────┬────────────────────────────┐
│  Top 5 Clientes │  Alertas        │  Sync Status               │
│  Tabla compacta │  Feed 5 ítems   │  Chip + timestamp          │
│                 │                 │  + acceso a Sync Center    │
└─────────────────┴─────────────────┴────────────────────────────┘
```

### Layout mobile

```
[DataFreshnessTag prominente]
[Selector período: tabs scrollables]

[KPI 1: Ventas]    [KPI 2: Margen%]
[KPI 3: Pedidos]   [KPI 4: CxC]
(KPIs 5 y 6 ocultos — reducir scroll inicial)

[Ventas del período — LineChart 160px]

[3 alertas críticas (si las hay)]

[→ Ver Cockpit completo]
```

### KpiCard — especificación visual

```
┌──────────────────────────────────────┐
│  [Ícono 20px]  VENTAS DEL PERÍODO   │  ← label 12px/400/muted
│                                      │
│  $4.230.000                          │  ← value 32px/700/tabular-nums
│                                      │
│  ↑ 12.4%  vs mes anterior           │  ← delta: flecha + % + contexto
│                                      │
│  [sparkline 60×24px]                │  ← ECharts mini line
└──────────────────────────────────────┘
```

### Estado operacional (widget)

```
┌──────────────────────────────────────┐
│  Estado operacional                  │
│                                      │
│  ● 23   Pedidos pendientes           │  amarillo
│  ● 7    Entregas retrasadas          │  rojo
│  ● 3    Stock crítico                │  rojo
│  ✓ 0    Errores de integración       │  verde
│                                      │
│  [Ver Cockpit completo →]            │
└──────────────────────────────────────┘
```

### Estados de la pantalla

| Estado | Comportamiento |
|---|---|
| **Loading** | Skeleton de KPI Strip + placeholders de gráfico y tablas. PageHeader visible. |
| **Stale data** | Banner amarillo no intrusivo en topbar: "Datos desactualizados — última sync hace Xh". DataFreshnessTag en amarillo. Datos se muestran igualmente. |
| **Sin sync inicial** | Full-page onboarding: SVG + "Configurando tu plataforma" + barra de progreso con estado del extractor. |
| **Error de fetch** | Cada widget muestra su EmptyState de error individualmente. No colapsa la página. |
| **Sin alertas** | Widget de alertas muestra: `✓ Sin alertas activas` con ícono verde. |

---

## 4. Spec 04 — Dashboard Ejecutivo

**URL:** `{slug}.databision.app/dashboard`

### Layout desktop

```
PAGE HEADER
[Período] [Comparar con: Mes ant. | Año ant. | Plan] [Granularidad: D|S|M] [↓ Exportar PDF]

─────────────────────────────────────────────────────────────────
KPI STRIP COMERCIAL (6 KpiCards expandidas)
Ventas mes | Ventas YTD | Ticket promedio | Clientes | Nuevos | Retención
─────────────────────────────────────────────────────────────────
KPI STRIP OPERACIONAL (6 KpiCards)
Pedidos | NC | NC% | Cancelados | CxC total | CxC vencida%
─────────────────────────────────────────────────────────────────

ROW 1 — Ventas del mes y acumulado
┌────────────────────────────────────────────────────────────────┐
│  [Número grande: $X.XXX.XXX]  Ventas del mes                  │
│  ↑ 18%  vs mismo mes año anterior                              │
│  [Número grande: $XX.XXX.XXX]  Ventas YTD                     │
│  ↑ 12%  vs YTD año anterior                                    │
└────────────────────────────────────────────────────────────────┘

ROW 2 — Ventas por día + comparación
┌──────────────────────────────────────────────────────────────────┐
│  BarChart: ventas por día del período                            │
│  Línea superpuesta: mismo período año anterior                  │
│  Granularidad: Diaria / Semanal / Mensual                       │
│  Altura: 240px                                                   │
└──────────────────────────────────────────────────────────────────┘

ROW 3 (tres columnas)
┌──────────────────┬──────────────────┬───────────────────────────┐
│  Top 5 Clientes  │  Top 5 Productos │  Top 5 Vendedores         │
│  Tabla rankeada  │  Tabla rankeada  │  Tabla rankeada           │
│  (con delta)     │  (con delta)     │  (con delta)              │
└──────────────────┴──────────────────┴───────────────────────────┘

ROW 4 (cuatro columnas)
┌─────────────┬─────────────┬─────────────┬──────────────────────┐
│  Facturas   │  Pedidos    │  NC emitidas│  Ticket promedio     │
│  emitidas   │  creados    │  y monto    │  con sparkline 24m   │
│  KpiCard    │  KpiCard    │  KpiCard    │  KpiCard             │
└─────────────┴─────────────┴─────────────┴──────────────────────┘
```

### Exportar PDF

Al hacer click en "Exportar PDF":
1. `Dialog` de confirmación: "Exportando Dashboard — período: MTD — comparación: Mes anterior."
2. Botón: "Generar PDF".
3. El PDF incluye: logo del tenant, nombre de la empresa, período, todos los KPIs y gráficos, timestamp de generación, marca de agua "Datos al [timestamp última sync]".
4. El PDF se descarga directamente — no se abre en nueva pestaña.

### KpiCard expandida (Dashboard)

Diferencia con Home: la KpiCard del Dashboard tiene una fila adicional con el delta vs plan y los últimos 3 valores de períodos anteriores en formato inline.

```
┌──────────────────────────────────────┐
│  [Ícono]  VENTAS DEL MES             │
│                                      │
│  $4.230.000                          │
│                                      │
│  ↑ 12.4%  vs mes anterior            │
│  ↑ 8.1%   vs mismo mes año ant.      │
│                                      │
│  [sparkline 24 períodos]             │
│                                      │
│  Apr: $3.9M  Mar: $3.7M  Feb: $3.4M  │  ← mini historial
└──────────────────────────────────────┘
```

### Estados

| Estado | Comportamiento |
|---|---|
| **Loading** | Skeleton de dos KPI Strips + placeholder de gráfico grande + tablas. |
| **Sin datos de plan** | KPIs de "vs plan" se ocultan silenciosamente (no muestran "N/A"). |
| **Período sin datos** | Cada componente muestra su EmptyState "No hay datos para el período seleccionado." |
| **Exportación en curso** | Botón "Exportar PDF" muestra spinner. El resto de la página es interactiva. |

---

## 5. Spec 05 — Módulo Ventas

**URL:** `{slug}.databision.app/sales`

### Sub-navegación (Tabs)

`Overview` · `Por Período` · `Por Vendedor` · `Por Cliente` · `Documentos`

### FilterBar

Aparece debajo de los tabs cuando hay filtros activos. Chips removibles por cada filtro:

```
[Período: MTD ×] [Vendedor: Juan Pérez ×] [Categoría: Electrónico ×]  [Limpiar todo]
```

### Tab: Overview

```
KPI STRIP (5 KpiCards)
[Ventas] [Facturas] [Ticket prom.] [Margen%] [Clientes únicos]

ROW 1 (70% / 30%)
┌──────────────────────────────────────┬────────────────────────────┐
│  RevenueLineChart                    │  DonutChart                │
│  Ventas diarias período actual       │  Ventas por categoría      │
│  vs período anterior                 │  Top 5 + "Otros"           │
│  Altura: 240px                       │                            │
└──────────────────────────────────────┴────────────────────────────┘

ROW 2 — Tabla Top 10 ítems vendidos
Columnas: Código | Descripción | Unidades | Monto | % total | Margen%
Exportable. Ordenable. Paginada (10 filas).
```

### Tab: Por Período

```
CONTROLES
[Granularidad: D | S | M]  [Mostrar tendencia: toggle]

BarChart agrupado (3 series: actual, anterior, año anterior)
Altura: 260px. Barras: brand-primary, brand-secondary, #94A3B8.
markLine de tendencia si toggle activo.

Tabla detalle por período:
Columnas: Período | Ventas | Δ vs anterior ($ y %) | Δ vs año ant.

HeatmapChart (ventas por día × hora)
Aparece solo en granularidad Diaria. Altura: 140px.
Label: "Patrón de ventas por hora del día"
```

### Tab: Por Vendedor

```
BarChart horizontal — Top vendedores (descendente)
Altura: máx 400px (según N° vendedores, 32px por barra).

Tabla de vendedores:
Columnas: Vendedor | Ventas | % total | Facturas | Ticket prom. | Margen% | Δ

Click en fila → Sheet lateral (600px):
  Header: nombre, foto/avatar (iniciales), cargo si disponible
  KPIs: ventas del período, ranking, tickets
  LineChart: evolución 12 meses
  Tabla: top 10 clientes
  Tabla: top 10 productos
```

### Tab: Por Cliente

```
Similar a Por Vendedor pero con columnas adicionales:
CxC vigente | Días prom. pago | Última compra | Segmento (badge)

Click en fila → navega a /customers/:id (no Sheet, página completa)
```

### Tab: Documentos

```
Sub-tabs: Facturas | Pedidos | Notas de Crédito

Cada sub-tab muestra tabla con:
  Facturas: N° doc | Fecha | Cliente | Vendedor | Monto | Estado
  Pedidos: N° doc | Fecha | Cliente | Estado | Monto | Días pendiente
  NC: N° doc | Fecha | Cliente | Factura referencia | Monto | Motivo

Filtro adicional: Estado (Abierto / Cerrado / Cancelado)
Exportación CSV disponible en cada sub-tab.
```

### Estados

| Estado | Comportamiento |
|---|---|
| **Loading** | Skeleton KPI Strip + placeholder gráfico + skeleton tabla (5 filas). |
| **Sin datos período** | EmptyState "no-results": "No hay ventas para el período y filtros seleccionados." + botón "Limpiar filtros". |
| **Filtro sin resultados** | EmptyState "no-results" con la lista de filtros activos. |
| **Error fetch** | EmptyState "error" por componente, no por página. |

---

## 6. Spec 06 — Módulo Clientes

**URL:** `{slug}.databision.app/customers`

### Sub-navegación

`Overview` · `AR Aging` · `En Riesgo`

### Tab: Overview

```
KPI STRIP (4 KpiCards)
[Activos] [Nuevos] [En riesgo] [CxC total]

ROW 1 (55% / 45%)
┌──────────────────────────────────┬────────────────────────────────┐
│  ScatterChart — Segmentación     │  Leyenda de segmentos:         │
│  Eje X: Frecuencia compras/mes   │  ● Champion (alta frec + monto)│
│  Eje Y: Monto promedio período   │  ● Leal (alta frec + med)      │
│  Tamaño punto: CxC pendiente     │  ● En riesgo (baja frec rec.)  │
│  Color punto: segmento           │  ● Inactivo (+180 días)        │
│                                  │                                │
│  Click en punto → /customers/:id │  Click en leyenda → filtro     │
└──────────────────────────────────┴────────────────────────────────┘

Tabla de clientes (25 por página):
Columnas: Nombre | Segmento | Último pedido | Compras período | CxC vencida | Vendedor
Ordenable. Exportable. Buscable (input de búsqueda sobre la tabla).
```

### Tab: AR Aging

```
KPI STRIP (4 KpiCards CxC)
[CxC total] [CxC vencida (>30d) y %] [CxC >90d y %] [DSO días]

StackedBarChart horizontal — Un cliente por barra
  Verde: Corriente (0–30d)
  Amarillo: Vencido 31–60d
  Naranja: Vencido 61–90d
  Rojo: Vencido >90d
Solo clientes con saldo > 0. Ordenados por saldo total desc.
Altura: min 300px, max 600px (dinámico según N° clientes).

Tabla de aging debajo:
Columnas: Cliente | Corriente | 31–60d | 61–90d | >90d | Total | Vendedor
Click en fila → expande facturas vencidas del cliente:
  Sub-tabla: N° factura | Fecha | Días vencida | Monto | [Ver en SAP]
```

### Tab: En Riesgo

```
Banner informativo:
"Clientes que no han comprado en los últimos N días"
N configurable en /settings/alerts (default: 60 días)

KPI:
[N° clientes en riesgo] [Valor histórico en riesgo ($sum 12m)]

Tabla:
Columnas: Cliente | Último pedido | Días sin compra | Compras 12m | Vendedor asignado | [Acción]
Columna Acción: botón "Crear seguimiento" → Business Action prellenado.

Ordenada por "Días sin compra" descendente.
```

### Perfil de Cliente — `/customers/:id`

```
HEADER (fondo surface, padding 24px)
[Avatar/iniciales]  Nombre del cliente
                    RUT/NIT · Vendedor asignado · Ciudad/Zona
                    [badge: segmento]  [badge: estado]
                    [Btn: Crear acción]

TABS: Resumen | Pedidos | Facturas | CxC | Acciones

Tab Resumen:
  KPIs del cliente: Ventas período | Ventas 12m | CxC pendiente | Días prom. pago
  LineChart: evolución de compras mensual (últimos 12 meses)
  Top 5 productos comprados (tabla compacta)

Tab Pedidos:
  Tabla: N° | Fecha | Monto | Estado | Vendedor
  Filtros: Estado · Período

Tab Facturas:
  Tabla: N° | Fecha | Monto | Pagada | Días para vencer / vencida
  Filtros: Estado pago · Período

Tab CxC:
  Aging del cliente (mismo componente que AR Aging pero para 1 cliente)
  Con detalle de facturas expandibles

Tab Acciones:
  Lista de Business Actions vinculadas a este cliente
  Con estado y notas de resolución
```

---

## 7. Spec 07 — Módulo Productos

**URL:** `{slug}.databision.app/products`

### Sub-navegación

`Overview` · `Márgenes` · `Sin Rotación`

### Tab: Overview

```
KPI STRIP (4 KpiCards)
[Ítems activos en ventas] [Ítem top por monto] [Margen promedio%] [Ítems sin venta]

Tabla de productos (25 por página):
Columnas: Código | Descripción | Categoría | Unidades | Monto | % total | Margen% | Stock | Rotación (días)
Ordenable. Exportable.
Filtros: Categoría · Con ventas/Sin ventas · Margen > X%

DonutChart (lateral o debajo en mobile):
Composición de ventas por categoría de producto.
```

### Tab: Márgenes

```
BarChart horizontal — Todos los ítems con ventas en período
  Eje Y: ItemCode/ItemName
  Eje X: Margen %
  markLine: promedio de la empresa (línea vertical punteada)
  Colores barras:
    Verde si margen > promedio + 5pp
    Brand-primary si margen ± 5pp
    Rojo si margen < promedio − 5pp
  Max 30 ítems visibles — paginación del gráfico o scroll vertical.
Altura total: dinámica.

Tabla detalle (debajo):
Columnas: Código | Descripción | Costo prom. | Precio prom. | Margen bruto | Margen% | Unidades | Monto
Click en fila → expande mini gráfico inline (LineChart precio vs costo, 12 meses)
```

### Tab: Sin Rotación

```
Banner:
"Ítems con stock disponible y sin ventas en los últimos N días"
N configurable (default: 90 días)

KPI:
[N° ítems sin rotación] [Valor inmovilizado] [% del stock total]

Tabla:
Columnas: Código | Descripción | Última venta | Días sin venta | Stock actual | Valor inmovilizado
Ordenada por días sin venta desc.
Acción en fila: [Crear insight] → abre /insights con draft prellenado con la lista.
```

### Perfil de Producto — `/products/:id`

```
HEADER
[Código]  [Descripción del ítem]
          Grupo/Categoría · Unidad de medida · Proveedor principal (si disponible)

TABS: Desempeño | Márgenes | Inventario | Sustitutos

Tab Desempeño:
  LineChart: evolución ventas mensuales (12 meses)
  Top 10 clientes que lo compraron (tabla)
  Distribución por vendedor (DonutChart)

Tab Márgenes:
  LineChart doble: precio promedio vs costo promedio (12 meses)
  Tabla: mes | precio | costo | margen%

Tab Inventario:
  Tabla: Almacén | Stock | Mínimo | Máximo | Status
  Últimos 10 movimientos (tabla)

Tab Sustitutos:
  Ítems del mismo grupo/categoría
  Tabla comparativa: código | descripción | ventas período | margen% | stock
```

---

## 8. Spec 08 — Módulo Vendedores

**URL:** `{slug}.databision.app/salesreps`

### Sub-navegación

`Overview / Ranking` · `Comparación Mensual`  
(Tab `Cumplimiento` solo si hay metas configuradas en Settings)

### Tab: Overview / Ranking

```
KPI STRIP (4 KpiCards)
[Ventas equipo] [Vendedor líder: nombre + monto] [Promedio por vendedor] [Vendedores activos]

BarChart horizontal — Ranking descendente
  Un vendedor por barra
  Color: brand-primary
  Label en la barra: monto formateado
  Altura: dinámica (40px por vendedor, max 600px con scroll)

Tabla de vendedores:
Columnas: # | Vendedor | Ventas período | % total | Facturas | Ticket prom. | Margen% | Δ vs mes ant.
Columna #: posición en ranking.
Click en fila → Sheet lateral (600px):
  Header: nombre, ranking actual, ranking mes anterior (subió/bajó N posiciones)
  KPIs: ventas período, clientes atendidos, ticket promedio
  LineChart: evolución ventas 12 meses
  Tabla: top 10 clientes del vendedor
  Tabla: top 10 productos del vendedor
```

### Tab: Comparación Mensual

```
BarChart agrupado — Cada grupo = un vendedor, cada barra = un mes (últimos 6 meses)
Permite comparar la evolución de cada vendedor mes a mes.
Altura: 300px.

HeatmapChart de actividad:
  Eje X: meses
  Eje Y: vendedores
  Color: cantidad de facturas o monto (seleccionable)
  Permite ver quién fue más activo en qué período.
Altura: 160px.

Tabla comparativa:
Columnas: Vendedor | Mes-5 | Mes-4 | Mes-3 | Mes-2 | Mes-1 | Mes actual | Tendencia
```

### Tab: Cumplimiento (si hay metas)

```
Para cada vendedor:
  [Avatar] [Nombre] [Meta: $X.XXX.XXX] [Alcanzado: $X.XXX.XXX] [%: XX%]
  ProgressBar: color verde si ≥100%, amarillo si 70–99%, rojo si <70%

Gauge (uso justificado aquí — es para comparar real vs meta):
  Gauge semicircular por vendedor (tamaño compacto: 120px)
  Dial: % de cumplimiento
  Solo si el número total de vendedores es ≤ 6 (si hay más, usar tabla)
```

### Perfil de Vendedor — `/salesreps/:id`

```
HEADER
[Avatar/iniciales]  Nombre del vendedor
                    Cargo · Zona asignada (si aplica)
                    Ranking actual: #3 de 8 (↑ desde #5)
                    [btn: Ver clientes asignados]

TABS: Ventas | Clientes | Productos | Meses

Tab Ventas: KPIs + LineChart evolución 12m + tabla facturas período
Tab Clientes: top 20 clientes del vendedor con ventas y CxC
Tab Productos: top 20 productos vendidos con monto y margen
Tab Meses: tabla comparativa mes a mes últimos 12 meses
```

---

## 9. Spec 09 — Módulo Inventario

**URL:** `{slug}.databision.app/inventory`

### Sub-navegación

`Overview` · `Por Almacén` · `Stock Crítico` · `Movimientos`

### Tab: Overview

```
KPI STRIP (4 KpiCards)
[Ítems bajo mínimo — badge rojo] [Sin stock con demanda — badge rojo] [Valor total] [Rotación prom. días]

Tabla de inventario (todos los ítems activos):
Columnas: Código | Descripción | Almacén | Stock actual | Mínimo | Máximo | Status | Rotación
Status badges:
  Sin stock: rojo sólido
  Bajo mínimo: amarillo
  Normal: verde
  Sobre stock: azul claro
Filtrable por Status. Default: muestra "Bajo mínimo" + "Sin stock" primero.

DonutChart (lateral):
Valor de inventario por categoría.
```

### Tab: Por Almacén

```
Tabla resumen:
Columnas: Almacén | Ítems totales | Valor total | Ítems bajo mínimo | Ítems sin stock
Fila clickeable → expande tabla de ítems de ese almacén (inline, no nueva página)

HeatmapChart (si hay ≥ 3 almacenes):
  Eje X: almacenes
  Eje Y: grupos de ítem
  Color: ocupación % (stock actual / stock máximo)
  Permite ver de un vistazo qué almacén tiene problemas en qué categoría.
```

### Tab: Stock Crítico

```
Banner rojo: "X ítems sin stock con demanda activa · Y ítems bajo mínimo"

Tabla stock crítico (filtrada, sin stock o bajo mínimo):
Columnas: Código | Descripción | Almacén | Stock actual | Mínimo | Déficit | Demanda activa (ORDR)
Ordenada por (Déficit + Demanda activa) — más urgente primero.

Acción en cada fila: [Sugerir orden de compra] → crea Business Action prellenado.
```

### Tab: Movimientos

```
FILTROS (siempre visibles en este tab):
Almacén (Select) · Ítem (búsqueda text) · Tipo (MultiSelect) · Período (DateRangePicker)

BarChart stacked:
  Entradas vs Salidas por período (semana o mes)
  Verde: entradas | Rojo: salidas
  Permite ver si el inventario crece o decrece.

Tabla cronológica (paginada 25 filas):
Columnas: Fecha | N° Documento | Tipo (badge) | Ítem | Almacén | Cantidad | Doc. referencia SAP
Colores en cantidad: verde (entradas, +) / rojo (salidas, −)
```

---

## 10. Spec 10 — Sync Center

**URL:** `{slug}.databision.app/sync`

### Layout

```
BANNER GLOBAL (48px, color dinámico)
● Verde: "Sincronización normal — última actualización hace X minutos"
● Amarillo: "Sincronización con retraso — última actualización hace X horas"
● Rojo: "Error de sincronización activo — ver detalles abajo"

SECCIÓN: Estado por extractor
[ExtractorStatusCard 1]
[ExtractorStatusCard 2]  (si hay más de un extractor)

SECCIÓN: Estado Service Layer (solo si Modalidad B)
[ServiceLayerStatusCard]

TABS (debajo)
[Historial de ejecuciones] | [Errores y logs]
```

### ExtractorStatusCard

```
┌──────────────────────────────────────────────────────────────────────┐
│  [badge: RUNNING ●] / [IDLE ○] / [WARNING ⚠] / [ERROR ✗]            │
│                                                                      │
│  Extractor — Empresa Torres (Modalidad A · Dedicado)                 │
│                                                                      │
│  Última ejecución exitosa:    hace 45 min  (12:15:30  03/06/2026)   │
│  Próxima ejecución:           en 15 min   (13:00:00)                │
│  Registros procesados:        1.247 total · 312 nuevos · 935 act.   │
│  Sin cambio:                  0  |  Errores:  0                     │
│  Duración:                    2 min 34 seg                          │
│                                                                      │
│  Objetos SAP sincronizados:                                          │
│  ✓ OINV  hace 45min    ✓ OCRD  hace 45min    ✓ OITM  hace 45min    │
│  ✓ ORDR  hace 45min    ✓ ODLN  hace 45min    ✓ OSLP  hace 45min    │
│                                                                      │
│                                          [Ver logs ↓]  [···]        │
└──────────────────────────────────────────────────────────────────────┘
```

Badge `RUNNING` tiene una animación de pulso verde (CSS `animate-pulse`).

### Tab: Historial de ejecuciones

```
Tabla paginada (25 filas):
Columnas: Timestamp | Extractor | Resultado (badge) | Objetos procesados | Registros | Duración
Resultado badges: Exitosa (verde) | Con errores (amarillo) | Fallida (rojo)

Click en fila → expande detalle:
  Por cada objeto SAP: nombre | resultado | filas ins. | filas act. | skipped | duración
```

### Tab: Errores y logs

```
Lista cronológica de errores:
Cada ítem:
  [badge tipo error]  Timestamp  Extractor
  Mensaje en lenguaje de negocio (1 línea)
  [Ver detalle técnico ▸] → expande stack trace colapsable

Tipos de error (badge + lenguaje de negocio):
  CONNECTIVITY → "No se pudo conectar a SAP B1"
  AUTH         → "Credenciales inválidas o expiradas"
  VALIDATION   → "Datos con formato inesperado"
  TRANSFORM    → "Error al procesar los datos recibidos"
  UNKNOWN      → "Error no clasificado — contactar soporte"

Filtros: extractor · tipo de error · período
```

### Estados

| Estado | Comportamiento |
|---|---|
| **Sin errores** | Tab "Errores y logs" muestra EmptyState positivo: ✓ "Sin errores en los últimos X días" |
| **Primer uso** | No hay historial → mensaje guía sobre primer extractor |
| **Extractor detenido** | Banner rojo + ExtractorStatusCard con badge ERROR en rojo pulsante |

---

## 11. Spec 11 — Alert Center

**URL:** `{slug}.databision.app/alerts`

### Layout

```
PAGE HEADER
[3 contadores: 🔴 3 Críticas | 🟡 12 Operativas | 🔵 5 Comerciales]
[Filtros: Severidad | Estado | Módulo | Asignada a]
[Tab: Activas | Historial]

LISTA DE ALERTAS (ordenadas: Críticas → Operativas → Comerciales → por timestamp)
[AlertCard]
[AlertCard]
...
```

### AlertCard

```
┌─[banda color 4px]──────────────────────────────────────────────────────┐
│  [ícono tipo]  Stock crítico — Ítem XYZ-001 en almacén Central          │
│                                                         hace 2 horas   │
│                                                                        │
│  Stock actual: 0 unidades. Hay 3 pedidos activos por 47 unidades.      │
│                                                                        │
│  [Ver detalle →]  [Crear acción]  [Resolver]  [Silenciar ▾]  [···]    │
└────────────────────────────────────────────────────────────────────────┘
```

Banda: 4px izquierda. Rojo: crítica. Naranja: operativa. Azul: comercial.

### Acciones de una alerta

**Resolver:**
- `Dialog` con textarea: "Nota de resolución (mínimo 10 caracteres)"
- Botón "Marcar como resuelta"
- → Alerta desaparece de Activas, aparece en Historial con nota y usuario

**Silenciar:**
- `DropdownMenu` con opciones: 1 hora · 4 horas · 24 horas
- Badge "Silenciada — reactiva en Xh" aparece en la card
- La alerta permanece en la lista pero con opacidad reducida (70%)

**Crear acción:**
- Abre `Sheet` lateral Business Actions con contexto prellenado
- La alerta muestra chip "Acción creada" con link

**Ver detalle →:**
- Navega al módulo relevante con filtros pre-aplicados
- Ej: alerta stock → /inventory/critical?item=XYZ-001

**Menú ··· (más opciones):**
- Asignar a usuario
- Desestimar (requiere nota)
- Copiar link a esta alerta

### Tab: Historial

```
Lista de alertas cerradas (resueltas · silenciadas vencidas · desestimadas)
Cada ítem muestra: título · severidad · duración activa · usuario que cerró · nota · timestamp cierre.
Filtros: tipo de cierre · período · severidad
```

---

## 12. Spec 12 — Configuración

**URL:** `{slug}.databision.app/settings`

### Sub-navegación lateral (Settings tiene su propio sub-sidebar)

```
SETTINGS SIDEBAR (200px)
  👥  Usuarios
  🔐  Roles y permisos
  🎨  Branding
  📦  Módulos habilitados
  🏢  Empresa SAP
  🔌  Conexión y extractor
  🔔  Alertas
  🔄  Sincronización
```

### Settings: Usuarios

```
Tabla de usuarios:
Columnas: Nombre | Email | Rol | Último acceso | Estado (Activo/Inactivo) | [Acciones]
Acciones: Editar · Desactivar

Botón "+ Nuevo usuario" → Sheet lateral:
  Nombre · Email · Rol (Select) · [Enviar invitación]
```

### Settings: Branding

```
Vista previa en vivo del portal con el branding aplicado (iframe o render simulado)

Campos configurables:
  Logo: upload (SVG/PNG/WEBP) — máx 200KB, bounding box 200×80px
  Logo dark mode: upload opcional
  Color primario: ColorPicker con input hex + preview en tiempo real
  Color sidebar: ColorPicker con preview
  Tagline del login: TextInput
  Favicon: upload (ICO/PNG) — futuro

Los cambios aplican al guardar, no en tiempo real global.
Preview en tiempo real: solo el panel de vista previa.
```

### Settings: Módulos habilitados

```
Lista de módulos con Toggle:
  ✓ Ventas (habilitado — plan Business)
  ✓ Clientes (habilitado — plan Business)
  ✓ Productos (habilitado — plan Business)
  ✓ Vendedores (habilitado — plan Business)
  ✓ Inventario (habilitado — plan Business)
  ✗ Cockpit (no disponible en plan actual — [Actualizar plan])
  ...

Los módulos no disponibles en el plan muestran un botón de upgrade,
no un toggle deshabilitado. La UI debe ser aspiracional, no bloqueante.
```

### Settings: Empresa SAP

```
Campos informativos (no editables por el usuario):
  Nombre empresa SAP
  Código base
  Ambiente (Productivo / Sandbox)
  Versión SAP B1 detectada
  Última validación de conexión

Botón: "Contactar soporte para modificar" (estos datos los gestiona DataBision).
```

### Settings: Conexión y extractor

```
Campos:
  Modalidad: badge (A — Dedicado / B — Service Layer)
  URL Service Layer (si Modalidad B): Input + botón [Test de conexión]
  API Key del extractor: campo oculto con botón [Mostrar] y botón [Regenerar]
  Estado del extractor: chip de estado + link a Sync Center

Botón "Test de conexión" → 
  Loading + 
  Resultado: ✓ "Conexión exitosa — SAP B1 v10.0 · latencia 142ms" 
           o ✗ "Error de conexión: [mensaje]"
```

### Settings: Alertas

```
Lista de tipos de alerta con:
  Toggle para activar/desactivar
  Input de umbral (si aplica)
  Select de destinatarios (usuarios del portal)

Ejemplo:
  ✓ Cliente sin compra en [60] días → avisar a [Todos los usuarios ▾]
  ✓ Stock bajo mínimo → avisar a [Gerente de operaciones ▾]
  ✗ Venta bajo umbral diario de [$X] → (desactivado)
```

### Settings: Sincronización

```
Frecuencia de sync: Select (30min · 60min · 2h — según plan)
Objetos SAP activos: checkboxes de objetos disponibles según plan
Horario de sync: toggle "Pausar entre [22:00] y [06:00]" (para no saturar SAP en horario productivo)
Ventana de lookback: Select "Re-procesar registros de los últimos [7] días"
```

---

## 13. Estados de Pantalla Universales

Todos los módulos deben implementar estos estados de forma consistente.

### 13.1. Loading

**Comportamiento:** Skeleton loaders mantienen el tamaño exacto del componente que reemplaza. No hay colapso ni resize al cargar.

| Componente | Skeleton |
|---|---|
| KpiCard | Rectángulo 100% × 100px con shimmer. Respeta el padding. |
| KpiGrid (6 cards) | 6 esqueletos en el mismo grid que las cards reales. |
| Tabla (N filas) | N rectángulos de 44px de altura separados por 1px. |
| Gráfico ECharts | Rectángulo del tamaño del contenedor con shimmer. Sin ejes. |
| Feed (lista) | 5 ítems esqueleto con círculo izquierdo + 2 líneas. |
| Page completa | Solo en primer cargue (sin caché): spinner centrado pequeño. |

El shimmer es una animación CSS de gradiente de izquierda a derecha, color `#E2E8F0` → `#F8FAFC` → `#E2E8F0` en light mode, adaptado para dark mode.

### 13.2. Empty State

Variantes estandarizadas:

| Variante | SVG | Título | Descripción | CTA |
|---|---|---|---|---|
| `onboarding` | Config/setup | "Configurando tu plataforma" | Descripción del proceso de primera sync | "Ver estado de sincronización" |
| `no-results` | Search/empty | "Sin resultados" | "Los filtros aplicados no devuelven datos" | "Limpiar filtros" |
| `no-data` | Chart/empty | "Sin datos" | "No hay datos para el período seleccionado" | Cambiar período |
| `no-permission` | Lock | "Sin acceso" | "No tienes permiso para ver este módulo" | "Ver planes disponibles" |
| `error` | Alert/warning | "Error al cargar" | "No se pudo obtener los datos" | "Reintentar" |
| `stale` | Clock/warning | "Datos desactualizados" | "Los datos tienen más de Xh" | "Ver Sync Center" |

El SVG de cada variante usa colores del design system (muted, no brand-primary) para no competir con el contenido real.

### 13.3. Error State

Los errores de fetch son por componente, nunca por página completa. Una tabla que falla no afecta los KPIs encima de ella.

El componente de error tiene: ícono warning + mensaje corto + botón "Reintentar" que re-ejecuta el query de TanStack Query.

### 13.4. Stale Data

Cuando `dataUpdatedAt` de TanStack Query está a más de 30 minutos y la sincronización no es reciente:

- El componente con datos desactualizados muestra un chip sutil en su esquina: "Datos de hace Xh" con ícono de reloj.
- La plataforma muestra datos aunque sean viejos (es mejor que no mostrar nada).
- El DataFreshnessTag en el topbar cambia a amarillo/rojo según la antigüedad.
- Un banner no intrusivo aparece en el topbar, no en cada componente individual.

### 13.5. Syncing

Cuando el extractor está en ejecución activa:

- El DataFreshnessTag en el topbar muestra: "Sincronizando..." con spinner pequeño.
- Los datos existentes se siguen mostrando normalmente.
- No hay bloqueo de la UI durante la sincronización.
- Al completar la sync, los queries de TanStack Query se invalidan automáticamente y se muestran los datos nuevos.

### 13.6. No Data (módulo sin datos históricos)

Diferente de Empty State con filtros. El módulo nunca ha tenido datos (extractor recién configurado o módulo recién activado).

Muestra el EmptyState `onboarding` con estimación de tiempo hasta tener datos y link al Sync Center.

---

## 14. shadcn/ui — Componentes por Pantalla

### 14.1. Catálogo de componentes usados

| Componente | Descripción de uso en DataBision |
|---|---|
| `Card` | Contenedor base para KpiCards, widgets, ExtractorStatusCard. `className="shadow-sm"`. |
| `Table` + `TableHeader` + `TableRow` + `TableCell` | Tablas de datos. Row height: 44px via `h-11`. `hover:bg-muted/50` en filas. |
| `Tabs` + `TabsList` + `TabsTrigger` + `TabsContent` | Sub-navegación dentro de módulos. Variant: `"underline"` preferido. |
| `Select` + `SelectContent` + `SelectItem` | Filtros de selector único (almacén, tipo, etc.). |
| `DatePickerWithRange` (de la demo de shadcn) | Selector de rango de fechas en filtros y PeriodSelector custom. |
| `Badge` | Status de pedidos, segmentos de clientes, tipos de error. |
| `Alert` + `AlertDescription` | Mensajes de error/éxito en formularios y banners de estado. |
| `Sheet` + `SheetContent` | Paneles laterales: drilldown de vendedor/cliente, creación de acciones. 600px ancho en desktop, full en mobile. |
| `Dialog` + `DialogContent` | Modales de confirmación de acciones destructivas, exportación PDF, test de conexión. |
| `Skeleton` | Loading states de todos los componentes. |
| `Sonner` (Toast) | Notificaciones de confirmación de acciones. Posición: bottom-right. |
| `Progress` | Barra de progreso en onboarding y sincronización en curso. |
| `Separator` | Separadores visuales entre secciones. |
| `Avatar` | Foto/iniciales de usuario en topbar y perfiles de vendedor/cliente. |
| `DropdownMenu` | Menú de usuario en topbar, acciones contextual `···` en cards. |
| `Tooltip` | Tooltips en íconos de la sidebar colapsada, en DataFreshnessTag. |
| `Switch` / `Toggle` | Settings de configuración (módulos, alertas, sync). |
| `Input` + `Label` | Formularios de settings y login. |
| `Button` | Acciones primarias y secundarias. Variants: `default`, `outline`, `ghost`, `destructive`. |
| `Command` | Búsqueda/filtro dentro de selects complejos (búsqueda de cliente o ítem). |
| `Collapsible` | Stack traces en Sync Center. Detalle de facturas en AR Aging. |
| `HoverCard` | Preview de datos al hover sobre un punto del ScatterChart. |
| `Popover` | Filtros complejos en mobile (FAB → Popover con filtros). |
| `ScrollArea` | Sidebar y listas largas con scroll controlado. |

### 14.2. Convenciones de uso

**Card padding:** siempre `p-5` (20px). No `p-6` — es demasiado generoso para un dashboard denso.

**Button sizes:** `size="sm"` para acciones secundarias dentro de tablas. `size="default"` para acciones primarias. `size="lg"` solo en formularios de login.

**Badge variants:** `variant="default"` para estados positivos. `variant="destructive"` para errores. `variant="secondary"` para estados neutros. `variant="outline"` para etiquetas informativas.

**Table:** nunca borders internos visibles (estilo `table-fixed` + separadores solo horizontales via `border-b`). La densidad se logra con row height controlado, no con bordes.

---

## 15. ECharts — Gráficos por Pantalla

### 15.1. Asignación por módulo

| Módulo / Pantalla | Gráfico | Tipo ECharts | Justificación |
|---|---|---|---|
| Home · Dashboard | Ventas del período | `line` + `areaStyle` | Evolución temporal continua |
| Home | Sparkline KPIs | `line` (mini, sin ejes) | Tendencia rápida en poco espacio |
| Dashboard | Ventas por día | `bar` o `line` según granularidad | Comparación discreta (barras) o continua (línea) |
| Dashboard · Productos | Ventas por categoría | `pie` con `radius: ['45%','70%']` (donut) | Composición de un todo |
| Ventas | Línea ventas período | `line` + `areaStyle` | Evolución con área suave |
| Ventas | Categoría de producto | `pie` donut | Composición |
| Ventas | Ventas agrupadas (períodos) | `bar` agrupado | Comparación entre grupos |
| Ventas | Patrón por hora/día | `heatmap` (calendar no — grid) | Identificar patrones temporales |
| Clientes | Segmentación | `scatter` | Relación frecuencia × monto |
| Clientes | AR Aging | `bar` horizontal apilado | Composición de un total por ítem |
| Productos | Márgenes | `bar` horizontal | Ranking de valores continuos |
| Vendedores | Ranking | `bar` horizontal | Ranking de valores continuos |
| Vendedores | Comparación mensual | `bar` agrupado | Evolución por persona y mes |
| Vendedores | Actividad heatmap | `heatmap` | Actividad por período |
| Vendedores | Cumplimiento meta | `gauge` (semicircular) | Solo si N ≤ 6 vendedores. Único uso de gauge justificado. |
| Inventario | Valor por categoría | `pie` donut | Composición de inventario |
| Inventario | Entradas vs salidas | `bar` apilado | Flujo neto de stock |
| Inventario | Almacenes × categorías | `heatmap` | Densidad bidimensional |
| Dashboard | Márgenes por categoría | `bar` horizontal | Comparación de ratios |
| Live Layer | Actividad en tiempo real | `bar` + `line` combinado | Series temporales recientes |

### 15.2. Configuración universal de ECharts

Todos los gráficos comparten estas configuraciones base:

```
backgroundColor: 'transparent'
grid: { containLabel: true, left: 16, right: 16, top: 8, bottom: 8 }
animation: true, animationDuration: 300
tooltip: { trigger: 'axis' | 'item', confine: true }
```

**Tooltip:** siempre en español. Formato: `{a}: {b} — {c}` adaptado al tipo de gráfico. Fecha con `date-fns` formateada en locale es-CL.

**Responsive:** `style={{ width: '100%', height: 'Npx' }}` en el contenedor. ECharts `autoResize` activo.

**Menú contextual (··· en esquina):** todos los gráficos tienen un `DropdownMenu` que aparece al hover sobre el gráfico, con opciones: Exportar PNG · Exportar CSV · Ver como tabla.

### 15.3. Gauge — reglas de uso

El gauge se usa **solo** para mostrar el cumplimiento de una meta explícita contra un target definido por el usuario (ej. metas de vendedor). No se usa para:
- Mostrar un porcentaje genérico (usar KpiCard con número y delta).
- Mostrar un rango de valores sin meta definida.
- Decorar una pantalla.

Si no hay metas configuradas, el Tab de Cumplimiento no muestra gauges — muestra barras de progreso.

---

## 16. Reglas de Experiencia

Veinte reglas que definen qué puede y qué no puede hacer la interfaz de DataBision.

### Reglas de claridad ejecutiva

1. **Cada pantalla responde una pregunta.** El Page Header enuncia implícitamente la pregunta. El contenido la responde. Si el contenido no responde la pregunta, el componente no pertenece a esa pantalla.

2. **Nunca más de 2 gráficos del mismo tipo en la misma vista.** Si hay necesidad de 3 line charts, uno de ellos debe ser un heatmap o un bar chart que aporte perspectiva diferente.

3. **Los números grandes tienen precedencia sobre los gráficos.** Un gerente en 3 segundos debe poder leer el número clave sin interpretar un gráfico.

4. **El tiempo relativo complementa pero no reemplaza el tiempo absoluto.** "hace 45 min" siempre tiene un tooltip con "12:15:30 del 03/06/2026".

5. **La frescura de los datos siempre es visible.** El DataFreshnessTag está en el topbar en todo momento. No hay pantalla sin él.

### Reglas de densidad y no-saturación

6. **Máximo 6 KPIs por KPI Strip.** Si se necesitan más, usar dos filas o mover KPIs menos importantes a una sección secundaria.

7. **Máximo 10 ítems en cualquier "top N" sin paginación.** Una tabla de "Top 10 clientes" sin paginación es aceptable. Una de "Top 50" requiere paginación.

8. **Los gráficos tienen altura fija determinada por su posición, no por la cantidad de datos.** Una barra horizontal de ranking puede tener scroll vertical, pero el contenedor del gráfico tiene altura máxima.

9. **No usar animaciones de entrada complejas en datos de negocio.** `animationDuration: 300` en ECharts. Sin slides, bounces, ni rotaciones.

10. **No usar colores de branding en elementos de estado.** El verde de éxito, rojo de error y amarillo de warning son del design system, no del brand-primary del cliente.

### Reglas de drill-down y navegación

11. **Todo número accionable tiene un destino de drill-down.** Si el usuario no puede hacer nada con ese número haciendo click, no debería ser un link.

12. **El breadcrumb siempre refleja el camino completo.** "Ventas › Por Vendedor › Juan Pérez" — cada nivel es clickeable.

13. **Los filtros activos siempre están visibles en la FilterBar.** El usuario nunca debe preguntarse "por qué solo veo parte de los datos".

14. **Al navegar dentro de un módulo, el período global se mantiene.** Cambiar de tab dentro de Ventas no resetea el período.

### Reglas de acción y decisión

15. **Los insights siempre tienen un CTA accionable.** Un insight sin botón de acción es ruido. Todo insight debe llevar a Business Actions o a un módulo relevante.

16. **Las alertas tienen acciones, no solo información.** Resolver · Silenciar · Crear acción son acciones reales — no solo "marcar como visto".

17. **Los estados vacíos son oportunidades de guía, no pantallas de error.** "No hay datos para el período" debe sugerir cambiar el período o sincronizar.

### Reglas de exportación y compartir

18. **Los filtros activos están en la URL.** Una vista filtrada puede ser compartida por URL. TanStack Query re-hidrata el estado desde el query string.

19. **La exportación siempre incluye metadata.** Un CSV exportado tiene la fila de cabecera con los filtros aplicados y el timestamp de exportación.

20. **El PDF del Dashboard incluye el logo del cliente.** No el logo de DataBision. Es el informe de la empresa, no del proveedor.

---

## 17. Priorización MVP / Fase 2 / Fase 3

### MVP — Plan Starter (implementar primero)

**Objetivo:** plataforma funcional mínima vendible a precio de entrada.

| Módulo / Feature | Prioridad | Dependencias |
|---|---|---|
| Login por tenant | P0 | BrandingLoader, JWT auth |
| Home ejecutivo (simplificado) | P0 | KPI Ventas, DataFreshnessTag |
| Ventas — Overview básico | P0 | mart.fact_sales |
| Ventas — Por período | P0 | mart.fact_sales |
| Sync Center (lectura) | P0 | ctl.sync_runs |
| Alertas críticas (solo extractor detenido) | P0 | ctl.sync_errors |
| Settings — Usuarios (básico) | P0 | Auth + roles |
| Settings — Branding | P0 | BrandingLoader |
| Dark mode | P1 | CSS tokens |
| Responsive desktop + mobile básico | P1 | Layout |

**Resultado:** cliente puede ver sus ventas, saber que los datos están actualizados, y gestionar usuarios básicos.

### Fase 2 — Plan Business (añadir tras MVP validado)

| Módulo / Feature | Prioridad |
|---|---|
| Dashboard ejecutivo completo | P0 |
| Ventas — Por Vendedor + Por Cliente | P0 |
| Módulo Clientes completo (Overview + AR Aging + Riesgo) | P0 |
| Módulo Productos completo | P0 |
| Módulo Vendedores completo | P0 |
| Módulo Inventario completo | P0 |
| Selector de empresa (multi-tenant) | P0 |
| Alert Center completo (todas las categorías) | P1 |
| Operational Cockpit | P1 |
| Exportación CSV en tablas | P1 |
| Exportación PDF en Dashboard | P2 |
| Settings — Alertas configurables | P2 |

**Resultado:** plataforma completa de analítica SAP B1. Justifica el precio de USD 600/mes.

### Fase 3 — Plan Advanced (diferenciación competitiva)

| Módulo / Feature | Prioridad | Complejidad |
|---|---|---|
| Operational Live Layer | P0 | Alta (polling agresivo o SSE) |
| Recommendations & Insights | P0 | Alta (motor de reglas) |
| Business Actions | P0 | Media |
| Cumplimiento de metas (vendedores) | P1 | Media |
| Notificaciones push mobile | P1 | Alta |
| Exportación PDF automatizada (email) | P2 | Media |
| Power BI como add-on (embed opcional) | P3 | Alta |

**Resultado:** plataforma diferenciada que justifica USD 1.000+/mes. Crea dependencia operacional.

---

## 18. Riesgos UX

| ID | Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|---|
| R01 | **Datos stale sin advertencia** — usuario toma decisiones con datos de 8+ horas sin saberlo | Alta | Alto | DataFreshnessTag prominente. Banner cuando > 6h. Badge en topbar. |
| R02 | **Sobrecarga de alertas** — el Alert Center con 50+ alertas activas se vuelve inútil | Media | Alto | Severidades claras. Silenciar. Paginación. Límite de alertas sin resolver (warn al admin). |
| R03 | **Branding roto en primer acceso** — flash de colores genéricos antes de cargar branding del tenant | Alta | Medio | Cache en localStorage. BrandingLoader bloquea render hasta tener config. |
| R04 | **Tablas ilegibles en mobile** — columnas no caben en pantalla pequeña | Alta | Alto | Columnas con prioridad. Modo colapso de fila. Scroll horizontal controlado. |
| R05 | **ECharts inaccesible** — gráficos sin alternativa para usuarios con discapacidad visual | Media | Medio | aria-label en contenedor. "Ver como tabla" en menú de cada gráfico. |
| R06 | **Drill-down confuso** — usuario pierde contexto al navegar | Media | Medio | Breadcrumb siempre visible. Filtros en URL. Botón atrás funcional. |
| R07 | **Período global vs local ambiguo** — usuario no sabe qué período aplica a cada componente | Alta | Alto | Chip "Período personalizado" cuando difiere del global. Filtros siempre visibles en FilterBar. |
| R08 | **Settings dañinos** — admin borra usuarios o deshabilita módulos sin confirmación | Media | Alto | Dialog de confirmación en acciones destructivas con descripción del impacto. |
| R09 | **Extractor detenido sin notificación** — datos dejan de actualizarse y el usuario no se entera por días | Alta | Alto | Alerta crítica automática. DataFreshnessTag rojo. Banner en Home. |
| R10 | **Gauge decorativo** — uso de gauges sin meta definida crea falsa sensación de objetivo | Media | Bajo | Regla explícita: gauge solo con meta configurada. Ver Regla #15 de ECharts. |
| R11 | **Módulos de plan superior visibles pero bloqueados** — experiencia frustrante tipo "lock con candado" | Media | Medio | Módulos no contratados simplemente no aparecen en la sidebar. |
| R12 | **Performance con datasets grandes** — tablas de 10.000+ filas, gráficos de 365 puntos | Media | Alto | Paginación. Server-side filtering. ECharts maneja bien 1.000+ puntos con canvas. |

---

## 19. Recomendación de Implementación

### 19.1. Secuencia recomendada

**Sprint 1 — Fundación (2 semanas):**
1. PortalLayout (Topbar + Sidebar + Content Area) con dark mode y responsive básico.
2. BrandingLoader: carga de tenant config, CSS custom properties, localStorage cache.
3. Login page con estados completos.
4. DataFreshnessTag conectado a Sync Center API.
5. uiStore, tenantStore, authStore.

**Sprint 2 — Home y Ventas básico (2 semanas):**
1. Home ejecutivo: KPI Strip + RevenueLineChart + estado operacional compacto.
2. Ventas Overview: KPI Strip + gráfico + tabla top ítems.
3. Ventas Por Período: BarChart agrupado + tabla.
4. FilterBar y PeriodSelector como componentes compartidos.
5. EmptyState y LoadingSkeleton estandarizados.

**Sprint 3 — Módulos analíticos (3 semanas):**
1. Módulo Clientes: Overview + AR Aging + perfil básico.
2. Módulo Productos: Overview + Márgenes.
3. Módulo Vendedores: Overview/Ranking.
4. Módulo Inventario: Overview + Stock Crítico.

**Sprint 4 — Operacional (2 semanas):**
1. Sync Center completo.
2. Alert Center con acciones.
3. Dashboard ejecutivo.
4. Cockpit.

**Sprint 5 — Pulido y Advanced (2 semanas):**
1. Selector de empresa (multi-tenant).
2. Settings completo.
3. Exportación CSV.
4. Mobile optimization: bottom nav, pull-to-refresh, gestos.

### 19.2. Componentes a construir primero (por impacto / reutilización)

1. `KpiCard` — aparece en todas las pantallas.
2. `DataFreshnessTag` — aparece en el topbar permanente.
3. `EmptyState` — aparece en todas las pantallas.
4. `LoadingSkeleton` — aparece en todas las pantallas.
5. `FilterBar` + `PeriodSelector` — aparece en todos los módulos.
6. `RevenueLineChart` (ECharts wrapper base) — patrón para todos los otros gráficos.
7. `StatusBadge` — aparece en tablas de todos los módulos.

### 19.3. Decisiones de UX a confirmar antes de implementar

- **Período default:** ¿MTD o YTD? MTD es más relevante para uso diario. YTD para reuniones de dirección. Recomendación: **MTD default**, con YTD accesible con un click.
- **Módulos no contratados:** ¿ocultos en sidebar o visibles con CTA de upgrade? Recomendación: **ocultos** — la experiencia de usuario no debe ser una presentación de ventas.
- **URL de drill-down:** ¿Sheet o navegación completa para el perfil de cliente/vendedor? Recomendación: **Sheet en desktop** (mantiene contexto), **página completa en mobile** (Sheet ocupa todo el viewport igualmente).
- **Idioma del portal:** ¿español de Chile, neutro o configurable? Recomendación: **español neutro** con opción de locale por tenant en Fase 2 (formatos de número/fecha).
- **Logo en PDF exportado:** ¿logo del cliente o logo DataBision? Recomendación: **logo del cliente, sin logo DataBision visible** — el cliente es el protagonista.

---

*Documento de especificación de pantallas — versión 1.0 — 2026-06-03*  
*Relacionado: `frontend-ux-architecture.md` · `dashboard-information-architecture.md`*
