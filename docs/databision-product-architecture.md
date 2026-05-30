# DataBision — Product Architecture (Extracción SAP B1 → Azure SQL)

Status: **Solo diseño — sin implementación.**
Foco de esta fase: **extracción, sincronización y base intermedia** desde SAP Business One hacia Azure SQL.

> **⚠️ PARCIALMENTE SUPERSEDED (2026-05-29):** La arquitectura de extracción (Modalidades A y B), los objetos SAP MVP, los patrones de watermark/lookback, y la estructura raw/stg/ctl/audit son válidos y se mantienen. Lo que cambió: (1) Azure SQL reemplazado por Supabase PostgreSQL — ver [ADR-001](adr/ADR-001-database-engine.md); (2) Una DB por tenant reemplazado por instancia compartida + company_id — ver [ADR-003](adr/ADR-003-multitenancy.md); (3) T-SQL MERGE reemplazado por PostgreSQL INSERT ON CONFLICT. Ver [master-architecture.md](master-architecture.md) como fuente de verdad.
Power BI aparece solo como destino futuro. Embedded, tokens, RLS y frontend de reportes están explícitamente fuera de alcance.

---

## 1. Visión del producto

DataBision es una **plataforma de reportería SaaS multiempresa para clientes de SAP Business One**, que entrega:

- Portal multiempresa con branding por tenant.
- Reportes ejecutivos (ventas, cobranza, inventario, márgenes) sin depender de Crystal Reports ni del cliente SAP B1.
- **Una base intermedia (Azure SQL) por tenant, sincronizada desde SAP B1 cada 30–60 min**, que alimenta dashboards e integraciones.
- Dos modalidades oficiales de extracción para cubrir el universo de instalaciones SAP B1 (on-prem dedicado vs cloud/restringido).

El valor real no es el visualizador: es la **base intermedia limpia, multiempresa, auditable y consultable** que cada cliente nunca antes tuvo fuera de su ERP.

---

## 2. Problema que resuelve

Realidades del cliente SAP B1:

- **Crystal Reports es legado**, lento y no se puede compartir fuera del cliente SAP B1 sin licencias adicionales.
- **No hay analítica multi-empresa.** Holdings con 3 sociedades SAP B1 no pueden consolidar sin Excel.
- **Acceso de gerencia al ERP es caro** (licencias profesional/CRM).
- **Cualquier cambio en reporte requiere consultor SAP** que cobra por hora.
- **No hay BI moderno** (sin filtros interactivos, sin drill-down, sin móvil, sin colaboración).
- **Datos viven encerrados** en HANA/SQL Server detrás del cliente SAP.

DataBision resuelve esto entregando:

1. Una **base intermedia replicada y enriquecida** (Azure SQL) por cliente.
2. Acceso web por subdominio con roles y permisos granulares.
3. Reportes preconstruidos sobre el modelo SAP B1 estándar.
4. Auditoría de quién vio qué y cuándo.
5. Onboarding repetible sin depender del consultor SAP de cabecera.

---

## 3. Arquitectura general

```
┌──────────────────────────────────────────────────────────────────────┐
│  CLIENTE SAP B1                                                       │
│  ┌─────────────────────────┐    ┌──────────────────────────────┐      │
│  │ SAP B1 HANA (on-prem)   │    │ SAP B1 (cloud/restringido)   │      │
│  │ + Extractor (Modalidad A)│    │ + UDT Queue (Modalidad B)    │      │
│  └────────────┬────────────┘    └──────────────┬───────────────┘      │
└───────────────┼─────────────────────────────────┼──────────────────────┘
                │ (HTTPS push)                    │ (Service Layer pull)
                ▼                                 ▼
┌──────────────────────────────────────────────────────────────────────┐
│  AZURE                                                                 │
│  ┌───────────────────┐   ┌──────────────────┐   ┌─────────────────┐   │
│  │ DataBision Ingest │   │  Azure Function  │   │  Azure Key      │   │
│  │ API (.NET 8)      │   │  (Modalidad B)   │   │  Vault          │   │
│  └─────────┬─────────┘   └────────┬─────────┘   └────────┬────────┘   │
│            │                       │                       │            │
│            └───────────┬───────────┘                       │            │
│                        ▼                                   │            │
│            ┌──────────────────────────┐                    │            │
│            │  Azure SQL — RAW         │ ← credenciales ────┘            │
│            │  (esquema espejo SAP B1) │                                 │
│            └────────────┬─────────────┘                                 │
│                         ▼                                               │
│            ┌──────────────────────────┐                                 │
│            │  Azure SQL — STAGING     │                                 │
│            │  (typed, denormalizado)  │                                 │
│            └────────────┬─────────────┘                                 │
│                         ▼                                               │
│            ┌──────────────────────────┐                                 │
│            │  Azure SQL — CURATED     │                                 │
│            │  (star schema, RLS-ready)│                                 │
│            └────────────┬─────────────┘                                 │
│                         ▼                                               │
│            ┌──────────────────────────┐                                 │
│            │  Power BI (FUTURO)       │                                 │
│            └──────────────────────────┘                                 │
└──────────────────────────────────────────────────────────────────────┘
```

Aislamiento por tenant: **una base de datos Azure SQL por cliente** (no schemas compartidos en MVP). Misma estructura, distinto contenido. Cualquier query siempre lleva `company_id` como cinturón + tirantes.

---

## 4. Modalidad A — Dedicated Extractor

### Cuándo aplica

- Cliente con SAP B1 HANA **on-prem o IaaS dedicado**.
- Tiene IT propio o partner SAP que permite instalar un servicio Windows / contenedor Docker.
- Quiere extracción de alta velocidad para cargas históricas grandes.

### Topología

```
[SAP HANA :30015]
     │ HANA SQL (Sap.Data.Hana.Core o ODBC)
     ▼
[Windows Service / .NET Worker Service en infra cliente]
     │  - schedule cada 30/60 min
     │  - SQLite local (checkpoints, último watermark, retries)
     │  - log local (diario rotado)
     │
     │ HTTPS POST (mTLS o API key)
     ▼
[DataBision Ingest API en Azure]
     │
     ▼
[Azure SQL — RAW del tenant]
```

### Componentes

| Componente | Tecnología | Notas |
|---|---|---|
| Driver HANA | `Sap.Data.Hana.Core` (NuGet, requiere licencia SAP) o System.Data.Odbc | ODBC más portable; nativo más rápido |
| Host | Windows Service o `Microsoft.Extensions.Hosting.WindowsServices` | Container opcional si IT permite Docker |
| Scheduler | Cron interno (`IHostedService` + `PeriodicTimer`) | No depender de Task Scheduler de Windows |
| Estado local | SQLite (`Microsoft.Data.Sqlite`) | Checkpoints + cola de reintentos offline |
| Logs | Serilog → archivo rotado diario + sink HTTP a DataBision | Cliente conserva copia local |
| Transporte | HTTPS POST con compresión gzip, batches de 500–5000 filas | API key + TLS; mTLS opcional para enterprise |
| Identidad | API key emitida por DataBision al onboarding | Almacenada en DPAPI o en variable de entorno |

### Responsabilidades del extractor

1. Leer checkpoint local: `last_watermark` por tabla.
2. Ejecutar query incremental contra HANA (ver §9).
3. Paginar resultados (5000 filas / batch) para no saturar memoria.
4. Comprimir batch y postear a `POST /api/ingest/{company}/{table}`.
5. Reintentar 3 veces con backoff exponencial; si falla, persistir batch en cola SQLite y reintentar en próxima ejecución.
6. Reportar heartbeat a `POST /api/ingest/heartbeat` cada 5 min.
7. Actualizar `last_watermark` solo después de recibir 2xx del API.

### Ventajas

- Velocidad máxima (HANA SQL bulk).
- Funciona offline-tolerante (cola local).
- Histórico grande (millones de filas) viable.
- Cliente conserva auditoría local.

### Desventajas

- Requiere instalación en infra del cliente.
- Mantenimiento de actualizaciones del agente (versión rota schema).
- Driver HANA puede requerir licencia SAP en el cliente.

---

## 5. Modalidad B — Cloud / Restricted Connector

### Cuándo aplica

- Cliente con SAP B1 en partner cloud / hosting compartido donde **no se puede instalar servicios**.
- Service Layer está habilitado y accesible (puerto 50000 HTTPS).
- Volumen razonable (cambios diarios, no históricos masivos cada vez).

### Topología

```
[SAP B1 Cloud]
     │
     │  ┌─────────────────────────────────┐
     │  │ UDT @DBI_SYNC_QUEUE             │ ← poblada por Formatted Search,
     │  │ UDT @DBI_SYNC_LOG               │   SDK trigger o stored proc
     │  └─────────────────────────────────┘
     │
     │ Service Layer (OData/REST)
     ▼
[Azure Function (TimerTrigger cada 5–15 min)]
     │  - lee cola: GET .../U_DBI_SYNC_QUEUE?$filter=U_Processed eq 'N'
     │  - por cada entrada: GET del objeto completo
     │  - marca procesado: PATCH al UDT
     │  - registra en @DBI_SYNC_LOG
     │
     ▼
[Azure SQL — RAW del tenant]
```

### Componentes

| Componente | Tecnología | Notas |
|---|---|---|
| Cola SAP | UDT `@DBI_SYNC_QUEUE` | Definida en §19 |
| Log SAP | UDT `@DBI_SYNC_LOG` | Histórico procesado |
| Disparador | Formatted Search (FMS) en campos clave + stored proc opcional | Sin necesidad de SDK compilado |
| Host | Azure Function (Consumption o Premium) | Timer trigger |
| Cliente HTTP | `HttpClient` con `B1SESSION` cookie + login Service Layer | Manejo de sesión expirada |
| Backoff | `Polly` retry + circuit breaker | Service Layer falla bajo carga |
| Identidad SL | Usuario SAP B1 dedicado `DBI_INTEGRATION` con rol mínimo de lectura | Password en Key Vault |

### Responsabilidades del connector

1. Login Service Layer → obtener `B1SESSION` + `ROUTEID`.
2. Leer `U_DBI_SYNC_QUEUE` con `$top=200`, ordenado por `U_CreatedAt`.
3. Para cada entrada: pedir objeto completo por su endpoint (`/Invoices(123)`, `/BusinessPartners('CARDCODE')`, etc.).
4. Postear al ingest API o escribir directo a RAW (mismo formato que Modalidad A).
5. Marcar entrada como procesada (PATCH) + insertar en `U_DBI_SYNC_LOG`.
6. Si falla la entrada: incrementar contador de reintentos en la cola; tras N fallos, mover a "error" para revisión.
7. Logout Service Layer.

### Ventajas

- Sin instalación en cliente.
- Footprint mínimo (solo 2 UDT + algunos FMS).
- Compatible con SAP B1 cloud restringido.
- Cambios near-real-time (5–15 min de lag).

### Desventajas

- **Service Layer es lento.** ~100–300 req/min sostenido. No sirve para histórico de millones de filas — la carga inicial se hace una sola vez con permiso del partner o se distribuye en días.
- Cliente debe mantener FMS / triggers (depende del partner SAP).
- Sin acceso directo a HANA → algunas queries complejas son imposibles.
- Service Layer rompe con upgrades menores de SAP B1.

---

## 6. Diferencias técnicas: HANA / ODBC / Service Layer / Queue Mode

| Dimensión | HANA SQL directo | ODBC (SQL Server) | Service Layer | Queue Mode (UDT) |
|---|---|---|---|---|
| Protocolo | SQL nativo (puerto 30015) | ODBC sobre TCP (puerto 1433) | HTTPS OData/REST (50000) | HTTPS sobre Service Layer + UDT |
| Latencia query | <50 ms | <100 ms | 200–2000 ms | 200–2000 ms |
| Throughput bulk | Excelente (millones/min) | Excelente | Pobre (~100–300/min) | Pobre, pero solo cambios |
| Acceso cloud SAP | No (puerto cerrado) | A veces | Sí | Sí |
| Necesita instalación local | Sí (extractor) | Sí (extractor) | No | UDT + FMS en SAP |
| Trabaja con SAP B1 HANA | Sí | No | Sí | Sí |
| Trabaja con SAP B1 SQL | No | Sí | Sí | Sí |
| Real-time | Polling | Polling | Polling | Near real-time |
| Maneja UDT y UDF | Sí (SQL directo) | Sí | Limitado (depende mapping) | Sí |
| Riesgo upgrade SAP | Bajo (queries SQL estables) | Bajo | Medio (endpoints cambian) | Medio |
| Costo licencias adicionales | Driver HANA puede requerirla | Driver ODBC nativo | Service Layer license del cliente | Service Layer license del cliente |

### Regla práctica

- **HANA SQL directo** si el cliente tiene HANA y permite extractor.
- **ODBC** si SAP B1 está sobre MS SQL Server.
- **Service Layer + Queue Mode** si el cliente está en cloud restringido o no permite instalación.
- **Service Layer puro (sin Queue)** evitarlo para cargas grandes; aceptable solo para casos de baja volumetría.

---

## 7. Cuándo usar cada modalidad

| Escenario cliente | Modalidad recomendada |
|---|---|
| SAP B1 HANA on-prem, IT propio, volumen alto (>500k tx/año) | **A** |
| SAP B1 HANA en partner cloud, sin acceso a puerto 30015 | **B** |
| SAP B1 SQL Server on-prem, IT propio | **A** (con driver ODBC) |
| SAP B1 SQL Server en partner cloud restringido | **B** |
| Cliente piloto, baja volumetría, sin IT | **B** |
| Cliente enterprise, quiere control total | **A** |
| Cliente quiere arrancar en <2 semanas | **B** si Service Layer ya está habilitado |
| Cliente con compliance estricto (datos no pueden salir del sitio en bulk) | **A** con egress controlado |

---

## 8. Carga inicial histórica

### Modalidad A

1. Acordar ventana de mantenimiento con cliente (1 vez, fuera de horario laboral).
2. Por cada tabla MVP, ejecutar dump completo paginado por `DocEntry` o `CardCode` (PK natural).
   - Page size: 5000 filas (configurable).
3. Hash de validación al final de cada página (count + suma de PKs) → posteado al API para validación.
4. Watermark inicial = `MAX(UpdateDate)` al cierre.
5. Validación: comparar `COUNT(*)` en RAW vs `COUNT(*)` en SAP por tabla.
6. Tiempos esperados: 1M filas OINV/INV1 en ~30 min con conexión LAN.

### Modalidad B

1. Service Layer no es viable para histórico masivo. Opciones:
   - **A.** Pedir al partner una exportación CSV/Excel inicial → cargar manualmente a RAW.
   - **B.** Drenar Service Layer en segundo plano durante días (acepta 1–3 semanas de carga inicial).
   - **C.** Si el cliente tiene réplica HANA con acceso temporal, usar Modalidad A solo para histórico.
2. Población inicial de `@DBI_SYNC_QUEUE` con todos los DocEntry existentes vía stored proc one-shot.
3. Connector drena la cola hasta vaciar.

### Validación de cierre

- Reporte por tabla: filas en RAW = filas en SAP ± 0.
- Sumas de monto en facturas: RAW = SAP ± 0.01 (tolerancia redondeo decimal).
- Top 10 customers por monto: lista coincide.
- Sign-off documentado por el customer admin.

---

## 9. Carga incremental cada 30/60 min

### Patrón base por tabla cabecera

```sql
SELECT *
FROM   OINV
WHERE  UpdateDate >= :last_watermark - INTERVAL :lookback HOUR
  AND  UpdateDate <  :now
ORDER  BY UpdateDate, DocEntry
```

### Patrón para líneas (INV1, RIN1)

Las tablas de líneas **no tienen `UpdateDate` confiable**. Estrategia:

```sql
SELECT L.*
FROM   INV1 L
JOIN   OINV H ON H.DocEntry = L.DocEntry
WHERE  H.UpdateDate >= :last_watermark - INTERVAL :lookback HOUR
  AND  H.UpdateDate <  :now
```

Es decir: cuando cambia la cabecera, traemos **todas** sus líneas y hacemos delete+insert por `DocEntry` en RAW para evitar líneas huérfanas.

### Frecuencia

- 30 min por defecto.
- 60 min para clientes de baja volumetría.
- 15 min para tablas críticas (OINV) si el cliente paga tier "near real-time".
- 24h para maestros pequeños (OSLP).

### Idempotencia

Toda extracción es idempotente: si el batch se reenvía, MERGE en RAW deduplica por PK (§12).

---

## 10. Diseño de checkpoints

### Tabla `etl.checkpoints` (en Azure SQL, replicada localmente en Modalidad A)

| Columna | Tipo | Notas |
|---|---|---|
| company_id | INT | FK del tenant |
| table_name | VARCHAR(64) | OINV, INV1, etc. |
| last_watermark | DATETIME2 | UpdateDate del último cambio procesado |
| last_run_at | DATETIME2 | UTC |
| last_run_status | VARCHAR(16) | SUCCESS / FAILED / PARTIAL |
| last_error_message | NVARCHAR(MAX) | NULL si OK |
| rows_in_last_run | INT | Telemetría |
| **PK** | (company_id, table_name) | |

### Reglas

- Solo se actualiza `last_watermark` después de **upsert exitoso**.
- Si falla a mitad: `last_watermark` permanece; próxima corrida reprocesa el rango (idempotente).
- En Modalidad A: replicado en SQLite local + sincronizado al cierre exitoso.
- En Modalidad B: solo en Azure SQL.

---

## 11. Diseño de lookback window

### Por qué existe

Usuarios SAP B1 editan documentos viejos. Una factura emitida el 2026-01-15 puede modificarse el 2026-05-15. Si solo usamos `UpdateDate > watermark`, no perdemos ese cambio (porque `UpdateDate` se actualiza). Pero hay casos:

- Cancelaciones que no actualizan `UpdateDate` en todas las tablas.
- Recálculos por job interno de SAP.
- Errores de reloj entre HANA y la app.

### Ventana

- **24 horas por defecto.** Cada corrida: `UpdateDate >= last_watermark - 24h`.
- **7 días al cierre de mes** (configurable): captura ajustes contables retroactivos.
- **30 días** para tablas críticas de cobranza por seguridad.

### Costo

Lookback aumenta el volumen leído pero el MERGE en RAW deduplica. Costo neto: queries de incremental son ~2× el delta real. Aceptable.

---

## 12. Diseño de upsert hacia Azure SQL

### Patrón

1. Cliente envía batch JSON al API (gzip).
2. API valida JWT, tenant, esquema básico.
3. API hace `BULK INSERT` a tabla temporal `#tmp_OINV`.
4. `MERGE` desde `#tmp` a `raw.OINV`:

```sql
MERGE raw.OINV AS tgt
USING #tmp_OINV AS src
   ON tgt._company_id = src._company_id
  AND tgt.DocEntry    = src.DocEntry
WHEN MATCHED AND src.UpdateDate >= tgt.UpdateDate THEN
   UPDATE SET ...,
              _ingested_at = SYSUTCDATETIME()
WHEN NOT MATCHED BY TARGET THEN
   INSERT (...)
   VALUES (...);
```

5. Confirmar transacción.
6. Registrar en `etl.run_log`.

### Reglas

- **MERGE solo si `src.UpdateDate >= tgt.UpdateDate`** — evita revertir un cambio reciente con uno viejo (orden de batch).
- **No DELETE** en RAW — registros borrados en SAP se marcan en columna `_is_deleted` (cuando la modalidad lo permite detectar; ver §20).
- Tablas de líneas (INV1, RIN1): borrar todas las líneas del DocEntry afectado e insertar las nuevas, dentro de una transacción.
- Columnas técnicas en cada tabla RAW: `_company_id`, `_ingested_at`, `_source_modality` ('A' o 'B'), `_batch_id`.

### Performance

- Índice clusterizado en `(_company_id, NaturalPK)`.
- Índice non-clustered en `UpdateDate` para queries incrementales.
- Batches de 500–5000 filas; benchmark elige óptimo por tabla.

---

## 13. Diseño Raw / Staging / Curated

### Raw

- **Espejo de SAP B1.** Mismos nombres de columna, mismos tipos.
- Decisiones: NULLs respetados, fechas en `DATETIME2`, decimales en `DECIMAL(19,6)`.
- Columnas técnicas: `_company_id`, `_ingested_at`, `_source_modality`, `_batch_id`, `_is_deleted` (default 0).
- Sin transformaciones.
- Útil para debugging y reconciliación contra SAP.

### Staging

- **Tipos limpios, joins resueltos, denormalización mínima.**
- Vistas materializadas (o tablas refrescadas vía job nocturno + delta).
- Ejemplos:
  - `stg.invoice_header` — OINV + flags derivados (canceled, paid, overdue).
  - `stg.invoice_line` — INV1 + nombre de item, categoría, costo.
  - `stg.customer` — OCRD limpio, con flags (active, blocked, group).
- Reglas de calidad: rechazar filas con `CardCode` NULL en facturas, etc., y loguear en `etl.quality_issues`.

### Curated

- **Star schema** para BI.
- `dim_company`, `dim_customer`, `dim_item`, `dim_salesperson`, `dim_date`.
- `fact_sales`, `fact_credit_memos`.
- Diseñado para servir directamente a Power BI con RLS por `company_id`.
- En MVP: **opcional**. Cliente piloto puede consumir staging directo. Curated llega en fase post-MVP.

### Refresco

| Capa | Frecuencia |
|---|---|
| Raw | Continuo (cada batch incremental) |
| Staging | Cada hora (job SQL Agent / Azure Function) |
| Curated | Nocturno (3 AM tenant TZ) |

---

## 14. Cómo Azure SQL alimentará Power BI (FUTURO)

Esta sección es solo orientación — **no se implementa en esta fase**.

### Conexión

- **Import mode** para Curated (mejor performance, requiere refresh schedulado).
- **DirectQuery** para Staging si el cliente quiere "casi-real-time" en tablas chicas.
- **Composite model** para combinar ambos.

### RLS

- En Power BI: filtro DAX `[CompanySlug] = USERPRINCIPALNAME()` sobre `dim_company`.
- En Azure SQL: opcionalmente Row-Level Security nativo de SQL Server como defensa en profundidad.

### Gateway

- En MVP futuro: no se necesita gateway (Azure SQL es PaaS público).
- Conexión por Service Principal + AAD auth de Power BI a Azure SQL.

### Cronograma

- No antes de tener los dos clientes piloto con datos limpios en Azure SQL.
- Estimado: meses 4–6 después del primer cliente en producción.

---

## 15. Logs y monitoreo

### Niveles

| Nivel | Qué se registra | Storage |
|---|---|---|
| **Run log** | Una fila por (run_id, table, company) con start/end/rows/status | `etl.run_log` en Azure SQL |
| **Batch log** | Una fila por batch HTTP recibido (id, size, rows, hash) | `etl.batch_log` |
| **Row error** | Filas que fallaron upsert con payload y error | `etl.row_error` |
| **Quality issue** | Filas que pasaron upsert pero violan reglas semánticas | `etl.quality_issue` |
| **Heartbeat** | Ping cada 5 min del extractor (modalidad A) | `etl.heartbeat` (TTL 30 días) |
| **Audit BI** | Quién consultó qué reporte (cuando llegue Power BI) | `audit.bi_event` |

### Alertas

- Heartbeat ausente >30 min → alerta a equipo de soporte.
- 3 runs consecutivos `FAILED` para misma tabla/cliente → alerta P1.
- Lag de incremental > 4h → alerta P2.
- `row_error` rate >1% → alerta a data ops.
- Service Layer 401/403 → alerta inmediata (credencial expirada).

### Dashboards (operación interna)

- Por cliente: última sincronización por tabla, rows extraídas hoy, errores.
- Global: heatmap de salud de clientes, top fallos, latencias.
- Tools: Azure Monitor + Application Insights + tabla custom en Azure SQL.

---

## 16. Seguridad de credenciales

### Credenciales en juego

| Credencial | Modalidad | Dónde vive |
|---|---|---|
| Password HANA del usuario lector | A | Servidor del cliente, DPAPI o env var, nunca en código |
| API key del extractor → DataBision | A | Servidor del cliente, DPAPI |
| User SAP B1 Service Layer (`DBI_INTEGRATION`) | B | Azure Key Vault |
| Connection string Azure SQL | Ambos | Azure Key Vault + Managed Identity |
| Certificados mTLS (si se usan) | A | Cert store de Windows |

### Reglas

- **Nada en código, nada en repos, nada en logs.**
- Rotación trimestral mínima.
- Auditoría de acceso a Key Vault habilitada.
- Usuario SAP B1 con **solo permisos de lectura** sobre las tablas MVP — nunca administrador.
- Aislamiento por cliente: un Key Vault por tenant (ver §17).
- API keys del extractor: revocables individualmente, una por tenant.

### Transporte

- HTTPS obligatorio, TLS 1.2 mínimo.
- Compresión gzip de batches.
- Validación de origen vía API key + tenant claim.

---

## 17. Azure Key Vault

### Estrategia: un Key Vault por tenant

| Pro | Contra |
|---|---|
| Aislamiento contractual claro | Más vaults para administrar |
| Permite revocar acceso por tenant sin tocar otros | Costo: ~$0.03/operación, despreciable |
| Cumplimiento (datos por cliente físicamente separados a nivel secreto) | Onboarding requiere paso extra |

Naming: `kv-databision-{slug}` (ej. `kv-databision-acme`).

### Secretos por tenant

```
kv-databision-acme/
  sap-sl-username
  sap-sl-password
  sap-hana-password           (solo si Modalidad A; el extractor vive en cliente y no accede al vault)
  azure-sql-connection
  extractor-api-key
  webhook-signing-key
```

### Identidad de acceso

- **Azure Function (Modalidad B):** Managed Identity con rol `Key Vault Secrets User` sobre el vault del tenant que está procesando.
- **DataBision API:** Managed Identity para leer connection strings de Azure SQL por tenant.
- **Extractor (Modalidad A):** NO accede al Key Vault. Solo conoce su API key y password HANA, almacenados localmente en DPAPI.

### Rotación

- API keys: rotables vía endpoint admin (revoca vieja, emite nueva).
- Service Layer password: rotada por partner SAP, actualizada en Key Vault.
- Azure SQL: managed identity preferida; si password, rotación automática 90 días.

---

## 18. Tablas SAP MVP

| Tabla | Significado | Frecuencia incremental sugerida | Lookback sugerido |
|---|---|---|---|
| **OCRD** | Business Partners (clientes y proveedores) | 1h | 24h |
| **OITM** | Items / artículos | 1h | 24h |
| **OSLP** | Salespersons | 24h | 7d (tabla chica, full refresh viable) |
| **OINV** | A/R Invoices header | 30min | 7d (al cierre de mes 30d) |
| **INV1** | A/R Invoice lines | Junto a OINV | Vía OINV |
| **ORIN** | A/R Credit memos header | 30min | 7d |
| **RIN1** | A/R Credit memo lines | Junto a ORIN | Vía ORIN |

### Columnas críticas por tabla

- **OCRD**: `CardCode` (PK), `CardName`, `CardType` (C/S/L), `GroupCode`, `Balance`, `frozenFor`, `validFor`, `UpdateDate`.
- **OITM**: `ItemCode` (PK), `ItemName`, `ItmsGrpCod`, `OnHand`, `IsCommited`, `ManSerNum`, `UpdateDate`.
- **OSLP**: `SlpCode` (PK), `SlpName`, `Active`.
- **OINV**: `DocEntry` (PK), `DocNum`, `CardCode`, `DocDate`, `DocDueDate`, `DocTotal`, `DocStatus`, `Canceled`, `SlpCode`, `UpdateDate`.
- **INV1**: `DocEntry` + `LineNum` (PK compuesta), `ItemCode`, `Quantity`, `Price`, `LineTotal`, `WhsCode`, `VatGroup`.
- **ORIN** / **RIN1**: estructura espejo de OINV/INV1 para notas de crédito.

### Pitfalls

- **Multi-moneda:** todos los montos tienen variantes FC (foreign currency), SC (system currency), LC (local currency). MVP usa LC; preservar SC para reportes consolidados futuros.
- **Cancelados:** `Canceled = 'Y'` o `DocStatus = 'C'`. No borrar del staging, marcar.
- **Borrados físicos:** SAP B1 rara vez borra; cuando sí, no hay forma de detectar sin polling completo. Aceptable en MVP.
- **UpdateDate granularidad:** segundo, no milisegundo. Riesgo de "perder" cambios concurrentes — el lookback de 24h lo cubre.
- **Tax detail (INV4/INV5):** fuera de MVP; agregar en fase 2 para reportes de IVA.

---

## 19. Objetos SAP Queue Mode (Modalidad B)

### `@DBI_SYNC_QUEUE` (UDT)

| Campo | Tipo | Descripción |
|---|---|---|
| Code | varchar(50) | PK auto-incremental SAP B1 |
| Name | varchar(100) | Descripción libre |
| U_ObjectType | varchar(20) | `OCRD`, `OITM`, `OINV`, `ORIN`, etc. |
| U_ObjectKey | varchar(50) | `CardCode` o `DocEntry` según el tipo |
| U_Action | char(1) | `I` insert, `U` update, `D` delete |
| U_CreatedAt | datetime | Cuando SAP encoló |
| U_Processed | char(1) | `N` pendiente, `Y` procesado, `E` error |
| U_ProcessedAt | datetime | Cuando connector procesó |
| U_RetryCount | int | Reintentos |
| U_LastError | varchar(500) | Mensaje del último fallo |

### `@DBI_SYNC_LOG` (UDT)

| Campo | Tipo | Descripción |
|---|---|---|
| Code | varchar(50) | PK |
| U_ObjectType | varchar(20) | |
| U_ObjectKey | varchar(50) | |
| U_Action | char(1) | |
| U_QueuedAt | datetime | Original `U_CreatedAt` de la cola |
| U_ProcessedAt | datetime | |
| U_Status | varchar(10) | `OK` / `FAILED` |
| U_DurationMs | int | Latencia procesamiento |
| U_ConnectorVersion | varchar(20) | Telemetría versionado |

### Población de la cola

Tres opciones, en orden de preferencia:

1. **Formatted Search (FMS)** sobre campos clave de cada tabla observada: cada vez que el usuario guarda, dispara INSERT a `@DBI_SYNC_QUEUE`. Cero código, todo en SAP B1 cliente. Limitación: solo dispara con cambios desde UI.
2. **Stored procedure / trigger SAP** sobre la tabla. Requiere acceso a HANA/SQL del cliente — no siempre disponible en cloud restringido.
3. **Polling diferencial periódico**: query a `SELECT MAX(UpdateDate) FROM OINV WHERE UpdateDate > ?` y poblar cola manualmente. Solo si las dos anteriores no son posibles.

### Reglas del connector

- Procesar en orden por `U_CreatedAt`.
- Idempotente: si una entry se procesa dos veces, el MERGE en RAW lo absorbe.
- Después de N reintentos (default 5), marcar `U_Processed = 'E'` y alertar.
- Limpieza: entries con `U_Processed = 'Y'` > 30 días → mover a `@DBI_SYNC_LOG`, borrar de cola.

---

## 20. Riesgos técnicos

| Riesgo | Severidad | Mitigación |
|---|---|---|
| **Driver HANA requiere licencia SAP** | Alta | Validar con cliente antes de comprometer Modalidad A; ODBC como fallback |
| Service Layer cae bajo carga | Alta | Polly retry + circuit breaker; backoff agresivo; alertas |
| `UpdateDate` no se actualiza en cancelaciones parciales | Media | Lookback 24h + reconciliación nocturna por count |
| Borrados físicos invisibles | Media | Aceptado en MVP; reconciliación nocturna por count detecta divergencias |
| Cliente upgrade SAP B1 rompe queries | Media | Versionado de queries por release de SAP; tests automatizados con dataset sintético |
| Cliente upgrade SAP rompe Service Layer endpoints | Media | Mismo |
| Multi-tenant SAP B1 (varias sociedades por cliente) | Media | Una conexión por sociedad; `company_id` de DataBision ≠ sociedad SAP, hay que mapear |
| Reloj del servidor SAP desfasado vs Azure | Baja | Lookback absorbe pequeños desfases; alerta si >5min |
| Volumen explosivo (cliente migra desde otro ERP, inserta 10M filas) | Media | Detección por delta diario; pausar ingest si supera umbral; notificar |
| Líneas huérfanas tras cambio de cabecera | Media | Delete-then-insert dentro de transacción por DocEntry |
| Encoding SAP B1 vs SQL (Latin1 vs UTF8) | Media | Normalizar a UTF-8 en API antes del MERGE |
| Cola `@DBI_SYNC_QUEUE` crece sin freno si connector cae | Alta | Alerta cuando size > 50k filas; truncar log antiguo nocturnamente |
| Service Layer requiere relogin cada N requests | Baja | Manejo de `401` con relogin automático |
| Azure SQL alcanza límites de DTU/vCore en cliente grande | Media | Plan de scaling; monitor desde día 1 |
| Egress de bandwidth desde cliente on-prem a Azure | Baja | gzip + batches grandes; estimación pre-onboarding |

---

## 21. Riesgos comerciales

| Riesgo | Severidad | Mitigación |
|---|---|---|
| **Partner SAP del cliente bloquea instalación de extractor** | Alta | Modalidad B como plan B siempre; relación directa con partner |
| Cliente exige due diligence de seguridad de varias semanas | Alta | Documentación de seguridad lista (SOC2-lite, pen-test report) |
| Cliente teme que "alguien lea su data" | Alta | Aislamiento por tenant explícito en contrato; logs auditables |
| Cliente no tiene IT para mantener extractor | Media | Ofrecer Modalidad B siempre que sea técnicamente viable |
| Cliente no quiere pagar Service Layer license | Media | Validar antes de cerrar contrato |
| Cambio de partner SAP del cliente rompe la integración | Media | Documentar todo lo que toca de SAP (UDT, FMS, usuario) |
| Cliente quiere features "ya" (multi-currency, consolidación, IA) | Media | Roadmap explícito y firmado; no prometer fuera de MVP |
| Pricing no cubre costo de infra del cliente grande | Media | Pricing por filas/mes o por tenant; calculadora antes de cerrar |
| Cliente comparte instancia SAP entre varias empresas | Media | Modelo soporta varias sociedades; pricing las contempla |
| Soporte 24/7 esperado en SLA | Alta | SLA explícito 8×5 en MVP; 24×7 con uplift |
| Cliente quiere on-prem (no en Azure) | Alta | Fuera de alcance; rechazar o cobrar muy caro |
| Pérdida de datos por bug del extractor → cliente pierde confianza | Crítica | Tests, reconciliación, runbook, comunicación proactiva ante errores |
| Cliente desinstala extractor por desconfianza | Alta | Transparencia: documentación de qué lee, logs locales accesibles al cliente |
| Competencia local (consultoras SAP) copia el modelo | Media | Diferenciar por velocidad de onboarding y calidad de UX, no por arquitectura |

---

## 22. Roadmap para los dos clientes iniciales

### Cliente 1 — Modalidad A (HANA dedicado)

| Semana | Hito |
|---|---|
| 0 | Kickoff + recolección de info: versión SAP B1, sociedades, volumen estimado, ventana mantenimiento |
| 1 | Provisionar Azure SQL del tenant + Key Vault + acceso de red al cliente |
| 1 | Instalar extractor en infra del cliente; verificar conectividad HANA |
| 2 | Carga inicial histórica de 7 tablas MVP; reconciliación |
| 2 | Activar incremental 30 min; monitor 72h |
| 3 | Sign-off de calidad de datos por customer admin |
| 3 | Acceso al portal DataBision para usuarios del cliente (sin Power BI todavía — vistas SQL servidas por API) |
| 4 | Documentación de runbook entregada al cliente |
| 4 | Cliente 1 en producción "MVP" |

### Cliente 2 — Modalidad B (cloud / restringido)

| Semana | Hito |
|---|---|
| 0 | Kickoff + validación de: Service Layer accesible, partner permite UDT + FMS, usuario `DBI_INTEGRATION` creable |
| 1 | Provisionar Azure SQL + Key Vault + Azure Function (en mismo grupo de recursos por tenant) |
| 1 | Partner crea UDT `@DBI_SYNC_QUEUE`, `@DBI_SYNC_LOG`, FMS triggers |
| 2 | Carga inicial (por opción de §8: probablemente CSV inicial + drenaje Service Layer) |
| 3 | Activar connector cada 15 min; monitor 72h |
| 3 | Reconciliación; sign-off de calidad |
| 4 | Acceso al portal DataBision |
| 5 | Documentación + cliente 2 en producción "MVP" |

### Aprendizaje cruzado

- **Después del cliente 1:** review interno de qué se subestimó, ajustar timelines del cliente 2.
- **Después del cliente 2:** comparar costo total de onboarding (horas + Azure) → calibrar pricing.

---

## 23. MVP ejecutable

### Backend (.NET 8)

- `DataBision.Ingest.Api`: nuevo proyecto o área en API existente. Endpoints:
  - `POST /api/ingest/{tenant}/{table}` — recibe batch
  - `POST /api/ingest/heartbeat` — recibe ping del extractor
  - `GET /api/ingest/status/{tenant}` — admin: checkpoints + last run
- `DataBision.Ingest.Service`: clase que toma JSON, valida, hace BULK INSERT a temp, MERGE.
- Tabla `etl.*` en Azure SQL (checkpoints, run_log, batch_log, row_error, quality_issue, heartbeat).
- Tablas `raw.*` por tenant (clonadas de SAP B1).

### Modalidad A — extractor

- Proyecto `DataBision.Extractor` (Worker Service .NET 8).
- Configuración por `appsettings.json` (HANA conn, API URL, API key, tablas activas, schedule).
- Instalable como Windows Service: `sc create DataBisionExtractor binPath="..."`.
- Empaquetado como single-file con `dotnet publish -p:PublishSingleFile=true`.
- Distribución: zip + instalador básico documentado en runbook.

### Modalidad B — connector

- Proyecto `DataBision.Connector.Function` (Azure Function, .NET 8 isolated).
- Timer trigger cada 15 min (configurable).
- Reads `@DBI_SYNC_QUEUE` vía Service Layer → fetch objects → POST al Ingest API → PATCH cola.
- Una function app por tenant (aislamiento + facturación).

### Compartido

- `DataBision.Etl.Shared`: contratos DTO de batch, helpers de hashing, types comunes.

### Lo que NO va en el MVP

Ver §24.

### Estimación de esfuerzo (rough)

- 1 ingeniero senior backend: 4 semanas para Modalidad A end-to-end.
- 1 ingeniero senior backend: 3 semanas para Modalidad B (asume conocimiento Service Layer previo).
- 1 ingeniero data: 2 semanas para diseño/implementación capa staging.
- Total: ~6–8 semanas para los dos modos antes de cliente 1.

---

## 24. Qué NO construir todavía

Mantener fuera de alcance hasta tener los 2 clientes iniciales en producción:

- ❌ Capa Curated (star schema). Suficiente con RAW + Staging.
- ❌ Power BI Embedded. Suficiente con vistas servidas por API + frontend simple.
- ❌ Service Principal Power BI, embed tokens, RLS Power BI.
- ❌ Azure Data Factory / Synapse / Fabric — el extractor + ingest API hace todo lo necesario.
- ❌ IA, OpenAI, Copilot.
- ❌ Multi-moneda consolidación.
- ❌ UDF / UDT del cliente más allá de lo definido para Queue Mode.
- ❌ Self-service onboarding (UI para que el cliente se conecte solo).
- ❌ Real-time CDC.
- ❌ DirectQuery contra SAP (siempre vía Azure SQL).
- ❌ Multi-región Azure.
- ❌ Disaster recovery cross-region.
- ❌ Soporte 24/7.
- ❌ Marketplace público de reportes.
- ❌ Tablas SAP fuera de las 7 MVP.
- ❌ Módulos: producción, RRHH, proyectos, intercompany.

Cada `❌` arriba es una conversación de "esto viene después, no en MVP" con sales y clientes.

---

## 25. Criterios de aceptación — vendible

El componente de extracción es **vendible** cuando se cumplen TODOS:

### Funcionales

1. ✅ Instalable en cliente Modalidad A en ≤1 día hábil (extractor + Azure SQL + Key Vault listos).
2. ✅ Connector Modalidad B desplegable en ≤2 días hábiles (asumiendo Service Layer + UDT autorizados por partner).
3. ✅ Carga histórica de 1M filas OINV/INV1 completa en ≤4 horas (Modalidad A).
4. ✅ Lag incremental observado ≤30 min en Modalidad A, ≤45 min en Modalidad B (p95).
5. ✅ Reconciliación con SAP B1 al cierre histórico: ±0 filas, ±0.01 monto.
6. ✅ Recuperación automática ante fallo de red sin pérdida de datos (idempotencia + cola local en A; reintento de cola en B).

### Operacionales

7. ✅ Heartbeat + alertas funcionando: falla detectada en <30 min.
8. ✅ Runbook documentado para los 5 errores más comunes.
9. ✅ Dashboard de salud por cliente accesible a soporte interno.
10. ✅ Logs auditables disponibles tanto en cliente (Modalidad A) como en Azure (ambas).
11. ✅ Rotación de credenciales documentada y probada al menos 1 vez.

### Seguridad

12. ✅ Cero credenciales en repo, código o logs (verificado por scan).
13. ✅ Cada tenant tiene su propio Azure SQL DB + Key Vault.
14. ✅ Conexiones HTTPS TLS 1.2 mínimo end-to-end.
15. ✅ Usuario SAP de integración con permisos mínimos (lectura sobre 7 tablas MVP), no admin.
16. ✅ Audit log persistente: quién accedió a qué secreto en Key Vault.

### Comerciales

17. ✅ Pricing por tenant calculado y validado con costo real de Azure ≤30% del precio.
18. ✅ SLA definido por escrito (8×5, recovery time, data freshness).
19. ✅ Contrato modelo con cláusulas de tratamiento de datos, retención, terminación.
20. ✅ Sales tiene una demo guiada que muestra extracción + dato fluyendo a Azure SQL + reporte simple.

### Clientes piloto

21. ✅ Cliente 1 (Modalidad A) en producción ≥30 días sin incidente crítico.
22. ✅ Cliente 2 (Modalidad B) en producción ≥30 días sin incidente crítico.
23. ✅ Sign-off escrito de los dos clientes piloto de calidad de datos.
24. ✅ Testimonial publicable de al menos un cliente.

Solo cuando **24/24 estén ✅**, el componente de extracción puede venderse a clientes nuevos sin riesgo reputacional.

---

## Glosario rápido

- **SAP B1**: SAP Business One, ERP para PyMEs.
- **HANA**: motor de base de datos in-memory de SAP, alternativa a SQL Server para SAP B1.
- **Service Layer**: API REST/OData oficial de SAP B1 (puerto 50000).
- **UDT**: User-Defined Table (tabla custom dentro de SAP B1).
- **UDO**: User-Defined Object (entidad de negocio custom).
- **UDF**: User-Defined Field (campo custom en tabla estándar).
- **FMS**: Formatted Search (mecanismo SAP B1 para disparar SQL al cambiar un campo).
- **DocEntry**: PK interna de documentos en SAP B1.
- **DocNum**: número visible al usuario, no es PK.
- **UpdateDate**: marca de tiempo de última modificación; granularidad de segundo.
- **MERGE**: comando SQL Server para upsert.
- **Watermark**: marca de "hasta dónde se ha procesado" en ETL incremental.
- **Lookback**: ventana hacia atrás que se vuelve a leer en cada corrida para capturar cambios retroactivos.
