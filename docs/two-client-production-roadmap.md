# DataBision — Roadmap técnico ejecutable: dos clientes en producción

Status: **Diseño técnico — sin implementación.**
Referencia: `dedicated-extractor-design.md`, `cloud-connector-queue-mode-design.md`, `azure-sql-staging-design.md`.

---

## Contexto

DataBision es una plataforma de reportería para SAP Business One. Este roadmap describe el camino mínimo para llevar dos clientes reales a producción con reportería de ventas actualizada cada 30–60 minutos.

| Cliente | Modalidad | SAP | Agente local |
|---|---|---|---|
| **Cliente A** | Dedicated Extractor (Modalidad A) | SAP B1 HANA, ambiente dedicado | Sí — Windows Service o Linux systemd |
| **Cliente B** | Cloud Connector / Queue Mode (Modalidad B) | SAP B1 cloud/restringido | No — Service Layer + UDT/UDO |

Ambos clientes comparten la misma infraestructura Azure (Resource Group, Ingest API, Azure Function) y el mismo schema lógico de Azure SQL. Sin embargo, **cada cliente tiene su propia Azure SQL Database** — no comparten la misma base de datos física. El aislamiento es por base de datos, no por fila ni schema.

> **Modelo MVP:** una Azure SQL DB por tenant/cliente. El Ingest API es único y distingue tenants por API Key + `tenant_id` en el payload. El schema (DDL, stored procedures) es idéntico en ambas DBs y se mantiene con migraciones versionadas.
>
> **Futuro:** si el número de tenants crece y el costo operacional de N bases individuales resulta alto, evaluar **Elastic Pool** o un modelo de DB compartida con RLS. Esa decisión se toma con datos reales de volumen y costo — no antes.

---

## Advertencia sobre duración y go-live comercial

> **MVP técnico vs producción confiable — no son lo mismo.**

Un MVP técnico (datos de SAP en Azure SQL, primera factura visible en el portal) puede lograrse en **días** con condiciones ideales: accesos listos, SAP disponible, sin problemas de firewall, volumen bajo.

Producción confiable para dos clientes reales — donde el cliente puede tomar decisiones de negocio basadas en los datos — probablemente requiere **semanas**, por los siguientes motivos reales:

| Causa de demora | Por qué no se puede acelerar |
|---|---|
| Validación de `UpdateTS` en HANA del cliente | Depende del entorno real; no predecible |
| Autorización para instalar agente en servidor del cliente | Requiere aprobación IT del cliente |
| Autorización para crear UDT/SP en SAP cloud (Cliente B) | Puede requerir solicitud formal al proveedor cloud |
| Medición real de throughput Service Layer | Solo se sabe con el ambiente productivo |
| Conciliación de conteos y totales contra SAP | No se puede asumir que el primer resultado es correcto |
| Detección de encoding/collation en datos SAP reales | Los datos sintéticos no reproducen todos los casos borde |
| Revisión con el cliente (¿los datos coinciden con su reporte SAP interno?) | El cliente necesita tiempo para validar |

**Regla de go-live comercial:**

No comprometer fecha de go-live productivo hasta tener:
1. Hito "SAP real → raw conciliado" validado para al menos uno de los dos clientes.
2. Throughput de Service Layer medido (para Cliente B).
3. Carga inicial completada y cronometrada en PROD (no en DEV).
4. Al menos una semana de ciclo incremental estable sin errores.

El go-live commercial puede negociarse como "acceso beta controlado" mientras se completa la estabilización — no como producto terminado.

---

## Fases del roadmap

---

### Fase 0 — Preparación Microsoft/Azure

**Objetivo:** Tener la infraestructura Azure mínima operativa antes de tocar código de extracción.

#### Tareas técnicas

- [ ] Crear Azure Subscription (DEV y futuro PROD en la misma suscripción o separadas).
- [ ] Crear Resource Group `rg-databision-dev`.
**Bloqueantes para empezar (hacer primero):**

- [ ] Provisionar Azure SQL Server + DB por tenant:
  - `databision-sql-dev` (servidor lógico compartido — una instancia lógica, una DB por tenant)
  - `db-tenant-clienta-dev` (DB dedicada Cliente A)
  - `db-tenant-clientb-dev` (DB dedicada Cliente B)
  - **Tier DEV:** S1 Standard (20 DTU) o S2 (50 DTU) — suficiente para desarrollo y pruebas. Ver tabla de tiers más abajo.
- [ ] Crear Azure Key Vault `kv-databision-dev`:
  - Secretos: `sql-conn-clienta`, `sql-conn-clientb`, `ingest-api-key-clienta`, `ingest-api-key-clientb`.
  - Política de acceso: solo servicios autorizados (no personas directamente).
- [ ] Crear Storage Account `stdatabisiondev`:
  - Blob container `raw-exports` para archivo futuro Parquet.
  - Blob container `csv-initial-loads` para carga inicial CSV Modalidad B.
- [ ] Crear Application Insights `appi-databision-dev` (para telemetría del Ingest API).
- [ ] Crear App Service Plan + Web App para Ingest API:
  - `app-databision-ingest-dev`
  - **Plan DEV: B1** (1 vCore, 1.75 GB RAM) — suficiente para DEV y pruebas con volumen bajo. Escalar durante carga inicial si es necesario (ver criterio abajo).
  - Managed Identity habilitada.
- [ ] Asignar Managed Identity del Ingest API como usuario en cada Azure SQL DB con rol `extractor_writer`.
- [ ] Configurar firewall Azure SQL: solo App Service Outbound IPs + IPs de soporte.
- [ ] Verificar conectividad App Service → Azure SQL (no Internet pública).

**No bloqueantes para empezar (preparación futura — hacer después de validar SAP→raw):**

- [ ] Crear App Registration en Azure AD (para Service Principal Power BI — no configurar todavía, solo el placeholder).
- [ ] Crear Power BI Workspace `DataBision DEV` (solo el workspace vacío, sin datasets).

> Estos dos ítems no son necesarios hasta que `raw.*` esté conciliado contra SAP real y el pipeline raw→fact esté probado. No crearlos como prerrequisito de go-live.

#### Criterio de escalamiento de tiers

| Momento | App Service | Azure SQL |
|---|---|---|
| DEV normal | B1 | S1/S2 |
| Carga inicial grande (> 100k docs) | Escalar temporalmente a B2/B3 | Escalar temporalmente a S3/S4 |
| PROD inicial (post-pruebas) | B1/B2 según pruebas de carga | S2/S3 según volumen real medido |
| PROD estabilizado (> 200k docs/día) | P1v3 o superior | P1/vCore General Purpose |

Regla: **no sobredimensionar DEV**. El costo de S1+B1 en DEV es < USD 50/mes. Escalar solo cuando los datos reales de volumen lo justifiquen.

#### Componentes afectados

Azure Subscription, Resource Groups, Azure SQL (dos DBs independientes), Key Vault, Storage Account, App Service, Application Insights.

#### Dependencias

Ninguna (es la fase cero).

#### Accesos requeridos

- Rol `Owner` o `Contributor` en Azure Subscription.
- Créditos Azure o suscripción activa.
- Acceso a Azure AD para App Registration (solo cuando se llegue a ese ítem futuro).

#### Criterios de aceptación (bloqueantes)

- [ ] `curl https://app-databision-ingest-dev.azurewebsites.net/health` responde `200 OK`.
- [ ] Managed Identity conecta a Azure SQL sin password en logs.
- [ ] Key Vault accesible desde App Service (GET secret sin error).
- [ ] Ambas DBs (`db-tenant-clienta-dev` y `db-tenant-clientb-dev`) son bases de datos independientes.
- [ ] Schemas `raw`, `stg`, `dim`, `fact`, `ctl`, `audit` creados en cada DB.
- [ ] Usuarios SQL con roles asignados en cada DB independientemente.

#### Criterios de aceptación (no bloqueantes — verificar cuando corresponda)

- [ ] App Registration creado en Azure AD (previo a configurar Power BI).
- [ ] Power BI Workspace DEV accesible (previo a publicar datasets).

#### Riesgos

| Riesgo | Mitigación |
|---|---|
| Firewall Azure SQL bloqueando App Service | Agregar "Allow Azure Services" como regla temporal; reemplazar con IPs explícitas |
| Managed Identity sin permisos en SQL | Script de provisioning crea el usuario SQL mapeado a la identidad antes del deploy |
| Tier insuficiente para carga inicial | Escalar temporalmente (ver tabla de tiers); degradar después de la carga |
| Power BI bloqueando go-live de extracción | Power BI no es bloqueante — el hito real es SAP→raw conciliado |

#### Duración estimada

2–3 días.

#### Entregable

Infraestructura Azure DEV operativa. Script de provisioning idempotente (`scripts/provision-azure-dev.sh`). Documento de accesos en Key Vault actualizado.

---

### Fase 1 — Azure SQL staging común

**Objetivo:** Tener el schema de Azure SQL completamente definido, con DDL aplicado, usuarios creados y stored procedures base operativos para ambos tenants.

#### Tareas técnicas

- [ ] Implementar DbUp o Flyway como mecanismo de migración versionada.
  - Carpeta `sql/migrations/` con scripts numerados `V001__create_schemas.sql`, `V002__create_ctl_tables.sql`, etc.
  - CI/CD aplica migraciones automáticamente al deploy.
- [ ] Aplicar DDL completo (basado en `azure-sql-staging-design.md` apéndice A):
  - Schemas: `raw`, `stg`, `dim`, `fact`, `ctl`, `audit`.
  - Tablas `ctl.*`: `extraction_run`, `extraction_run_detail`, `extraction_checkpoint`, `extraction_error`, `source_object_config`.
  - Tablas `audit.*`: `ingestion_event`, `data_quality_event`.
  - Tablas `raw.sap_*` MVP: `oinv`, `inv1`, `orin`, `rin1`, `ocrd`, `oitm`, `oslp`.
  - Índices base (watermark, card_doc_date).
  - Constraints (PK, CK, FK donde aplica).
- [ ] Implementar stored procedures base:
  - `raw.sp_upsert_sap_oinv` (MERGE con guarda temporal).
  - `raw.sp_upsert_sap_ocrd` / `oitm` / `oslp` / `orin`.
  - `raw.sp_upsert_sap_inv1` (delete+insert por DocEntry).
  - `raw.sp_upsert_sap_rin1` (mismo patrón que inv1).
- [ ] Poblar `ctl.source_object_config` con configuración inicial MVP por tenant:
  - OINV, INV1, ORIN, RIN1, OCRD, OITM, OSLP.
  - Valores default: `frequency_minutes=60`, `lookback_normal_hours=2`, etc.
- [ ] Implementar Ingest API endpoints:
  - `POST /api/ingest/{tenant_slug}/{source_object}` — recibe batch y llama SP correspondiente.
  - `GET /api/health` — liveness check.
  - `GET /api/ingest/checkpoint/{tenant_slug}/{company_id}/{source_object}` — retorna watermark actual.
  - Autenticación: API Key en header `X-DataBision-ApiKey` validado contra Key Vault.
- [ ] Test de carga básico: insertar 5000 filas OINV sintéticas y medir tiempo (objetivo < 2s p95).

#### Decisión de diseño: `source_hash` — quién lo calcula y cómo

`source_hash` es SHA-256 del contenido canónico de las columnas de negocio. Esta decisión tiene impacto directo en la equivalencia entre modalidades.

**Riesgo de dos implementaciones divergentes:** si el Dedicated Extractor (C#, HANA) y el Cloud Connector (C#, Service Layer) calculan `source_hash` de forma ligeramente distinta (diferente canonicalización, diferente set de columnas, diferente manejo de NULLs), el mismo registro SAP tendrá hashes diferentes por modalidad — lo que rompe el criterio de equivalencia.

**Recomendación:**

1. **Librería compartida obligatoria:** crear un proyecto `DataBision.Shared` con la función `CanonicalHash.Compute(Dictionary<string, object?> columns)`. Tanto el Extractor como el Connector deben referenciar este proyecto — nunca reimplementar el hash.
2. **Validación en el Ingest API:** el Ingest API puede opcionalmente re-calcular el hash desde el payload canónico y compararlo con el `source_hash` recibido. Si difieren, loguear `audit.ingestion_event` con flag `hash_validation_mismatch` (no rechazar, solo detectar). Esto actúa como red de seguridad si una versión del agente tiene un bug de canonicalización.
3. **Lista de columnas versionada:** el set de columnas incluidas en el hash está en `ctl.source_object_config.hash_columns` (campo JSON). Cambiar el set de columnas implica recalcular el hash de todo el histórico — hacer con cuidado y versionado.

#### Componentes afectados

Azure SQL, Ingest API (DataBision.Api), stored procedures, librería `DataBision.Shared`.

#### Dependencias

Fase 0 completada (infraestructura Azure disponible).

#### Accesos requeridos

- Azure SQL admin para aplicar DDL inicial.
- Managed Identity del Ingest API con rol `extractor_writer`.

#### Criterios de aceptación

- [ ] Migrations aplican sin error en DB vacía y en DB ya migrada (idempotente).
- [ ] `POST /api/ingest/clienta-ar/OINV` con payload de 100 filas sintéticas responde `200`.
- [ ] `raw.sap_oinv` tiene las 100 filas + columnas de trazabilidad.
- [ ] Re-enviar el mismo batch no duplica (MERGE idempotente).
- [ ] Re-enviar con `source_hash` distinto sí actualiza (watermark guard funciona).
- [ ] `ctl.extraction_checkpoint` tiene fila para `(tenant, company, 'OINV')` tras primer batch.
- [ ] `audit.ingestion_event` tiene registro de cada batch.

#### Riesgos

| Riesgo | Mitigación |
|---|---|
| MERGE deadlock bajo carga concurrente | Usar UPDATE+INSERT si se detecta; ver §12 del staging design |
| Schema drift entre tenants | Migrations versionadas; aplicar siempre a ambas DBs en CI |
| Ingest API sin manejo de errores en SP | SP con `SET XACT_ABORT ON` + THROW; API retorna 500 con error detallado |

#### Duración estimada

3–4 días.

#### Entregable

Ingest API desplegado en DEV. Ambas Azure SQL DBs con schema completo. Script de smoke test que valida inserción, idempotencia y watermark.

---

### Fase 2 — Modelo BI inicial de ventas (estructura con datos sintéticos)

**Objetivo:** Tener el DDL y los stored procedures de `stg`/`dim`/`fact` listos y verificados con un dataset sintético, de modo que cuando los extractores empiecen a poblar `raw` con datos reales, el pipeline ya esté probado.

> **Importante sobre el orden:** esta fase construye la estructura del modelo BI y lo verifica con datos sintéticos. Esto no reemplaza la validación con datos SAP reales — esa validación ocurre en las Fases 3 y 4. El modelo BI no se considera **cerrado** hasta que `fact.sales` se haya reconciliado contra SAP real con al menos un cliente. Ver hito de conciliación en §Fase 3 y §Fase 4.
>
> **¿Por qué construir el modelo BI antes de tener datos reales?** Porque si se espera a tener datos de SAP para definir el schema de `stg`/`dim`/`fact`, el Dedicated Extractor y el Cloud Connector no tienen destino preparado. Las Fases 3 y 4 dependen de que el pipeline raw→fact ya exista. Sin embargo, los stored procedures de refresh **no son validados como correctos hasta probar contra datos SAP reales**.

#### Tareas técnicas

- [ ] DDL `stg.*` tablas MVP:
  - `stg.customer`, `stg.item`, `stg.salesperson`.
  - `stg.sales_invoice_header`, `stg.sales_invoice_line`.
  - `stg.sales_credit_memo_header`, `stg.sales_credit_memo_line`.
- [ ] DDL `dim.*` tablas:
  - `dim.company`, `dim.customer` (SCD2), `dim.item` (SCD2), `dim.salesperson`, `dim.date`.
  - Pre-poblar `dim.date` 2020–2030.
- [ ] DDL `fact.*` tablas:
  - `fact.sales`, `fact.sales_credit`.
  - Vista `fact.vw_sales_net` (union con signo).
- [ ] Stored procedures de refresh:
  - `stg.sp_refresh_stg_customer` (raw.sap_ocrd → stg.customer).
  - `stg.sp_refresh_stg_item`.
  - `stg.sp_refresh_stg_salesperson`.
  - `stg.sp_refresh_stg_sales_invoice_header`.
  - `stg.sp_refresh_stg_sales_invoice_line`.
  - `dim.sp_refresh_dim_customer` (SCD2 logic).
  - `dim.sp_refresh_dim_item` (SCD2 logic).
  - `dim.sp_refresh_dim_salesperson`.
  - `fact.sp_refresh_fact_sales`.
  - `fact.sp_refresh_fact_sales_credit`.
- [ ] Jobs de refresh (Azure Function Timer o SQL Agent):
  - Raw→stg: cada hora.
  - Stg→dim/fact: cada noche 03:00 tenant TZ.
- [ ] Stored procedure DQ: `audit.sp_dq_check_run` con las 10 reglas del staging design §27.
- [ ] Test de regresión con dataset sintético completo:
  - 1000 facturas, 5 clientes, 3 vendedores, 20 ítems.
  - Verificar que `fact.sales` tiene el resultado correcto.
  - Verificar SCD2 en dim.customer (cambio de nombre → versión nueva).

#### Componentes afectados

Azure SQL (stg/dim/fact), Azure Functions o SQL Agent, stored procedures.

#### Dependencias

Fase 1 completada (raw schema + Ingest API operativo).

#### Accesos requeridos

- Azure SQL con rol `transform_runner` para jobs.
- Azure Function con Managed Identity o SQL user `databision_refresh`.

#### Criterios de aceptación (con datos sintéticos — cierre de Fase 2)

- [ ] Con raw poblado con datos sintéticos: `fact.sales` tiene filas correctas tras refresh.
- [ ] Refresh stg completo de 10k filas sintéticas: < 5 minutos.
- [ ] Refresh fact delta de 1k filas sintéticas: < 2 minutos.
- [ ] SCD2 crea nueva versión al cambiar `CardName` en OCRD sintético.
- [ ] Cancelación en OINV sintético se refleja en `fact.sales.is_canceled = 1` tras refresh.
- [ ] `audit.data_quality_event` registra filas problemáticas sin romper pipeline.
- [ ] Mismo input sintético produce mismo `fact.sales` independientemente del `ingestion_mode`.

#### Criterios de aceptación adicionales (con datos SAP reales — cierre de Fases 3/4)

- [ ] `fact.sales` con datos reales de Cliente A coincide ± 1% con reporte SAP.
- [ ] `fact.sales` con datos reales de Cliente B coincide ± 1% con reporte SAP.
- [ ] Stored procedures de refresh ajustados si se detectan diferencias entre datos sintéticos y datos SAP reales (tipos, NULLs, encoding, etc.).

#### Riesgos

| Riesgo | Mitigación |
|---|---|
| SCD2 genera duplicados si se corre dos veces | Unique filtered index `(company_id, customer_bk) WHERE is_current=1` previene |
| Refresh nocturno tarda más que la ventana | Monitorear duración; partir en jobs paralelos por company_id |
| DQ falsos positivos bloquean adopción | Severity WARN no bloquea; revisar reglas con datos reales del cliente |

#### Duración estimada

4–5 días.

#### Entregable

Pipeline raw→stg→dim→fact completo con jobs automáticos. Dataset sintético con 1000 facturas reproduciendo `fact.sales` correcto. Runbook de refresh.

---

### Fase 3 — Dedicated Extractor para Cliente A

**Objetivo:** Tener el agente DataBision instalado en el servidor de Cliente A, extrayendo datos desde SAP B1 HANA, enviando a Ingest API, y poblando `raw.*` de forma incremental.

#### Tareas técnicas

- [ ] **Preparar máquina de agente** (servidor Cliente A):
  - Verificar conectividad TCP a SAP HANA (puerto 30015 o custom).
  - Instalar .NET 8 Runtime.
  - Instalar HDBODBC driver (SAP HANA Client).
  - Crear usuario Windows/Linux de servicio `databision-agent`.
- [ ] **Configurar conexión HANA:**
  - Crear usuario SAP HANA de solo lectura para DataBision (no usar SYSTEM ni SAP\_\*).
  - Otorgar `SELECT` en tablas MVP: OINV, INV1, ORIN, RIN1, OCRD, OITM, OSLP.
  - Verificar acceso a `UpdateDate`, `UpdateTS`, `CreateDate`, `CreateTS` en cada tabla.
  - Confirmar que `UpdateTS` tiene valores coherentes (no siempre 0).
  - Ejecutar query de validación de watermark (ver Apéndice 1).
- [ ] **Configurar agente:**
  - `appsettings.json` (o env vars) con: host HANA, puerto, usuario, base SAP, Ingest API URL, API Key.
  - Secretos en archivo cifrado local (DPAPI en Windows) — nunca en texto plano.
  - `ctl.source_object_config` en Azure SQL preconfigurado con el `company_id` del Cliente A.
- [ ] **Carga inicial histórica:**
  - Ejecutar con ventana acordada con cliente (fuera de horario SAP).
  - Modalidad: `initial_load_from_date = NULL` (todo el histórico) o fecha acordada.
  - Tablas maestros primero (OCRD, OITM, OSLP), luego documentos (OINV, ORIN, INV1, RIN1).
  - Monitorear `audit.ingestion_event` durante la carga.
  - Tiempo estimado: depende del volumen; planificar ventana de 4–8 horas.
- [ ] **Instalar como servicio:**
  - Windows: `sc create DatabisionExtractor binPath= "..." start= auto`.
  - Linux: archivo `.service` systemd con `Restart=on-failure`.
  - Verificar que el servicio arranca automáticamente tras reboot.
- [ ] **Validar ciclo incremental:**
  - Crear una factura de prueba en SAP.
  - Esperar máximo 60 minutos.
  - Verificar que aparece en `raw.sap_oinv`.
- [ ] **Configurar alertas:**
  - Alerta si el agente no reporta heartbeat en > 2 horas.
  - Alerta si `ctl.extraction_error` tiene errores no resueltos > 1 hora.

#### Componentes afectados

DataBision.Extractor (Worker Service), Ingest API, Azure SQL (raw), agente instalado en servidor Cliente A.

#### Dependencias

- Fases 0, 1 y 2 completadas.
- Checklist B (accesos Cliente A) completado y validado.
- Ventana de carga inicial acordada con Cliente A.

#### Accesos requeridos

- RDP/SSH al servidor donde se instalará el agente.
- Usuario SAP HANA de solo lectura con `SELECT` en tablas MVP.
- API Key del Ingest API para el tenant de Cliente A (en Key Vault).

#### Criterios de aceptación

- [ ] `SELECT TOP 1 * FROM raw.sap_oinv WHERE tenant_id = <clienta_id>` retorna filas.
- [ ] Carga inicial completa sin errores en `ctl.extraction_error`.
- [ ] Factura de prueba nueva en SAP aparece en `raw.sap_oinv` dentro de 60 minutos.
- [ ] Agente arranca automáticamente tras reboot del servidor.
- [ ] Heartbeat visible en `ctl.extraction_run` cada ciclo.
- [ ] Conteo de documentos en `raw.sap_oinv` ± 1% vs `SELECT COUNT(*) FROM OINV` en HANA.

#### Hito de validación: SAP real → Azure SQL raw conciliado (Cliente A)

**Este hito es prerrequisito para considerar la Fase 3 cerrada.** No avanzar a Fase 5 (portal) sin él.

| Check | Criterio | Query de validación |
|---|---|---|
| OINV cargada | COUNT raw = COUNT SAP ± 1% | `SELECT COUNT(*) FROM raw.sap_oinv` vs `SELECT COUNT(*) FROM OINV` en HANA |
| INV1 cargada | COUNT raw = COUNT SAP ± 1% | Idem para inv1 |
| ORIN cargada | COUNT raw = COUNT SAP ± 1% | Idem para orin |
| RIN1 cargada | COUNT raw = COUNT SAP ± 1% | Idem para rin1 |
| OCRD cargada | COUNT raw = COUNT SAP ± 0 | Maestros deben ser exactos |
| OITM cargada | COUNT raw = COUNT SAP ± 0 | |
| OSLP cargada | COUNT raw = COUNT SAP ± 0 | |
| Suma DocTotal conciliada | `SUM(DocTotal)` raw vs HANA ± 0.01% | Sobre período de 6 meses |
| Incremental probado | Documento nuevo en SAP → raw en ≤ 60 min | Factura de prueba creada y verificada |
| Documento modificado | Modificar factura → raw actualizado en ≤ 60 min | Cambiar Comments; verificar update en raw |
| Reintento probado | Detener Ingest API 10 min → reactivar → datos no perdidos | Verificar `ctl.extraction_error` y recovery |
| Checkpoint probado | Reiniciar agente → retoma desde watermark correcto | No re-extrae todo desde cero |

#### Riesgos

| Riesgo | Mitigación |
|---|---|
| HDBODBC no compatible con versión HANA del cliente | Validar versión HANA antes de instalar; tener fallback SL disponible |
| Carga inicial satura SAP en horario productivo | Throttle: `page_size=500`, `max_per_run=5000`; solo en ventana nocturna |
| Firewall cliente bloquea salida HTTPS a Azure | VPN o whitelist IP del App Service en reglas de firewall del cliente |
| UpdateTS siempre 0 en versión SAP del cliente | Compound watermark fallback a solo UpdateDate + NaturalKey; aceptable con lookback |
| Servidor cliente sin .NET 8 o HDBODBC | Script de instalación pre-validado; checklist B completo antes de empezar |

#### Duración estimada

3–4 días (incluyendo carga inicial y validación).

#### Entregable

Agente instalado y corriendo como servicio en Cliente A. `raw.*` de Cliente A poblado con histórico. Ciclo incremental validado con factura de prueba. Runbook de operación del agente.

---

### Fase 4 — Cloud Connector / Queue Mode para Cliente B

**Objetivo:** Tener el conector Cloud funcionando para Cliente B, con UDT/UDO creados en SAP, cola procesada por Azure Function, y datos incrementales llegando a `raw.*`.

#### Tareas técnicas

- [ ] **Validar Service Layer de Cliente B:**
  - Confirmar URL base Service Layer (ej. `https://sap-sl.clienteb.com/b1s/v1`).
  - Confirmar usuario/contraseña (Service Layer usa HTTP Basic o cookie session).
  - Ejecutar `GET /b1s/v1/$metadata` para validar conectividad.
  - Confirmar que el usuario tiene permiso de CRUD en BusinessPartners, Items, Invoices.
  - Medir throughput real: cuántas requests/minuto sin throttling.
- [ ] **Crear UDT `@DBI_SYNC_QUEUE` en SAP:**
  - Validar si el ambiente permite UDT via Service Layer PATCH (`/b1s/v1/UserTablesMD`).
  - Alternativa: solicitar al cliente que lo cree vía SAP B1 IDE (System > User-Defined Tables).
  - Confirmar que UDT es `NoObject` (no genera serie documental).
  - Probar GET/POST/PATCH en `U_DBI_SYNC_QUEUE` via SL.
  - Si falla: documentar y evaluar fallback a polling directo SL.
- [ ] **Crear UDT `@DBI_SYNC_LOG` en SAP** (mismo proceso).
- [ ] **Crear TransactionNotification SP** (si el cliente lo permite):
  - SP que hace INSERT en `@DBI_SYNC_QUEUE` cuando OINV/ORIN/OCRD/OITM/OSLP cambia.
  - Envuelto en TRY/CATCH que siempre retorna 0 — nunca bloquea SAP.
  - Validar que no impacta rendimiento de operaciones SAP (test con transacción de prueba).
- [ ] **Configurar Azure Function Timer:**
  - Timer cada 5 minutos: lee items pendientes de `@DBI_SYNC_QUEUE`.
  - Por cada item: GET full record via SL → DTO batch → POST /api/ingest/{tenant}/{object}.
  - Claim atómico: PATCH `U_Status='PROCESSING'` antes de procesar.
  - Completar: PATCH `U_Status='DONE'`.
  - Error: PATCH `U_Status='ERROR'`, `U_Retries += 1`.
  - Max retries: 3. Si supera → PATCH `U_Status='DEAD'` → alerta.
- [ ] **Carga inicial para Cliente B:**
  - Opción 1: SL drain completo con paginación (lento pero self-service).
  - Opción 2: CSV exportado desde SAP Business One → upload a Blob → `CSV_INITIAL_LOAD`.
  - Decidir con el cliente según volumen y disponibilidad.
  - Verificar que `initial_loaded = 1` en `ctl.extraction_checkpoint` tras completar.
- [ ] **Validar ciclo incremental:**
  - Crear BusinessPartner de prueba en SAP.
  - Verificar que aparece en `@DBI_SYNC_QUEUE` (TN funcionó).
  - Esperar ciclo Azure Function (máx 5–10 minutos).
  - Verificar en `raw.sap_ocrd`.

#### Componentes afectados

DataBision.CloudConnector (Azure Function), Ingest API, Azure SQL (raw), SAP B1 UDT/SP/TN del Cliente B.

#### Dependencias

- Fases 0, 1 y 2 completadas.
- Checklist C (accesos Cliente B) completado y validado.
- Autorización del cliente para crear UDT/UDO y SP en SAP.

#### Accesos requeridos

- URL y credenciales de Service Layer Cliente B.
- Usuario SL con permisos de CRUD en tablas MVP + permisos para crear UDT (o confirmación de que IT del cliente lo creará).
- Azure Function desplegada con Managed Identity o connection string al Ingest API.

#### Criterios de aceptación

- [ ] `GET /b1s/v1/Invoices` desde Azure Function retorna datos reales.
- [ ] POST en `@DBI_SYNC_QUEUE` y GET del item funciona via SL.
- [ ] TN inserta en la cola al crear/modificar documento SAP (probado con factura de prueba).
- [ ] Azure Function procesa la cola y llama Ingest API correctamente.
- [ ] `raw.sap_oinv` para Cliente B tiene filas tras ciclo de la Function.
- [ ] Factura de prueba nueva en SAP aparece en `raw.sap_oinv` dentro de 10 minutos.
- [ ] Conteo documentos en `raw.sap_oinv` ± 1% vs conteo vía `GET /b1s/v1/Invoices/$count`.

#### Hito de validación: SAP real → Azure SQL raw conciliado (Cliente B)

**Este hito es prerrequisito para considerar la Fase 4 cerrada.** No avanzar a Fase 5 (portal) sin él.

| Check | Criterio | Método de validación |
|---|---|---|
| OINV cargada | COUNT raw = `GET /b1s/v1/Invoices/$count` ± 1% | Query Azure SQL vs SL |
| INV1 cargada | COUNT raw conciliado | Derivado por DocEntry |
| ORIN cargada | COUNT raw = SL/$count ± 1% | |
| OCRD cargada | COUNT exacto | Maestros |
| OITM cargada | COUNT exacto | |
| OSLP cargada | COUNT exacto | |
| Suma DocTotal conciliada | `SUM(DocTotal)` raw vs SL paginado ± 0.01% | Script de comparación |
| Incremental por cola probado | Documento nuevo → raw en ≤ 10 min | Factura de prueba; verificar TN + Function |
| Documento modificado | Modificar → raw actualizado en ≤ 10 min | Cambio en SAP; verificar MERGE en raw |
| Reintento probado | Bajar Function 10 min → levantar → cola procesada | `@DBI_SYNC_QUEUE` no pierde items |
| Checkpoint probado | Reiniciar Function → retoma desde checkpoint | `ctl.extraction_checkpoint` correcto |

#### Riesgos

| Riesgo | Mitigación |
|---|---|
| UDT no autorizado en ambiente cloud del cliente | Fallback: polling SL puro sin cola; mayor lag pero funcional |
| TN bloqueando SAP si hay bug | TRY/CATCH con RETURN 0 es la regla dura — revisar el SP antes de deployar |
| SL throttling (429 demasiadas requests) | Exponential backoff en la Function; reducir frecuencia del timer si necesario |
| ETag/If-Match no soportado en SL del cliente | Fallback: claim con campo `U_ProcessingOwner` y re-lectura (ver cloud connector doc §23) |
| Carga inicial vía SL demasiado lenta | CSV fallback; estimar volumen con cliente antes de decidir |

#### Duración estimada

4–5 días (incluyendo validación UDT, carga inicial y ciclo incremental).

#### Entregable

Azure Function desplegada y procesando cola de SAP Cliente B. `raw.*` de Cliente B poblado con histórico. Ciclo incremental validado. Runbook de operación del conector.

---

### Fase 5 — Portal DataBision conectado a datos reales

**Objetivo:** Los usuarios finales de Cliente A y Cliente B pueden ver reportes de ventas reales en el portal DataBision, con datos actualizados cada hora.

#### Tareas técnicas

- [ ] Verificar que el portal actual funciona contra Azure SQL DEV (no SQLite local).
  - Actualizar `ConnectionStrings__DefaultConnection` en App Service Portal a Azure SQL.
  - Verificar que el portal API usa el rol `portal_reader` (SELECT only en stg/dim/fact).
- [ ] Conectar los endpoints de reportes del portal a las queries de `fact.*`:
  - `GET /api/reports/sales-summary` → query `fact.sales` con filtro `company_id`.
  - `GET /api/reports/sales-by-customer` → join `fact.sales + dim.customer`.
  - `GET /api/reports/sales-by-item` → join `fact.sales + dim.item`.
  - `GET /api/reports/sales-by-period` → join `fact.sales + dim.date`.
- [ ] Configurar tenant routing:
  - `GET /api/tenant/config` retorna branding por slug.
  - Queries de reportes filtran por `company_id` derivado del JWT.
- [ ] Crear usuarios finales de prueba:
  - `clienta_admin@acme.com` → JWT con `company_id` de Cliente A.
  - `clientb_admin@otroempresa.com` → JWT con `company_id` de Cliente B.
- [ ] Validar aislamiento: usuario Cliente A no puede ver datos de Cliente B.
- [ ] Validar que los reportes muestran datos coherentes vs SAP directo (muestra de 50 facturas).
- [ ] Configurar branding por tenant (logo, colores) en `tenant.config`.

#### Componentes afectados

DataBision.Api (portal endpoints), DataBision Frontend, Azure SQL (dim/fact).

#### Dependencias

- **Hito "SAP real → raw conciliado" de Fase 3 completado** (Cliente A con datos reales en raw).
- **Hito "SAP real → raw conciliado" de Fase 4 completado** (Cliente B con datos reales en raw).
- Al menos un ciclo de refresh raw→stg→dim→fact corrido con datos SAP reales (no solo sintéticos).

> No conectar el portal a datos reales si la conciliación raw vs SAP no está validada. Mostrar datos incorrectos al cliente final es peor que no mostrar nada.

#### Accesos requeridos

- Azure SQL con rol `portal_reader` para el portal API.
- URL del portal DEV accesible para el equipo de QA.

#### Criterios de aceptación

- [ ] Login con usuario Cliente A muestra solo datos de Cliente A.
- [ ] Login con usuario Cliente B muestra solo datos de Cliente B.
- [ ] Reporte "ventas del mes actual" coincide ± 1% con reporte SAP equivalente.
- [ ] Datos se actualizan tras refresh nocturno (nueva factura del día aparece al día siguiente).
- [ ] No aparecen errores 500 ni queries sin `company_id` filter en logs.

#### Riesgos

| Riesgo | Mitigación |
|---|---|
| Portal API con query sin `company_id` filter | Code review de cada endpoint; test de aislamiento en CI |
| Datos stg/fact desactualizados durante demo | Trigger manual del refresh job antes de demo |
| Branding no configurable por tenant sin deploy | Config en `ctl.source_object_config` o tabla `tenant.config` editable sin redeploy |

#### Duración estimada

2–3 días.

#### Entregable

Portal DataBision mostrando datos reales de ventas para ambos clientes. Validación de aislamiento de tenants. Screenshot/video de la demo interna.

---

### Fase 6 — Pruebas E2E

**Objetivo:** Validar el sistema completo de punta a punta — desde SAP hasta el portal — incluyendo escenarios de error, recuperación y equivalencia entre modalidades.

#### Tareas técnicas

Ver Checklist E (completo) más abajo.

Pruebas prioritarias:

- [ ] **Carga inicial:** ambos clientes cargados desde cero, `fact.sales` correcto.
- [ ] **Incremental normal:** factura nueva → portal en ≤ 65 minutos (Cliente A) o ≤ 10 minutos (Cliente B).
- [ ] **Lookback:** factura modificada hace 3 horas es recapturada en siguiente ciclo.
- [ ] **Cancelación:** factura cancelada → `is_canceled=1` en `fact.sales` tras refresh.
- [ ] **Nota de crédito:** ORIN procesada → `fact.sales_credit` tiene la fila.
- [ ] **Documento sin líneas:** cabecera OINV sin filas INV1 → DQ event, sin crash.
- [ ] **Duplicado de batch:** mismo batch re-enviado → `source_hash` evita duplicado.
- [ ] **Caída Ingest API:** agente reintenta con backoff exponencial; no pierde datos.
- [ ] **Caída Azure SQL:** función/agente encola en memoria o disco local; resume al recuperar.
- [ ] **Caída SAP (Modalidad B):** Azure Function maneja timeout SL con retry.
- [ ] **Reconciliación nocturna:** `sp_reconcile_against_source` detecta una divergencia intencional.
- [ ] **Equivalencia de modalidades:** mismo dataset cargado por A y por B → mismo `fact.sales`.
- [ ] **Aislamiento de tenant:** request con JWT de Cliente A no retorna datos de Cliente B.
- [ ] **Performance:** query "top 10 clientes por venta mes actual": < 1s p95.

#### Duración estimada

3–4 días.

#### Entregable

Reporte de pruebas E2E firmado. Lista de bugs encontrados y resueltos. Confirmación de go/no-go.

---

### Fase 7 — Go-live controlado

**Objetivo:** Activar producción para ambos clientes con supervisión activa y capacidad de rollback inmediato.

#### Tareas técnicas

- [ ] **Provisionar infraestructura PROD** (mismo proceso que DEV, Resource Group separado).
  - `rg-databision-prod`, `databision-sql-prod`, `kv-databision-prod`.
  - Tier más alto: S4 para Cliente A (esperando carga inicial grande), P1 para Cliente B si es cloud.
- [ ] **Migrar DBs DEV → PROD:**
  - Aplicar migrations en PROD (no copiar la DB DEV).
  - Re-correr carga inicial en PROD (no copiar datos DEV).
- [ ] **Deploy Ingest API PROD** + Azure Function PROD.
- [ ] **Go-live secuencial:**
  - Día 1: Solo Cliente A (Modalidad A). Validar 24 horas antes de activar Cliente B.
  - Día 2+: Cliente B (Modalidad B), con monitoreo activo las primeras 4 horas.
- [ ] **Comunicar a usuarios finales** horarios de disponibilidad y lag esperado.
- [ ] **Activar alertas de producción:**
  - Alerta si `ctl.extraction_run` sin SUCCESS en > 2 horas.
  - Alerta si `ctl.extraction_error` con error crítico.
  - Alerta si Ingest API retorna > 5% de 5xx en ventana de 15 minutos.
  - Dashboard en Application Insights visible para el equipo.

#### Riesgos

| Riesgo | Mitigación |
|---|---|
| Carga inicial PROD más lenta de lo esperado | Plan de rollback: mantener DEV disponible para demo mientras PROD se carga |
| Bug descubierto en PROD tras go-live | Ver Plan de Rollback (sección F) |
| Credenciales de PROD expuestas | Todos los secrets en Key Vault PROD; no en config files |

#### Duración estimada

2–3 días (go-live Cliente A + validación + go-live Cliente B).

#### Entregable

Ambos clientes en producción. Dashboard de monitoreo operativo. Runbook de go-live documentado.

---

### Fase 8 — Soporte, monitoreo y estabilización

**Objetivo:** Estabilizar el sistema en producción durante las primeras 4 semanas, resolver incidentes rápidamente y documentar lecciones aprendidas.

Ver Plan de Soporte Post Go-Live (sección G) más abajo.

#### Tareas técnicas

- [ ] Revisión diaria de logs y errores (primeras 2 semanas).
- [ ] Conciliación semanal ventas SAP vs `fact.sales`.
- [ ] Ajuste de lookbacks y frecuencias según comportamiento real.
- [ ] Documentar los 10 incidentes más comunes con su resolución.
- [ ] Retrospectiva al final de la semana 4: decisiones pendientes (sección J).

#### Duración estimada

4 semanas de estabilización activa.

#### Entregable

Runbook de soporte. Dashboard de monitoreo funcional. Documento de lecciones aprendidas. Estado de las decisiones abiertas.

---

## Checklists

---

### A. Checklist Microsoft/Azure

| Item | Estado | Notas |
|---|---|---|
**Bloqueantes (necesarios antes de empezar extracción):**

| Item | Estado | Notas |
|---|---|---|
| Azure Subscription activa | ⬜ | Confirmar si DEV y PROD en misma suscripción |
| Resource Group DEV (`rg-databision-dev`) | ⬜ | |
| Azure SQL Server DEV (`databision-sql-dev`) | ⬜ | Región: la más cercana al servidor de Cliente A |
| Azure SQL DB Cliente A DEV — **base independiente** | ⬜ | Tier S1/S2; escalar durante carga inicial si necesario |
| Azure SQL DB Cliente B DEV — **base independiente** | ⬜ | Tier S1/S2; escalar durante carga inicial si necesario |
| Key Vault DEV (`kv-databision-dev`) | ⬜ | Soft-delete habilitado |
| Storage Account DEV (`stdatabisiondev`) | ⬜ | Containers: `raw-exports`, `csv-initial-loads` |
| Application Insights DEV | ⬜ | Connection string en App Settings del API |
| App Service Plan DEV (**B1 recomendado para DEV**) | ⬜ | Escalar a B2/B3 solo durante carga inicial grande |
| App Service Ingest API DEV | ⬜ | Managed Identity habilitada |
| Azure Function App DEV (Cloud Connector) | ⬜ | Consumption plan para DEV; Premium V2 solo si cold start es problema |
| Firewall Azure SQL configurado | ⬜ | Solo App Service IPs + IPs soporte |
| Managed Identity → SQL DB vinculada (ambas DBs) | ⬜ | Usuario SQL mapeado en cada DB independientemente |
| Todos los secrets en Key Vault | ⬜ | Verificar que no hay secrets en appsettings.json productivo |

**No bloqueantes (preparación futura — hacer cuando corresponda):**

| Item | Estado | Cuándo activar |
|---|---|---|
| App Registration Azure AD | ⬜ | Antes de configurar Service Principal Power BI |
| Power BI Workspace DEV | ⬜ | Después de validar raw→fact con datos SAP reales |

---

### B. Checklist Cliente A — Dedicated Extractor

| Item | Estado | Notas |
|---|---|---|
| Hostname/IP del servidor SAP HANA | ⬜ | `___.___.___.___` puerto `_____` |
| Puerto HANA (default 30015 o custom) | ⬜ | |
| Usuario SAP HANA de solo lectura creado | ⬜ | Nombre: `DatabisionReader` o equivalente |
| Permisos SELECT en tablas MVP confirmados | ⬜ | OINV, INV1, ORIN, RIN1, OCRD, OITM, OSLP |
| Base SAP real confirmada (ej. `SBO_ACME_AR`) | ⬜ | |
| Reglas de firewall/VPN para salida HTTPS a Azure | ⬜ | El agente necesita salida a `*.azurewebsites.net` |
| Servidor disponible para instalar agente | ⬜ | Windows o Linux con acceso al servidor HANA |
| .NET 8 Runtime instalable | ⬜ | O permisos para instalarlo |
| HDBODBC Driver instalable | ⬜ | Compatible con versión HANA del cliente |
| Permisos para crear Windows Service o systemd | ⬜ | Admin local del servidor |
| `UpdateTS` tiene valores coherentes (no siempre 0) | ⬜ | Validar con query de diagnóstico antes de instalar |
| `CreateTS` disponible en tablas transaccionales | ⬜ | |
| Volumen estimado de documentos | ⬜ | Para estimar duración de carga inicial |
| Ventana de carga inicial acordada | ⬜ | Fecha/hora fuera de horario SAP productivo |
| Contacto técnico en el cliente durante instalación | ⬜ | Nombre y WhatsApp/email |

---

### C. Checklist Cliente B — Cloud Connector

| Item | Estado | Notas |
|---|---|---|
| URL base Service Layer confirmada | ⬜ | `https://___/b1s/v1` |
| Usuario Service Layer con permisos de lectura | ⬜ | |
| Permisos mínimos confirmados | ⬜ | GET en Invoices, BusinessPartners, Items, CreditNotes |
| Autorización para crear UDT (`@DBI_SYNC_QUEUE`) | ⬜ | Validar si cloud SAP lo permite o requiere IT |
| Autorización para crear UDT (`@DBI_SYNC_LOG`) | ⬜ | |
| Validación CRUD en `U_DBI_SYNC_QUEUE` via SL | ⬜ | POST/PATCH/GET probados |
| Validación ETag/If-Match en SL | ⬜ | Para claim atómico; si no → fallback U_ProcessingOwner |
| Throughput real Service Layer medido | ⬜ | Requests/minuto sostenibles sin 429 |
| Autorización para crear TransactionNotification SP | ⬜ | Validar con IT del cliente |
| SP TN probado sin bloquear transacción SAP | ⬜ | TRY/CATCH + RETURN 0 validado |
| Opción CSV para carga inicial disponible | ⬜ | Si volumen > umbral de SL drain práctico |
| Contacto IT del cliente para creación de UDT/SP | ⬜ | Nombre y datos de contacto |
| Aprobación de job de reconciliación diario | ⬜ | Job que llama SL una vez por noche |

---

### D. Checklist seguridad

| Item | Estado | Notas |
|---|---|---|
| Cero secretos en repositorio git | ⬜ | `git grep -i "password\|secret\|connstring"` limpio |
| Credenciales Azure en Key Vault | ⬜ | No en appsettings.json productivos |
| Credenciales SAP en archivo cifrado local (agente A) | ⬜ | DPAPI Windows o Vault Linux |
| Usuarios SAP dedicados para DataBision | ⬜ | No usar SYSTEM ni usuarios compartidos |
| Permisos mínimos en SAP (solo SELECT tablas MVP) | ⬜ | |
| Logs sin passwords, tokens ni hashes de credenciales | ⬜ | Revisar outputs de logging en código |
| Separación DEV/TST/PROD en Key Vault | ⬜ | KVs separados por ambiente |
| Rotación de API Keys documentada | ⬜ | Proceso para rotar sin downtime |
| TLS 1.2+ forzado en Azure SQL | ⬜ | `ssl=true;encrypt=true` en connection string |
| Firewall reglas mínimas | ⬜ | Sin "Allow All Azure" en PROD |
| Managed Identity en lugar de SQL passwords donde posible | ⬜ | App Service + Azure Function |
| Auditoría SQL activada en PROD | ⬜ | Login failures + DDL al menos |

---

## E. Checklist de pruebas

| Escenario | Modalidad A | Modalidad B | Criterio |
|---|---|---|---|
| Carga inicial desde cero | ⬜ | ⬜ | `fact.sales` correcto vs SAP |
| Incremental: factura nueva | ⬜ | ⬜ | Aparece en raw dentro del SLA |
| Incremental: factura modificada | ⬜ | ⬜ | raw actualizado; watermark avanza |
| Lookback: factura modificada hace 3h | ⬜ | ⬜ | Capturada en próximo ciclo |
| Cancelación de factura | ⬜ | ⬜ | `is_canceled=1` en fact.sales |
| Nota de crédito | ⬜ | ⬜ | fact.sales_credit tiene la fila |
| Documento sin líneas (OINV sin INV1) | ⬜ | ⬜ | DQ event; pipeline no crashea |
| Líneas sin cabecera (INV1 llega antes) | ⬜ | ⬜ | SP rechaza con ORPHAN_LINES_REJECTED |
| Batch duplicado (mismo batch_id) | ⬜ | ⬜ | source_hash evita duplicado |
| Batch con source_hash distinto (dato cambió) | ⬜ | ⬜ | raw actualizado correctamente |
| Caída del Ingest API (5 minutos) | ⬜ | ⬜ | Agente/Function reintenta; no pierde datos |
| Caída de Azure SQL (5 minutos) | ⬜ | ⬜ | Agente/Function reintenta con backoff |
| Caída SAP HANA (solo A) | ⬜ | — | Agente registra error; alertas; resume |
| Caída SL (solo B) | — | ⬜ | Function registra error; alertas; resume |
| Retries agotados (3 intentos fallidos) | ⬜ | ⬜ | Alerta enviada; no loop infinito |
| Reconciliación detecta divergencia intencional | ⬜ | ⬜ | audit.data_quality_event generado |
| Requeue de rango divergente | ⬜ | ⬜ | Rango re-extraído y resuelto |
| Equivalencia A vs B para mismo dataset | ⬜ | ⬜ | fact.sales idéntico |
| Aislamiento: usuario A no ve datos B | ⬜ | ⬜ | API retorna 403 o vacío |
| Query performance "top clientes 30 días" | ⬜ | ⬜ | < 1s p95 |
| Refresh stg completo 10k filas | ⬜ | ⬜ | < 5 minutos |
| Upsert batch 5000 filas | ⬜ | ⬜ | < 2s p95 |

---

## F. Plan de rollback

### Nivel 1 — Detener extracción sin tocar datos

Aplicar si un ciclo produjo datos sospechosos pero raw aún no contaminó stg/fact:

```
1. Detener agente Cliente A:  sc stop DatabisionExtractor   (Windows)
                               systemctl stop databision-extractor  (Linux)

2. Detener Azure Function Cliente B: Disable timer trigger desde Azure Portal.

3. Verificar:  SELECT * FROM ctl.extraction_run
               WHERE tenant_id IN (...) AND started_at > GETUTCDATE() - 1
               ORDER BY started_at DESC;

4. Identificar el run_id problemático en ctl.extraction_error.
```

### Nivel 2 — Restaurar checkpoint al estado pre-incidente

Si raw fue contaminado con datos incorrectos pero stg/fact aún no se refrescaron:

```sql
-- Leer checkpoint antes del run problemático
SELECT * FROM ctl.extraction_run WHERE run_id = <run_problemático - 1>;

-- Restaurar checkpoint
UPDATE ctl.extraction_checkpoint
SET    last_update_date    = '<fecha_anterior>',
       last_update_ts_norm = '<ts_anterior>',
       last_run_id         = <run_id_anterior>,
       last_run_status     = 'SUCCESS'
WHERE  tenant_id = <tenant> AND company_id = <company> AND object_name = '<objeto>';

-- Opcional: borrar filas del batch problemático si son identificables
DELETE FROM raw.sap_oinv
WHERE  tenant_id = <tenant>
  AND  company_id = <company>
  AND  _batch_id = '<batch_id_problemático>';
```

### Nivel 3 — Reprocesar rango desde SAP

Si raw tiene datos incorrectos que ya propagaron a stg:

```
1. Detener jobs de refresh (stg/dim/fact).
2. Truncar stg.<tabla> para el tenant afectado.
3. Correr extractor con lookback ampliado para el rango problemático.
4. Verificar raw correcto.
5. Re-correr sp_refresh_stg_<tabla>.
6. Verificar stg correcto.
7. Re-activar jobs de refresh.
```

### Nivel 4 — Restaurar desde PITR (Azure SQL Point-in-Time Restore)

Para corrupción grave o bug irreversible en raw+stg+fact:

```
1. Azure Portal → SQL Database → Restore.
2. Seleccionar punto antes del incidente.
3. Restaurar a nueva DB (no overwrite la DB productiva).
4. Validar datos en la DB restaurada.
5. Swap de connection string (o rename de DB).
6. Re-replay incremental desde el timestamp de restore.
```

### Reglas de rollback

- **SAP nunca es afectado** por ningún nivel de rollback DataBision.
- Comunicar al cliente antes del nivel 3 o 4.
- Documentar el incidente en `audit.ingestion_event` con `triggered_by='ROLLBACK'`.
- Test de restore PITR: ejecutar trimestralmente.

---

## G. Plan de soporte post go-live

### SLA inicial (primeras 4 semanas)

| Tipo | Tiempo de respuesta | Tiempo de resolución |
|---|---|---|
| Datos no actualizados > 2 horas | 30 minutos | 4 horas |
| Error en portal (no carga) | 1 hora | 8 horas |
| Dato incorrecto reportado | 2 horas diagnóstico | 24 horas resolución |
| Pregunta de usuario | 4 horas hábiles | — |

### Monitoreo diario (primeras 2 semanas)

```sql
-- 1. Últimos runs por tenant
SELECT tenant_id, company_id, status, started_at, ended_at,
       DATEDIFF(MINUTE, started_at, ended_at) AS duration_min
FROM   ctl.extraction_run
WHERE  started_at > GETUTCDATE() - 1
ORDER  BY started_at DESC;

-- 2. Errores no resueltos
SELECT * FROM ctl.extraction_error
WHERE  occurred_at > GETUTCDATE() - 24
ORDER  BY occurred_at DESC;

-- 3. Audit de calidad
SELECT source_object, dq_rule, severity, affected_rows, detected_at
FROM   audit.data_quality_event
WHERE  detected_at > GETUTCDATE() - 24
ORDER  BY detected_at DESC;

-- 4. Lag por tenant (tiempo desde último update)
SELECT object_name,
       last_update_date,
       DATEDIFF(MINUTE, MAX(started_at), GETUTCDATE()) AS minutes_since_last_run
FROM   ctl.extraction_checkpoint cp
JOIN   ctl.extraction_run r ON r.run_id = cp.last_run_id
GROUP  BY object_name, last_update_date;
```

### Conciliación semanal de ventas

1. Exportar de SAP: total facturas, suma DocTotal, max DocEntry para el mes en curso.
2. Consultar `fact.sales` con los mismos filtros.
3. Tolerancia: ± 0.1% en suma, ± 0 en conteo (todo debe estar).
4. Discrepancias → encolar reextracción del rango.

### Revisión y ajuste de configuración (fin de semana 2)

- Revisar `ctl.source_object_config`: ¿los lookbacks son adecuados?
- Revisar `audit.ingestion_event`: ratio hash_collisions — si > 90%, lookbacks son demasiado amplios.
- Revisar DTU/CPU de Azure SQL: ¿el tier actual es suficiente?
- Documentar cambios de configuración con `updated_by` en el config.

---

## H. Roadmap por semanas

| Semana | Fases activas | Hitos clave |
|---|---|---|
| **Semana 1** | Fase 0 + Fase 1 | Infraestructura Azure DEV operativa (sin Power BI aún). Ingest API funcional. DDL aplicado en ambas DBs. Smoke test de MERGE con datos sintéticos. |
| **Semana 2** | Fase 2 + inicio Fase 3 | Pipeline raw→stg→fact con datos sintéticos probado. Inicio instalación agente Cliente A. Checklists B y C iniciados con los clientes. |
| **Semana 3** | Fase 3 — carga inicial + hito conciliación Cliente A | Carga inicial Cliente A completa. **Hito: raw conciliado contra SAP real.** Ciclo incremental estable. Inicio setup UDT/SP en SAP Cliente B (en paralelo si es posible). |
| **Semana 4** | Fase 4 — carga inicial + hito conciliación Cliente B | Carga inicial Cliente B completa. **Hito: raw conciliado contra SAP real Cliente B.** Ciclo incremental estable. Pipeline raw→fact con datos reales de ambos. |
| **Semana 5** | Fase 5 + Fase 6 | Portal conectado a datos reales. Pruebas E2E. Validación de aislamiento y conciliación final. Decisión go/no-go. |
| **Semana 6** | Fase 7 (go-live controlado) | Infra PROD provisionada. Cargas iniciales PROD. Go-live Cliente A. Validar 24h. Go-live Cliente B. |
| **Semana 7–8+** | Fase 8 | Monitoreo activo, ajustes, conciliación semanal, retrospectiva. Power BI Workspace activado si es prioridad. |

> **Nota sobre semanas:** estas estimaciones asumen accesos disponibles desde el día 1 de cada semana. Demoras en accesos (firewall, autorización IT, UDT en SAP cloud) pueden extender cada fase en 1–2 semanas adicionales. Ver advertencia de duración al inicio del documento.

---

## I. MVP de 5 días

### MVP Cliente A — Dedicated Extractor (5 días)

| Día | Actividades | Entregable del día |
|---|---|---|
| **Día 1** | Provisionar Azure SQL + schemas + usuarios + roles. Deploy Ingest API DEV. Configurar Managed Identity. Verificar `GET /health`. | Azure SQL con schema completo. Ingest API respondiendo. |
| **Día 2** | Instalar agente en servidor Cliente A. Configurar conexión HANA. Validar `UpdateTS`. Ejecutar query de diagnóstico de watermark. | Agente conectado a HANA, loguea tablas disponibles. |
| **Día 3** | Carga inicial histórica (tablas maestros primero, luego documentos). Monitorear `audit.ingestion_event`. | `raw.*` de Cliente A poblado con histórico validado. |
| **Día 4** | Activar ciclo incremental. Crear factura de prueba en SAP. Verificar aparición en raw dentro de 60 minutos. Refresh stg activado. | Ciclo incremental funcionando. stg con datos correctos. |
| **Día 5** | Refresh dim/fact. Validar `fact.sales` vs reporte SAP. Instalar agente como servicio. Documentar runbook. | fact.sales coincide con SAP. Agente como servicio. |

### MVP Cliente B — Cloud Connector (5 días)

| Día | Actividades | Entregable del día |
|---|---|---|
| **Día 1** | Validar Service Layer (GET metadata, GET Invoices). Medir throughput. Deploy Ingest API DEV (puede reutilizar el de Cliente A si ya existe). | SL accesible, throughput medido, Ingest API operativo. |
| **Día 2** | Crear UDT `@DBI_SYNC_QUEUE` y `@DBI_SYNC_LOG`. Validar CRUD via SL. Crear TransactionNotification SP (si permitido). | UDTs operativos. TN probado sin bloquear SAP. |
| **Día 3** | Configurar Azure Function Timer. Probar flujo completo: crear documento SAP → TN → cola → Function → Ingest API → raw. | Ciclo completo de 1 documento probado end-to-end. |
| **Día 4** | Carga inicial (SL drain o CSV). Monitorear `audit.ingestion_event`. Verificar `initial_loaded=1` en checkpoint. | `raw.*` de Cliente B poblado con histórico. |
| **Día 5** | Refresh stg + dim + fact. Validar `fact.sales` vs SL. Documentar runbook. | fact.sales correcto para Cliente B. Runbook listo. |

---

## J. Decisiones abiertas

Las siguientes preguntas deben responderse **antes de codificar** los componentes correspondientes:

### Infraestructura

1. **¿DEV y PROD en la misma Azure Subscription o suscripciones separadas?**
   - Separadas → aislamiento de facturación y políticas, pero más overhead.
   - Misma → más simple para empezar; separar en Fase 7 si es necesario.

2. **¿Qué región Azure para los recursos?**
   - Depende de la ubicación del servidor HANA de Cliente A y del cloud SAP de Cliente B.
   - East US 2 / Brazil South / West Europe son candidatos frecuentes.

3. **¿Azure Function en Consumption Plan o Premium V2?**
   - Consumption: más barato, pero cold start puede afectar el timer de 5 minutos.
   - Premium V2: sin cold start, más caro. Necesario si el timer es crítico.

### Cliente A — Dedicated Extractor

4. **¿HDBODBC disponible en el servidor del cliente o se usa Sap.Data.Hana.Core?**
   - HDBODBC: requiere instalación del SAP HANA Client.
   - Sap.Data.Hana.Core: requiere validar licencia SAP con el cliente.

5. **¿`UpdateTS` tiene valores coherentes en el ambiente HANA del cliente?**
   - Si siempre es 0, el compound watermark cae back a UpdateDate + NaturalKey.
   - Validar antes de instalar (query diagnóstico en Checklist B).

6. **¿El servidor del agente es Windows o Linux?**
   - Impacta en el método de instalación como servicio y en el cifrado de credenciales locales.

7. **¿La carga inicial incluye todo el histórico o solo últimos N años?**
   - Acordar con el cliente la fecha desde `initial_load_from_date`.

### Cliente B — Cloud Connector

8. **¿El ambiente cloud de Cliente B permite crear UDTs via Service Layer?**
   - Si no: fallback a polling SL puro sin cola. Mayor lag pero funcional.
   - Confirmar con IT del cliente antes de diseñar el SP TN.

9. **¿Service Layer soporta ETag/If-Match para claim atómico?**
   - Si no: usar fallback `U_ProcessingOwner` + re-lectura.
   - Validar en el sandbox del cliente antes de codificar el claim.

10. **¿Cuál es el throughput real de SL del cliente sin throttling?**
    - Necesario para calcular si la carga inicial por SL es viable en < 48 horas.
    - Si < 100 req/min con datos de 5 años → CSV es obligatorio.

### Modelo de datos

11. **¿Los usuarios finales necesitan ver datos de múltiples sociedades consolidados?**
    - Si sí: el portal API debe soportar multi-company por usuario.
    - Si no: un usuario = una company (más simple para MVP).

12. **¿La moneda de reporte es siempre la moneda local de SAP o se necesita conversión?**
    - Conversión → requiere tabla de tipos de cambio (fuera de MVP inicial).
    - Solo local → MVP cubre el caso.

### Operación

13. **¿Quién gestiona el agente de Cliente A cuando el cliente necesita reiniciar el servidor?**
    - DataBision puede dar soporte remoto o el IT del cliente tiene el runbook.

14. **¿SLA mínimo aceptable para el portal?**
    - Para fijar nivel de alertas y tier de infraestructura.

---

## K. Qué NO construir todavía

Los siguientes módulos y funcionalidades están **explícitamente fuera del alcance** de este roadmap:

| Categoría | Qué se excluye | Cuándo considerar |
|---|---|---|
| **IA / ML** | Predicciones, anomalías, recomendaciones, OpenAI | Fase post-estabilización (después de 3 meses en producción) |
| **Microsoft Fabric** | Fabric lakehouses, Dataflows Gen2, OneLake | Solo si Azure SQL resulta insuficiente para el volumen |
| **Azure Data Factory** | Pipelines ADF, Integration Runtime | Evaluar si los SPs de refresh no escalan; no antes |
| **Power BI Embedded productivo** | Embed tokens en portal, RLS PBI, dataset publishing | Fase 3 del producto (posterior a este roadmap) |
| **RLS avanzado en Azure SQL** | Row-Level Security SQL nativo activo | Solo cuando multi-company por usuario sea requerimiento |
| **Módulos no ventas** | Compras, stock, contabilidad, producción, RRHH | Roadmap separado por módulo según demanda |
| **Contabilidad completa** | JDT1/OJDT, asientos, balance general | Módulo independiente con reglas contables propias |
| **Stock avanzado** | OINM, movimientos de inventario, valorización | Módulo independiente; requiere OINM con alto volumen |
| **Integración bidireccional** | Escribir de vuelta en SAP desde DataBision | Out of scope por definición — DataBision es solo lectura |
| **White-label avanzado** | Dominio propio del cliente (`reportes.acme.com`) | Infraestructura DNS + cert; post go-live |
| **Multi-idioma UI** | Inglés, portugués u otros idiomas en el portal | Post-MVP; solo español en v1 |
| **Exportación a Excel/PDF desde portal** | Descarga de reportes formateados | Fase siguiente; los datos ya están disponibles en fact.* |

---

## Apéndice 1 — Query de diagnóstico de watermark en HANA

Ejecutar en el ambiente HANA del Cliente A **antes de instalar el agente**, para validar que los campos de watermark son utilizables:

```sql
-- Validar UpdateTS en OINV
SELECT
    COUNT(*)                          AS total_docs,
    SUM(CASE WHEN "UpdateTS" = 0 THEN 1 ELSE 0 END) AS ts_zero_count,
    MAX("UpdateDate")                 AS max_update_date,
    MAX("UpdateTS")                   AS max_update_ts,
    MIN("CreateDate")                 AS min_create_date,
    MAX("CreateDate")                 AS max_create_date
FROM SBO_ACME_AR.OINV;   -- reemplazar con base real

-- Si ts_zero_count / total_docs > 90%: UpdateTS no es confiable
-- Decisión: usar solo UpdateDate + NaturalKey como watermark (lookback más amplio)

-- Validar granularidad de UpdateDate (si muchos docs tienen mismo UpdateDate)
SELECT "UpdateDate", COUNT(*) AS cnt
FROM SBO_ACME_AR.OINV
GROUP BY "UpdateDate"
ORDER BY cnt DESC
LIMIT 10;
-- Si el top 1 tiene miles de docs con el mismo UpdateDate:
-- el lookback debe ser ≥ 2 horas para capturar todos los cambios del día
```

---

## Apéndice 2 — Estimación de duración de carga inicial

| Tabla | Rows (estimado) | Tiempo SL (Mod. B) | Tiempo HANA (Mod. A) |
|---|---|---|---|
| OCRD | < 10k | 2–5 min | < 1 min |
| OITM | < 50k | 10–30 min | 1–3 min |
| OSLP | < 500 | < 1 min | < 1 min |
| OINV (5 años) | 50k–500k | 2–24 horas | 5–30 min |
| INV1 (5 años) | 250k–2.5M | 10–48 horas | 15 min–2 horas |
| ORIN (5 años) | 5k–50k | 20 min–3 horas | 1–5 min |
| RIN1 (5 años) | 25k–250k | 1–10 horas | 5–30 min |

**Regla de decisión para Modalidad B:** si el tiempo estimado total de SL drain > 24 horas → usar CSV bulk para la carga inicial de esa tabla y activar cola solo para incremental.

---
