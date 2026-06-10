# DataBision — Frontend UX Architecture

**Versión:** 3.0  
**Fecha:** 2026-06-03  
**Autor:** Lead UX Architect  
**Estado:** Documento de diseño — sin implementación  
**Stack:** React · TypeScript · shadcn/ui · Apache ECharts · TanStack Query · Zustand · Tailwind CSS  
**Relacionado:** `dashboard-information-architecture.md` · `native-bi-screen-specs.md`

> **Dominio canónico:** `{slug}.databision.com` (no `.app`). Todas las URLs de ejemplo en este documento usan `.app` como placeholder — leer como `.com`.

---

## Índice

1. [Filosofía de Diseño](#1-filosofía-de-diseño)
2. [Roles de Usuario](#2-roles-de-usuario)
3. [Arquitectura de Navegación](#3-arquitectura-de-navegación)
4. [Sistema de Layout](#4-sistema-de-layout)
5. [Módulo 01 — Login por Tenant](#5-módulo-01--login-por-tenant)
6. [Módulo 01b — Selector de Empresa](#6-módulo-01b--selector-de-empresa)
7. [Módulo 02 — Home Ejecutivo](#7-módulo-02--home-ejecutivo)
8. [Módulo 03 — Dashboard Ejecutivo](#8-módulo-03--dashboard-ejecutivo)
9. [Módulo 04 — Native BI](#9-módulo-04--native-bi)
10. [Módulo 05 — Ventas](#10-módulo-05--ventas)
11. [Módulo 06 — Clientes](#11-módulo-06--clientes)
12. [Módulo 07 — Productos](#12-módulo-07--productos)
13. [Módulo 08 — Vendedores](#13-módulo-08--vendedores)
14. [Módulo 09 — Inventario](#14-módulo-09--inventario)
15. [Módulo 10 — Sync Center](#15-módulo-10--sync-center)
16. [Módulo 11 — Operational Cockpit](#16-módulo-11--operational-cockpit)
17. [Módulo 12 — Operational Live Layer](#17-módulo-12--operational-live-layer)
18. [Módulo 13 — Alert Center](#18-módulo-13--alert-center)
19. [Módulo 14 — Recommendations & Insights](#19-módulo-14--recommendations--insights)
20. [Módulo 15 — Business Actions](#20-módulo-15--business-actions)
21. [Responsive Design](#21-responsive-design)
22. [Mobile para Gerencia](#22-mobile-para-gerencia)
23. [Dark Mode](#23-dark-mode)
24. [Branding por Cliente](#24-branding-por-cliente)
25. [Design System y Componentes Transversales](#25-design-system-y-componentes-transversales)
26. [Patrones de Interacción](#26-patrones-de-interacción)
27. [Estructura de Archivos Frontend](#27-estructura-de-archivos-frontend)
28. [Glosario SAP B1 → UI](#28-glosario-sap-b1--ui)

---

## 1. Filosofía de Diseño

DataBision no es un viewer de reportes ni un BI genérico. Es una **plataforma de inteligencia operacional** construida para empresas que viven dentro de SAP Business One. Cada decisión de diseño parte de esa realidad.

### 1.1. Principios fundacionales

**El dato es el protagonista, no el gráfico.**
La plataforma no impresiona con visualizaciones. Informa con claridad. Un número correcto con buena tipografía vale más que un gráfico 3D que no se entiende. Los charts existen para revelar patrones, no para decorar.

**La frescura es parte de la información.**
Un dato sin fecha es un dato sospechoso. En toda pantalla, el usuario sabe en todo momento cuándo fue la última sincronización con SAP. Cuando los datos tienen más de 6 horas, la plataforma lo advierte. La confianza en el dato es tan importante como el dato mismo.

**La densidad es una feature.**
El usuario tipo no es un consumidor casual. Es un gerente o analista que usa la plataforma varias veces al día para tomar decisiones reales. Las pantallas son densas porque el contexto importa. No se sacrifica información útil por estética minimalista.

**Acción sobre observación.**
Un insight sin acción es ruido. La plataforma no solo muestra qué pasó — sugiere qué hacer, permite crear acciones, y hace seguimiento del resultado. El loop dato → insight → acción → resultado es el core del producto.

**La identidad es del cliente.**
El portal se siente como el portal de la empresa, no como un SaaS de terceros. El white-label es real: colores, logo, favicon, tipografía del sidebar, dominio propio. El cliente no debería necesitar explicar a sus gerentes que están usando una herramienta externa.

**Mobile primero para gerencia.**
Los gerentes consultan el teléfono. Los analistas usan desktop. El diseño mobile no es un "también funciona" — es la experiencia principal para el rol más importante del cliente.

### 1.2. Lo que DataBision no es

- No es Power BI: no hay archivos `.pbix`, no hay workspaces de Microsoft, no hay Service Principal.
- No es un dashboard builder donde el usuario arrastra widgets.
- No es un reporte de Crystal Reports moderno.
- No es una herramienta self-service de exploración libre de datos.

DataBision es una plataforma **opinionada**: sabe qué métricas importan en SAP B1 y las presenta de la mejor forma posible, configurada por el equipo DataBision para cada cliente.

---

## 2. Roles de Usuario

| Rol | Descripción | Módulos disponibles | Dispositivo principal |
|---|---|---|---|
| **Gerente General** | Visión de alto nivel, toma decisiones estratégicas | Home, Dashboard, Cockpit, Live Layer, Alertas, Insights | Mobile / Tablet |
| **Gerente Comercial** | Seguimiento de ventas, clientes y equipo | Ventas, Clientes, Productos, Dashboard, Actions | Mobile / Desktop |
| **Gerente de Operaciones** | Estado logístico, stock, entregas, sync | Cockpit, Inventario, Sync Center, Live Layer | Desktop / Mobile |
| **Analista de BI** | Análisis profundo, todos los módulos | Todos | Desktop |
| **Administrador de empresa** | Configuración del portal, usuarios, branding | Settings + todos los analíticos | Desktop |
| **SuperAdmin DataBision** | Gestión de tenants, onboarding, infraestructura | admin.databision.app (separado) | Desktop |

Los módulos operacionales (Cockpit, Live Layer, Alertas) son transversales a todos los roles activos. Los módulos analíticos (Ventas, Clientes, Productos, Inventario) se activan por módulos contratados en el plan comercial.

---

## 3. Arquitectura de Navegación

### 3.1. URLs del portal cliente

```
{slug}.databision.app
│
├── /login                      → Login por tenant
├── /select-company             → Selector de empresa (solo multi-empresa)
├── /                           → Home Ejecutivo
├── /dashboard                  → Dashboard Ejecutivo
│
├── /cockpit                    → Operational Cockpit
├── /live                       → Operational Live Layer
├── /alerts                     → Alert Center
│   └── /alerts/settings        → Umbrales de alerta
├── /insights                   → Recommendations & Insights
├── /actions                    → Business Actions
│
├── /sales                      → Ventas
│   ├── /sales/overview
│   ├── /sales/by-period
│   ├── /sales/by-salesperson
│   ├── /sales/by-customer
│   └── /sales/documents
│
├── /customers                  → Clientes
│   ├── /customers/overview
│   ├── /customers/ar-aging
│   ├── /customers/at-risk
│   └── /customers/:id
│
├── /products                   → Productos
│   ├── /products/overview
│   ├── /products/margins
│   ├── /products/slow-moving
│   └── /products/:id
│
├── /salesreps                  → Vendedores
│   ├── /salesreps/overview
│   ├── /salesreps/performance
│   ├── /salesreps/comparison
│   └── /salesreps/:id
│
├── /inventory                  → Inventario
│   ├── /inventory/overview
│   ├── /inventory/by-warehouse
│   ├── /inventory/critical
│   └── /inventory/movements
│
├── /sync                       → Sync Center
│
└── /settings                   → Configuración
    ├── /settings/users
    ├── /settings/roles
    ├── /settings/branding
    ├── /settings/modules
    ├── /settings/company
    ├── /settings/connection
    ├── /settings/alerts
    └── /settings/sync
```

### 3.2. Estructura de la sidebar

La sidebar agrupa los módulos en cuatro categorías semánticas. Los módulos que no están contratados en el plan del cliente **no aparecen** — no se muestran bloqueados ni con candado.

```
SIDEBAR GROUPS

  ── EJECUTIVO ──────────────────
  🏠  Home
  📊  Dashboard

  ── OPERACIONAL ────────────────
  🎛️  Cockpit               [badge alertas]
  ⚡  Live Layer            [badge live]
  🔔  Alert Center          [badge count]
  💡  Insights
  ✅  Actions               [badge pendientes]

  ── ANALÍTICA ──────────────────
  📈  Ventas
  👥  Clientes
  📦  Productos
  👤  Vendedores
  🏭  Inventario

  ── SISTEMA ────────────────────
  🔄  Sync Center           [badge errores]
  ⚙️  Settings
```

Los badges numéricos en los ítems de la sidebar muestran el conteo de ítems activos que requieren atención. Son rojos si hay ítems críticos, amarillos si hay advertencias.

### 3.3. Jerarquía de navegación

**Nivel 1 — Sidebar:** navegación entre módulos primarios.  
**Nivel 2 — Tabs o sub-nav:** vistas dentro de un módulo (Overview, Detalle, etc.).  
**Nivel 3 — Drilldown:** detalle de un ítem específico (cliente, producto, pedido).

El breadcrumb en el topbar refleja siempre la profundidad actual: `Ventas > Por Vendedor > Juan Pérez`.

---

## 4. Sistema de Layout

### 4.1. PortalLayout

```
┌────────────────────────────────────────────────────────────────┐
│ TOPBAR (56px)                                                  │
│ [Logo] [Módulo activo]  [DataFreshnessTag]  [Alerts] [User ▾] │
├─────────────────┬──────────────────────────────────────────────┤
│                 │  PAGE HEADER                                 │
│  SIDEBAR        │  [Título]  [Período]  [Filtros globales]     │
│  240px          ├──────────────────────────────────────────────┤
│                 │                                              │
│  Nav groups     │           CONTENT AREA                      │
│  con badges     │                                              │
│                 │                                              │
│  [collapse ◄]   │                                             │
└─────────────────┴──────────────────────────────────────────────┘
```

### 4.2. Topbar

**Izquierda:** Logo del tenant (max 140×32px, `object-fit: contain`) + separador vertical + nombre del módulo activo en text-sm/600.

**Centro:** `DataFreshnessTag` — chip con ícono de reloj y timestamp de última sincronización exitosa. Colores: verde < 2h · amarillo 2–6h · rojo > 6h · gris con error si hay fallo activo. Click navega a Sync Center.

**Derecha:** 
- Ícono de campana con badge numérico rojo (alertas activas críticas).
- Ícono de rayo con indicador pulsante verde/rojo (estado Live Layer).
- Avatar del usuario con dropdown: Nombre · Empresa · Perfil · Toggle dark mode · Cerrar sesión.

### 4.3. Sidebar

- Fondo: `var(--brand-sidebar)` (default `#0F172A`).
- Ítem activo: fondo `#1E293B` + borde izquierdo 3px `var(--brand-primary)`.
- Ícono: 20px Lucide React, color blanco/70 inactivo, blanco activo.
- Label: 14px/500, color blanco/70 inactivo, blanco activo.
- Badge: círculo 18px, fondo rojo `#DC2626`, texto blanco 11px/700.
- Separadores de grupo: label 10px/600 uppercase blanco/40.
- Colapsar: botón `◄` al pie de la sidebar. En estado colapsado (64px) muestra solo íconos con tooltip al hover.

### 4.4. Page Header

Banda de 64px debajo del topbar, fondo `#FFFFFF`, borde inferior `#E2E8F0`.

- Izquierda: título del módulo/vista (18px/700) + breadcrumb opcional (13px, muted).
- Derecha: controles contextuales — selector de período, botones de acción primaria del módulo, menú de exportación.

El `PeriodSelector` es un componente de botones segmentados: `HOY` · `MTD` · `QTD` · `YTD` · `12M` · `Custom`. Al seleccionar, el período se guarda en la URL (`?period=mtd`) y en `uiStore`. Todos los componentes de datos de la página son reactivos al período global.

### 4.5. Content Area

- Padding: `24px` desktop · `16px` tablet · `12px` mobile.
- Max-width: `1440px`, centrado.
- Fondo: `#F8FAFC` (light) · `#0F172A` (dark).
- Scroll: solo vertical. Nunca scroll horizontal en el layout principal.

---

## 5. Módulo 01 — Login por Tenant

**URL:** `{slug}.databision.app/login`

### 5.1. Propósito

Autenticar al usuario dentro del contexto de identidad de su empresa. El login es la primera impresión del producto — debe transmitir profesionalismo y pertenencia a la marca del cliente.

### 5.2. Carga del tenant antes del formulario

El componente `BrandingLoader` ejecuta `GET /api/tenant/config` antes de renderizar el formulario. Este endpoint es público y retorna los colores, logo y nombre del tenant. Mientras carga: spinner centrado con el favicon genérico DataBision. Si el tenant no existe o retorna 404: página de error "Portal no encontrado" con soporte de contacto. Si hay error de red: mensaje "No se pudo cargar el portal. Verifica tu conexión."

Los valores del tenant config se guardan en `localStorage` para que la próxima visita cargue instantáneamente sin flash de colores.

### 5.3. Layout

**Desktop / Tablet landscape:**
- Panel izquierdo 60%: fondo `var(--brand-primary)` sólido. Logo del tenant centrado (máx 200×80px). Tagline configurable debajo en blanco/80. Ninguna imagen de fondo — el color es la identidad.
- Panel derecho 40%: formulario de login.

**Mobile / Tablet portrait:**
- Pantalla completa. Logo del tenant centrado en la parte superior con fondo `var(--brand-primary)` (solo la mitad superior). Formulario en la mitad inferior sobre fondo blanco/superficie.

### 5.4. Formulario

- Título: "Bienvenido a [Nombre empresa]" — 24px/700.
- Subtítulo: "Ingresa tus credenciales para continuar" — 14px, muted.
- Campo Email: `input[type=email]`, label "Correo electrónico", autofocus en desktop.
- Campo Contraseña: `input[type=password]`, label "Contraseña", botón toggle show/hide (ícono ojo).
- Botón "Ingresar": ancho 100%, color `var(--brand-primary)`, 44px altura.
- Link "¿Olvidaste tu contraseña?" — 13px, color `var(--brand-primary)`, debajo del botón.
- Separador + logo DataBision en gris/40 al pie. Texto "Powered by DataBision" — 11px.

### 5.5. Estados del formulario

| Estado | Comportamiento |
|---|---|
| Loading | Botón muestra spinner (reemplaza texto), se deshabilita. Campos no se bloquean. |
| Error 401 | Banner rojo inline: "Email o contraseña incorrectos." Campo email no se limpia. |
| Error de red | Banner amarillo: "No se pudo conectar. Verifica tu conexión." |
| Primer login | Redirección automática a `/change-password` con token temporal. |
| 2FA (futuro) | Pantalla de código TOTP en paso 2 sin volver al formulario principal. |

### 5.6. Post-login

Redirección al `returnUrl` si está en query string. Si no, redirige a `/` (Home). El JWT y el refresh token httpOnly se gestionan de forma transparente — el usuario nunca los ve.

---

## 6. Módulo 01b — Selector de Empresa

**URL:** `{slug}.databision.app/select-company`

### 6.1. Propósito

Permite al usuario con acceso a más de una empresa SAP B1 elegir con cuál trabajar en la sesión actual. Aplica principalmente a holdings o consultores con acceso multi-cliente. Si el usuario tiene acceso a una sola empresa, esta pantalla se salta automáticamente.

### 6.2. Cuándo aparece

- Post-login: si el JWT devuelve `companies.length > 1`.
- Al hacer click en "Cambiar empresa" en el menú del topbar.
- No aparece en el 80% de los clientes (empresas con una sola base SAP).

### 6.3. Layout

```
┌────────────────────────────────────────────────────────────────┐
│  [Logo DataBision pequeño centrado]                            │
│                                                                │
│  Selecciona la empresa a consultar                             │
│  Sesión activa como: nombre@empresa.com                        │
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  [Logo]  Comercial Torres S.A.                           │  │
│  │          Base: Torres_PRD  ·  ● Productivo               │  │
│  │          Último acceso: hace 2 días              [chip]  │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  [Logo]  Holding Torres Norte                            │  │
│  │          Base: Torres_HLD  ·  ● Productivo               │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  [Logo]  Torres Demo                                     │  │
│  │          Base: Torres_SBX  ·  ○ Sandbox                  │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### 6.4. Comportamiento

- Click en card → aplica branding del tenant → redirige a `/`.
- La última empresa seleccionada tiene chip "Último acceso" y aparece primera en la lista.
- Si hay > 5 empresas: campo de búsqueda sobre las cards (para consultores con múltiples clientes).
- Badge de ambiente: `● Productivo` verde · `○ Sandbox` amarillo · `▲ Staging` naranja.
- El ambiente sandbox tiene un banner amarillo persistente en el topbar durante la sesión: "Ambiente de pruebas — los datos no son reales."

---

## 7. Módulo 02 — Home Ejecutivo

**URL:** `{slug}.databision.app/`

### 6.1. Propósito

**La pregunta que responde:** "¿Cómo está el negocio hoy?"

El Home es la pantalla de aterrizaje post-login. Diseñada para ser completamente informativa sin ninguna interacción. En 10 segundos, el gerente tiene el pulso completo del negocio. No es configurable — es opinionado sobre qué métricas importan.

### 6.2. Layout de la página

```
[Selector de período global]

[KPI Strip — 6 tarjetas métricas principales]

┌─────────────────────────────┬───────────────────────────────┐
│  Ventas del período         │  Estado operacional           │
│  Gráfico línea/área ECharts │  (resumen Cockpit compacto)   │
│  60% del ancho              │  40% del ancho                │
└─────────────────────────────┴───────────────────────────────┘

┌─────────────────┬─────────────────┬─────────────────────────┐
│  Top 5 Clientes │  Alertas activas│  Sync Status            │
│  Tabla compacta │  Feed           │  Chip de estado         │
│  33%            │  33%            │  33%                    │
└─────────────────┴─────────────────┴─────────────────────────┘
```

### 6.3. KPI Strip

Seis `KpiCard` en fila horizontal scrollable en mobile, grid 6 columnas en desktop.

**KPIs del Home:**

| # | Métrica | Fuente SAP | Dirección positiva |
|---|---|---|---|
| 1 | Ventas del período | OINV DocTotal | ↑ |
| 2 | Margen bruto % | (OINV − costo) / OINV | ↑ |
| 3 | Pedidos pendientes | ORDR Abiertos | ↓ (menos = mejor) |
| 4 | CxC vencida | OINV sin pagar >30d | ↓ |
| 5 | Rotación inventario | Días promedio | ↓ |
| 6 | Clientes activos período | OCRD con OINV | ↑ |

Cada `KpiCard` tiene: ícono de categoría · número principal (32px/700, tabular-nums) · etiqueta (12px/400, muted) · delta vs período anterior (flecha + %) · sparkline de 12 períodos (60×24px ECharts).

La `deltaSemantics` de cada KPI es explícita: para pedidos pendientes y CxC vencida, un número menor es positivo (flecha verde hacia abajo).

### 6.4. Gráfico Ventas del período

`RevenueLineChart` ECharts con dos series superpuestas:
- Serie actual: línea sólida color `var(--brand-primary)` con `areaStyle` suave (gradiente de 30% a 0% de opacidad).
- Serie período anterior: línea punteada color muted/50.

Tooltip sincronizado: al hover muestra fecha, valor actual, valor comparación, delta % con colores semánticos.

Altura 220px. Sin leyenda — el tooltip explica todo. Fondo transparente. Sin bordes de gráfico. Ejes: solo línea base X, sin grillas verticales.

### 6.5. Estado operacional (resumen Cockpit)

Panel compacto con cuatro contadores de estado del Cockpit:

```
  ┌──────────────────────────────┐
  │  ● 23   Pedidos pendientes   │  → amarillo si >0
  │  ● 7    Entregas retrasadas  │  → rojo si >0
  │  ● 3    Stock crítico        │  → rojo si >0
  │  ● 0    Errores integración  │  → verde ✓
  └──────────────────────────────┘
  [Ver Cockpit completo →]
```

Click en cualquier contador navega al Cockpit filtrado por esa categoría.

### 6.6. Feed de alertas activas

Las 5 alertas más recientes con: banda de color izquierda por severidad · título corto · tiempo relativo ("hace 2h"). Si hay 0 alertas activas: estado vacío positivo — checkmark verde + "Sin alertas activas".

Link "Ver todas →" al Alert Center.

### 6.7. Estado vacío (primer uso)

Si el extractor nunca ha corrido exitosamente, toda la página muestra un estado de onboarding:
- SVG de configuración inicial (sin stock art genérico — ícono temático DataBision).
- "Configurando tu plataforma" en 20px/600.
- "Estamos sincronizando los datos de tu SAP B1 por primera vez. El proceso toma entre 15 y 60 minutos."
- Barra de progreso con el último status del extractor (tomado del Sync Center).
- Link "Ver estado de sincronización →".

---

## 7. Módulo 03 — Dashboard Ejecutivo

**URL:** `{slug}.databision.app/dashboard`

### 7.1. Propósito y diferencia con Home

**La pregunta que responde:** "¿Cuál es la situación comparada con el plan y la historia?"

El Home es el snapshot diario sin contexto. El Dashboard Ejecutivo es la vista analítica profunda para reuniones de dirección, revisiones mensuales y análisis de tendencias. Incluye más contexto temporal, comparaciones vs plan, y análisis multi-dimensional.

### 7.2. Layout de la página

```
[Controles: Período | Comparar con | Granularidad | Exportar PDF]

[Sección: Resumen Ejecutivo — KPIs con contexto histórico]

[Sección: Análisis de Tendencias — gráficos comparativos]

[Sección: Composición del negocio — por segmento, producto, zona]

[Sección: Indicadores de salud — márgenes, rotación, cobranza]
```

### 7.3. Controles del Dashboard

**Selector de período:** más granular que el Home. Opciones: MTD · QTD · YTD · Este año · Año anterior · Últimos 12 meses · Personalizado.

**Comparar con:** Período anterior · Mismo período año anterior · Plan (si plan está cargado).

**Granularidad:** Diaria · Semanal · Mensual (afecta el eje X de los gráficos de tendencia).

**Exportar PDF:** genera un PDF del Dashboard con los filtros actuales aplicados. El PDF tiene el logo del tenant, fecha de generación, y la nota "Datos al [timestamp]".

### 7.4. Sección: Resumen Ejecutivo

Grid 3×2 de `KpiCard` expandidas. Cada tarjeta en el Dashboard tiene más información que en el Home:
- Valor actual.
- Delta vs período de comparación (valor absoluto + %).
- Delta vs plan (si configurado), con chip "Sobre plan" / "Bajo plan".
- Gráfico sparkline de 24 períodos (más largo que en Home).
- Mini tabla de los últimos 3 períodos al expandir la tarjeta (hover o click).

KPIs del Dashboard (12 en total, 2 filas de 6):

**Fila 1 — Comercial:**
Ventas totales · Margen bruto % · Ticket promedio · Clientes activos · Nuevos clientes · Tasa de retención

**Fila 2 — Operacional:**
Pedidos procesados · Días promedio de entrega · CxC total · CxC vencida % · Rotación inventario · Stock disponible %

### 7.5. Sección: Análisis de Tendencias

Gráfico central de tendencias, tipo ECharts `line` con múltiples series:

- Serie principal: ventas del período seleccionado.
- Serie comparación: período de referencia seleccionado.
- Serie plan (si configurado): línea punteada horizontal o por período.
- Banda de confianza (opcional): área entre percentil 10 y 90 del histórico.

Debajo del gráfico principal: tres gráficos secundarios en fila (cada uno 33% del ancho):
1. Evolución del margen bruto %.
2. Evolución del número de clientes activos.
3. Evolución de pedidos vs entregas (barras agrupadas).

### 7.6. Sección: Composición del negocio

Tres `DonutChart` ECharts en fila:
1. Ventas por categoría de producto.
2. Ventas por zona/región (si configurada en SAP).
3. Ventas por canal (si configurado) o por forma de pago.

Debajo: tabla de composición con 4 columnas: Dimensión · Ventas período · % del total · Delta vs comparación. Ordenable.

### 7.7. Sección: Indicadores de salud

Cuatro paneles en grid 2×2, cada uno con un gráfico ECharts especializado:

**Márgenes por categoría:** gráfico de barras horizontales con el margen % de cada categoría de producto. Línea de referencia en el margen promedio de la empresa.

**Aging de cobranza:** stacked bar con los 4 buckets de aging (0–30d, 31–60d, 61–90d, >90d). Evolución mensual para ver si la cartera vencida está mejorando o empeorando.

**Rotación de inventario:** gráfico de barras por categoría de ítem, días de rotación. Línea de referencia en el benchmark de la industria (configurable).

**Concentración de clientes:** curva de Lorenz simplificada o gráfico de barras mostrando que el top N% de clientes genera X% de las ventas. Indicador de riesgo de concentración.

---

## 8. Módulo 04 — Native BI

**URL:** No es un módulo con ruta propia. Es la capa de componentes de visualización que subyace a todos los módulos analíticos.

### 8.1. Propósito de este módulo en el documento

Esta sección documenta el sistema de visualización nativo de DataBision: los tipos de gráficos disponibles, sus comportamientos, sus convenciones visuales y las reglas de uso. Cualquier desarrollador que trabaje en un módulo analítico debe leer esta sección antes de implementar charts.

### 8.2. Stack de visualización

**Apache ECharts vía `echarts-for-react`.**

Razones:
- Rendimiento: canvas-based, maneja datasets grandes sin degradar.
- Completitud: todos los tipos de gráfico necesarios están disponibles nativamente.
- Control total: todo es configurable vía option object — sin restricciones de un wrapper opinionado.
- Dark mode: soporte nativo de tema `'dark'` con control granular.
- Responsive: `autoResize` built-in.

### 8.3. Catálogo de gráficos

#### RevenueLineChart — Línea con área

**Casos de uso:** evolución temporal de métricas continuas (ventas, margen, CxC).

**Configuración visual:**
- Dos series como máximo (período actual + comparación).
- `areaStyle` con gradiente: `opacity: 0.3` en la base → `opacity: 0` en el fondo.
- Serie actual: `var(--brand-primary)`, grosor 2px.
- Serie comparación: `#94A3B8`, 1.5px, línea punteada `[4, 4]`.
- Tooltip compartido (trigger: axis) con formateador personalizado en español.
- Eje X: fechas formateadas según granularidad. Sin líneas verticales de grilla.
- Eje Y: solo línea base. Valores formateados con separadores de miles.
- Sin leyenda visible — el tooltip explica todo.

#### BarChart — Barras verticales o horizontales

**Casos de uso:** comparar categorías (top vendedores, top clientes, top productos).

**Configuración:**
- Horizontal cuando los labels son largos (nombres de clientes, descripciones de productos).
- Vertical cuando el eje X es tiempo.
- Barras agrupadas (máx 3 grupos) o apiladas al 100%.
- `barMaxWidth: 40` para evitar barras demasiado anchas.
- Color: primera serie `var(--brand-primary)`, segunda `var(--brand-secondary)`, tercera `#94A3B8`.
- Sin borde en barras, radio `[4, 4, 0, 0]` (esquinas superiores redondeadas).

#### StackedBarChart — Barras apiladas

**Casos de uso:** composición (aging por bucket, ventas por categoría en el tiempo).

**Configuración:**
- Paleta de colores semántica para aging: `#16A34A` · `#D97706` · `#EA580C` · `#DC2626`.
- Paleta brand para composición no-semántica: 6 colores derivados de `var(--brand-primary)` en distintas luminosidades.
- Tooltips al hover muestran el desglose completo de la barra.

#### DonutChart — Donut/Pie

**Casos de uso:** composición de un total (ventas por categoría, distribución de cartera).

**Configuración:**
- `radius: ['45%', '70%']` (donut, no pie).
- Etiquetas fuera del donut con línea de conexión, solo para los 5 segmentos más grandes. Los menores se agrupan como "Otros".
- Centro del donut: texto con el total general.
- Sin hover animado exagerado — solo subrayado sutil.
- Legend al costado derecho, desplazada del área del gráfico.

#### HeatmapChart — Mapa de calor

**Casos de uso:** patrones temporales (ventas por día de semana × hora, actividad de clientes).

**Configuración:**
- Eje X: horas del día (0–23) o días de la semana.
- Eje Y: días de la semana o semanas del mes.
- Color: gradiente de `#EFF6FF` (azul muy claro, sin actividad) a `var(--brand-primary)` (máxima actividad).
- Tooltip: muestra valor exacto y descripción.

#### SparklineChart — Micro gráfico

**Casos de uso:** tendencia inline dentro de un KpiCard o tabla.

**Configuración:**
- 60×24px (KpiCard) o 80×28px (tabla).
- Sin ejes, sin tooltip, sin interacción.
- Tipo: línea con área suave.
- Color heredado del contexto del componente padre.
- El objetivo es mostrar la forma de la tendencia, no los valores exactos.

#### ScatterChart — Dispersión

**Casos de uso:** relación entre dos variables (frecuencia vs monto de clientes, precio vs rotación de productos).

**Configuración:**
- Tooltip con nombre del punto + valores X e Y.
- Colores semánticos para destacar outliers.
- `symbolSize` proporcional a un tercer atributo (ej. margen del cliente).

### 8.4. Convenciones visuales universales

**Colores semánticos (nunca sobreescritos por branding):**
- Positivo/Éxito: `#16A34A`
- Negativo/Error: `#DC2626`
- Advertencia: `#D97706`
- Neutro/Sin cambio: `#64748B`

**Colores de series (determinados por branding):**
- Serie primaria: `var(--brand-primary)`
- Serie secundaria: `var(--brand-secondary)`
- Serie terciaria: `#94A3B8`

**Formatos de número:**
- Montos: separador de miles, 0 decimales para montos > 10.000, 2 decimales para menores.
- Porcentajes: siempre 1 decimal.
- Unidades: sin decimales.
- Días: enteros.

### 8.5. Comportamiento de loading

Todos los gráficos tienen un estado de loading con un placeholder rectangular del tamaño del gráfico con un shimmer animado (sin spinner). El shimmer tiene el mismo border-radius que el contenedor del gráfico. No hay resize cuando los datos cargan — el contenedor mantiene su tamaño durante todo el ciclo.

### 8.6. Comportamiento de error

Si el fetch de datos falla, el gráfico muestra un estado de error inline: ícono de advertencia + "No se pudo cargar" + botón "Reintentar". El tamaño del contenedor no cambia.

### 8.7. Exportación

Todos los gráficos tienen un menú contextual (ícono `⋮` en la esquina superior derecha al hover) con opciones:
- Exportar como imagen (PNG).
- Exportar datos como CSV.
- Ver tabla de datos (abre un Sheet con la tabla de los datos del gráfico).

La exportación de imagen usa el método `echartsInstance.getDataURL()` de ECharts. Incluye el logo del tenant en la esquina inferior derecha de la imagen exportada.

### 8.8. Implementación actual — Native BI Portal (Sprints 7A–7H)

> Esta subsección documenta el estado **implementado** del módulo Native BI a 2026-06-08.
> La sección 8.1–8.7 describe la especificación de diseño target; esta subsección es el estado real del código.

#### Rutas y páginas

| Ruta | Componente | Acceso |
|---|---|---|
| `/client/bi/dashboard` | `NativeBiDashboardPage` | Todos los usuarios |
| `/client/bi/sales` | `NativeBiSalesPage` | Todos los usuarios |
| `/client/bi/diagnostics` | `NativeBiDiagnosticsPage` | Solo `CompanyAdmin` |

#### Estructura de archivos

```
src/client/
├── pages/
│   ├── NativeBiDashboardPage.tsx
│   ├── NativeBiSalesPage.tsx
│   └── NativeBiDiagnosticsPage.tsx
├── components/nativebi/
│   ├── NativeBiPageHeader.tsx      ← header compartido (título + badge + acciones)
│   ├── NativeBiStatusBadge.tsx     ← badge de estado SyncStatusLevel
│   ├── NativeBiState.tsx           ← NbLoadingSkeleton, NbErrorState, NbEmptyState
│   ├── KpiCard.tsx
│   ├── SalesBarChart.tsx           ← ECharts BarChart (ventas diarias)
│   ├── TopCustomersTable.tsx
│   ├── SyncStatusWidget.tsx        ← widget inline para el header
│   ├── DateRangePicker.tsx
│   └── SortableTable.tsx           ← tabla genérica paginable y ordenable
├── hooks/
│   ├── useNativeBiDashboard.ts     ← useDashboardSummary, useDashboardSalesDaily, useTopCustomers
│   ├── useNativeBiSales.ts         ← useSalesOverview, useSalesCustomers, useSalesItems, useSalesSalespersons
│   └── useNativeBiDiagnostics.ts   ← useNativeBiDiagnostics, useNativeBiTableCounts
└── types/
    └── nativeBi.ts                 ← todos los tipos del módulo (SyncStatusLevel, NbPagedMeta, etc.)
```

#### API endpoints consumidos

| Endpoint | Hook | Página |
|---|---|---|
| `GET /api/client/bi/dashboard/summary` | `useDashboardSummary` | Dashboard |
| `GET /api/client/bi/dashboard/sales-daily` | `useDashboardSalesDaily` | Dashboard |
| `GET /api/client/bi/dashboard/top-customers` | `useTopCustomers` | Dashboard |
| `GET /api/client/bi/sales/overview` | `useSalesOverview` | Ventas |
| `GET /api/client/bi/sales/customers` | `useSalesCustomers` | Ventas |
| `GET /api/client/bi/sales/items` | `useSalesItems` | Ventas |
| `GET /api/client/bi/sales/salespersons` | `useSalesSalespersons` | Ventas |
| `GET /api/client/bi/diagnostics` | `useNativeBiDiagnostics` | Diagnósticos |
| `GET /api/client/bi/diagnostics/table-counts` | `useNativeBiTableCounts` | Diagnósticos |

#### Clases CSS `nb-*` (Sprint 7F — `src/index.css`)

| Clase | Propósito | Breakpoints |
|---|---|---|
| `nb-page-header` | Header flex con actions | `flex-direction: column` @ 540px |
| `nb-card-grid` | Grid KPI 4 columnas | 2 col @ 900px · 1 col @ 540px |
| `nb-table-scroll` | `overflow-x: auto` para tablas | — |
| `nb-mobile-stack` | Flex wrap para controles | `flex-direction: column` @ 540px |
| `nb-grid` | Grid genérico con gap | — |
| `nb-tab-bar` | Tab bar con `overflow-x: auto` | — |

#### Convenciones de componentes compartidos

- **`NativeBiPageHeader`** — todos los NativeBi pages usan este componente. Props: `title`, `description?`, `badge?` (default `'Native BI'`), `actions?`.
- **`NativeBiStatusBadge`** — mapea `SyncStatusLevel` (`ok` · `warning` · `error` · `unknown`) a clases `db-badge--*`.
- **`NbLoadingSkeleton`** — skeletons con `cp-skeleton`. Props: `rows` (default 5), `height` (default 44px).
- **`NbErrorState`** — alerta de error con botón opcional de reintento. Props: `message?`, `onRetry?`.
- **`NbEmptyState`** — estado vacío con ícono SVG. Props: `message?`, `icon?` (`'table'` · `'chart'` · `'search'`).

---

## 9. Módulo 05 — Ventas

**URL:** `{slug}.databision.app/sales`

### 9.1. Propósito

Análisis completo del desempeño comercial. Responde quién vende qué, cuándo, a quién y con qué margen.

### 9.2. Sub-navegación

Tabs debajo del Page Header: `Overview` · `Por Período` · `Por Vendedor` · `Por Cliente`

### 9.3. Overview de Ventas

**KPI Strip (5 tarjetas):**

| KPI | Descripción | Semántica |
|---|---|---|
| Ventas totales | Suma DocTotal del período | ↑ positivo |
| Facturas emitidas | Cantidad OINV | neutral |
| Ticket promedio | Ventas / facturas | ↑ positivo |
| Margen bruto % | (Ventas − Costo) / Ventas | ↑ positivo |
| Clientes únicos | OCRD con OINV en período | ↑ positivo |

**Gráfico principal:** `RevenueLineChart` con ventas diarias del período vs período de comparación.

**Gráfico secundario (33% de ancho):** `DonutChart` de ventas por categoría de producto.

**Tabla Top 10 ítems vendidos:**
Columnas: Código · Descripción · Unidades · Monto · % del total · Margen %. Ordenable. Exportable.

### 9.4. Ventas por Período

**Granularidad:** toggle Diaria · Semanal · Mensual.

**Gráfico principal:** `BarChart` vertical. Barras agrupadas: período actual (brand-primary) vs período anterior (brand-secondary) vs mismo período año anterior (#94A3B8).

**Línea de tendencia:** `markLine` de ECharts con regresión lineal. Muestra si la tendencia es creciente o decreciente.

**Tabla de datos:**
Una fila por período (día/semana/mes según granularidad). Columnas: Período · Ventas · Delta vs anterior (importe + %) · Delta vs año anterior. Click en fila drilla a los documentos de ese período.

**Análisis de estacionalidad:**
`HeatmapChart` mostrando ventas promedio por día de la semana × semana del mes. Permite identificar patrones de compra recurrentes.

### 9.5. Ventas por Vendedor

**Gráfico:** `BarChart` horizontal con los vendedores ordenados por ventas descendente. Barras coloreadas por `var(--brand-primary)` con opacidad proporcional a la posición (el primero al 100%, los siguientes degradan levemente).

**Tabla de vendedores:**
Columnas: Vendedor · Ventas período · % del total · Facturas · Ticket promedio · Margen % · Delta vs período anterior. Ordenable por cualquier columna.

**Drilldown de vendedor:**
Click en cualquier fila abre un Sheet lateral (shadcn Sheet, 600px) con:
- Header: nombre, foto/avatar (iniciales si no hay foto), ventas totales.
- Gráfico de evolución de ventas últimos 12 meses.
- Top 10 clientes del vendedor.
- Top 10 ítems vendidos.
- Tabla de facturas del período.

### 9.6. Ventas por Cliente

Estructura similar a Por Vendedor. Columnas adicionales: CxC vigente · Días promedio de pago · Fecha último pedido.

**Segmentación visual:** los clientes tienen un badge de segmento (Champion / Leal / En riesgo / Inactivo) basado en su frecuencia y monto relativo.

Click en cliente navega a `/customers/:id` (detalle completo del cliente).

### 9.7. Filtros globales del módulo

Todos los tabs comparten la FilterBar:
- Período (hereda del selector global o se puede sobreescribir por módulo).
- Vendedor (multi-select con búsqueda).
- Categoría de producto (árbol multi-select).
- Zona / región (si configurado en SAP B1).

Los filtros activos se muestran como chips bajo el Page Header. Cada chip tiene un botón `×` para removerlo. "Limpiar filtros" si hay más de uno.

---

## 10. Módulo 06 — Clientes

**URL:** `{slug}.databision.app/customers`

### 10.1. Propósito

Visión 360° del comportamiento de la cartera de clientes. Frecuencia de compra, antigüedad de deuda, segmentación por valor y riesgo de pérdida.

### 10.2. Overview de Clientes

**KPI Strip (4 tarjetas):**
- Clientes activos (compraron en el período seleccionado).
- Nuevos clientes (primera factura en el período).
- Clientes en riesgo (sin compra en más de N días configurables).
- Total CxC (suma de cuentas por cobrar).

**Mapa de segmentación:**
`ScatterChart` ECharts con:
- Eje X: Frecuencia de compra (pedidos/mes promedio últimos 6 meses).
- Eje Y: Monto promedio por período.
- Tamaño del punto: CxC pendiente.
- Color del punto: segmento (Champion/Leal/En riesgo/Inactivo).

Los cuadrantes del scatter definen la segmentación visualmente. Click en un punto navega al detalle del cliente. Click en la leyenda de segmento filtra la tabla.

**Tabla de clientes:**
Columnas: Nombre · Segmento (badge) · Último pedido (fecha relativa) · Compras período · CxC vencida · Vendedor · Acciones. Paginada 25 filas. Exportable.

### 10.3. AR Aging — Cuentas por Cobrar

**Gráfico principal:** `StackedBarChart` horizontal donde cada barra es un cliente con su aging desglosado:
- Corriente (0–30d): `#16A34A`
- Vencido 31–60d: `#D97706`
- Vencido 61–90d: `#EA580C`
- Vencido >90d: `#DC2626`

Solo se muestran clientes con saldo > 0. Ordenados por saldo total descendente.

**KPI Strip contextual:**
- Total CxC.
- CxC vencida (suma >30d) y su % del total.
- CxC >90 días y su % del total.
- DSO — Days Sales Outstanding (días promedio de cobro).

**Tabla de aging:**
Una fila por cliente con saldo. Columnas: Cliente · Corriente · 31–60d · 61–90d · >90d · Total · Vendedor. Click en una fila expande las facturas vencidas de ese cliente con número de documento, fecha, monto, días vencida.

### 10.4. Detalle de Cliente

**URL:** `/customers/:id`

**Header del cliente:**
Nombre · RUT o NIT · Segmento (badge) · Vendedor asignado · Ciudad/Zona · Status (Activo / Inactivo / En riesgo). Botón "Crear acción" crea un Business Action vinculado a este cliente.

**Tabs:**
1. **Resumen** — KPIs del cliente, gráfico de evolución de compras 12 meses, top 5 productos comprados.
2. **Pedidos** — tabla de todos los pedidos con estado, monto, fecha, vendedor.
3. **Facturas** — tabla de facturas con estado de pago (pagada/vencida/corriente).
4. **CxC** — aging específico del cliente con detalle por factura.
5. **Acciones** — Business Actions creadas para este cliente, con su estado.

---

## 11. Módulo 07 — Productos

**URL:** `{slug}.databision.app/products`

### 11.1. Propósito y diferencia con Inventario

**Productos** es el módulo de rendimiento comercial de los ítems: cuáles venden más, con qué margen, cuáles están estancados, cuáles son estratégicos. Es un módulo **analítico-comercial**.

**Inventario** es el módulo de estado físico del stock: cuánto hay, dónde, qué movimientos hay. Es un módulo **operacional**.

Misma fuente de datos (OITM), perspectiva diferente.

### 11.2. Overview de Productos

**KPI Strip (4 tarjetas):**
- Ítems activos (con ventas en el período).
- Ítem más vendido (nombre + monto).
- Margen promedio del catálogo %.
- Ítems sin venta en el período (slow-movers).

**Tabla de productos:**
Columnas: Código · Descripción · Categoría · Unidades vendidas · Monto ventas · % del total · Margen % · Stock actual · Rotación (días). Exportable y ordenable.

**Filtros:** Categoría/Grupo · Con ventas / Sin ventas · Margen > X% · Período.

### 11.3. Márgenes de Producto

**Gráfico:** `BarChart` horizontal ordenado por margen % descendente. Línea de referencia en el margen promedio de la empresa. Barras coloreadas:
- Verde si margen > promedio + 5pp.
- Color brand si margen en ±5pp del promedio.
- Rojo si margen < promedio − 5pp.

**Tabla de márgenes:**
Columnas: Código · Descripción · Costo promedio · Precio promedio · Margen bruto · Margen % · Unidades vendidas · Monto total. Click en una fila muestra el histórico de precio y costo del ítem en un mini gráfico inline.

### 11.4. Slow-Moving Products

**Definición:** ítems con stock disponible y sin ventas en el período configurado (default: 90 días).

**KPI:** Número de ítems slow-moving · Valor de inventario inmovilizado · % del stock total.

**Tabla:**
Columnas: Código · Descripción · Última venta (fecha) · Días sin venta · Stock actual · Valor de inventario bloqueado. Ordenada por días sin venta descendente.

**Acción sugerida:** botón "Crear insight" en la tabla → crea un Insight en el módulo de Recommendations con la lista de slow-movers y el valor inmovilizado.

### 11.5. Detalle de Producto

**URL:** `/products/:id`

**Header:** Código · Descripción · Grupo/Categoría · Proveedor principal.

**Tabs:**
1. **Desempeño** — evolución de ventas mensual (12 meses), top 10 clientes que lo compraron.
2. **Márgenes** — evolución de precio y costo en el tiempo. Gráfico de líneas doble.
3. **Inventario** — stock por almacén, movimientos recientes.
4. **Sustitutos** — ítems del mismo grupo/categoría (tabla comparativa de ventas y margen).

---

## 13. Módulo 08 — Vendedores

**URL:** `{slug}.databision.app/salesreps`

### 13.1. Propósito

Análisis del desempeño del equipo comercial. Ranking, evolución mensual, cumplimiento de metas (si están configuradas) y detalle por vendedor. Es el módulo que el gerente comercial usa para sus reuniones de equipo.

### 13.2. Sub-navegación

Tabs: `Overview / Ranking` · `Comparación Mensual` · `Cumplimiento` (solo si hay metas configuradas)

### 13.3. Overview / Ranking

**KPI Strip (4 tarjetas):**
- Ventas totales del equipo.
- Vendedor líder (nombre + monto del período).
- Promedio de ventas por vendedor.
- Vendedores activos en el período.

**Gráfico:** `BarChart` horizontal con los vendedores ordenados por ventas descendente. Color `var(--brand-primary)`. Label en cada barra con el monto formateado.

**Tabla de vendedores:**
Columnas: `#` (posición en ranking) · Vendedor · Ventas período · % total · Facturas · Ticket promedio · Margen % · Delta vs período anterior. Ordenable por cualquier columna.

**Drilldown de vendedor:** click en fila abre Sheet lateral (600px en desktop):
- Header: nombre, ranking actual vs mes anterior (↑ desde #5 a #3), ventas totales.
- KPIs: ventas período, clientes atendidos, ticket promedio.
- `LineChart`: evolución de ventas 12 meses.
- Tabla: top 10 clientes del vendedor.
- Tabla: top 10 productos vendidos.

### 13.4. Comparación Mensual

**Gráfico:** `BarChart` agrupado. Cada grupo = un vendedor, cada barra = un mes (últimos 6 meses). Permite ver quién creció y quién decreció mes a mes.

**HeatmapChart de actividad:** eje X = meses, eje Y = vendedores, color = N° de facturas o monto (toggle). Permite identificar vendedores con baja actividad en períodos específicos.

**Tabla comparativa:** una fila por vendedor, columnas = los últimos 6 meses + columna de tendencia (flecha con dirección).

### 13.5. Cumplimiento de metas

Visible solo cuando el administrador ha configurado metas por vendedor en Settings.

**Para N ≤ 6 vendedores:** Gauge semicircular por vendedor (120px) mostrando % de cumplimiento. Verde ≥ 100% · Amarillo 70–99% · Rojo < 70%.

**Para N > 6 vendedores:** `Progress` bar por vendedor en lista vertical. Mismo código de colores.

**Por debajo:** tabla con Vendedor · Meta · Alcanzado · % Cumplimiento · Días restantes del período.

### 13.6. Perfil de Vendedor — `/salesreps/:id`

```
HEADER
[Avatar/iniciales]  Nombre del vendedor
                    Cargo · Zona asignada (si configurada en SAP)
                    Ranking actual: #3 de 8 (↑ desde #5 el mes anterior)
                    [Btn: Ver clientes asignados]

TABS: Ventas | Clientes | Productos | Meses

Tab Ventas:
  KPIs del vendedor + LineChart evolución 12m + tabla facturas del período.

Tab Clientes:
  Top 20 clientes del vendedor con ventas y CxC del período.

Tab Productos:
  Top 20 productos vendidos con monto y margen %.

Tab Meses:
  Tabla comparativa: N° mes | Ventas | Facturas | Ticket prom. | Clientes únicos | Ranking ese mes.
```

---

## 14. Módulo 09 — Inventario

**URL:** `{slug}.databision.app/inventory`

### 14.1. Propósito

Estado físico del inventario: cuánto hay, dónde, qué movimientos hay, cuáles ítems están críticos.

### 12.2. Overview de Inventario

**KPI Strip (4 tarjetas):**
- Ítems bajo mínimo (badge rojo si > 0).
- Ítems sin stock con demanda activa (badge rojo si > 0).
- Valor total de inventario.
- Rotación promedio en días.

**Gráfico de composición:**
`DonutChart` de valor de inventario por categoría/grupo.

**Mapa de calor de almacenes (si hay múltiples):**
`HeatmapChart` donde el eje X son los almacenes y el eje Y son las categorías de ítem. El color representa el nivel de ocupación (stock actual / stock máximo).

**Tabla de inventario:**
Columnas: Código · Descripción · Almacén · Stock actual · Mínimo · Máximo · Status. Status options con badges:
- `Sin stock`: rojo
- `Bajo mínimo`: amarillo
- `Normal`: verde
- `Sobre stock`: azul

Filtrable por Status. Click en una fila expande el detalle por almacén si hay múltiples.

### 12.3. Inventario por Almacén

Vista de tabla por almacén:
Columnas: Almacén · Ítems totales · Valor total · Ítems bajo mínimo (badge) · Ítems sin stock (badge). Click en almacén → drilldown a todos los ítems de ese almacén.

### 12.4. Movimientos de Stock

**Filtros:** Almacén · Ítem · Tipo (Entrada / Salida / Ajuste / Transferencia) · Período.

**Tabla:**
Columnas: Fecha · Doc. SAP · Tipo (badge color) · Ítem · Almacén · Cantidad (verde entradas, rojo salidas) · Doc. referencia.

**Gráfico de flujo:**
`StackedBarChart` por período mostrando entradas vs salidas. Permite ver si el inventario neto está creciendo o decreciendo.

---

## 15. Módulo 10 — Sync Center

**URL:** `{slug}.databision.app/sync`

### 15.1. Propósito

Transparencia total sobre el estado de las integraciones con SAP B1. El usuario sabe exactamente cuándo se sincronizó por última vez, qué pasó, y qué error hay si algo falla. La confianza en los datos empieza aquí.

### 15.2. Status global

Banner horizontal (48px) en la parte superior de la página:
- Verde: "Sincronización normal — Última actualización hace X minutos".
- Amarillo: "Sincronización con retraso — Última actualización hace X horas".
- Rojo: "Error de sincronización activo — Ver detalles".

### 15.3. Estado por Extractor

Cada extractor tiene una `ExtractorStatusCard`:

```
┌──────────────────────────────────────────────────────────────┐
│ [badge RUNNING / IDLE / WARNING / ERROR]                      │
│ Nombre del extractor                 Modalidad A · Dedicado   │
│                                                               │
│ Última ejecución exitosa:  hace 45 minutos (hh:mm dd/mm)     │
│ Próxima ejecución:         en 15 minutos (hh:mm)             │
│ Registros procesados:      1.247 · 312 nuevos · 935 actualizados │
│                                                               │
│ Objetos SAP sincronizados:                                    │
│  ✓ OINV  hace 45min    ✓ OCRD  hace 45min                    │
│  ✓ OITM  hace 45min    ✓ ORDR  hace 45min                    │
│                                                               │
│                                          [Ver logs]  [···]   │
└──────────────────────────────────────────────────────────────┘
```

El color del badge: `RUNNING` verde con pulso animado · `IDLE` gris · `WARNING` amarillo · `ERROR` rojo.

### 15.4. Estado Service Layer (Modalidad B)

Card específica:
- URL del endpoint (parcialmente oculta: `https://sap-b1.empresa.com:50000/...***`).
- Conectividad: chip Reachable / Unreachable con latencia en ms.
- Última validación: timestamp.
- Versión SAP B1 detectada.
- Credenciales: "Válidas" / "Expiran en X días".

### 15.5. Historial de sincronizaciones

Tabla con columnas: Timestamp · Extractor · Resultado (badge) · Objetos · Registros procesados · Duración. Paginada 25 filas. Filtros: extractor · resultado · período.

Click en una fila expande el detalle de esa ejecución: todos los objetos sincronizados con su resultado individual.

### 15.6. Errores y logs

Lista cronológica de errores. Cada error tiene:
- Timestamp.
- Extractor.
- Tipo de error (badge): `CONNECTIVITY` · `AUTH` · `DATA_VALIDATION` · `TRANSFORM` · `UNKNOWN`.
- Mensaje legible.
- Stack trace colapsable (botón "Ver detalle técnico").

Tipos de error en lenguaje de negocio:
- `CONNECTIVITY` → "No se pudo conectar a SAP B1".
- `AUTH` → "Credenciales de acceso inválidas o expiradas".
- `DATA_VALIDATION` → "Los datos recibidos tienen un formato inesperado".
- `TRANSFORM` → "Error al procesar los datos recibidos".

---

## 16. Módulo 11 — Operational Cockpit

**URL:** `{slug}.databision.app/cockpit`

### 16.1. Propósito

**La pregunta que responde:** "¿Qué necesita atención operacional hoy?"

El Cockpit es un semáforo de la operación diaria. Está diseñado para que en 5 segundos sin scroll se vea si hay algo crítico. No es un módulo analítico — es un módulo de acción. Cada sección del Cockpit es directamente accionable.

### 16.2. Diferencia con el Live Layer

El Cockpit muestra el estado acumulado del día (pedidos pendientes hoy, stock crítico hoy). El Live Layer muestra eventos que están ocurriendo ahora mismo (una sincronización en curso, un pedido entrando en este momento).

### 14.3. Barra de estado global

Banda de 56px en la parte superior de la página. Color determinado por el peor estado de cualquier sección del Cockpit:
- Verde: "Operación normal".
- Amarillo: "X ítems requieren seguimiento".
- Rojo: "X ítems críticos requieren acción inmediata".

El texto muestra un resumen ejecutivo: "23 pedidos pendientes · 7 entregas retrasadas · 3 ítems sin stock".

### 14.4. Sección: Pedidos Pendientes

**Definición:** ORDR con estado Abierto que superan el tiempo promedio de entrega del cliente o que llevan más de N días (configurable).

**Header de sección:**
Contador grande "23 pedidos" + tiempo promedio de espera actual del lote pendiente.

**Tabla:**
Columnas: N° Pedido · Cliente · Fecha · Días pendiente (badge de color por urgencia) · Monto · Vendedor · Acción.

- Días pendiente badge: verde < 3d · amarillo 3–7d · rojo > 7d.
- Fila con `> 7d`: fondo rojo/5 (muy sutil, para destacar sin agresividad visual).

**Acciones de fila:**
- "Ver en SAP": link externo si el URL de SAP está configurado.
- "Crear acción": abre Business Actions con el pedido prellenado.
- "Marcar gestionado": registra una nota y mueve el pedido fuera del Cockpit activo.

### 14.5. Sección: Entregas Pendientes

ODLN pendientes o con fecha comprometida vencida.

**Columnas:** N° Entrega · Cliente · Fecha comprometida · Días de retraso · Almacén origen · Status (badge). Status: `En preparación` · `Retrasada` · `Sin confirmar`.

Filas con status `Retrasada` tienen fondo naranja/5.

### 14.6. Sección: Stock Crítico

OITM con stock < mínimo o stock = 0 con demanda activa (pedidos abiertos).

**Tabla:**
Columnas: Código · Descripción · Almacén · Stock actual · Mínimo · Déficit · Demanda pendiente (unidades en ORDR abiertos).

Badge de estado: `Sin stock` rojo · `Bajo mínimo` amarillo.

**Acción contextual:** botón "Sugerir orden de compra" en la fila → crea un Business Action con draft prellenado.

### 14.7. Sección: Integraciones con Error

Extractores en estado ERROR o WARNING en las últimas 24h.

Lista compacta: nombre del extractor · tipo de error · timestamp · último éxito · botón "Ver en Sync Center".

Si hay 0 errores: estado vacío positivo "Todas las integraciones activas" con ícono verde.

### 14.8. Sección: Alertas Activas (resumen)

Feed de las 10 alertas activas más recientes ordenadas por severidad. Link "Ver todas en Alert Center →".

### 14.9. Actualización automática

Refetch cada 5 minutos vía TanStack Query `refetchInterval`. Un chip "Actualizando..." aparece brevemente en el topbar. El usuario puede forzar refresh manual con botón en el page header sin perder posición de scroll.

---

## 17. Módulo 12 — Operational Live Layer

**URL:** `{slug}.databision.app/live`

### 15.1. Propósito

**La pregunta que responde:** "¿Qué está pasando ahora mismo?"

El Live Layer es la pantalla de tiempo real (o cuasi-real). Muestra eventos y actividad con timestamps de segundos o minutos, no de horas. Es la diferencia entre "el negocio hoy" y "el negocio ahora".

Es una pantalla de monitoreo, no de análisis. Los usuarios que la usan son operadores en turno, gerentes en seguimiento activo de una situación, o analistas verificando una sincronización reciente.

### 15.2. Layout de la página

```
[Indicador de actualización: pulsante verde "En vivo — actualizado hace X seg"]

┌─────────────────────────────┬──────────────────────────────────┐
│  Live Feed                  │  Métricas en tiempo real         │
│  Eventos recientes          │  Contadores que cambian          │
│  (stream de actividad)      │  KPIs de las últimas 2 horas     │
│  60% ancho                  │  40% ancho                       │
└─────────────────────────────┴──────────────────────────────────┘

[Sección: Sincronizaciones en curso]
[Sección: Pedidos recientes (últimas 2 horas)]
[Sección: Alertas generadas hoy]
```

### 15.3. Indicador de actualización

Chip en el page header con punto verde pulsante: "En vivo — actualizado hace X segundos". El componente actualiza su texto cada segundo usando un timer local. Si la última actualización fue hace más de 60 segundos, el punto cambia a amarillo. Si fue hace más de 5 minutos, cambia a rojo y muestra "Conexión pausada".

### 15.4. Live Feed

Stream cronológico de eventos del sistema en las últimas 4 horas. Cada evento es una fila en un feed estilo timeline:

```
  ●  [2 min]  Sincronización completada — 347 registros actualizados
  ●  [5 min]  Alerta: Stock crítico — Ítem XYZ-001 en almacén Central
  ●  [8 min]  Pedido #12345 recibido — Cliente Comercial Torres — $2.450.000
  ●  [12 min] Sincronización iniciada — Extractor Empresa 1
  ●  [15 min] Acción resuelta — "Revisar pedidos retrasados"
  ●  [23 min] Alerta: Entrega comprometida vencida — Cliente ABC
```

El ícono `●` es el color del tipo de evento: verde (sincronización OK) · rojo (alerta) · azul (pedido) · gris (sistema) · amarillo (advertencia).

Los nuevos eventos aparecen en la parte superior con una animación suave de entrada (`slide-in` desde arriba). La lista nunca tiene más de 50 ítems — los más antiguos desaparecen.

**Pausa automática:** el feed pausa la auto-actualización cuando el usuario hace hover sobre él o hace scroll (para leer sin que los ítems salten). Un chip "Actualización pausada — click para reanudar" aparece al pausar.

### 15.5. Métricas en tiempo real

Panel derecho con KPIs de las últimas 2 horas:
- Pedidos recibidos (contador, actualiza en tiempo real).
- Facturas emitidas.
- Registros sincronizados.
- Alertas generadas.
- Errores activos.

Cada métrica tiene un mini gráfico de tipo `SparklineChart` mostrando la actividad por los últimos 30 minutos (un punto cada 2 minutos).

### 15.6. Sincronizaciones en curso

Si hay un extractor ejecutándose en el momento, muestra:
- Nombre del extractor.
- Barra de progreso (si la API reporta progreso): "Procesando OINV — 1.247 de 3.892 registros".
- Tiempo transcurrido.
- ETA estimada.

Si no hay sincronizaciones activas: estado neutro "Siguiente sincronización en X minutos".

### 15.7. Pedidos recientes

Tabla de los pedidos SAP B1 recibidos en las últimas 2 horas. Columnas: Timestamp · N° Pedido · Cliente · Monto · Vendedor. Actualización automática cada 30 segundos.

### 15.8. Frecuencia de actualización

El Live Layer usa polling con `refetchInterval: 30000` (30 segundos) para la mayoría de los datos. El Live Feed usa polling con `refetchInterval: 15000` (15 segundos). En una versión futura, el feed puede migrarse a WebSocket/SSE para actualización verdaderamente en tiempo real.

---

## 18. Módulo 13 — Alert Center

**URL:** `{slug}.databision.app/alerts`

### 16.1. Propósito

Centro unificado de todas las alertas del sistema. Las alertas son condiciones que superaron un umbral configurado y requieren atención o acción.

**Diferencia con Insights:** Las alertas son binarias (activa/resuelta) y configuradas. Los insights son detectados y requieren interpretación.

### 16.2. Resumen de estado

Tres contadores en el page header:

```
  🔴  3   Críticas     🟡  12  Operativas     🔵  5   Comerciales
```

Click en cualquier contador filtra la lista a esa categoría.

### 16.3. Categorías de alertas

**Alertas Críticas (rojo):**
- Stock a cero con demanda activa.
- Extractor sin sincronización por más de X horas.
- Error de integración bloqueante.
- Margen bruto cayó por debajo del umbral configurado.
- CxC >90 días superó X% de la cartera.

**Alertas Operativas (naranja):**
- Pedido sin movimiento por más de N días.
- Entrega comprometida vencida.
- Stock por debajo del mínimo (no cero).
- Cuenta por cobrar vencida superando N días.
- Cliente sin actividad en N días.

**Alertas Comerciales (azul):**
- Cliente nuevo (primera factura en el portal).
- Venta que supera en X% el ticket promedio histórico del cliente.
- Ítem sin ventas en el período con stock disponible.
- Vendedor sin actividad en N días.

### 16.4. Tarjeta de alerta

```
┌─ [banda color severidad] ─────────────────────────────────────────┐
│  [ícono] Stock crítico — Ítem XYZ-001          ⏱ hace 2 horas     │
│  Stock actual: 0 unidades. Hay 3 pedidos activos por 47 unidades.  │
│                                                                    │
│  [Ver detalle]  [Crear acción]  [Resolver]  [Silenciar 4h]  [···] │
└────────────────────────────────────────────────────────────────────┘
```

La banda izquierda es el indicador principal de severidad (rojo/naranja/azul). El cuerpo de la tarjeta describe el problema en lenguaje natural con el valor concreto que lo originó.

### 16.5. Gestión de alertas

**Resolver:** requiere una nota de resolución corta (textarea, mín 10 caracteres). La alerta pasa al historial con la nota, el usuario que la resolvió y el timestamp.

**Silenciar:** silencia la alerta durante 1h / 4h / 24h. Reaparece automáticamente si sigue activa al vencer el silencio. Útil para alertas conocidas en proceso de resolución.

**Crear acción:** shortcut a Business Actions con el contexto de la alerta prellenado. Al crear la acción, la alerta muestra un chip "Acción creada" vinculado.

**Asignar:** asigna la alerta a un usuario del portal. El usuario recibe una notificación interna (badge en el topbar).

**Desestimar:** cierra la alerta como no-accionable. Requiere nota. Queda en historial como desestimada.

### 16.6. Historial de alertas

Tab "Historial" en la sub-navegación muestra todas las alertas cerradas (resueltas, silenciadas vencidas, desestimadas) con su nota de resolución y el usuario que actuó.

### 16.7. Configuración de umbrales

Link "Configurar alertas" en el page header. Navega a `/settings/alerts`. Permite al administrador de la empresa configurar:
- Umbrales por tipo de alerta (ej. "Alertar cuando stock < mínimo").
- Período de inactividad de cliente (ej. "N días sin compra").
- Umbral de margen bruto.
- Horarios de supresión (ej. no generar alertas comerciales los fines de semana).
- Destinatarios de notificaciones por email (futuro).

---

## 19. Módulo 14 — Recommendations & Insights

**URL:** `{slug}.databision.app/insights`

### 17.1. Propósito

Superficie donde el sistema presenta hallazgos, patrones y recomendaciones accionables derivados del análisis de los datos del cliente. Va más allá de las alertas (que son umbrales) — son observaciones sobre el comportamiento del negocio.

### 17.2. Diferencia con las Alertas

| Alertas | Insights |
|---|---|
| Condición binaria (superó/no superó umbral) | Patrón observado en los datos |
| Binaria: activa / resuelta | Continua: detectada, en proceso, cerrada |
| Configurada explícitamente | Detectada por análisis |
| "El stock X llegó a cero" | "Las ventas de los martes están cayendo sistemáticamente" |

### 17.3. Layout

```
[Resumen: X nuevas · Y en proceso · Z cerradas esta semana]

[Tabs: Insights | Recomendaciones | Anomalías | Historial]
```

### 17.4. Tab: Insights

Hallazgos detectados por el sistema sobre el comportamiento del negocio.

Cada Insight es una tarjeta con:
- **Categoría** (chip): Ventas · Clientes · Inventario · Financiero.
- **Importancia** (chip): Alta · Media · Baja.
- **Título:** lenguaje natural, específico. Ej: "Las ventas de los martes son 34% más altas que el promedio semanal".
- **Descripción:** 2–4 líneas con contexto. Incluye el período observado y la fuente del hallazgo.
- **Mini gráfico ECharts** (SparklineChart o mini BarChart, 120px altura): visualiza la métrica relevante.
- **Acciones:** "Investigar" (navega al módulo con filtros aplicados) · "Crear recomendación" · "Cerrar".

Ejemplos de insights generados automáticamente:
- "El cliente [X] no ha comprado en 68 días. Históricamente compra cada 30 días."
- "Las ventas del canal [Y] cayeron 45% en las últimas 2 semanas."
- "El ítem [Z] tiene una rotación de 180 días — 3× el promedio del grupo."
- "El vendedor [A] tiene un margen promedio 8pp por encima del equipo."

### 17.5. Tab: Recomendaciones

Acciones concretas sugeridas por el sistema, con sustento cuantitativo.

Cada recomendación tiene:
- Título de la acción sugerida.
- Justificación con dato: "Los 15 clientes sin compra en >90 días representan $X en ventas históricas."
- Impacto estimado: "Si el 30% responde, recuperas ~$Y en ventas este mes."
- Botón "Aplicar" → crea un Business Action con el contexto prellenado.
- Botón "Descartar" → cierra la recomendación con motivo.

Ejemplos de recomendaciones:
- "Reactivar 15 clientes inactivos con más de 90 días sin compra."
- "Reducir precio del ítem [Z] — tiene stock alto y margen sobre el 40%."
- "Asignar vendedor al cliente [X] — tiene historial de compra pero no tiene vendedor asignado."

### 17.6. Tab: Anomalías

Variaciones estadísticamente significativas respecto al comportamiento histórico del mismo tenant.

Cada anomalía tiene:
- Tipo: Incremento anómalo / Caída anómala / Patrón roto.
- Métrica afectada y cuánto se desvió del histórico.
- Mini gráfico mostrando la serie histórica con la anomalía marcada con un punto rojo.
- Período de detección.
- Botones: "Investigar" · "Es esperado" (cierra la anomalía como esperada, el sistema aprende) · "Crear acción".

### 17.7. Indicadores de riesgo

Panel de riesgo empresarial agregado. Siempre visible en la parte inferior del módulo (no en tab). Muestra 6 indicadores con nivel de riesgo:

| Indicador | Nivel | Métrica |
|---|---|---|
| Concentración de clientes | Bajo / Medio / Alto | Top 3 clientes = X% de ventas |
| Cartera vencida >90d | Bajo / Medio / Alto | X% de la CxC total |
| Dependencia de ítems | Bajo / Medio / Alto | Top 5 ítems = X% de ventas |
| Stock de seguridad | OK / En riesgo | X% de SKUs activos bajo mínimo |
| Frescura de datos | OK / Retrasada | Última sync hace X horas |
| Margen en tendencia | Estable / Cayendo | Delta vs mismo período año anterior |

Cada indicador tiene una flecha de tendencia (mejoró / empeoró / estable) y la fecha de última actualización.

---

## 20. Módulo 15 — Business Actions

**URL:** `{slug}.databision.app/actions`

### 18.1. Propósito

Registro y seguimiento de acciones concretas derivadas de los insights del sistema. Cierra el loop entre "detectar" y "resolver". No es un CRM ni un gestor de tareas genérico — es específicamente para acciones que responden a algo que el sistema identificó.

### 18.2. KPIs del módulo

Tres contadores en el page header: "X activas · Y resueltas este mes · Z vencidas".

### 18.3. Views: Lista y Kanban

**Toggle en el page header:** vista Lista (default) / vista Kanban.

**Vista Lista:**
Tabla de acciones con columnas: Título · Origen (chip) · Módulo · Responsable · Fecha límite · Prioridad · Estado (badge).

**Vista Kanban:**
Cuatro columnas: `Pendiente` · `En progreso` · `Resuelta` · `Desestimada`. Cada acción es una tarjeta draggable. Arrastrar una tarjeta entre columnas cambia su estado con una llamada a la API. La vista Kanban es especialmente útil en reuniones de revisión operacional.

### 18.4. Tabs de estado

`Todas` · `Pendientes` · `En progreso` · `Resueltas` · `Vencidas`

### 18.5. Tarjeta de acción

```
┌──────────────────────────────────────────────────────────────────┐
│ [alta] Reactivar cliente Comercial Torres                        │
│ Origen: Insight  |  Módulo: Clientes  |  Responsable: J. Pérez  │
│ Creada: hace 3 días  |  Vence: en 2 días                        │
│ "Cliente sin compra en 68 días. Históricamente compra cada 30d" │
│                          [Actualizar estado]  [Ver contexto]    │
└──────────────────────────────────────────────────────────────────┘
```

El chip de origen indica de dónde vino la acción: `Insight` · `Anomalía` · `Recomendación` · `Cockpit` · `Alerta` · `Manual`. Click en el chip de origen navega al insight o alerta que la originó.

### 18.6. Sheet de creación / edición

Se abre como Sheet lateral de 600px desde el lado derecho.

**Campos:**
- Título (requerido, min 5 chars).
- Descripción / contexto (textarea, prellenado si viene de un insight).
- Origen (select, autocompletado si viene de un insight).
- Módulo relacionado (select: Ventas / Clientes / Inventario / Operaciones / General).
- Responsable (select de usuarios de la empresa con búsqueda).
- Fecha límite (date picker, opcional).
- Prioridad (segmented: Alta · Media · Baja).
- Documentos adjuntos (futuro).

### 18.7. Cambio de estado

Al mover una acción a "Resuelta" o "Desestimada" se muestra un Dialog de confirmación pidiendo:
- Una nota corta de resultado (textarea, min 10 caracteres).
- Rating de resultado (opcional): ¿fue accionable? ¿produjo resultado?

El historial de cambios de estado de una acción queda registrado en el detalle.

### 18.8. Métricas de seguimiento

Tab "Métricas" en el módulo muestra:
- Tasa de resolución (acciones resueltas / creadas).
- Tiempo promedio de resolución por categoría.
- Acciones por responsable.
- Acciones creadas vs resueltas por semana (gráfico de barras agrupadas).

---

## 21. Responsive Design

### 19.1. Breakpoints

| Token | Viewport | Descripción |
|---|---|---|
| `sm` | < 640px | Mobile pequeño (iPhone SE, iPhone 14) |
| `md` | 640–1023px | Mobile grande / Tablet portrait |
| `lg` | 1024–1279px | Tablet landscape / Laptop 13" |
| `xl` | 1280–1535px | Desktop estándar 15"+ |
| `2xl` | ≥ 1536px | Monitor grande 24"+ |

### 19.2. Adaptaciones por breakpoint

**2xl / xl — Desktop grande y estándar:**
- Sidebar expandida 240px. Siempre visible.
- KpiGrid: 6 columnas.
- Gráficos y tablas en grid 2 columnas.
- Page header con controles completos visible.
- Tablas con todas las columnas visibles.
- Sheet lateral usa 600px de ancho.

**lg — Tablet landscape / Laptop:**
- Sidebar colapsada a íconos (64px). Toggle para expandir.
- KpiGrid: 3 columnas.
- Gráficos en pantalla completa, tablas debajo.
- Filtros en dropdown/popover en lugar de inline.

**md — Tablet portrait:**
- Sidebar oculta, accesible como drawer desde botón hamburger en topbar.
- KpiGrid: 2 columnas.
- Gráficos 100% de ancho, altura reducida 20%.
- Tablas con columnas prioritarias (ocultar columnas de menor importancia).

**sm — Mobile:**
- Sidebar reemplazada por bottom navigation bar.
- KpiGrid: 2 columnas, scroll horizontal desactivado.
- Gráficos: 100% ancho, altura 160px.
- Tablas: 3 columnas clave + botón "+" para expandir la fila y ver el resto.
- Filtros detrás de un FAB con ícono de filtro.
- Sheet: pantalla completa (bottom sheet deslizable).
- Todos los tap targets: mínimo 44×44px.

### 19.3. Tablas en mobile

El comportamiento de tablas en mobile es crítico porque son el componente más frecuente:

**Modo de colapso responsivo:**
Las columnas tienen una prioridad explícita (1 = siempre visible, 2 = visible en md+, 3 = visible en lg+). En mobile solo se ven las columnas con prioridad 1. Un botón `▸` al inicio de cada fila expande la fila y muestra las columnas ocultas en formato vertical (label: valor).

**Alternativa — scroll horizontal controlado:**
Para tablas de alta densidad, el contenedor permite scroll horizontal con un indicador de "desliza para ver más →" visible en el borde derecho.

---

## 22. Mobile para Gerencia

### 20.1. Principio

Los gerentes no acceden al portal desde un laptop con doble monitor. Lo consultan entre reuniones, en el aeropuerto, en el auto. La experiencia mobile no es una concesión — es la experiencia diseñada para el usuario más importante.

### 20.2. Bottom Navigation Bar

5 ítems fijos, siempre visible en la parte inferior de la pantalla en mobile:

```
┌──────────────────────────────────────────────────────────────┐
│  🏠        🎛️         🔔         📈         ···             │
│  Home    Cockpit    Alertas   Ventas      Más               │
└──────────────────────────────────────────────────────────────┘
```

- "Más" abre un bottom sheet con el resto de módulos.
- Los ítems con alertas activas muestran badge numérico rojo.
- La barra usa `safe-area-inset-bottom` para respetar el home indicator de iOS.
- Fondo: `var(--brand-sidebar)`. Ícono activo: `var(--brand-primary)`. Ícono inactivo: blanco/50.

### 20.3. Carga priorizada en mobile

El Home mobile tiene una estrategia de carga en dos fases:

**Fase 1 (inmediata desde caché TanStack Query):**
Los 4 KPIs críticos se muestran instantáneamente con los datos del caché de la visita anterior. Se muestran con un indicador sutil de "datos guardados" hasta que refresca.

**Fase 2 (datos frescos):**
El resto del Home carga con los datos actualizados. Una animación suave actualiza los KPIs si los valores cambiaron.

### 20.4. Gestos nativos

- **Pull-to-refresh:** en cualquier pantalla de lista o dashboard. Inicia un refetch de todos los datos de la página.
- **Swipe desde borde izquierdo:** abre el navigation drawer.
- **Swipe horizontal en tablas:** revela columnas adicionales.
- **Tap en badge del topbar:** navega directamente al Alert Center.

### 20.5. Vista mobile del Home

En mobile el Home se reorganiza verticalmente:

```
[DataFreshnessTag prominente]

[KPI 1]  [KPI 2]
[KPI 3]  [KPI 4]

[Ventas del período — gráfico línea, 160px]

[3 alertas críticas — lista compacta]

[Ver Cockpit completo →]
```

Los KPIs 5 y 6 del Home desktop se ocultan en mobile para reducir el scroll inicial.

### 20.6. Notificaciones push (futuro)

El diseño prevé soporte de push notifications para:
- Alertas críticas (stock cero, error de extractor).
- Resúmenes diarios (7am: "El negocio ayer: $X en ventas, X pedidos").
- Acción asignada al usuario.

---

## 23. Dark Mode

### 21.1. Estrategia de activación

**Fuente de verdad:** `uiStore.theme` con valores `'light' | 'dark' | 'system'`.

El modo `'system'` respeta `prefers-color-scheme` del OS. En el primer acceso, el default es `'system'`. El usuario puede sobreescribir en el dropdown del avatar del topbar.

La preferencia persiste en `localStorage` (`'theme': 'dark'`). Al montar la app, se lee antes de renderizar el árbol React para evitar flash.

### 21.2. Implementación

La clase `dark` se aplica al elemento `<html>`. Tailwind usa `darkMode: 'class'`. Los CSS custom properties del branding se mantienen iguales — el dark mode no afecta los colores del tenant, solo los colores de interfaz.

Transición: `transition-colors duration-200` en los componentes de fondo y texto para cambio suave.

### 21.3. Paleta Dark Mode

| Token CSS / Tailwind | Light | Dark |
|---|---|---|
| Fondo base | `#F8FAFC` | `#0B1120` |
| Superficie (cards) | `#FFFFFF` | `#162032` |
| Superficie elevada (dropdowns) | `#FFFFFF` | `#1E2D45` |
| Borde | `#E2E8F0` | `#1E3A5F` |
| Texto primario | `#0F172A` | `#F1F5F9` |
| Texto muted | `#64748B` | `#7FA3C4` |
| Sidebar | `var(--brand-sidebar)` | Versión 15% más oscura del sidebar |
| Input fondo | `#FFFFFF` | `#162032` |
| Input borde | `#CBD5E1` | `#2D4A6A` |

### 21.4. Gráficos ECharts en dark mode

Los charts reciben la prop `theme` del store (`'light' | 'dark'`). ECharts tiene un tema dark built-in que ajusta automáticamente: fondo del tooltip, colores de ejes, grillas.

Configuraciones explícitas que se mantienen en dark:
- Color de las series de datos: `var(--brand-primary)` no cambia (es identidad del tenant).
- Colores semánticos (éxito/error/advertencia): no cambian.
- Fondo de los charts: siempre `transparent`.

### 21.5. Componentes shadcn/ui en dark mode

shadcn/ui usa CSS custom properties que responden automáticamente a la clase `dark`. No se necesita configuración adicional en los componentes. Solo se necesita asegurar que los tokens del design system de DataBision estén correctamente mapeados a las variables de shadcn.

### 21.6. Imágenes y logos en dark mode

El logo del tenant puede tener versiones para light y dark mode si el cliente provee ambas URLs en el tenant config (`logo_url` y `logo_dark_url`). Si no se provee logo dark, se usa el mismo logo con un drop shadow sutil para asegurar visibilidad.

---

## 24. Branding por Cliente

### 22.1. Fuente de verdad

`GET /api/tenant/config` retorna el objeto de branding:

```json
{
  "company_name": "Comercial Torres S.A.",
  "logo_url": "https://cdn.databision.app/tenants/torres/logo.svg",
  "logo_dark_url": "https://cdn.databision.app/tenants/torres/logo-dark.svg",
  "favicon_url": "https://cdn.databision.app/tenants/torres/favicon.ico",
  "primary_color": "#1D4ED8",
  "secondary_color": "#64748B",
  "sidebar_color": "#0F172A",
  "tagline": "Tu operación en tiempo real",
  "plan": "business",
  "modules": ["sales", "customers", "products", "inventory", "cockpit", "alerts"]
}
```

El campo `modules` controla qué ítems aparecen en la sidebar. Un módulo no contratado simplemente no aparece en la navegación — no se muestra un "lock" ni mensaje de upgrade (eso se maneja desde admin.databision.app).

### 22.2. BrandingLoader

Componente que se ejecuta antes de renderizar el árbol de rutas. Proceso:

1. Lee `localStorage` para ver si hay un tenant config cacheado. Si hay, aplica instantáneamente (evita flash).
2. Ejecuta `GET /api/tenant/config`.
3. Al recibir respuesta: actualiza `tenantStore`, aplica CSS custom properties, actualiza title y favicon.
4. Si el API falla y hay cache: usa el cache con un banner de advertencia "Usando configuración guardada".
5. Si el API falla y no hay cache: muestra página de error con logo genérico DataBision.

### 22.3. Aplicación de CSS custom properties

```css
:root {
  --brand-primary: <primary_color>;
  --brand-secondary: <secondary_color>;
  --brand-sidebar: <sidebar_color>;
  --brand-primary-hover: <primary_color con 10% más oscuro>;
  --brand-primary-light: <primary_color con 90% de opacidad sobre blanco>;
}
```

Las variables se calculan en runtime en JavaScript usando manipulación de color (la librería `color2k` o similar) para generar los estados hover y light automáticamente sin que el admin deba configurarlos.

### 22.4. Logo del cliente

- Bounding box en sidebar: 140×40px. `object-fit: contain`, sin forzar dimensiones.
- Bounding box en login: 200×80px. `object-fit: contain`.
- Si no hay logo: nombre de la empresa en tipografía Inter 600.
- Formatos soportados: SVG (preferido), PNG, WEBP.
- Los SVGs se renderizan como `<img>` (no inline) para no exponer el markup del SVG al DOM del portal.

### 22.5. Favicon dinámico

Si `favicon_url` está definido, se actualiza el `<link rel="icon">` del documento en el BrandingLoader. Si no, se usa el favicon genérico DataBision.

### 22.6. Límites del white-label

| Elemento | Configurable |
|---|---|
| Logo | Sí |
| Favicon | Sí |
| Color primario | Sí |
| Color sidebar | Sí |
| Tagline del login | Sí |
| Tipografía | No — siempre Inter |
| Layout y spacing | No |
| Colores semánticos (éxito/error) | No — son del design system |
| Estructura de navegación | No — solo se activan/desactivan módulos |

### 22.7. Módulos habilitados por plan

| Plan | Módulos disponibles |
|---|---|
| **Starter** | Home · Ventas · Sync Center |
| **Business** | + Clientes · Productos · Inventario · Cockpit · Alertas |
| **Advanced** | + Dashboard · Live Layer · Insights · Actions |

---

## 25. Design System y Componentes Transversales

### 23.1. Paleta de colores de la plataforma

**Colores de plataforma (fijos, no sobreescribles por branding):**

| Token | Valor | Uso |
|---|---|---|
| `background` | `#F8FAFC` | Fondo base de la app |
| `surface` | `#FFFFFF` | Cards, panels |
| `border` | `#E2E8F0` | Bordes de cards y separadores |
| `text-primary` | `#0F172A` | Texto principal |
| `text-muted` | `#64748B` | Etiquetas, texto secundario |
| `success` | `#16A34A` | Positivo, OK |
| `error` | `#DC2626` | Error, negativo |
| `warning` | `#D97706` | Advertencia |
| `info` | `#2563EB` | Información |

**Colores de branding (per-tenant):**

| Token CSS | Default | Configurable |
|---|---|---|
| `--brand-primary` | `#2563EB` | Sí |
| `--brand-secondary` | `#64748B` | Sí |
| `--brand-sidebar` | `#0F172A` | Sí |

### 23.2. Tipografía

**Familia:** Inter (Google Fonts). Cargada con `font-display: swap`.

| Rol | Tamaño | Peso | Uso |
|---|---|---|---|
| Heading 1 | 24px | 700 | Títulos de página |
| Heading 2 | 18px | 600 | Títulos de sección |
| Heading 3 | 16px | 600 | Títulos de card |
| Body | 14px | 400 | Texto general |
| Label | 13px | 500 | Labels de formulario, badges |
| Caption | 12px | 400 | Metadata, timestamps |
| Numbers | 32px/24px/18px | 700 | KPIs (tabular-nums) |

### 23.3. Espaciado

Base unit: 4px. Escala: 4 · 8 · 12 · 16 · 20 · 24 · 32 · 40 · 48 · 64.

Card padding: 20px. Section gap: 24px. Row height tablas: 44px.

### 23.4. Border radius

| Elemento | Radius |
|---|---|
| Cards | 8px |
| Buttons | 6px |
| Inputs | 6px |
| Badges / chips | 9999px (pill) |
| Gráficos (contenedor) | 8px |
| Tooltips | 6px |

### 23.5. Sombras

Solo `shadow-sm` (`0 1px 2px rgba(0,0,0,0.05)`). No shadow-md ni shadow-lg en elementos de datos. La jerarquía se logra con color de fondo, no con sombras.

### 23.6. Componentes base

**KpiCard**
Props conceptuales: `label` · `value` (formateado) · `delta` (valor + direction) · `deltaSemantics` (`positive-up | positive-down`) · `sparkline` (array) · `icon` · `loading` · `onClick`.
- En loading: shimmer animado que respeta el tamaño de la card.
- El click es opcional (navega al módulo relevante).

**DataFreshnessTag**
- Verde: < 2h · Amarillo: 2–6h · Rojo: > 6h · Gris pulsante: error activo.
- Tooltip en hover: detalle por extractor.
- Click: navega a Sync Center.

**FilterBar**
- Aparece bajo el Page Header cuando hay filtros activos.
- Chip por filtro con valor visible y botón × para remover.
- "Limpiar todo" si hay más de 1 filtro.

**StatusBadge**
- Variantes de color: success · error · warning · info · neutral.
- Tamaño: sm (11px) · md (12px) · lg (13px).
- Con o sin ícono puntual (dot).

**EmptyState**
Variantes:
- `onboarding`: primera vez. SVG + título + descripción + CTA.
- `no-results`: filtros sin resultado. Texto + "Limpiar filtros".
- `no-permission`: acceso denegado. Texto + link a settings.
- `error`: fetch fallido. Texto + "Reintentar".

**LoadingSkeleton**
Variantes: `kpi-grid` · `table` (N filas) · `chart` · `page`.
Animación shimmer con `@keyframes shimmer` de izquierda a derecha.

---

## 26. Patrones de Interacción

### 24.1. Drill-down

Patrón consistente en toda la plataforma. Click en un elemento de una visualización (barra de gráfico, fila de tabla, segmento de donut, punto de scatter) aplica ese elemento como filtro y navega a la vista de detalle correspondiente. La URL refleja el filtro: `/sales/by-customer?customer_id=123`.

El breadcrumb en el topbar siempre refleja la ruta de drill-down actual con cada nivel clickeable para remontar.

### 24.2. Persistencia de filtros en URL

Todos los filtros activos se serializan en query string. Esto garantiza:
- Compartir una vista exacta con un colega.
- Bookmarking de vistas frecuentes.
- Volver con el botón atrás del navegador a la vista exacta anterior.
- El servidor puede renderizar el estado correcto si se implementa SSR en el futuro.

### 24.3. Período global vs período de módulo

Existe un período global en `uiStore.globalPeriod` que todos los módulos respetan por defecto. Cada módulo puede tener su propio período local que sobreescribe el global (con indicador visual "Período personalizado" en el page header).

### 24.4. Toast notifications

Todas las acciones con efecto muestran un toast en la esquina inferior derecha:
- Ícono de resultado (✓ verde / ✗ rojo).
- Mensaje descriptivo corto.
- Auto-dismiss a los 4 segundos.
- Botón "Deshacer" si la acción es reversible (con timeout de 5 segundos).
- Máximo 3 toasts simultáneos — los nuevos reemplazan los más antiguos.

### 24.5. Confirmación de acciones destructivas

Las acciones irreversibles (desestimar alerta, eliminar acción, cerrar insight) abren un Dialog (modal de confirmación):
- Título: "¿Confirmar [acción]?" en 16px/600.
- Descripción del impacto en lenguaje natural.
- Dos botones: "Cancelar" (outline, prominente) + "Confirmar" (filled, rojo si destructivo).
- No se puede confirmar pulsando Enter accidentalmente — el botón de confirmación tiene `autofocus={false}`.

### 24.6. Feedback de carga en botones

Todos los botones de acción que disparan una llamada a API:
- Al hacer click: el texto del botón se reemplaza con un spinner del mismo tamaño.
- El botón se deshabilita para prevenir doble click.
- El tamaño del botón no cambia (el spinner reemplaza el texto, no se añade al lado).

### 24.7. Manejo global de errores

**401 Unauthorized:** el interceptor de Axios intenta el refresh token. Si falla, limpia el store de auth y redirige a `/login` con toast "Tu sesión expiró. Ingresa nuevamente."

**403 Forbidden:** el componente muestra `EmptyState` variante `no-permission`. No redirige. No rompe el layout.

**5xx:** el componente de datos muestra `EmptyState` variante `error` con botón "Reintentar". No colapsa la página completa.

**Sin conexión:** TanStack Query muestra datos del caché. Banner amarillo no-intrusivo en el topbar: "Sin conexión — mostrando datos guardados". El DataFreshnessTag cambia a estado de advertencia.

---

## 27. Estructura de Archivos Frontend

```
databision-frontend/src/
│
├── App.tsx                       ← Detección de subdominio → AdminApp | PortalApp
│
├── apps/
│   ├── admin/                    ← admin.databision.app
│   │   ├── AdminApp.tsx
│   │   └── pages/ components/ api/ store/
│   │
│   └── portal/                   ← {slug}.databision.app
│       ├── PortalApp.tsx
│       │
│       ├── pages/
│       │   ├── LoginPage.tsx
│       │   ├── SelectCompanyPage.tsx
│       │   ├── home/
│       │   │   └── HomePage.tsx
│       │   ├── dashboard/
│       │   │   └── DashboardPage.tsx
│       │   ├── cockpit/
│       │   │   └── CockpitPage.tsx
│       │   ├── live/
│       │   │   └── LiveLayerPage.tsx
│       │   ├── alerts/
│       │   │   └── AlertCenterPage.tsx
│       │   ├── insights/
│       │   │   └── InsightsPage.tsx
│       │   ├── actions/
│       │   │   └── ActionsPage.tsx
│       │   ├── sales/
│       │   │   ├── SalesOverviewPage.tsx
│       │   │   ├── SalesByPeriodPage.tsx
│       │   │   ├── SalesBySalespersonPage.tsx
│       │   │   └── SalesByCustomerPage.tsx
│       │   ├── customers/
│       │   │   ├── CustomersOverviewPage.tsx
│       │   │   ├── ArAgingPage.tsx
│       │   │   └── CustomerDetailPage.tsx
│       │   ├── products/
│       │   │   ├── ProductsOverviewPage.tsx
│       │   │   ├── ProductMarginsPage.tsx
│       │   │   ├── SlowMovingPage.tsx
│       │   │   └── ProductDetailPage.tsx
│       │   ├── salesreps/
│       │   │   ├── SalesRepsOverviewPage.tsx
│       │   │   ├── SalesRepsComparisonPage.tsx
│       │   │   ├── SalesRepsPerformancePage.tsx
│       │   │   └── SalesRepDetailPage.tsx
│       │   ├── inventory/
│       │   │   ├── InventoryOverviewPage.tsx
│       │   │   ├── InventoryByWarehousePage.tsx
│       │   │   └── StockMovementsPage.tsx
│       │   └── sync/
│       │       └── SyncCenterPage.tsx
│       │
│       └── components/
│           ├── layout/
│           │   ├── PortalLayout.tsx
│           │   ├── Sidebar.tsx
│           │   ├── Topbar.tsx
│           │   ├── MobileBottomNav.tsx
│           │   └── PageHeader.tsx
│           ├── kpi/
│           │   ├── KpiCard.tsx
│           │   └── KpiGrid.tsx
│           ├── charts/
│           │   ├── RevenueLineChart.tsx
│           │   ├── BarChart.tsx
│           │   ├── StackedBarChart.tsx
│           │   ├── DonutChart.tsx
│           │   ├── HeatmapChart.tsx
│           │   ├── ScatterChart.tsx
│           │   └── SparklineChart.tsx
│           ├── filters/
│           │   ├── PeriodSelector.tsx
│           │   ├── FilterBar.tsx
│           │   └── MultiSelectFilter.tsx
│           ├── alerts/
│           │   ├── AlertCard.tsx
│           │   └── AlertFeed.tsx
│           ├── actions/
│           │   ├── ActionCard.tsx
│           │   ├── ActionKanban.tsx
│           │   └── ActionSheet.tsx
│           ├── insights/
│           │   ├── InsightCard.tsx
│           │   └── RiskIndicatorPanel.tsx
│           ├── sync/
│           │   ├── ExtractorStatusCard.tsx
│           │   └── DataFreshnessTag.tsx
│           └── shared/
│               ├── EmptyState.tsx
│               ├── LoadingSkeleton.tsx
│               ├── StatusBadge.tsx
│               └── ConfirmDialog.tsx
│
├── hooks/
│   ├── useKpis.ts
│   ├── useSalesAnalytics.ts
│   ├── useCustomerAnalytics.ts
│   ├── useProductAnalytics.ts
│   ├── useInventoryAnalytics.ts
│   ├── useSalesRepsAnalytics.ts
│   ├── useDashboardData.ts
│   ├── useCockpitData.ts
│   ├── useLiveLayer.ts
│   ├── useAlerts.ts
│   ├── useInsights.ts
│   ├── useActions.ts
│   └── useSyncStatus.ts
│
├── stores/
│   ├── authStore.ts
│   ├── tenantStore.ts             ← branding + módulos habilitados
│   └── uiStore.ts                 ← tema dark/light, sidebar, período global
│
├── lib/
│   ├── api.ts                     ← instancia Axios + interceptores
│   ├── theme.ts                   ← lógica de branding runtime
│   └── formatters.ts              ← números, fechas, monedas
│
└── types/
    └── index.ts                   ← todos los tipos TypeScript del portal
```

---

## 28. Glosario SAP B1 → UI

La interfaz habla el idioma del negocio, no del ERP. Los usuarios finales no son consultores SAP.

| Concepto SAP B1 | Nombre en UI DataBision | Módulo |
|---|---|---|
| OINV (Facturas de venta) | Facturas / Ventas | Ventas |
| ORDR (Pedidos de venta) | Pedidos | Cockpit / Ventas |
| ODLN (Entregas) | Entregas | Cockpit |
| OCRD (Socios de negocio — clientes) | Clientes | Clientes |
| OITM (Ítems del inventario) | Productos / Ítems | Productos / Inventario |
| OWTQ / OIVL (Movimientos stock) | Movimientos de inventario | Inventario |
| OPCH (Facturas de proveedor) | Compras (futuro) | — |
| OVPM / OCRP (Pagos recibidos) | Cobros | Clientes |
| DocDate | Fecha del documento | Global |
| DocTotal | Monto total | Global |
| UpdateTS | Última modificación (interna) | Sync Center |
| Business Partner Code | ID Cliente / Código | Clientes |
| ItemCode | Código de producto | Productos |
| WhsCode | Almacén | Inventario |
| SlpCode / SlpName | Vendedor | Ventas |
| GroupCode / GroupName | Categoría / Grupo | Productos |

---

*Documento vivo — actualizar al agregar nuevos módulos o cambiar decisiones de diseño.*
*Versión 2.0 — 2026-05-29 — Lead UX Architect*
