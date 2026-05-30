# Service Layer Delta — Diseño Técnico

**Versión:** 1.1  
**Fecha:** 2026-05-30  
**Estado:** ✅ Canónico — Mode B oficial de DataBision

> **Documento canónico para Mode B (Service Layer Delta).** Ver [ADR-010](adr/ADR-010-mode-b-canonical-design.md).
>
> **Naming canónico de UDTs:** Este documento ya usa los nombres correctos:
> - `@DBS_QUEUE` — cola de ítems pendientes (un ítem por documento activo)
> - `@DBS_CHANGES` — log de eventos recibidos (un registro por notificación)
>
> **Mecanismo de población:** El mecanismo primario es **UDT Queue + mecanismo de disparo disponible en el cliente** (PostTransactionNotice, TransactionNotification, FMS, SP programado). PTN es el mecanismo preferido cuando está disponible, pero NO es una dependencia obligatoria. Ver §2 actualizado.
>
> **Destino:** Supabase PostgreSQL vía DataBision Ingest API. No Azure SQL. Ver [ADR-001](adr/ADR-001-database-engine.md).
>
> **Worker:** BackgroundService .NET (`SLDeltaWorker`). No Azure Function. Ver [ADR-007](adr/ADR-007-background-workers.md).

---

## 1. Contexto y Problema

La arquitectura de extracción actual (DataBision Extractor / Windows Service) requiere instalación en un servidor con acceso directo a la base de datos HANA o SQL Server de SAP B1. Los clientes que operan **SAP B1 Cloud** (hosting SAP) no pueden instalar software adicional en la infraestructura de SAP.

El enfoque full-extract vía Service Layer tampoco es viable como solución primaria: consultar tablas completas con `$filter=UpdateDate ge {watermark}` supone:

- Carga innecesaria sobre el Service Layer del cliente.
- Rate limits y throttling en tenants con volumen alto.
- Ventanas de extracción de horas para objetos maestros (OCRD, OITM) con decenas de miles de registros.
- Riesgo de datos incompletos si el extractor no termina antes del siguiente ciclo.

---

## 2. Solución: Service Layer Delta con UDT Queue en SAP B1

La estrategia delta de DataBision funciona en dos etapas independientes:

1. **SAP B1 registra cambios** en la UDT `@DBS_CHANGES` / `@DBS_QUEUE` usando el mecanismo disponible según el ambiente del cliente.
2. **DataBision consulta `@DBS_QUEUE`** vía Service Layer y procesa solo los cambios pendientes.

```
SAP B1 confirma transacción
        │
        │  Mecanismo de disparo (ver §2.1)
        ▼
@DBS_CHANGES (UDT en B1)  ←── registro de cada evento
        │ (deduplicado en @DBS_QUEUE: 1 entrada por documento activo)
        ▼
@DBS_QUEUE (UDT en B1)    ←── cola de procesamiento
        │
        │ DataBision SLDeltaWorker hace polling cada 5 seg por tenant activo
        ▼
SLDeltaWorker (BackgroundService .NET)
        │ Adquiere lock → Fetch objeto completo vía Service Layer
        │
        ├── POST → Ingest API → raw.sap_* (Supabase PostgreSQL)
        ├── Actualiza @DBS_QUEUE.U_Status = DONE
        └── Actualiza @DBS_CHANGES.U_ProcStat = DONE
```

El pipeline de staging existente (`IngestService`, `SapRawRepository`, `ctl.ingest_checkpoint`) se mantiene sin cambios. El delta introduce una nueva **fuente de despacho** que reemplaza al extractor polling para clientes cloud.

### 2.1 Mecanismos de Población de la Cola (por prioridad)

El mecanismo a usar depende de lo que el cliente y su partner SAP permitan configurar. Se usa el primero disponible:

| Prioridad | Mecanismo | Disponibilidad | Notas |
|---|---|---|---|
| 1 | **Post Transaction Notification (PTN)** | SAP B1 ≥ 9.3, SL ≥ 1.3 | Webhook a DataBision. Más rápido, menos intrusivo en SAP. Ver §6. |
| 2 | **TransactionNotification (SP)** | SAP B1 todos (HANA/SQL) | SP mínimo en B1: INSERT + RETURN 0. Requiere acceso a HANA/SQL del cliente. |
| 3 | **FMS (Formatted Search)** | SAP B1 todos | Se dispara desde campos clave al guardar desde UI. No cubre operaciones masivas. |
| 4 | **SP de reconciliación programado** | SAP B1 con SQL Job / DB Scheduler | Barrido periódico de UpdateDate. Fallback universal. Más latencia. |

> **Importante:** PTN es el mecanismo preferido cuando está disponible, pero **no es una dependencia obligatoria**. El sistema funciona correctamente con cualquiera de los mecanismos anteriores. La reconciliación periódica (§10.4) cubre los gaps de cualquier mecanismo.

### 2.2 Flujo Principal (independiente del mecanismo de disparo)

Independientemente de cómo se pobló `@DBS_QUEUE`, el `SLDeltaWorker` siempre sigue el mismo ciclo descrito en §7.

---

## 3. UDT: @DBS_CHANGES

### 3.1 Propósito

Log inmutable de cada evento PTN recibido. Un registro por notificación. Actúa como buffer durable: si DataBision tiene downtime, los cambios se acumulan en B1 y se procesan cuando la API se recupera.

### 3.2 Creación

```
TableName:   @DBS_CHANGES
Description: DataBision Change Log
TableType:   bott_NoObject (tabla sin objeto B1 ligado)
Archivable:  No
```

### 3.3 Campos

| Campo | Tipo B1 | Longitud | Valores / Descripción |
|---|---|---|---|
| `Code` | Alpha | 8 | Auto-generado por B1 (secuencia) |
| `Name` | Alpha | 100 | Auto (igual a Code) |
| `U_ObjType` | Alpha | 20 | Código de objeto SAP: `"17"`, `"13"`, `"2"`, etc. |
| `U_DocEntry` | Numeric | — | DocEntry del documento afectado |
| `U_DocNum` | Numeric | — | DocNum (legible por humanos) |
| `U_CmpDB` | Alpha | 20 | Nombre de la base de datos B1 (SBODemoUS, etc.) |
| `U_Action` | Alpha | 10 | `ADD` \| `UPDATE` \| `CANCEL` \| `CLOSE` \| `REOPEN` |
| `U_PrevStat` | Alpha | 1 | Estado previo del documento: `O`, `C`, `L`, `Y`, ` ` |
| `U_NewStat` | Alpha | 1 | Estado nuevo: `O`, `C`, `L`, `Y` |
| `U_UserCode` | Alpha | 20 | Usuario B1 que originó la transacción |
| `U_TxRef` | Alpha | 50 | Referencia única de la transacción PTN (dedup primario) |
| `U_RcvAt` | DateTime | — | Timestamp de recepción en DataBision (UTC) |
| `U_ProcStat` | Alpha | 15 | `PENDING` \| `PROCESSING` \| `DONE` \| `FAILED` \| `SKIPPED` \| `DLQ` |
| `U_Retries` | Numeric | — | Número de intentos de procesamiento |
| `U_ErrMsg` | Memo | — | Detalle del error en el último intento fallido |
| `U_ProcAt` | DateTime | — | Timestamp de procesamiento exitoso (UTC) |
| `U_Worker` | Alpha | 50 | Identificador del nodo DataBision que procesó el registro |
| `U_QRef` | Alpha | 20 | Code del registro correspondiente en @DBS_QUEUE |

### 3.4 Índices Recomendados (B1 User Queries / DI API)

| Índice | Campos | Propósito |
|---|---|---|
| IDX01 | `U_ProcStat`, `U_RcvAt` | Polling de pendientes (WHERE U_ProcStat = 'PENDING' ORDER BY U_RcvAt) |
| IDX02 | `U_ObjType`, `U_DocEntry`, `U_CmpDB` | Búsqueda de duplicados por clave natural |
| IDX03 | `U_TxRef` | Deduplicación por referencia de transacción |
| IDX04 | `U_QRef` | Join con @DBS_QUEUE |

### 3.5 Retención

Los registros con `U_ProcStat = 'DONE'` se pueden archivar después de 90 días. Los registros `DLQ` o `FAILED` se retienen indefinidamente hasta resolución manual.

---

## 4. UDT: @DBS_QUEUE

### 4.1 Propósito

Cola de trabajo deduplicada. Mientras `@DBS_CHANGES` tiene un registro por cada evento PTN, `@DBS_QUEUE` tiene **un registro por documento activo en la cola** — independientemente de cuántas notificaciones PTN hayan llegado para ese documento durante la ventana de procesamiento.

Si un documento recibe 5 PTN en 10 segundos (usuario guarda múltiples veces), hay 5 entradas en `@DBS_CHANGES` pero solo **1** en `@DBS_QUEUE`. El Processor hace una única llamada al Service Layer.

### 4.2 Creación

```
TableName:   @DBS_QUEUE
Description: DataBision Processing Queue
TableType:   bott_NoObject
Archivable:  No
```

### 4.3 Campos

| Campo | Tipo B1 | Longitud | Valores / Descripción |
|---|---|---|---|
| `Code` | Alpha | 8 | Auto-generado por B1 |
| `Name` | Alpha | 100 | Auto |
| `U_ObjType` | Alpha | 20 | Código de objeto SAP |
| `U_DocEntry` | Numeric | — | DocEntry del documento |
| `U_CmpDB` | Alpha | 20 | Base de datos B1 |
| `U_TenantId` | Alpha | 50 | GUID del tenant en DataBision |
| `U_Priority` | Numeric | — | `1`=Crítico, `2`=Alto, `3`=Normal, `5`=Bajo |
| `U_Status` | Alpha | 15 | `PENDING` \| `RUNNING` \| `DONE` \| `ERROR` \| `BLOCKED` |
| `U_NChanges` | Numeric | — | Cantidad de @DBS_CHANGES que alimentaron esta entrada |
| `U_LastAction` | Alpha | 10 | Última acción recibida (determina operación a ejecutar) |
| `U_EnqAt` | DateTime | — | Timestamp de primera entrada en cola |
| `U_StartAt` | DateTime | — | Timestamp de inicio de procesamiento |
| `U_DoneAt` | DateTime | — | Timestamp de completado exitoso |
| `U_Attempts` | Numeric | — | Intentos totales realizados |
| `U_MaxAttempts` | Numeric | — | Máximo de intentos (default: `5`) |
| `U_NextRetry` | DateTime | — | Próximo intento permitido (backoff exponencial) |
| `U_Worker` | Alpha | 50 | Nodo DataBision que tiene el lock |
| `U_StagingKey` | Alpha | 100 | Clave del registro en raw.sap_* tras procesamiento |

### 4.4 Índices Recomendados

| Índice | Campos | Propósito |
|---|---|---|
| IDX01 | `U_Status`, `U_Priority`, `U_NextRetry` | Dequeue ordenado (workers) |
| IDX02 | `U_ObjType`, `U_DocEntry`, `U_CmpDB`, `U_Status` | Dedup al encolar nuevo cambio |
| IDX03 | `U_Worker`, `U_Status` | Detección de workers muertos / heartbeat |
| IDX04 | `U_TenantId`, `U_Status` | Monitoreo por tenant |

### 4.5 Relación @DBS_CHANGES → @DBS_QUEUE

```
@DBS_CHANGES (N)  ──────────────── (1) @DBS_QUEUE
    U_QRef = @DBS_QUEUE.Code                │
                                     (ObjType, DocEntry, CmpDB, Status IN (PENDING, RUNNING))
                                      → única entrada activa por documento
```

---

## 5. ObjectTypes Soportados

### 5.1 Prioridad 1 — Comerciales y Financieros Críticos

| ObjType | Tabla B1 | Descripción | Tiene líneas |
|---|---|---|---|
| `13` | OINV | Factura de venta (AR Invoice) | Sí → INV1 |
| `15` | ODLN | Entrega / Remito (Delivery) | Sí → DLN1 |
| `14` | ORIN | Nota de crédito venta (AR Credit Memo) | Sí → RIN1 |
| `17` | ORDR | Pedido de venta (Sales Order) | Sí → RDR1 |
| `163` | ODPI | Anticipo de venta (AR Down Payment) | Sí → DPI1 |
| `2` | OCRD | Socios de negocio (Business Partners) | No |
| `4` | OITM | Artículos / Maestro de productos (Items) | No |

### 5.2 Prioridad 2 — Compras y Tesorería

| ObjType | Tabla B1 | Descripción | Tiene líneas |
|---|---|---|---|
| `22` | OPOR | Orden de compra (Purchase Order) | Sí → POR1 |
| `18` | OPCH | Factura de compra (AP Invoice) | Sí → PCH1 |
| `20` | OPDN | Recepción de mercadería (Goods Receipt PO) | Sí → PDN1 |
| `19` | ORPC | Nota de crédito compra (AP Credit Memo) | Sí → RPC1 |
| `162` | OPPI | Anticipo de compra (AP Down Payment) | Sí → PPI1 |
| `24` | ORCT | Cobros (Incoming Payments) | No |
| `46` | OVPM | Pagos (Outgoing Payments) | No |
| `30` | OJDT | Asientos contables (Journal Entries) | Sí → JDT1 |

### 5.3 Prioridad 3 — Inventario y Servicio (Fase 2)

| ObjType | Tabla B1 | Descripción |
|---|---|---|
| `59` | OIGE | Transferencia entre almacenes |
| `67` | OBST | Extracto bancario |
| `310000001` | OWOR | Orden de producción |
| `1470000113` | OSCN | Contrato de servicio |

### 5.4 No Soportados en v1 (Carga inicial completa solamente)

Objetos de configuración del sistema que no generan movimientos de negocio y cuyos cambios son infrecuentes:

- Tipos de cambio (ORTT)
- Configuración de impuestos (OSTC, OTAX)
- Calendarios y períodos fiscales
- Condiciones de pago (OCTG)
- Almacenes (OWHS)
- Grupos de comisión (OCGR)

Estos objetos se sincronizan mediante una extracción completa programada (weekly full-load), no mediante PTN.

---

## 6. Post Transaction Notification — Registro y Flujo

### 6.1 Registro de Suscripciones PTN

Durante el onboarding del tenant, DataBision registra subscripciones PTN en B1 vía Service Layer para cada ObjectType soportado en Prioridad 1 y 2:

```
Endpoint de destino: https://ingest.databision.app/api/ptn/{tenantSlug}
Método:             HTTP POST
Autenticación:      Header X-DataBision-PtnKey: {ptn_secret}   (HMAC-SHA256 rotado cada 90 días)
Reintentos B1:      Según versión B1 (≥ 10.0 soporta hasta 3 reintentos con backoff)
```

### 6.2 Payload PTN Recibido

SAP B1 Service Layer envía la notificación con el siguiente cuerpo (estructura basada en SAP B1 Service Layer 10.x):

```json
{
  "CompanyDB":    "SBODemoUS",
  "ObjectType":   "17",
  "ObjectCode":   "1045",
  "ObjectAction": "add | update | cancel | close",
  "UserCode":     "manager",
  "SessionId":    "7F3A2B...",
  "TransactionId": "TXN-20260529-00142389"
}
```

> **Nota:** Diferentes versiones de SAP B1 Service Layer pueden variar en los nombres exactos de los campos. El webhook debe manejar variantes de campo (`ObjectCode` / `DocEntry`, `ObjectAction` / `Action`) mediante normalización.

> **Timing importante:** PTN dispara **después** de que B1 confirma la transacción. El documento en B1 ya refleja el nuevo estado cuando llega el webhook. El estado previo (`U_PrevStat`) **nunca** puede leerse desde B1 en ese momento — solo está disponible si el payload PTN lo incluye explícitamente (algunas versiones lo hacen) o se consulta desde `raw.sap_*` del staging de DataBision, que contiene el último estado conocido antes de este cambio.

### 6.3 Derivación de U_Action desde ObjectAction

| ObjectAction (B1) | U_Action (@DBS_CHANGES) | Condición |
|---|---|---|
| `add` | `ADD` | Siempre |
| `update` | `UPDATE` | `U_PrevStat` == `U_NewStat` y ambos ≠ `C`/`Y` |
| `update` | `CLOSE` | `U_NewStat` == `C` y `U_PrevStat` == `O` |
| `update` | `REOPEN` | `U_NewStat` == `O` y `U_PrevStat` == `C` |
| `cancel` | `CANCEL` | `U_NewStat` == `Y` |
| `close` | `CLOSE` | `U_NewStat` == `C` |

> `U_PrevStat` se determina en el momento de recepción del webhook: (1) del payload PTN si B1 lo incluye, o (2) del valor actual de `raw.sap_*."DocStatus"` en staging (estado previo al UPSERT de este cambio). Si el documento no existe aún en staging (primer ADD), `U_PrevStat` queda en blanco.

### 6.4 Flujo del Webhook PTN

```
1. POST /api/ptn/{tenantSlug}
   ├── Valida Header X-DataBision-PtnKey (HMAC-SHA256)
   ├── Resuelve tenant_id por slug
   ├── Valida ObjectType en lista soportada
   └── Rechaza con 400 si ObjectType no soportado (evita ruido en cola)

2. Normaliza payload:
   ├── ObjectCode → DocEntry (numeric)
   ├── ObjectAction → U_Action (ver tabla §6.3)
   └── Lee U_PrevStat desde raw.sap_* si no viene en payload

3. Deduplicación rápida:
   └── ¿Existe registro en @DBS_CHANGES con U_TxRef = TransactionId?
       ├── Sí → responde 200 OK (idempotente, no inserta)
       └── No → continúa

4. Escribe @DBS_CHANGES vía Service Layer:
   └── U_ProcStat = PENDING

5. Actualiza @DBS_QUEUE:
   ├── ¿Existe entrada ACTIVE para (ObjType, DocEntry, CmpDB)?
   │   ├── Sí → U_NChanges++, U_LastAction = nuevo action, U_Priority = max(actual, nuevo)
   │   └── No → INSERT nueva entrada con U_Status = PENDING
   └── U_QRef en @DBS_CHANGES = Code del @DBS_QUEUE

6. Responde HTTP 202 Accepted
   └── Body: {"data": {"changeRef": "@DBS_CHANGES.Code", "queued": true}}
```

---

## 7. Flujo Completo de Procesamiento

### 7.1 Worker Loop

El `DeltaProcessorWorker` (BackgroundService .NET) ejecuta el siguiente ciclo:

```
CADA {ProcessorInterval} ms (default: 5000 ms por tenant activo):

1. DEQUEUE:
   Leer @DBS_QUEUE WHERE:
     U_Status = 'PENDING'
     AND U_TenantId = {tenantId}
     AND U_NextRetry <= NOW()
   ORDER BY U_Priority ASC, U_EnqAt ASC
   LIMIT {BatchSize} (default: 20)

2. LOCK (optimistic):
   Para cada entrada dequeued:
   PATCH @DBS_QUEUE/{Code}
   {
     "U_Status": "RUNNING",
     "U_StartAt": "{now}",
     "U_Worker": "{workerId}",
     "U_Attempts": U_Attempts + 1
   }
   WHERE U_Status = 'PENDING'   ← si ya fue tomada por otro worker, skip

3. FETCH:
   GET /b1s/v1/{ServiceLayerEntity}({DocEntry})
   ├── Incluye líneas si aplica ($expand=DocumentLines)
   └── Maneja 404 → documento eliminado → marcar @DBS_QUEUE DONE + soft-delete en staging

4. UPSERT en staging:
   Llama IngestService.IngestXxxAsync() con batch de 1 item
   ├── Computa source_hash
   ├── Normaliza TSNorm
   └── INSERT ON CONFLICT ... DO UPDATE WHERE hash cambia

5. COMPLETE:
   PATCH @DBS_QUEUE/{Code} { "U_Status": "DONE", "U_DoneAt": "{now}", "U_StagingKey": "{key}" }
   PATCH @DBS_CHANGES/{Code} { "U_ProcStat": "DONE", "U_ProcAt": "{now}", "U_Worker": "{workerId}" }
   Actualiza ctl.ingest_checkpoint (tenant, companyDB, objType) → last_processed_at = now

6. ON ERROR:
   Ver sección §9 (Retries)
```

### 7.2 Entidad Service Layer por ObjectType

| ObjType | Endpoint Service Layer | Expand para líneas |
|---|---|---|
| `13` (OINV) | `/b1s/v1/Invoices({DocEntry})` | `$expand=DocumentLines` |
| `15` (ODLN) | `/b1s/v1/DeliveryNotes({DocEntry})` | `$expand=DocumentLines` |
| `14` (ORIN) | `/b1s/v1/CreditNotes({DocEntry})` | `$expand=DocumentLines` |
| `17` (ORDR) | `/b1s/v1/Orders({DocEntry})` | `$expand=DocumentLines` |
| `163` (ODPI) | `/b1s/v1/DownPayments({DocEntry})` | `$expand=DocumentLines` |
| `2` (OCRD) | `/b1s/v1/BusinessPartners('{CardCode}')` | — |
| `4` (OITM) | `/b1s/v1/Items('{ItemCode}')` | — |
| `22` (OPOR) | `/b1s/v1/PurchaseOrders({DocEntry})` | `$expand=DocumentLines` |
| `18` (OPCH) | `/b1s/v1/PurchaseInvoices({DocEntry})` | `$expand=DocumentLines` |
| `20` (OPDN) | `/b1s/v1/PurchaseDeliveryNotes({DocEntry})` | `$expand=DocumentLines` |
| `30` (OJDT) | `/b1s/v1/JournalEntries({JdtNum})` | `$expand=JournalEntryLines` |
| `24` (ORCT) | `/b1s/v1/IncomingPayments({DocEntry})` | — |
| `46` (OVPM) | `/b1s/v1/VendorPayments({DocEntry})` | — |

> Para OCRD (BusinessPartners) y OITM (Items), la clave primaria es alfanumérica (`CardCode`, `ItemCode`), no `DocEntry`. El payload PTN entrega `ObjectCode` que puede ser la clave natural o el DocEntry según la versión B1. El Processor debe resolver el tipo de clave por ObjectType antes de hacer la llamada Service Layer.

---

## 8. Ciclo de Vida de Documentos

### 8.1 Cancelaciones (U_Action = CANCEL)

Una cancelación en B1 es irreversible. El documento pasa a `DocStatus = 'Y'` (Anulado) y B1 genera automáticamente un contra-asiento contable.

**Comportamiento del Processor:**

1. Fetch del documento con `DocStatus = 'Y'`.
2. UPSERT en staging con `"DocStatus": "Y"`, `"Cancelled": "Y"`.
3. El hash cambia (DocStatus Y ≠ O/C anterior) → siempre genera UPDATE.
4. Downstream Power BI: las medidas deben excluir documentos cancelados (`WHERE "DocStatus" <> 'Y'`).
5. No borrar de staging — el registro cancelado es dato histórico.

**Documentos con líneas canceladas:**
- Las líneas heredan el estado de la cabecera.
- Se hace UPSERT de todas las líneas con el mismo DocEntry y su nuevo estado.

### 8.2 Reaperturas (U_Action = REOPEN)

Aplica principalmente a: Órdenes de compra (OPOR), órdenes de venta (ORDR), y en algunos add-ons. Un documento pasa de `DocStatus = 'C'` (Cerrado) a `DocStatus = 'O'` (Abierto).

**Comportamiento del Processor:**

1. Fetch del documento con `DocStatus = 'O'`.
2. UPSERT en staging → el guard temporal (`UpdateDate + UpdateTSNorm`) garantiza que no se sobreescriba con dato viejo.
3. Downstream Power BI: el documento vuelve a aparecer en reportes de documentos abiertos.
4. Si había líneas con `OpenQty = 0` (cerradas), el UPSERT actualiza sus cantidades abiertas.

**Detección:**
- `U_PrevStat = 'C'` y `U_NewStat = 'O'` en @DBS_CHANGES.
- Si el payload PTN no incluye estado previo: comparar `raw.sap_*."DocStatus"` con el nuevo `DocStatus` del fetch.

### 8.3 Cierres (U_Action = CLOSE)

Un documento se cierra cuando todas sus líneas están completamente entregadas/facturadas, o cuando el usuario lo cierra manualmente. `DocStatus = 'C'`.

**Comportamiento del Processor:**

1. Fetch del documento con `DocStatus = 'C'`.
2. UPSERT en staging → actualiza `"DocStatus": "C"`, `"ClsDate"` si está disponible.
3. Las líneas abiertas pasan a `"LineStatus": "C"` y `"OpenQty": 0`.
4. Downstream Power BI: documento excluido de pendientes, incluido en realizados.

### 8.4 Tabla Resumen de Transiciones

| Transición | U_Action | U_PrevStat | U_NewStat | Prioridad Cola |
|---|---|---|---|---|
| Documento nuevo | ADD | ` ` | `O` | Normal (3) |
| Edición en borrador | UPDATE | `O` | `O` | Normal (3) |
| Cierre automático | CLOSE | `O` | `C` | Alto (2) |
| Cierre manual | CLOSE | `O` | `C` | Alto (2) |
| Cancelación | CANCEL | `O`/`C`/`L` | `Y` | Crítico (1) |
| Reapertura | REOPEN | `C` | `O` | Alto (2) |
| Posting contable | UPDATE | `O` | `L` | Normal (3) |

---

## 9. Retries y Dead Letter Queue

### 9.1 Política de Reintentos

| Intento | Delay antes del reintento | U_Status entre intentos |
|---|---|---|
| 1 | 0 (inmediato) | RUNNING |
| 2 | 30 s | PENDING (U_NextRetry = now + 30s) |
| 3 | 90 s | PENDING (U_NextRetry = now + 90s) |
| 4 | 270 s | PENDING (U_NextRetry = now + 270s) |
| 5 | 810 s | PENDING (U_NextRetry = now + 810s) |
| > 5 | — | BLOCKED → DLQ |

**Fórmula:** `delay(attempt) = 30s × 3^(attempt-2)` para attempt ≥ 2, con jitter ±15%.

**Jitter:** `actual_delay = delay × (1 + random(-0.15, 0.15))`. Evita tormenta de reintentos cuando múltiples documentos fallan simultáneamente (e.g., Service Layer no disponible).

### 9.2 Categorías de Error

| Tipo de error | Comportamiento |
|---|---|
| HTTP 5xx (Service Layer) | Reintento con backoff |
| HTTP 4xx / 404 documento | SKIP (soft-delete en staging si 404) |
| HTTP 401/403 (credenciales expiradas) | Pausa todo el tenant, alerta inmediata, no consumir reintentos |
| Timeout de conexión | Reintento con backoff |
| Error de validación staging | SKIP + log (no reintentar — error de datos) |
| Error de serialización JSON | SKIP + log + alerta (bug en Processor) |

### 9.3 Dead Letter Queue (DLQ)

Cuando `U_Attempts >= U_MaxAttempts`:

1. `@DBS_QUEUE.U_Status` → `BLOCKED`
2. Se escribe en `audit.dlq_events` (DataBision PostgreSQL):
   - `tenant_id`, `company_db`, `obj_type`, `doc_entry`, `last_error`, `blocked_at`
3. Se dispara notificación a DataBision Operations (email + Slack).
4. Disponible para retry manual desde el SuperAdmin panel: botón "Reintentar desde DLQ" llama `PATCH @DBS_QUEUE/{Code} {"U_Status": "PENDING", "U_Attempts": 0, "U_NextRetry": "now"}`.

### 9.4 Circuit Breaker por Tenant

Si el Processor detecta ≥ 5 errores consecutivos de Service Layer para el mismo tenant (independientemente del documento):

1. Pausa el worker de ese tenant por 5 minutos.
2. Escribe `audit.circuit_breaks` con timestamp y motivo.
3. Al reactivarse, reintenta con un solo documento para verificar conectividad antes de procesar el batch completo.

---

## 10. Procesamiento Incremental y Checkpoint

### 10.1 Cursor por Tenant

El checkpoint de la estrategia delta no es un watermark de fecha (eso corresponde al modo polling). Es un cursor sobre `@DBS_CHANGES`:

```
ctl.ingest_checkpoint:
  (tenant_id, company_db, source_object = 'DBS_DELTA')
  → last_change_code: Code del último @DBS_CHANGES procesado con DONE
  → last_processed_at: timestamp del procesamiento
```

En el arranque del Processor, si `last_change_code` existe, se ignorarán registros `@DBS_CHANGES.Code < last_change_code` con `U_ProcStat = DONE` (ya procesados). Solo se procesan los `PENDING` y los `FAILED` con `U_NextRetry <= NOW()`.

### 10.2 Procesamiento de Líneas

Para documentos con líneas (facturas, pedidos, etc.), el Processor **siempre hace UPSERT de todas las líneas del documento** al procesar la cabecera, no solo las líneas modificadas. Razón: SAP B1 no identifica qué líneas cambiaron en el PTN payload — solo indica que el documento cambió.

Este comportamiento es consistente con el patrón UPSERT existente en `SapRawRepository` (INSERT ON CONFLICT con guard temporal).

### 10.3 Ordenamiento Garantizado

Dentro de `@DBS_QUEUE`, el orden de procesamiento es:

1. `U_Priority` ASC (1=Crítico primero).
2. `U_EnqAt` ASC (FIFO dentro del mismo nivel de prioridad).

Si llegan dos notificaciones para el mismo documento antes de que se procese (e.g., un ADD seguido inmediatamente de un UPDATE), `@DBS_QUEUE` consolida en una sola entrada con `U_LastAction = UPDATE`. El Processor hace una sola llamada a Service Layer y obtiene el estado más reciente. No hay riesgo de aplicar el estado ADD después del estado UPDATE.

### 10.4 Modo Híbrido (PTN + Polling Fallback)

El pipeline delta no elimina el checkpoint de polling existente. Ambos modos coexisten:

| Condición | Modo activo |
|---|---|
| PTN disponible, B1 ≥ 9.3 | Delta (PTN) como primario |
| PTN no disponible o deshabilitado | Polling incremental (watermark) |
| Tenant recién onboardeado (sin datos históricos) | Full-load inicial → luego Delta |
| Reconciliación semanal | Polling incremental completo (detección de gaps) |

La **reconciliación semanal** es obligatoria: aunque PTN sea confiable, pueden existir gaps por:
- Downtime de DataBision durante eventos en B1.
- Fallos de PTN en actualizaciones masivas (bulk operations en B1 que no disparan PTN).
- Migraciones de datos en B1.

El job de reconciliación compara `raw.sap_*."UpdateDate" + "UpdateTSNorm"` vs el watermark almacenado y detecta documentos que debieron procesarse pero no tienen registro en `@DBS_CHANGES`.

---

## 11. Idempotencia

### 11.1 Capas de Deduplicación

La estrategia tiene tres capas independientes de deduplicación, cada una con diferente alcance:

| Capa | Mecanismo | Duplicado que previene |
|---|---|---|
| **PTN Webhook** | `U_TxRef` en @DBS_CHANGES | Mismo evento PTN recibido 2+ veces (reintentos B1 o red) |
| **@DBS_QUEUE** | Clave natural `(ObjType, DocEntry, CmpDB, Status ∈ {PENDING, RUNNING})` | Múltiples PTN para el mismo documento antes de procesar |
| **Staging UPSERT** | `ON CONFLICT (company_id, "DocEntry") WHERE hash cambia` | Fetch duplicado o reconciliación que trae el mismo estado |

### 11.2 Deduplicación en Webhook (Capa 1)

Antes de escribir en @DBS_CHANGES, el webhook verifica:

```
GET /b1s/v1/DBS_CHANGESCollection?$filter=U_TxRef eq '{TransactionId}'&$select=Code
```

- Resultado vacío → INSERT normal.
- Resultado con registros → responder `202 Accepted` sin insertar.

Costo: una llamada Service Layer adicional por PTN. Justificado para evitar procesamiento duplicado.

### 11.3 Idempotencia en @DBS_QUEUE (Capa 2)

Al crear/actualizar entrada en @DBS_QUEUE:

```
¿Existe entrada con ObjType + DocEntry + CmpDB y Status IN ('PENDING', 'RUNNING')?
  Sí → PATCH: U_NChanges++, U_LastAction = nuevo, U_Priority = min(actual, nuevo) [menor número = más urgente]
  No → POST: nueva entrada con Status = 'PENDING'
```

Esto garantiza que incluso si el webhook es llamado en paralelo por dos PTN simultáneos del mismo documento, solo existe una entrada activa en la cola.

### 11.4 Idempotencia en Staging (Capa 3)

El patrón UPSERT existente con temporal guard es idempotente por diseño:

```sql
ON CONFLICT (company_id, "DocEntry") DO UPDATE SET ...
WHERE
    staging.source_hash_hex != EXCLUDED.source_hash_hex
    AND (
        EXCLUDED."UpdateDate" > staging."UpdateDate"
        OR (EXCLUDED."UpdateDate" = staging."UpdateDate"
            AND COALESCE(EXCLUDED."UpdateTSNorm", '000000') >= COALESCE(staging."UpdateTSNorm", '000000'))
    )
```

Si el Processor procesa el mismo documento dos veces con el mismo estado, el UPDATE no se ejecuta (condición WHERE falsa). No hay datos corruptos, no hay doble conteo en Power BI.

---

## 12. Auditoría

### 12.1 Tablas de Auditoría (DataBision PostgreSQL)

Además del estado en B1 (`@DBS_CHANGES`, `@DBS_QUEUE`), DataBision mantiene su propio log de auditoría en el schema `audit.*`:

#### `audit.ptn_received`
Log de cada llamada al webhook PTN. Incluye payload raw (para diagnóstico).

| Columna | Tipo | Descripción |
|---|---|---|
| `id` | UUID | PK |
| `tenant_id` | UUID | Tenant DataBision |
| `company_db` | VARCHAR(20) | Base de datos B1 |
| `obj_type` | VARCHAR(20) | ObjectType SAP |
| `doc_entry` | INT | DocEntry |
| `action` | VARCHAR(10) | ADD/UPDATE/CANCEL/CLOSE/REOPEN |
| `tx_ref` | VARCHAR(50) | TransactionId del payload PTN |
| `payload_json` | JSONB | Payload completo recibido |
| `change_ref` | VARCHAR(20) | @DBS_CHANGES.Code creado |
| `received_at` | TIMESTAMPTZ | Timestamp UTC de recepción |
| `was_duplicate` | BOOLEAN | Si fue deduplicado (no insertado) |
| `http_status` | SMALLINT | Status code respondido |

#### `audit.ingest_events`
Resultado de cada procesamiento desde @DBS_QUEUE.

| Columna | Tipo | Descripción |
|---|---|---|
| `id` | UUID | PK |
| `tenant_id` | UUID | Tenant DataBision |
| `company_db` | VARCHAR(20) | Base de datos B1 |
| `queue_ref` | VARCHAR(20) | @DBS_QUEUE.Code procesado |
| `obj_type` | VARCHAR(20) | ObjectType SAP |
| `doc_entry` | INT | DocEntry |
| `action_executed` | VARCHAR(10) | Acción efectivamente ejecutada |
| `worker_id` | VARCHAR(50) | Nodo que procesó |
| `attempt_number` | SMALLINT | Número de intento |
| `outcome` | VARCHAR(10) | `SUCCESS`/`SKIP`/`ERROR` |
| `rows_upserted` | SMALLINT | Filas afectadas en staging (cabecera + líneas) |
| `was_insert` | BOOLEAN | True si fue INSERT nuevo, false si UPDATE |
| `sl_latency_ms` | INT | Tiempo de llamada a Service Layer |
| `total_latency_ms` | INT | Tiempo total de procesamiento |
| `error_detail` | TEXT | Detalle del error si outcome = ERROR |
| `processed_at` | TIMESTAMPTZ | Timestamp UTC de fin |

#### `audit.dlq_events`
Entradas que alcanzaron el DLQ.

| Columna | Tipo | Descripción |
|---|---|---|
| `id` | UUID | PK |
| `tenant_id` | UUID | |
| `company_db` | VARCHAR(20) | |
| `queue_ref` | VARCHAR(20) | @DBS_QUEUE.Code |
| `obj_type` | VARCHAR(20) | |
| `doc_entry` | INT | |
| `last_error` | TEXT | Último error antes de BLOCKED |
| `total_attempts` | SMALLINT | |
| `blocked_at` | TIMESTAMPTZ | |
| `resolved_at` | TIMESTAMPTZ | NULL hasta resolución |
| `resolved_by` | VARCHAR(100) | Email del operador que resolvió |

### 12.2 Trazabilidad End-to-End

Para cualquier documento en staging, se puede reconstruir la cadena completa:

```
raw.sap_oinv (DocEntry=1045, company_id='acme')
    → raw_updated_at_utc = '2026-05-29T14:32:11Z'
    ↑ procesado por
audit.ingest_events (queue_ref='00042', processed_at='2026-05-29T14:32:11Z')
    → action_executed='UPDATE', worker_id='worker-02', sl_latency_ms=187
    ↑ originado por
@DBS_QUEUE.Code='00042' (ObjType=13, DocEntry=1045, U_NChanges=2)
    ↑ alimentado por
@DBS_CHANGES.Code='00138', Code='00137' (2 PTN del mismo documento)
    ↑ recibidos en
audit.ptn_received (tx_ref='TXN-2026...', received_at='2026-05-29T14:31:58Z')
```

---

## 13. Performance y Escalabilidad

### 13.1 Batching

- El Processor dequeue `BatchSize` entradas por ciclo (default: `20`).
- Dentro del batch, los documentos **sin líneas** (OCRD, OITM) se procesan en paralelo (max 5 concurrent Service Layer calls).
- Los documentos **con líneas** se procesan secuencialmente dentro del batch para evitar conflictos de escritura en tablas de líneas (misma tabla `raw.sap_inv1` puede recibir líneas de múltiples facturas).

### 13.2 Rate Limiting contra Service Layer

SAP B1 Service Layer impone límites de concurrencia por sesión y puede responder `HTTP 429` o degradar el rendimiento. DataBision aplica:

| Límite | Valor default | Configurable por tenant |
|---|---|---|
| Llamadas concurrentes a Service Layer | 3 | Sí |
| Llamadas por minuto a Service Layer | 50 | Sí |
| Pause entre batches | 500 ms | Sí |
| Timeout por llamada Service Layer | 30 s | No |

Configuración almacenada en `ctl.tenant_config`:
```json
{
  "delta_batch_size": 20,
  "delta_sl_concurrency": 3,
  "delta_sl_rate_per_min": 50,
  "delta_pause_ms": 500
}
```

### 13.3 Workers Distribuidos

Para tenants con alto volumen (> 500 documentos/día), el Processor puede escalar horizontalmente:

- Hasta `MaxWorkersPerTenant` nodos procesando en paralelo (default: `2`).
- El lock optimista en @DBS_QUEUE (`WHERE U_Status = 'PENDING'` al hacer el PATCH) garantiza que dos workers no toman el mismo documento.
- `U_Worker` en @DBS_QUEUE permite detectar workers muertos (heartbeat timeout: 5 min).

### 13.4 Detección de Workers Muertos

Un supervisor job revisa cada 5 minutos:

```
SELECT * FROM @DBS_QUEUE WHERE U_Status = 'RUNNING' AND U_StartAt < NOW() - INTERVAL '10 minutes'
```

Para cada entrada encontrada: reset a `PENDING`, incrementa `U_Attempts`, evalúa si va a DLQ.

### 13.5 Monitoreo y Alertas

Métricas expuestas en `/metrics` (Prometheus):

| Métrica | Descripción |
|---|---|
| `databision_delta_queue_depth{tenant, status}` | Entradas en @DBS_QUEUE por estado |
| `databision_delta_processing_latency_ms{tenant, obj_type}` | P50/P95/P99 de latencia total |
| `databision_delta_sl_latency_ms{tenant}` | Latencia de llamadas Service Layer |
| `databision_delta_retry_rate{tenant}` | % de entradas que requirieron > 1 intento |
| `databision_delta_dlq_total{tenant}` | Acumulado de entradas que llegaron al DLQ |
| `databision_delta_ptn_received_total{tenant, obj_type}` | PTN recibidos por tipo de objeto |
| `databision_delta_circuit_breaks_total{tenant}` | Activaciones del circuit breaker |

Alertas recomendadas:
- `queue_depth{status="PENDING"} > 200` por más de 10 min → warning.
- `queue_depth{status="BLOCKED"} > 0` → alerta crítica (DLQ tiene elementos).
- `circuit_breaks_total` incrementa → alerta crítica (Service Layer del cliente no responde).

---

## 14. Seguridad y Acceso

### 14.1 Autenticación del Webhook PTN

- El endpoint PTN **no usa JWT** — los JWTs son para usuarios humanos del portal.
- Cada tenant tiene un `ptn_secret` (32 bytes random, generado en onboarding).
- B1 envía el header `X-DataBision-PtnKey: {HMAC-SHA256(payload, ptn_secret)}`.
- DataBision valida el HMAC antes de cualquier procesamiento.
- Rotación del secreto cada 90 días o on-demand desde SuperAdmin.

### 14.2 Credenciales Service Layer

- DataBision almacena las credenciales Service Layer del cliente (usuario + contraseña o token OAuth) en Azure Key Vault, encriptadas.
- Las credenciales se resuelven en memoria en el momento del request; nunca se persisten en logs ni en @DBS_CHANGES/@DBS_QUEUE.
- Las sesiones Service Layer se renuevan automáticamente (cookie B2BSESSION con TTL de 30 min).

### 14.3 Aislamiento Multi-Tenant

- El endpoint PTN incluye `{tenantSlug}` en la URL: `POST /api/ptn/{tenantSlug}`.
- El Processor valida que el `CompanyDB` del payload pertenece al tenant del slug.
- Un payload PTN de un tenant no puede escribir en @DBS_CHANGES de otro tenant.
- En @DBS_QUEUE, `U_TenantId` es inmutable tras creación y es validado en cada operación.

---

## 15. Onboarding por Tenant

### 15.1 Pasos de Setup (ejecutados por DataBision Operations)

1. **Crear credencial Service Layer:** Almacenar usuario/password del cliente en Key Vault bajo `sap/{tenantSlug}/sl_credentials`.

2. **Crear UDTs en B1 del cliente** vía Service Layer:
   - POST `/b1s/v1/UserTablesMD` para @DBS_CHANGES
   - POST `/b1s/v1/UserTablesMD` para @DBS_QUEUE
   - POST `/b1s/v1/UserFieldsMD` para cada campo custom de ambas tablas

3. **Generar ptn_secret** y almacenar en Key Vault bajo `sap/{tenantSlug}/ptn_secret`.

4. **Registrar suscripciones PTN en B1** para cada ObjectType de Prioridad 1 y 2 soportado. En B1 Service Layer ≥ 10.x:
   ```
   POST /b1s/v1/NotificationSubscriptions
   {
     "EventType": "add|update|cancel|close",
     "ObjectType": "{code}",
     "CallbackUrl": "https://ingest.databision.app/api/ptn/{tenantSlug}",
     "ApiKey": "{hmac_key}"
   }
   ```
   > La API exacta varía por versión B1. Para versiones sin API de suscripción, el cliente configura manualmente la URL en B1 Administration > General Settings > Advanced.

5. **Ejecutar full-load inicial** de todos los ObjectTypes (modo polling) para tener staging sincronizado antes de activar el delta.

6. **Activar modo delta** en `ctl.tenant_config`: `"delta_enabled": true`.

7. **Verificar health** ejecutando el smoke test del PTN:
   - Crear un documento de prueba en B1 → verificar que aparece en @DBS_CHANGES en < 10 s.
   - Verificar que el documento aparece en staging en < 30 s.

### 15.2 Rollback

Si el modo delta falla para un tenant, se puede deshabilitar (`"delta_enabled": false`) y el sistema cae automáticamente al modo polling incremental. Los datos en @DBS_CHANGES y @DBS_QUEUE se preservan para diagnóstico.

---

## 16. Limitaciones Conocidas y Decisiones Explícitas

| Limitación | Decisión tomada |
|---|---|
| PTN no disponible en SAP B1 < 9.3 | Modo polling exclusivo para esas versiones |
| PTN en bulk operations (importación masiva vía DI) puede no disparar por cada documento | La reconciliación semanal cubre estos gaps |
| @DBS_CHANGES crece indefinidamente | Política de retención de 90 días para registros DONE |
| Service Layer no garantiza entrega de PTN si el endpoint destino no responde | La reconciliación semanal cubre los gaps de downtime |
| B1 Cloud SAP puede no permitir UDTs en algunos planes | Verificar en onboarding; si no disponible, usar solo tablas DataBision-side |
| Líneas de documento: no se sabe qué línea cambió, solo que el documento cambió | Se hace UPSERT completo de todas las líneas (mismo patrón que el extractor actual) |
| OCRD y OITM: PTN no siempre disponible para objetos maestro en todas las versiones B1 | Fallback a polling para esos objetos si PTN no llega |

---

*Documento generado como parte del diseño técnico de DataBision. Revisión requerida por arquitectura antes de iniciar implementación.*
