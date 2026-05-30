# DataBision — Master Architecture

**Versión:** 4.0  
**Fecha:** 2026-05-29  
**Estado:** Documento vivo — fuente de verdad arquitectónica  
**Autor:** Chief Architect  

> Este documento es la fuente de verdad. Cuando existe contradicción entre este documento y cualquier otro, este documento tiene precedencia. Los documentos anteriores se mantienen como referencia histórica con nota de estado.

---

## 1. Visión del Producto

DataBision es una **plataforma SaaS de inteligencia operacional para clientes de SAP Business One**. El producto entrega:

- Extracción automatizada e idempotente de datos SAP B1 (tres modalidades de adquisición)
- Base de datos intermedia auditable en Supabase PostgreSQL
- **BI nativo propio** construido con React + ECharts (sin Power BI)
- **Operational Live Layer**: acceso directo a SAP para datos operacionales en tiempo casi real
- Inteligencia operacional: estado de datos, frescura, anomalías
- Motor de alertas proactivas por umbral y condición
- Motor de recomendaciones accionables — inteligencia, no solo reportes
- Módulo de acciones de negocio desde el portal
- Portal multi-tenant con subdominio y branding por empresa

> **Principio rector:** DataBision no es un contenedor de reportes. Es la plataforma operacional del cliente. El cliente debería poder tomar decisiones de negocio desde el portal sin necesidad de entrar a SAP.

---

## 2. Arquitectura de Alto Nivel

```
┌────────────────────────────────────────────────────────────────────────┐
│  ORIGEN: SAP Business One                                                │
│                                                                          │
│  ┌───────────────────┐   ┌──────────────────────────────────────┐       │
│  │ SAP B1 HANA/SQL   │   │ SAP B1 Cloud / Service Layer :50000  │       │
│  │ (on-prem)         │   │                                      │       │
│  └────────┬──────────┘   └──────────┬───────────────────────────┘       │
└───────────┼──────────────────────────┼───────────────────────────────────┘
            │                          │
            │ Mode A          ┌────────┴─────────┐
            │                 │ Mode B   Mode C   │
            ▼                 ▼                   ▼
  ┌─────────────────┐  ┌──────────────┐  ┌─────────────────┐
  │Dedicated Extract│  │SL Delta Wkr  │  │SL Polling Wkr   │
  │.NET 8 Wkr Svc   │  │Pull cola UDT │  │Pull OData filter│
  │On-prem cliente  │  │(Bkgd Worker) │  │(Bkgd Worker)    │
  └────────┬────────┘  └──────┬───────┘  └────────┬────────┘
           └─────────────────┼───────────────────┘
                              │  HTTPS POST JSON + X-DataBision-ApiKey
                              ▼
           ┌──────────────────────────────────────────┐
           │  Ingest API  (ASP.NET Core 8)            │
           │  Valida · hash SHA-256 · normaliza · upsert│
           └──────────────────┬───────────────────────┘
                              │
                              ▼
           ┌──────────────────────────────────────────┐
           │  Supabase PostgreSQL                     │
           │  raw.* · stg.* · dim.* · fact.*          │
           │  ctl.* · oper.* · audit.*                │
           └──────┬──────────────────┬────────────────┘
                  │                  │
           ┌──────▼──────┐   ┌───────▼───────────────────────────────┐
           │   Events    │   │  Background Workers  (.NET IHosted)   │
           │ Processing  │   │  StagingTransformWorker               │
           │   Layer     │   │  HeartbeatMonitorWorker               │
           └──────┬──────┘   │  DataFreshnessWorker                  │
                  │          │  AlertingWorker                        │
                  │          │  RecommendationWorker (F1.5)           │
                  │          └───────────────────────────────────────┘
                  │
                  ▼
           ┌──────────────────────────────────────────┐
           │  DataBision API  (ASP.NET Core 8)        │
           │  Analytics · OpsIntel · Actions          │
           │  Alerts · Recommendations · Live         │
           └──────────────────┬───────────────────────┘
                              │           ▲
                              │           │ Operational Live Layer
                              │     ┌─────┴──────────────────────────┐
                              │     │  Operational Live API          │
                              │     │  Query directa · 30s/60s/120s  │
                              │     └──────────────┬─────────────────┘
                              │                    │  Service Layer OData
                              │                    ▼  (sin ETL, sin caché)
                              │     ┌──────────────────────────────────┐
                              │     │  SAP B1 Service Layer :50000     │
                              │     │  Estado en tiempo real           │
                              │     └──────────────────────────────────┘
                              ▼
           ┌──────────────────────────────────────────┐
           │  DataBision Portal  (React + TypeScript) │
           │  ┌─────────────┐   ┌───────────────────┐ │
           │  │  BI Nativo  │   │  Operational Live │ │
           │  │  ECharts    │   │  /live/*          │ │
           │  └─────────────┘   └───────────────────┘ │
           │  ┌─────────────┐   ┌───────────────────┐ │
           │  │  Alertas    │   │  Recomendaciones  │ │
           │  │  Notif.     │   │  (Fase 1.5+)      │ │
           │  └─────────────┘   └───────────────────┘ │
           │  ┌─────────────────────────────────────┐  │
           │  │      Business Actions Module        │  │
           │  └─────────────────────────────────────┘  │
           └──────────────────────────────────────────┘
```

---

## 3. Capas del Sistema

### 3.1 Acquisition Strategy

La capa de adquisición mueve datos desde SAP B1 hacia el Ingest API. Tres modos coexisten. La decisión de qué modo usar es técnica y depende de la infraestructura del cliente.

#### Prioridad Oficial DataBision

```
1º  Mode A — Dedicated Extractor      (preferido cuando es posible)
2º  Mode B — Service Layer Delta       (cuando no se puede instalar agente)
3º  Mode C — Service Layer Polling     (último recurso / PoC / entrada)
```

Esta jerarquía no es arbitraria. Mode A elimina la dependencia de Service Layer para la extracción analítica, lo que reduce la exposición a los límites de rate y la fragilidad de la API REST de SAP.

---

#### Matriz Comparativa

| Criterio | Mode A — Dedicated Extractor | Mode B — Service Layer Delta | Mode C — Service Layer Polling |
|---|---|---|---|
| **Performance** | ★★★★★ Excelente — SQL bulk directo | ★★★☆☆ Media — limitado por SL rate | ★★☆☆☆ Baja — polling secuencial |
| **Complejidad de setup** | Media — requiere instalar agente | Alta — requiere UDT + FMS en SAP | Baja — solo credenciales SL |
| **Latencia de datos** | 15–30 min (configurable) | 5–15 min (near real-time) | 30–120 min (mínimo aceptable) |
| **Dependencia de SAP** | SQL directo (HANA / SQL Server) | Service Layer + UDT + FMS | Service Layer únicamente |
| **Escalabilidad** | Alta — sin bottleneck en SL | Media — SL es un cuello de botella compartido | Baja — escala lineal con nro. tenants |
| **Carga inicial histórica** | Viable (millones de filas) | No viable vía SL — CSV manual | No viable — CSV manual |
| **Instalación en cliente** | Sí — Windows Service | No | No |
| **Restricciones cloud SAP** | No funciona si SL no es accesible | Sí — requiere SL habilitado | Sí — requiere SL habilitado |
| **Riesgo de pérdida de datos** | Mínimo — cola offline local | Bajo — cola en SAP | Medio — ventana de polling |
| **Adecuado para volumen alto** | Sí | No | No |

---

#### Árbol de Decisión

```
¿Puede DataBision instalar un servicio en el servidor del cliente?
  ├── Sí → Mode A (Dedicated Extractor)
  └── No → ¿Service Layer habilitado en el cliente?
              ├── No → Bloquear onboarding. Escalar.
              └── Sí → ¿El partner SAP puede crear UDT + FMS?
                          ├── Sí → Mode B (Service Layer Delta)
                          └── No → Mode C (Service Layer Polling)
                                   ⚠️ Documentar como temporal.
                                   Objetivo: migrar a B en 90 días.
```

---

#### Mode A — Dedicated Extractor

**Cuándo usar:**
- SAP B1 HANA on-prem con acceso al puerto 30015
- SAP B1 SQL Server on-prem con acceso ODBC
- IT del cliente permite instalar un servicio Windows o contenedor
- Volumen > 500k transacciones/año

**Topología:**
```
[SAP HANA :30015 / SQL Server :1433]
        │ SQL nativo / ODBC
        ▼
[.NET 8 Worker Service — en infra del cliente]
  - Watermark + lookback adaptativo por tabla
  - Cola offline SQLite (resiliencia ante cortes de red)
  - Heartbeat cada 5 min → DataBision Cloud
        │ HTTPS POST gzip
        ▼
[Ingest API]
```

**Ventajas:** throughput máximo, offline-tolerant, histórico masivo viable.  
**Desventajas:** instalación en infra del cliente, mantención de versiones del agente.

---

#### Mode B — Service Layer Delta

**Cuándo usar:**
- SAP B1 en partner cloud donde no se puede instalar agentes
- Service Layer habilitado (puerto 50000)
- Partner SAP puede crear `@DBI_SYNC_QUEUE` (UDT) y FMS triggers
- Volumen < 200k transacciones/año

**Topología:**
```
[SAP B1 Cloud]
  [@DBI_SYNC_QUEUE UDT] ← poblada por FMS triggers al guardar
        │ Service Layer OData REST
        ▼
[SL Delta Background Worker — en cloud DataBision]
  - Pull solo registros encolados (delta real)
  - Marca procesados en SAP (PATCH al UDT)
  - Sin instalación en infra del cliente
        │ HTTPS POST
        ▼
[Ingest API]
```

**Carga inicial:** CSV manual exportado por el partner SAP → cargado via Ingest API. No via Service Layer.

**Ventajas:** sin instalación en cliente, near-real-time vía cola SAP.  
**Desventajas:** Service Layer lento para volumen alto, requiere UDT + FMS.

---

#### Mode C — Service Layer Polling

**Cuándo usar:**
- Service Layer habilitado pero partner SAP no puede crear UDT
- Baja volumetría (< 50k transacciones/año)
- PoC inicial o cliente piloto en etapa de validación
- Último recurso cuando A y B no son viables

**Topología:**
```
[SAP B1 Cloud]
        │ Service Layer OData REST
        │ GET /Invoices?$filter=UpdateDate gt 'watermark'
        ▼
[SL Polling Background Worker — en cloud DataBision]
  - Polling por tabla según watermark almacenado
  - Intervalo mínimo: 30 min
  - Lookback: 2× el intervalo para no perder cambios en ventana
        │ HTTPS POST
        ▼
[Ingest API]
```

**Ventajas:** footprint cero en SAP, setup < 2 horas.  
**Desventajas:** latencia alta (30–120 min), riesgo de pérdida en ventana de polling, no escala con volumen.

> **Nota importante:** Mode C es un modo de entrada. Cualquier cliente en Mode C debe tener un plan de migración a Mode B documentado en el onboarding. Si el cliente crece, Mode C se convierte en un riesgo operacional.

---

#### Recomendación Oficial DataBision

**Para el primer cliente:** negociar activamente Mode A. El esfuerzo de instalación del agente se paga con la estabilidad operacional del extractor a largo plazo.

**Si el cliente es cloud-only:** Mode B es la opción de producción. Mode C solo si el partner SAP no puede crear el UDT en el timeline del onboarding.

**Regla de oro:** nunca bloquear un onboarding por no poder usar Mode A. Entrar con Mode C si es necesario, pero con compromiso documentado de migrar a B.

---

### 3.2 Ingest API

Receptor único para todos los modos de adquisición. El contrato es idéntico para Mode A, B y C.

**Responsabilidades:**
- Autenticar al extractor por `X-DataBision-ApiKey` → resuelve `company_id`
- Calcular `source_hash_hex` (SHA-256) para idempotencia
- Normalizar `UpdateTSNorm` (CHAR(6) HHMMSS)
- Upsert idempotente en `raw.*` usando `INSERT ON CONFLICT DO UPDATE`
- Registrar checkpoint en `ctl.ingest_checkpoint`
- Emitir evento `DataIngested` al Event Processing Layer
- Retornar `{ inserted, updated, skipped }`

**Lo que NO hace:** transformaciones de negocio, queries analíticas, autenticación de usuarios finales.

---

### 3.3 Capa de Datos (Supabase PostgreSQL)

Schema medallion ampliado. Multi-tenant por `company_id` en todas las tablas de datos.

```
raw.*    → Réplica fiel de SAP B1. Sin transformaciones. Fuente de auditoría.
stg.*    → Tipos limpios, joins resueltos, flags derivados (canceled, overdue).
dim.*    → Dimensiones conformed (cliente, item, vendedor, fecha, empresa).
fact.*   → Hechos calculados (ventas, créditos, inventario, cartera).
ctl.*    → Control: checkpoints, run_log, batch_log, quality_issues, alert_rules.
oper.*   → Inteligencia operacional: freshness, anomalías, alertas, recomendaciones.
audit.*  → Log inmutable de eventos (ingest, view, export, login, action).
```

> La capa `raw.*` / `stg.*` / `fact.*` sirve al **Analytics Layer** (datos históricos, dashboards).  
> Los datos en tiempo real del **Operational Live Layer** nunca pasan por esta capa — van directo desde SAP Service Layer al frontend via Operational API.

---

### 3.4 Event Processing Layer

Mecanismo interno de comunicación entre capas. Desacopla el Ingest API de los workers downstream.

**Eventos de dominio:**

| Evento | Publicado por | Consumido por |
|---|---|---|
| `DataIngested` | Ingest API | StagingTransformWorker, HeartbeatMonitorWorker |
| `CheckpointUpdated` | Ingest API | DataFreshnessWorker |
| `StagingTransformed` | StagingTransformWorker | AlertingWorker, RecommendationWorker |
| `FreshnessThresholdBreached` | DataFreshnessWorker | AlertingWorker |
| `ExtractorHeartbeatMissed` | HeartbeatMonitorWorker | AlertingWorker |
| `AnomalyDetected` | DataFreshnessWorker | AlertingWorker, OpsIntelAPI |
| `RecommendationGenerated` | RecommendationWorker | AlertingWorker, NotificationService |
| `AlertTriggered` | AlertingWorker | NotificationService |

**Implementación por fase:**

- **Fase 1:** eventos síncronos in-process (patrón mediador .NET). Sin infraestructura adicional.
- **Fase 2:** `System.Threading.Channels` para desacoplar con backpressure.
- **Fase 3:** Azure Service Bus o Redis Streams cuando el volumen o la separación de procesos lo justifique.

**Principio:** todos los consumidores de eventos son idempotentes.

---

### 3.5 Background Workers Architecture

**Decisión firme: .NET `IHostedService` como mecanismo principal de procesamiento en background. Azure Functions NO son el mecanismo principal.**

Los workers corren dentro del mismo proceso del API (o en `DataBision.Workers` separado) como `BackgroundService`. Esta decisión está documentada en ADR-007.

#### Por qué .NET Workers sobre Azure Functions

| Criterio | Azure Functions | .NET IHostedService |
|---|---|---|
| Costo adicional | USD 0 en consumption, pero necesita Premium para evitar cold starts (~USD 13/mes) | USD 0 — corre en el App Service que ya pagamos |
| Latencia de inicio | Cold start 1–5s en consumption | Inmediato — proceso ya activo |
| Contexto de DI compartido | No — proceso aislado, reconecta BD en cada invocación | Sí — misma DI, mismo pool de conexiones |
| Debugging local | Requiere `func` CLI separado | `dotnet run` — mismo proceso |
| Scheduling | Timer Trigger (CRON expression) | `PeriodicTimer` nativo .NET 6+ |
| Gestión de errores por tenant | Requiere binding externo | Loop over tenants con try/catch nativo |
| Separación futura de proceso | Cambio de arquitectura | Cambio de `csproj` + `Program.cs` |
| Complejidad operacional | Alta — Function App separado, deployment separado | Baja — un único deployment |

**Azure Functions se mantiene únicamente para:** Mode B Service Layer Delta en escenarios de muy alto volumen donde se requiera escalado horizontal independiente del pull por tenant.

#### Workers Definidos

| Worker | Trigger | Acción | Frecuencia |
|---|---|---|---|
| `StagingTransformWorker` | `DataIngested` o periódico | `raw.*` → `stg.*` → `dim.*` → `fact.*` | Cada 15 min o tras ingest |
| `HeartbeatMonitorWorker` | Periódico | Detecta extractores sin heartbeat | Cada 5 min |
| `DataFreshnessWorker` | Periódico | Calcula freshness score por tabla/tenant | Cada 10 min |
| `AlertingWorker` | Eventos + periódico | Evalúa `ctl.alert_rules` → `oper.alerts` | Cada 5 min |
| `RecommendationWorker` | Tras `StagingTransformed` o nightly | Evalúa reglas → `oper.recommendations` | Cada 60 min (Fase 1.5) |

**Gestión de errores:**
- Fallo en un tenant → loguea + continúa con el siguiente
- N fallos consecutivos → alerta operacional interna a SuperAdmin
- Un worker nunca detiene el proceso principal por fallo en un tenant aislado

**Separación de proceso en Fase 3:**
```
DataBision.Api/      → endpoints HTTP únicamente
DataBision.Workers/  → IHostedService workers únicamente
DataBision.Application/ + Infrastructure/ → compartidos sin cambios
```

---

### 3.6 Operational Intelligence Layer

Capa de visibilidad sobre el estado del propio sistema de datos. Distinta del BI analítico: no reporta sobre el negocio del cliente, reporta sobre la salud de los datos del cliente.

**Qué provee:**

| Componente | Descripción | Fuente |
|---|---|---|
| **Freshness Score** | % de datos actualizados en N horas por objeto SAP | `oper.freshness_scores` |
| **Extractor Health** | Verde / Amarillo / Rojo por tabla y tenant | `ctl.run_log` + HeartbeatMonitorWorker |
| **Sync Lag** | Delta entre `last_run_utc` y `NOW()` | `ctl.ingest_checkpoint` |
| **Data Quality** | Filas con campos críticos nulos o rangos inválidos | `ctl.quality_issues` |
| **Volume Anomalies** | Días con conteo fuera del rango histórico | `DataFreshnessWorker` |
| **Extraction History** | Timeline de últimas N ejecuciones por tabla | `ctl.run_log` |

**Páginas en portal:**
```
/data-status              → Health consolidado por tabla
/data-status/history      → Timeline 30 días
/data-status/{table}      → Detalle: lag, calidad, última sincronización
```

---

### 3.7 Operational Live Layer *(nuevo en v4.0)*

**El problema que resuelve:** no toda la información puede esperar 30–60 minutos para ser útil. Existen escenarios operacionales donde la latencia del pipeline ETL hace que el dato llegue tarde para tomar una decisión.

La capa ETL (raw → stg → fact) está optimizada para analítica histórica y dashboards ejecutivos. No fue diseñada para consultar el estado actual de operaciones en tiempo real.

#### Cuándo usar Analytics Layer vs Operational Live Layer

| Dimensión | Analytics Layer (ETL + PostgreSQL) | Operational Live Layer (Direct SL) |
|---|---|---|
| **Latencia** | 15–120 min según modo | 30–120 segundos |
| **Uso principal** | Tendencias, dashboards, comparativas, KPIs históricos | Estado actual de documentos, flujos operacionales |
| **Fuente** | `fact.*`, `stg.*` en Supabase | SAP Service Layer en tiempo real |
| **Concurrencia** | Alta — datos en PostgreSQL | Limitada — Service Layer tiene rate limit |
| **Offline tolerance** | Total — los datos ya están en PostgreSQL | Ninguna — SAP debe estar disponible |
| **Multi-tenant** | Filtra por `company_id` en PostgreSQL | Autentica por tenant en Service Layer |
| **Carga en SAP** | Ninguna después del ETL | Cada consulta llega a SAP |

#### Casos de Uso Operacionales

Los siguientes escenarios no son apropiados para el Analytics Layer. Requieren el Operational Live Layer:

| Caso de uso | Por qué no puede esperar 30 min |
|---|---|
| **Pedidos listos para despacho** | El equipo de bodega necesita la lista actualizada al minuto para coordinar |
| **Guías pendientes de entrega** | El transportista sale en horas; la lista de entregas no puede tener 1h de retraso |
| **Picking pendiente** | El operario de bodega consulta la pantalla y actúa en minutos |
| **Rutas de transporte activas** | Estado de entrega en campo — cambia cada 15-30 min |
| **Documentos bloqueados** | El área de crédito necesita desbloquear en tiempo real para no detener ventas |
| **Integraciones con error** | Un SAP B1 con interfaz a otro sistema necesita alerta inmediata ante fallo |
| **Pedidos sin stock** | El vendedor en llamada necesita confirmar disponibilidad al cliente ahora |
| **Entregas pendientes de facturar** | El área de facturación opera al cierre del día; la lista no puede ser del día anterior |

#### Arquitectura del Operational Live Layer

```
Portal (/live/*)
        │
        ▼  HTTP GET (autenticado con JWT)
DataBision Operational Live API
  (controlador separado en el mismo API .NET)
        │
        │  Parámetros de consulta:
        │  - company_id (del JWT)
        │  - filtros del caso de uso
        │  - NO caché en PostgreSQL
        ▼
Service Layer OData REST (:50000)
  GET /Orders?$filter=DocStatus eq 'O' and (ShipDate le today)&$select=...
        │
        ▼
SAP Business One (estado actual)
```

**Frecuencias de polling por caso de uso:**

| Caso de uso | Frecuencia | Justificación |
|---|---|---|
| Pedidos para despacho | 60 segundos | Cambia con cada confirmación de bodega |
| Documentos bloqueados | 30 segundos | Impacta ventas activas |
| Stock disponible | 120 segundos | Cambios por transacciones de bodega |
| Entregas pendientes | 120 segundos | Equipo actualiza esporádicamente |
| Integraciones con error | 30 segundos | Alerta temprana crítica |

#### Restricciones del Operational Live Layer

1. **Solo disponible si el cliente tiene Service Layer activo.** Mode A (HANA directo) no habilita esta capa automáticamente — requiere configurar acceso a Service Layer adicionalmente.

2. **Rate limit de Service Layer:** una instalación SAP B1 soporta ~300 req/min simultáneas. Con N usuarios consultando datos live cada 30–60 segundos, esto puede saturarse rápidamente. Implementar rate limiting por tenant en el Operational Live API.

3. **No persistir en PostgreSQL:** los datos del Operational Live Layer son efímeros. Se consultan, se muestran, no se almacenan. Si se necesita historización, el ETL pipeline ya lo cubre.

4. **Solo para operaciones — no para analítica:** no construir dashboards ejecutivos sobre el Operational Live Layer. Las tendencias y comparativas van sobre PostgreSQL.

#### Impacto en el Frontend

Páginas con sufijo `/live`:
```
/live/dispatch        → Pedidos listos para despacho (actualización 60s)
/live/blocked         → Documentos bloqueados (actualización 30s)
/live/picking         → Lista de picking pendiente (actualización 60s)
/live/stock-alerts    → Pedidos sin stock (actualización 120s)
/live/pending-invoice → Entregas por facturar (actualización 120s)
```

Estas páginas usan **polling activo** vía TanStack Query con `refetchInterval`. No WebSockets — el overhead de mantener conexiones abiertas no se justifica para este patrón.

#### Endpoint API

```
GET /api/live/{resource}
  Authorization: Bearer {JWT}
  Headers incluyen company_id extraído del token

Recursos iniciales (Fase 2):
  /api/live/dispatch-ready
  /api/live/blocked-documents
  /api/live/pending-picking
  /api/live/stock-alerts
  /api/live/pending-invoicing
```

#### Planes Comerciales y Operational Live Layer

| Plan | Operational Live Layer |
|---|---|
| Starter | No incluido (solo Analytics) |
| Business | Incluido — 3 vistas live predefinidas |
| Advanced | Incluido — vistas configurables + frecuencias custom |

> La decisión de incluir esta capa en Business y no en Starter es deliberada. Las empresas con necesidades operacionales reales justifican el plan Business. El Starter es para decisión gerencial basada en datos históricos.

---

### 3.8 Recommendation Engine *(fortalecido en v4.0 — disponible desde Fase 1.5)*

#### Objetivo: Transformar DataBision de Reportería a Inteligencia Operacional

La diferencia entre reportería e inteligencia no es tecnológica. Es semántica.

**Reportería (lo que existe hoy):**
> "Las ventas bajaron 20% este mes."

**Inteligencia operacional (lo que el Recommendation Engine entrega):**
> "Las ventas bajaron 20% este mes.
> Los clientes que explican el 80% de la caída son: Cliente A (−$45K), Cliente B (−$28K), Cliente C (−$19K).
> Cliente A no compra desde hace 47 días. Último contacto: sin registro.
> **Acción sugerida:** Contactar a Cliente A esta semana."

La diferencia es la atribución y la acción sugerida. El cliente no necesita analizar — necesita actuar.

#### Fase 1.5 — Definición

La Fase 1.5 es la evolución natural entre el MVP (Fase 1) y la operación completa (Fase 2). No requiere todos los componentes de Fase 2, pero entrega el mayor valor percibido por el cliente después del acceso a los datos.

```
Fase 1 (MVP)     → Datos accesibles + dashboards básicos + estado del extractor
Fase 1.5         → Recomendaciones de alto valor + alertas de negocio
Fase 2           → Gestión completa de usuarios + historial + alertas configurables
```

**Criterio de entrada a Fase 1.5:** el primer cliente está estable en producción durante 30 días sin incidentes críticos de datos.

#### Ejemplos por Dominio

**Ventas:**
```
Insight: "Las ventas de octubre bajaron 23% vs. septiembre."
Atribución: "El 78% de la caída se concentra en 3 clientes:
  - Cliente A: −$52.000 (no compra desde 2026-10-03, hace 28 días)
  - Cliente B: −$31.000 (compras esporádicas, último pedido hace 15 días)
  - Cliente C: −$19.000 (devolvió la última factura; nota de crédito pendiente)"
Acción sugerida: "Priorizar contacto comercial con Cliente A esta semana."
```

**Inventario:**
```
Insight: "5 ítems con stock disponible por debajo del mínimo."
Atribución: "Los ítems críticos son:
  - Ítem X: stock 0 — 3 pedidos de venta pendientes por $15.000
  - Ítem Y: stock 2 — lead time estimado 12 días
  - Ítem Z: stock 1 — rotación alta, agota en ~3 días"
Acción sugerida: "Generar orden de compra para Ítem X de manera urgente."
```

**Compras:**
```
Insight: "Orden de compra OC-2234 lleva 8 días de retraso."
Atribución: "Impacto: 4 órdenes de venta por $38.000 en espera de este proveedor.
  El proveedor tiene historial de retrasos: 3 de las últimas 5 OC entregaron tarde."
Acción sugerida: "Contactar proveedor hoy. Evaluar split de OC con proveedor alternativo."
```

**Clientes:**
```
Insight: "12 clientes activos no han comprado en más de 60 días."
Atribución: "Top 3 por volumen histórico 12 meses:
  - Cliente M: facturación histórica $120.000/año — sin compra desde 2026-09-15
  - Cliente N: facturación histórica $89.000/año — sin compra desde 2026-09-28
  - Cliente O: facturación histórica $67.000/año — sin compra desde 2026-10-02"
Acción sugerida: "Asignar a Vendedor 3 para seguimiento esta semana (Cliente M y N)."
```

**Cobranza:**
```
Insight: "Cartera vencida total: $187.000 en 23 facturas."
Atribución: "Concentración en 3 clientes:
  - Cliente P: $72.000 — factura vencida hace 45 días (mayor riesgo)
  - Cliente Q: $45.000 — vencida hace 30 días
  - Cliente R: $31.000 — vencida hace 22 días"
Acción sugerida: "Priorizar gestión de cobranza con Cliente P. Evaluar suspensión de crédito."
```

#### Arquitectura

```
ctl.recommendation_rules  (por tenant)
  ├── domain: sales | inventory | purchases | customers | collections
  ├── rule_id, title_template, body_template, action_template
  ├── condition_sql   → query que retorna entidades afectadas + contexto
  ├── attribution_sql → query de atribución (Pareto 80/20)
  ├── severity: info | warning | critical
  └── is_active

RecommendationWorker
  ├── Evalúa cada regla activa por tenant
  ├── Genera insight + atribución + acción desde templates
  ├── Persiste en oper.recommendations
  └── Publica RecommendationGenerated → AlertingWorker

oper.recommendations
  ├── id, company_id, rule_id, domain
  ├── title, body, suggested_action (rendered)
  ├── severity, generated_at
  ├── status: active | dismissed | handled
  └── dismissed_by, handled_at, handling_note
```

#### Portal

- Sección "Insights" en el sidebar con badge de conteo activo
- Formato de card por recomendación: título → cuerpo → atribución → acción sugerida
- Acciones: **"Marcar gestionado"** (con nota opcional) + **"Descartar"** + **"Ver detalle"**
- Historial de recomendaciones gestionadas (últimos 90 días)

#### Reglas de Inicio Recomendadas (Fase 1.5)

Comenzar con solo 5 reglas de alta confianza y bajo nivel de ruido:

1. Clientes sin compra en 60+ días (ordenados por facturación histórica)
2. Facturas vencidas > 30 días (con monto total en riesgo)
3. Stock por debajo del mínimo con pedidos pendientes afectados
4. Caída de ventas > 15% mes a mes (con atribución a top clientes)
5. Vendedores sin facturas en los últimos 14 días

Medir el **dismissal rate** por regla. Si > 60% de una regla se descarta sin gestión → revisar la condición.

---

### 3.9 Alerting Engine

Sistema de notificaciones proactivas. El cliente es notificado sin tener que abrir el portal.

**Tipos de alertas:**

| Categoría | Ejemplo | Trigger |
|---|---|---|
| **Operacional** | Extractor sin datos > 4 horas | HeartbeatMonitorWorker |
| **Operacional** | N extracciones consecutivas fallidas | StagingTransformWorker |
| **Negocio — métrica** | Ventas del mes < umbral definido | AlertingWorker vs `fact.*` |
| **Negocio — entidad** | Cliente sin compra en 60 días | AlertingWorker vs `stg.*` |
| **Calidad de datos** | Volumen de registros fuera del rango histórico | DataFreshnessWorker |

**Schema de reglas:**
```
ctl.alert_rules
  company_id     UUID FK
  alert_type     VARCHAR   -- operational | business_metric | data_quality
  condition_sql  TEXT      -- retorna TRUE si la condición se cumple
  threshold      NUMERIC
  cooldown_min   INT       -- tiempo mínimo entre alertas del mismo tipo
  channels       JSONB     -- ["email", "in_portal"]
  is_active      BOOLEAN
```

**Canales de entrega:**

| Canal | Fase | Implementación |
|---|---|---|
| In-portal (badge + centro de notificaciones) | Fase 1 | Polling `oper.alerts` cada 30s desde frontend |
| Email | Fase 1 | Resend.com / SMTP |
| Webhook (Slack, Teams, custom) | Fase 3 | POST al endpoint configurado por tenant |

**Deduplicación:** una alerta del mismo tipo no se re-envía antes de que expire `cooldown_min`.

---

### 3.10 Business Actions Module

Acciones ejecutables desde el portal con efectos en el sistema. Separado conceptualmente del BI de solo lectura.

**Principio:** Fase 1–2 son efectos internos en DataBision. Escritura a SAP es Fase 3+.

**Fase 1:**
| Acción | Quién | Efecto |
|---|---|---|
| Solicitar actualización | CompanyAdmin, Analyst | Dispara extracción inmediata (throttled 30 min) |
| Exportar a Excel/CSV | Analyst, Viewer | Genera desde `stg.*` |
| Descargar PDF | Viewer | PDF del dashboard actual |

**Fase 2:**
| Acción | Quién | Efecto |
|---|---|---|
| Marcar recomendación gestionada | CompanyAdmin | `oper.recommendations.status = handled` |
| Descartar alerta | CompanyAdmin | `oper.alerts.status = dismissed` |
| Definir meta mensual | CompanyAdmin | Inserta en `oper.business_targets` |
| Añadir anotación | Analyst | Inserta en `oper.annotations` |
| Invitar usuario | CompanyAdmin | Email de invitación |

**Fase 3 (diseño separado):**
- Crear actividad en SAP vinculada a recomendación
- Actualizar campo de cliente en SAP via Service Layer
- Generar cotización en SAP (Fase 4+)

---

### 3.11 DataBision API (Portal API)

Orquesta todas las capas internas y expone endpoints al frontend.

| Grupo | Prefijo | Descripción |
|---|---|---|
| Auth | `/api/auth/*` | Login, refresh, logout |
| Analytics | `/api/analytics/*` | KPIs, series, rankings |
| Operational Intel | `/api/operational/*` | Freshness, health, history |
| **Live** | `/api/live/*` | **Operational Live Layer — consulta directa SAP** |
| Alerts | `/api/alerts/*` | Listado, configuración, ack |
| Recommendations | `/api/recommendations/*` | Insights del Recommendation Engine |
| Actions | `/api/actions/*` | Business Actions Module |
| Reports | `/api/reports/*` | Catálogo de dashboards |
| Tenant | `/api/tenant/*` | Config pública (branding, sin auth) |
| Users | `/api/users/*` | Gestión de usuarios del tenant |
| Admin | `/api/admin/*` | SuperAdmin — sin restricción de tenant |
| Ingest | `/api/ingest/*` | Solo para extractores |

---

### 3.12 DataBision Native BI Portal

**Stack:** React 18 + TypeScript + ECharts + TanStack Query + Zustand

**Estructura de páginas:**
```
/login                     → Auth con branding del tenant
/                          → Dashboard principal (KPIs + ECharts)
/reports/{id}              → Dashboard individual (ECharts)
/data-status               → Operational Intelligence Layer
/live/*                    → Operational Live Layer (Fase 2 / Business plan)
/insights                  → Recommendations (Fase 1.5+)
/alerts                    → Centro de notificaciones
/settings/*                → Usuarios, alertas, metas (por fase)
```

**Visualizaciones Fase 1:** KPI cards con delta, serie de tiempo mensual, barras horizontales, tabla analítica paginada.

**Visualizaciones Fase 2:** Donut, gauge de meta, heatmap de actividad, scatter.

---

## 4. Modelo Multi-Tenant

### 4.1 Aislamiento por `company_id`

Una sola instancia Supabase PostgreSQL. Aislamiento por fila.

```
companies
  id           UUID PK
  slug         VARCHAR(50) UNIQUE  ← subdominio
  display_name VARCHAR(200)
  is_active    BOOLEAN

Todas las tablas de datos:
  company_id   UUID FK → companies.id   ← SIEMPRE en WHERE
```

### 4.2 Subdomain Routing

```
databision.com              → Landing / marketing
admin.databision.com        → SuperAdmin
{slug}.databision.com       → Portal del tenant
```

- **Producción:** `TenantMiddleware` lee `Host` header → resuelve `company_id`
- **Dev local:** `?tenant=slug`
- **SuperAdmin:** `company_id = null`, `/api/admin/*` sin restricción de tenant

### 4.3 Escalabilidad del modelo

| Escenario | Decisión |
|---|---|
| 1–10 clientes | Supabase compartida, company_id por fila |
| 10–50 clientes | Supabase Pro + PgBouncer |
| Cliente con aislamiento contractual | Schema separado o instancia Supabase propia |
| 50+ clientes, alto volumen | PostgreSQL managed (Neon, Railway, Azure DB for PostgreSQL) |

---

## 5. Modelo de Datos — Tablas SAP MVP

| Tabla | Objeto SAP | PK Natural |
|---|---|---|
| `raw.sap_oinv` | Facturas | `(company_id, "DocEntry")` |
| `raw.sap_inv1` | Líneas de factura | `(company_id, "DocEntry", "LineNum")` |
| `raw.sap_orin` | Notas de crédito | `(company_id, "DocEntry")` |
| `raw.sap_rin1` | Líneas nota de crédito | `(company_id, "DocEntry", "LineNum")` |
| `raw.sap_ocrd` | Clientes / BP | `(company_id, "CardCode")` |
| `raw.sap_oitm` | Items | `(company_id, "ItemCode")` |
| `raw.sap_oslp` | Vendedores | `(company_id, "SlpCode")` |

**Columnas técnicas (raw):** `source_hash_hex`, `raw_created_at_utc`, `raw_updated_at_utc`.

**Tablas operacionales (`oper.*`):**

| Tabla | Propósito | Escritor |
|---|---|---|
| `oper.freshness_scores` | Score de frescura por tabla/tenant | DataFreshnessWorker |
| `oper.data_anomalies` | Anomalías de volumen | DataFreshnessWorker |
| `oper.alerts` | Alertas disparadas | AlertingWorker |
| `oper.recommendations` | Recomendaciones generadas | RecommendationWorker |
| `oper.business_targets` | Metas por CompanyAdmin | Business Actions API |
| `oper.annotations` | Anotaciones sobre entidades | Business Actions API |

**Configuración (`ctl.*` ampliado):**

| Tabla | Propósito |
|---|---|
| `ctl.alert_rules` | Reglas de alerta por tenant |
| `ctl.recommendation_rules` | Reglas del Recommendation Engine |

---

## 6. Seguridad

### 6.1 Autenticación

| Componente | Mecanismo |
|---|---|
| Extractor → Ingest API | `X-DataBision-ApiKey`, una clave por tenant |
| Usuario → Portal API | JWT RS256 (15 min) + httpOnly refresh token (7 días) |
| Refresh tokens | Hasheados en BD, rotación en cada uso |
| JWT claims | `sub`, `email`, `role`, `company_id`, `company_slug`, `module_ids[]` |
| SuperAdmin JWT | `company_id = null`, `role = SuperAdmin` |
| Operational Live API | JWT del usuario — `company_id` valida acceso al SL del tenant |

### 6.2 Aislamiento de Datos

1. Toda query a tablas de datos lleva `WHERE company_id = @company_id` explícito
2. `TenantMiddleware` valida que `company_id` del JWT coincida con el subdomain
3. SuperAdmin cruza tenants solo desde `/api/admin/*`
4. Ingest API valida API key vs tenant del payload
5. Operational Live API usa credenciales de SL del tenant almacenadas en secretos — nunca del JWT del usuario

### 6.3 Credenciales

- API keys del extractor: una por tenant, revocables individualmente
- Credenciales de Service Layer para Operational Live: almacenadas como secretos por tenant, no en JWT
- Sin credenciales en código ni repositorios
- Variables de entorno en Azure App Service

### 6.4 Transporte

- HTTPS obligatorio, TLS 1.2 mínimo
- Batches con compresión gzip
- Cookies httpOnly + SameSite=Strict

---

## 7. Stack Tecnológico

### Backend

| Capa | Tecnología |
|---|---|
| Runtime | .NET 8 / ASP.NET Core 8 |
| ORM (portal) | Entity Framework Core 8 + Npgsql |
| Acceso raw (ingest) | Dapper + Npgsql |
| Workers | .NET `IHostedService` + `PeriodicTimer` |
| Event Processing | .NET `Channel<T>` (Fase 1-2), Azure Service Bus (Fase 3) |
| Base de datos analítica | Supabase PostgreSQL 15 |
| Base de datos local dev | SQLite (AppDbContext únicamente) |
| Operational Live | HttpClient → SAP Service Layer OData |
| Autenticación | JWT RS256, refresh tokens hasheados |
| Notificaciones email | Resend.com / SendGrid |
| Logging | Serilog |

### Frontend

| Capa | Tecnología |
|---|---|
| Framework | React 18 + TypeScript (strict) |
| Build | Vite |
| Visualización | Apache ECharts (echarts-for-react) |
| State global | Zustand |
| Data fetching | TanStack Query v5 (con `refetchInterval` para Live pages) |
| HTTP | Axios con interceptor de refresh |
| Routing | React Router v6 |
| Estilos | Tailwind CSS + CSS custom properties |
| Notificaciones real-time | SSE para alertas in-portal |

### Infraestructura MVP

| Servicio | Proveedor | Costo |
|---|---|---|
| Base de datos | Supabase Pro | USD 25/mes |
| API + Workers | Azure App Service B2 | USD 30/mes |
| Frontend | Vercel Pro | USD 20/mes |
| DNS + dominio | Cloudflare | USD 10/mes |
| Storage (branding) | Azure Blob Storage | USD 2/mes |
| Email | Resend.com | USD 0–20/mes |
| **Total infraestructura** | — | **~USD 87–107/mes** |

---

## 8. Planes Comerciales

| Plan | Precio | Objetos SAP | Frecuencia | Usuarios | Live Layer | Recomendaciones |
|---|---|---|---|---|---|---|
| **Starter** | USD 350/mes | 4 | Cada 2h | Hasta 5 | No | No |
| **Business** | USD 600/mes | 7 MVP | Cada hora | Hasta 15 | Sí — 3 vistas | Sí (Fase 1.5) |
| **Advanced** | USD 1.000+/mes | 7 MVP + adicionales | 30 min | Hasta 50 | Sí — configurable | Sí + custom rules |

**Márgenes estimados:**
- Starter: ~USD 255/mes (73%)
- Business: ~USD 493/mes (82%)
- Advanced: ~USD 800/mes (80%)

---

## 9. Roadmap de Producto

### Fase 1 — MVP (Semanas 1–8)

- [ ] Ingest API 7 tablas SAP (Hito 1 — completado a nivel código)
- [ ] Migración Supabase PostgreSQL (INSERT ON CONFLICT)
- [ ] Mode A: Dedicated Extractor (.NET Worker Service)
- [ ] Portal: Login + branding por tenant
- [ ] Dashboards nativos: KPI + tendencia + top clientes (ECharts)
- [ ] Operational Intelligence Layer (estado del extractor, freshness)
- [ ] Workers: StagingTransform, HeartbeatMonitor, DataFreshness
- [ ] Alertas operacionales in-portal + email

### Fase 1.5 — Inteligencia Básica (Semanas 8–16, tras primer cliente estable)

- [ ] Recommendation Engine con 5 reglas de alto valor
- [ ] Insights en portal: caída ventas + atribución, clientes inactivos, stock crítico
- [ ] Alertas de negocio (métricas + entidades)
- [ ] Business Actions: marcar recomendación, solicitar actualización
- [ ] AlertingWorker con cooldown configurable

### Fase 2 — Operacional (Semanas 9–20)

- [ ] Mode B: Service Layer Delta
- [ ] Mode C: Service Layer Polling
- [ ] **Operational Live Layer**: 3 vistas predefinidas (Plan Business)
- [ ] Historial de actualizaciones 30 días
- [ ] Gestión de usuarios por CompanyAdmin
- [ ] Más dashboards: vendedores, notas de crédito, cartera
- [ ] SuperAdmin: gestión de tenants, estado global

### Fase 3 — Inteligencia Avanzada (Meses 6–12)

- [ ] Alertas vía webhook (Slack, Teams)
- [ ] Reglas de recomendación custom por cliente
- [ ] Exportación Excel / PDF
- [ ] Tablas SAP adicionales: ORDR, OPCH, OITW
- [ ] Operational Live Layer ampliado (Plan Advanced)
- [ ] Multi-empresa por tenant (holdings)
- [ ] Anomalías con baseline histórico

### Fase 4 — Enterprise (Año 2+)

- [ ] Escritura controlada a SAP
- [ ] Chat analítico (RAG sobre PostgreSQL)
- [ ] Power BI Embedded como add-on
- [ ] S&OP básico
- [ ] Multi-región

---

## 10. Top Architectural Risks *(nuevo en v4.0)*

Esta sección cubre riesgos que afectan la arquitectura en su conjunto, no solo componentes individuales. Son los riesgos que pueden invalidar decisiones de diseño si se materializan.

### AR-01 — Dependencia Excesiva de Service Layer

**Descripción:** Mode B y C dependen de Service Layer de SAP B1. Si un cliente tiene SL inestable, lento, o si SAP cambia la API en un upgrade, todo el flujo de extracción se interrumpe.

| Dimensión | Valor |
|---|---|
| **Impacto** | Crítico — extracción detenida para ese tenant |
| **Probabilidad** | Alta — Service Layer es conocido por ser inestable bajo carga |
| **Afecta** | Mode B, Mode C, Operational Live Layer |

**Mitigaciones:**
- Polly retry con circuit breaker en SL Delta Worker y SL Polling Worker
- Alertas automáticas cuando el circuito se abre (SL no responde)
- Documentar en contrato: si el cliente usa Mode B/C, la estabilidad del SL es responsabilidad compartida
- Operational Live Layer: rate limiting propio para no saturar el SL del cliente
- Test de conectividad a SL como pre-requisito del onboarding

---

### AR-02 — Heterogeneidad entre Instalaciones SAP B1

**Descripción:** No existen dos instalaciones de SAP B1 iguales. Los clientes tienen versiones distintas (9.x, 10.x), UDFs con nombres distintos, monedas distintas, add-ons que modifican tablas estándar, y encoding de datos diferente.

| Dimensión | Valor |
|---|---|
| **Impacto** | Alto — puede requerir personalización por cliente |
| **Probabilidad** | Alta — es la realidad de SAP B1 en el mercado |
| **Afecta** | Todas las modalidades de adquisición, calidad de datos |

**Mitigaciones:**
- Cuestionario de onboarding obligatorio: versión SAP, tablas adicionales activas, moneda base, encoding del servidor
- Queries de extracción con manejo explícito de NULL en campos que varían entre versiones
- MVP usa solo tablas estándar del kernel SAP (OINV, OCRD, OITM, OSLP, ORIN) — sin UDFs
- UDFs del cliente: fuera del alcance MVP, documentado explícitamente en contrato
- Tests con dataset sintético que reproduce variantes comunes (SAP HANA vs SQL Server, v9.3 vs v10.0)

---

### AR-03 — Escalamiento del Polling con Crecimiento de Tenants

**Descripción:** Mode C (SL Polling) y el Operational Live Layer generan N requests a SAP por polling interval, donde N crece con el número de tenants. Con 10 tenants en Mode C, cada 30 minutos se disparan 10 × (N tablas) requests a SAP. Con 30 tenants y vistas live activas, el volumen de requests puede saturar los workers.

| Dimensión | Valor |
|---|---|
| **Impacto** | Alto — degrada calidad del servicio a todos los tenants |
| **Probabilidad** | Media — no es un problema con 5 clientes, sí con 20+ |
| **Afecta** | SL Polling Worker, Operational Live API, SL Delta Worker |

**Mitigaciones:**
- **Mode C → migrar a Mode B** antes de 20 tenants. Mode C no fue diseñado para operar a escala.
- Workers con **staggered schedule**: distribuir el polling de tenants en el tiempo para no ejecutar todos simultáneamente
- **Rate limiting por tenant** en Operational Live API: máximo N requests a SL por tenant por minuto
- **Circuit breaker global**: si el total de requests a SL en una ventana supera el umbral, pausar el polling de tenants de menor prioridad
- Monitorear `avg_polling_duration_ms` por worker → alerta si > 80% del intervalo

---

### AR-04 — Volumen Histórico en Clientes con Datos Legacy

**Descripción:** Un cliente con 15 años de operación en SAP B1 puede tener millones de filas en OINV y OCRD. La carga inicial histórica via Mode A puede tomar horas. Via Mode B o C, puede ser inviable.

| Dimensión | Valor |
|---|---|
| **Impacto** | Alto — retrasa el go-live del cliente |
| **Probabilidad** | Media-Alta — clientes maduros de SAP tienen datos legacy significativos |
| **Afecta** | Onboarding, Ingest API throughput, Supabase storage |

**Mitigaciones:**
- **Pre-evaluación de volumen en onboarding**: ejecutar `SELECT COUNT(*), MIN(DocDate), MAX(DocDate) FROM OINV` antes de firmar contrato
- **Windowed historical load**: cargar por rangos de fecha en lugar de un bulk único (ej: año por año)
- **Mode B/C con volumen histórico**: exportar CSV desde SAP y cargarlo vía script separado — documentado en `databision-product-architecture.md`
- **Límite de retención negociado**: no cargar más de 36 meses de historial en el MVP; datos anteriores se cargan bajo demanda
- **Supabase storage**: monitorear uso; la instancia Pro tiene 8 GB de BD incluidos (ampliable)

---

### AR-05 — Crecimiento de Tenants y Límites de Supabase

**Descripción:** La arquitectura actual usa una sola instancia Supabase Pro compartida entre todos los tenants. Los límites de Supabase Pro (15 conexiones vía PgBouncer, 8 GB de BD) pueden convertirse en cuellos de botella antes de los 20 tenants activos.

| Dimensión | Valor |
|---|---|
| **Impacto** | Alto — degrada performance de todos los tenants si se satura |
| **Probabilidad** | Media — depende del volumen y concurrencia por tenant |
| **Afecta** | Toda la capa de datos |

**Mitigaciones:**
- **Monitorear conexiones activas** desde el primer cliente: si PgBouncer supera 12 conexiones en horas pico, actuar antes de llegar a 15
- **10 tenants = punto de evaluación**: revisar si Supabase Pro sigue siendo adecuado
- **Ruta de migración documentada**: `pg_dump` de Supabase → `pg_restore` en Neon / Railway / Azure DB for PostgreSQL. Sin cambios en el código .NET (Npgsql es compatible con cualquier Postgres).
- **Instancias separadas para clientes Enterprise**: si un cliente requiere aislamiento contractual, provisionar instancia propia (Supabase USD 25/mes por cliente) — lo absorbe el Plan Advanced

---

### AR-06 — Riesgos de Sincronización y Consistencia de Datos

**Descripción:** El pipeline ETL (raw → stg → fact) no es atómico. Si `StagingTransformWorker` procesa a mitad de una transformación y falla, puede haber inconsistencia entre `raw.*` y `fact.*`. El Recommendation Engine puede leer datos de `fact.*` que no reflejan el estado actual de `raw.*`.

| Dimensión | Valor |
|---|---|
| **Impacto** | Medio — recomendaciones basadas en datos parciales |
| **Probabilidad** | Baja — pero aumenta con volumen y frecuencia de ingest |
| **Afecta** | StagingTransformWorker, Recommendation Engine, Alerting Engine |

**Mitigaciones:**
- **Transforms idempotentes**: el `StagingTransformWorker` puede re-ejecutarse completamente sin producir duplicados
- **Timestamp de transformación**: `stg.*` y `fact.*` tienen columna `transformed_at` — el Recommendation Engine puede detectar si el dato está "fresco" antes de usarlo
- **Transacciones por `company_id`**: las transformaciones se ejecutan en scope de un tenant, no globalmente — fallo en un tenant no afecta a otros
- **Reconciliación nocturna**: un job de validación nocturna compara conteos entre `raw.*` y `fact.*` por tenant — alerta si hay divergencia > umbral
- **Datos del Live Layer son siempre frescos**: la inconsistencia solo aplica al Analytics Layer. El Operational Live Layer va directo a SAP.

---

## 11. Registro de Riesgos Operacionales

| ID | Riesgo | Prob. | Impacto | Mitigación | Estado |
|---|---|---|---|---|---|
| R01 | ECharts no cubre un gráfico que el cliente pide | Media | Bajo | Catálogo de 30+ tipos; documentar upfront | Abierto |
| R02 | Supabase Free pausa tras 7 días inactividad | Alta (Free) | Alto | Pro desde primer cliente real | Mitigado |
| R03 | Supabase límite de conexiones (15 Pro) | Media | Medio | PgBouncer; monitorear desde día 1 | Abierto |
| R04 | INSERT ON CONFLICT edge cases vs MERGE | Baja | Medio | Tests de integración previos a producción | Abierto |
| R05 | Partner SAP bloquea instalación del extractor | Alta | Alto | Mode B o C como plan B — siempre | Abierto |
| R06 | Driver HANA requiere licencia SAP | Media | Alto | Validar pre-onboarding; ODBC como fallback | Abierto |
| R07 | Service Layer lento para carga inicial | Alta | Medio | CSV manual; SL solo para incrementales | Abierto |
| R08 | Multi-currency SAP B1 (LC/SC/FC) | Alta | Medio | MVP usa LC; limitación en contrato | Abierto |
| R09 | UpdateDate no actualiza en cancelaciones SAP | Media | Medio | Lookback 24h + reconciliación nocturna | Abierto |
| R10 | BI nativo más costoso de lo estimado | Media | Alto | Scope Fase 1: 4 tipos de gráficos | Abierto |
| R11 | Cliente exige Power BI en negociación | Media | Medio | Ofrecer iframe PBI como add-on | Abierto |
| R12 | Workers consumen memoria excesiva | Media | Medio | PeriodicTimer + cancellation token; B2 App Service | Abierto |
| R13 | Alertas generan spam en cliente nuevo | Alta | Bajo | Cooldown 48h primeros días; reglas operacionales graduales | Abierto |
| R14 | Mode C pierde cambios entre polling intervals | Media | Medio | Lookback 2× el intervalo; documentar en contrato | Abierto |
| R15 | Recommendation Engine genera ruido sin valor | Media | Medio | 5 reglas de alta confianza; medir dismissal rate | Abierto |
| R16 | SL saturado por Operational Live Layer | Media | Alto | Rate limiting por tenant; solo Plan Business+ | Abierto |

---

## 12. Contradicciones en Documentos Anteriores

| Documento | Estado | Nota |
|---|---|---|
| `databision-product-architecture.md` | **PARCIALMENTE SUPERSEDED** | Extracción SAP válida; Azure SQL no. Ver ADR-001. |
| `azure-sql-staging-design.md` | **REFERENCIA ENTERPRISE** | Válido para Plan Enterprise futuro. |
| `two-client-production-roadmap.md` | **PARCIALMENTE VIGENTE** | Modelo de datos aplica; infraestructura de BD y workers revisada. |
| `dedicated-extractor-design.md` | **VIGENTE** | Destino actualizado: Ingest API → Supabase (no Azure SQL). |
| `phase-3-bi-architecture.md` | **SUPERSEDED** | Power BI no es el núcleo. Ver ADR-002. |
| `powerbi-pro-import-mode-strategy.md` | **REPOSICIONADO** | Add-on opcional, no producto principal. |
| `commercial-mvp-strategy.md` | **ACTUALIZACIÓN PENDIENTE** | Precios USD 300/500 → USD 350/600. Ver ADR-005. |

---

## 13. Architecture Decision Records (ADRs)

| ADR | Título | Estado |
|---|---|---|
| [ADR-001](adr/ADR-001-database-engine.md) | Motor de BD: Azure SQL → Supabase PostgreSQL | Aceptado |
| [ADR-002](adr/ADR-002-bi-layer.md) | Capa BI: Power BI → DataBision Native BI (ECharts) | Aceptado |
| [ADR-003](adr/ADR-003-multitenancy.md) | Multi-tenant: DB por tenant → instancia compartida | Aceptado |
| [ADR-004](adr/ADR-004-extractor-modes.md) | Modos A + B + C de adquisición | Aceptado |
| [ADR-005](adr/ADR-005-pricing-model.md) | Revisión de precios: USD 300/500 → USD 350/600 | Aceptado |
| [ADR-006](adr/ADR-006-native-bi-vs-powerbi.md) | Por qué construir BI propio | Aceptado |
| [ADR-007](adr/ADR-007-background-workers.md) | Workers .NET IHostedService sobre Azure Functions | Aceptado |
| [ADR-008](adr/ADR-008-operational-live-layer.md) | Operational Live Layer: cuándo ir directo a SAP vs Analytics Layer | Aceptado |
| [ADR-009](adr/ADR-009-native-bi-mvp-scope.md) | Native BI MVP: scope de visualizaciones y catálogo por fase | Aceptado |
| [ADR-010](adr/ADR-010-mode-b-canonical-design.md) | Mode B canónico: @DBS_QUEUE/@DBS_CHANGES, mecanismo de disparo flexible | Aceptado |
| [ADR-011](adr/ADR-011-powerbi-as-addon.md) | Power BI como add-on futuro: condiciones, scope y governance | Aceptado |

---

## 14. Principios Arquitectónicos No Negociables

1. **Tenant isolation:** toda query incluye `company_id` explícito. Sin excepciones.
2. **DTOs en el boundary:** nunca exponer entidades de dominio desde controllers.
3. **No SQL interpolation:** EF Core o Dapper con parámetros. Nunca concatenación de strings.
4. **Idempotencia en ingest:** cualquier batch puede reenviarse sin duplicar datos.
5. **Audit everything:** CREATE, UPDATE, DELETE, VIEW_REPORT, y toda Business Action van a `audit.*`.
6. **API shape consistente:** `{ "data": T }` en éxito, `{ "error": "code", "message": "..." }` en error.
7. **Migrations are immutable:** nunca editar una migración existente; crear migración correctiva.
8. **Watermark-first:** toda extracción incremental usa `UpdateDate >= watermark - lookback`.
9. **Brand colors via CSS vars:** nunca hardcodear colores de marca; solo `var(--brand-primary)`.
10. **SuperAdmin sin company:** `company_id = null` en JWT; `/api/admin/*` no requiere tenant.
11. **Workers resilientes por tenant:** un error en un tenant no detiene el procesamiento de los demás.
12. **Alertas con cooldown:** sin spam al cliente. Un alert del mismo tipo espera su cooldown.
13. **Actions son auditables:** toda Business Action se registra en `audit.*` con user_id, timestamp, entidad.
14. **Event consumers idempotentes:** procesar el mismo evento dos veces produce el mismo resultado.
15. **Live Layer no persiste:** los datos del Operational Live Layer no se almacenan en PostgreSQL. Se consultan, se muestran, se descartan.
16. **Recommendation = insight + atribución + acción:** una recomendación sin los tres componentes no es útil.
