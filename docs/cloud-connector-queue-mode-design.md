# DataBision Cloud / Restricted Connector — SAP Queue Mode

Status: **Diseño técnico — sin implementación.**
Modalidad B del producto: cliente SAP B1 en hosting cloud / restringido donde **no se puede instalar agente local**, pero Service Layer está habilitado.

Power BI fuera de alcance salvo como consumidor futuro de Azure SQL.

---

## 1. Arquitectura Cloud Connector

```
┌─────────────────────────── SAP B1 CLOUD/RESTRICTED ──────────────────────────┐
│                                                                                │
│  ┌────────────────────┐                                                        │
│  │  SAP B1 HANA/SQL   │                                                        │
│  │  (cliente)         │                                                        │
│  │                    │                                                        │
│  │  ┌──────────────┐  │     ┌─────────────────────────────────────┐           │
│  │  │ Tablas SAP   │  │     │ TransactionNotification /            │           │
│  │  │ OINV ORIN    │──┼────►│ PostTransactionNotice (SP minimal)  │           │
│  │  │ ORDR ODLN    │  │     │  • INSERT @DBI_SYNC_QUEUE (ref only) │           │
│  │  │ OCRD OITM    │  │     │  • RETURN 0 inmediato                │           │
│  │  └──────────────┘  │     └────────────────┬────────────────────┘           │
│  │                    │                       ▼                                │
│  │  ┌─────────────────────────────────────────────────────┐                   │
│  │  │ @DBI_SYNC_QUEUE  (UDT)                              │                   │
│  │  │   PENDING / PROCESSING / DONE / ERROR / SKIPPED     │                   │
│  │  └─────────────────────────────────────────────────────┘                   │
│  │                              ▲                                              │
│  │                              │ INSERTs de reconciliación                    │
│  │  ┌───────────────────────────┴─────────────────────┐                       │
│  │  │ SP DBI_RECONCILE_QUEUE (job programado)         │                       │
│  │  │  • Busca docs por CreateDate/UpdateDate         │                       │
│  │  │  • NOT EXISTS en LOG ni PENDING                 │                       │
│  │  │  • INSERTa a la cola con source=RECON           │                       │
│  │  └─────────────────────────────────────────────────┘                       │
│  │                                                                              │
│  │  ┌─────────────────────────────────────────────────────┐                   │
│  │  │ @DBI_SYNC_LOG  (UDT, histórico de procesados)       │                   │
│  │  └─────────────────────────────────────────────────────┘                   │
│  │                                                                              │
│  └────────────────────────┬───────────────────────────────────────────────────┘
│                            │                                                    │
│                            │  Service Layer HTTPS :50000                        │
│                            ▼                                                    │
└──────────────────────────────────────────────────────────────────────────────-─┘
                                  │
                                  ▼
┌────────────────────────────── AZURE ────────────────────────────────────────────┐
│                                                                                   │
│  ┌──────────────────────────────────────────────────────┐                        │
│  │ Azure Function — Cloud Connector                     │                        │
│  │ TimerTrigger cada 5–15 min                           │                        │
│  │                                                       │                        │
│  │  1. Leader lock (singleton por tenant)               │                        │
│  │  2. SL Login                                          │                        │
│  │  3. GET pending por batch                             │                        │
│  │  4. Claim atómico → PROCESSING                        │                        │
│  │  5. Por entry: GET objeto + POST a Ingest API         │                        │
│  │  6. PATCH a DONE / ERROR                              │                        │
│  │  7. POST a @DBI_SYNC_LOG                              │                        │
│  │  8. Rate-limit + circuit breaker                      │                        │
│  │  9. SL Logout                                          │                        │
│  └────────────────────┬─────────────────────────────────┘                        │
│                        │                                                          │
│                        ▼                                                          │
│  ┌──────────────────────────────────────────────────────┐                        │
│  │ DataBision Ingest API + Azure SQL (tenant)           │                        │
│  │ raw.OINV / raw.INV1 / raw.OCRD / ...                 │                        │
│  │ ctl.extraction_run / ctl.extraction_error            │                        │
│  └──────────────────────────────────────────────────────┘                        │
│                                                                                   │
│  ┌──────────────────────────────────────────────────────┐                        │
│  │ Azure Key Vault (tenant)                             │                        │
│  │  • SL user / password                                 │                        │
│  │  • Ingest API key                                     │                        │
│  └──────────────────────────────────────────────────────┘                        │
└───────────────────────────────────────────────────────────────────────────────--─┘
```

### Principios de diseño

1. **SAP nunca es data warehouse.** La cola guarda **referencias** (ObjectType + DocEntry), no datos analíticos.
2. **TransactionNotification queda mínimo.** INSERT + return 0. Cualquier otra cosa rompe la operación del cliente.
3. **Reconciliación periódica suple lo que TN pierde.** No dependemos de un único mecanismo.
4. **Conector externo lee solo pendientes.** Nunca scan completo de SAP por Service Layer.
5. **Service Layer es lento. Diseñar para 100–300 req/min sostenidos.**
6. **Una Azure Function por tenant.** Aislamiento + facturación + límites de concurrencia.

---

## 2. Qué es SAP Queue Mode

**SAP Queue Mode** es un patrón (no una feature oficial de SAP B1) que combina tres piezas:

1. **Tabla de cola** dentro de SAP B1 (UDT `@DBI_SYNC_QUEUE`) que guarda **referencias mínimas** a documentos nuevos/modificados.
2. **Mecanismos de población** que escriben a la cola: `TransactionNotification` / `PostTransactionNotice` (cobertura en línea) + stored procedure de reconciliación (cobertura por barrido).
3. **Conector externo** (Azure Function) que lee la cola vía Service Layer, fetchea cada objeto referenciado, lo empuja a Azure SQL y marca el entry como procesado.

Diferencia clave vs polling directo de SL: en Queue Mode el conector consulta `U_DBI_SYNC_QUEUE` (volumen acotado = solo cambios pendientes), no las tablas de negocio completas. Esto reduce el consumo de Service Layer en órdenes de magnitud.

---

## 3. Cuándo SÍ usar Queue Mode

- Cliente SAP B1 en **hosting cloud/restringido**: sin acceso directo a HANA/SQL, sin posibilidad de instalar servicio Windows/Linux.
- Service Layer **habilitado y estable** (puerto 50000 HTTPS).
- Partner SAP acepta crear **2 UDTs y modificar TransactionNotification o PostTransactionNotice** mínimamente.
- Volumen de cambios diarios moderado (<10k documentos/día). Volúmenes mayores → considerar Modalidad A o tiering.
- Cliente acepta latencia de incremental de 5–15 min entre cambio y disponibilidad en Azure SQL.

---

## 4. Cuándo NO usar Queue Mode

- **Service Layer no disponible** o inestable → no hay forma de leer la cola desde afuera.
- **Partner SAP no autoriza modificar TN ni crear UDTs** → no se puede poblar la cola.
- Volumen de cambios **>50k docs/día sostenido** → SL no escala; sugerir Modalidad A con extractor en infra dedicada.
- Cliente requiere **latencia <2 min** (near real-time): SL polling + cola añade overhead; Modalidad A es mejor.
- **Carga inicial >5M filas** y cliente no permite ventana extendida de drenaje (días). Pedir CSV bulk como alternativa.
- Cliente con **HANA on-prem dedicado**: usar Modalidad A directamente, sale más simple y barato.

---

## 5. UDT `@DBI_SYNC_QUEUE`

### 5.0 Decisión: UDT (No Object) vs UDO registrado

Antes de definir campos hay que decidir cómo se expone la cola al Service Layer.

| Opción | Qué es | Pros | Contras |
|---|---|---|---|
| **UDT tipo `bott_NoObject`** | Tabla custom plana, sin entidad SAP detrás. Endpoint SL: `/b1s/v1/U_DBI_SYNC_QUEUE`. | Setup mínimo (solo `UserTablesMD` + `UserFieldsMD`); sin permisos UDO; sin lógica de SAP intermedia. | El endpoint y soporte a CRUD/ETags vía SL **depende de la versión de SAP B1** del cliente; algunas releases no exponen UDTs `NoObject` de forma estable por SL para PATCH/DELETE. |
| **UDO (User-Defined Object) registrado** | Objeto custom con su entidad oficial, basado en una UDT. Endpoint SL: `/b1s/v1/U_DBI_SYNC_QUEUE` o el alias del UDO según registro. | CRUD estable y oficial via SL; ETags y autorización integradas con el modelo SAP; mejor soporte de upgrades. | Requiere registrar el UDO (más pasos al onboarding); ocupa license slot de UDO en algunas configs; partner SAP suele exigir documentación adicional. |

#### Decisión

- **MVP: UDT `bott_NoObject`** — si y solo si en el ambiente cloud real del cliente se valida que el endpoint `/b1s/v1/U_DBI_SYNC_QUEUE` soporta de forma estable GET, POST, PATCH y DELETE con ETags razonables sobre 10k+ filas.
- **Fallback robusto: UDO registrado** — si la validación anterior falla (PATCH no funciona, ETags inconsistentes, performance degradada), se promueve la cola a UDO con la misma estructura subyacente. Sin cambios en la lógica del conector más allá del endpoint.

#### Plan de validación (semana 1 de onboarding)

1. Crear `@DBI_SYNC_QUEUE` como UDT `NoObject` en sandbox del cliente.
2. Insertar 5k filas dummy vía SL POST.
3. Probar: GET con $filter, $top, $select; PATCH masivo (100 PATCHs en 1 min); DELETE; ETags If-Match.
4. Si todo verde → fijar Opción UDT en producción.
5. Si algo amarillo → escalar a UDO. Documentar el cambio.

> Cualquier decisión final debe quedar **firmada por escrito** con el partner SAP del cliente. La estructura de campos (§7) es idéntica en ambas opciones.

### Propósito

Tabla de cambios pendientes de procesar. Una fila por evento de creación/modificación a un objeto SAP B1 relevante.

### Tipo de UDT en SAP B1 (default MVP)

- **Object Type:** `bott_NoObject` (No Object). No se asocia a una entidad SAP — es storage puro.
- **Archivable:** false (la limpieza la hace nuestro job).
- **Alternativa:** UDO registrado sobre la misma UDT, si la validación cloud lo requiere (§5.0).

### Endpoint Service Layer

`/b1s/v1/U_DBI_SYNC_QUEUE` (URL canónica de UDT tipo "No Object" en SL; idéntica para UDO registrado sobre la misma tabla).

---

## 6. UDT `@DBI_SYNC_LOG`

### Propósito

Histórico inmutable de eventos ya procesados (o descartados). Se usa para:

- Auditoría: qué se procesó, cuándo, con qué resultado.
- Reconciliación: para saber qué entries ya fueron procesados (NOT EXISTS en LOG).
- Telemetría: latencia, conteos, tasa de error.

### Tipo

- **Object Type:** `bott_NoObject`.
- **Archivable:** true (se mueve a almacenamiento frío cada 90 días).

### Endpoint Service Layer

`/b1s/v1/U_DBI_SYNC_LOG`.

---

## 7. Campos exactos sugeridos

### `@DBI_SYNC_QUEUE`

| Campo | Tipo SAP B1 | Tamaño | Mandatorio | Descripción |
|---|---|---|---|---|
| `Code` | Alphanumeric (auto) | 50 | Sí | PK auto-asignada por SAP B1 (o secuencia) |
| `Name` | Alphanumeric (auto) | 100 | No | Descripción libre — se rellena con `ObjectType:DocEntry` para legibilidad |
| `U_ObjectType` | Numeric | int | Sí | 13/14/17/15/2/4 (ver §10) |
| `U_ObjectKey` | Alphanumeric | 50 | Sí | `DocEntry` como string para docs; `CardCode`/`ItemCode` para maestros |
| `U_Action` | Alphanumeric | 30 | Sí | CREATE / UPDATE / CANCEL / CLOSE / DELETE_LOGICAL |
| `U_Status` | Alphanumeric | 20 | Sí | PENDING / PROCESSING / DONE / ERROR / SKIPPED |
| `U_QueuedAt` | Date+Time | datetime | Sí | UTC cuando se encoló |
| `U_QueuedSource` | Alphanumeric | 20 | Sí | TN / POST_TN / RECON_JOB / INITIAL_LOAD / MANUAL |
| `U_Priority` | Numeric | int | Sí | 0 normal; 10 reconciliación retroactiva; 100 manual high |
| `U_ProcessingSince` | Date+Time | datetime | No | UTC cuando pasó a PROCESSING |
| `U_ProcessingOwner` | Alphanumeric | 80 | No | Identidad del conector que reclamó (function instance id + tenant). Usado por claim fallback no-ETag (ver §18) |
| `U_AttemptCount` | Numeric | int | Sí | Default 0 |
| `U_NextAttemptAt` | Date+Time | datetime | No | Backoff para reintentos |
| `U_LastError` | Alphanumeric | 500 | No | Truncado mensaje último error |
| `U_ConnectorVersion` | Alphanumeric | 20 | No | Versión del conector que procesó/intentó |

### `@DBI_SYNC_LOG`

| Campo | Tipo | Tamaño | Mandatorio | Descripción |
|---|---|---|---|---|
| `Code` | Alphanumeric (auto) | 50 | Sí | PK |
| `Name` | Alphanumeric (auto) | 100 | No | `ObjectType:DocEntry:Status` |
| `U_ObjectType` | Numeric | int | Sí | |
| `U_ObjectKey` | Alphanumeric | 50 | Sí | |
| `U_Action` | Alphanumeric | 30 | Sí | |
| `U_QueuedAt` | Date+Time | datetime | Sí | Copiado del QUEUE |
| `U_ProcessedAt` | Date+Time | datetime | Sí | UTC fin de procesamiento |
| `U_Status` | Alphanumeric | 20 | Sí | DONE / ERROR_FINAL / SKIPPED |
| `U_AttemptsTotal` | Numeric | int | Sí | Total de intentos antes de cerrar |
| `U_DurationMs` | Numeric | int | Sí | Latencia procesamiento |
| `U_ConnectorVersion` | Alphanumeric | 20 | Sí | |
| `U_PayloadSizeKb` | Numeric | int | No | Tamaño del objeto fetcheado en KB |
| `U_LastError` | Alphanumeric | 500 | No | Mensaje final si ERROR_FINAL |
| `U_AzureBatchId` | Alphanumeric | 50 | No | UUID del batch enviado a Azure (correlación end-to-end) |

---

## 8. Estados

| Estado | Significado | Quién lo setea | Siguiente estado posible |
|---|---|---|---|
| **PENDING** | Encolado, esperando ser tomado | TN/PostTN/SP/Manual | PROCESSING o SKIPPED |
| **PROCESSING** | Tomado por un conector, en vuelo | Conector (claim atómico) | DONE / ERROR / PENDING (timeout zombi) |
| **DONE** | Procesado con éxito, fila copiada a Azure | Conector | (terminal — se mueve a @DBI_SYNC_LOG) |
| **ERROR** | Fallo recuperable; será reintentado | Conector | PENDING (auto) o ERROR_FINAL (tras N intentos) |
| **SKIPPED** | Descartado intencionalmente (duplicado lógico, objeto inválido) | Conector / Job | (terminal — log con razón) |

### Transiciones permitidas (state machine)

```
                  ┌─────────────┐
   (encolar)      │  PENDING    │ ◄──── (timeout)
   ─────────────► │             │ ◄──── (retry tras backoff)
                  └──────┬──────┘
                         │ claim atómico
                         ▼
                  ┌─────────────┐
                  │ PROCESSING  │
                  └──┬───┬──────┘
              éxito  │   │  fallo
                     ▼   ▼
              ┌──────────┐  ┌─────────┐
              │   DONE   │  │  ERROR  │
              └────┬─────┘  └────┬────┘
                   │             │ N≥maxAttempts
                   │             ▼
                   │       ┌──────────────┐
                   │       │ ERROR_FINAL  │
                   │       │ (en LOG)     │
                   │       └──────────────┘
                   ▼
          (mueve a @DBI_SYNC_LOG; row se borra de QUEUE)
```

### Estados terminales

`DONE`, `SKIPPED`, `ERROR_FINAL` son terminales. Las filas en estos estados se **mueven** a `@DBI_SYNC_LOG` y se borran de `@DBI_SYNC_QUEUE` por el job de limpieza.

---

## 9. Acciones

| Acción | Cuándo se usa | Cómo lo decide la fuente |
|---|---|---|
| **CREATE** | Alta de un nuevo documento o maestro | TransactionType `'A'` en TN; insert en tabla SAP |
| **UPDATE** | Modificación de un documento existente | TransactionType `'U'`; update en tabla SAP |
| **CANCEL** | Documento marcado como cancelado | `Canceled = 'Y'` se setea (TransactionType típicamente `'U'` con flag específico); o TransactionType `'C'` |
| **CLOSE** | Documento marcado como cerrado/pagado | `DocStatus = 'C'`; TransactionType `'L'` o `'U'` según versión SAP |
| **DELETE_LOGICAL** | Borrado lógico (raro en SAP B1) | TransactionType `'D'` — solo aplica a algunos objetos |

### Nota técnica

SAP B1 distingue internamente entre `Update`, `Cancel`, `Close` con la misma TransactionType `'U'` en algunos casos. El conector externo, cuando hace GET del objeto via SL, detecta los flags (`Canceled`, `DocStatus`) y los registra como parte del payload. La acción registrada en la cola es indicativa, no autoritativa — el estado real del objeto manda.

---

## 10. ObjectTypes iniciales (MVP)

| ObjectType ID | Objeto SAP B1 | Tabla cabecera | Tabla líneas | Service Layer endpoint |
|---|---|---|---|---|
| **13** | A/R Invoice | OINV | INV1 | `/Invoices` |
| **14** | A/R Credit Memo | ORIN | RIN1 | `/CreditNotes` |
| **17** | Sales Order | ORDR | RDR1 | `/Orders` |
| **15** | Delivery Note | ODLN | DLN1 | `/DeliveryNotes` |
| **2** | Business Partner | OCRD | (sin líneas) | `/BusinessPartners` |
| **4** | Item | OITM | (sin líneas) | `/Items` |

### Líneas: política

- Cuando un cabecera entra a la cola, el conector hace **un solo GET** al endpoint del documento (que incluye `DocumentLines` en la response).
- No se encolan líneas por separado.
- En Azure SQL se persisten cabecera (raw.OINV) + líneas (raw.INV1) en la misma transacción del Ingest API.

### Expansión futura

- 18 oPurchaseInvoices (OPCH)
- 22 oPurchaseOrders (OPOR)
- 20 oPurchaseDeliveryNotes
- 21 oPurchaseCreditNotes
- 30 oJournalEntries

No incluidos en MVP. Se agregan editando el SP de reconciliación + TN.

---

## 11. Cómo llenar la cola

Cuatro mecanismos, complementarios:

### 11.1 `TransactionNotification` (legacy, on-prem mayormente)

- Stored procedure que SAP B1 invoca **dentro de la transacción**, antes del commit.
- Parámetros recibidos: `@object_type`, `@transaction_type` (A/U/C/L/D), `@num_of_cols_in_keys`, `@list_of_cols_val_tab_del`, `@list_of_keys_tab_del`.
- Output: `@error` (0 = OK; ≠ 0 aborta la transacción del cliente).
- **Bloquea la sesión del usuario hasta que retorna.**

### 11.2 `PostTransactionNotice` (B1 9.2 PL10+ / 10.0+)

- Variante asíncrona post-commit. **No bloquea al usuario.**
- Mismos parámetros.
- **Preferida sobre TransactionNotification** si está disponible.
- Disponibilidad: depende de versión y SP de B1; verificar con partner.

### 11.2.1 ⚠️ Regla dura — best-effort no-bloqueante

**El INSERT a `@DBI_SYNC_QUEUE` desde TN/PostTN es best-effort y NUNCA debe bloquear la transacción de negocio.**

Una falla al encolar (UDT no existe, deadlock, error de tipo, permission denied, etc.) **no puede impedir** que el cliente cree/modifique/cancele/cierre/borre:

- Facturas (OINV)
- Notas de crédito (ORIN)
- Pedidos (ORDR)
- Entregas (ODLN)
- Clientes/proveedores (OCRD)
- Artículos (OITM)

Reportería **nunca** prevalece sobre operación. Implementación:

- En TN: el bloque que inserta a `@DBI_SYNC_QUEUE` va dentro de `BEGIN TRY ... END TRY BEGIN CATCH ... END CATCH` (SQL Server) o `BEGIN ... EXCEPTION WHEN OTHERS THEN ... END` (HANA SQLScript). Cualquier excepción se **traga** y se registra en un log separado (`@DBI_QUEUE_INSERT_ERRORS` o equivalente).
- El SP siempre retorna `@error = 0`, sin importar lo que pase con la cola.
- La reconciliación (§11.3) recoge cualquier cambio omitido por una falla de TN. La pérdida es temporal.

**Excepción única:** si el cliente firma por escrito que prefiere bloquear ventas antes que perder un dato de reportería, se aplica la versión "strict" (que sí retorna `@error ≠ 0`). Esta excepción debe quedar archivada en el expediente del tenant con firma del responsable del cliente. Default: best-effort.

### 11.3 Stored procedure programado (`DBI_RECONCILE_QUEUE`) — ventana escalonada

- SP independiente que corre con **ventanas escalonadas según la hora del día**, no con una ventana fija.
- Para cada fila encontrada: si NO existe en `@DBI_SYNC_LOG` reciente NI en `@DBI_SYNC_QUEUE` pendiente/procesando → INSERTa con `U_QueuedSource = 'RECON_JOB'`.

#### Ventanas

| Nivel | Cuándo | Lookback | Frecuencia |
|---|---|---|---|
| **Hourly** | Cada hora durante horario operativo (08:00–20:00 local) | **Últimas 2 horas** | 1× por hora |
| **Nightly** | 02:00 hora local | **Últimas 24 horas** | 1× por noche |
| **Weekly / cierre de mes** | Día 1 del mes 03:00 local + domingo nocturno opcional | **Últimos 7 días** (o 30 días al cierre de año) | 1× por mes (o por semana) |

#### Por qué NO 7 días cada hora

Una reconciliación cada hora con lookback de 7 días lee, en cada corrida, las 6 tablas filtradas por `UpdateDate >= now - 7d OR CreateDate >= now - 7d`. En clientes medianos esto puede ser:

- 200k filas de OINV escaneadas.
- 1.5M filas de INV1 escaneadas (si se observa).
- × 6 tablas.
- × 24 corridas/día.

= un escaneo equivalente a multiplicar varias veces el volumen real de la base de SAP cada día. Resultado: **degrada la performance de SAP B1 para los usuarios reales** (locks, I/O en HANA/SQL, cache thrashing). Además es desperdicio: el 99% de las filas escaneadas no cambiaron.

La estrategia escalonada cubre los mismos casos con dos órdenes de magnitud menos de I/O:

- Hourly + lookback 2h: el cambio más reciente entra en <1 hora con costo despreciable.
- Nightly + 24h: atrapa cualquier cambio del día completo que TN haya perdido.
- Weekly/Cierre + 7d/30d: caza ajustes contables retroactivos y reaperturas.

### 11.4 Carga inicial (`DBI_BULK_INITIAL_LOAD`)

- SP one-shot que poblamiento histórico (últimos 24 meses por default).
- Inserta todos los DocEntry/CardCode/ItemCode históricos como `U_Action = 'CREATE'`, `U_QueuedSource = 'INITIAL_LOAD'`, `U_Priority = 0`.
- Se ejecuta una vez al onboarding. El conector drena en horas/días (rate-limited).

### Cobertura combinada

| Fuente | Pros | Contras |
|---|---|---|
| TN / PostTN | Tiempo real, cobertura amplia | Toca a cada transacción del cliente; si falla se nota |
| SP reconciliación | Recupera lo perdido por TN | Latencia (depende frecuencia del job) |
| Carga inicial | Necesaria al onboarding | One-shot |

**Estrategia recomendada:** PostTN minimal + SP de reconciliación cada 1 hora + SP one-shot al onboarding. Si PostTN no disponible, TN minimal con las precauciones de §12.

### 11.5 ⚠️ FMS (Formatted Search) — NO como mecanismo principal

FMS dispara solo en eventos UI (campo formateado al guardar). **No** cubre:

- DI API operations
- Service Layer changes desde otras apps
- Bulk imports
- Movimientos por jobs internos SAP
- Modificaciones DB-side por consultoría/partner

Su cobertura es fundamentalmente incompleta. Se acepta solo como mecanismo redundante en escenarios donde TN/PostTN no son posibles, y nunca solo.

---

## 12. Qué NO hacer en `TransactionNotification`

`TransactionNotification` corre dentro de **cada transacción de SAP B1**: cada save de cualquier usuario, cada operación de DI API, cada batch del Service Layer. Si rompe, el usuario ve un error. Si tarda, el usuario espera.

### Lista de "nunca" en TN

| ❌ Anti-patrón | Por qué |
|---|---|
| **Hacer JOINs o lecturas pesadas** | Latencia → freeze del cliente B1 |
| **Llamar APIs externas (HTTP/SOAP)** | Timeouts; SAP no espera 30s por una API |
| **Lock en tablas de negocio** | Deadlock con la transacción del usuario |
| **Manejar lógica de negocio** | Cualquier RAISERROR aborta el guardado del usuario |
| **Insertar en muchas tablas** | Latencia escala lineal con cantidad de inserts |
| **Iterar sobre líneas (INV1, etc.)** | Volumen variable; algunos docs tienen 1000+ líneas |
| **Llamar a SPs grandes recursivos** | Performance imprevisible |
| **Tirar excepciones** | Aborta la transacción del usuario |
| **Try/catch que oculta errores** | Errores silenciosos rompen sincronización |

### Lo único que SÍ se hace en TN

```
SI @object_type IN (13, 14, 17, 15, 2, 4) Y @transaction_type IN ('A', 'U', 'C', 'L'):
   INSERT INTO "@DBI_SYNC_QUEUE" (Code, U_ObjectType, U_ObjectKey, U_Action, U_Status, U_QueuedAt, U_QueuedSource)
     VALUES (NEXT_VAL, @object_type, @key_from_list, MapAction(@transaction_type),
             'PENDING', CURRENT_TIMESTAMP, 'TN');
RETURN 0;
```

INSERT a UDT, retornar 0. Eso es todo. Nada más.

---

## 13. Estrategia recomendada (decisión final)

```
┌─────────────────────────────────────────────────────────────────┐
│  CAPA 1 — En línea (preferida, best-effort)                     │
│  ───────────────────────────────────────                        │
│  PostTransactionNotice (o TN si PostTN no disponible)           │
│    - Minimal: INSERT @DBI_SYNC_QUEUE dentro de TRY/CATCH        │
│    - Falla de INSERT NO bloquea la transacción de negocio       │
│    - Cobertura: ~95% de cambios en tiempo real                  │
│    - Latencia: <1 segundo                                       │
└─────────────────────────────────────────────────────────────────┘
                              +
┌─────────────────────────────────────────────────────────────────┐
│  CAPA 2 — Reconciliación escalonada (red de seguridad)          │
│  ────────────────────────────────────────────────────────       │
│  SP DBI_RECONCILE_QUEUE con tres niveles:                       │
│    • Hourly  → lookback 2h, en horario operativo                │
│    • Nightly → lookback 24h, a las 02:00 local                  │
│    • Weekly/Cierre → lookback 7d (30d en cierre de año)         │
│  Cobertura: 100% acumulado en 24h; lookback 2h gratis           │
│  Razón: 7d-cada-hora satura I/O de SAP — evitar                 │
└─────────────────────────────────────────────────────────────────┘
                              +
┌─────────────────────────────────────────────────────────────────┐
│  CAPA 3 — Carga inicial (one-shot)                              │
│  ────────────────────────────                                   │
│  SP DBI_BULK_INITIAL_LOAD (manual al onboarding)                │
│    - Poblamiento históricos 24 meses                            │
│    - U_QueuedSource = 'INITIAL_LOAD'                            │
│    - Drena durante días vía conector rate-limited               │
│    - Fallback: CSV bulk si SL es demasiado lento (§25)          │
└─────────────────────────────────────────────────────────────────┘
```

### Razones

- **TN/PostTN solo no alcanza:** algunos changes ocurren fuera de TN (jobs de SAP, restarts, scenarios edge).
- **Reconciliación sola es lenta:** latencia mínima = frecuencia del job. Inaceptable para BI moderno.
- **Combinados:** TN cubre el 95% rápido + reconciliación captura el 5% restante con latencia tolerable.

---

## 14. Estrategia de deduplicación

### Problema

Pueden encolarse múltiples entries por el mismo `(ObjectType, ObjectKey)`:

- TN inserta al cambiar la cabecera.
- Reconciliación inserta porque ve el cambio en UpdateDate.
- Usuario edita 3 veces seguidas: 3 entries.
- Conector ya procesó pero la reconciliación inserta uno nuevo.

### Reglas de dedupe (orden de aplicación)

1. **Dedupe en INSERT (cuando es posible):**
   - El SP de reconciliación incluye `WHERE NOT EXISTS` contra LOG y QUEUE pendiente:
     ```sql
     INSERT ... WHERE NOT EXISTS (
       SELECT 1 FROM "@DBI_SYNC_QUEUE"
       WHERE U_ObjectType = :ot AND U_ObjectKey = :ok
         AND U_Status IN ('PENDING', 'PROCESSING')
     )
     AND NOT EXISTS (
       SELECT 1 FROM "@DBI_SYNC_LOG"
       WHERE U_ObjectType = :ot AND U_ObjectKey = :ok
         AND U_ProcessedAt >= DATEADD(HOUR, -1, NOW())
     )
     ```
2. **Dedupe en TN:** *opcional* — un `IF NOT EXISTS` chequea entries PENDING del mismo `(ObjectType, ObjectKey)` y omite si ya hay uno. Trade-off: agrega lectura a TN (latencia). Decisión MVP: **no dedupe en TN** (permitir duplicados; el conector los colapsa).
3. **Dedupe en el conector (coalescing):**
   - Al hacer GET pendientes con `$top=100`, el conector agrupa por `(ObjectType, ObjectKey)`.
   - Para cada grupo: procesa solo el más reciente; los anteriores se marcan `SKIPPED` con razón `'superseded_by_newer'`.
   - Garantiza idempotencia: el objeto en Azure refleja el estado actual SAP, no estados intermedios.

### Por qué dedupe-en-conector y no en SAP

- Mantener TN minimalista (§12).
- SAP no es responsable de la pipeline.
- El conector ya tiene la lógica de batch y la latencia tolera el filtrado.

---

## 15. Índices recomendados

### En `@DBI_SYNC_QUEUE`

UDTs no permiten índices custom desde el UI estándar de SAP B1. El partner DBA debe crearlos a nivel HANA/SQL directamente. Coordinarlo en el runbook de onboarding.

| Índice | Columnas | Tipo | Uso |
|---|---|---|---|
| `IX_QUEUE_STATUS_NEXT` | `(U_Status, U_NextAttemptAt, U_Priority DESC, U_QueuedAt)` | non-clustered | Picking de pendientes |
| `IX_QUEUE_OBJ` | `(U_ObjectType, U_ObjectKey)` | non-clustered | Dedupe + reconciliación |
| `IX_QUEUE_PROCESSING_SINCE` | `(U_Status, U_ProcessingSince)` WHERE U_Status = 'PROCESSING' | filtered | Liberar zombis |

### En `@DBI_SYNC_LOG`

| Índice | Columnas | Tipo | Uso |
|---|---|---|---|
| `IX_LOG_OBJ_PROC` | `(U_ObjectType, U_ObjectKey, U_ProcessedAt DESC)` | non-clustered | Dedupe lookup |
| `IX_LOG_PROC_DATE` | `(U_ProcessedAt)` | non-clustered | Retention scan |

### Performance esperada

- Pick batch de 100 pendientes: <10 ms.
- Dedupe lookup contra LOG: <5 ms.
- Liberación de zombis: O(N) sobre pequeñísimo subset, despreciable.

---

## 16. Estrategia de reintentos

### Política

| Intento | Backoff antes del próximo |
|---|---|
| 1 (inmediato) | — |
| 2 | 1 minuto |
| 3 | 5 minutos |
| 4 | 30 minutos |
| 5 (último) | 2 horas |
| 6+ | → ERROR_FINAL, mover a LOG |

### Implementación

- Tras fallo del intento N: PATCH a la entry con `U_Status = 'PENDING'`, `U_NextAttemptAt = NOW + backoff(N)`, `U_AttemptCount = N`, `U_LastError = trunc(err, 500)`.
- Pickup filtra por `U_Status = 'PENDING' AND (U_NextAttemptAt IS NULL OR U_NextAttemptAt <= NOW())`.

### Errores no-reintentables

Algunos errores NO ameritan reintento (entry pasa directo a ERROR_FINAL):

- HTTP 404 en SL: el objeto no existe (borrado físicamente). Mover a LOG con `SKIPPED + 'object_not_found'`.
- Schema validation en Azure: el objeto no cumple shape esperado. Mover a LOG con `ERROR_FINAL + 'schema_invalid'` para análisis manual.
- 401/403 al ingest API: credencial revocada. Alerta crítica.

### Errores reintentables (default)

- 5xx del Service Layer.
- 5xx del ingest API.
- Timeouts.
- Rate limit (429).

---

## 17. Cómo el conector lee pendientes vía Service Layer

### Endpoint

```http
GET /b1s/v1/U_DBI_SYNC_QUEUE
  ?$filter=U_Status eq 'PENDING' and (U_NextAttemptAt eq null or U_NextAttemptAt le 'YYYY-MM-DDTHH:MM:SS')
  &$orderby=U_Priority desc, U_QueuedAt asc
  &$top=100
  &$select=Code,U_ObjectType,U_ObjectKey,U_Action,U_AttemptCount,U_QueuedAt
Authorization: cookie B1SESSION=...; ROUTEID=...
```

### Lógica

1. Login SL → guarda `B1SESSION` cookie.
2. GET pendientes (página de 100).
3. **Claim atómico:** por cada entry, PATCH a `PROCESSING` (ver §18).
4. Agrupa por ObjectType → reduce overhead de routing.
5. Por entry: GET del objeto, POST al ingest API.
6. PATCH a DONE o ERROR.
7. POST a `@DBI_SYNC_LOG`.
8. Si queda más pendiente: rate-limit wait → siguiente página.
9. Logout.

### Paginación

- Una corrida procesa **hasta 1000 entries** (10 páginas × 100). Más que eso → próxima corrida.
- Si quedan pendientes al cierre: heartbeat reporta `pending_remaining > 0` → próxima corrida.

---

## 18. Cómo marcar PROCESSING

Service Layer es REST → sin transacciones DB-grade. Se simula atomicidad de "claim" de tres maneras complementarias.

### 18.1 Opción A — ETag / If-Match (preferida si disponible)

```http
PATCH /b1s/v1/U_DBI_SYNC_QUEUE('CODE-123')
If-Match: "ETag-anterior"
Content-Type: application/json

{
  "U_Status": "PROCESSING",
  "U_ProcessingSince": "2026-05-17T14:30:00",
  "U_ProcessingOwner": "func-acme-01:abc-uuid",
  "U_AttemptCount": 3,
  "U_ConnectorVersion": "1.0.3"
}
```

- `If-Match` con ETag previo: si otro conector concurrente ya tomó la entry, esta llamada falla con `412 Precondition Failed` → se descarta y se pasa a la siguiente.
- Service Layer asigna ETag automáticamente a cada fila de UDT en B1 reciente; **debe validarse en el ambiente cloud real del cliente** que el ETag se devuelve estable y el `If-Match` es honrado (algunas releases lo ignoran y aceptan PATCH sin guard).

### 18.2 Opción B — Fallback `U_ProcessingOwner` + re-lectura (siempre disponible)

Si la validación de ETags falla en el cliente, se usa el siguiente patrón **sin** depender de `If-Match`:

```
1. GET entry (relectura previa al claim)
   → confirmar U_Status = 'PENDING' (si cambió, saltar)

2. PATCH /U_DBI_SYNC_QUEUE('CODE-123')
   {
     "U_Status": "PROCESSING",
     "U_ProcessingSince": now_utc,
     "U_ProcessingOwner": "<function_instance_id>:<run_uuid>",
     "U_AttemptCount": <prev + 1>,
     "U_ConnectorVersion": "<version>"
   }

3. GET entry (relectura inmediata post-PATCH)
   → verificar U_ProcessingOwner == nuestro id
   → si igual: claim ganado, proceder
   → si distinto: otro conector ganó la carrera, saltar la entry

4. Procesar.
```

#### Por qué funciona

- El primer PATCH establece nuestro `U_ProcessingOwner`. Si otro conector hace PATCH después, sobreescribe.
- La relectura inmediata es la "prueba" de quién quedó dueño.
- Es una elección racional cuando el "último PATCH gana" — el conector que pierda simplemente salta.

#### Trade-offs

- 1 GET extra por entry (latencia +50–200 ms).
- Ventana pequeña entre los dos PATCHs donde dos conectores podrían trabajar la misma entry — mitigado por leader lock (§18.3).

### 18.3 Opción C — Leader lock por tenant (combinable con A o B)

- **Una sola Azure Function instance activa por tenant** vía blob lease (Azure Storage) o registro en `ops.leader_lock` (Azure SQL).
- TTL del lock: 10 min, renovado cada 2 min durante la corrida.
- Si una instancia muere, otra toma el lock cuando expira.

Esto elimina concurrencia entre instances pero **no** entre runs sucesivos que puedan solaparse (lock vencido por crash, etc.). Por eso A o B siguen siendo necesarias como segunda línea.

### 18.4 Recomendación MVP

- **Validar ETags en el cliente cloud real durante semana 1.**
- Si ETags OK → **Opción A + Opción C** (ETag con leader lock).
- Si ETags inestables → **Opción B + Opción C** (ProcessingOwner+relectura con leader lock).
- Documentar la decisión por tenant en el expediente del cliente.

---

## 19. Cómo marcar DONE

Tras procesar con éxito (objeto fetcheado + POST a Ingest API + 2xx):

```http
PATCH /b1s/v1/U_DBI_SYNC_QUEUE('CODE-123')
Content-Type: application/json

{
  "U_Status": "DONE"
}
```

Inmediatamente después, POST a LOG:

```http
POST /b1s/v1/U_DBI_SYNC_LOG
Content-Type: application/json

{
  "U_ObjectType": 13,
  "U_ObjectKey": "12345",
  "U_Action": "UPDATE",
  "U_QueuedAt": "2026-05-17T14:25:00",
  "U_ProcessedAt": "2026-05-17T14:30:42",
  "U_Status": "DONE",
  "U_AttemptsTotal": 1,
  "U_DurationMs": 4200,
  "U_ConnectorVersion": "1.0.3",
  "U_PayloadSizeKb": 18,
  "U_AzureBatchId": "5f1b3c2e-..."
}
```

El job de limpieza (§22) borra después la fila de QUEUE.

---

## 20. Cómo marcar ERROR

Tras fallo recuperable:

```http
PATCH /b1s/v1/U_DBI_SYNC_QUEUE('CODE-123')
{
  "U_Status": "PENDING",
  "U_ProcessingSince": null,
  "U_NextAttemptAt": "2026-05-17T15:00:00",
  "U_AttemptCount": 3,
  "U_LastError": "503 Service Unavailable from Ingest API"
}
```

Vuelve a `PENDING` con `U_NextAttemptAt` futuro → no será re-pickeada hasta esa hora.

Tras fallo terminal (intento N≥max o error no-reintentable):

```http
PATCH /b1s/v1/U_DBI_SYNC_QUEUE('CODE-123')
{
  "U_Status": "ERROR",
  "U_LastError": "Schema validation failed: missing DocEntry"
}
```

Y POST a LOG con `U_Status = 'ERROR_FINAL'`. El job de limpieza borra después de QUEUE.

---

## 21. Cómo evitar registros pegados en PROCESSING

Causas de zombis en PROCESSING:

- Crash de Azure Function durante el procesamiento.
- Timeout del Function execution.
- Network blip durante el PATCH final.
- Logout del session SL antes del PATCH.

### Job de liberación de zombis

Ejecutado por el propio conector al inicio de cada corrida (antes de pickup):

```sql
-- Conceptualmente (vía SL no SQL):
UPDATE "@DBI_SYNC_QUEUE"
SET    U_Status = 'PENDING',
       U_ProcessingSince = NULL,
       U_LastError = COALESCE(U_LastError, 'auto-released: stuck in PROCESSING')
WHERE  U_Status = 'PROCESSING'
  AND  U_ProcessingSince < DATEADD(MINUTE, -30, GETUTCDATE())
```

Vía Service Layer:

1. GET entries `U_Status eq 'PROCESSING' and U_ProcessingSince lt :threshold`.
2. Por cada una: PATCH a PENDING.

### Umbral

- 30 min default.
- Configurable por tenant (algunos clientes con SL lento pueden necesitar 60 min).
- Se loguea cada liberación con `U_LastError = 'auto-released: stuck >30min'`.

### Alertas

- Si el conector libera >50 zombis en una corrida → alerta P2 (algo está mal).
- Si la misma entry se libera 3× seguidas → mover a ERROR_FINAL y alertar manualmente.

---

## 22. Cómo limpiar histórico

### Movimiento QUEUE → LOG

Las filas terminales (DONE, SKIPPED, ERROR_FINAL) se mueven a LOG y se borran de QUEUE.

**Estrategia recomendada:** el conector hace el move sincrónicamente tras cada éxito/fallo terminal:

1. POST a `@DBI_SYNC_LOG` con el shape final.
2. DELETE de `@DBI_SYNC_QUEUE`.

Si el POST a LOG falla, la entry queda en QUEUE con `U_Status = DONE` — se intenta de nuevo en próxima corrida (job de "consolidación" al inicio de cada corrida).

### Retención LOG

- 90 días hot en SAP B1 LOG.
- Después: export nocturno a Azure Blob (Parquet) → DELETE de LOG.
- El export incluye `tenant_slug` para que en Azure se pueda consultar histórico.

### Job de purga

Diario, ejecuta SP:

```sql
DELETE FROM "@DBI_SYNC_LOG"
WHERE U_ProcessedAt < DATEADD(DAY, -90, GETUTCDATE());
```

Antes del delete: export al blob (con tenant_slug, batch_id, fecha).

---

## 23. Cómo evitar performance issues en SAP

### Reglas duras

1. **TN/PostTN ≤ 5 ms.** Solo INSERT a UDT + return.
2. **Job de reconciliación NO en horario pico.** Configurable; default 02:00 + 14:00 hora local.
3. **Índices creados desde día 1** (§15). Sin ellos, scan de UDT degrada con miles de entries.
4. **QUEUE acotada:** alerta si rows > 10k. Si > 50k → pausar reconciliación, escalar.
5. **LOG con retención corta** (90d) y export al blob.
6. **NO usar SELECT * en SPs** — siempre listas explícitas de columnas.
7. **Service Layer queries del conector siempre con `$top`** (max 100). No paginar más.
8. **No ejecutar dos jobs SAP simultáneos** sobre las mismas tablas (configurar scheduler para no solapar).

### Métricas a vigilar (en SAP)

- Tamaño de `@DBI_SYNC_QUEUE` y `@DBI_SYNC_LOG`.
- Tiempo promedio de TransactionNotification (debe ser <5 ms).
- Duración del job de reconciliación (debe ser <5 min).
- Uso de Service Layer (en logs de SAP).

---

## 24. Cómo mapear a Azure SQL raw

### Misma estructura que Modalidad A

El Ingest API recibe payloads con la misma forma para Modalidad A y B. Diferencia: `_source_modality` = `'B'`. Esto garantiza:

- Mismas tablas `raw.OINV`, `raw.INV1`, etc.
- Mismas reglas de MERGE con `(UpdateDate, UpdateTS)` (ver doc Modalidad A §22).
- Misma capa staging consumiéndolas.

### Forma del payload (POST al Ingest API)

```json
{
  "tenant_slug": "acme",
  "table": "OINV",
  "batch_id": "5f1b3c2e-...",
  "source_modality": "B",
  "source_event": {
    "queue_code": "CODE-123",
    "queued_at": "2026-05-17T14:25:00Z",
    "action": "UPDATE",
    "object_type": 13
  },
  "rows": [
    {
      "DocEntry": 12345,
      "DocNum": 98765,
      "CardCode": "C00001",
      "UpdateDate": "2026-05-17",
      "UpdateTS": 51200,
      "CreateDate": "2025-08-12",
      "CreateTS": 31415,
      ...
      "Lines": [
        { "LineNum": 0, "ItemCode": "ABC", "Quantity": 1, "Price": 100.00, ... },
        { "LineNum": 1, ... }
      ]
    }
  ]
}
```

### Importante

- Para documentos (OINV, ORIN, ORDR, ODLN): el batch incluye **cabecera + líneas anidadas** en una sola entrada. El Ingest API separa al persistir.
- Para maestros (OCRD, OITM): sin líneas.
- `UpdateDate` viene de SAP como ISO date; `UpdateTS` viene como **INT decimal** (sin ceros a la izquierda), lo cual es engañoso visualmente (`91530` y `9153` se ven similares pero son momentos distintos: `09:15:30` vs `00:09:15`).

### 24.1 Normalización obligatoria de `CreateTS` / `UpdateTS`

`UpdateTS` y `CreateTS` en SAP B1 se almacenan como **INT que representa HHMMSS sin ceros a la izquierda**. Por ejemplo, `9:15:30 AM` se guarda como `91530`, y `00:09:15` como `915`. Tratarlos como números crudos rompe comparaciones, ordenamientos y joins.

#### Regla estándar — se aplica tanto en el conector como en el Ingest API y en SQL del warehouse

1. **Siempre normalizar a string `HHMMSS` con LPAD a 6 caracteres** antes de cualquier comparación, persistencia o serialización:
   ```
   ts_normalized = LPAD(CAST(U_UpdateTS AS VARCHAR), 6, '0')
   // 91530  → "091530"
   // 915    → "000915"
   // 235959 → "235959"
   ```
2. **No tratar `UpdateTS`/`CreateTS` como decimal visual.** Cualquier render UI, log o reporte usa el formato normalizado.
3. **Persistir en Azure SQL como `CHAR(6)` o `VARCHAR(6)` en columnas `UpdateTSNorm` / `CreateTSNorm`** además de la columna original `INT` (para auditoría).
4. **El watermark de reconciliación se compone como triple `(Date, TS_Normalized, ObjectKey)`** — igual patrón que Modalidad A (ver `dedicated-extractor-design.md` §14).

#### Por qué string y no INT

- Ordenamiento total trivial: `'091530' < '152000'` funciona lexicográficamente.
- Comparaciones entre runs idempotentes: `'000915' < '091500'` correcto.
- Imposible confundir `915` (que en INT podría parecer "9:15") con `091500` (que es 9:15:00 real).
- Joins entre tablas no se rompen por tipos divergentes.

#### Ejemplo de payload normalizado (corregido)

```json
{
  "DocEntry": 12345,
  "DocDate": "2026-05-17",
  "CreateDate": "2025-08-12",
  "CreateTS": 31415,             // raw, para auditoría
  "CreateTSNorm": "031415",      // 03:14:15
  "UpdateDate": "2026-05-17",
  "UpdateTS": 152000,            // raw
  "UpdateTSNorm": "152000"       // 15:20:00
}
```

#### Comparación en consultas HANA al normalizar

```sql
-- Forma correcta para comparar (Date, TS) entre filas:
WHERE   "UpdateDate" >  :last_update_date
   OR ( "UpdateDate" = :last_update_date
        AND LPAD("UpdateTS", 6, '0') > :last_update_ts_norm )
```

#### Triple para reconciliación

`(Date, TS_Normalized, ObjectKey)`:
- `Date` — DATE.
- `TS_Normalized` — CHAR(6).
- `ObjectKey` — DocEntry / CardCode / ItemCode (string).

Este triple es el cursor de incremental y el ancla para detección de gaps en reconciliación (ver §11.3 y Apéndice G).

---

## 25. Carga inicial histórica en cliente cloud

### Problema

Service Layer es lento (~100–300 req/min). Para 500k facturas históricas → 30–50 horas de drenaje continuo.

### Tres opciones

#### Opción A — Drenaje completo via SL (default)

1. Al onboarding: el partner ejecuta `DBI_BULK_INITIAL_LOAD` que llena `@DBI_SYNC_QUEUE` con 24 meses de DocEntries de las 6 tablas (`U_QueuedSource = 'INITIAL_LOAD'`, `U_Priority = 0`).
2. El conector drena en background durante días, rate-limited.
3. Mientras tanto, TN/PostTN sigue inyectando nuevos cambios → `U_Priority = 0` (mismo nivel) pero entran después por orden FIFO.
4. **Trade-off:** el cliente no tiene datos completos en Azure hasta que la cola se drene (días).

#### Opción B — Bulk import vía CSV del partner

1. Partner exporta 24m de OINV/INV1/ORIN/RIN1/OCRD/OITM a CSV (usando SQL directo o consulta nativa).
2. CSVs cargados manualmente al Azure SQL raw del tenant.
3. La cola incremental arranca limpia desde T0 → solo cambios post-CSV.
4. **Trade-off:** requiere acceso DB del partner; algunos no lo permiten.

#### Opción C — Modalidad A temporal solo para inicial

1. Si el cliente tiene una réplica HANA temporal accesible (ventana de mantenimiento de 1-2 días), instalar extractor temporal (Modalidad A) solo para la inicial.
2. Una vez cargada la inicial: desinstalar extractor, arrancar conector cloud (Modalidad B) para incremental.
3. **Trade-off:** logística compleja; solo viable si partner colabora.

### Recomendación

Antes de elegir: **el drenaje vía Service Layer es lento por diseño**. Aún con SL óptimo (200 req/min), un cliente con 500k–1M documentos históricos toma 3–10 días reales en quedar al día. Para muchos clientes este tiempo es inaceptable comercialmente.

#### Cuándo usar cada opción

| Volumen histórico estimado | SL throughput observado | Opción recomendada |
|---|---|---|
| < 100k filas totales | Cualquiera | **A — Drenaje SL** (queda en horas) |
| 100k–300k | ≥150 req/min | **A — Drenaje SL** (queda en 1–2 días) |
| 100k–300k | <150 req/min | **B — CSV bulk** |
| 300k–1M | Cualquiera | **B — CSV bulk** (default) |
| > 1M | Cualquiera | **B — CSV bulk** obligatorio; **C** si partner permite réplica HANA temporal |
| Cliente exige <48h time-to-data | Cualquiera | **B — CSV bulk** o **C** |

#### Cómo obtener el CSV bulk (Opción B detallada)

El export puede salir de uno de estos canales, en orden de preferencia:

1. **SAP Query Generator** (UI estándar de SAP B1): partner ejecuta queries pre-definidas por DataBision (`SELECT * FROM OINV WHERE DocDate >= ...`) y exporta a Excel/CSV. Compatible con cualquier hosting.
2. **HANA Studio o HANA Cockpit**: si el partner tiene acceso DBA al HANA del cliente, exporta resultsets directos como CSV. Más rápido para volúmenes grandes.
3. **API del proveedor cloud SAP** (p. ej. cloud hosting partner ofrece "export schema dump"): cuando está disponible, es la forma más limpia y completa.
4. **Backup logical / dump** del esquema SBO_<COMPANY>: solo si hosting lo permite y se firma NDA de tratamiento de datos.

Independiente del canal, el flujo es:

```
1. Partner produce CSV por tabla MVP (OCRD, OITM, OSLP, OINV, INV1, ORIN, RIN1, ORDR, RDR1, ODLN, DLN1).
2. CSVs subidos vía portal admin DataBision (HTTPS multipart) o blob storage privado.
3. Ingest API carga a raw.* con _source_modality='B', _ingested_via='CSV_BULK'.
4. Validación: count CSV vs count en raw después del carga.
5. Inicialización de checkpoints:
     last_update_date/ts/key = MAX(UpdateDate, normalized UpdateTS, NaturalKey) en CSV
     last_create_date/ts/key = MAX(CreateDate, normalized CreateTS, NaturalKey) en CSV
     initial_loaded = true
6. Activar TN/PostTN del cliente.
7. Reconciliación semanal compara: any drift entre CSV histórico y SAP actual.
```

#### Carga inicial asistida temporal — patrón híbrido

Cuando partner permite **una ventana de mantenimiento de 1–2 días** con acceso temporal a HANA (Modalidad A only para inicial):

1. Instalar agente Modalidad A temporal en infra del partner.
2. Drenar 24 meses históricos en horas.
3. Desinstalar agente.
4. Habilitar UDTs + TN/PostTN.
5. Activar conector Modalidad B incremental.
6. Los checkpoints quedan inicializados desde la inicial vía A; B continúa desde ahí.

Trade-off: logística más compleja, pero combina velocidad de A con sostenibilidad de B.

#### Resumen rápido

- **Default operacional:** intentar Opción A (drenaje SL) hasta 300k filas con SL razonable.
- **Default comercial:** prometer Opción B (CSV bulk) en la oferta inicial. Si A alcanza, mejor; si no, ya está pactado.
- **Reservado para casos especiales:** Opción C (Modalidad A temporal). Requiere colaboración profunda del partner.

### Estrategia para Opción A (drenaje SL)

```
1. DBI_BULK_INITIAL_LOAD inserta todos los DocEntry históricos como PENDING.
2. Conector funciona en modo "initial":
   - Sin rate-limit agresivo (usa el techo de SL).
   - Procesa 100/min sostenido.
3. Métricas: progreso en %, ETA, errores.
4. Al llegar a 0 pending de INITIAL_LOAD:
   - Cambiar a modo "incremental".
   - Marcar `ctl.extraction_checkpoint.initial_loaded = true`.
   - Notificar a operaciones.
```

---

## 26. Diferencia entre carga inicial y cola incremental

| Aspecto | Carga inicial | Cola incremental |
|---|---|---|
| **Frecuencia** | Una vez (onboarding) | Continuo (5–15 min) |
| **Tamaño cola** | Decenas/cientos de miles | Decenas/cientos |
| **Latencia tolerada** | Días | Minutos |
| **Source** | `INITIAL_LOAD` | `TN`, `POST_TN`, `RECON_JOB` |
| **Rate limit** | Usar el techo de SL | Conservador (<60% techo) |
| **Drenaje** | Sin pausas | Drenaje + pausa entre páginas |
| **Reconciliación** | Diferida hasta terminar inicial | Cada 1 hora |
| **Checkpoint inicial** | `initial_loaded=false` hasta drenar | `initial_loaded=true` permanente |
| **Reporte** | Progreso % + ETA | Lag + throughput |

### Política de transición

Mientras `initial_loaded = false`:

- Reconciliación pausada (no inserta a la cola).
- El conector procesa cola SIN rate-limit conservador (usa techo SL).
- TN/PostTN siguen activos (los nuevos cambios entran y se mezclan en orden FIFO).

Una vez vacía la cola y validada la inicial:

- Set `initial_loaded = true`.
- Activar reconciliación.
- Bajar a rate-limit conservador.

---

## 27. Seguridad de Service Layer

### Transporte

- HTTPS obligatorio, TLS 1.2 mínimo.
- Validación de certificado: igual regla que dedicated extractor §5 — en producción, valid CA o cert pinneado. Excepción solo con firma del cliente.

### Sesión

- Login retorna cookie `B1SESSION` + `ROUTEID`. Duración: default 30 min (configurable por SAP).
- Mantener una sola sesión por corrida (login → operaciones → logout).
- Renovar sesión si expira (cobertura por catch de 401, re-login automático).
- Logout explícito al final de cada corrida (libera recursos en SL).

### Throttling

- Service Layer típicamente limita a ~200 req/min sostenido. Default conservador del conector: **100 req/min**.
- Token bucket: 100 capacity, refill 100/min.
- Si 429: respect `Retry-After` header.

### Logs

- No loguear payload de objetos (PII).
- Sí loguear: `(endpoint, status, latency, error_code)`.
- Cookies de sesión NUNCA al log.

---

## 28. Usuario SAP recomendado

Crear usuario dedicado en SAP B1:

```
SAP User: DBI_INTEGRATION
License Type: Indirect Access (no necesita seat de UI)
Department: Integration (custom o "IT")
Email: integration@databision.app (placeholder, sin uso real)
Password: random 32 chars, en Azure Key Vault del tenant
Branch / Cost Center: vacío (no aplica)
```

### Roles SAP B1 asignados

- **Read** sobre las 6 tablas MVP (OINV, INV1, ORIN, RIN1, ORDR, RDR1, ODLN, DLN1, OCRD, OITM, OSLP).
- **Read** sobre tablas de soporte (OACT, OFPR, OWHS, OUSR) si requeridas por queries.
- **Read + Update** sobre `@DBI_SYNC_QUEUE` (para PATCH a PROCESSING/DONE/ERROR).
- **Read + Insert** sobre `@DBI_SYNC_LOG`.
- **NO write** sobre ninguna tabla de negocio. Cero.

### Bloqueado para

- Acceso UI de SAP B1 client (license type indirect access).
- DI API (no necesario).
- COM addons.
- Cualquier tabla fuera del scope MVP.

---

## 29. Permisos mínimos

Detalle por endpoint Service Layer:

| Endpoint | Método | Permiso necesario |
|---|---|---|
| `/Login`, `/Logout` | POST | Acceso a SL (todo usuario) |
| `/Invoices(:id)` | GET | Read en OINV/INV1 |
| `/CreditNotes(:id)` | GET | Read en ORIN/RIN1 |
| `/Orders(:id)` | GET | Read en ORDR/RDR1 |
| `/DeliveryNotes(:id)` | GET | Read en ODLN/DLN1 |
| `/BusinessPartners(:id)` | GET | Read en OCRD |
| `/Items(:id)` | GET | Read en OITM |
| `/U_DBI_SYNC_QUEUE` | GET, PATCH | Read + Update en UDT |
| `/U_DBI_SYNC_LOG` | POST | Insert en UDT |

Cualquier otro endpoint debe retornar 403 (auditable).

---

## 30. Logs y monitoreo

### Niveles

| Nivel | Storage | Retención |
|---|---|---|
| **Connector logs** | Application Insights (Azure Function) | 30 d |
| **Run telemetría** | `ctl.extraction_run` en Azure SQL del tenant | 1 año |
| **Per-entry log** | `@DBI_SYNC_LOG` en SAP + export Parquet | 90 d hot, 1 año warm |
| **Métricas agregadas** | Custom dashboard Azure Monitor | 90 d |

### Métricas clave

- `connector_runs_total{tenant, status}`
- `connector_entries_processed_total{tenant, object_type, status}`
- `connector_sl_requests_total{tenant, endpoint, status_code}`
- `connector_sl_latency_ms{tenant, endpoint, p95}`
- `connector_queue_depth{tenant}` (gauge — entries PENDING)
- `connector_zombies_released_total{tenant}`
- `connector_processing_lag_min{tenant}` (delta entre `U_QueuedAt` y `U_ProcessedAt`)

### Alertas

| Alerta | Severidad | Condición |
|---|---|---|
| Connector run failed | P1 | 3 corridas consecutivas en error |
| Queue depth | P2 | > 5000 entries por 30 min |
| Lag | P2 | p95 > 30 min |
| Service Layer 401/403 | P1 | Credencial revocada |
| Zombies released | P3 | >50 en una corrida |
| Error rate | P2 | >5% sobre 100 entries |
| Logging gap | P2 | Sin runs por 60 min |

---

## 31. Roadmap de implementación

### Sprint 1 (semana 1) — UDTs y SAP

- [ ] Spec final de UDTs (firmar con partner SAP).
- [ ] Partner crea `@DBI_SYNC_QUEUE` y `@DBI_SYNC_LOG` con campos definidos.
- [ ] Partner crea índices DBA-side (HANA/SQL).
- [ ] Partner crea usuario `DBI_INTEGRATION` con permisos.
- [ ] Validar Service Layer accesible desde Internet (firewall).
- [ ] Validar Service Layer login con `DBI_INTEGRATION`.

### Sprint 2 (semana 2) — SPs y TN

- [ ] SP `DBI_RECONCILE_QUEUE` (acepta param `lookback_days`).
- [ ] SP `DBI_BULK_INITIAL_LOAD`.
- [ ] Modificar TransactionNotification (o PostTN) minimal — pegar al final del SP existente.
- [ ] Job scheduler SAP que dispare reconciliación cada 1 hora.
- [ ] Pruebas en cliente lab: insertar 100 docs en SAP y validar que entran a QUEUE.

### Sprint 3 (semana 3) — Azure Function

- [ ] Proyecto `DataBision.Connector.Function` (.NET 8 isolated).
- [ ] Cliente SL con Polly (retry + circuit breaker).
- [ ] Leader lock por tenant (blob lease).
- [ ] Pipeline pickup → claim → process → done/error → log.
- [ ] Deploy Azure Function App + Application Insights + Key Vault binding.
- [ ] Ingest API endpoints + raw tables ya existen (compartidos con Modalidad A).

### Sprint 4 (semana 4) — onboarding tools

- [ ] Script de onboarding cliente: setup Key Vault + crear usuario + desplegar function.
- [ ] Dashboard de salud por tenant.
- [ ] Runbook errores comunes.
- [ ] Tests integrales con cliente lab: drenaje inicial completo + incremental durante 7 días.

---

## 32. MVP de 5 días para cliente cloud

Asumiendo Sprint 1–4 completos y agente Azure Function ya empaquetado:

### Día 1 — Pre-trabajo + autorización

- Reunión con cliente + partner SAP.
- Capturar: versión SAP B1, versión SL, IP/dominio público SL, ventana mantenimiento partner.
- Solicitar al partner:
  - Crear UDTs y SP de reconciliación (con script enviado).
  - Modificar TN/PostTN (con snippet enviado).
  - Crear usuario `DBI_INTEGRATION`.
  - Listar índices DBA-side.
- Provisionar Azure SQL DB del tenant + Key Vault.

### Día 2 — Validación SAP-side

- Partner ejecuta scripts en cliente.
- Validar:
  - Login SL desde Azure con `DBI_INTEGRATION` OK.
  - GET `/U_DBI_SYNC_QUEUE` retorna OK (vacía).
  - INSERT manual de prueba en QUEUE → visible vía SL.
  - TN/PostTN: hacer cambio dummy en SAP → entry aparece en QUEUE.
  - SP de reconciliación corre y popula correctamente.

### Día 3 — Function deploy + carga inicial trigger

- Deploy de Azure Function App del tenant.
- Configurar conexión + Key Vault.
- Partner ejecuta `DBI_BULK_INITIAL_LOAD` (24m).
- Activar function en modo "initial" (rate-limit alto).
- Monitorear progreso de drenaje.

### Día 4 — Drenaje + validación

- Continuar drenaje (puede tomar 1–3 días reales).
- Reconciliación count + sums contra SAP (las primeras tablas drenadas).
- Activar TN/PostTN para producción (nuevos cambios entran a cola).
- Validar end-to-end: editar factura en SAP → aparece en raw.OINV en <15 min.

### Día 5 — Sign-off + producción

- Reconciliación final completa.
- Cambiar function a modo "incremental" (rate-limit conservador).
- Activar alertas.
- Entregar runbook al cliente.
- Sign-off del customer admin.

*Nota: el "día 5" calendario puede ser día 7–10 real si la carga inicial tarda más por volumen. Diferenciar tiempo de personal vs tiempo de máquina.*

---

## 33. Criterios de aceptación

### Funcionales

1. ✅ TN/PostTN minimal validado: tiempo de ejecución <5 ms p95 sobre 1000 transacciones.
2. ✅ SP reconciliación corre <5 min con 7 días de lookback.
3. ✅ Carga inicial drena hasta 0 pendientes (medida del 100%).
4. ✅ Incremental: lag p95 < 15 min entre `U_QueuedAt` y `U_ProcessedAt`.
5. ✅ Recuperación automática de zombis (test: matar function mid-run, validar que próxima corrida los libera).
6. ✅ Reconciliación detecta y reprocesa cualquier omisión de TN.

### Operacionales

7. ✅ Dashboard expone queue depth, lag, error rate por tenant.
8. ✅ Alertas P1 funcionan: probar revocando API key del Ingest.
9. ✅ Runbook documenta 5 errores más comunes.
10. ✅ Logs no incluyen PII ni credenciales.

### Seguridad

11. ✅ Usuario `DBI_INTEGRATION` con permisos mínimos validados (intento de write a OINV → 403).
12. ✅ Credenciales SL en Key Vault con Managed Identity (no en config).
13. ✅ Service Layer accesible solo desde Azure Function IP (whitelist opcional).
14. ✅ Cero entries con PII en LOG (validado por scan).

### Performance

15. ✅ Service Layer: <100 req/min sostenido por function (no excede techo).
16. ✅ Queue depth promedio < 100 en operación normal.
17. ✅ Tamaño de `@DBI_SYNC_QUEUE` estable < 1k filas en steady state.

---

## 34. Riesgos y mitigaciones

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| **Partner SAP no autoriza modificar TN** | Alta | Crítico | Acordar pre-contrato; tener PostTN como plan B; si no, no aceptar al cliente |
| **Partner SAP no autoriza crear UDTs** | Media | Crítico | Misma; sin UDTs no hay modalidad B |
| **Service Layer no expuesto a Internet** | Media | Alto | Pedir al partner que configure NAT/firewall; VPN site-to-site si necesario |
| **Service Layer caído** | Media | Alto | Circuit breaker; alerta; aceptar lag temporal |
| **Service Layer lento (<50 req/min)** | Media | Medio | Bajar throughput; renegociar SLA con partner |
| **TN bloquea al usuario por bug nuestro** | Baja | Crítico | TN minimal absoluto + tests de regresión + rollback rápido (script de remoción listo) |
| **`@DBI_SYNC_QUEUE` crece sin freno** | Media | Alto | Monitoreo + alerta a 10k; pausar reconciliación a 50k |
| **`@DBI_SYNC_LOG` crece sin freno** | Media | Medio | Retención 90d + export a Blob |
| **Carga inicial dura semanas** | Media | Medio | Documentar ETA al cliente; ofrecer Opción B (CSV) |
| **Zombis pegados que no liberan** | Baja | Medio | Job de liberación tras 30 min; alerta si >50/run |
| **Upgrade SAP rompe endpoint SL** | Media | Alto | Tests de regresión por versión; matriz de compatibilidad |
| **Partner desinstala UDTs por error** | Baja | Crítico | Documentación clara; alerta si UDT no existe al arranque |
| **Session SL expira mid-batch** | Media | Bajo | Auto-relogin en catch 401 |
| **Rate limit 429 prolongado** | Baja | Medio | Backoff exponencial; respect Retry-After |
| **Reconciliación duplica entries** | Media | Bajo | Dedupe en conector (§14) absorbe |
| **TN dispara en transacciones masivas (importaciones)** | Alta | Medio | Cola se llena; conector drena en horas (esperado) |
| **Cliente cambia password de `DBI_INTEGRATION` sin avisar** | Baja | Alto | Alerta inmediata 401; runbook para rotación |
| **Azure Function timeout (10 min)** | Media | Bajo | Procesar máx 1000 entries por corrida; checkpoint frecuente |
| **Cambio de versión Service Layer rompe ETags** | Baja | Medio | Fallback a leader lock; tests de regresión |

---

## Apéndice A — Pseudocódigo Azure Function Timer Trigger

```
function CloudConnectorTimerTrigger(timer, context):
    tenant = ResolveTenantFromConfig(context)
    if not AcquireLeaderLock(tenant, ttl=10min):
        log("Skipped — another instance is running")
        return

    run = StartRun(tenant, modality='B', trigger=TIMER)
    session = null
    try:
        // ── 0. Release zombies before pickup ──
        ReleaseZombies(tenant, threshold_min=30)

        // ── 1. Login ──
        session = SLLogin(tenant)
        if session is null:
            run.MarkFailed("SL login failed")
            return

        pending_total_processed = 0
        max_per_run = 1000     // safety cap
        page_size  = 100
        rate_bucket = TokenBucket(rate=100/min, capacity=100)

        while pending_total_processed < max_per_run:
            // ── 2. GET pending page ──
            page = SLGet(
                session,
                path='/U_DBI_SYNC_QUEUE',
                filter="U_Status eq 'PENDING' and (U_NextAttemptAt eq null or U_NextAttemptAt le " + now_iso + ")",
                orderby="U_Priority desc, U_QueuedAt asc",
                top=page_size,
                select="Code,U_ObjectType,U_ObjectKey,U_Action,U_AttemptCount,U_QueuedAt"
            )
            if page.empty:
                break

            // ── 3. Coalesce duplicates within page ──
            entries = DedupeByObject(page)        // keep most recent per (ObjectType, ObjectKey)
            superseded = page.difference(entries)
            for s in superseded:
                MarkSkipped(session, s, reason='superseded_by_newer_in_page')

            // ── 4. Claim atomically (PATCH each to PROCESSING) ──
            claimed = []
            for entry in entries:
                ok = SLPatch(
                    session,
                    path="/U_DBI_SYNC_QUEUE('" + entry.Code + "')",
                    body={
                        "U_Status": "PROCESSING",
                        "U_ProcessingSince": now_iso,
                        "U_AttemptCount": entry.U_AttemptCount + 1,
                        "U_ConnectorVersion": APP_VERSION
                    }
                )
                if ok.status == 200 or ok.status == 204:
                    claimed.append(entry)
                else:
                    // probably already taken or stale; skip

            // ── 5. Process each claimed entry ──
            for entry in claimed:
                rate_bucket.consume(1)

                try:
                    obj = SLGet(
                        session,
                        path=EndpointFor(entry.U_ObjectType) + "(" + entry.U_ObjectKey + ")"
                    )
                    if obj is null:
                        FinalizeEntry(session, entry, status='SKIPPED',
                                      reason='object_not_found_in_SAP')
                        continue

                    payload = MapToIngestPayload(entry, obj)
                    push_ok = PushToIngestApi(tenant, payload)

                    if push_ok:
                        FinalizeEntry(session, entry, status='DONE',
                                      azure_batch_id=payload.batch_id,
                                      attempts_total=entry.U_AttemptCount,
                                      payload_size_kb=size(payload)/1024)
                        pending_total_processed += 1
                    else:
                        HandleError(session, entry, "ingest_api_failed",
                                    retryable=true)

                catch SLTransientError as e:
                    HandleError(session, entry, "sl_transient: " + e, retryable=true)
                catch SLNotFoundError as e:
                    FinalizeEntry(session, entry, status='SKIPPED',
                                  reason='sl_404_object_missing')
                catch SchemaError as e:
                    FinalizeEntry(session, entry, status='ERROR_FINAL',
                                  reason='schema: ' + e)
                catch Exception as e:
                    HandleError(session, entry, "unexpected: " + e, retryable=true)

            // ── 6. Inter-page pause to respect SL ──
            rate_bucket.wait_if_needed()

        run.MarkSuccess(processed=pending_total_processed)

    catch Exception as e:
        run.MarkFailed(error=e)
        log_error(e)
    finally:
        if session is not null:
            SLLogout(session)
        ReleaseLeaderLock(tenant)
        ReportRunToCloud(run)


function HandleError(session, entry, error_msg, retryable):
    if not retryable or entry.U_AttemptCount >= MAX_ATTEMPTS:
        FinalizeEntry(session, entry, status='ERROR_FINAL', reason=error_msg)
    else:
        backoff = ComputeBackoff(entry.U_AttemptCount)
        SLPatch(session, "/U_DBI_SYNC_QUEUE('" + entry.Code + "')", {
            "U_Status": "PENDING",
            "U_ProcessingSince": null,
            "U_NextAttemptAt": now + backoff,
            "U_LastError": truncate(error_msg, 500)
        })


function FinalizeEntry(session, entry, status, reason=null, azure_batch_id=null, ...):
    // Atomic-ish: log first, then delete from queue
    SLPost(session, "/U_DBI_SYNC_LOG", {
        "U_ObjectType": entry.U_ObjectType,
        "U_ObjectKey":  entry.U_ObjectKey,
        "U_Action":     entry.U_Action,
        "U_QueuedAt":   entry.U_QueuedAt,
        "U_ProcessedAt": now_iso,
        "U_Status":     status,
        "U_AttemptsTotal": entry.U_AttemptCount,
        "U_DurationMs": durationMs,
        "U_ConnectorVersion": APP_VERSION,
        "U_PayloadSizeKb": payload_size_kb,
        "U_LastError":  reason,
        "U_AzureBatchId": azure_batch_id
    })
    SLDelete(session, "/U_DBI_SYNC_QUEUE('" + entry.Code + "')")


function ReleaseZombies(tenant, threshold_min):
    // GET stuck entries
    stuck = SLGet(
        session,
        path='/U_DBI_SYNC_QUEUE',
        filter="U_Status eq 'PROCESSING' and U_ProcessingSince lt '" + (now - threshold_min) + "'",
        top=500
    )
    for entry in stuck:
        SLPatch(session, "/U_DBI_SYNC_QUEUE('" + entry.Code + "')", {
            "U_Status": "PENDING",
            "U_ProcessingSince": null,
            "U_LastError": "auto-released: stuck in PROCESSING > " + threshold_min + " min"
        })
        log_warn("Zombie released", tenant, entry.Code)
```

---

## Apéndice B — Estructura UDT (registro vía Service Layer)

### Crear `@DBI_SYNC_QUEUE`

```http
POST /b1s/v1/UserTablesMD
Content-Type: application/json

{
  "TableName": "DBI_SYNC_QUEUE",
  "TableDescription": "DataBision sync queue — referencias a cambios pendientes",
  "TableType": "bott_NoObject",
  "Archivable": "tNO"
}
```

### Crear campos (uno por uno, ejemplos)

```http
POST /b1s/v1/UserFieldsMD
{
  "TableName": "@DBI_SYNC_QUEUE",
  "Name": "ObjectType",
  "Description": "SAP B1 object type id",
  "Type": "db_Numeric",
  "EditSize": 11
}

POST /b1s/v1/UserFieldsMD
{
  "TableName": "@DBI_SYNC_QUEUE",
  "Name": "ObjectKey",
  "Description": "DocEntry / CardCode / ItemCode",
  "Type": "db_Alpha",
  "EditSize": 50,
  "Mandatory": "tYES"
}

POST /b1s/v1/UserFieldsMD
{
  "TableName": "@DBI_SYNC_QUEUE",
  "Name": "Status",
  "Description": "PENDING/PROCESSING/DONE/ERROR/SKIPPED",
  "Type": "db_Alpha",
  "EditSize": 20,
  "Mandatory": "tYES",
  "DefaultValue": "PENDING",
  "ValidValuesMD": [
    { "Value": "PENDING",     "Description": "Pending" },
    { "Value": "PROCESSING",  "Description": "In flight" },
    { "Value": "DONE",        "Description": "Processed" },
    { "Value": "ERROR",       "Description": "Failed, will retry" },
    { "Value": "SKIPPED",     "Description": "Discarded" }
  ]
}

-- ... etc. para U_Action, U_QueuedAt, U_QueuedSource, U_Priority, U_ProcessingSince,
--    U_AttemptCount, U_NextAttemptAt, U_LastError, U_ConnectorVersion
```

Mismo patrón para `@DBI_SYNC_LOG` con sus campos (§7).

---

## Apéndice C — SQL/SP para insertar cambios en cola

### TransactionNotification minimal — BEST-EFFORT (SQL Server syntax)

Snippet a pegar en el SP existente del cliente. **No reemplaza el SP completo** — se agrega al final, antes del `RETURN`. **Todo el bloque está envuelto en TRY/CATCH para garantizar que un fallo de la cola NO bloquea la operación de SAP.**

```sql
-- Al final de [dbo].[SBO_SP_TransactionNotification] o equivalente
-- Asume parámetros estándar: @object_type, @transaction_type, @num_of_cols_in_keys,
-- @list_of_key_cols_tab_del, @list_of_cols_val_tab_del

BEGIN TRY
   IF @object_type IN ('13','14','17','15','2','4')
      AND @transaction_type IN ('A','U','C','L')
   BEGIN
      DECLARE @key NVARCHAR(50)
      DECLARE @action NVARCHAR(30)

      -- @list_of_cols_val_tab_del trae el valor de la PK separado por tab
      SET @key = LEFT(@list_of_cols_val_tab_del,
                      CHARINDEX(CHAR(9), @list_of_cols_val_tab_del + CHAR(9)) - 1)

      SET @action = CASE @transaction_type
           WHEN 'A' THEN 'CREATE'
           WHEN 'U' THEN 'UPDATE'
           WHEN 'C' THEN 'CANCEL'
           WHEN 'L' THEN 'CLOSE'
           END

      INSERT INTO [@DBI_SYNC_QUEUE]
        (Code, Name, U_ObjectType, U_ObjectKey, U_Action, U_Status,
         U_QueuedAt, U_QueuedSource, U_Priority, U_AttemptCount)
      VALUES
        (NEWID(),
         CAST(@object_type AS NVARCHAR(10)) + ':' + @key,
         CAST(@object_type AS INT), @key, @action,
         'PENDING', GETUTCDATE(), 'TN', 0, 0)
   END
END TRY
BEGIN CATCH
   -- BEST-EFFORT: cualquier error al encolar se traga.
   -- Reconciliación (§11.3) lo recoge en próxima ventana.
   -- Log opcional en tabla separada para diagnóstico — sin propagar.
   BEGIN TRY
      INSERT INTO [@DBI_QUEUE_INSERT_ERRORS]
         (Code, Name, U_OccurredAt, U_ObjectType, U_TransactionType,
          U_KeyRaw, U_ErrorMessage)
      VALUES
         (NEWID(), 'tn-fail',
          GETUTCDATE(), @object_type, @transaction_type,
          LEFT(@list_of_cols_val_tab_del, 100),
          LEFT(ERROR_MESSAGE(), 500))
   END TRY
   BEGIN CATCH
      -- Si hasta el log de errores falla, seguimos sin hacer nada.
      -- La operación de negocio NO se interrumpe bajo ninguna circunstancia.
   END CATCH
END CATCH

-- RETURN 0 al final del SP (siempre, sin importar lo que pasó arriba)
```

### Variante HANA (HANA SQLScript)

```sql
-- Bloque equivalente para SAP B1 sobre HANA — al final del SP correspondiente
BEGIN
  BEGIN
    IF :object_type IN ('13','14','17','15','2','4')
       AND :transaction_type IN ('A','U','C','L') THEN

       DECLARE v_key NVARCHAR(50);
       DECLARE v_action NVARCHAR(30);

       v_key := SUBSTR_BEFORE(:list_of_cols_val_tab_del, CHAR(9));
       v_action := CASE :transaction_type
                     WHEN 'A' THEN 'CREATE'
                     WHEN 'U' THEN 'UPDATE'
                     WHEN 'C' THEN 'CANCEL'
                     WHEN 'L' THEN 'CLOSE'
                   END;

       INSERT INTO "@DBI_SYNC_QUEUE"
         ("Code","Name","U_ObjectType","U_ObjectKey","U_Action","U_Status",
          "U_QueuedAt","U_QueuedSource","U_Priority","U_AttemptCount")
       VALUES
         (LPAD(SYSUUID, 50, '0'),
          CAST(:object_type AS NVARCHAR) || ':' || v_key,
          CAST(:object_type AS INT), v_key, v_action,
          'PENDING', CURRENT_UTCTIMESTAMP, 'TN', 0, 0);
    END IF;
  EXCEPTION
    WHEN OTHERS THEN
      -- BEST-EFFORT: no propagar. Opcionalmente loguear a @DBI_QUEUE_INSERT_ERRORS.
      NULL;
  END;
END;
```

### Regla operativa

- El SP nunca retorna `@error ≠ 0` por una falla relacionada con la cola de reportería.
- Si el cliente firma la excepción "strict" (ver §11.2.1), se reemplaza el `END CATCH` por un `THROW`. **Esta es la decisión por defecto que NO se aplica.**

### PostTransactionNotice minimal (B1 9.2+)

Idéntico pero en `[dbo].[SBO_SP_PostTransactionNotice]` o equivalente. Variante: `U_QueuedSource = 'POST_TN'`.

### SP `DBI_RECONCILE_QUEUE` (HANA syntax — ajustar a SQL Server si aplica)

```sql
CREATE OR REPLACE PROCEDURE "DBI_RECONCILE_QUEUE"(IN p_lookback_days INT DEFAULT 7)
LANGUAGE SQLSCRIPT
AS
BEGIN
   -- OINV (ObjectType 13)
   INSERT INTO "@DBI_SYNC_QUEUE"
     ("Code", "Name", "U_ObjectType", "U_ObjectKey", "U_Action", "U_Status",
      "U_QueuedAt", "U_QueuedSource", "U_Priority", "U_AttemptCount")
   SELECT
      LPAD(SYSUUID, 50, '0'),
      '13:' || CAST(t."DocEntry" AS NVARCHAR),
      13,
      CAST(t."DocEntry" AS NVARCHAR),
      'UPDATE',
      'PENDING',
      CURRENT_UTCTIMESTAMP,
      'RECON_JOB',
      10,         -- prioridad media-alta para reconciliados
      0
   FROM "OINV" t
   WHERE (t."UpdateDate" >= ADD_DAYS(CURRENT_DATE, -:p_lookback_days)
          OR t."CreateDate" >= ADD_DAYS(CURRENT_DATE, -:p_lookback_days))
     AND NOT EXISTS (
         SELECT 1 FROM "@DBI_SYNC_QUEUE" q
         WHERE q."U_ObjectType" = 13
           AND q."U_ObjectKey" = CAST(t."DocEntry" AS NVARCHAR)
           AND q."U_Status" IN ('PENDING','PROCESSING')
     )
     AND NOT EXISTS (
         SELECT 1 FROM "@DBI_SYNC_LOG" l
         WHERE l."U_ObjectType" = 13
           AND l."U_ObjectKey" = CAST(t."DocEntry" AS NVARCHAR)
           AND l."U_ProcessedAt" >= ADD_HOURS(CURRENT_UTCTIMESTAMP, -1)
     );

   -- Repeat for ObjectType 14 (ORIN), 17 (ORDR), 15 (ODLN), 2 (OCRD), 4 (OITM)
   -- ...
END;
```

### SP `DBI_BULK_INITIAL_LOAD`

Mismo patrón, sin `NOT EXISTS` (asume cola vacía), con `p_months_back = 24`:

```sql
CREATE OR REPLACE PROCEDURE "DBI_BULK_INITIAL_LOAD"(IN p_months_back INT DEFAULT 24)
AS
BEGIN
   INSERT INTO "@DBI_SYNC_QUEUE" (...)
   SELECT ..., 'INITIAL_LOAD', 0, 0
   FROM "OINV"
   WHERE "DocDate" >= ADD_MONTHS(CURRENT_DATE, -:p_months_back);
   -- Repeat for other ObjectTypes
END;
```

---

## Apéndice D — Ejemplos de Service Layer calls

### Login

```http
POST /b1s/v1/Login
Content-Type: application/json

{
  "CompanyDB": "SBO_ACME",
  "UserName": "DBI_INTEGRATION",
  "Password": "<from-keyvault>"
}

→ 200 OK
Set-Cookie: B1SESSION=<uuid>; HttpOnly
Set-Cookie: ROUTEID=.node0;

{
  "@odata.context": "...",
  "SessionId": "<uuid>",
  "Version": "10.00.190",
  "SessionTimeout": 30
}
```

### GET pendientes

```http
GET /b1s/v1/U_DBI_SYNC_QUEUE?$filter=U_Status eq 'PENDING'&$orderby=U_Priority desc,U_QueuedAt asc&$top=100&$select=Code,U_ObjectType,U_ObjectKey,U_Action,U_AttemptCount,U_QueuedAt
Cookie: B1SESSION=<uuid>; ROUTEID=.node0

→ 200 OK
{
  "@odata.context": "...",
  "value": [
    {
      "Code": "C-001",
      "U_ObjectType": 13,
      "U_ObjectKey": "12345",
      "U_Action": "UPDATE",
      "U_AttemptCount": 0,
      "U_QueuedAt": "2026-05-17T14:25:00"
    },
    ...
  ]
}
```

### Claim atómico (PATCH a PROCESSING)

```http
PATCH /b1s/v1/U_DBI_SYNC_QUEUE('C-001')
Cookie: B1SESSION=...
Content-Type: application/json

{
  "U_Status": "PROCESSING",
  "U_ProcessingSince": "2026-05-17T14:30:00",
  "U_AttemptCount": 1,
  "U_ConnectorVersion": "1.0.3"
}

→ 204 No Content
```

### GET objeto SAP

```http
GET /b1s/v1/Invoices(12345)?$select=DocEntry,DocNum,CardCode,DocDate,DocTotal,DocStatus,Canceled,UpdateDate,UpdateTS,CreateDate,CreateTS,DocumentLines
Cookie: B1SESSION=...

→ 200 OK
{
  "@odata.context": "...",
  "DocEntry": 12345,
  "DocNum": 98765,
  "CardCode": "C00001",
  "DocDate": "2026-05-17",
  "DocTotal": 12450.50,
  "DocStatus": "bost_Open",
  "Canceled": "tNO",
  "UpdateDate": "2026-05-17",
  "UpdateTS": 152000,        // raw int — 15:20:00, no "152000"
  "CreateDate": "2025-08-12",
  "CreateTS": 94530,         // raw int — 09:45:30
  "DocumentLines": [
    { "LineNum": 0, "ItemCode": "ABC", "Quantity": 1, "Price": 10000.00, ... },
    { "LineNum": 1, "ItemCode": "DEF", "Quantity": 1, "Price": 2450.50, ... }
  ]
}
```

**Antes de empujar al Ingest API**, el conector normaliza los TS:

```
UpdateTSNorm = LPAD("152000", 6, "0") = "152000"
CreateTSNorm = LPAD("94530",  6, "0") = "094530"
```

El payload final a `/api/ingest/...` incluye ambos campos (raw + normalizado). Ver §24.1.

### Mark DONE + log

```http
PATCH /b1s/v1/U_DBI_SYNC_QUEUE('C-001')
{ "U_Status": "DONE" }

POST /b1s/v1/U_DBI_SYNC_LOG
{
  "U_ObjectType": 13,
  "U_ObjectKey": "12345",
  "U_Action": "UPDATE",
  "U_QueuedAt": "2026-05-17T14:25:00",
  "U_ProcessedAt": "2026-05-17T14:30:42",
  "U_Status": "DONE",
  "U_AttemptsTotal": 1,
  "U_DurationMs": 4200,
  "U_ConnectorVersion": "1.0.3",
  "U_PayloadSizeKb": 18,
  "U_AzureBatchId": "5f1b3c2e-..."
}

DELETE /b1s/v1/U_DBI_SYNC_QUEUE('C-001')
```

### Logout

```http
POST /b1s/v1/Logout
Cookie: B1SESSION=...

→ 204 No Content
```

---

## Apéndice E — Estrategia para no saturar Service Layer

### Reglas

1. **Una sola function instance por tenant** (leader lock). No paralelizar entre instances.
2. **Token bucket**: 100 req/min capacity, refill 100/min. Conservador (techo SAP ~200/min).
3. **Concurrencia interna acotada**: max 5 requests SL concurrentes dentro de una function execution.
4. **Inter-page wait**: 1 segundo entre páginas de pendientes.
5. **`$select` siempre**: nunca traer toda la entidad si no se necesita.
6. **`$top` siempre**: max 100 por consulta de cola; max 1 por GET de objeto.
7. **No usar `$batch`** del SL para escrituras (no es atomicidad real; complica error handling).
8. **Sesión persistente** durante la corrida (no login/logout por entry).
9. **Backoff exponencial en 429** con respect `Retry-After`.
10. **Circuit breaker**: 5 fallos consecutivos → suspender 5 min.

### Cálculo de throughput

- 100 req/min × 60 min × 24 h = 144,000 req/day por tenant.
- Cada entry de cola consume ~3 reqs (claim + GET + finalize) = ~48,000 entries/day teórico.
- Steady-state realista: 5,000–10,000 entries/day por tenant.
- Pico permitido: 20,000–30,000 entries/day (carga inicial o reconciliación retroactiva).

### Métricas

- `sl_requests_per_min` — gauge.
- `sl_429_total` — counter.
- `sl_circuit_state` — gauge.

---

## Apéndice F — Carga inicial usando Service Layer por lotes

```
PRE-CONDICIONES:
  - UDTs creadas en SAP.
  - Usuario DBI_INTEGRATION con permisos.
  - Azure Function deploy + Key Vault.
  - Azure SQL del tenant vacía (raw.*).

EJECUCIÓN:

1. Partner ejecuta en SAP:
     EXEC DBI_BULK_INITIAL_LOAD @months_back = 24

2. Verifica:
     SELECT U_ObjectType, COUNT(*) FROM "@DBI_SYNC_QUEUE"
       WHERE U_QueuedSource = 'INITIAL_LOAD' AND U_Status = 'PENDING'
       GROUP BY U_ObjectType

   Resultado esperado típico (cliente mediano):
     ObjectType  Count
     2 (OCRD)    5,000
     4 (OITM)    20,000
     13 (OINV)   200,000
     14 (ORIN)   30,000
     15 (ODLN)   150,000
     17 (ORDR)   180,000

3. Activar function en modo INITIAL:
     - rate_bucket: 200/min (techo)
     - max_per_run: 5000 entries
     - no rate-limit conservador
     - reconciliación pausada

4. Función corre cada 5 min en modo INITIAL.
   ETA total = total_entries / 8000_per_hour
   Ejemplo: 585k entries / 8000 = ~73 horas = ~3 días.

5. Monitoreo:
     SELECT
       U_QueuedSource,
       U_Status,
       COUNT(*) total,
       AVG(DATEDIFF(MINUTE, U_QueuedAt, U_ProcessedAt)) avg_lag_min
     FROM "@DBI_SYNC_LOG"
     WHERE U_ProcessedAt >= ADD_HOURS(NOW(), -1)
     GROUP BY U_QueuedSource, U_Status

6. Al llegar a 0 pendientes INITIAL_LOAD:
     - Reconciliación full sweep (24 meses) para validar.
     - Comparar counts SAP vs Azure SQL.
     - Sign-off.
     - Cambiar function a modo INCREMENTAL:
         - rate_bucket: 100/min (conservador)
         - max_per_run: 1000
         - reconciliación activa cada 1h
     - Set ctl.extraction_checkpoint.initial_loaded = true
```

### Si el drenaje es inaceptablemente lento

Plan B explícito: **abortar el drenaje SL y pasar a CSV bulk**.

#### Triggers para aborto

- ETA estimado > 5 días reales (medido tras 24h de drenaje).
- Cliente demanda data en <48h.
- Service Layer throttle bajo (<50 req/min sostenido) que no se puede negociar con partner.
- SL caído >4h por día en promedio (inestabilidad de la plataforma).

#### Procedimiento de cambio a CSV

```
1. Pausar Azure Function del tenant.
2. Comunicación al cliente: explicar cambio + ETA con CSV.
3. Partner produce CSVs por canal disponible (ver §25):
     a. SAP Query Generator (UI estándar)
     b. HANA Studio / HANA Cockpit (si DBA accesible)
     c. API del cloud hosting (si se ofrece)
     d. Backup logical (con NDA)
4. CSVs validados localmente: encoding UTF-8, headers exactos, sin filas vacías.
5. Subida al portal admin DataBision o blob storage privado.
6. TRUNCATE raw.<tabla> para el tenant.
7. Ingest API carga CSV → raw.* con _source_modality='B', _ingested_via='CSV_BULK'.
8. Reconciliación: count CSV vs SAP, sums DocTotal, MAX(DocEntry) — todo debe coincidir.
9. Inicializar checkpoints:
     last_update_(date, ts_norm, key) = MAX triple en CSV
     last_create_(date, ts_norm, key) = MAX triple en CSV
     initial_loaded = true
10. Limpiar @DBI_SYNC_QUEUE de entradas INITIAL_LOAD pendientes (DELETE).
11. Activar TN/PostTN (si no estaba activo).
12. Reanudar Azure Function en modo INCREMENTAL.
```

#### Comparación SL vs CSV (decisión rápida)

| Indicador | SL drenaje | CSV bulk |
|---|---|---|
| Setup | Bajo (ya está deployado) | Medio (coordinar con partner) |
| Tiempo data lista | Días | Horas |
| Confiabilidad volumen | Variable | Alta |
| Dependencia partner | Baja | Alta |
| Auditabilidad | Alta (cada GET loguea) | Media (un dump no auditado fila a fila) |
| Costo Azure | Function hours | Storage + ingest API CPU |

Cuando ambos son posibles, **el factor decisivo es el tiempo prometido al cliente**. Hablar honesto: si la inicial dura 5 días por SL y la promesa de venta fue "en operación esta semana", planear CSV desde día 0.

---

## Apéndice G — Reconciliación escalonada

### Frecuencia y horario (estrategia escalonada — reemplaza horario fijo de 7d)

| Nivel | Cuándo | Lookback | Objetivo |
|---|---|---|---|
| **Hourly** | Cada hora 08:00–20:00 local | **Últimas 2 horas** | Caza cambios perdidos por TN en tiempo casi real, costo I/O mínimo |
| **Nightly** | 02:00 local | **Últimas 24 horas** | Barrido del día completo cuando SAP está ocioso |
| **Weekly** | Domingo 03:00 local | **Últimos 7 días** | Atrapa edits retroactivos de la semana |
| **Cierre de mes** | Día 1 del mes 03:00 local | **Últimos 7 días** (30 al cierre de año) | Ajustes contables al cierre |

Razón de NO usar 7d cada hora: ver §11.3. Resumido: I/O en SAP escala lineal con la ventana × frecuencia → 7d × 24/día satura la base por puro overhead innecesario.

### Qué chequea

Tres dimensiones contra Azure SQL (vía ingest API endpoint `/api/recon/{table}?from=...&to=...`). Comparaciones de `UpdateTS`/`CreateTS` siempre se hacen sobre la versión **normalizada `HHMMSS` con LPAD** (ver §24.1):

```
PARA cada ObjectType (13,14,17,15,2,4):
  count_sap   = SELECT COUNT(*) FROM <tabla>
                WHERE UpdateDate IN [from,to] OR CreateDate IN [from,to]
  count_azure = GET /api/recon/{table}?from=:from&to=:to → COUNT raw.<tabla>
  if abs(count_sap - count_azure) > tolerance:
      LogAndAlert("RECON_COUNT_MISMATCH", object_type, count_sap, count_azure)
      // El SP de reconciliación ya encoló los faltantes; este alert es informativo.

  // Sumas (solo OINV, ORIN, ORDR, ODLN):
  sum_sap   = SELECT SUM(DocTotal) FROM <tabla> WHERE UpdateDate IN [from,to]
  sum_azure = GET /api/recon/{table}/sum?from=:from&to=:to
  if abs(sum_sap - sum_azure) > 0.01:
      LogAndAlert("RECON_SUM_MISMATCH", object_type, sum_sap, sum_azure)

  // MAX(DocEntry) y MAX(UpdateDate, UpdateTS_Norm):
  max_de_sap   = SELECT MAX(DocEntry) FROM <tabla> WHERE CreateDate IN [from,to]
  max_de_azure = GET /api/recon/{table}/max_de?from=:from&to=:to
  if max_de_sap > max_de_azure:
      LogAndAlert("RECON_MAX_GAP", object_type, max_de_sap, max_de_azure)

  max_upd_sap   = SELECT MAX("UpdateDate"),
                         MAX(CASE WHEN "UpdateDate" = MAX("UpdateDate")
                                  THEN LPAD("UpdateTS", 6, '0') END)
                  FROM <tabla> WHERE UpdateDate IN [from,to]
  max_upd_azure = GET /api/recon/{table}/max_update?from=:from&to=:to
  if (max_upd_sap.date, max_upd_sap.ts_norm) >
     (max_upd_azure.date, max_upd_azure.ts_norm):
      LogAndAlert("RECON_UPDATE_GAP", object_type)

PERSISTE A ops.recon_log:
  - tenant, table, date_from, date_to, count_sap, count_azure,
    sum_sap, sum_azure, max_de_sap, max_de_azure,
    max_upd_sap_date, max_upd_sap_ts_norm,
    max_upd_azure_date, max_upd_azure_ts_norm,
    level (HOURLY|NIGHTLY|WEEKLY|MONTHLY), status
```

### Reproceso quirúrgico

- Si reconciliación detecta gap: el SP ya encoló los DocEntries faltantes en `@DBI_SYNC_QUEUE` con `U_Priority = 10`.
- El conector procesa esos faltantes con prioridad (orden FIFO dentro de la prioridad).
- No es necesario reproceso full.

### Alertas

- 1 día con count mismatch: P3 (informativo).
- 3 días consecutivos: P2 (algo sistemático).
- Mismatch >5% en un día: P1 (revisar TN/PostTN).

---

## Apéndice H — Reintentar ERROR y liberar PROCESSING vencidos

### Reintentar ERROR

Las entries en `U_Status = 'PENDING'` con `U_NextAttemptAt > now` ya están auto-gestionadas (no son pickeadas hasta llegar la hora). Las entries en `U_Status = 'ERROR'` (transitorio, no terminal) se vuelven a PENDING automáticamente al final del `HandleError` (§16).

**No hay job manual de reintento.** Está implícito en el pickup.

### Liberar PROCESSING vencidos (zombis)

Ejecutado al inicio de cada corrida del conector (antes del pickup):

```
ReleaseZombies(tenant, threshold_min=30):
  GET pendientes con U_Status='PROCESSING' AND U_ProcessingSince < now - 30min
  FOR cada zombi:
    PATCH a PENDING:
      U_Status='PENDING'
      U_ProcessingSince=null
      U_LastError='auto-released: stuck >30min'
  log_metric("zombies_released", count)
```

### Umbral configurable

- Default 30 min.
- Aumentar si el cliente tiene SL lento y procesamientos legítimos de >30 min (caso extremo).
- Bajar a 15 min si los runs son rápidos y queremos recovery más agresivo.

### Acción manual ante zombis repetitivos

Si la misma entry es liberada 3 veces:

1. El conector la marca `ERROR_FINAL` con razón `'repeatedly_stuck'`.
2. Se mueve a LOG.
3. Alerta a operaciones para análisis manual.

---

## Glosario

- **Service Layer (SL):** API REST/OData de SAP B1 (puerto 50000 default).
- **UDT (User-Defined Table):** tabla custom dentro de SAP B1, prefijada con `@` en el storage.
- **UDF (User-Defined Field):** campo custom en tabla estándar o UDT, prefijado con `U_`.
- **TN (`TransactionNotification`):** SP de SAP B1 invocado dentro de cada transacción de negocio. Síncrono, bloqueante.
- **PostTN (`PostTransactionNotice`):** variante asíncrona post-commit. Preferida sobre TN.
- **FMS (Formatted Search):** mecanismo SAP B1 para disparar SQL al cambiar un campo de UI. Cobertura incompleta.
- **`@DBI_SYNC_QUEUE`:** cola de cambios pendientes.
- **`@DBI_SYNC_LOG`:** histórico de procesados.
- **DocEntry:** PK interna de documentos.
- **Zombi:** entry pegada en PROCESSING porque el conector murió antes de finalizar.
- **Claim atómico:** patrón PATCH+ETag para tomar entries sin race condition.
- **Coalescing:** colapsar múltiples entries para el mismo objeto en una sola operación.
- **Rate bucket:** token bucket que limita el throughput de requests a Service Layer.
- **Drenaje:** vaciar la cola de pendientes existentes.
- **Leader lock:** mecanismo para garantizar una sola instancia activa por tenant.
