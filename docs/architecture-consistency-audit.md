# DataBision — Architecture Consistency Audit

**Versión:** 1.1  
**Fecha:** 2026-05-30  
**Autor:** Chief Architect  
**Alcance:** Revisión de 9 documentos contra `master-architecture.md` v4.0

> **Estado v1.1:** Auditoría completada. Las 4 contradicciones críticas fueron resueltas. La arquitectura queda lista para commit baseline y Sprint 0.

---

## Cierre de Auditoría — 2026-05-30

### Contradicciones Críticas — Estado Final

| ID | Descripción | Estado | Resolución |
|---|---|---|---|
| C-01 | Destino extractor: Azure SQL vs Supabase | ✅ RESUELTO | `dedicated-extractor-design.md` actualizado con nota y diagrama |
| C-02 | Azure Functions vs BackgroundService .NET | ✅ RESUELTO | `cloud-connector-queue-mode-design.md` actualizado. ADR-007 referenciado. |
| C-03 | `@DBI_SYNC_QUEUE` vs `@DBS_CHANGES`/`@DBS_QUEUE` | ✅ RESUELTO | ADR-010 creado. Naming canónico definido. `cloud-connector-queue-mode-design.md` actualizado. `service-layer-delta-design.md` marcado como canónico. |
| C-04 | Pricing USD 300/500 vs USD 350/600 + Power BI posicionamiento | ✅ RESUELTO | `commercial-mvp-strategy.md` actualizado con tabla de precios canónicos, nota de piloto USD 500, y reposicionamiento de Power BI. |

### Documentos Modificados

| Documento | Cambios aplicados |
|---|---|
| `dedicated-extractor-design.md` | Header + nota de destino (Azure SQL → Supabase). Diagrama actualizado. |
| `cloud-connector-queue-mode-design.md` | Header completo con 3 correcciones. Azure SQL → Supabase. Azure Functions → BackgroundService. `@DBI_SYNC_QUEUE` → `@DBS_QUEUE`. |
| `service-layer-delta-design.md` | Header canónico. §2 reestructurado: UDT Queue como mecanismo principal, PTN como opcional prioritario. |
| `commercial-mvp-strategy.md` | Tabla de precios canónicos. Nota de piloto USD 500. Power BI reposicionado como add-on. |
| `native-bi-architecture.md` | `ctl.etl_watermarks` → `ctl.ingest_checkpoint`. `ctl.etl_run_log` → `ctl.run_log`. Roadmap actualizado con Fase 1.5. |
| `tenant-subdomain-portal-strategy.md` | Header de actualización. Dominio y Power BI reposicionado. |
| `frontend-ux-architecture.md` | Nota de dominio canónico (`.com` no `.app`). |
| `implementation-roadmap-v1.md` | DocStatus fix (SAP B1: C=cerrado, no cancelado). Migration instructions condicionales. `@DBI_SYNC_QUEUE` → `@DBS_QUEUE`. |
| `master-architecture.md` | Tabla de ADRs actualizada con ADR-008 a ADR-011. |

### ADRs Creados

| ADR | Título |
|---|---|
| [ADR-008](adr/ADR-008-operational-live-layer.md) | Operational Live Layer: cuándo ir directo a SAP |
| [ADR-009](adr/ADR-009-native-bi-mvp-scope.md) | Native BI MVP: scope de visualizaciones por fase |
| [ADR-010](adr/ADR-010-mode-b-canonical-design.md) | Mode B canónico: @DBS_QUEUE/@DBS_CHANGES, mecanismo flexible |
| [ADR-011](adr/ADR-011-powerbi-as-addon.md) | Power BI como add-on futuro: condiciones y governance |

### Decisiones Congeladas

1. **Base de datos MVP:** Supabase PostgreSQL. Azure SQL = Enterprise futuro.
2. **Motor BI:** DataBision Native BI (React + ECharts). Power BI = add-on cuando ≥3 clientes lo soliciten.
3. **Background processing:** BackgroundService .NET. Azure Functions = alternativa escala alta únicamente.
4. **Mode B UDTs:** `@DBS_QUEUE` (cola) y `@DBS_CHANGES` (log). No `@DBI_SYNC_QUEUE`.
5. **Mode B disparo:** jerarquía PTN → TransactionNotification → FMS → SP programado. PTN es preferido, no obligatorio.
6. **Precios oficiales:** Starter USD 350/mes, Business USD 600/mes, Advanced USD 1.000+/mes. Piloto primeros clientes: Business a USD 500 por 3 meses.
7. **Dominio canónico:** `databision.com`. Subdominios: `admin.databision.com`, `{slug}.databision.com`.
8. **SAP B1 DocStatus:** `DocStatus='C'` = Cerrado (venta válida). `Cancelled='Y'` = Anulado. Nunca excluir DocStatus='C' de ventas.
9. **Migraciones:** nunca eliminar migraciones productivas. Solo reset si no hay datos en producción.

### Pendientes No Bloqueantes (post Sprint 0)

| # | Acción | Prioridad |
|---|---|---|
| A05 | Renombrar `powerbi-pro-import-mode-strategy.md` a `powerbi-addon-guide.md` | Baja |
| A06 | Definir dominio oficial en WHOIS/DNS y actualizarlo en todos los archivos con replace global | Media |
| A07 | Documentar nombres finales de tablas `fact.*` en `supabase-postgres-mvp-architecture.md` | Media |
| A08 | Crear guía técnica de onboarding para partner SAP (4 mecanismos de disparo Mode B) | Alta (para Fase 2) |

### Veredicto Final

**La arquitectura está lista para commit baseline.**

`master-architecture.md` v4.0 es internamente consistente y completo. Los documentos periféricos tienen los headers de actualización necesarios. Las 4 contradicciones críticas están resueltas. Los 4 ADRs pendientes fueron creados. Se puede iniciar Sprint 0.

---

---

## 1. Resumen Ejecutivo

La arquitectura v4.0 definida en `master-architecture.md` es internamente consistente y completa. El problema no está en el master — está en la **divergencia documental acumulada** de los documentos periféricos.

**Resultado de la auditoría:**

| Documentos auditados | 11 |
|---|---|
| Completamente vigentes | 3 |
| Parcialmente vigentes | 4 |
| Superseded | 2 |
| Reposicionados | 1 |
| Sin contradicciones con master | 1 (master mismo) |

**Contradicciones críticas detectadas:** 4  
**Contradicciones moderadas detectadas:** 5  
**Contradicciones menores detectadas:** 4  
**ADRs pendientes de crear:** 4

**Veredicto de pre-congelamiento:** la arquitectura puede congelarse, pero **no antes de resolver las 4 contradicciones críticas**. Son confusiones que desorientarían a cualquier persona que implemente siguiendo los documentos periféricos en lugar del master.

---

## 2. Tabla de Estado Documental

| Documento | Estado | Problema principal | Acción recomendada |
|---|---|---|---|
| `master-architecture.md` | ✅ VIGENTE — Fuente de verdad | Ninguno | Mantener. Congelar como v4.0. |
| `native-bi-architecture.md` | ✅ VIGENTE | Naming inconsistente (`ctl.etl_watermarks` vs `ctl.ingest_checkpoint`). Roadmap no incluye Fase 1.5. | Actualizar referencias de tabla y fase. |
| `frontend-ux-architecture.md` | ✅ VIGENTE | Usa `databision.app` en URLs (master dice `databision.com`). | Actualizar dominio. |
| `service-layer-delta-design.md` | ✅ VIGENTE | UDT `@DBS_CHANGES` contradice `@DBI_SYNC_QUEUE` del otro documento SL. Usa PTN en lugar de FMS. | Consolidar con `cloud-connector-queue-mode-design.md` en un único doc canónico. |
| `dedicated-extractor-design.md` | ⚠️ PARCIALMENTE SUPERSEDED | Destino descrito es "Azure SQL" — la arquitectura actual usa Supabase via Ingest API. | Actualizar destino. Principios de extracción: vigentes. |
| `cloud-connector-queue-mode-design.md` | ⚠️ PARCIALMENTE SUPERSEDED | Azure SQL como destino. Azure Functions como mecanismo principal (contradice ADR-007). `@DBI_SYNC_QUEUE` conflicta con `@DBS_CHANGES` del otro SL doc. | Actualizar destino y mecanismo. Definir UDT canónico. |
| `supabase-postgres-mvp-architecture.md` | ✅ VIGENTE | Sigue siendo válido como especificación de migración. | Mantener. Agregar nota de estado: "Especificación implementada". |
| `commercial-mvp-strategy.md` | ⚠️ PARCIALMENTE VIGENTE | Precios USD 300/500 en lugar de USD 350/600. Power BI Pro descrito como producto principal, no add-on. | Actualizar precios y reposicionar Power BI. |
| `tenant-subdomain-portal-strategy.md` | ⚠️ PARCIALMENTE VIGENTE | Power BI iframe como mecanismo primario de reportes. Roadmap no incluye Cockpit, Live Layer, Recommendation Engine. Dominio `databision.app` vs `databision.com`. | Actualizar sección de reportes y roadmap. |
| `powerbi-pro-import-mode-strategy.md` | 🔄 REPOSICIONADO | Describe Power BI Pro como estrategia principal. Ahora es add-on opcional. | Marcar como add-on guide. Actualizar título y scope. |
| `phase-3-bi-architecture.md` | ❌ SUPERSEDED | Power BI Embedded como núcleo del producto — reemplazado por Native BI. Ya tiene nota de superseded. | Mantener como referencia histórica para Fase 4+ si se evalúa Embedded. |

---

## 3. Contradicciones Críticas

*Estas contradicciones son confusas para un implementador y deben resolverse antes de congelar.*

---

### C-01 — Destino del Extractor: Azure SQL vs Supabase PostgreSQL

**Severidad:** CRÍTICA  
**Documentos afectados:** `dedicated-extractor-design.md`, `cloud-connector-queue-mode-design.md`

**Descripción:**

`dedicated-extractor-design.md` describe explícitamente:
> "agente local en infra del cliente que extrae SAP B1 HANA y **empuja a Azure SQL**"

`cloud-connector-queue-mode-design.md`:
> "Power BI fuera de alcance salvo como consumidor futuro de **Azure SQL**"

**Arquitectura actual (master-architecture.md v4.0):** el extractor envía datos vía HTTPS al Ingest API, que persiste en **Supabase PostgreSQL**. Azure SQL quedó como referencia Enterprise futura.

**Riesgo:** Un implementador que lea el documento del extractor antes que el master diseñaría la infraestructura sobre Azure SQL.

**Acción requerida:** Actualizar ambos documentos. El destino es `Supabase PostgreSQL via Ingest API`. Azure SQL es opción Enterprise documentada en `azure-sql-staging-design.md`.

---

### C-02 — Mecanismo de Background Processing: Azure Functions vs .NET IHostedService

**Severidad:** CRÍTICA  
**Documentos afectados:** `cloud-connector-queue-mode-design.md`

**Descripción:**

`cloud-connector-queue-mode-design.md` define el Cloud Connector como:
> "Azure Function — Cloud Connector. TimerTrigger cada 5–15 min"

**Decisión actual (ADR-007, master §3.5):** Los workers de background son .NET `IHostedService`. Azure Functions **no son el mecanismo principal**. Se mantienen como opción solo para Mode B en escenarios de muy alto volumen.

**Riesgo:** Un implementador de Mode B construiría una Azure Function App separada cuando la decisión correcta es un `BackgroundService` .NET dentro del mismo proceso o `DataBision.Workers`.

**Acción requerida:** Actualizar `cloud-connector-queue-mode-design.md`. El SL Delta Worker es un `BackgroundService`, no una Azure Function. Referenciar ADR-007.

---

### C-03 — Naming Inconsistente del UDT: `@DBS_CHANGES` vs `@DBI_SYNC_QUEUE`

**Severidad:** CRÍTICA  
**Documentos afectados:** `service-layer-delta-design.md` vs `cloud-connector-queue-mode-design.md`

**Descripción:**

Dos documentos describen el mismo concepto (Service Layer Delta, Mode B) con nombres de UDT completamente distintos:

| Documento | UDT principal | Mecanismo de disparo |
|---|---|---|
| `service-layer-delta-design.md` | `@DBS_CHANGES` | Post Transaction Notification (PTN) |
| `cloud-connector-queue-mode-design.md` | `@DBI_SYNC_QUEUE` | TransactionNotification / SP |

**Implicación:** Un implementador no sabe qué UDT crear en SAP B1, qué SP configurar, o cómo se llaman los campos. Los dos diseños son arquitecturalmente incompatibles entre sí.

**Raíz del problema:** Los dos documentos se crearon en momentos distintos del diseño sin consolidarse. `service-layer-delta-design.md` es más reciente (v1.0 con fecha 2026-05-29) e incorpora PTN que es más elegante. `cloud-connector-queue-mode-design.md` es más detallado en el conector pero más antiguo en decisiones.

**Acción requerida:** Consolidar en un único documento canónico `service-layer-delta-design.md`. Deprecar o absorber `cloud-connector-queue-mode-design.md` como referencia histórica. Definir explícitamente el nombre oficial del UDT.

---

### C-04 — Pricing: USD 300/500 vs USD 350/600

**Severidad:** CRÍTICA (impacto comercial directo)  
**Documentos afectados:** `commercial-mvp-strategy.md`

**Descripción:**

`commercial-mvp-strategy.md` (Versión 1.0) define:
- Plan Starter: USD 300/mes
- Plan Business: USD 500/mes

**Decisión actual (ADR-005, master §8):**
- Plan Starter: USD 350/mes (+50)
- Plan Business: USD 600/mes (+100)

**Riesgo:** Si se usa este documento en una propuesta comercial, se cotizará incorrectamente. El margen real es distinto al calculado en las tablas del documento.

**Adicionalmente:** El documento describe Power BI Pro como la forma de entrega de reportes, no Native BI. La propuesta de valor cambia radicalmente con Native BI — el cliente ya no necesita licencias Power BI Pro propias.

**Acción requerida:** Actualizar precios, márgenes, y propuesta de valor del producto.

---

## 4. Contradicciones Moderadas

*Estas contradicciones generan confusión pero no bloquean la implementación si el implementador consulta el master.*

---

### M-01 — Power BI como Producto Principal vs Add-on

**Severidad:** MODERADA  
**Documentos afectados:** `commercial-mvp-strategy.md`, `tenant-subdomain-portal-strategy.md`, `powerbi-pro-import-mode-strategy.md`

**Descripción:**

Tres documentos describen Power BI Pro como la forma principal de entrega de reportes al cliente. La arquitectura actual define Native BI (React + ECharts) como el núcleo, y Power BI como add-on opcional para clientes que ya tienen Pro.

El documento `powerbi-pro-import-mode-strategy.md` entero está escrito desde la perspectiva de "este es el producto". Ejemplo:
> "El MVP usa Power BI Pro para el desarrollador/analista de DataBision (USD 10/mes), Import Mode para los datasets, Workspaces compartidos para entregar reportes a los clientes"

`tenant-subdomain-portal-strategy.md` §3.3 describe:
> "Botón 'Actualizar': Llama a `POST /api/reports/{id}/refresh` → Backend llama a Power BI REST API"

Este endpoint ya no existe en la arquitectura actual.

**Acción requerida:**
- `powerbi-pro-import-mode-strategy.md`: Renombrar a `powerbi-addon-guide.md` o añadir nota clara: "Power BI es un add-on futuro, no el producto principal".
- `tenant-subdomain-portal-strategy.md`: Actualizar sección de reportes para reflejar Native BI.
- `commercial-mvp-strategy.md`: Remover dependencia de licencias Power BI Pro del cliente como pre-requisito.

---

### M-02 — Roadmap Sin Fase 1.5

**Severidad:** MODERADA  
**Documentos afectados:** `native-bi-architecture.md`, `tenant-subdomain-portal-strategy.md`

**Descripción:**

`native-bi-architecture.md` §16 define un roadmap con Fases 1–4. `master-architecture.md` v4.0 introdujo Fase 1.5 (Recommendation Engine + Alertas de negocio) como etapa explícita entre el MVP y la Fase 2.

`tenant-subdomain-portal-strategy.md` §9 "Roadmap de Features del Portal" tampoco incluye Operational Live Layer, Operational Cockpit, ni el Recommendation Engine.

**Acción requerida:** Alinear los roadmaps de `native-bi-architecture.md` y `tenant-subdomain-portal-strategy.md` con las fases del master. Añadir Fase 1.5 y actualizar los features por fase.

---

### M-03 — Naming de Tabla de Control: `ctl.etl_watermarks` vs `ctl.ingest_checkpoint`

**Severidad:** MODERADA  
**Documentos afectados:** `native-bi-architecture.md`

**Descripción:**

`native-bi-architecture.md` §8.6 (Sincronización) referencia:
```
ctl.etl_watermarks.last_extracted_at
ctl.etl_run_log.records_upserted
ctl.etl_run_log.status
```

`master-architecture.md` y `supabase-postgres-mvp-architecture.md` definen:
```
ctl.ingest_checkpoint
ctl.run_log
```

No es un cambio de arquitectura — es un naming inconsistente. Pero si un desarrollador implementa queries de sincronización basándose en `native-bi-architecture.md`, usará nombres de tabla incorrectos.

**Acción requerida:** Actualizar `native-bi-architecture.md` §8.6 con los nombres canónicos.

---

### M-04 — Módulos del Portal: Cockpit y Live Layer Ausentes

**Severidad:** MODERADA  
**Documentos afectados:** `tenant-subdomain-portal-strategy.md`

**Descripción:**

`tenant-subdomain-portal-strategy.md` describe la estructura de páginas del portal:
```
/login, /, /reports/{id}, /data-status, /data-status/history,
/insights, /alerts, /settings/*
```

La arquitectura actual (master v4.0, `frontend-ux-architecture.md`) incluye módulos adicionales críticos:
- `/cockpit` — Operational Cockpit (semáforo de la operación)
- `/live/*` — Operational Live Layer (datos en tiempo real desde SAP)
- `/actions` — Business Actions Module
- `/dashboard` — Dashboard Ejecutivo separado del Home

`tenant-subdomain-portal-strategy.md` fue escrito antes de que estos módulos se definieran.

**Acción requerida:** Actualizar sección de páginas en `tenant-subdomain-portal-strategy.md` con la estructura de navegación de `frontend-ux-architecture.md`.

---

### M-05 — Dos Documentos Compitiendo para el Mismo Modo de Extracción

**Severidad:** MODERADA  
**Documentos afectados:** `service-layer-delta-design.md`, `cloud-connector-queue-mode-design.md`

**Descripción:**

Ambos documentos describen "Mode B" (extracción vía Service Layer para clientes cloud) pero son incompatibles:

| Aspecto | `service-layer-delta-design.md` | `cloud-connector-queue-mode-design.md` |
|---|---|---|
| UDT principal | `@DBS_CHANGES` | `@DBI_SYNC_QUEUE` |
| Mecanismo de disparo | Post Transaction Notification (PTN) | TransactionNotification SP / FMS |
| Worker | No especifica (describe el flujo) | Azure Function TimerTrigger |
| Logging table | No descrita | `@DBI_SYNC_LOG` |
| Estado campos | `U_ProcStat` | `U_Processed`, `U_Status` |

Son dos diseños distintos para el mismo problema. No hay una versión canónica oficial definida.

**Acción requerida:** Ver C-03 — consolidar en un único documento.

---

## 5. Contradicciones Menores

*Inconsistencias de detalle que no afectan la implementación pero deben corregirse para pulir la documentación.*

---

### m-01 — Dominio: `databision.app` vs `databision.com`

**Documentos afectados:** `frontend-ux-architecture.md`, `tenant-subdomain-portal-strategy.md`

`frontend-ux-architecture.md` usa `{slug}.databision.app` en todas las URLs del portal.  
`master-architecture.md` y `tenant-subdomain-portal-strategy.md` usan `*.databision.com`.

El dominio oficial no está confirmado en ningún documento. Debe definirse y ser consistente en todos.

**Acción requerida:** Definir el dominio oficial en master-architecture.md (o en un ADR). Actualizar todos los documentos.

---

### m-02 — Naming de Tablas Fact: Prefijos Distintos

**Documentos afectados:** `native-bi-architecture.md`

`native-bi-architecture.md` usa `fact_Sales`, `fact_ARAging`, `fact_Inventory`.  
`master-architecture.md` usa `fact.*` genérico (sin especificar nombres finales).

No es una contradicción de arquitectura, pero los nombres concretos de las tablas deberían estar en un único lugar canónico (probablemente `supabase-postgres-mvp-architecture.md`) para evitar divergencias en las implementaciones.

**Acción requerida:** Documentar los nombres finales de tablas `fact.*` en `supabase-postgres-mvp-architecture.md`.

---

### m-03 — Roadmap del Dedicated Extractor: Referencia a Azure SQL

**Documentos afectados:** `dedicated-extractor-design.md` (en el roadmap de dos clientes)

El roadmap de implementación del extractor (Semanas 1–5) menciona:
> "Provisionar Azure SQL del tenant + Key Vault + acceso de red al cliente"

Estos pasos ya no aplican para el MVP. El stack ahora es Supabase (sin Azure SQL, sin Key Vault).

**Acción requerida:** Actualizar el roadmap de `dedicated-extractor-design.md`.

---

### m-04 — `ctl.ingest_checkpoint` vs `ctl.etl_checkpoints`

**Documentos afectados:** `databision-product-architecture.md`

`databision-product-architecture.md` §10 usa `etl.checkpoints` como nombre de tabla.  
La arquitectura actual usa `ctl.ingest_checkpoint` (schema `ctl`, no `etl`).

Este documento ya está marcado como parcialmente superseded, pero el naming incorrecto puede confundir si se consulta como referencia.

---

## 6. Validación de master-architecture.md como Fuente de Verdad

`master-architecture.md` v4.0 **sí cumple el rol de fuente de verdad**. Se verificaron los siguientes criterios:

| Criterio | Estado |
|---|---|
| Cubre todas las capas del sistema | ✅ — Acquisition (3 modos), Ingest, Data, Events, Workers, OpsIntel, Live Layer, Alerting, Recommendations, Business Actions, API, Portal |
| Define el stack tecnológico | ✅ — Backend, Frontend, Infraestructura completos |
| Define el modelo de datos | ✅ — Tablas raw/stg/dim/fact/ctl/oper/audit con PKs |
| Define seguridad | ✅ — Auth, aislamiento, credenciales, transporte |
| Define el modelo multi-tenant | ✅ — company_id, subdomain routing, escalabilidad |
| Define planes comerciales | ✅ — Starter/Business/Advanced con precios actuales |
| Contiene roadmap por fase | ✅ — Fase 1, 1.5, 2, 3, 4 |
| Registra riesgos arquitectónicos | ✅ — Top Architectural Risks + Riesgos Operacionales |
| Lista ADRs | ✅ — ADR-001 a ADR-007, ADR-008 pendiente |
| Define principios no negociables | ✅ — 16 principios |
| Mantiene trazabilidad de documentos obsoletos | ✅ — §12 Contradicciones documentadas |

**Una observación sobre el master:** La sección de Operational Live Layer (§3.7) define que los datos no se persisten en PostgreSQL, pero la sección de tablas `oper.*` (§5) no menciona ninguna tabla dedicada para el Live Layer. Esto es correcto — el Live Layer es stateless por diseño — pero podría generar preguntas. Considerar añadir una nota explícita.

---

## 7. ADRs Pendientes de Crear

| ADR | Título | Prioridad | Cubre |
|---|---|---|---|
| **ADR-008** | Operational Live Layer: cuándo ir directo a SAP vs Analytics Layer | ALTA | Ya marcado pendiente en master. Decide dónde está el boundary entre los dos caminos de datos. |
| **ADR-009** | Native BI MVP: scope de visualizaciones y catálogo de gráficos | MEDIA | Formaliza qué tipos de gráficos se construyen y en qué fase. Evita scope creep. |
| **ADR-010** | Consolidación de modos Service Layer: PTN vs FMS vs Polling | ALTA | Resuelve C-03. Define cuál es el mecanismo oficial para Mode B y Mode C. |
| **ADR-011** | Power BI como add-on: condiciones, scope y governance | MEDIA | Formaliza que Power BI no es el producto, cuándo se ofrece, y cómo se integra. Resuelve M-01. |

**ADRs ya existentes que cubren el resto de la lista del usuario:**

| Requerimiento del usuario | ADR que lo cubre |
|---|---|
| Supabase como base MVP | ADR-001 (Motor de BD) |
| Background Workers | ADR-007 (IHostedService vs Azure Functions) |
| Pricing strategy | ADR-005 (Revisión de precios) |
| Acquisition Strategy prioridad | ADR-004 (Modos de adquisición A+B+C) |
| Native BI vs Power BI | ADR-002 + ADR-006 |

---

## 8. Plan de Acciones Antes de Congelar

### Acciones Bloqueantes (deben hacerse antes de congelar)

| # | Acción | Documento(s) | Tipo | Esfuerzo |
|---|---|---|---|---|
| A01 | Actualizar destino del extractor: Azure SQL → Supabase via Ingest API | `dedicated-extractor-design.md` | Edición puntual | 30 min |
| A02 | Actualizar mecanismo: Azure Functions → BackgroundService .NET | `cloud-connector-queue-mode-design.md` | Edición puntual | 20 min |
| A03 | Definir UDT canónico para SL Delta: decidir entre `@DBS_CHANGES` (PTN) y `@DBI_SYNC_QUEUE` (FMS) | `service-layer-delta-design.md`, `cloud-connector-queue-mode-design.md` | Decisión + ADR-010 | 2 horas |
| A04 | Actualizar precios USD 300/500 → USD 350/600 y reposicionar Power BI | `commercial-mvp-strategy.md` | Edición + reframing | 1 hora |

### Acciones Recomendadas (pueden hacerse post-congelamiento)

| # | Acción | Documento(s) | Tipo | Esfuerzo |
|---|---|---|---|---|
| A05 | Actualizar naming de tablas: `ctl.etl_watermarks` → `ctl.ingest_checkpoint` | `native-bi-architecture.md` | Edición puntual | 15 min |
| A06 | Añadir Fase 1.5 al roadmap | `native-bi-architecture.md` | Edición sección | 30 min |
| A07 | Actualizar estructura de páginas del portal | `tenant-subdomain-portal-strategy.md` | Edición sección | 45 min |
| A08 | Reposicionar Power BI como add-on | `powerbi-pro-import-mode-strategy.md` | Nota de estado + rename scope | 30 min |
| A09 | Definir dominio oficial y actualizarlo en todos los docs | Todos | Búsqueda + reemplazo | 20 min |
| A10 | Crear ADR-008 (Operational Live Layer boundary) | ADR nuevo | Documento nuevo | 1 hora |
| A11 | Crear ADR-009 (Native BI MVP scope) | ADR nuevo | Documento nuevo | 45 min |
| A12 | Crear ADR-010 (Consolidar modos SL) | ADR nuevo | Documento nuevo + consolidar SL docs | 2 horas |
| A13 | Crear ADR-011 (Power BI como add-on) | ADR nuevo | Documento nuevo | 45 min |
| A14 | Documentar nombres finales de tablas `fact.*` | `supabase-postgres-mvp-architecture.md` | Edición sección | 30 min |

---

## 9. Hallazgos de Calidad Documental

### Documentos bien escritos y coherentes

- `master-architecture.md` v4.0 — completo, jerárquico, con ADRs, risks, principios.
- `native-bi-architecture.md` — nivel de detalle correcto: contratos API, DTOs, módulos, KPI formulas.
- `frontend-ux-architecture.md` — excelente nivel de detalle: layouts, comportamientos, responsive, dark mode.
- `service-layer-delta-design.md` — técnicamente preciso con el mecanismo PTN.

### Documentos que necesitan actualización de contexto

- `dedicated-extractor-design.md` — el body técnico del extractor es sólido. Solo cambia el destino (Azure SQL → Supabase). Mantener los principios de diseño (idempotencia, watermark, heartbeat).
- `cloud-connector-queue-mode-design.md` — buena cobertura del problema de SL polling, pero las decisiones de implementación están desactualizadas.

### Documentos que requieren reposicionamiento editorial

- `powerbi-pro-import-mode-strategy.md` — el contenido técnico sobre Power BI Pro sigue siendo válido como referencia. El problema es que el framing es "este es el producto" en lugar de "esto es el add-on".
- `commercial-mvp-strategy.md` — los precios y la propuesta de valor son el corazón del documento. Ambos cambiaron.

---

## 10. Recomendación Final para Congelar la Arquitectura

### Condiciones de congelamiento

**Congelar `master-architecture.md` v4.0 AHORA.** Es completo, correcto, y coherente. No tiene contradicciones internas. Es la fuente de verdad y debe quedar bloqueado en esta versión.

**No congelar los documentos periféricos todavía.** Primero ejecutar las 4 acciones bloqueantes (A01–A04).

### Secuencia recomendada

```
Paso 1 (hoy):
  - Congelar master-architecture.md como v4.0-FREEZE
  - Ejecutar A01: actualizar dedicated-extractor-design.md (30 min)
  - Ejecutar A02: actualizar cloud-connector-queue-mode-design.md (20 min)
  - Ejecutar A04: actualizar commercial-mvp-strategy.md (1 hora)

Paso 2 (esta semana):
  - Decidir entre @DBS_CHANGES vs @DBI_SYNC_QUEUE (requiere decisión técnica)
  - Ejecutar A03: crear ADR-010 + documento SL Delta canónico (2 horas)

Paso 3 (post primera semana):
  - Ejecutar A05–A14: acciones de calidad
  - Crear ADR-008, ADR-009, ADR-011
```

### Qué significa "congelar"

Congelar la arquitectura v4.0 significa que:
1. `master-architecture.md` no cambia sin un ADR aprobado.
2. Cualquier nueva decisión arquitectónica genera un ADR numerado.
3. Los documentos periféricos pueden actualizarse para alinearse con el master, pero no pueden contradecirlo sin un ADR nuevo.
4. El inicio de implementación puede comenzar usando `master-architecture.md` como referencia primaria.

### Riesgos de congelar antes de resolver las acciones bloqueantes

Si se empieza a implementar con `dedicated-extractor-design.md` sin actualizar:
- El equipo podría provisionar Azure SQL innecesariamente.
- El cloud connector podría implementarse como Azure Function en lugar de BackgroundService.
- El SAP B1 del primer cliente podría configurarse con el UDT equivocado.

Estos tres errores son costosos de revertir en producción. Por eso se clasifican como bloqueantes.

---

*Auditoría completada. Ningún documento fue modificado durante esta revisión. Los cambios se ejecutan en una fase separada.*
