# DataBision — Azure SQL Staging Design

Status: **Diseño técnico — sin implementación.**
Capa intermedia común a ambas modalidades de extracción (Dedicated Extractor + Cloud Connector). Power BI fuera de alcance salvo como consumidor downstream.

---

## 1. Objetivo de Azure SQL dentro de DataBision

Azure SQL es la **base intermedia común** entre SAP B1 (origen) y la capa de visualización/reportería (destino). Cumple cuatro funciones:

1. **Réplica fiel del estado SAP** en capa `raw` — fuente de auditoría y reconciliación.
2. **Limpieza + tipado + denormalización** en capa `stg` — datos listos para análisis sin tocar formato SAP.
3. **Modelo dimensional** en capas `dim`/`fact` — star schema RLS-ready, alimenta Power BI futuro y queries del portal.
4. **Plano de control y auditoría** en capas `ctl`/`audit` — checkpoints, runs, errores, calidad, trazabilidad end-to-end.

Una **Azure SQL DB por tenant DataBision** (un cliente puede contener varias sociedades SAP B1; se distinguen por `company_id`). No hay multi-tenancy a nivel SQL — el aislamiento es por base de datos, no por schema ni por fila.

### MVP: una DB por tenant

**Decisión MVP:** una Azure SQL Database dedicada por tenant. Cada tenant es una DB independiente, con su propio plan de servicio, backups, accesos y ciclo de vida.

| Dimensión | Impacto de DB por tenant |
|---|---|
| **Provisioning** | Script idempotente por tenant al onboarding. DDL versionado con DbUp/Flyway. |
| **Backups** | PITR independiente por DB. Restaurar un tenant no afecta otros. |
| **Monitoreo** | Métricas de DTU/CPU/storage por DB. Alerta por tenant sin cross-contamination. |
| **Migraciones** | Cada tenant puede estar en versión de schema diferente durante rollouts graduales. |
| **Costos** | DTU/vCore mínimo por DB; se optimiza individualmente. Sin noisy-neighbor. |

**Evolución futura:** cuando el número de tenants supere ~20 y el costo operacional sea dominante, evaluar migración a **Elastic Pool** (DBs compartiendo un pool de DTU/vCore). Trade-off: noisy-neighbor posible si un tenant tiene carga alta puntual. Requiere validar que todas las DBs estén en el mismo tier y región.

### Principios

- **Una sola fuente de verdad:** ambos modos (A, B) escriben al mismo `raw` con la misma forma. Diferencia solo en `ingestion_mode`.
- **Idempotencia obligatoria:** cualquier batch repetido produce el mismo estado.
- **Trazabilidad completa:** cada fila en `raw` apunta a su `extraction_run_id` y `source_*`.
- **Capa raw es réplica idempotente:** se hace UPSERT para reflejar el estado actual de SAP. No es inmutable (se actualiza con datos más recientes), pero tampoco se transforma. Ver §6 para detalle.
- **`stg` y arriba no saben qué modalidad alimentó qué fila.**

---

## 2. Arquitectura general raw / staging / curated

```
┌───────────────────────────────────────────────────────────────────────┐
│  CAPA raw    — espejo SAP, sin transformaciones                       │
│  ─────────────────────────────────────────                            │
│  raw.sap_oinv  raw.sap_inv1  raw.sap_orin  raw.sap_rin1               │
│  raw.sap_ocrd  raw.sap_oitm  raw.sap_oslp                             │
│                                                                        │
│  Reglas: UPSERT idempotente, columnas técnicas + columnas SAP         │
└────────────────────────────┬──────────────────────────────────────────┘
                              │  refresh job (continuo / horario)
                              ▼
┌───────────────────────────────────────────────────────────────────────┐
│  CAPA stg    — tipado, limpio, denormalizado                          │
│  ─────────────────────────────────────────                            │
│  stg.customer  stg.item  stg.salesperson                              │
│  stg.sales_invoice_header  stg.sales_invoice_line                     │
│  stg.sales_credit_memo_header  stg.sales_credit_memo_line             │
│                                                                        │
│  Reglas: tipos consistentes, flags derivados, joins resueltos         │
└────────────────────────────┬──────────────────────────────────────────┘
                              │  refresh job (nocturno)
                              ▼
┌───────────────────────────────────────────────────────────────────────┐
│  CAPA dim / fact   — star schema, RLS-ready                           │
│  ─────────────────────────────────────────                            │
│  dim.company  dim.customer  dim.item  dim.salesperson  dim.date       │
│  fact.sales  fact.sales_credit                                         │
│                                                                        │
│  Reglas: surrogate keys, RLS por company_id, agregable                │
└────────────────────────────┬──────────────────────────────────────────┘
                              │  consumido por
                              ▼
┌───────────────────────────────────────────────────────────────────────┐
│  Portal DataBision (API)  /  Power BI futuro  /  Exportadores         │
└───────────────────────────────────────────────────────────────────────┘

┌──── Planos transversales ──────────────────────────────────────────────┐
│                                                                         │
│  ctl   — control: runs, checkpoints, config, errores                   │
│  audit — auditoría: ingestion events, data quality, BI refresh         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Frecuencia de refresh entre capas

| Capa origen | Capa destino | Mecanismo | Frecuencia |
|---|---|---|---|
| Source SAP | `raw.*` | Ingest API + MERGE | Continuo (cada batch) |
| `raw.*` | `stg.*` | Stored procedures | Cada hora (configurable) |
| `stg.*` | `dim.*` / `fact.*` | Stored procedures | Nocturno (03:00 tenant TZ) |

---

## 3. Esquemas recomendados

| Schema | Propósito | Quién escribe | Quién lee |
|---|---|---|---|
| `raw` | Espejo SAP fiel | Ingest API | Refresh jobs, soporte |
| `stg` | Tipado/limpio/denormalizado | Refresh jobs (raw → stg) | Refresh jobs (stg → dim/fact), portal API |
| `dim` | Dimensiones star schema | Refresh jobs (stg → dim) | Portal API, Power BI futuro |
| `fact` | Hechos star schema | Refresh jobs (stg → fact) | Portal API, Power BI futuro |
| `ctl` | Control de pipeline | Ingest API, refresh jobs | Soporte, dashboards admin |
| `audit` | Eventos de auditoría | Ingest API, refresh jobs, portal | Compliance, soporte |
| `ops` (opcional) | Operación: leader locks, jobs | Procesos internos | Procesos internos |

Cada schema con permisos diferenciados (ver §31).

---

## 4. Naming convention

### Reglas

- **Tablas:** `snake_case`, singular cuando posible. Schema prefijo obligatorio.
  - `raw.sap_oinv` (raw mantiene prefijo `sap_` para señalizar origen exacto).
  - `stg.customer`, `stg.sales_invoice_header`.
  - `dim.customer`, `fact.sales`.
- **Columnas:** `snake_case`, descriptivas, sin abreviaturas opacas.
  - `doc_entry`, `card_code`, `update_date`, `update_ts_norm`.
- **Columnas técnicas:** prefijo `_` solo si son metadata pura del pipeline (`_batch_id`, `_ingested_at`). Resto sin prefijo.
- **Claves surrogadas en `dim`:** sufijo `_sk` (`customer_sk`, `company_sk`).
- **Claves de negocio en `dim`/`fact`:** sufijo `_bk` (`customer_bk`).
- **Foreign keys:** mismo nombre que la columna referenciada (no `fk_xxx`).
- **Constraints:**
  - PK: `pk_<schema>_<table>`
  - FK: `fk_<schema>_<table>_<referenced_table>`
  - Index: `ix_<schema>_<table>_<columns>`
  - Unique: `uq_<schema>_<table>_<columns>`
  - Check: `ck_<schema>_<table>_<column>`
- **Vistas:** prefijo `vw_` opcional (`stg.vw_customer_active`).
- **Stored procedures:** prefijo `sp_` (`sp_refresh_stg_customer`).
- **Funciones:** prefijo `fn_` (`fn_normalize_ts`).

### Convenciones de tipos

| Concepto | Tipo Azure SQL |
|---|---|
| PK surrogate | `BIGINT IDENTITY(1,1)` |
| PK natural string | `NVARCHAR(50)` |
| PK natural numérica | `INT` |
| Texto corto | `NVARCHAR(100)` |
| Texto medio | `NVARCHAR(500)` |
| Texto largo | `NVARCHAR(MAX)` |
| Money / decimales | `DECIMAL(19, 6)` |
| Cantidad | `DECIMAL(19, 6)` |
| Porcentajes | `DECIMAL(9, 4)` |
| Fechas (sin hora) | `DATE` |
| Timestamps UTC | `DATETIME2(3)` |
| Booleanos | `BIT` |
| TS normalizado | `CHAR(6)` (HHMMSS LPAD) |
| Hash | `BINARY(32)` (SHA-256) |
| UUID | `UNIQUEIDENTIFIER` |
| Rowversion | `ROWVERSION` (auto) |

---

## 5. Modelo multiempresa

### Definiciones canónicas (no mezclar)

| Término | Definición | Tipo | Ejemplo |
|---|---|---|---|
| **tenant** | **Cliente comercial DataBision** — la organización que contrató el servicio. Tiene un subdominio en `databision.app`. | `INT tenant_id` + `NVARCHAR tenant_slug` | Holding "Acme Corp", slug `"acme"` |
| **company** | **Sociedad SAP B1** dentro del tenant — una base de datos SAP independiente. Un tenant puede tener 1 a N companies. | `INT company_id` + `NVARCHAR company_slug` | Acme Argentina → `company_slug = "acme-ar"` |
| **source_database** | **Nombre real de la base/schema SAP** en HANA o SQL Server. Es el string de conexión, no el nombre de negocio. | `NVARCHAR source_database` | `"SBO_ACME_AR"` (HANA), `"SBO_ACME_CL"` (SQL Server) |
| **source_system** | Familia tecnológica del origen SAP | `NVARCHAR source_system` | `"SAP_B1_HANA"`, `"SAP_B1_SQL"` |

> **Regla de oro:** un `tenant_id` puede tener N `company_id`. Cada `company_id` tiene exactamente un `source_database`. Un `source_database` no se comparte entre tenants distintos.

### Ejemplo: holding con dos sociedades SAP

```
Tenant:  Acme Corp (tenant_id=42, tenant_slug="acme")
         └── Company 1: Acme Argentina
             company_id=1, company_slug="acme-ar"
             source_database="SBO_ACME_AR"
             source_system="SAP_B1_HANA"

         └── Company 2: Acme Chile
             company_id=2, company_slug="acme-cl"
             source_database="SBO_ACME_CL"
             source_system="SAP_B1_SQL"
```

- Las dos sociedades comparten la misma Azure SQL DB (tenant_id=42).
- Toda tabla en raw/stg/dim/fact tiene `company_id` como columna obligatoria.
- Los reportes de "Acme Corp consolidado" hacen `WHERE company_id IN (1, 2)`.
- Los reportes de "solo Argentina" hacen `WHERE company_id = 1`.
- Un usuario con acceso solo a Argentina recibe `company_id=1` en su JWT → filtra automáticamente.

### Aislamiento

- **Una Azure SQL DB por tenant.** Tenant_id es redundante a nivel de DB (la DB IS el tenant), pero se mantiene en todas las tablas — ver §26 para la justificación detallada.
- **Múltiples sociedades en el mismo tenant:** muy común en holdings. Se distinguen por `company_id`. Toda query downstream filtra por `company_id` explícitamente.
- **RLS futuro (§26):** los predicados de RLS usarán `company_id` (no `tenant_id`).

### Tabla canónica de empresas

```sql
dim.company:
  company_sk           BIGINT PK
  tenant_id            INT
  company_id           INT
  company_slug         NVARCHAR(50)
  company_name         NVARCHAR(200)
  source_database      NVARCHAR(50)
  source_system        NVARCHAR(30)
  base_currency        NVARCHAR(10)
  fiscal_year_start    DATE
  timezone             NVARCHAR(50)
  is_active            BIT
  valid_from           DATE
  valid_to             DATE NULL
```

Anclaje de RLS y join estrella.

---

## 6. Tablas raw MVP

Estructura común: columnas SAP originales (preservando nombres) + columnas técnicas (§11). Naming `raw.sap_<tabla>` en lowercase.

| Tabla raw | Origen SAP | Tipo |
|---|---|---|
| `raw.sap_ocrd` | OCRD — Business Partners | Master |
| `raw.sap_oitm` | OITM — Items | Master |
| `raw.sap_oslp` | OSLP — Salespersons | Master pequeño |
| `raw.sap_oinv` | OINV — A/R Invoices header | Documento cabecera |
| `raw.sap_inv1` | INV1 — A/R Invoice lines | Documento líneas |
| `raw.sap_orin` | ORIN — A/R Credit Memos header | Documento cabecera |
| `raw.sap_rin1` | RIN1 — A/R Credit Memo lines | Documento líneas |

### Qué significa "réplica idempotente"

`raw` **no es inmutable**. Se hace UPSERT (MERGE o DELETE+INSERT) para reflejar el estado actual de SAP. Lo que distingue a `raw` de `stg`/`fact`:

- **No se transforma:** tipos, nombres SAP y valores se preservan sin derivar ni calcular.
- **No se borra por lógica de negocio:** cancelaciones = actualización del flag, no DELETE.
- **Es idempotente:** el mismo batch aplicado dos veces produce el mismo estado final.
- **Es auditable:** cada fila tiene `extraction_run_id`, `_batch_id`, `_ingested_at` para trazabilidad completa.

La palabra "inmutable" es incorrecta. La definición correcta es: **réplica fiel e idempotente del estado de SAP en un momento dado**.

#### `raw_history` / `raw_change_log` (no MVP)

Si en el futuro se requiere auditoría fina de cambios por fila (ej. "qué valores tenía esta factura hace 3 días"), se puede agregar un trigger o CDC que popule una tabla `raw_history`. **No se implementa en MVP** — el `_batch_id` y `audit.ingestion_event` permiten reconstrucción suficiente para soporte.

### Reglas de raw

1. Preservar nombres SAP originales para columnas de negocio (`DocEntry`, `CardCode`, etc.). Excepción: `snake_case` en columnas técnicas DataBision (`tenant_id`, `extraction_run_id`).
2. NULL respetado tal como viene de SAP.
3. Sin transformaciones de tipos: `DocStatus` queda `NVARCHAR(1)` con valores `'O'`/`'C'`.
4. Cancelaciones se reflejan en `Canceled = 'Y'` — sin DELETE.
5. Borrados físicos en SAP (raros) → marcar `_is_deleted = 1`, no borrar fila.

### Expansión futura (no MVP)

- `raw.sap_ordr` (Sales Orders)
- `raw.sap_odln` (Delivery Notes)
- `raw.sap_opch` (A/P Invoices)
- `raw.sap_oinm` (Inventory Transactions)
- `raw.sap_ojdt` / `raw.sap_jdt1` (Journal Entries)

---

## 7. Tablas staging

Capa tipada, denormalizada parcialmente. Refrescada periódicamente desde `raw.*` por stored procedures.

| Tabla stg | Fuente raw | Notas |
|---|---|---|
| `stg.customer` | `raw.sap_ocrd` filtrado `CardType='C'` | Solo clientes; flags `is_active`, `is_blocked` derivados |
| `stg.vendor` (futuro) | `raw.sap_ocrd` filtrado `CardType='S'` | Por simetría — fuera de MVP |
| `stg.item` | `raw.sap_oitm` | Tipado consistente; columnas derivadas (`is_sellable`, `is_inventory`) |
| `stg.salesperson` | `raw.sap_oslp` | Espejo limpio |
| `stg.sales_invoice_header` | `raw.sap_oinv` + join | Flags `is_canceled`, `is_open`, `is_paid` |
| `stg.sales_invoice_line` | `raw.sap_inv1` + join header | Incluye `card_code` desnormalizado, `currency_doc` |
| `stg.sales_credit_memo_header` | `raw.sap_orin` | Idéntico patrón a invoice header |
| `stg.sales_credit_memo_line` | `raw.sap_rin1` | Idéntico patrón a invoice line |

### Reglas de stg

- Cada fila `stg` tiene un `raw_row_hash` para detectar drift entre raw y stg.
- Columnas de negocio con tipos finales (no NVARCHAR para fechas).
- Joins resueltos: en `stg.sales_invoice_line` ya viene `card_code`, `salesperson_code`, etc.
- Reglas de calidad aplicadas: filas que fallan se loguean a `audit.data_quality_event` pero NO se descartan (entran con flag `quality_status`).
- Vista alternativa (`stg.vw_*`) o tabla materializada según volumen.

---

## 8. Tablas curated (dim / fact)

Star schema clásico, optimizado para consulta.

### Dimensiones

| Tabla | Notas |
|---|---|
| `dim.company` | Anclaje RLS. SCD Type 1 (overwrites). |
| `dim.customer` | SCD Type 2 (history) para cambios significativos (nombre, status). |
| `dim.item` | SCD Type 2 para nombre/categoría. |
| `dim.salesperson` | SCD Type 1. |
| `dim.date` | Calendario poblado al deploy. |

### Hechos

| Tabla | Grano | Notas |
|---|---|---|
| `fact.sales` | Una fila por línea de factura | Une OINV + INV1 |
| `fact.sales_credit` | Una fila por línea de nota de crédito | Une ORIN + RIN1 |

### Por qué fact.sales y fact.sales_credit separados

Aunque conceptualmente son ventas con signo, mantener separados permite:
- Reportes específicos de devoluciones / cancelaciones contables.
- Distinto régimen tributario en algunos países.
- Joins más simples downstream.

Una vista `fact.vw_sales_net` puede unir ambos con signo si se necesita.

### SCD Type 2 (cuando aplica)

Columnas estándar en `dim.customer` y `dim.item`:
- `valid_from` DATE
- `valid_to` DATE NULL
- `is_current` BIT
- `customer_sk` cambia con cada versión; `customer_bk` (= `card_code`) se mantiene.

`fact.sales` referencia `customer_sk` específico al momento de la venta — preserva historia de cómo se llamaba el cliente entonces.

---

## 9. Tablas control (ctl)

### `ctl.extraction_run`

Una fila por ejecución del extractor (Modalidad A) o connector (Modalidad B).

### `ctl.extraction_run_detail`

Detalle por (run × tabla extraída). Una fila por tabla procesada en un run.

### `ctl.extraction_checkpoint`

Triple watermark por (tenant, object). Una sola fila por objeto observado.

### `ctl.extraction_error`

Errores no terminales del pipeline. Una fila por incidente.

### `ctl.source_object_config`

Catálogo de objetos observados por tenant. Permite habilitar/deshabilitar tablas, ajustar frecuencias, lookbacks y configuración de extracción por objeto sin tocar código. Es la fuente de verdad que el agente/connector lee al arrancar cada ciclo.

```sql
ctl.source_object_config:
  -- Identificación
  tenant_id               INT          NOT NULL,
  company_id              INT          NOT NULL,
  source_object           NVARCHAR(50) NOT NULL,  -- nombre lógico: 'OINV', 'INV1', 'OCRD'

  -- Control de ejecución
  enabled                 BIT          NOT NULL DEFAULT 1,
  extraction_mode         NVARCHAR(20) NOT NULL,  -- INCREMENTAL / FULL_REFRESH / DISABLED

  -- Frecuencia
  frequency_minutes       INT          NOT NULL DEFAULT 60,   -- cada cuántos minutos corre

  -- Lookback configurable por nivel
  lookback_normal_hours   INT          NOT NULL DEFAULT 2,    -- ciclo normal
  lookback_nightly_hours  INT          NOT NULL DEFAULT 24,   -- ejecución nocturna
  lookback_month_close_days INT        NOT NULL DEFAULT 7,    -- cierre mensual

  -- Carga inicial
  initial_load_from_date  DATE         NULL,  -- NULL = sin límite (carga todo)

  -- Capacidades del objeto en SAP
  supports_update_ts      BIT          NOT NULL DEFAULT 1,  -- tiene columna UpdateTS
  supports_create_ts      BIT          NOT NULL DEFAULT 1,  -- tiene columna CreateTS
  is_master_data          BIT          NOT NULL DEFAULT 0,  -- maestro vs documento

  -- Relación cabecera/líneas
  header_table            NVARCHAR(50) NULL,   -- tabla cabecera (para objetos línea)
  line_table              NVARCHAR(50) NULL,   -- tabla línea (para objetos cabecera con líneas)

  -- Claves
  primary_key             NVARCHAR(200) NOT NULL,  -- columna(s) PK, separadas por coma: 'DocEntry'
  natural_key             NVARCHAR(200) NOT NULL,  -- clave de negocio: 'DocEntry' o 'CardCode'

  -- Paginación y volumen
  page_size               INT          NOT NULL DEFAULT 500,
  max_per_run             INT          NOT NULL DEFAULT 5000,

  -- Retención
  retention_days_raw      INT          NOT NULL DEFAULT 730,  -- días en Azure SQL hot

  -- Alertas
  alert_email             NVARCHAR(200) NULL,

  -- Auditoría de configuración
  updated_at              DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  updated_by              NVARCHAR(100) NOT NULL,

  CONSTRAINT pk_ctl_source_object_config
      PRIMARY KEY (tenant_id, company_id, source_object)
```

#### Ejemplo de filas MVP

| `source_object` | `is_master_data` | `header_table` | `line_table` | `extraction_mode` |
|---|---|---|---|---|
| `OINV` | 0 | NULL | `INV1` | INCREMENTAL |
| `INV1` | 0 | `OINV` | NULL | INCREMENTAL |
| `ORIN` | 0 | NULL | `RIN1` | INCREMENTAL |
| `RIN1` | 0 | `ORIN` | NULL | INCREMENTAL |
| `OCRD` | 1 | NULL | NULL | INCREMENTAL |
| `OITM` | 1 | NULL | NULL | INCREMENTAL |
| `OSLP` | 1 | NULL | NULL | FULL_REFRESH |

---

## 10. Tablas auditoría (audit)

### `audit.ingestion_event`

Cada batch recibido por el Ingest API, sin importar el resultado.

```
audit.ingestion_event:
  event_id BIGINT PK,
  tenant_id, company_id, batch_id UUID,
  source_object, ingestion_mode,
  rows_received, rows_inserted, rows_updated, rows_skipped,
  bytes_received, bytes_after_decompress,
  source_hash_collisions INT,   -- filas que coincidieron por hash → skip
  occurred_at DATETIME2,
  duration_ms INT,
  status NVARCHAR(20),          -- SUCCESS / PARTIAL / FAILED
  error_message NVARCHAR(MAX) NULL
```

### `audit.data_quality_event`

Una fila por hallazgo de calidad (no por fila — agrupado por tipo+run).

```
audit.data_quality_event:
  dq_event_id BIGINT PK,
  tenant_id, company_id,
  extraction_run_id BIGINT,
  source_object, dq_rule NVARCHAR(50),
  severity NVARCHAR(10),        -- INFO / WARN / ERROR
  affected_rows INT,
  sample_keys NVARCHAR(MAX),    -- JSON con primeras 10 claves afectadas
  description NVARCHAR(500),
  detected_at DATETIME2,
  resolved_at DATETIME2 NULL,
  resolution_note NVARCHAR(500) NULL
```

### `audit.powerbi_refresh_event` (futuro)

Reservada para cuando se integre Power BI.

```
audit.powerbi_refresh_event:
  event_id BIGINT PK,
  tenant_id, company_id,
  dataset_id NVARCHAR(50),
  triggered_by NVARCHAR(50),   -- SCHEDULE / API / MANUAL
  started_at, ended_at,
  status, error_message
```

---

## 11. Campos obligatorios de trazabilidad en raw

Cada tabla `raw.*` incluye un bloque común. **Esto se aplica sin excepción** — es la disciplina que hace toda capa downstream auditable.

| Columna | Tipo | NULL? | Notas |
|---|---|---|---|
| `tenant_id` | INT | NN | Cliente DataBision |
| `company_id` | INT | NN | Sociedad SAP (dentro del tenant) |
| `company_slug` | NVARCHAR(50) | NN | Redundante pero útil en exports |
| `source_database` | NVARCHAR(50) | NN | `SBO_ACME` |
| `source_system` | NVARCHAR(30) | NN | `SAP_B1_HANA` / `SAP_B1_SQL` |
| `source_object` | NVARCHAR(20) | NN | `OINV`, `INV1`, etc. |
| `source_doc_entry` | BIGINT | NULL | Solo documentos |
| `source_line_num` | INT | NULL | Solo líneas |
| `source_card_code` | NVARCHAR(50) | NULL | Solo OCRD/documentos con CardCode |
| `source_item_code` | NVARCHAR(50) | NULL | Solo OITM/líneas con ItemCode |
| `source_create_date` | DATE | NULL | SAP CreateDate |
| `source_create_ts` | INT | NULL | SAP CreateTS raw (HHMMSS sin LPAD) |
| `source_create_ts_norm` | CHAR(6) | NULL | LPAD 6, normalizado |
| `source_update_date` | DATE | NULL | SAP UpdateDate |
| `source_update_ts` | INT | NULL | SAP UpdateTS raw |
| `source_update_ts_norm` | CHAR(6) | NULL | LPAD 6 |
| `extracted_at_utc` | DATETIME2(3) | NN | UTC cuando el agente extrajo |
| `extraction_run_id` | BIGINT | NN | FK a `ctl.extraction_run` |
| `ingestion_mode` | NVARCHAR(30) | NN | DEDICATED_HANA / SERVICE_LAYER_QUEUE / SERVICE_LAYER_DIRECT / CSV_INITIAL_LOAD |
| `source_hash` | BINARY(32) | NN | SHA-256 de columnas de negocio canonicalizadas |
| `_batch_id` | UNIQUEIDENTIFIER | NN | UUID del batch HTTP |
| `_ingested_at` | DATETIME2(3) | NN | UTC cuando Azure SQL hizo el upsert |
| `_is_deleted` | BIT | NN | Default 0 |
| `_dq_status` | NVARCHAR(20) | NN | OK / WARN / ERROR — flag de validación |
| `_rowver` | ROWVERSION | auto | Concurrencia óptima futura |

### Por qué TODOS estos campos

- **`tenant_id` + `company_id`:** RLS futuro + sanidad defensiva.
- **`source_*`:** trazabilidad fila→origen para reconciliación y soporte.
- **`source_create_*` + `source_update_*` + `_norm`:** watermark compuesto correcto (ver §15, §22).
- **`extraction_run_id`:** correlación con telemetría completa del run.
- **`ingestion_mode`:** debugging cross-modalidad (¿por qué esta fila vino con CSV cuando esperaba SL?).
- **`source_hash`:** dedupe (§14) y skip de updates sin cambios.
- **`_batch_id`:** rollback de un batch específico.
- **`_is_deleted`:** SAP rara vez borra pero hay que poder marcarlo.
- **`_dq_status`:** filtrar downstream sin recalcular reglas.

### Anti-regla

- **No agregar campos "por las dudas".** Si una columna no se va a usar en `stg`/`audit`, no entra en `raw`.

---

## 12. Estrategia de upsert para cabeceras

### Patrón MERGE con guarda temporal

Para `raw.sap_oinv`, `raw.sap_orin`, `raw.sap_ocrd`, `raw.sap_oitm`, `raw.sap_oslp`:

```
MERGE raw.sap_oinv AS tgt
USING #tmp_oinv AS src
   ON tgt.tenant_id = src.tenant_id
  AND tgt.company_id = src.company_id
  AND tgt.DocEntry  = src.DocEntry
WHEN MATCHED AND (
     src.source_update_date >  tgt.source_update_date
  OR (src.source_update_date = tgt.source_update_date
      AND src.source_update_ts_norm > tgt.source_update_ts_norm)
  OR (src.source_update_date = tgt.source_update_date
      AND src.source_update_ts_norm = tgt.source_update_ts_norm
      AND src.source_hash <> tgt.source_hash)
   ) THEN UPDATE SET ...
WHEN NOT MATCHED BY TARGET THEN INSERT ...
;
```

### Reglas

- Guarda temporal compuesta: `(source_update_date, source_update_ts_norm)`.
- Tie-breaker por `source_hash` cuando timestamps iguales pero contenido distinto (raro pero posible con TN best-effort + reconciliación).
- **Skip silencioso** cuando `source_hash` coincide → audit cuenta `source_hash_collisions` (§10) para detectar reextraciones inútiles.
- **No DELETE** en raw para cabeceras. Cancelaciones via `Canceled='Y'` (§18).
- Toda transacción `SET XACT_ABORT ON` + try/catch en el SP.

### Advertencia de concurrencia en MERGE

**MERGE es seguro en MVP solo si se cumplen todas estas condiciones:**

1. La PK de la tabla target tiene un índice clustered o unique que SQL Server puede usar como clave de merge — sin esto puede haber deadlocks o lecturas sucias.
2. No hay dos procesos haciendo MERGE concurrente sobre la misma tabla para el mismo `(tenant_id, company_id)` — los batches del mismo tenant deben procesarse secuencialmente (una fila en `ctl.extraction_checkpoint` actúa de lock lógico).
3. Se usa `SET XACT_ABORT ON` y try/catch, nunca MERGE sin transacción explícita.
4. Se prueba con cargas concurrentes en staging antes de go-live con tenants de alto volumen.

**Si las condiciones no se pueden garantizar o el volumen supera ~50k filas por batch, usar la alternativa:**

#### Alternativa UPDATE + INSERT (más predecible bajo concurrencia)

```sql
BEGIN TRAN;
    -- 1. Actualizar existentes
    UPDATE tgt
    SET    tgt.CardCode = src.CardCode,
           tgt.DocTotal = src.DocTotal,
           -- ... resto de columnas ...
           tgt._ingested_at = SYSUTCDATETIME()
    FROM   raw.sap_oinv tgt
    INNER JOIN @rows src
           ON tgt.tenant_id  = src.tenant_id
          AND tgt.company_id = src.company_id
          AND tgt.DocEntry   = src.DocEntry
    WHERE  src.source_update_date >  tgt.source_update_date
       OR (src.source_update_date = tgt.source_update_date
           AND src.source_update_ts_norm > tgt.source_update_ts_norm)
       OR (src.source_update_date = tgt.source_update_date
           AND src.source_update_ts_norm = tgt.source_update_ts_norm
           AND src.source_hash <> tgt.source_hash);

    -- 2. Insertar nuevos
    INSERT INTO raw.sap_oinv (...)
    SELECT src.*
    FROM   @rows src
    WHERE  NOT EXISTS (
        SELECT 1 FROM raw.sap_oinv tgt
        WHERE  tgt.tenant_id  = src.tenant_id
          AND  tgt.company_id = src.company_id
          AND  tgt.DocEntry   = src.DocEntry
    );
COMMIT;
```

| Criterio | MERGE | UPDATE + INSERT |
|---|---|---|
| Sintaxis concisa | ✅ | Más verboso |
| Deadlock bajo concurrencia | Mayor riesgo | Menor riesgo |
| Comportamiento en batches grandes | Puede escalar peor | Predecible |
| Soporte en Azure SQL | Completo | Completo |
| **Recomendación MVP** | OK con batches < 10k y un writer por tenant | Preferir si > 10k filas o concurrencia alta |

### Variantes según tabla

| Tabla | PK MERGE | Notas |
|---|---|---|
| `raw.sap_oinv` / `raw.sap_orin` | `(tenant_id, company_id, DocEntry)` | Documentos |
| `raw.sap_ocrd` | `(tenant_id, company_id, CardCode)` | CardCode es la natural key |
| `raw.sap_oitm` | `(tenant_id, company_id, ItemCode)` | ItemCode natural key |
| `raw.sap_oslp` | `(tenant_id, company_id, SlpCode)` | Tabla chica; full refresh nocturno aceptable |

---

## 13. Estrategia para líneas — delete + insert por DocEntry

### Patrón

Para `raw.sap_inv1`, `raw.sap_rin1`:

```
BEGIN TRAN;
   DELETE FROM raw.sap_inv1
   WHERE  tenant_id = @tenant_id
     AND  company_id = @company_id
     AND  DocEntry IN (SELECT DocEntry FROM #tmp_inv1);

   INSERT INTO raw.sap_inv1 (...)
   SELECT ... FROM #tmp_inv1;
COMMIT;
```

### Justificación como MVP recomendado

| Razón | Detalle |
|---|---|
| **SAP B1 borra líneas** | El usuario puede eliminar una línea sin que `LineNum` quede en la tabla. Si usamos MERGE por `(DocEntry, LineNum)` la fila huérfana en raw queda fantasma. |
| **Idempotencia trivial** | Reprocesar el mismo DocEntry produce el mismo estado. |
| **Volumen acotado** | Una factura típica tiene 5–50 líneas. Delete+insert por DocEntry es despreciable. |
| **Simplicidad** | Cero lógica de "líneas faltantes" para resolver. |
| **Transacción atómica** | Garantiza no quedar inconsistente entre borrado y reinserción. |

### Cuándo NO usar este patrón

- Volúmenes extremos (líneas > 1000 por documento) → considerar MERGE + DELETE de huérfanas. Fuera de MVP.
- Auditoría histórica fina por línea (ej.: "esta línea cambió 3 veces") — actualmente no se requiere; staging puede capturar la información del último estado.

### Reglas operativas (reforzadas)

1. **Siempre dentro de transacción explícita** con `SET XACT_ABORT ON`. Nunca DELETE + INSERT sin transacción.
2. **Siempre filtrar por `company_id` además de `DocEntry`.** Un bug que omita `company_id` podría borrar líneas de otro tenant si se compartiera DB en el futuro. La regla se aplica hoy como defensa.
3. **Máximo 500–1000 DocEntry por batch.** Si el agente/connector envía más, el Ingest API debe dividir en batches antes de llamar al SP. Nunca pasar una lista de DocEntry sin límite superior.
4. **Nunca hacer DELETE sin lista cerrada de DocEntry.** El SP recibe la tabla `@rows` con los DocEntry que se van a reinsertar — ese es el conjunto cerrado. No hacer `DELETE WHERE DocEntry IN (SELECT DocEntry FROM raw.sap_oinv WHERE DocDate > ...)` porque ese conjunto no es determinístico.
5. **No borrar líneas si la cabecera no fue confirmada en el mismo batch o en un batch previo confirmado.** Si el Ingest API recibe solo líneas sin la cabecera correspondiente (posible en pipelines asíncronos), retener las líneas en staging y no hacer DELETE+INSERT hasta confirmar que la cabecera existe en `raw.sap_oinv`. El SP debe verificar que cada `DocEntry` en `@rows` existe en `raw.sap_oinv` antes de proceder.
6. **Transacción aislada con `READ COMMITTED SNAPSHOT` activado a nivel de DB.**
7. **Latch en `raw.sap_inv1` por DocEntry:** microsegundos bajo carga normal. Sin conflicto con upstream readers (que filtran por `DocStatus` o fechas).

```sql
-- Verificación de cabeceras antes del DELETE+INSERT (regla 5)
IF EXISTS (
    SELECT 1 FROM @doc_entries d
    WHERE NOT EXISTS (
        SELECT 1 FROM raw.sap_oinv h
        WHERE h.tenant_id  = d.tenant_id
          AND h.company_id = d.company_id
          AND h.DocEntry   = d.DocEntry
    )
)
BEGIN
    RAISERROR('ORPHAN_LINES_REJECTED: DocEntry sin cabecera en raw.sap_oinv', 16, 1);
    RETURN;
END
```

---

## 14. Estrategia de deduplicación

### En raw

- PK compuesta `(tenant_id, company_id, natural_key)` previene duplicados de identidad.
- `source_hash` (§15) previene re-aplicar el mismo contenido.

### En el Ingest API (antes del MERGE)

- Si llegan dos filas con la misma natural key en el mismo batch (raro pero posible con reconciliación + TN concurrente):
  1. Ordenar por `(source_update_date, source_update_ts_norm)` descendente.
  2. Tomar la primera (la más reciente).
  3. Marcar el resto como `skipped_in_batch` en audit.

### Cross-modalidad

- En Modalidad B, el conector ya hace dedupe in-page (§14 del cloud connector doc) → llega a Ingest API limpio.
- En Modalidad A, el agente garantiza orden monotónico por keyset → no debería duplicar.
- El Ingest API confía pero verifica: dedupe in-batch defensivo siempre activo.

---

## 15. Estrategia `source_hash` para detectar cambios

### Cálculo

`source_hash = SHA-256(canonical_json(business_columns))`

Donde:
- `business_columns`: lista explícita y versionada de columnas de negocio (no técnicas).
- `canonical_json`: serialización determinística — keys ordenadas alfabéticamente, NULLs como `null`, decimales con precisión fija, strings sin trailing whitespace.

Calculado **en el agente / connector**, no en Azure SQL. Llega al Ingest API como parte del payload.

### Uso

1. Al hacer MERGE: si `src.source_hash = tgt.source_hash`, **se omite el UPDATE** aunque `source_update_date` haya cambiado.
2. Auditoría incrementa `source_hash_collisions` cuando esto pasa.
3. Métricas: ratio `(hash_collisions / rows_received)` indica eficiencia de reconciliación (alta colisión = reconciliación reextrayendo data idéntica = ajustar lookbacks).

### Columnas incluidas (ejemplo OINV)

```
[
  "Canceled", "CancelDate", "CardCode", "DocDate", "DocDueDate",
  "DocNum", "DocStatus", "DocTotal", "DocTotalFc", "DocCur",
  "GroupNum", "Indicator", "ObjType", "SlpCode",
  "TaxDate", "Comments",
  "CreateDate", "CreateTS", "UpdateDate", "UpdateTS"
]
```

Específico por tabla, listado en `ctl.source_object_config` versionado.

### Beneficio

- Reduce I/O downstream (refresh stg/dim/fact se entera solo de cambios reales).
- Detecta divergencias raras (mismo timestamp, distinto contenido) → audit flagea para revisión.
- Permite "soft retry" de batches sin escribir si nada cambió.

---

## 16. Índices recomendados

### En `raw.*`

| Tabla | Índice | Tipo |
|---|---|---|
| `raw.sap_oinv` | `(tenant_id, company_id, DocEntry)` | Clustered PK |
| `raw.sap_oinv` | `(tenant_id, company_id, source_update_date, source_update_ts_norm)` | Non-clustered |
| `raw.sap_oinv` | `(tenant_id, company_id, CardCode, DocDate)` | Non-clustered, para queries por cliente |
| `raw.sap_oinv` | `(tenant_id, company_id, source_create_date, source_create_ts_norm)` | Non-clustered, para reconciliación de altas |
| `raw.sap_inv1` | `(tenant_id, company_id, DocEntry, LineNum)` | Clustered PK |
| `raw.sap_inv1` | `(tenant_id, company_id, ItemCode)` | Non-clustered |
| `raw.sap_ocrd` | `(tenant_id, company_id, CardCode)` | Clustered PK |
| `raw.sap_ocrd` | `(tenant_id, company_id, source_update_date, source_update_ts_norm)` | Non-clustered |
| `raw.sap_oitm` | análogo | |

### En `stg.*` y `dim.*`

- Clustered por surrogate key.
- Non-clustered por natural key + flags más usados (`is_active`, `is_current`).

### En `fact.*`

- Clustered columnstore en tablas grandes (`fact.sales` > 1M filas).
- O clustered tradicional por `(company_id, date_sk, customer_sk)` si volumen modesto.

### En `ctl.*` y `audit.*`

- `ctl.extraction_run`: clustered por `started_at` (queries de tendencia).
- `ctl.extraction_error`: clustered por `occurred_at`.
- `audit.ingestion_event`: clustered por `occurred_at`.

### Reglas

- No más de 5 índices por tabla raw — costo de mantener en MERGE.
- Filtered indexes para flags (`WHERE _is_deleted = 0`).
- Incluir columnas covering donde el query plan lo justifique (medir con SET STATISTICS IO antes de agregar).

---

## 17. Constraints recomendadas

### Primary Keys

- Todas las tablas raw, stg, dim, fact, ctl, audit con PK explícita.
- raw: PK natural (compuesta con `tenant_id`, `company_id`).
- stg/dim/fact: PK surrogate `BIGINT IDENTITY`.

### Foreign Keys

| Origen | Destino | Notas |
|---|---|---|
| `raw.sap_inv1.DocEntry` → `raw.sap_oinv.DocEntry` | **NO se crea FK** en raw — raw es laxa por diseño (puede llegar línea antes que cabecera). Validación en `stg`. |
| `stg.sales_invoice_line.invoice_header_sk` → `stg.sales_invoice_header.invoice_header_sk` | Sí FK |
| `fact.sales.customer_sk` → `dim.customer.customer_sk` | Sí FK |
| `fact.sales.item_sk` → `dim.item.item_sk` | Sí FK |
| `fact.sales.company_sk` → `dim.company.company_sk` | Sí FK |
| `fact.sales.date_sk` → `dim.date.date_sk` | Sí FK |

### Check Constraints

- `ck_raw_sap_oinv_canceled CHECK (Canceled IN ('Y','N'))`
- `ck_raw_sap_oinv_doc_status CHECK (DocStatus IN ('O','C'))`
- `ck_ingestion_mode CHECK (ingestion_mode IN ('DEDICATED_HANA','SERVICE_LAYER_QUEUE','SERVICE_LAYER_DIRECT','CSV_INITIAL_LOAD'))`
- `ck_extraction_run_status CHECK (status IN ('RUNNING','SUCCESS','FAILED','PARTIAL'))`

### Unique Constraints

- `uq_dim_customer_bk_current`: `(company_id, customer_bk) WHERE is_current = 1`. Asegura una sola versión actual por cliente.
- Similar para dim.item.

### NOT NULL

- Columnas de trazabilidad (§11) todas NN excepto las que son condicionales (`source_doc_entry` NULL en OCRD).
- `tenant_id`, `company_id` NN en TODAS las tablas.

---

## 18. Manejo de cancelaciones

### En SAP B1

Cancelar una factura:
- Setea `Canceled='Y'` en cabecera.
- Llena `CancelDate`.
- Actualiza `UpdateDate`/`UpdateTS`.
- Genera documento de cancelación con `ObjType` distinto (no se procesa en MVP).
- `DocStatus` puede pasar a `'C'`.

### En `raw`

- Upsert estándar refleja `Canceled='Y'` en `raw.sap_oinv`.
- Sin DELETE.

### En `stg`

- `stg.sales_invoice_header.is_canceled = (Canceled = 'Y')`.
- Filas canceladas se mantienen en stg con flag.

### En `fact`

- `fact.sales` incluye filas canceladas con `is_canceled = 1`.
- Reportes ejecutivos filtran por defecto `WHERE is_canceled = 0`.
- Reportes contables consolidados pueden incluirlas.

### Cancelaciones tardías

- Una factura emitida hace 6 meses puede cancelarse hoy → `UpdateDate` cambia.
- Cubierta por lookback semanal/mensual (§21).

---

## 19. Manejo de notas de crédito

### Mismas reglas que factura

- `raw.sap_orin` ↔ `raw.sap_rin1` (espejo de OINV/INV1).
- `stg.sales_credit_memo_header` ↔ `stg.sales_credit_memo_line`.
- `fact.sales_credit` independiente de `fact.sales`.

### Reportes consolidados

Una vista `fact.vw_sales_net` puede unir:

```sql
SELECT ..., DocTotal AS amount FROM fact.sales WHERE is_canceled = 0
UNION ALL
SELECT ..., -DocTotal AS amount FROM fact.sales_credit WHERE is_canceled = 0
```

Para análisis "venta neta".

### Vinculación con factura origen

- Algunas NCs en SAP B1 referencian la factura original via `BaseEntry` en RIN1 → INV1.
- En stg/fact se preserva el vínculo (`linked_invoice_sk`) cuando existe.

---

## 20. Manejo de datos maestros

### OCRD (Business Partners)

- `raw.sap_ocrd` incluye clientes y proveedores (CardType `C`/`S`/`L`).
- `stg.customer` filtra `CardType = 'C'`.
- `dim.customer` SCD Type 2: cambios en `card_name`, `group_code`, `frozen_for` versionan.

### OITM (Items)

- `raw.sap_oitm` espejo completo.
- `stg.item` excluye campos volátiles de stock (`OnHand`, `IsCommited`) — para stock real se usa OINM fase futura.
- `dim.item` SCD Type 2 sobre `item_name`, `items_group_code`.

### OSLP (Salespersons)

- Tabla chica, refresh nocturno completo (TRUNCATE + INSERT por tenant).
- `stg.salesperson` + `dim.salesperson` SCD Type 1.

### Reglas

- Maestros no se borran lógicamente en SAP normalmente. Si lo hacen, `_is_deleted` marca y SCD2 cierra la versión actual con `valid_to = now`.
- Refresh de stg de maestros: cada hora.
- Refresh de dim: nocturno (consolida cambios del día y resuelve SCD2).

---

## 21. Manejo de lookback

Idéntico patrón a Modalidad A doc §16 — aplica en el lado del extractor/connector que escribe a `raw`. El **Azure SQL no decide** el lookback; lo aplica el agente al armar la query HANA o el connector al armar el filtro de cola.

Resumen:

| Nivel | Cuándo | Lookback |
|---|---|---|
| Normal | Default | 2 horas |
| Nocturno | 02:00 local | 24 horas |
| Mes | Día 1 mes | 7 días |
| Año | 2 enero | 30 días |

### En Azure SQL

- Reconciliación (§22) usa los mismos niveles para sus comparaciones contra SAP.
- `ctl.source_object_config.lookback_*` permite override por tenant.

---

## 22. Manejo de reconciliación diaria

### Job nocturno: `sp_reconcile_against_source`

Para cada `(tenant, company, table)`:

1. Llamar al agente (Modalidad A) o function (Modalidad B) vía endpoint `/api/recon/sample` para obtener:
   - `count_by_date` últimas 24h
   - `sum_doctotal` últimas 24h (donde aplica)
   - `max_doc_entry` últimas 24h
   - `max_update_date`, `max_update_ts_norm` últimas 24h
2. Comparar contra `raw.*`.
3. Si divergencia:
   - Loguear `audit.data_quality_event` con `dq_rule = 'recon_mismatch'`.
   - Encolar reextraction del rango específico (no full re-extract).

### Estrategia escalonada

| Frecuencia | Lookback | Alcance |
|---|---|---|
| Hourly (08:00–20:00) | 2h | Sanity check |
| Nightly (03:00) | 24h | Reconciliación principal |
| Weekly (domingo 04:00) | 7d | Cierre semanal |
| Monthly (día 1 05:00) | 7d (30d en cierre de año) | Cierre contable |

Detalle en cloud connector doc apéndice G y dedicated extractor doc estrategia "Reconciliación diaria contra SAP".

---

## 23. Diseño de checkpoints común para ambas modalidades

### `ctl.extraction_checkpoint` (unificado)

| Columna | Tipo |
|---|---|
| `tenant_id` | INT |
| `company_id` | INT |
| `object_name` | NVARCHAR(50) |
| `last_update_date` | DATE |
| `last_update_ts_norm` | CHAR(6) |
| `last_update_key` | NVARCHAR(50) |
| `last_create_date` | DATE |
| `last_create_ts_norm` | CHAR(6) |
| `last_create_key` | NVARCHAR(50) |
| `last_run_id` | BIGINT |
| `last_run_status` | NVARCHAR(16) |
| `last_error_at` | DATETIME2 NULL |
| `last_error_message` | NVARCHAR(MAX) NULL |
| `initial_loaded` | BIT |
| `initial_loaded_at` | DATETIME2 NULL |
| `rows_extracted_total` | BIGINT |
| `ingestion_mode_last` | NVARCHAR(30) |
| **PK** | (tenant_id, company_id, object_name) |

### Reglas

- Mismo schema entre A y B. El campo `ingestion_mode_last` registra la modalidad del último update.
- Ambos modos actualizan via Ingest API después de cada batch confirmado.
- El conector/agente lee desde Azure (al inicio) o desde local (Modalidad A tiene copia local).
- Modalidad A: copia local en SQLite es source-of-truth operacional. Azure refleja con lag corto.
- Modalidad B: Azure SQL es source-of-truth (no hay local).

---

## 24. Diferencias de `ingestion_mode`

| Modo | Cuándo se usa | Tabla afectada | Notas |
|---|---|---|---|
| `DEDICATED_HANA` | Modalidad A | `raw.*` | Carga incremental |
| `SERVICE_LAYER_QUEUE` | Modalidad B con cola | `raw.*` | El estándar de B |
| `SERVICE_LAYER_DIRECT` | Modalidad B sin cola (fallback) | `raw.*` | Polling SL puro, raro |
| `CSV_INITIAL_LOAD` | Carga inicial CSV bulk | `raw.*` | One-shot al onboarding |

### Cómo lo usa downstream

- **Staging/dim/fact:** **no diferencian** por ingestion_mode. Todas las filas son iguales.
- **Auditoría:** discrimina por modo para entender procedencia.
- **Métricas operacionales:** throughput por modo.
- **Soporte:** debugging cross-modalidad ("¿cuántas filas vinieron por CSV vs SL?").

### Reglas

- Una sola fila en raw puede tener su `ingestion_mode` actualizado cuando otra modalidad la reescribe. La columna refleja la **última escritura ganadora**.
- Para histórico completo se consulta `audit.ingestion_event` por `batch_id`.

---

## 25. Preparación para incremental refresh de Power BI futuro

### Diseño que lo facilita

1. **Columnas `_ingested_at` y `source_update_date`** en raw/stg/fact: Power BI puede pivotar sobre ellas para incremental refresh.
2. **Fechas en DATE/DATETIME2 sin formato custom** en columnas que se usarán como filtros.
3. **fact.sales con `_dq_status` filtrable**: dataset ignora ERRORs sin necesidad de query custom.
4. **dim.date pre-poblada**: Power BI no genera dim.date — la conecta directamente.

### Pattern recomendado (sin implementar)

- Dataset PBI usa incremental refresh con:
  - RangeStart, RangeEnd parámetros sobre `_ingested_at` o `source_update_date`.
  - Política: refresh últimos 7 días + import histórico mensual.
- Particiones automáticas por Power BI Service.

### Implicancia ahora

- Indexar `source_update_date` (ya listado en §16).
- No usar tipos exotic en columnas de filtro PBI.
- Mantener consistencia tipo entre stg y fact (PBI se vuelve loco con conversiones).

---

## 26. Preparación para RLS futuro

### Diseño RLS-ready

Cada tabla downstream incluye `company_id` (y `tenant_id`).

`dim.company.company_slug` es la **anchor**: el embed token de Power BI (futuro) tendrá `username = company_slug`. La regla DAX será:

```dax
[company_slug] = USERPRINCIPALNAME()
```

Propagado al fact via relación `dim.company` ↔ `fact.sales` con cross-filter direction single (dim → fact).

### RLS en Azure SQL (defense in depth, futuro)

Adicional al RLS de Power BI, se puede activar Row-Level Security nativo de Azure SQL:

```sql
CREATE SECURITY POLICY rls.fact_sales_policy
   ADD FILTER PREDICATE rls.fn_company_filter(company_id) ON fact.sales,
   ADD BLOCK  PREDICATE rls.fn_company_filter(company_id) ON fact.sales AFTER INSERT
WITH (STATE = ON);
```

Donde `rls.fn_company_filter` lee `SESSION_CONTEXT('company_id')`. El portal/API setea este context al conectar.

### Sobre `tenant_id` en el modelo DB-por-tenant

En el modelo MVP (una DB por tenant), `tenant_id` es técnicamente redundante: la propia DB identifica al tenant. Sin embargo, se mantiene en todas las tablas por tres razones:

1. **Portabilidad futura:** si se migra a modelo de DB compartida (Elastic Pool con schema segregation o RLS SQL), `tenant_id` ya está en cada fila y no hay que hacer ALTER TABLE masivo.
2. **Exportaciones y archivos Blob:** cuando se exporta data a Parquet, el archivo pierde el contexto de la DB — `tenant_id` es el único indicador del origen.
3. **Validación defensiva:** `WHERE tenant_id = @expected_tenant_id` en queries críticos previene errores silenciosos si algún día hay una confusión de conexión.

**`company_id` y `company_slug` son obligatorios en dim y fact** — son el eje de RLS real (tanto en Power BI como en Azure SQL futuro). `tenant_id` es trazabilidad, `company_id` es el filtro de seguridad.

### Reglas

- En el MVP **no se activa RLS SQL** — el portal/API filtra explícitamente por `company_id`. Más simple, menos propenso a sorpresas.
- Para fase 2 (multi-company per tenant más complejo), activar RLS SQL como red de seguridad adicional.
- Para fase Power BI (capa downstream), activar RLS en el modelo PBI usando `company_slug = USERPRINCIPALNAME()`.
- `tenant_id` en todas las tablas es un requisito de diseño a largo plazo, no opcional aunque sea redundante hoy.

---

## 27. Data quality checks

### Reglas mínimas MVP

| Regla | Severity | Acción |
|---|---|---|
| Documento sin líneas | WARN | Loguear, no descartar |
| Líneas huérfanas (sin cabecera) | ERROR | Loguear, hold en stg hasta que aparezca cabecera |
| Cliente referenciado no existe en OCRD | WARN | Loguear, dejar fact pendiente con `customer_sk = -1` (cliente "desconocido") |
| Item referenciado no existe en OITM | WARN | Idem con `item_sk = -1` |
| `DocTotal` negativo en factura no cancelada | ERROR | Loguear; revisión manual |
| Suma de `LineTotal` != `DocTotal ± 0.01` | WARN | Loguear (puede ser por descuentos/redondeo) |
| Duplicado por `(company_id, DocEntry)` | ERROR | Conservar el de hash más reciente; loguear |
| `UpdateDate` futura | WARN | Loguear (reloj SAP desfasado) |
| `CardCode` con caracteres no imprimibles | WARN | Loguear |
| `Quantity = 0 AND LineTotal != 0` | WARN | Posible bug SAP |

### Implementación

- Stored procedure `sp_dq_check_run` invocado por jobs nocturnos.
- Resultados a `audit.data_quality_event`.
- Las filas afectadas se marcan `_dq_status` en stg/fact.

### Reglas operativas

- DQ **nunca descarta filas**. Marca y reporta.
- Decisión final sobre qué hacer con cada flag → fuera del pipeline, en el dashboard de soporte.

---

## 28. Estrategia de retención

### Principio general

La retención de `raw` debe cubrir **al menos el período contratado de reportería**. Si un cliente contrata 24 meses de análisis histórico, `raw` debe tener 24 meses. Si el contrato es 5 o 7 años, `raw` debe cubrir ese período — ya sea en hot storage (Azure SQL) o archivado en Blob Storage.

> **Regla:** `retención_raw ≥ período_comercial_contratado`. No hardcodear 24 meses sin verificar el alcance del contrato.

| Capa | Retención hot | Tier después | Justificación |
|---|---|---|---|
| `raw.*` documentos | **Período contratado** (mínimo 24 meses si MVP) | Archivo Blob/Parquet (ver abajo) | Trazabilidad y reconciliación; el histórico no siempre se puede reconstruir desde SAP |
| `raw.*` maestros | Sin retención (refresh continuo) | — | Solo estado actual relevante |
| `stg.*` | Mismo que raw downstream | — | Derivado |
| `dim.*` (Type 1) | Sin retención | — | Solo estado actual |
| `dim.*` (Type 2 history) | Sin retención | — | Histórico contable necesario |
| `fact.sales` | 7 años (compliance) | Particiones cold | Para reportes históricos contables |
| `ctl.extraction_run` | 90 días | Borrar | Operacional |
| `ctl.extraction_run_detail` | 90 días | Borrar | |
| `ctl.extraction_error` | 1 año | Borrar | Para análisis post-mortem |
| `audit.ingestion_event` | 90 días hot | 1 año warm (Blob Parquet) | Compliance ingreso |
| `audit.data_quality_event` | 1 año hot | 7 años warm | Calidad histórica |

### Archivo raw en Blob Storage (no MVP — opción futura)

Cuando la retención comercial supere lo que es razonable mantener en Azure SQL por costo:

1. Job mensual `sp_export_raw_to_blob`: exporta particiones vencidas a Blob Storage en formato **Parquet** (por año/mes/company_id).
2. Una vez confirmada la exportación en `audit.export_event`, purgar filas de `raw.*`.
3. Blob Storage tier: **Cool** para 2–7 años, **Archive** para > 7 años.
4. Para reconciliación puntual de períodos archivados: Azure Synapse Analytics o Azure Data Factory pueden leer el Parquet sin restaurar a SQL.

### Mecanismo

- Job mensual `sp_purge_old_records` borra registros > retención, **solo después de confirmada la exportación Blob si está activa**.
- Job mensual `sp_export_warm` exporta a Blob Parquet antes de borrar.
- Particiones por año en `fact.sales` para retirar particiones cold rápido.
- `ctl.source_object_config.retention_days_raw` es el override por tenant (ver §9).

### Configuración por contrato

| Alcance contratado | Retención raw recomendada | Retención fact |
|---|---|---|
| MVP sin SLA histórico | 24 meses hot | 7 años hot/cold |
| 5 años | 5 años hot o 3 hot + 2 Blob | 7 años |
| 7 años | 3 hot + 4 Blob | 7 años |
| Compliance estricto (banca, salud) | 7–10 años (hot o Blob) | 10 años |

---

## 29. Estrategia para reprocesos

### Tres niveles

#### Nivel 1 — Re-MERGE de un batch

Si un batch falló o vino con bug:
1. Identificar `_batch_id` en `audit.ingestion_event`.
2. Re-fetch del agente/connector con el mismo rango.
3. Ingest API procesa de nuevo. MERGE es idempotente; `source_hash` evita writes innecesarios.

#### Nivel 2 — Reprocesar staging desde raw

Si se cambia lógica de mapping raw → stg:
1. Truncate `stg.<tabla>` para el tenant.
2. Ejecutar `sp_refresh_stg_<tabla>` con `@full_reload = 1`.
3. Auditar diferencias con snapshot previo.

#### Nivel 3 — Reconstruir fact desde stg

Si se cambia lógica de mapping stg → fact:
1. Backup de `fact.sales` (copia a `fact.sales_backup_YYYYMMDD`).
2. Truncate `fact.sales`.
3. Ejecutar `sp_rebuild_fact_sales`.
4. Comparar conteos vs backup.
5. Si OK, drop backup tras 7 días.

### Reglas

- Reprocesos staging/fact **fuera de horario operacional** del tenant.
- Notificar al cliente si reproceso afecta reportes consumidos.
- Cada reproceso loguea en `audit.ingestion_event` con `triggered_by = 'REPROCESS'`.

---

## 30. Estrategia para rollback

### Rollback ligero (soft)

Recurso si una corrida normal "envenenó" datos:

1. `UPDATE ctl.extraction_checkpoint SET last_update_* = <valor_anterior>` (recordado de `ctl.extraction_run` previo).
2. Próxima corrida re-extrae el rango → MERGE reescribe con datos correctos.
3. `source_hash` evita writes redundantes.

### Rollback duro

Para corruption seria:

1. Restaurar desde backup de Azure SQL (PITR — Point in Time Restore).
   - Azure SQL mantiene 7 días de PITR por default.
2. Re-replay incremental desde el momento del restore.
3. Reconciliar contra SAP.

### Reglas

- **Backup**: Azure SQL automático (PITR 7 días, geo-redundant LRS).
- **Test de restore**: trimestral, documentado.
- **Comunicación**: rollback duro siempre con aviso previo al cliente.

---

## 31. Seguridad

### Reglas duras de acceso (no negociables)

1. **El frontend (React / portal cliente / admin) NUNCA accede directo a Azure SQL.** Toda consulta pasa por la API DataBision. Sin excepciones.
2. **El portal no escribe en `raw`, `stg`, ni `fact`.** Solo lee desde `stg`/`dim`/`fact` a través de la API.
3. **Power BI solo tiene acceso de lectura** a `dim` y `fact`. Nunca escribe, nunca accede a `raw`/`ctl`/`audit`.
4. **El extractor (Modalidad A) y el Ingest API son los únicos que escriben en `raw` y `ctl`.**
5. **Los refresh jobs (Modalidad A background + Azure Functions) son los únicos que transforman** raw→stg→dim/fact.
6. **Ningún proceso tiene acceso a todas las capas con escritura simultánea.** Principio de mínimo privilegio por rol.

### Roles SQL definidos

| Rol | Schemas con escritura | Schemas con lectura | Conexión desde |
|---|---|---|---|
| `extractor_writer` | `raw`, `ctl`, `audit` (INSERT/UPDATE) | — | Ingest API (Managed Identity o API key) |
| `transform_runner` | `stg`, `dim`, `fact`, `audit` (INSERT/UPDATE/DELETE) | `raw`, `ctl` (SELECT) | Refresh jobs (SQL Agent / Azure Function) |
| `portal_reader` | — | `stg`, `dim`, `fact`, `ctl` (solo consulta de estado) | Portal API (Managed Identity) |
| `powerbi_reader` | — | `dim`, `fact` | Power BI Service (futuro, Managed Identity) |
| `admin_operator` | Todos (DDL incluido) | Todos | Solo con MFA + IP autorizada. Emergency only. |

> Los roles se crean como SQL Server Database Roles y se asignan a los login/user correspondientes. No se asignan permisos directamente a usuarios — siempre a través del rol.

### Usuarios SQL recomendados

| Usuario | Rol asignado | Notas |
|---|---|---|
| `databision_ingest` | `extractor_writer` | Managed Identity si App Service; API key si agente on-prem |
| `databision_refresh` | `transform_runner` | Azure Function con Managed Identity |
| `databision_portal` | `portal_reader` | Portal API con Managed Identity |
| `databision_powerbi` (futuro) | `powerbi_reader` | Service Principal de Power BI |
| `databision_admin` | `admin_operator` | Solo para soporte de emergencia |

### Reglas de infraestructura

- **Cada usuario su password/secret en Azure Key Vault.** Rotation automática recomendada.
- **Managed Identity preferida** sobre SQL auth donde sea posible (App Service, Azure Function).
- **Ningún usuario tiene `db_owner`** salvo admin_operator, y solo en emergencias documentadas.
- **Auditoría SQL activada** para login failures y DDL (`sys.fn_get_audit_file`).
- **TDE (Transparent Data Encryption)** habilitado por default en Azure SQL.
- **Conexiones solo TLS 1.2+.**
- **Firewall**: solo IPs autorizadas (Azure services + IPs de Office si soporte necesita acceso directo).
- **Private endpoint** para Ingest API → Azure SQL (sin tráfico por Internet pública).

### Conexión desde extractor

- **El extractor (Modalidad A) NO se conecta a Azure SQL directamente.** Habla solo con Ingest API por HTTPS.
- Esto reduce surface area: agente del cliente no necesita credentials de DB, solo API key rotable.
- **El Cloud Connector (Modalidad B) tampoco conecta directo a Azure SQL.** También usa Ingest API.

---

## 32. Costos y escalabilidad inicial

### MVP (tier por tenant)

| Tenant size | Azure SQL tier | Costo aprox/mes |
|---|---|---|
| < 10k docs/día | S2 Standard (50 DTU) | ~USD 75 |
| 10k–50k docs/día | S4 Standard (200 DTU) | ~USD 300 |
| 50k–200k docs/día | P1 Premium (125 DTU) | ~USD 465 |
| > 200k docs/día | vCore General Purpose 4 vCore | ~USD 700+ |

### Almacenamiento

- Standard incluye 250 GB; Premium escalable.
- Raw + stg + dim + fact para cliente mediano: ~10–50 GB tras 2 años.

### Backups

- PITR 7 días incluido.
- LTR (Long-Term Retention) opcional para compliance: ~USD 0.05/GB/mes adicional.

### Costos transversales

- Bandwidth Azure → Power BI (futuro): mínimo si misma región.
- Azure Monitor + Log Analytics: ~USD 5–15/mo por tenant.

### Elastic Pool (futuro)

Cuando se tengan >20 tenants: mover a Elastic Pool reduce costo agregado. Trade-off: noisy neighbor risk.

---

## 33. Roadmap de implementación SQL

### Sprint 1 — esqueleto (semana 1)

- [ ] Crear DB por tenant en provisioning.
- [ ] Schemas: raw, stg, dim, fact, ctl, audit.
- [ ] Tablas `ctl.*` completas.
- [ ] Tablas `audit.*` completas.
- [ ] Usuarios SQL + roles + permisos.
- [ ] DDL de raw.sap_* tablas MVP (con columnas trazabilidad).
- [ ] Índices base en raw.

### Sprint 2 — Ingest API + raw (semana 2)

- [ ] Endpoints `/api/ingest/{tenant}/{table}` + `/api/ingest/heartbeat`.
- [ ] Stored procedures `sp_upsert_raw_<tabla>` con MERGE compuesto.
- [ ] Stored procedure `sp_upsert_raw_inv1` (delete+insert).
- [ ] Validación `source_hash` en API antes de MERGE.
- [ ] Audit en cada batch.

### Sprint 3 — staging (semana 3)

- [ ] Tablas `stg.*` con DDL.
- [ ] Stored procedures `sp_refresh_stg_<tabla>` desde raw.
- [ ] Reglas DQ aplicadas a stg.
- [ ] Jobs SQL Agent / Azure Function para refresh.

### Sprint 4 — dim/fact + cierre MVP (semana 4)

- [ ] DDL `dim.*` con SCD2 donde aplica.
- [ ] DDL `fact.sales` y `fact.sales_credit`.
- [ ] Pre-poblar `dim.date`.
- [ ] Procedures `sp_refresh_dim_*` y `sp_refresh_fact_*`.
- [ ] Tests: end-to-end raw → fact con dataset sintético.
- [ ] Documentación y runbook.

---

## 34. MVP de 5 días

Asumiendo Sprint 1–4 completos y agente / connector ya extraen:

| Día | Hito |
|---|---|
| 1 | Provisionar Azure SQL del tenant + schemas + usuarios + permisos |
| 2 | Deploy de Ingest API + raw tables + ctl tables; primera corrida del agente; validar raw poblado |
| 3 | Refresh stg activado (job cada hora); validar DQ events |
| 4 | Refresh dim/fact nocturno; validar fact.sales con datos correctos vs SAP cliente |
| 5 | Documentación, dashboards básicos, sign-off interno |

---

## 35. Criterios de aceptación

### Funcionales

1. ✅ Ambas modalidades escriben al mismo schema `raw` sin diferencias estructurales.
2. ✅ UPSERT idempotente: ejecutar el mismo batch dos veces no duplica.
3. ✅ `source_hash` previene writes innecesarios (medible: hash_collision_ratio > 50% en operación normal).
4. ✅ Líneas huérfanas se detectan y loguean (no rompen pipeline).
5. ✅ Reconciliación nocturna corre y genera audit events.
6. ✅ Refresh stg→dim→fact reproduce el mismo resultado dado el mismo raw.
7. ✅ **Equivalencia entre modalidades:** para el mismo dataset de entrada (mismas filas SAP, mismo período), Dedicated Extractor y Cloud Connector producen resultados idénticos en `stg`, `dim` y `fact`. El único campo diferente entre las dos modalidades es `ingestion_mode` en `raw`.
8. ✅ **`ingestion_mode` no cambia semántica:** un `fact.sales` generado desde `DEDICATED_HANA` es numéricamente idéntico al generado desde `SERVICE_LAYER_QUEUE` para las mismas facturas. Los refresh jobs (`sp_refresh_stg_*`, `sp_refresh_fact_*`) no leen `ingestion_mode` para ninguna decisión de negocio.

### Operacionales

7. ✅ Dashboards de soporte muestran lag por tenant/objeto en tiempo real.
8. ✅ DQ events son consultables y filtrables.
9. ✅ Reproceso de un día de raw queda en <30 min.
10. ✅ Runbook documenta los 10 escenarios de soporte más comunes.

### Seguridad

11. ✅ Cero usuario con `db_owner` excepto admin emergency.
12. ✅ Conexiones solo TLS 1.2+ validadas.
13. ✅ Auditoría SQL muestra failed logins.
14. ✅ Backup PITR confirmado tras simulación.
15. ✅ Restore test pasa.

### Performance

16. ✅ Upsert batch 5000 filas: <2s p95.
17. ✅ Query "top 10 clientes por venta últimos 30 días": <1s p95.
18. ✅ Refresh stg.sales_invoice_header completo: <5 min.
19. ✅ Refresh fact.sales delta diario: <10 min.

---

## 36. Riesgos y mitigaciones

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| **DTU saturado por refresh nocturno** | Media | Alto | Monitor; escalar a tier mayor; mover refresh a Azure Function escalada |
| Schema drift entre tenants (DDL diferente) | Alta | Alto | Migrations versionadas con DbUp / Flyway; aplicación automática en provisioning |
| Bug en MERGE corrompe raw | Baja | Crítico | Backups PITR; reproceso desde SAP; tests de regresión |
| Volumen explosivo (carga histórica + tenant grande) | Media | Alto | Particionado de fact por año; tier elásticamente escalable |
| Reglas DQ falsos positivos | Alta | Bajo | Severity WARN no bloquea; revisión periódica de reglas |
| Lookback amplio crea duplicates de hash | Media | Bajo | `source_hash` evita writes; auditoría detecta ratio anómalo |
| Reloj cliente desfasado | Media | Medio | Job de heartbeat detecta drift; alerta si >5 min |
| Llegada de batch incompleta (líneas sin cabecera) | Alta | Medio | DQ flagea; staging espera y re-evalúa cada hora |
| Power BI futuro genera load no anticipada | Media | Medio | Tier dedicado para readers de PBI; índices columnstore |
| Encoding LATIN1 vs UTF8 | Media | Medio | Forzar UTF-8 en API; validación en ingest |
| Tenant con 5 sociedades SAP saturando schema | Media | Medio | `company_id` discrimina; particionado adicional |
| Bug en refresh stg crea inconsistencia | Media | Alto | DQ comparativo stg vs raw; auto-rerun en discrepancia |
| Retención agresiva borra datos consultados | Baja | Alto | Confirmar con cliente antes de habilitar purge; soft-delete por 30 días |
| Restore PITR no funciona cuando se necesita | Baja | Crítico | Test trimestral; runbook detallado |

---

## Apéndice A — DDL ejemplos

### `ctl.extraction_run`

```sql
CREATE TABLE ctl.extraction_run (
    run_id              BIGINT IDENTITY(1,1)
                        CONSTRAINT pk_ctl_extraction_run PRIMARY KEY,
    tenant_id           INT             NOT NULL,
    company_id          INT             NOT NULL,
    agent_id            NVARCHAR(100)   NOT NULL,
    ingestion_mode      NVARCHAR(30)    NOT NULL,
    started_at          DATETIME2(3)    NOT NULL,
    ended_at            DATETIME2(3)    NULL,
    status              NVARCHAR(16)    NOT NULL,
    trigger_source      NVARCHAR(20)    NOT NULL,   -- SCHEDULE / MANUAL / RECOVERY / RECONCILE
    rows_total          INT             NOT NULL DEFAULT 0,
    error_count         INT             NOT NULL DEFAULT 0,
    extractor_version   NVARCHAR(20)    NOT NULL,
    notes               NVARCHAR(500)   NULL,

    CONSTRAINT ck_ctl_extraction_run_status
        CHECK (status IN ('RUNNING','SUCCESS','FAILED','PARTIAL')),
    CONSTRAINT ck_ctl_extraction_run_mode
        CHECK (ingestion_mode IN
              ('DEDICATED_HANA','SERVICE_LAYER_QUEUE','SERVICE_LAYER_DIRECT','CSV_INITIAL_LOAD'))
);

CREATE INDEX ix_ctl_extraction_run_started_at
    ON ctl.extraction_run (started_at DESC);
CREATE INDEX ix_ctl_extraction_run_tenant_status
    ON ctl.extraction_run (tenant_id, status, started_at DESC);
```

### `ctl.extraction_checkpoint`

```sql
CREATE TABLE ctl.extraction_checkpoint (
    tenant_id              INT             NOT NULL,
    company_id             INT             NOT NULL,
    object_name            NVARCHAR(50)    NOT NULL,

    last_update_date       DATE            NULL,
    last_update_ts_norm    CHAR(6)         NULL,
    last_update_key        NVARCHAR(50)    NULL,

    last_create_date       DATE            NULL,
    last_create_ts_norm    CHAR(6)         NULL,
    last_create_key        NVARCHAR(50)    NULL,

    last_run_id            BIGINT          NULL
                           CONSTRAINT fk_ctl_extraction_checkpoint_run
                           REFERENCES ctl.extraction_run(run_id),
    last_run_status        NVARCHAR(16)    NULL,
    last_error_at          DATETIME2(3)    NULL,
    last_error_message     NVARCHAR(MAX)   NULL,

    initial_loaded         BIT             NOT NULL DEFAULT 0,
    initial_loaded_at      DATETIME2(3)    NULL,
    rows_extracted_total   BIGINT          NOT NULL DEFAULT 0,
    ingestion_mode_last    NVARCHAR(30)    NULL,

    updated_at             DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT pk_ctl_extraction_checkpoint
        PRIMARY KEY (tenant_id, company_id, object_name)
);
```

### `raw.sap_oinv`

```sql
CREATE TABLE raw.sap_oinv (
    -- Trazabilidad
    tenant_id              INT             NOT NULL,
    company_id             INT             NOT NULL,
    company_slug           NVARCHAR(50)    NOT NULL,
    source_database        NVARCHAR(50)    NOT NULL,
    source_system          NVARCHAR(30)    NOT NULL,
    source_object          NVARCHAR(20)    NOT NULL DEFAULT 'OINV',
    extracted_at_utc       DATETIME2(3)    NOT NULL,
    extraction_run_id      BIGINT          NOT NULL,
    ingestion_mode         NVARCHAR(30)    NOT NULL,
    source_hash            BINARY(32)      NOT NULL,
    _batch_id              UNIQUEIDENTIFIER NOT NULL,
    _ingested_at           DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    _is_deleted            BIT             NOT NULL DEFAULT 0,
    _dq_status             NVARCHAR(20)    NOT NULL DEFAULT 'OK',

    -- SAP columnas de negocio (selección — listado completo versionado en spec)
    DocEntry               INT             NOT NULL,
    DocNum                 INT             NULL,
    CardCode               NVARCHAR(50)    NULL,
    CardName               NVARCHAR(200)   NULL,
    DocDate                DATE            NULL,
    DocDueDate             DATE            NULL,
    TaxDate                DATE            NULL,
    DocTotal               DECIMAL(19,6)   NULL,
    DocTotalFc             DECIMAL(19,6)   NULL,
    DocCur                 NVARCHAR(10)    NULL,
    DocStatus              NCHAR(1)        NULL,
    Canceled               NCHAR(1)        NULL,
    CancelDate             DATE            NULL,
    SlpCode                INT             NULL,
    ObjType                NVARCHAR(20)    NULL,
    GroupNum               INT             NULL,
    VatSum                 DECIMAL(19,6)   NULL,
    DiscPrcnt              DECIMAL(9,4)    NULL,
    DiscSum                DECIMAL(19,6)   NULL,
    PaidToDate             DECIMAL(19,6)   NULL,
    Comments               NVARCHAR(500)   NULL,

    -- Watermark
    source_create_date     DATE            NULL,
    source_create_ts       INT             NULL,
    source_create_ts_norm  CHAR(6)         NULL,
    source_update_date     DATE            NULL,
    source_update_ts       INT             NULL,
    source_update_ts_norm  CHAR(6)         NULL,

    _rowver                ROWVERSION      NOT NULL,

    CONSTRAINT pk_raw_sap_oinv
        PRIMARY KEY CLUSTERED (tenant_id, company_id, DocEntry),
    CONSTRAINT ck_raw_sap_oinv_canceled
        CHECK (Canceled IS NULL OR Canceled IN ('Y','N')),
    CONSTRAINT ck_raw_sap_oinv_status
        CHECK (DocStatus IS NULL OR DocStatus IN ('O','C')),
    CONSTRAINT fk_raw_sap_oinv_run
        FOREIGN KEY (extraction_run_id) REFERENCES ctl.extraction_run(run_id)
);

CREATE INDEX ix_raw_sap_oinv_update_watermark
    ON raw.sap_oinv (tenant_id, company_id, source_update_date, source_update_ts_norm)
    INCLUDE (DocEntry, source_hash);

CREATE INDEX ix_raw_sap_oinv_create_watermark
    ON raw.sap_oinv (tenant_id, company_id, source_create_date, source_create_ts_norm)
    INCLUDE (DocEntry);

CREATE INDEX ix_raw_sap_oinv_card_doc_date
    ON raw.sap_oinv (tenant_id, company_id, CardCode, DocDate DESC)
    INCLUDE (DocEntry, DocTotal, DocStatus, Canceled);
```

### `raw.sap_inv1`

```sql
CREATE TABLE raw.sap_inv1 (
    -- Trazabilidad
    tenant_id              INT             NOT NULL,
    company_id             INT             NOT NULL,
    company_slug           NVARCHAR(50)    NOT NULL,
    source_database        NVARCHAR(50)    NOT NULL,
    source_system          NVARCHAR(30)    NOT NULL,
    source_object          NVARCHAR(20)    NOT NULL DEFAULT 'INV1',
    extracted_at_utc       DATETIME2(3)    NOT NULL,
    extraction_run_id      BIGINT          NOT NULL,
    ingestion_mode         NVARCHAR(30)    NOT NULL,
    source_hash            BINARY(32)      NOT NULL,
    _batch_id              UNIQUEIDENTIFIER NOT NULL,
    _ingested_at           DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    _is_deleted            BIT             NOT NULL DEFAULT 0,
    _dq_status             NVARCHAR(20)    NOT NULL DEFAULT 'OK',

    -- SAP columnas
    DocEntry               INT             NOT NULL,
    LineNum                INT             NOT NULL,
    ItemCode               NVARCHAR(50)    NULL,
    Dscription             NVARCHAR(200)   NULL,
    Quantity               DECIMAL(19,6)   NULL,
    Price                  DECIMAL(19,6)   NULL,
    PriceAfVAT             DECIMAL(19,6)   NULL,
    Currency               NVARCHAR(10)    NULL,
    Rate                   DECIMAL(19,6)   NULL,
    DiscPrcnt              DECIMAL(9,4)    NULL,
    LineTotal              DECIMAL(19,6)   NULL,
    TotalFrgn              DECIMAL(19,6)   NULL,
    WhsCode                NVARCHAR(20)    NULL,
    VatGroup               NVARCHAR(20)    NULL,
    VatPrcnt               DECIMAL(9,4)    NULL,
    SlpCode                INT             NULL,
    Project                NVARCHAR(50)    NULL,
    OcrCode                NVARCHAR(50)    NULL,
    BaseEntry              INT             NULL,
    BaseRef                NVARCHAR(50)    NULL,
    BaseType               INT             NULL,
    BaseLine               INT             NULL,
    GrossBuyPr             DECIMAL(19,6)   NULL,

    _rowver                ROWVERSION      NOT NULL,

    CONSTRAINT pk_raw_sap_inv1
        PRIMARY KEY CLUSTERED (tenant_id, company_id, DocEntry, LineNum),
    CONSTRAINT fk_raw_sap_inv1_run
        FOREIGN KEY (extraction_run_id) REFERENCES ctl.extraction_run(run_id)
);

CREATE INDEX ix_raw_sap_inv1_item
    ON raw.sap_inv1 (tenant_id, company_id, ItemCode);
```

### `dim.customer`

```sql
CREATE TABLE dim.customer (
    customer_sk            BIGINT IDENTITY(1,1)
                           CONSTRAINT pk_dim_customer PRIMARY KEY,
    tenant_id              INT             NOT NULL,
    company_id             INT             NOT NULL,
    company_slug           NVARCHAR(50)    NOT NULL,
    customer_bk            NVARCHAR(50)    NOT NULL,    -- = CardCode

    -- Atributos
    customer_name          NVARCHAR(200)   NULL,
    customer_full_name     NVARCHAR(200)   NULL,
    group_code             INT             NULL,
    group_name             NVARCHAR(100)   NULL,
    tax_id                 NVARCHAR(50)    NULL,
    currency               NVARCHAR(10)    NULL,
    salesperson_bk         INT             NULL,
    territory              NVARCHAR(100)   NULL,

    -- Flags derivados
    is_active              BIT             NOT NULL DEFAULT 1,
    is_blocked             BIT             NOT NULL DEFAULT 0,

    -- SCD2
    valid_from             DATE            NOT NULL,
    valid_to               DATE            NULL,
    is_current             BIT             NOT NULL DEFAULT 1,

    _ingested_at           DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    _dq_status             NVARCHAR(20)    NOT NULL DEFAULT 'OK'
);

CREATE UNIQUE INDEX uq_dim_customer_bk_current
    ON dim.customer (tenant_id, company_id, customer_bk)
    WHERE is_current = 1;

CREATE INDEX ix_dim_customer_company_active
    ON dim.customer (tenant_id, company_id, is_active);
```

### `fact.sales`

```sql
CREATE TABLE fact.sales (
    sales_sk               BIGINT IDENTITY(1,1)
                           CONSTRAINT pk_fact_sales PRIMARY KEY,
    tenant_id              INT             NOT NULL,
    company_sk             BIGINT          NOT NULL
                           CONSTRAINT fk_fact_sales_company
                           REFERENCES dim.company(company_sk),
    customer_sk            BIGINT          NOT NULL
                           CONSTRAINT fk_fact_sales_customer
                           REFERENCES dim.customer(customer_sk),
    item_sk                BIGINT          NOT NULL
                           CONSTRAINT fk_fact_sales_item
                           REFERENCES dim.item(item_sk),
    salesperson_sk         BIGINT          NULL
                           CONSTRAINT fk_fact_sales_salesperson
                           REFERENCES dim.salesperson(salesperson_sk),
    date_sk                INT             NOT NULL
                           CONSTRAINT fk_fact_sales_date
                           REFERENCES dim.date(date_sk),

    -- Identidad fuente (degenerate dimensions)
    doc_entry              INT             NOT NULL,
    doc_num                INT             NULL,
    line_num               INT             NOT NULL,

    -- Métricas
    quantity               DECIMAL(19,6)   NOT NULL,
    unit_price             DECIMAL(19,6)   NOT NULL,
    line_total_lc          DECIMAL(19,6)   NOT NULL,    -- moneda local
    line_total_fc          DECIMAL(19,6)   NULL,        -- moneda extranjera si aplica
    currency_doc           NVARCHAR(10)    NULL,
    discount_pct           DECIMAL(9,4)    NULL,
    vat_amount_lc          DECIMAL(19,6)   NULL,

    -- Flags
    is_canceled            BIT             NOT NULL DEFAULT 0,
    is_closed              BIT             NOT NULL DEFAULT 0,
    doc_status             NCHAR(1)        NULL,

    -- Trazabilidad
    extraction_run_id      BIGINT          NOT NULL,
    source_update_date     DATE            NULL,
    source_update_ts_norm  CHAR(6)         NULL,
    _ingested_at           DATETIME2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    _dq_status             NVARCHAR(20)    NOT NULL DEFAULT 'OK',

    CONSTRAINT uq_fact_sales_natural
        UNIQUE (tenant_id, company_sk, doc_entry, line_num)
);

CREATE INDEX ix_fact_sales_company_date
    ON fact.sales (tenant_id, company_sk, date_sk DESC)
    INCLUDE (line_total_lc, is_canceled);

CREATE INDEX ix_fact_sales_customer_date
    ON fact.sales (tenant_id, company_sk, customer_sk, date_sk DESC)
    INCLUDE (line_total_lc);

-- Para volúmenes grandes: columnstore
-- CREATE CLUSTERED COLUMNSTORE INDEX ccx_fact_sales ON fact.sales;
```

---

## Apéndice B — MERGE / UPSERT

### MERGE para `raw.sap_oinv`

```sql
CREATE OR ALTER PROCEDURE raw.sp_upsert_sap_oinv
    @batch_id           UNIQUEIDENTIFIER,
    @extraction_run_id  BIGINT,
    @ingestion_mode     NVARCHAR(30),
    @rows               raw.OinvRowsType READONLY   -- table-valued parameter
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @inserted INT = 0, @updated INT = 0, @hash_skipped INT = 0;

    BEGIN TRY
        BEGIN TRAN;

        -- Pre-filtrar filas con hash idéntico a target (skip silencioso)
        DECLARE @to_merge raw.OinvRowsType;
        INSERT INTO @to_merge
        SELECT src.*
        FROM @rows src
        LEFT JOIN raw.sap_oinv tgt
               ON tgt.tenant_id  = src.tenant_id
              AND tgt.company_id = src.company_id
              AND tgt.DocEntry   = src.DocEntry
        WHERE tgt.source_hash IS NULL
           OR tgt.source_hash <> src.source_hash;

        SET @hash_skipped = (SELECT COUNT(*) FROM @rows) - (SELECT COUNT(*) FROM @to_merge);

        MERGE raw.sap_oinv AS tgt
        USING @to_merge   AS src
           ON tgt.tenant_id  = src.tenant_id
          AND tgt.company_id = src.company_id
          AND tgt.DocEntry   = src.DocEntry
        WHEN MATCHED AND (
                src.source_update_date >  tgt.source_update_date
             OR (src.source_update_date = tgt.source_update_date
                 AND src.source_update_ts_norm > tgt.source_update_ts_norm)
             OR (src.source_update_date = tgt.source_update_date
                 AND src.source_update_ts_norm = tgt.source_update_ts_norm
                 AND src.source_hash <> tgt.source_hash)
        ) THEN UPDATE SET
            tgt.DocNum               = src.DocNum,
            tgt.CardCode             = src.CardCode,
            tgt.CardName             = src.CardName,
            tgt.DocDate              = src.DocDate,
            tgt.DocDueDate           = src.DocDueDate,
            tgt.TaxDate              = src.TaxDate,
            tgt.DocTotal             = src.DocTotal,
            tgt.DocTotalFc           = src.DocTotalFc,
            tgt.DocCur               = src.DocCur,
            tgt.DocStatus            = src.DocStatus,
            tgt.Canceled             = src.Canceled,
            tgt.CancelDate           = src.CancelDate,
            tgt.SlpCode              = src.SlpCode,
            tgt.ObjType              = src.ObjType,
            tgt.GroupNum             = src.GroupNum,
            tgt.VatSum               = src.VatSum,
            tgt.DiscPrcnt            = src.DiscPrcnt,
            tgt.DiscSum              = src.DiscSum,
            tgt.PaidToDate           = src.PaidToDate,
            tgt.Comments             = src.Comments,
            tgt.source_create_date   = src.source_create_date,
            tgt.source_create_ts     = src.source_create_ts,
            tgt.source_create_ts_norm = src.source_create_ts_norm,
            tgt.source_update_date   = src.source_update_date,
            tgt.source_update_ts     = src.source_update_ts,
            tgt.source_update_ts_norm = src.source_update_ts_norm,
            tgt.source_hash          = src.source_hash,
            tgt.extracted_at_utc     = src.extracted_at_utc,
            tgt.extraction_run_id    = @extraction_run_id,
            tgt.ingestion_mode       = @ingestion_mode,
            tgt._batch_id            = @batch_id,
            tgt._ingested_at         = SYSUTCDATETIME()
        WHEN NOT MATCHED BY TARGET THEN
            INSERT (tenant_id, company_id, company_slug, source_database, source_system,
                    source_object, extracted_at_utc, extraction_run_id, ingestion_mode,
                    source_hash, _batch_id,
                    DocEntry, DocNum, CardCode, CardName, DocDate, DocDueDate, TaxDate,
                    DocTotal, DocTotalFc, DocCur, DocStatus, Canceled, CancelDate,
                    SlpCode, ObjType, GroupNum, VatSum, DiscPrcnt, DiscSum,
                    PaidToDate, Comments,
                    source_create_date, source_create_ts, source_create_ts_norm,
                    source_update_date, source_update_ts, source_update_ts_norm)
            VALUES (src.tenant_id, src.company_id, src.company_slug, src.source_database,
                    src.source_system, 'OINV', src.extracted_at_utc, @extraction_run_id,
                    @ingestion_mode, src.source_hash, @batch_id,
                    src.DocEntry, src.DocNum, src.CardCode, src.CardName, src.DocDate,
                    src.DocDueDate, src.TaxDate, src.DocTotal, src.DocTotalFc, src.DocCur,
                    src.DocStatus, src.Canceled, src.CancelDate, src.SlpCode, src.ObjType,
                    src.GroupNum, src.VatSum, src.DiscPrcnt, src.DiscSum, src.PaidToDate,
                    src.Comments,
                    src.source_create_date, src.source_create_ts, src.source_create_ts_norm,
                    src.source_update_date, src.source_update_ts, src.source_update_ts_norm)
        OUTPUT $action INTO @action_log(action);

        -- Conteo desde @action_log
        SELECT @inserted = SUM(CASE WHEN action = 'INSERT' THEN 1 ELSE 0 END),
               @updated  = SUM(CASE WHEN action = 'UPDATE' THEN 1 ELSE 0 END)
        FROM @action_log;

        -- Auditoría
        INSERT INTO audit.ingestion_event
            (tenant_id, company_id, batch_id, source_object, ingestion_mode,
             rows_received, rows_inserted, rows_updated, rows_skipped,
             source_hash_collisions, occurred_at, status)
        VALUES
            (@first_tenant_id, @first_company_id, @batch_id, 'OINV', @ingestion_mode,
             (SELECT COUNT(*) FROM @rows), @inserted, @updated, 0,
             @hash_skipped, SYSUTCDATETIME(), 'SUCCESS');

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        INSERT INTO ctl.extraction_error (run_id, source_object, error_code, error_message, occurred_at)
        VALUES (@extraction_run_id, 'OINV', 'MERGE_FAILED', ERROR_MESSAGE(), SYSUTCDATETIME());
        THROW;
    END CATCH
END
```

### MERGE para `raw.sap_ocrd`

Mismo patrón con natural key = `CardCode`:

```sql
MERGE raw.sap_ocrd AS tgt
USING @rows AS src
   ON tgt.tenant_id  = src.tenant_id
  AND tgt.company_id = src.company_id
  AND tgt.CardCode   = src.CardCode
WHEN MATCHED AND (
     src.source_update_date >  tgt.source_update_date
  OR (src.source_update_date = tgt.source_update_date
      AND src.source_update_ts_norm > tgt.source_update_ts_norm)
  OR (src.source_update_date = tgt.source_update_date
      AND src.source_update_ts_norm = tgt.source_update_ts_norm
      AND src.source_hash <> tgt.source_hash)
   ) THEN UPDATE SET
        tgt.CardName       = src.CardName,
        tgt.CardFName      = src.CardFName,
        tgt.CardType       = src.CardType,
        tgt.GroupCode      = src.GroupCode,
        tgt.LicTradNum     = src.LicTradNum,
        tgt.Address        = src.Address,
        tgt.Phone1         = src.Phone1,
        tgt.E_Mail         = src.E_Mail,
        tgt.Balance        = src.Balance,
        tgt.frozenFor      = src.frozenFor,
        tgt.validFor       = src.validFor,
        tgt.Currency       = src.Currency,
        tgt.SlpCode        = src.SlpCode,
        tgt.Territory      = src.Territory,
        tgt.source_create_date   = src.source_create_date,
        tgt.source_create_ts_norm = src.source_create_ts_norm,
        tgt.source_update_date   = src.source_update_date,
        tgt.source_update_ts_norm = src.source_update_ts_norm,
        tgt.source_hash          = src.source_hash,
        tgt._ingested_at         = SYSUTCDATETIME(),
        tgt._batch_id            = @batch_id,
        tgt.extraction_run_id    = @extraction_run_id
WHEN NOT MATCHED BY TARGET THEN
    INSERT (...) VALUES (...);
```

---

## Apéndice C — Delete + Insert para `raw.sap_inv1` por DocEntry

```sql
CREATE OR ALTER PROCEDURE raw.sp_upsert_sap_inv1
    @batch_id           UNIQUEIDENTIFIER,
    @extraction_run_id  BIGINT,
    @ingestion_mode     NVARCHAR(30),
    @rows               raw.Inv1RowsType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        -- 1. Obtener DocEntries afectados (distintos)
        DECLARE @doc_entries TABLE (
            tenant_id  INT,
            company_id INT,
            DocEntry   INT,
            PRIMARY KEY (tenant_id, company_id, DocEntry)
        );

        INSERT INTO @doc_entries (tenant_id, company_id, DocEntry)
        SELECT DISTINCT tenant_id, company_id, DocEntry FROM @rows;

        -- 2. DELETE de todas las líneas de esos DocEntries
        DELETE l
        FROM   raw.sap_inv1 l
        INNER JOIN @doc_entries d
             ON l.tenant_id  = d.tenant_id
            AND l.company_id = d.company_id
            AND l.DocEntry   = d.DocEntry;

        DECLARE @deleted INT = @@ROWCOUNT;

        -- 3. INSERT de las nuevas líneas
        INSERT INTO raw.sap_inv1
            (tenant_id, company_id, company_slug, source_database, source_system,
             source_object, extracted_at_utc, extraction_run_id, ingestion_mode,
             source_hash, _batch_id,
             DocEntry, LineNum, ItemCode, Dscription, Quantity, Price, PriceAfVAT,
             Currency, Rate, DiscPrcnt, LineTotal, TotalFrgn, WhsCode,
             VatGroup, VatPrcnt, SlpCode, Project, OcrCode,
             BaseEntry, BaseRef, BaseType, BaseLine, GrossBuyPr)
        SELECT
             r.tenant_id, r.company_id, r.company_slug, r.source_database, r.source_system,
             'INV1', r.extracted_at_utc, @extraction_run_id, @ingestion_mode,
             r.source_hash, @batch_id,
             r.DocEntry, r.LineNum, r.ItemCode, r.Dscription, r.Quantity, r.Price,
             r.PriceAfVAT, r.Currency, r.Rate, r.DiscPrcnt, r.LineTotal, r.TotalFrgn,
             r.WhsCode, r.VatGroup, r.VatPrcnt, r.SlpCode, r.Project, r.OcrCode,
             r.BaseEntry, r.BaseRef, r.BaseType, r.BaseLine, r.GrossBuyPr
        FROM @rows r;

        DECLARE @inserted INT = @@ROWCOUNT;

        -- 4. Audit
        INSERT INTO audit.ingestion_event
            (tenant_id, company_id, batch_id, source_object, ingestion_mode,
             rows_received, rows_inserted, rows_updated, rows_skipped,
             occurred_at, status)
        VALUES
            ((SELECT TOP 1 tenant_id FROM @rows),
             (SELECT TOP 1 company_id FROM @rows),
             @batch_id, 'INV1', @ingestion_mode,
             @inserted, @inserted, 0, 0,
             SYSUTCDATETIME(), 'SUCCESS');

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        INSERT INTO ctl.extraction_error (run_id, source_object, error_code, error_message, occurred_at)
        VALUES (@extraction_run_id, 'INV1', 'UPSERT_FAILED', ERROR_MESSAGE(), SYSUTCDATETIME());
        THROW;
    END CATCH
END
```

Mismo patrón para `raw.sap_rin1`.

---

## Apéndice D — Estrategia unificada: ambas modalidades sin duplicar lógica de BI

### El principio

**Una sola pipeline raw → stg → dim/fact. Cero conocimiento de modalidad downstream.**

```
┌──── MODALIDAD A ────┐                ┌──── MODALIDAD B ────┐
│ Dedicated Extractor │                │ Cloud Connector     │
│ (Worker Service en  │                │ (Azure Function)    │
│  infra del cliente) │                │                     │
└──────────┬──────────┘                └──────────┬──────────┘
           │                                       │
           │  Mismo DTO de batch                   │  Mismo DTO de batch
           │  ingestion_mode = DEDICATED_HANA      │  ingestion_mode = SERVICE_LAYER_QUEUE
           │  o CSV_INITIAL_LOAD                   │  o CSV_INITIAL_LOAD
           │                                       │
           └───────────────┬───────────────────────┘
                           ▼
              ┌────────────────────────────┐
              │  DataBision Ingest API     │
              │  POST /api/ingest/{...}    │
              │  Single endpoint           │
              └─────────────┬──────────────┘
                            ▼
              ┌────────────────────────────┐
              │  Stored procs upsert raw   │
              │  sp_upsert_sap_oinv etc.   │
              │  → No conocen modalidad    │
              │  → Solo consumen el DTO    │
              └─────────────┬──────────────┘
                            ▼
              ┌────────────────────────────┐
              │  raw.sap_oinv etc.         │
              │  Schema único              │
              │  ingestion_mode = columna  │
              └─────────────┬──────────────┘
                            ▼
              ┌────────────────────────────┐
              │  Refresh jobs raw → stg    │
              │  Refresh jobs stg → fact   │
              │  → NO referencian          │
              │    ingestion_mode          │
              └─────────────┬──────────────┘
                            ▼
              ┌────────────────────────────┐
              │  stg.* / dim.* / fact.*    │
              │  Idénticos para A y B      │
              └────────────────────────────┘
```

### DTO de batch — único para ambos modos

```json
{
  "tenant_id": 42,
  "tenant_slug": "acme",
  "company_id": 1,
  "company_slug": "acme-ar",
  "source_database": "SBO_ACME",
  "source_system": "SAP_B1_HANA",
  "source_object": "OINV",
  "ingestion_mode": "DEDICATED_HANA",
  "extraction_run_id": 12345,
  "batch_id": "5f1b3c2e-1234-...",
  "extracted_at_utc": "2026-05-17T14:30:00Z",
  "rows": [
    {
      "DocEntry": 9876,
      "DocNum": 12345,
      "CardCode": "C00001",
      "DocDate": "2026-05-17",
      "DocTotal": 12450.50,
      "Canceled": "N",
      "DocStatus": "O",
      "source_create_date": "2025-08-12",
      "source_create_ts": 94530,
      "source_create_ts_norm": "094530",
      "source_update_date": "2026-05-17",
      "source_update_ts": 152000,
      "source_update_ts_norm": "152000",
      "source_hash": "base64_sha256_of_business_cols"
    }
  ]
}
```

Modalidad B produce **exactamente la misma forma** con `ingestion_mode = "SERVICE_LAYER_QUEUE"`. El Ingest API no distingue.

### Beneficios

1. **Cero duplicación de SQL.** Una sola pipeline staging/fact, no dos.
2. **Cero duplicación de tests.** Tests de DQ corren igual para ambos.
3. **Cross-tenant migrations triviales.** Si un cliente cambia de Modalidad A a B, solo cambia ingestion_mode; raw queda intacto.
4. **Auditoría unificada.** `audit.ingestion_event` tiene la verdad de qué entró por dónde.
5. **El portal y Power BI ven la misma estructura sin importar el cliente.**

### Reglas que mantienen la separación

- **Agente / Connector** son responsables únicamente de extracción → DTO.
- **Ingest API** es responsable únicamente de DTO → raw.
- **Refresh jobs** son responsables únicamente de raw → stg → dim/fact.
- Cada capa NO conoce la capa anterior excepto por su contrato (DTO o schema SQL).

### Diferencias permitidas entre modalidades

- **Lookback strategy:** Modalidad A usa lookback propio en HANA queries; Modalidad B usa reconciliación SAP-side por tiered windows. **Ambas terminan generando el mismo tipo de batch** al Ingest API.
- **Carga inicial:** A usa drenaje HANA bulk; B usa drenaje SL o CSV bulk; **CSV bulk también es un DTO normal**, solo con `ingestion_mode = CSV_INITIAL_LOAD`.
- **Checkpoint storage:** A tiene copia local en SQLite (más Azure SQL); B solo Azure SQL. Pero el formato del checkpoint en Azure SQL es idéntico.

### Lo único que NO se comparte

- **Telemetría operacional del agente / connector:** logs locales (A) y Application Insights (B) son específicos de su entorno.
- **Health checks:** A expone `:8081/health` en cliente; B expone via Azure Function dashboard. Distintos endpoints, mismos KPIs.

---

## Glosario

- **raw:** capa de espejo SAP sin transformaciones.
- **stg:** staging — tipado, limpio, denormalizado.
- **dim/fact:** modelo dimensional para BI.
- **ctl:** control de pipeline.
- **audit:** trazabilidad de eventos.
- **source_hash:** SHA-256 de columnas de negocio canonicalizadas, para dedupe y skip de updates sin cambios.
- **Watermark compuesto:** triple `(update_date, update_ts_norm, natural_key)` para incremental correcto cuando `UpdateDate` es DATE de día.
- **TS normalizado:** `LPAD(CAST(UpdateTS AS VARCHAR), 6, '0')` — string de 6 caracteres comparable lexicográficamente.
- **SCD2:** Slowly Changing Dimension tipo 2 — versiona cambios manteniendo historia.
- **Ingestion mode:** etiqueta de la modalidad que produjo una fila (`DEDICATED_HANA`, `SERVICE_LAYER_QUEUE`, etc.).
- **Batch:** unidad atómica de envío entre agente/connector y Ingest API.
- **Lookback:** ventana hacia atrás aplicada en cada corrida para capturar cambios retroactivos.
- **RLS:** Row-Level Security — filtrado por fila según contexto del usuario.
