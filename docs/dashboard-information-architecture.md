# DataBision — Dashboard Information Architecture

**Versión:** 1.0  
**Fecha:** 2026-06-03  
**Autor:** Lead UX Architect  
**Estado:** Documento de diseño — sin implementación  
**Relacionado con:** `frontend-ux-architecture.md` · `native-bi-screen-specs.md`

---

## Índice

1. [Sitemap Completo](#1-sitemap-completo)
2. [Jerarquía de Contenido por Módulo](#2-jerarquía-de-contenido-por-módulo)
3. [KPIs por Pantalla](#3-kpis-por-pantalla)
4. [Filtros por Pantalla](#4-filtros-por-pantalla)
5. [Matriz Módulo × Plan Comercial](#5-matriz-módulo--plan-comercial)
6. [Flujo de Datos SAP B1 → UI](#6-flujo-de-datos-sap-b1--ui)
7. [Jerarquía de Navegación por Rol](#7-jerarquía-de-navegación-por-rol)
8. [Árbol de Decisión de Drill-Down](#8-árbol-de-decisión-de-drill-down)
9. [Mapa de Alertas y Orígenes](#9-mapa-de-alertas-y-orígenes)
10. [Relaciones entre Módulos](#10-relaciones-entre-módulos)

---

## 1. Sitemap Completo

### 1.1. Portal Cliente — `{slug}.databision.app`

```
{slug}.databision.app
│
├── /login                                Login por tenant
├── /select-company                       Selector de empresa (multi-empresa)
│
├── / (Home)                              Home ejecutivo
├── /dashboard                            Dashboard ejecutivo consolidado
│
├── /cockpit                              Operational Cockpit (estado del día)
├── /live                                 Operational Live Layer (tiempo real)
├── /alerts                               Alert Center
│   └── /alerts/settings                  Configuración de umbrales
├── /insights                             Recommendations & Insights
└── /actions                              Business Actions
│
├── /sales                                Módulo Ventas
│   ├── /sales/overview                   Vista general
│   ├── /sales/by-period                  Por período
│   ├── /sales/by-salesperson             Por vendedor
│   ├── /sales/by-customer                Por cliente
│   └── /sales/documents                  Documentos emitidos
│
├── /customers                            Módulo Clientes
│   ├── /customers/overview               Vista general
│   ├── /customers/ar-aging               Cuentas por cobrar
│   ├── /customers/at-risk                En riesgo / sin compra reciente
│   └── /customers/:id                    Perfil de cliente
│
├── /products                             Módulo Productos
│   ├── /products/overview                Vista general
│   ├── /products/margins                 Márgenes
│   ├── /products/slow-moving             Sin rotación
│   └── /products/:id                     Perfil de producto
│
├── /salesreps                            Módulo Vendedores
│   ├── /salesreps/overview               Vista general y ranking
│   ├── /salesreps/performance            Cumplimiento y metas
│   ├── /salesreps/comparison             Comparación mensual
│   └── /salesreps/:id                    Perfil de vendedor
│
├── /inventory                            Módulo Inventario
│   ├── /inventory/overview               Vista general
│   ├── /inventory/by-warehouse           Por almacén
│   ├── /inventory/critical               Stock crítico
│   └── /inventory/movements              Movimientos
│
├── /sync                                 Sync Center
│
└── /settings                             Configuración
    ├── /settings/users                   Usuarios y roles
    ├── /settings/branding                Branding del portal
    ├── /settings/modules                 Módulos habilitados
    ├── /settings/company                 Datos de empresa SAP
    ├── /settings/connection              Conexión SAP / extractor
    ├── /settings/alerts                  Umbrales de alertas
    └── /settings/sync                    Frecuencia de sincronización
```

### 1.2. Admin Panel — `admin.databision.app`

```
admin.databision.app
│
├── /                                     Dashboard SuperAdmin
├── /companies                            Gestión de empresas (tenants)
│   ├── /companies/new                    Nueva empresa
│   └── /companies/:id                    Detalle de empresa
│       ├── /companies/:id/users          Usuarios de la empresa
│       ├── /companies/:id/modules        Módulos contratados
│       └── /companies/:id/extractors     Extractores configurados
├── /users                                Todos los usuarios
├── /audit                                Log de auditoría
└── /billing (futuro)                     Facturación
```

---

## 2. Jerarquía de Contenido por Módulo

### 2.1. Home Ejecutivo

```
Home
├── [L1] Resumen de estado global
│    └── DataFreshnessTag + status extractor
├── [L1] KPI Strip (6 métricas críticas)
│    ├── Ventas del período
│    ├── Margen bruto %
│    ├── Pedidos pendientes
│    ├── CxC vencida
│    ├── Rotación inventario
│    └── Clientes activos
├── [L2] Gráfico ventas del período (RevenueLineChart)
├── [L2] Estado operacional compacto (resumen Cockpit)
├── [L3] Top 5 Clientes (tabla)
├── [L3] Alertas activas (feed 5 ítems)
└── [L3] Sync status (chip)
```

### 2.2. Dashboard Ejecutivo

```
Dashboard
├── [L0] Controles: Período · Comparar con · Granularidad · Exportar
├── [L1] KPI Strip 6+6 (comercial + operacional)
├── [L1] Ventas del mes (número grande + delta)
├── [L1] Ventas acumuladas YTD (número grande + delta)
├── [L2] Gráfico ventas por día (BarChart o LineChart)
├── [L2] Comparación período anterior (líneas superpuestas)
├── [L2] Top 5 Clientes (tabla rankeada)
├── [L2] Top 5 Productos (tabla rankeada)
├── [L2] Top 5 Vendedores (tabla rankeada)
├── [L3] Documentos emitidos (facturas, pedidos, NC)
├── [L3] Ticket promedio (KpiCard con sparkline)
├── [L3] Notas de crédito (conteo + % sobre ventas)
└── [L3] Márgenes por categoría (BarChart horizontal)
```

### 2.3. Módulo Ventas

```
Ventas
├── [L0] Filtros globales del módulo
├── [L1] Overview
│    ├── KPI Strip (5 métricas)
│    ├── Gráfico principal (RevenueLineChart)
│    ├── Donut por categoría
│    └── Tabla Top 10 ítems
├── [L2] Por Período
│    ├── BarChart agrupado (actual vs anterior vs año anterior)
│    ├── Línea de tendencia
│    ├── Tabla por período
│    └── Heatmap día × hora
├── [L2] Por Vendedor
│    ├── BarChart horizontal ranking
│    ├── Tabla de vendedores
│    └── Drilldown Sheet por vendedor
├── [L2] Por Cliente
│    ├── BarChart horizontal ranking
│    ├── Tabla de clientes
│    └── Link a /customers/:id
└── [L2] Documentos
     ├── Facturas emitidas (tabla)
     ├── Pedidos (tabla)
     └── Notas de crédito (tabla)
```

### 2.4. Módulo Clientes

```
Clientes
├── [L1] Overview
│    ├── KPI Strip (4 métricas)
│    ├── ScatterChart segmentación (frecuencia × monto)
│    └── Tabla de clientes con segmento
├── [L2] AR Aging
│    ├── KPI Strip CxC
│    ├── StackedBarChart aging por cliente
│    └── Tabla aging con drilldown por facturas
├── [L2] En Riesgo
│    ├── Lista de clientes sin compra en N días
│    ├── Valor en riesgo (suma histórica)
│    └── Acción sugerida (reactivar)
└── [L3] Perfil de Cliente (/customers/:id)
     ├── Header: datos maestros
     └── Tabs: Resumen · Pedidos · Facturas · CxC · Acciones
```

### 2.5. Módulo Productos

```
Productos
├── [L1] Overview
│    ├── KPI Strip (4 métricas)
│    ├── Tabla de productos con filtros
│    └── Donut por categoría
├── [L2] Márgenes
│    ├── BarChart horizontal por margen %
│    ├── Línea de referencia (promedio)
│    └── Tabla margen detalle
├── [L2] Sin Rotación
│    ├── KPI valor inmovilizado
│    ├── Tabla slow-movers
│    └── Acción sugerida
└── [L3] Perfil de Producto (/products/:id)
     └── Tabs: Desempeño · Márgenes · Inventario · Sustitutos
```

### 2.6. Módulo Vendedores

```
Vendedores
├── [L1] Overview / Ranking
│    ├── KPI Strip (4 métricas)
│    ├── BarChart horizontal (ranking por ventas)
│    ├── Tabla con cumplimiento (si hay metas)
│    └── Período seleccionable
├── [L2] Cumplimiento (si plan Business+)
│    ├── Gauge o BarChart meta vs real por vendedor
│    ├── % cumplimiento
│    └── Tendencia mensual
├── [L2] Comparación Mensual
│    ├── BarChart agrupado por mes
│    ├── Heatmap actividad
│    └── Tabla comparativa
└── [L3] Perfil de Vendedor (/salesreps/:id)
     ├── Header: nombre, ventas totales, ranking
     └── Tabs: Ventas · Clientes · Productos · Meses
```

### 2.7. Módulo Inventario

```
Inventario
├── [L1] Overview
│    ├── KPI Strip (4 métricas)
│    ├── Tabla con Status (Normal/Bajo/Crítico/Sin stock)
│    └── Donut valor por categoría
├── [L2] Por Almacén
│    ├── Tabla resumen por almacén
│    └── Drilldown a ítems del almacén
├── [L2] Stock Crítico
│    ├── Tabla filtrada (stock < mínimo o = 0)
│    └── Sugerencia reposición
└── [L2] Movimientos
     ├── Filtros: almacén · ítem · tipo · período
     ├── Tabla cronológica
     └── BarChart entradas vs salidas
```

### 2.8. Sync Center

```
Sync Center
├── [L0] Banner status global
├── [L1] Estado por extractor (ExtractorStatusCard)
├── [L1] Estado Service Layer (si Modalidad B)
├── [L2] Historial de ejecuciones (tabla paginada)
└── [L2] Errores y logs (lista con stack colapsable)
```

### 2.9. Alert Center

```
Alert Center
├── [L0] Resumen: Críticas · Operativas · Comerciales
├── [L1] Lista de alertas activas (por severidad)
│    └── Acciones: Resolver · Silenciar · Asignar · Crear acción
├── [L2] Historial (tab)
└── [L2] Configuración (link → /settings/alerts)
```

### 2.10. Configuración

```
Settings
├── Usuarios → tabla + crear/editar usuario · asignar rol
├── Roles → permisos por módulo
├── Branding → logo · color primario · sidebar color · tagline
├── Módulos → toggle de módulos habilitados (según plan)
├── Empresa SAP → nombre, código, ambiente (productivo/sandbox)
├── Conexión → URL Service Layer · API Key extractor · test de conexión
├── Alertas → umbrales por tipo de alerta
└── Sincronización → frecuencia · objetos activos · ventana de horario
```

---

## 3. KPIs por Pantalla

### 3.1. Home Ejecutivo

| KPI | Fuente | Tipo | Semántica |
|---|---|---|---|
| Ventas del período | OINV DocTotal (período MTD) | Moneda | ↑ positivo |
| Margen bruto % | (Ventas − Costo) / Ventas | Porcentaje | ↑ positivo |
| Pedidos pendientes | ORDR abiertos | Entero | ↓ positivo |
| CxC vencida | OINV sin pagar > 30d | Moneda | ↓ positivo |
| Rotación inventario | Días promedio stock | Días | ↓ positivo |
| Clientes activos | OCRD con OINV en período | Entero | ↑ positivo |

### 3.2. Dashboard Ejecutivo

**Fila comercial:**

| KPI | Cálculo | Semántica |
|---|---|---|
| Ventas del mes (MTD) | Suma OINV DocTotal mes actual | ↑ |
| Ventas acumuladas (YTD) | Suma OINV DocTotal año actual | ↑ |
| Ticket promedio | Ventas / N° facturas | ↑ |
| Clientes activos | Distintos OCRD con OINV | ↑ |
| Nuevos clientes | OCRD con primera OINV en período | ↑ |
| Tasa de retención | Clientes activos / Clientes mes anterior | ↑ |

**Fila operacional:**

| KPI | Cálculo | Semántica |
|---|---|---|
| Pedidos procesados | N° ORDR cerrados en período | ↑ |
| Notas de crédito | N° ORIN emitidas | ↓ positivo (menos = mejor) |
| NC sobre ventas % | Monto ORIN / Monto OINV | ↓ positivo |
| Documentos cancelados | OINV con DocStatus = 'C' | ↓ positivo |
| CxC total | Suma OINV Balance > 0 | ↓ positivo |
| CxC vencida % | CxC >30d / CxC total | ↓ positivo |

### 3.3. Módulo Ventas

| KPI | Cálculo |
|---|---|
| Ventas totales período | Suma DocTotal OINV |
| Facturas emitidas | Count OINV |
| Ticket promedio | DocTotal promedio |
| Margen bruto % | (DocTotal − LineTotal costo) / DocTotal |
| Clientes únicos | Distinct CardCode OINV |

### 3.4. Módulo Clientes

| KPI | Cálculo |
|---|---|
| Clientes activos | OCRD con OINV en período |
| Nuevos clientes | OCRD con primera OINV en período |
| Clientes en riesgo | Sin OINV en últimos N días (configurable) |
| Total CxC | Suma OINV Balance |
| DSO (días) | (CxC / Ventas anuales) × 365 |

### 3.5. Módulo Productos

| KPI | Cálculo |
|---|---|
| Ítems activos en ventas | ItemCode distintos en OINV período |
| Ítem más vendido | ItemCode top por DocTotal |
| Margen promedio catálogo | Promedio margen % por ítem activo |
| Ítems sin venta en período | ItemCode en OITM sin OINV en período |

### 3.6. Módulo Vendedores

| KPI | Cálculo |
|---|---|
| Ventas totales del equipo | Suma DocTotal OINV período |
| Vendedor líder | SlpCode con mayor DocTotal |
| Promedio por vendedor | Total / N° vendedores activos |
| Vendedores activos | SlpCode con mínimo 1 OINV en período |
| % cumplimiento promedio | Si hay metas configuradas |

### 3.7. Módulo Inventario

| KPI | Cálculo |
|---|---|
| Ítems bajo mínimo | OITM donde OnHand < MinLevel |
| Ítems sin stock con demanda | OnHand = 0 AND en ORDR abierto |
| Valor total inventario | Suma (OnHand × AvgPrice) |
| Rotación promedio (días) | Días promedio entre movimientos |

### 3.8. Sync Center

| KPI | Fuente |
|---|---|
| Última sync exitosa | ctl.sync_runs timestamp |
| Registros procesados (última run) | ctl.sync_runs rows_processed |
| Registros insertados | ctl.sync_runs rows_inserted |
| Registros actualizados | ctl.sync_runs rows_updated |
| Registros sin cambio | ctl.sync_runs rows_skipped |
| Duración última sync | ctl.sync_runs duration_ms |
| Errores activos | Count ctl.sync_errors sin resolve |

---

## 4. Filtros por Pantalla

### 4.1. Filtros globales disponibles

Todos los módulos analíticos heredan estos filtros:

| Filtro | Tipo | Valores | Persistencia |
|---|---|---|---|
| Período | Segmented + DatePicker | HOY · MTD · QTD · YTD · 12M · Custom | URL query string |
| Comparar con | Select | Período anterior · Mismo período año ant. | URL query string |

### 4.2. Filtros por módulo

#### Ventas

| Filtro | Tipo UI | Fuente |
|---|---|---|
| Período | PeriodSelector | — |
| Vendedor | MultiSelect con búsqueda | OSLP.SlpName |
| Cliente | MultiSelect con búsqueda | OCRD.CardName |
| Producto / Ítem | MultiSelect con búsqueda | OITM.ItemName |
| Categoría de producto | MultiSelect jerárquico | OITG.ItmsGrpNam |
| Tipo de documento | Segmented | Factura · Pedido · NC |
| Estado del documento | Segmented | Abierto · Cerrado · Cancelado |

#### Clientes

| Filtro | Tipo UI | Fuente |
|---|---|---|
| Período | PeriodSelector | — |
| Vendedor asignado | Select | OSLP.SlpName |
| Segmento | Segmented | Champion · Leal · En riesgo · Inactivo |
| Zona / Región | Select | OCRD.Territory (si configurado) |
| Con saldo vencido | Toggle | Computed |
| Días sin compra | RangeSlider | Computed |

#### Productos

| Filtro | Tipo UI | Fuente |
|---|---|---|
| Período | PeriodSelector | — |
| Grupo / Categoría | MultiSelect | OITG.ItmsGrpNam |
| Con ventas / Sin ventas | Segmented | Computed |
| Margen > % | NumberInput | Computed |
| Stock disponible | Toggle | OITM.OnHand > 0 |

#### Vendedores

| Filtro | Tipo UI | Fuente |
|---|---|---|
| Período | PeriodSelector | — |
| Vendedor específico | MultiSelect | OSLP.SlpName |
| Con ventas en período | Toggle | Computed |

#### Inventario

| Filtro | Tipo UI | Fuente |
|---|---|---|
| Almacén | MultiSelect | OWHS.WhsName |
| Grupo / Categoría | MultiSelect | OITG.ItmsGrpNam |
| Estado stock | Segmented | Normal · Bajo mínimo · Sin stock · Sobre stock |
| Tipo de movimiento | MultiSelect | Entrada · Salida · Ajuste · Transferencia |

#### Sync Center

| Filtro | Tipo UI |
|---|---|
| Extractor | Select |
| Resultado | Segmented (Exitosa · Con errores · Fallida) |
| Período | DateRangePicker |
| Tipo de error | Select |

#### Alert Center

| Filtro | Tipo UI |
|---|---|
| Severidad | Segmented (Crítica · Operativa · Comercial) |
| Estado | Segmented (Activa · Silenciada · Resuelta) |
| Módulo origen | Select |
| Asignada a | Select (usuarios) |

---

## 5. Matriz Módulo × Plan Comercial

### 5.1. Módulos por plan

| Módulo | Starter USD 350 | Business USD 600 | Advanced USD 1.000+ |
|---|---|---|---|
| Login + Selector empresa | ✓ | ✓ | ✓ |
| Home ejecutivo | ✓ | ✓ | ✓ |
| Dashboard ejecutivo | ✗ | ✓ | ✓ |
| Ventas (overview + período) | ✓ básico | ✓ completo | ✓ completo |
| Ventas por vendedor/cliente | ✗ | ✓ | ✓ |
| Módulo Clientes (overview) | ✗ | ✓ | ✓ |
| AR Aging + Riesgo | ✗ | ✓ | ✓ |
| Módulo Productos | ✗ | ✓ | ✓ |
| Módulo Vendedores | ✗ | ✓ | ✓ |
| Módulo Inventario | ✗ | ✓ | ✓ |
| Sync Center (básico) | ✓ lectura | ✓ | ✓ |
| Alert Center | ✓ críticas | ✓ todas | ✓ configurables |
| Operational Cockpit | ✗ | ✓ | ✓ |
| Operational Live Layer | ✗ | ✗ | ✓ |
| Recommendations & Insights | ✗ | ✗ | ✓ |
| Business Actions | ✗ | ✗ | ✓ |
| Settings básicos (usuarios) | ✓ hasta 3 | ✓ hasta 10 | ✓ ilimitado |
| Settings branding | ✓ | ✓ | ✓ |
| Settings alertas custom | ✗ | ✓ | ✓ |
| Exportación PDF/CSV | ✗ | ✓ | ✓ |

### 5.2. Frecuencia de sincronización por plan

| Plan | Frecuencia | Objetos SAP incluidos |
|---|---|---|
| Starter | Cada 2 horas | OINV · INV1 · OCRD · OITM (4 objetos) |
| Business | Cada 60 min | + ORDR · ODLN · ORIN · OSLP (8 objetos) |
| Advanced | Cada 30 min | + OWTQ · OIVL · OPCH · OVPM (12 objetos) |

### 5.3. Módulos del piloto (USD 500/mes por 3 meses)

El piloto incluye el plan Business completo para acelerar el onboarding y reducir fricción de adopción en los primeros clientes. No tiene restricciones de módulos.

---

## 6. Flujo de Datos SAP B1 → UI

### 6.1. Pipeline completo

```
SAP Business One (HANA / SQL Server)
    │
    │   Modalidad A: Dedicated Extractor (.NET Worker)
    │   Modalidad B: Service Layer Delta (Azure Function)
    │
    ▼
DataBision Ingest API
    │  Valida API Key + company_id
    │  Calcula source_hash (SHA-256)
    │  Normaliza timestamps
    │
    ▼
Supabase PostgreSQL
    │
    ├── raw.*         Réplica idempotente sin transformar
    │       raw.oinv, raw.ocrd, raw.oitm, raw.ordr...
    │
    ├── stg.*         Datos limpios y tipados
    │       stg.invoices, stg.customers, stg.items, stg.orders...
    │
    ├── mart.*        Hechos calculados (star schema)
    │       mart.fact_sales, mart.fact_ar, mart.fact_inventory...
    │
    ├── dim.*         Dimensiones conformed
    │       dim.customers, dim.items, dim.salesreps, dim.time...
    │
    ├── ctl.*         Control ETL: checkpoints y sync_runs
    └── audit.*       Log de cambios por company_id
    │
    ▼
DataBision Analytics API (.NET 8)
    │  /api/analytics/kpis
    │  /api/analytics/sales/**
    │  /api/analytics/customers/**
    │  /api/analytics/products/**
    │  /api/analytics/salesreps/**
    │  /api/analytics/inventory/**
    │  /api/sync/status
    │  /api/alerts/**
    │
    ▼
Portal React ({slug}.databision.app)
    │  TanStack Query (caché + stale-while-revalidate)
    │  Zustand (auth + tenant + ui)
    │
    ▼
Usuario final (Gerente / Analista)
```

### 6.2. Tablas SAP → Módulo UI

| Tabla SAP | Módulo UI principal | Módulo UI secundario |
|---|---|---|
| OINV (Facturas) | Ventas | Dashboard · Home |
| INV1 (Líneas factura) | Ventas (margen) | Productos |
| ORDR (Pedidos) | Cockpit · Ventas | Dashboard |
| ODLN (Entregas) | Cockpit | Inventario |
| ORIN (NC de cliente) | Ventas (documentos) | Dashboard |
| OCRD (Socios negocio) | Clientes | Ventas |
| OITM (Ítems) | Productos · Inventario | Ventas |
| OITG (Grupos ítems) | Productos (filtro) | Inventario |
| OSLP (Vendedores) | Vendedores | Ventas |
| OWHS (Almacenes) | Inventario | Sync Center |
| OWTQ / OIVL (Movimientos) | Inventario (movimientos) | — |
| OPCH (Facturas proveedor) | Futuro: Compras | — |
| OVPM (Pagos recibidos) | Clientes (CxC) | Dashboard |

---

## 7. Jerarquía de Navegación por Rol

### 7.1. Gerente General

**Pantalla principal:** Home → Dashboard → Cockpit

**Flujo típico:**
```
Login → Home (pulso rápido)
     → Dashboard (reunión de dirección)
     → Cockpit (situación operacional)
     → Alert Center (si hay badge)
```

**Módulos que NO usa habitualmente:** Sync Center · Settings · Detalle de movimientos.

**Dispositivo:** Mobile (Home, Dashboard simplificado) / Desktop (Dashboard completo).

### 7.2. Gerente Comercial

**Pantalla principal:** Dashboard → Ventas → Clientes → Vendedores

**Flujo típico:**
```
Login → Dashboard (ventas del mes)
     → Ventas / Por Vendedor (seguimiento equipo)
     → Clientes / En Riesgo (oportunidades)
     → Vendedores / Ranking (reunión de equipo)
```

### 7.3. Gerente de Operaciones

**Pantalla principal:** Cockpit → Inventario → Sync Center

**Flujo típico:**
```
Login → Cockpit (pendientes del día)
     → Inventario / Stock Crítico (reposición)
     → Sync Center (estado de datos)
     → Alert Center (errores de sync)
```

### 7.4. Analista de BI

**Pantalla principal:** Todos los módulos — análisis profundo

**Acceso completo a filtros avanzados, tablas completas, exportaciones.**

---

## 8. Árbol de Decisión de Drill-Down

### 8.1. Desde Dashboard → módulo detalle

```
Click en KPI "Ventas" → /sales/overview (filtro período aplicado)
Click en KPI "Clientes activos" → /customers/overview
Click en "Top 5 Clientes" fila → /customers/:id
Click en "Top 5 Productos" fila → /products/:id
Click en "Top 5 Vendedores" fila → /salesreps/:id
Click en KPI "Pedidos pendientes" → /cockpit (sección pedidos)
Click en KPI "CxC vencida" → /customers/ar-aging
```

### 8.2. Desde Ventas → detalle

```
Click en barra de gráfico (período) → tabla filtrada por ese período
Click en vendedor (tabla o gráfico) → /salesreps/:id (Sheet lateral)
Click en cliente (tabla o gráfico) → /customers/:id (Sheet lateral)
Click en categoría de producto (donut) → /products?category=X
```

### 8.3. Desde Alert Center → módulo

```
Alerta stock crítico → /inventory/critical (filtro ítem)
Alerta cliente inactivo → /customers/at-risk (filtro cliente)
Alerta sync error → /sync (filtro extractor)
Alerta venta bajo umbral → /sales/by-period (período afectado)
```

### 8.4. Desde Cockpit → módulo

```
Pedido pendiente → clic "Ver en SAP" (externo) o "Crear acción"
Stock crítico → /inventory/critical
Error integración → /sync
Entrega retrasada → tabla ODLN con fila expandida
```

---

## 9. Mapa de Alertas y Orígenes

### 9.1. Alertas por categoría y fuente

| Alerta | Categoría | Fuente de datos | Umbral configurable | Plan mínimo |
|---|---|---|---|---|
| Extractor detenido | Crítica | ctl.sync_runs | Tiempo sin sync (default: 3h) | Starter |
| Error de sincronización | Crítica | ctl.sync_errors | N° errores consecutivos | Starter |
| Stock a cero con demanda | Crítica | OITM + ORDR | — | Business |
| Margen bajo umbral | Crítica | mart.fact_sales | % configurable | Business |
| CxC >90 días | Crítica | mart.fact_ar | % del total configurable | Business |
| Ventas bajo umbral diario | Operativa | mart.fact_sales | Importe configurable | Business |
| Pedido sin movimiento | Operativa | ORDR | Días configurable (default: 7d) | Business |
| Entrega vencida | Operativa | ODLN | — | Business |
| Stock bajo mínimo | Operativa | OITM | — | Business |
| Cliente sin compra N días | Operativa | OCRD + OINV | Días configurable (default: 60d) | Business |
| CxC vencida 30–60d | Operativa | mart.fact_ar | — | Business |
| Vendedor sin actividad | Operativa | OSLP + OINV | Días configurable | Business |
| Cliente nuevo | Comercial | OCRD + OINV | — | Business |
| Venta sobre ticket promedio | Comercial | mart.fact_sales | % sobre promedio configurable | Business |
| Ítem sin ventas con stock | Comercial | OITM + OINV | Días sin venta configurable | Business |
| Documento cancelado | Comercial | OINV DocStatus | N° documentos configurable | Business |

### 9.2. Ciclo de vida de una alerta

```
GENERADA (condición detectada en sync)
    ↓
ACTIVA (visible en Alert Center)
    ↓
┌──── SILENCIADA (por N horas, reaparece) ──── vuelve a ACTIVA
├──── ASIGNADA (a usuario específico) ──────── sigue ACTIVA
├──── RESUELTA (nota de cierre requerida) ──── → HISTORIAL
└──── DESESTIMADA (nota requerida) ─────────── → HISTORIAL
```

---

## 10. Relaciones entre Módulos

### 10.1. Dependencias de datos

```
  Ventas ────────────────────────────────────────────── fuente primaria
    │
    ├──→ Dashboard Ejecutivo (KPIs agregados)
    ├──→ Home Ejecutivo (KPI strip)
    ├──→ Vendedores (ventas por SlpCode)
    ├──→ Clientes (ventas por CardCode)
    └──→ Productos (ventas por ItemCode)

  Inventario ────────────────────────────────────────── fuente primaria
    │
    ├──→ Cockpit (stock crítico)
    ├──→ Alertas (stock bajo mínimo)
    └──→ Productos (disponibilidad)

  Sync Center ───────────────────────────────────────── fuente primaria
    │
    ├──→ Home (DataFreshnessTag)
    ├──→ Topbar (DataFreshnessTag global)
    ├──→ Alert Center (errores de sync)
    └──→ Live Layer (sincronizaciones en curso)

  Clientes ──────────────────────────────────────────── fuente primaria
    │
    ├──→ Dashboard (top clientes)
    ├──→ AR Aging (CxC)
    ├──→ Cockpit (entregas pendientes)
    └──→ Alertas (clientes inactivos)
```

### 10.2. Entidades compartidas entre módulos

| Entidad | Aparece en | Dato clave |
|---|---|---|
| Cliente (OCRD) | Ventas · Clientes · Cockpit · Dashboard | CardCode, CardName |
| Vendedor (OSLP) | Ventas · Vendedores · Dashboard | SlpCode, SlpName |
| Producto / Ítem (OITM) | Productos · Inventario · Ventas | ItemCode, ItemName |
| Almacén (OWHS) | Inventario · Cockpit | WhsCode, WhsName |
| Período de tiempo | Todos los módulos analíticos | DateRange |

---

*Documento de información arquitectónica — versión 1.0 — 2026-06-03*  
*Relacionado: `frontend-ux-architecture.md` · `native-bi-screen-specs.md`*
