# DataBision Dedicated Extractor — Technical Design

Status: **Diseño técnico — sin implementación.**
Modalidad A del documento maestro: agente local en infra del cliente que extrae SAP B1 HANA y empuja a **Supabase PostgreSQL vía DataBision Ingest API** (HTTPS POST JSON).

> **✅ ACTUALIZADO (2026-05-30 — resolución C-01 auditoría):**
> El destino del extractor NO es Azure SQL. El extractor envía batches comprimidos al **DataBision Ingest API** (`POST /api/ingest/sap-b1`), que persiste en **Supabase PostgreSQL** usando `INSERT ON CONFLICT`. Azure SQL queda documentado como opción Enterprise futura en `docs/azure-sql-staging-design.md`.
>
> Power BI no es el destino ni el núcleo del producto. Ver [ADR-002](adr/ADR-002-bi-layer.md).

---

## 1. Qué es DataBision Extractor Agent

Servicio autónomo que vive en la infraestructura del cliente, junto al servidor SAP B1 HANA. Responsable de:

- Conectarse a SAP HANA con credenciales locales.
- Mantener checkpoints por objeto (tabla) extraído.
- Ejecutar carga inicial histórica una sola vez por tabla.
- Ejecutar incremental cada 30/60 min con lookback adaptativo.
- Empujar batches comprimidos vía HTTPS al API de DataBision en Azure.
- Persistir cola offline cuando Azure no responde.
- Reportar heartbeat + telemetría al cloud.

**No** es un ETL completo. **No** transforma datos. **No** decide reglas de negocio. Es un "pipe" robusto, idempotente y observable entre HANA y Azure SQL.

### Principios de diseño

1. **Idempotencia absoluta.** Cualquier ejecución puede reprocesarse sin duplicar.
2. **Boundary-only.** No conoce reglas de negocio del cliente; copia, no interpreta.
3. **Watermark + lookback, nunca full table scan después de la inicial.**
4. **Falla local antes que falla silenciosa.** Si no puede empujar a Azure, encola; si no puede leer HANA, alerta.
5. **Cliente lo puede auditar.** Logs locales rotados, configuración legible, sin obfuscación.
6. **Versionable.** Cada agente reporta su versión; el cloud sabe qué corre cada cliente.

---

## 2. Arquitectura técnica

```
┌─────────────────────────── INFRA DEL CLIENTE ──────────────────────────┐
│                                                                          │
│   ┌──────────────────┐         ┌─────────────────────────────────────┐  │
│   │   SAP HANA       │◄────────┤  DataBision Extractor Agent         │  │
│   │   :30015         │  HANA   │  (.NET 8 Worker Service)            │  │
│   │   SBO_<COMPANY>  │   SQL   │                                     │  │
│   └──────────────────┘         │  ┌───────────────────────────────┐  │  │
│                                │  │ Scheduler (PeriodicTimer)     │  │  │
│                                │  └───────┬───────────────────────┘  │  │
│                                │          ▼                          │  │
│                                │  ┌───────────────────────────────┐  │  │
│                                │  │ Extraction Pipeline           │  │  │
│                                │  │ - watermark + lookback        │  │  │
│                                │  │ - paginate                    │  │  │
│                                │  │ - serialize + compress        │  │  │
│                                │  └───────┬───────────────────────┘  │  │
│                                │          ▼                          │  │
│                                │  ┌───────────────────────────────┐  │  │
│                                │  │ Azure Pusher                  │  │  │
│                                │  │ - HTTPS POST                  │  │  │
│                                │  │ - Polly retry + CB            │  │  │
│                                │  │ - offline queue on failure    │  │  │
│                                │  └───────┬───────────────────────┘  │  │
│                                │          │                          │  │
│                                │  ┌───────▼──────────────┐           │  │
│                                │  │ Local SQLite store   │           │  │
│                                │  │ - checkpoints        │           │  │
│                                │  │ - offline queue      │           │  │
│                                │  │ - last 30d run log   │           │  │
│                                │  └──────────────────────┘           │  │
│                                │                                     │  │
│                                │  ┌──────────────────────┐           │  │
│                                │  │ Serilog → rolling    │           │  │
│                                │  │ file (90 days)       │           │  │
│                                │  └──────────────────────┘           │  │
│                                └───────────────┬─────────────────────┘  │
└──────────────────────────────────────────────────┼──────────────────────┘
                                                    │
                                                    │ HTTPS gzip
                                                    │ + API key
                                                    ▼
┌──────────────────────────────── AZURE ─────────────────────────────────┐
│                                                                         │
│   ┌────────────────────────┐      ┌────────────────────────────┐          │
│   │ DataBision Ingest API  │─────►│  Supabase PostgreSQL       │          │
│   │ POST /api/ingest/sap-b1│      │  raw.* + ctl.* + audit.*   │          │
│   │ POST /heartbeat        │      │  INSERT ON CONFLICT         │          │
│   │ GET /config (agente)   │      └────────────────────────────┘          │
│   └────────────────────────┘                                              │
└─────────────────────────────────────────────────────────────────────────┘
```

### Componentes internos del agente

| Componente | Rol |
|---|---|
| `Scheduler` | Dispara extracción según frecuencia configurada por objeto |
| `ExtractionPipeline` | Lee checkpoint, ejecuta query HANA, paginá, serializa |
| `HanaClient` | Pool de conexiones, retry on transient errors |
| `AzurePusher` | POST a Ingest API con Polly (retry + circuit breaker) |
| `LocalStore` | SQLite: checkpoints, offline queue, run history |
| `HeartbeatService` | Ping a `/heartbeat` cada 5 min |
| `HealthCheck` | Endpoint local `:8081/health` (admin del cliente lo consume) |
| `ConfigService` | Lee `appsettings.json` + variables de entorno + opcionalmente refresca config remota |
| `Logger` | Serilog: archivo local rotado + sink HTTP al cloud |

---

## 3. Instalación como Windows Service

### Empaquetado

`dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true` produce:

- `DataBision.Extractor.exe` (single file, sin requerir .NET runtime instalado en el cliente)
- `appsettings.json` (config base)
- `appsettings.{Environment}.json` (override por ambiente cliente)
- Carpeta `data/` (vacía; SQLite se crea al primer run)
- Carpeta `logs/` (vacía; Serilog escribe aquí)

### Layout en disco

```
C:\DataBision\
├── DataBision.Extractor.exe
├── appsettings.json
├── appsettings.Customer.json   ← editado en cliente (gitignored upstream)
├── data\
│   └── extractor.db            ← SQLite
└── logs\
    └── extractor-YYYYMMDD.log  ← rotado diario, 90 días
```

### Instalación del servicio

```powershell
# Usuario administrador local con permiso "Log on as a service"
sc create DataBisionExtractor `
   binPath= "C:\DataBision\DataBision.Extractor.exe" `
   start= auto `
   DisplayName= "DataBision Extractor Agent" `
   obj= ".\DataBisionSvc" password= "<secret>"

sc description DataBisionExtractor "Extrae datos de SAP B1 HANA a DataBision Cloud"
sc failure DataBisionExtractor reset= 86400 actions= restart/60000/restart/60000/restart/300000

sc start DataBisionExtractor
```

- **Usuario dedicado** `DataBisionSvc` con permisos mínimos: leer `C:\DataBision`, escribir en `data/` y `logs/`, abrir socket saliente a HANA :30015 y Azure :443.
- **Recovery**: reiniciar el servicio en cualquier crash, hasta 3 veces, después esperar 5 min.

### Permisos firewall

- **Outbound:** HANA :30015 (LAN), Azure ingest API :443.
- **Inbound:** ninguno requerido. El health check local `:8081` es opcional y solo accesible desde localhost.

### Actualización

Política recomendada:

1. Publicación de nueva versión en blob storage privado de DataBision.
2. Agente pregunta a `/api/config/version` cada hora. Si hay versión nueva mayor: alerta a equipo de soporte (no auto-update).
3. Soporte coordina ventana con cliente, baja el zip, reemplaza binarios, reinicia servicio.

Auto-update está deliberadamente fuera del MVP — riesgo de actualizar y romper cliente sin ventana.

---

## 4. Instalación como Linux systemd

Para clientes con SAP B1 HANA en Linux (común — HANA corre en SUSE/RHEL).

### Empaquetado

`dotnet publish -c Release -r linux-x64 -p:PublishSingleFile=true -p:SelfContained=true`.

### Layout

```
/opt/databision/
├── DataBision.Extractor
├── appsettings.json
├── appsettings.Customer.json
├── data/
└── logs/
```

### Unidad systemd

`/etc/systemd/system/databision-extractor.service`:

```ini
[Unit]
Description=DataBision Extractor Agent
After=network.target

[Service]
Type=notify
User=databision
Group=databision
WorkingDirectory=/opt/databision
ExecStart=/opt/databision/DataBision.Extractor
Restart=on-failure
RestartSec=60
StartLimitInterval=86400
StartLimitBurst=10
KillMode=process
SyslogIdentifier=databision-extractor

# Hardening
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ReadWritePaths=/opt/databision/data /opt/databision/logs
ProtectHome=true
CapabilityBoundingSet=
AmbientCapabilities=

# Environment
EnvironmentFile=/etc/databision/extractor.env

[Install]
WantedBy=multi-user.target
```

### Activación

```bash
useradd --system --no-create-home --shell /usr/sbin/nologin databision
chown -R databision:databision /opt/databision
systemctl daemon-reload
systemctl enable --now databision-extractor
journalctl -u databision-extractor -f
```

### Diferencias con Windows

- Usuario `databision` no-login, sin home.
- `EnvironmentFile` para secretos (ver §9), permisos `0600` y `root:databision`.
- Logs van también a journald via `Type=notify` + `SyslogIdentifier`.
- Hardening systemd (`ProtectSystem=strict`) protege al sistema operativo de bugs del agente.

---

## 5. Conexión a SAP HANA — opciones técnicas

### 5.1 SAP HANA Client + ODBC

- **Driver:** "HDBODBC" provisto por SAP (instalable desde `hdbsetup`).
- **Disponible:** Windows + Linux.
- **.NET access:** `System.Data.Odbc` (built-in en .NET).
- **Connection string (DEV/LAB):**
  ```
  Driver={HDBODBC};ServerNode=hana-host:30015;UID=DBI_READER;PWD=...;CS=SBO_ACME;ENCRYPT=TRUE;sslValidateCertificate=FALSE
  ```
- **Connection string (PRODUCCIÓN):**
  ```
  Driver={HDBODBC};ServerNode=hana-host:30015;UID=DBI_READER;PWD=...;CS=SBO_ACME;ENCRYPT=TRUE;sslValidateCertificate=TRUE;sslTrustStore=/etc/ssl/certs/hana-ca.pem
  ```
- ⚠️ **`sslValidateCertificate=FALSE` solo se permite en DEV/LAB.** En producción debe validarse el certificado contra una CA conocida; o, si el cliente usa cert auto-firmado, se distribuye explícitamente vía `sslTrustStore` apuntando al CA pinneado. Cualquier excepción a esta regla requiere **aprobación formal por escrito del cliente** (riesgo MITM aceptado), archivada en el expediente del tenant.
- **Pros:** estándar, sin NuGet propietario, mismo código Windows/Linux.
- **Contras:** requiere instalar HANA Client en cada host (~150 MB), versión debe coincidir con servidor.

### 5.2 `Sap.Data.Hana.Core` (NuGet nativo)

- **Paquete:** `Sap.Data.Hana.Core.v2.1` (distribuido por SAP, no NuGet público hasta versión reciente; verificar licencia y términos antes de adoptar).
- **Sin necesidad de HDBODBC** en runtime.
- **Connection string (DEV/LAB):**
  ```
  Server=hana-host:30015;UserID=DBI_READER;Password=...;CurrentSchema=SBO_ACME;Encrypt=true;ValidateCertificate=false
  ```
- **Connection string (PRODUCCIÓN):**
  ```
  Server=hana-host:30015;UserID=DBI_READER;Password=...;CurrentSchema=SBO_ACME;Encrypt=true;ValidateCertificate=true;TrustStore=C:\DataBision\certs\hana-ca.pem
  ```
- ⚠️ **`ValidateCertificate=false` solo se permite en DEV/LAB.** Misma regla que §5.1 — en producción se valida la cadena de certificación o se pinnea el CA. Si el cliente exige aceptar cualquier cert (porque no quiere emitir uno público), requiere excepción formal por escrito.
- **Pros:** menos dependencias en cliente, API tipada (`HanaCommand`, `HanaParameter`).
- **Contras:** licenciamiento ambiguo según SAP partner; tamaño binarios; menos comunidad; **distribución / runtime aún no validados como solución multi-cliente** — adoptar solo cuando se cierre ese análisis.

### 5.3 Service Layer como fallback

- **Cuándo se usa en Modalidad A:** *solo* para recuperación puntual de un objeto cuya extracción SQL falló (ej.: fila con campo BLOB corrupto en SQL pero accesible por OData).
- **Nunca para carga masiva.** Ya documentado en doc maestro.

### 5.4 Nota: HDBCLI Python vs `Sap.Data.Hana.Core` (.NET)

"HDBCLI" es el nombre del paquete Python oficial de SAP (`pip install hdbcli`). Internamente llama a `libhdbsql` — el mismo runtime C que usa `Sap.Data.Hana.Core` (.NET).

DataBision Extractor es .NET. HDBCLI Python **no aplica directamente**. La correspondencia correcta de terminología es:

| Término común | .NET | Python |
|---|---|---|
| HANA ODBC | `System.Data.Odbc` + HDBODBC | `pyodbc` + HDBODBC |
| HANA nativo ("HDBCLI") | `Sap.Data.Hana.Core` | `hdbcli` (PyPI) |
| Service Layer | `HttpClient` OData | `requests` OData |

Para el agente .NET, la comparación "ODBC vs HDBCLI" se lee como "§5.1 vs §5.2". Si DataBision reescribiera el agente en Python en el futuro, `hdbcli` sería la opción natural frente a `pyodbc`.

**Diferencia operacional relevante:** `hdbcli` Python está disponible en PyPI público con términos de distribución más claros que `Sap.Data.Hana.Core` NuGet — uno de los tres bloqueadores del provider .NET (§5.2, §7).

### 5.5 Driver matrix

| Driver | Lenguaje | Plataforma | Bulk | Tipado | Tamaño dep. | Recomendación |
|---|---|---|---|---|---|---|
| **HDBODBC + System.Data.Odbc** | .NET | Win + Linux | Excelente | Débil (DataReader) | 150 MB | **MVP — default obligatorio** |
| `Sap.Data.Hana.Core` (≈ HDBCLI nativo .NET) | .NET | Win + Linux | Excelente | Fuerte | 40 MB | **Alternativa FUTURA** — solo tras validar distribución, licencia y runtime |
| `hdbcli` | Python | Win + Linux | Excelente | Fuerte | 40 MB | Solo si agente se reescribe en Python |
| HANA JDBC | Java | — | — | — | — | No aplica (.NET) |
| Service Layer OData | Cualquiera | Win + Linux | Pobre | Fuerte | 0 | Solo fallback puntual |

---

## 6. ODBC vs HDBCLI (nativo) vs Service Layer — comparación

La siguiente tabla usa "HDBCLI" como nombre genérico para el cliente nativo HANA (§5.4 explica la distinción Python/.NET). En contexto .NET: ODBC = §5.1, HDBCLI = §5.2.

| Dimensión | HANA SQL (ODBC) | HANA SQL (HDBCLI / nativo) | Service Layer OData |
|---|---|---|---|
| Throughput bulk | 10k–100k rows/s | 10k–100k rows/s | 100–300 rows/s |
| Latencia query | <50 ms | <50 ms | 200–2000 ms |
| Acceso a TODA la tabla | Sí | Sí | Sí ($top, paginación lenta) |
| Acceso a UDF y UDT | Sí | Sí | Sí (depende mapping) |
| Joins complejos | Sí | Sí | Limitado |
| Trabaja sin acceso a puerto BD | No | No | Sí (solo HTTP 50000) |
| Requiere instalación en cliente | Sí (HANA Client ~150 MB) | Sí (lib ~40 MB) | No |
| Estable ante upgrades SAP B1 | Alta | Alta | Media (endpoints cambian) |
| Telemetría / observabilidad | Buena | Buena | Pobre |
| Licenciamiento .NET | Estándar ODBC — claro | `Sap.Data.Hana.Core` — ambiguo | Service Layer license |
| Licenciamiento Python | `pyodbc` — libre | `hdbcli` PyPI — más claro que .NET | Service Layer license |
| Distribución en cliente | Driver SAP previo | Bundleable (pendiente validación) | Sin instalación |
| Debuggeable con herramientas | `isql`, `odbctest`, `hdbsql` | Solo via binding | Cualquier HTTP tool |
| Soporta SQL Server futuro | Sí (cambia driver ODBC) | No (solo HANA) | No (solo SAP B1 API) |

---

## 7. Recomendación técnica final de conexión

### MVP: HANA Client + HDBODBC + `System.Data.Odbc` (obligatorio)

Razones:

1. Driver oficial SAP, soportado y documentado.
2. Mismo código Windows + Linux.
3. Sin dependencias de NuGet propietario.
4. Performance equivalente al provider nativo para bulk.
5. Más fácil de troubleshootear con herramientas estándar (`isql`, `odbctest`, `hdbsql`).
6. Sin ambigüedad de licenciamiento en cliente.

### Alternativa futura: `Sap.Data.Hana.Core`

Solo se considera **después de validar formalmente** los tres bloqueadores actuales:

- **Distribución:** confirmar canal oficial de obtención del paquete (NuGet público vs distribución SAP) y permisos de redistribución a clientes finales.
- **Licencia:** validar términos de uso comercial multi-tenant; obtener confirmación escrita si fuese necesaria.
- **Runtime:** validar que el binario nativo (`libhdbsql.so` / `hdbsql.dll`) puede coexistir con instalaciones HANA Client del cliente sin conflicto de versiones.

Hasta que esos tres puntos estén cerrados, **no se adopta** el provider nativo en producción. La capa de acceso a HANA queda detrás de una interface (`IHanaQuery`) para que el cambio futuro sea local y sin afectar la pipeline.

### Service Layer

Como **fallback puntual** — solo en handlers de error específicos (recuperar un objeto cuya fila falla por blob corrupto en SQL pero accesible por OData). Nunca en el flujo principal de la corrida.

---

## 8. Configuración por cliente

### `appsettings.Customer.json` (editado en sitio)

```json
{
  "Tenant": {
    "Slug": "acme",
    "CompanyId": 42,
    "AgentId": "acme-extractor-01"
  },
  "Hana": {
    "Driver": "HDBODBC",
    "ServerNode": "10.20.30.40:30015",
    "Schema": "SBO_ACME",
    "Username": "DBI_READER",
    "PasswordSource": "env:DBI_HANA_PASSWORD",
    "Encrypt": true,
    "ValidateCertificate": true,
    "TrustStorePath": "/etc/databision/hana-ca.pem",
    "_comment_validate": "ValidateCertificate=false SOLO en DEV/LAB. En prod debe ser true y TrustStorePath debe apuntar al CA del HANA del cliente. Excepción requiere firma del cliente.",
    "CommandTimeoutSec": 300,
    "MaxPoolSize": 4
  },
  "Cloud": {
    "IngestBaseUrl": "https://api.databision.app",
    "ApiKeySource": "env:DBI_CLOUD_API_KEY",
    "TimeoutSec": 60,
    "MaxBatchRows": 5000,
    "CompressionLevel": "Optimal"
  },
  "Schedule": {
    "Default": "*/30 * * * *",
    "Overrides": {
      "OINV": "*/30 * * * *",
      "INV1": "follow:OINV",
      "ORIN": "*/30 * * * *",
      "RIN1": "follow:ORIN",
      "OCRD": "0 * * * *",
      "OITM": "0 * * * *",
      "OSLP": "0 4 * * *"
    }
  },
  "Lookback": {
    "DefaultHours": 2,
    "NightlyHours": 24,
    "NightlyTriggerHourLocal": 2,
    "MonthCloseDays": 7,
    "MonthCloseDayOfMonth": 1
  },
  "Retry": {
    "MaxAttempts": 3,
    "InitialDelayMs": 1000,
    "BackoffFactor": 2.0,
    "MaxDelayMs": 30000
  },
  "CircuitBreaker": {
    "FailureThreshold": 5,
    "OpenDurationSec": 300
  },
  "OfflineQueue": {
    "MaxBytes": 1073741824,
    "PathRelative": "data/queue"
  },
  "Logging": {
    "Local": {
      "Path": "logs/extractor-.log",
      "RetainedFileCountLimit": 90,
      "MinLevel": "Information"
    },
    "Cloud": {
      "Enabled": true,
      "BatchSize": 100,
      "FlushSec": 60
    }
  }
}
```

### Reglas

- Cliente tiene una copia editable de `appsettings.Customer.json` con su info.
- Secretos **nunca** aquí — siempre por env var.
- `follow:OINV` en INV1 significa "se extrae junto a OINV" (ver §19).
- Versionable: cualquier cambio se hace por commit del partner / soporte, no en producción manual.

---

## 9. Manejo de credenciales locales

### Credenciales en juego

1. **Password de HANA** del usuario `DBI_READER`.
2. **API key cloud** para hablar con DataBision.

### Windows — opciones

| Mecanismo | Pros | Contras | Recomendado |
|---|---|---|---|
| Variable de entorno del servicio | Simple, estándar | Visible vía `Get-Process`, `wmic` con admin | OK MVP |
| DPAPI (encripta con clave de la máquina) | No portable a otra máquina | Solo Windows | Producción |
| Windows Credential Manager | UI accesible | Sólo Windows, requiere desbloquear | Aceptable |
| Archivo encriptado con certificado | Auditable, rotable | Más complejo | Enterprise |

**Recomendación MVP Windows:** env var del servicio (`sc config DataBisionExtractor obj= "..."` no acepta env directos; se setean por `Environment` del servicio o por archivo `.env` cargado al inicio con permisos `0600`).

**Producción Windows:** DPAPI con scope `LocalMachine`. El agente desencripta al arrancar. Setup: tool de instalación encripta el password ingresado por admin del cliente.

### Linux — opciones

| Mecanismo | Pros | Contras |
|---|---|---|
| `EnvironmentFile=` de systemd | Estándar | Texto plano, requiere `chmod 600 root:databision` |
| `systemd-creds` | Encriptado con TPM | Solo distros recientes |
| HashiCorp Vault local | Robusto | Otra dependencia |
| Archivo + GPG | Portable | Requiere passphrase al boot |

**Recomendación MVP Linux:** `EnvironmentFile=/etc/databision/extractor.env` con permisos `0600 root:databision`. Solo root y el servicio leen.

### Regla absoluta

- **Nada en `appsettings.json` checkeable.**
- **Nada en logs.**
- **Nada en stack traces.** Tipo `SecureString` o redacción explícita.
- Si una credencial se ve filtrada en log, rotación inmediata.

---

## 10. Azure Key Vault desde ambiente del cliente

### TL;DR: no se usa desde el agente.

Razones:

1. **No hay Managed Identity en infra del cliente.** El agente debería autenticarse con un Service Principal (client_id + client_secret) → solo movimos la credencial de capa: ahora el secret del SP vive en el cliente y abre la puerta al vault.
2. **Latencia.** Cada query al vault son ~100 ms + dependencia de Internet para arrancar el servicio.
3. **Cliente lo ve como "nube ajena leyendo nuestras llaves"** — fricción comercial.
4. **Fallo de Internet** = el agente no puede arrancar (KV down → no creds → no extracción).
5. **Auditabilidad** del cliente sobre sus propias credenciales se pierde.

### Cuándo *sí* tiene sentido Azure Key Vault

- Para credenciales que viven en Azure y son consumidas por Azure Functions (Modalidad B) → ahí sí, con Managed Identity.
- Para emitir/rotar la API key del agente desde el lado cloud: el endpoint admin de DataBision **guarda** la nueva API key en KV del cloud, y se la entrega al cliente por canal seguro al onboarding.
- Para el connection string de Azure SQL (lo usa el Ingest API, no el agente).

### Patrón recomendado para Modalidad A

```
Onboarding → Admin DataBision genera API key → la guarda en KV cloud
          → la entrega al cliente vía canal seguro (TLS portal, no email)
          → cliente la setea como env var del servicio
          → agente nunca llama a KV
```

Rotación: admin genera nueva, cliente la actualiza en sitio, vieja se revoca tras 7 días de gracia.

---

## 11. Carga inicial histórica

### Alcance temporal: últimos 24 meses

Justificación:

- 24 meses cubre cierre anual N-1 + año actual + algo de comparativos.
- Históricos más viejos rara vez se consultan en BI ejecutivo.
- Reducir volumen baja onboarding de horas a minutos.
- Si el cliente requiere histórico mayor, se contrata como extensión.

### Estrategia por tipo de objeto

| Tipo | Filtro inicial |
|---|---|
| Documentos (OINV, ORIN) | `WHERE DocDate >= ADD_MONTHS(CURRENT_DATE, -24)` |
| Líneas (INV1, RIN1) | **Por chunks de DocEntry derivados de las cabeceras ya extraídas — NUNCA full scan** |
| Maestros (OCRD, OITM, OSLP) | full table (son chicos) |

### ⚠️ Reglas duras para líneas en carga inicial

INV1 y RIN1 **nunca** se cargan con un `SELECT *` sin filtro, ni siquiera durante la inicial. Razones: tamaño (millones de filas en clientes medianos), saturación de memoria del agente, locks largos en HANA, riesgo de timeout HTTP a Azure.

Patrón obligatorio:

1. Cabeceras extraídas paginadas (5000 cabeceras por página).
2. Por cada página de cabeceras: tomar el array de DocEntry de esa página.
3. Sub-chunkear el array si supera N (1000 DocEntry por sub-chunk).
4. Para cada sub-chunk: extraer `WHERE DocEntry IN (...)` y enviar.
5. Recién después de procesar las líneas de una página de cabeceras se avanza a la siguiente página.

Esto garantiza:
- Memoria acotada (5000 cabeceras × ~30 líneas promedio = 150k filas máximas en memoria).
- Cancelable a granularidad de página.
- Idempotencia trivial (re-extraer un sub-chunk no duplica por delete+insert por DocEntry).

### Procedimiento

1. Verificar pre-condiciones (conexión HANA, conexión Azure, tablas raw vacías para el tenant).
2. Confirmar con cliente: ventana de mantenimiento (típicamente off-hours).
3. Por cada tabla, ejecutar en orden:
   - OSLP (chico, sin dependencias).
   - OCRD (master).
   - OITM (master).
   - OINV (cabeceras paginadas; tras cada página, INV1 por chunks de DocEntry).
   - ORIN (cabeceras paginadas; tras cada página, RIN1 por chunks de DocEntry).
4. Cada tabla cabecera en modo "initial-load" (sin filtro de UpdateDate, con filtro de fecha histórica `DocDate >= ADD_MONTHS(CURRENT_DATE, -24)`).
5. Al terminar cada tabla: setear watermark inicial:
   - Modificación: `(MAX(UpdateDate), MAX(UpdateTS) en esa fecha, MAX(NaturalKey) en esa combinación)`.
   - Creación: `(MAX(CreateDate), MAX(CreateTS) en esa fecha, MAX(NaturalKey) en esa combinación)`.
6. Reconciliación al final (ver "Reconciliación diaria").
7. Marcar checkpoint como "InitialLoaded = true".

### Tiempos esperados (referencia)

- OSLP: <1 min
- OCRD (50k filas): 2–5 min
- OITM (100k filas): 5–10 min
- OINV (200k filas / 24m): 10–20 min
- INV1 (1M filas / 24m): 30–60 min
- ORIN (50k filas / 24m): 5–10 min
- RIN1 (200k filas / 24m): 15–30 min

**Total típico: 1.5–3 horas para cliente mediano.**

---

## 12. Extracción por bloques

### Estrategia: paginación por PK ordenada

No usar `LIMIT + OFFSET` (HANA recalcula desde cero cada page). En su lugar **keyset pagination**:

```sql
SELECT TOP 5000 *
FROM   "OINV"
WHERE  "DocDate" >= ADD_MONTHS(CURRENT_DATE, -24)
  AND  "DocEntry" > :last_doc_entry
ORDER  BY "DocEntry"
```

Donde `:last_doc_entry` es el último valor recibido en la página anterior. Primera page: `:last_doc_entry = 0`.

### Para incremental por watermark compuesto

`UpdateDate` es DATE (día). Para paginar dentro de un mismo día se necesita `UpdateTS` + natural key. Ver §14.0 para la justificación.

```sql
SELECT TOP 5000 *
FROM   "OINV"
WHERE  "UpdateDate" >= :window_start_date
  AND ("UpdateDate" < :window_end_date
       OR ("UpdateDate" = :window_end_date AND "UpdateTS" < :window_end_ts))
  AND  (    "UpdateDate" >  :last_update_date
         OR ("UpdateDate" = :last_update_date AND "UpdateTS" >  :last_update_ts)
         OR ("UpdateDate" = :last_update_date AND "UpdateTS" =  :last_update_ts
                                              AND "DocEntry"  >  :last_update_key) )
ORDER  BY "UpdateDate", "UpdateTS", "DocEntry"
```

Keyset compuesto `(UpdateDate, UpdateTS, DocEntry)`:
- `UpdateDate` (DATE) ordena por día.
- `UpdateTS` (INT/SECONDDATE) desempata dentro del día con granularidad de segundo.
- `DocEntry` desempata cuando dos cambios comparten exactamente fecha + hora (raro pero posible).

Para tablas maestras se usa la natural key correspondiente:
- OCRD → `CardCode`
- OITM → `ItemCode`
- OSLP → `SlpCode`

### Page size

- 5000 default (configurable por tabla).
- Más chico (1000) para tablas anchas (>100 columnas).
- Más grande (20000) si la red lo permite y el batch comprime bien.

### Métricas a registrar por bloque

- `rows_in_block`, `bytes_serialized`, `bytes_compressed`, `ms_query`, `ms_serialize`, `ms_upload`, `azure_response_code`.

---

## 13. Carga incremental cada 30/60 minutos

### Flujo de alto nivel

```
LOOP cada 30/60 min (por tabla, según schedule):
  1. acquire singleton lock por (tabla, tenant)
  2. checkpoint = read_checkpoint(tabla)
        -> (last_update_date, last_update_ts, last_update_key,
            last_create_date, last_create_ts, last_create_key)
  3. lookback = pick_lookback(now)  -- 2h / 24h / 7d (ver §16)
  4. window_start = (checkpoint.last_update_date, last_update_ts) menos lookback
     (calculado como una marca (date, ts) consistente)
  5. window_end   = now() - safety_buffer (60s)
        -> normalizado también a (date, ts)
  6. if checkpoint es "header" tipo (OINV, ORIN):
        // Dos queries: modificados + creados
        modified = extract_headers_by_update(
                       tabla, window_start, window_end, checkpoint.update_triple)
        created  = extract_headers_by_create(
                       tabla, window_start, window_end, checkpoint.create_triple)
        headers = dedupe_by_natural_key(modified, created)
        push_batches(headers)
        doc_entries = headers.docEntries
        if doc_entries.any():
            for chunk in chunks_of(doc_entries, 1000):
                lines = extract_lines(tabla_lineas, chunk)
                push_batches(lines, strategy="delete_by_parent_then_insert")
  7. if checkpoint es "master" tipo (OCRD, OITM, OSLP):
        modified = extract_master_by_update(tabla, window_start, window_end, ...)
        created  = extract_master_by_create(tabla, window_start, window_end, ...)
        rows = dedupe_by_natural_key(modified, created)
        push_batches(rows)
  8. nuevos watermarks (solo si cada batch confirmó 2xx):
        last_update_(date, ts, key) = max triple visto sobre UpdateDate/TS/key
        last_create_(date, ts, key) = max triple visto sobre CreateDate/TS/key
  9. update_checkpoint(triples, status=SUCCESS, rows=...)
 10. release_lock
```

### Singleton lock

Implementado como advisory lock en SQLite local: `INSERT INTO runtime_lock (table_name, started_at)` con UNIQUE constraint. Garantiza que dos ejecuciones del mismo objeto no se solapan si una corrida anterior demoró más que el intervalo.

### Safety buffer

Restamos 60 segundos a `now()` para evitar carrera: documentos que están siendo escritos en SAP en este instante pueden no aparecer aún. El próximo ciclo los captura por lookback.

---

## 14. Checkpoints por objeto

### 14.0 Por qué `UpdateDate` solo no alcanza

`UpdateDate` en muchas tablas SAP B1 (incluyendo OINV/ORIN/OCRD/OITM) **es de tipo `DATE`, con granularidad de día**, no de segundo. Confiar solo en `MAX(UpdateDate)` como watermark rompe en varios escenarios:

1. **Múltiples cambios el mismo día.** Una factura modificada 3 veces en el día tiene 3 estados con el mismo `UpdateDate`. `MAX(UpdateDate)` no permite paginar dentro del día — al avanzar a `UpdateDate > :ud` se saltean cambios pendientes del mismo día.
2. **Inserciones tras corrida parcial.** Si la corrida procesó 10k filas con `UpdateDate = 2026-05-15` pero falló a mitad, al reintentar `>= :watermark` re-extrae **todo** el día. Sin tie-breaker no hay forma de retomar desde el medio.
3. **Algunos patches/triggers no tocan `UpdateDate`** (raro pero documentado: cancelaciones parciales, actualizaciones por job interno SAP). Quedan invisibles al delta puro.
4. **Reloj del servidor SAP** puede tener desfasaje de segundos vs Azure. Un watermark con segundo-de-resolución podría perder filas por carrera.
5. **Inserciones nuevas con `CreateDate` ≠ `UpdateDate`.** En SAP B1, al insertar una fila `CreateDate = UpdateDate = today`, pero si se inserta con `UpdateDate` retroactiva (importaciones, migraciones), el delta por UpdateDate la pierde si su valor cae bajo el watermark.

### 14.1 Watermark compuesto: `(UpdateDate, UpdateTS, NaturalKey)`

SAP B1 HANA expone en tablas estándar:

- `UpdateDate` — DATE (día).
- `UpdateTS` — INT/SECONDDATE con la hora (HHMMSS o segundos). Granularidad de segundo.
- Clave natural — `DocEntry` (OINV/ORIN), `CardCode` (OCRD), `ItemCode` (OITM), `SlpCode` (OSLP).

El watermark se persiste como triple compuesto. Filtro de delta:

```sql
WHERE   "UpdateDate" >  :last_update_date
   OR ( "UpdateDate" = :last_update_date AND "UpdateTS"  >  :last_update_ts )
   OR ( "UpdateDate" = :last_update_date AND "UpdateTS"  =  :last_update_ts
                                          AND "<NaturalKey>" > :last_natural_key )
ORDER BY "UpdateDate", "UpdateTS", "<NaturalKey>"
```

Esto garantiza:

- Total ordering dentro de un día.
- Reanudación exacta tras fallo parcial.
- Idempotencia (re-correr desde el mismo triple no extrae duplicados).

### 14.2 Watermark para altas: `(CreateDate, CreateTS, NaturalKey)`

Las altas (INSERTs) deben rastrearse en paralelo a las modificaciones:

- `CreateDate`, `CreateTS` también existen como pareja en B1 HANA.
- Una fila nueva con `UpdateDate < watermark_update` pero `CreateDate > watermark_create` debe ser capturada.
- Esto ocurre con importaciones masivas, migraciones de cliente, o cuando un trigger fija `UpdateDate` retroactivamente.

Solución: además del watermark de modificación, mantener watermark de creación. En cada corrida se ejecutan **dos queries**:

1. Query "modified since": filtra por `(UpdateDate, UpdateTS, key) > last_update_*`.
2. Query "created since": filtra por `(CreateDate, CreateTS, key) > last_create_*`.
3. Unión de resultados deduplicada por natural key (la fila más reciente gana).

### 14.3 Estructura del checkpoint

| Campo | Tipo | Notas |
|---|---|---|
| `tenant_slug` | VARCHAR(50) | Identidad del tenant |
| `object_name` | VARCHAR(50) | OINV, INV1, OCRD, … |
| `last_update_date` | DATE | Última `UpdateDate` procesada con éxito |
| `last_update_ts` | INT | `UpdateTS` que acompaña a `last_update_date` |
| `last_update_key` | VARCHAR(50) | Natural key (DocEntry como string, CardCode, ItemCode, SlpCode) |
| `last_create_date` | DATE | Watermark de altas |
| `last_create_ts` | INT | |
| `last_create_key` | VARCHAR(50) | |
| `last_run_started_at` | DATETIME2 | UTC |
| `last_run_ended_at` | DATETIME2 NULL | UTC |
| `last_run_status` | VARCHAR(16) | SUCCESS / FAILED / PARTIAL / RUNNING |
| `last_run_id` | BIGINT FK | a `ctl.extraction_run` |
| `last_error_code` | VARCHAR(50) NULL | |
| `last_error_message` | NVARCHAR(MAX) NULL | |
| `rows_extracted_total` | BIGINT | Acumulado desde inicial |
| `initial_loaded` | BIT | |
| `initial_loaded_at` | DATETIME2 NULL | |
| **PK** | (tenant_slug, object_name) | |

> El triple `(last_update_date, last_update_ts, last_update_key)` reemplaza al antiguo escalar `last_watermark`. Cualquier referencia anterior a `last_watermark` en este documento debe leerse como ese triple.

### 14.4 Reglas

- **Watermark se avanza solo después de batch upserted con 2xx del API.** El triple se actualiza atómicamente con el último valor del último batch confirmado, no con el `window_end` planeado.
- Si la corrida es PARTIAL: el triple queda en el último valor confirmado → próximo ciclo retoma exacto desde ahí (con lookback adicional).
- Status RUNNING con `last_run_started_at > 1 hora atrás` → corrida zombi; el siguiente arranque la cierra como FAILED y procede.
- `initial_loaded = false` bloquea incremental hasta que la inicial completa.
- Al cierre de la inicial: `last_update_date/ts/key` = MAX observado en cabeceras; `last_create_date/ts/key` = MAX observado de `CreateDate/TS/key`.

---

## 15. Checkpoints locales y cloud

### Local (SQLite del agente)

- Tabla `local_checkpoint` con la misma estructura que arriba.
- Es la **fuente de verdad operacional**. El agente nunca consulta cloud para decidir desde dónde extraer.
- Beneficio: si Azure cae 6 horas, el agente sigue acumulando datos en la cola offline sin perder el rastro.

### Cloud (Azure SQL `ctl.extraction_checkpoint`)

- Refleja el estado del local con un lag corto.
- Se actualiza desde el Ingest API después de cada batch exitoso.
- Permite a soporte/admin ver el estado de cada tenant sin pedirle al cliente.

### Reconciliación entre ambos

- Al startup del agente: leer cloud, comparar triples `(update_date, update_ts, update_key)` y `(create_date, create_ts, create_key)` con los locales. **Loguear divergencia** pero no actuar (el local manda).
- Si el cloud está más adelantado que el local en el triple: warning crítico — apunta a corrupción del local o agente clonado en otra máquina.
- Si local está dañado/borrado: se puede recuperar desde cloud (modo recovery, ver §41).

---

## 16. Lookback window por niveles

### Tres niveles, decididos por reloj local

| Nivel | Cuándo aplica | Lookback |
|---|---|---|
| **Normal** | Resto del día (default) | **2 horas** |
| **Nocturno** | Una ejecución por noche, hora local ~02:00 | **24 horas** |
| **Cierre de mes** | Primer día del mes calendario (1×) | **7 días** |
| **Cierre de año** (opcional) | 2 de enero (1×) | **30 días** |

### Lógica

```
function pick_lookback(now_local, table):
    if first_run_of_month_for(table):
        return 7 days
    if first_run_after(02:00 local) and not_already_done_today(table):
        return 24 hours
    return 2 hours
```

### Por qué tres niveles

- **2h:** captura cambios recientes; cero costo extra; cubre la mayoría.
- **24h:** atrapa edits del día completo (factura emitida en mañana y modificada en tarde con UpdateDate desfasada por carga del servidor SAP).
- **7d (cierre mes):** los usuarios contables suelen editar documentos viejos al cierre — ajustes, anulaciones, recálculos.
- **30d (cierre año):** opcional, cubre reapertura excepcional.

### Por qué no siempre 7d

- Costo de query proporcional al rango.
- Volumen de upserts crece. Mayoría de filas reprocesadas no cambian → trabajo desperdiciado.
- 2h es prácticamente gratis y resuelve 99% de los casos.

---

## 17. Cómo evitar leer todo SAP cada ciclo

Resumen práctico:

1. **Watermark estricto + lookback acotado.** Nunca query sin filtro temporal en operación normal.
2. **Keyset pagination** (no OFFSET), evita full scan en queries paginadas.
3. **Maestros con frecuencia baja.** OCRD/OITM se extraen cada 1h, no cada 30m.
4. **OSLP nightly.** Tabla chica, full refresh nocturno alcanza.
5. **Líneas siempre derivadas de cabeceras**, nunca scan completo de INV1/RIN1.
6. **Índices HANA.** Validar al onboarding que `UpdateDate` está indexado en OINV, ORIN, OCRD, OITM. Si no lo está, sugerir al partner crearlo (no-op para usuarios SAP).
7. **No JOINs costosos.** Cada query toca máximo una tabla; los joins se hacen en Azure SQL en capa staging.

---

## 18. Extracción de cabeceras

Cabeceras = tablas con `(UpdateDate, UpdateTS)` y `(CreateDate, CreateTS)` confiables (OINV, ORIN, OCRD, OITM). Ver §14.0 para la razón del watermark compuesto.

### Query "modified since" — incremental por actualización

```sql
SELECT TOP 5000
  "DocEntry", "DocNum", "CardCode",
  "DocDate", "DocDueDate",
  "DocTotal", "DocStatus", "Canceled", "CancelDate",
  "SlpCode", "ObjType",
  "CreateDate", "CreateTS",
  "UpdateDate", "UpdateTS"
FROM "OINV"
WHERE
       (    "UpdateDate" >  :window_start_date
         OR ("UpdateDate" = :window_start_date AND "UpdateTS" >= :window_start_ts) )
  AND  (    "UpdateDate" <  :window_end_date
         OR ("UpdateDate" = :window_end_date   AND "UpdateTS" <  :window_end_ts) )
  AND  (    "UpdateDate" >  :last_update_date
         OR ("UpdateDate" = :last_update_date AND "UpdateTS" >  :last_update_ts)
         OR ("UpdateDate" = :last_update_date AND "UpdateTS" =  :last_update_ts
                                              AND "DocEntry"  >  :last_update_key) )
ORDER BY "UpdateDate", "UpdateTS", "DocEntry";
```

### Query "created since" — incremental por alta

```sql
SELECT TOP 5000
  "DocEntry", "DocNum", "CardCode",
  "DocDate", "DocDueDate",
  "DocTotal", "DocStatus", "Canceled", "CancelDate",
  "SlpCode", "ObjType",
  "CreateDate", "CreateTS",
  "UpdateDate", "UpdateTS"
FROM "OINV"
WHERE
       (    "CreateDate" >  :window_start_date
         OR ("CreateDate" = :window_start_date AND "CreateTS" >= :window_start_ts) )
  AND  (    "CreateDate" <  :window_end_date
         OR ("CreateDate" = :window_end_date   AND "CreateTS" <  :window_end_ts) )
  AND  (    "CreateDate" >  :last_create_date
         OR ("CreateDate" = :last_create_date AND "CreateTS" >  :last_create_ts)
         OR ("CreateDate" = :last_create_date AND "CreateTS" =  :last_create_ts
                                              AND "DocEntry"  >  :last_create_key) )
ORDER BY "CreateDate", "CreateTS", "DocEntry";
```

### Deduplicación

Los resultados de ambas queries se unen y deduplican por `DocEntry`. En caso de aparecer en ambas (fila nueva del día con cambio posterior), gana la versión cuyo `(UpdateDate, UpdateTS)` es mayor.

### Columnas extraídas

- Toda columna estándar relevante para reporting (no `SELECT *` en producción; lista explícita por tabla y versionada).
- UDF (`U_*`) excluidas en MVP; agregables por tabla en fase 2.
- **Obligatorias para watermark:** `UpdateDate, UpdateTS, CreateDate, CreateTS` además de la natural key.

### Resultado: lista de cabeceras + sus DocEntry

El array de DocEntry alimenta la extracción de líneas en chunks (§19).

---

## 19. Extracción de líneas asociadas

INV1 y RIN1 no tienen `UpdateDate`/`UpdateTS` confiables (la columna existe pero no siempre se actualiza junto a la cabecera). Se extraen **siempre por rangos/chunks de DocEntry derivados de las cabeceras** — nunca por filtro temporal propio ni full scan.

### Estrategia obligatoria: chunks por DocEntry

```sql
SELECT
  "DocEntry", "LineNum",
  "ItemCode", "Dscription",
  "Quantity", "Price", "LineTotal",
  "WhsCode", "VatGroup", "VatPrcnt",
  "Currency", "Rate"
FROM "INV1"
WHERE "DocEntry" IN ( :doc_entries_chunk )
ORDER BY "DocEntry", "LineNum";
```

### Reglas duras de chunking

1. **Tamaño máximo del IN list:** 1000 DocEntry por sub-query. Mayor a esto, HANA degrada y el query plan empeora.
2. **Nunca se ejecuta `SELECT * FROM INV1`** sin filtro de DocEntry. Ni en inicial, ni en incremental, ni en recovery.
3. **Si el conjunto de cabeceras cambiadas en una corrida tiene >1000 DocEntry:** se sub-chunkea (chunks de 1000) y se envían en serie (no en paralelo, para no saturar memoria del agente ni la red).
4. **Por cada chunk:** se hace push del batch antes de extraer el siguiente. Failover de un chunk no contamina el siguiente.

### Schedule

INV1 está marcado como `follow:OINV` en el config — significa que su ciclo se dispara **automáticamente al final** de la corrida exitosa de OINV. No tiene su propio scheduler. Mismo para RIN1 con ORIN.

---

## 20. Manejo de INV1/RIN1 sin UpdateDate propio

### El problema

- INV1 *tiene* columna `UpdateDate`/`UpdateTS` (heredadas de B1), pero **no siempre se actualizan** cuando la cabecera cambia.
- En particular: edits que solo tocan campos de cabecera (DocStatus, fecha de pago, cancelación) **no tocan `UpdateDate` de las líneas**.
- Filtrar líneas por su propio `UpdateDate`/`UpdateTS` → se pierden líneas asociadas a cabeceras cambiadas.

### La solución

- Watermark de líneas se sigue **desde la cabecera**, nunca desde la línea.
- Cada vez que una cabecera entra al delta, **todas** las líneas de ese DocEntry se vuelven a leer por chunk.
- No se mantiene watermark separado para INV1/RIN1.
- Costo: redundancia (líneas re-leídas cuando solo cambió la cabecera). Aceptable.

### Extracción por chunks de DocEntry (§19)

- IN list con máximo 1000 DocEntry por sub-query.
- Sub-chunks en serie (no paralelos).
- Push del batch tras cada sub-chunk antes de extraer el siguiente.
- **Nunca** `SELECT * FROM INV1/RIN1` sin filtro.

### Excepción: backfill

Si por bug el agente no extrajo líneas de cabeceras que sí extrajo: existe modo `repair --lines-for-headers` que itera todas las cabeceras en Azure SQL **por rangos de DocEntry** (chunks de 1000) y vuelve a extraer líneas de SAP. Nunca se hace full scan en este modo tampoco.

---

## 21. Estrategia de líneas — delete+insert vs upsert

### Opción A: delete+insert por DocEntry dentro de transacción (RECOMENDADA MVP)

Por cada DocEntry afectado en el batch:

```sql
BEGIN TRAN;
  DELETE FROM raw.INV1 WHERE _company_id = @cid AND DocEntry = @docEntry;
  INSERT INTO raw.INV1 (...) VALUES (...);  -- todas las líneas
COMMIT;
```

**Pros:**
- Maneja casos donde una línea se elimina en SAP (raro pero pasa).
- No requiere PK compuesta exacta en raw.
- Idempotente trivialmente.

**Contras:**
- Latch en raw.INV1 durante delete+insert (microsegundos).
- Cada DocEntry es una transacción → más overhead que MERGE bulk.

### Opción B: MERGE por (DocEntry, LineNum)

```sql
MERGE raw.INV1 AS tgt
USING #tmp_INV1 AS src
   ON tgt._company_id = src._company_id
  AND tgt.DocEntry    = src.DocEntry
  AND tgt.LineNum     = src.LineNum
WHEN MATCHED THEN UPDATE SET ...
WHEN NOT MATCHED BY TARGET THEN INSERT (...);
```

**Pros:**
- Más rápido para volúmenes grandes.
- Bulk friendly.

**Contras:**
- No detecta líneas eliminadas (LineNum borrado en SAP queda huérfano en raw).
- Requiere paso adicional: `DELETE WHERE NOT IN (...)` por DocEntry → complejidad.

### Recomendación MVP

**Opción A: delete+insert por DocEntry.**

- Volumen típico: 5–50 líneas por DocEntry → delete+insert es despreciable.
- Simplicidad gana en MVP.
- Migrar a Opción B + lógica de "delete missing" solo si profiling muestra problema.

---

## 22. Upsert en Azure SQL

### Patrón estándar (cabeceras y maestros)

```sql
-- Server side, en el Ingest API:

-- 1. Recibe batch JSON, parsea a tipos correctos
-- 2. BULK INSERT a tabla temp
CREATE TABLE #tmp_OINV (...);
BULK INSERT #tmp_OINV FROM @json;

-- 3. MERGE
MERGE raw.OINV AS tgt
USING #tmp_OINV AS src
   ON tgt._company_id = src._company_id
  AND tgt.DocEntry    = src.DocEntry
WHEN MATCHED AND (
        src."UpdateDate" > tgt."UpdateDate"
     OR (src."UpdateDate" = tgt."UpdateDate" AND src."UpdateTS" >= tgt."UpdateTS")
   ) THEN
   UPDATE SET
     tgt."DocNum"        = src."DocNum",
     tgt."CardCode"      = src."CardCode",
     -- ...
     tgt."UpdateDate"    = src."UpdateDate",
     tgt."UpdateTS"      = src."UpdateTS",
     tgt._ingested_at    = SYSUTCDATETIME(),
     _batch_id           = @batch_id
WHEN NOT MATCHED BY TARGET THEN
   INSERT (_company_id, DocEntry, DocNum, CardCode, ...,
           CreateDate, CreateTS, UpdateDate, UpdateTS,
           _ingested_at, _batch_id)
   VALUES (@cid, src.DocEntry, src.DocNum, src.CardCode, ...,
           src."CreateDate", src."CreateTS", src."UpdateDate", src."UpdateTS",
           SYSUTCDATETIME(), @batch_id);

-- 4. Confirmar
COMMIT;
```

### Reglas críticas

- Condición compuesta `src.UpdateDate > tgt.UpdateDate OR (igual AND src.UpdateTS >= tgt.UpdateTS)` — evita revertir un update reciente con uno viejo si los batches llegan desordenados. La granularidad fina la da `UpdateTS`.
- **No DELETE** en raw para cabeceras. Cancelaciones se reflejan en `Canceled='Y'` (§23).
- Cada upsert dentro de transacción.
- Métricas: rows_inserted, rows_updated, ms_merge.

### Performance

- Índice clustered: `(_company_id, DocEntry)` para OINV/ORIN; `(_company_id, CardCode)` para OCRD; `(_company_id, ItemCode)` para OITM.
- Índice non-clustered: `UpdateDate` para queries staging.
- BULK INSERT a temp evita lock escalation en raw.

---

## 23. Manejo de cancelaciones

### En SAP B1

Cancelar una factura no la borra. Crea uno o más documentos relacionados:

- `Canceled = 'Y'` en la cabecera original.
- `CancelDate` se llena.
- Se genera un documento "cancellation" linkeado (`ObjType` distinto).
- `DocStatus` puede pasar a `C` (closed).

### En el extractor

- Detectamos la cancelación porque `UpdateDate` de la cabecera cambia → entra al delta.
- Upsert estándar refleja `Canceled = 'Y'` en raw.OINV.
- La capa staging interpreta `Canceled = 'Y' OR DocStatus = 'C'` como "no contar en ventas activas".

### Líneas

- Las líneas de un doc cancelado **no se borran** en SAP; quedan con la cabecera.
- En raw: delete+insert vuelve a traerlas igual.
- En staging: filtrar por `Canceled = 'N'` antes de sumar.

---

## 24. Manejo de documentos cerrados

- `DocStatus = 'C'` (Closed) → factura totalmente pagada o cancelada.
- `DocStatus = 'O'` (Open) → pendiente de pago.
- Cambio de O→C dispara UpdateDate → entra al delta normal.
- Reportes de cobranza filtran por DocStatus = 'O' AND Canceled = 'N'.

No requiere lógica especial en extractor; sí en staging.

---

## 25. Manejo de notas de crédito

### Tablas

- **ORIN** (cabecera) + **RIN1** (líneas) — espejo de OINV/INV1.
- Estructura idéntica de columnas relevantes.

### Tratamiento en raw

- Mismas reglas que OINV/INV1: cabecera por UpdateDate, líneas derivadas.

### Tratamiento en staging (referencia, no MVP)

- Notas de crédito son ventas con signo negativo en reportes consolidados.
- Vinculación con factura original: `OINV.DocEntry` referenciado desde `ORIN` vía `RDR1.BaseRef` o `ORIN.U_BaseDoc` (depende del cliente).

---

## 26. Manejo de maestros

### OCRD (Business Partners)

- Filtro: `UpdateDate >= watermark_start`.
- Frecuencia: 1h.
- Volumen típico: cambios diarios = decenas, raro centenas.
- Pueden venir vendors mezclados (`CardType = 'S'`) y clientes (`CardType = 'C'`). En staging se separan, en raw se guardan juntos.

### OITM (Items)

- Filtro: `UpdateDate >= watermark_start`.
- Frecuencia: 1h.
- Volumen típico: cambios diarios = decenas.
- Stock (`OnHand`, `IsCommited`) cambia con cada movimiento → puede ser muy ruidoso. En MVP **no se extraen estos campos** desde OITM; el stock real se construye desde OINM (fase futura).

### OSLP (Salespersons)

- Tabla pequeña (decenas a centenas de filas).
- Filtro: ninguno (full snapshot nocturno).
- Frecuencia: 1× por día (04:00 hora local).
- Estrategia: TRUNCATE raw.OSLP del tenant + INSERT all. Aceptable por tamaño.

---

## 27. Manejo de stock — fase futura

**No incluido en MVP.**

Diseño futuro (referencia):

- Tabla OINM (Inventory Transactions) registra cada movimiento.
- Stock punto-en-el-tiempo = `SUM(InQty - OutQty)` por item/almacén.
- Volumen alto: OINM crece millones de filas.
- Extracción incremental por `CreateDate` (no UpdateDate — OINM rara vez se modifica).
- Reportería de stock requiere materialized view o snapshot diario.

---

## 28. Logs locales

### Sink: Serilog → archivo rotado

- Path: `logs/extractor-YYYYMMDD.log`.
- Rotación: diario, retención 90 días.
- Formato: JSON line (parseable).
- Nivel default: Information. Configurable a Debug en troubleshooting.

### Qué se loguea localmente

- Inicio/fin de cada corrida con `run_id`.
- Cada batch: tabla, página, rows, ms.
- Errores con stack trace.
- Conexión HANA: connect / disconnect, errores de auth.
- Conexión Azure: status code, latencia.
- Heartbeats.

### Qué NO se loguea

- Contenido de filas extraídas (PII).
- Passwords o API keys (siempre redactados).
- Connection strings completos (host visible, password no).

### Audit del cliente

El cliente puede abrir cualquier `.log` y ver qué hizo el agente. Transparencia.

---

## 29. Logs cloud

### Sink: Serilog HTTP sink → Ingest API `/api/logs`

- Batch async cada 60s o 100 entries.
- Solo eventos `Warning+` por default (configurable).
- Si Azure no responde: log queda en archivo local; no se pierde.

### Qué llega al cloud

- Errores.
- Resúmenes de corrida (no detalle batch a batch).
- Heartbeats.
- Métricas: rows/run, ms/run, lag observado.

### Storage cloud

- Tabla `ops.agent_log` en base operativa de DataBision (no en raw del tenant).
- Indexado por `tenant_slug` + `timestamp`.
- Retención: 90 días hot, 1 año warm (Blob Parquet).

### Dashboards

- Soporte interno ve salud de todos los agentes en un solo lugar.
- Per-cliente: lag, errores 24h, throughput.

---

## 30. Reintentos

### Política

Implementado con **Polly** sobre el HttpClient hacia Azure y sobre las queries HANA.

```
Retry policy:
  - max_attempts: 3
  - initial_delay: 1s
  - backoff: exponential, factor 2
  - jitter: ±20%
  - retry_on:
       network errors (DNS, timeout, connection refused)
       HTTP 5xx
       HTTP 429 (con Retry-After header)
       HANA transient codes (depend on driver)
  - DO NOT retry on:
       HTTP 4xx (excepto 408, 429)
       HANA authentication errors
       JSON serialization errors
```

### Granularidad

- Reintento por **batch**, no por toda la corrida.
- Si un batch falla 3 veces: se persiste en cola offline (§32) y se continúa con el siguiente.
- La corrida marca status PARTIAL si algún batch quedó en cola.

---

## 31. Circuit breaker

Implementado con **Polly**.

```
CircuitBreaker:
  - failure_threshold: 5 fallos consecutivos
  - sampling_duration: 60s
  - open_duration: 300s (5 min)
  - half_open_test: 1 batch
```

### Comportamiento

- **Closed:** operación normal.
- **Open:** después de 5 fallos seguidos, el agente deja de intentar Azure por 5 min. Continúa extrayendo HANA y persistiendo en cola offline.
- **Half-Open:** después de 5 min, prueba un batch; si OK → Closed; si falla → Open de nuevo.

### Beneficios

- No martillea al Ingest API cuando está caído.
- Cliente sigue extrayendo HANA — no se pierden cambios.
- Recuperación automática.

---

## 32. Cola offline local

### Por qué existe

Si Azure cae 30 min – 24 h, el agente debe seguir extrayendo y guardar los batches para enviarlos cuando Azure vuelva.

### Implementación

- Carpeta `data/queue/` con un archivo por batch fallido.
- Nombre: `{timestamp}_{table}_{batch_id}.json.gz` (comprimido).
- Metadata en SQLite: tabla `offline_batch (id, table, file_path, rows, created_at, last_attempt_at, attempt_count, status)`.

### Drenaje

Cuando el circuit breaker vuelve a Closed:

1. Background task lee cola por orden FIFO.
2. Reenvía cada batch a Azure.
3. En 2xx: borra archivo + marca status='SENT'.
4. En fallo: incrementa attempt_count; tras N intentos (default 10), marca 'DEAD' y alerta.

### Límite

- Tamaño máximo de cola: 1 GB default (config).
- Si se alcanza: agente para extracción y alerta crítica.
- Política conservadora: prefiero parar el extractor a llenar el disco del cliente.

---

## 33. Modo dry-run

Flag `--dry-run` en arranque (o setting en config).

### Comportamiento

- Lee config, conecta HANA, ejecuta queries reales.
- **No** envía batches a Azure.
- **No** actualiza checkpoint.
- Loguea: "DRY-RUN: would push N rows, batch size X, estimated bytes Y".
- Útil para:
  - Validar conectividad en onboarding.
  - Estimar volumen antes de inicial.
  - Troubleshoot sin riesgo.

### Variante: `--dry-run --simulate-azure`

- Hace POST a Azure con header `X-DataBision-DryRun: true`.
- Ingest API valida pero no escribe a raw.
- Útil para validar tipos y esquemas end-to-end.

---

## 34. Health check

### Endpoint local

- HTTP `:8081/health` (solo escucha en `127.0.0.1`).
- GET `/health` → JSON:

```json
{
  "status": "Healthy",
  "agentId": "acme-extractor-01",
  "version": "1.0.3",
  "uptime": "2d 14h",
  "lastRun": {
    "table": "OINV",
    "endedAt": "2026-05-15T14:30:00Z",
    "status": "Success",
    "rows": 142
  },
  "hana": { "reachable": true, "lastQueryMs": 87 },
  "azure": { "reachable": true, "circuitState": "Closed" },
  "offlineQueueSize": 0,
  "checkpoints": [
    { "table": "OINV", "watermark": "2026-05-15T14:28:33Z", "lagMin": 2 },
    { "table": "OCRD", "watermark": "2026-05-15T14:00:00Z", "lagMin": 30 }
  ]
}
```

### Códigos

- `200 Healthy` — todo OK.
- `503 Degraded` — circuit breaker abierto o cola offline > umbral.
- `500 Unhealthy` — no conecta HANA o config inválida.

### Uso

- Admin del cliente puede consultarlo manualmente.
- Sistemas de monitoreo del cliente (Zabbix, Nagios, etc.) pueden polear.
- DataBision soporte lo usa en sesiones de troubleshoot.

---

## 35. Configuración de frecuencia por objeto

### Esquema cron en config

```json
"Schedule": {
  "Default": "*/30 * * * *",
  "Overrides": {
    "OINV":  "*/30 * * * *",       // cada 30 min
    "INV1":  "follow:OINV",        // junto a OINV
    "ORIN":  "*/30 * * * *",
    "RIN1":  "follow:ORIN",
    "OCRD":  "0 * * * *",          // cada hora en punto
    "OITM":  "0 * * * *",
    "OSLP":  "0 4 * * *"           // diario 04:00 local
  }
}
```

### Tier comercial

Frecuencia ajustable por cliente según contrato:

| Tier | OINV/ORIN | OCRD/OITM | OSLP |
|---|---|---|---|
| Standard | 60 min | 4h | 24h |
| Pro | 30 min | 1h | 24h |
| Premium | 15 min | 30 min | 4h |

Default MVP: tier Pro.

### Concurrencia

- Distintas tablas pueden correr en paralelo (hasta `MaxConcurrency` configurable, default 2).
- Misma tabla nunca concurrente (singleton lock §13).
- HANA pool con 4 conexiones default.

---

## 36. Refresh futuro de modelos BI

**No implementado en MVP.**

Esquema futuro:

1. Después de una corrida exitosa con `rows_total > 0`:
   - Agente reporta a `/api/sync/completed`.
2. Ingest API encola refresh:
   - Job `refresh_staging` en Azure SQL Agent o Azure Function.
3. Tras staging refreshed:
   - Dispara refresh de dataset Power BI vía REST API.
4. Telemetría: tiempo end-to-end (HANA → BI).

Para MVP: solo el paso 1 (reportar fin de corrida exitosa). Refresh de staging/BI manual o por job time-based.

---

## 37. Tablas Azure SQL requeridas

### Schema `ctl` (control plane, una vez en cada DB tenant)

#### `ctl.extraction_run`

| Columna | Tipo | Notas |
|---|---|---|
| run_id | BIGINT IDENTITY PK | |
| tenant_slug | VARCHAR(50) | |
| agent_id | VARCHAR(100) | |
| started_at | DATETIME2 | UTC |
| ended_at | DATETIME2 NULL | UTC |
| status | VARCHAR(16) | RUNNING / SUCCESS / FAILED / PARTIAL |
| trigger | VARCHAR(20) | SCHEDULE / MANUAL / RECOVERY |
| rows_total | INT | Suma de tablas |
| error_count | INT | |
| extractor_version | VARCHAR(20) | |

#### `ctl.extraction_run_detail`

| Columna | Tipo | Notas |
|---|---|---|
| detail_id | BIGINT IDENTITY PK | |
| run_id | BIGINT FK | |
| object_name | VARCHAR(50) | OINV, INV1, … |
| window_start | DATETIME2 | UTC |
| window_end | DATETIME2 | UTC |
| lookback_level | VARCHAR(20) | NORMAL / NIGHTLY / MONTH_CLOSE |
| rows_extracted | INT | |
| rows_inserted | INT | |
| rows_updated | INT | |
| rows_skipped | INT | UpdateDate older than target |
| batches_total | INT | |
| batches_failed | INT | |
| ms_query_hana | INT | |
| ms_upsert_azure | INT | |
| status | VARCHAR(16) | |
| error_message | NVARCHAR(MAX) | |

#### `ctl.extraction_checkpoint`

| Columna | Tipo | Notas |
|---|---|---|
| tenant_slug | VARCHAR(50) | |
| object_name | VARCHAR(50) | |
| last_update_date | DATE | Watermark de modificaciones — fecha |
| last_update_ts | INT | Watermark de modificaciones — hora (SAP UpdateTS) |
| last_update_key | VARCHAR(50) | Natural key (DocEntry/CardCode/ItemCode/SlpCode) |
| last_create_date | DATE | Watermark de altas — fecha |
| last_create_ts | INT | Watermark de altas — hora |
| last_create_key | VARCHAR(50) | Natural key |
| last_run_id | BIGINT FK | |
| last_run_status | VARCHAR(16) | |
| last_error_at | DATETIME2 NULL | |
| last_error_message | NVARCHAR(MAX) NULL | |
| initial_loaded | BIT | |
| initial_loaded_at | DATETIME2 NULL | |
| rows_extracted_total | BIGINT | |
| **PK** | (tenant_slug, object_name) | |

> Reemplaza al antiguo `last_watermark DATETIME2`. Cualquier query / código que aún lo referencie debe migrarse a los triples `(date, ts, key)`. Ver §14.0–14.3.

#### `ctl.extraction_error`

| Columna | Tipo | Notas |
|---|---|---|
| error_id | BIGINT IDENTITY PK | |
| run_id | BIGINT FK | |
| object_name | VARCHAR(50) | |
| batch_id | UNIQUEIDENTIFIER | |
| error_code | VARCHAR(50) | HANA_TRANSIENT / AZURE_5XX / SERIALIZATION / … |
| error_message | NVARCHAR(MAX) | |
| payload_sample | NVARCHAR(MAX) | Primeras 4KB del batch para debug |
| occurred_at | DATETIME2 | UTC |
| resolved_at | DATETIME2 NULL | |

### Schema `raw` (espejo SAP)

- `raw.OINV`, `raw.INV1`, `raw.ORIN`, `raw.RIN1`, `raw.OCRD`, `raw.OITM`, `raw.OSLP`
- Columnas técnicas en todas: `_company_id INT`, `_ingested_at DATETIME2`, `_source_modality CHAR(1)`, `_batch_id UNIQUEIDENTIFIER`, `_is_deleted BIT DEFAULT 0`.

---

## 38. Roadmap de implementación técnica

### Sprint 1 (semana 1) — esqueleto y HANA

- [ ] Proyecto `DataBision.Extractor` (.NET 8 Worker Service).
- [ ] `IHanaQuery` interface + implementación ODBC.
- [ ] `LocalStore` (SQLite) con tablas checkpoint, run, offline_batch.
- [ ] `ConfigService` lee appsettings + env vars.
- [ ] Health endpoint `:8081`.
- [ ] Logging Serilog local.
- [ ] Tests unitarios: parsing config, mock HANA.

### Sprint 2 (semana 2) — pipeline + Azure

- [ ] `ExtractionPipeline` con keyset paginación.
- [ ] `AzurePusher` con Polly retry + CB.
- [ ] Watermark + lookback (los 3 niveles).
- [ ] Offline queue + drainer.
- [ ] Ingest API endpoints: POST batch, POST heartbeat.
- [ ] Tablas `raw.OINV`, `raw.INV1` + MERGE procedure.
- [ ] Tablas `ctl.*`.

### Sprint 3 (semana 3) — full table set + recovery

- [ ] Soporte de ORIN/RIN1, OCRD, OITM, OSLP.
- [ ] Schedule con cron por objeto + follow.
- [ ] Recovery mode (restore checkpoint desde cloud).
- [ ] Dry-run.
- [ ] Reconciliación diaria (job).
- [ ] Logs cloud sink.

### Sprint 4 (semana 4) — empaquetado y onboarding

- [ ] Build single-file Windows + Linux.
- [ ] Script de instalación Windows Service + systemd.
- [ ] Runbook de onboarding cliente.
- [ ] Tests de carga con dataset sintético (1M filas).
- [ ] Documentación admin.

---

## 39. MVP de 5 días para cliente dedicado

Suponiendo agente ya empaquetado y probado (post-Sprint 4):

### Día 1 — Pre-trabajo

- Reunión con cliente + partner SAP.
- Capturar: versión SAP B1, versión HANA, hostname, IP, sociedades (DBs), volumen estimado.
- Crear usuario `DBI_READER` en SAP HANA con permisos `SELECT` sobre las 7 tablas MVP.
- Provisionar Azure SQL DB del tenant + Key Vault + emitir API key.
- Validar conectividad de red cliente → Azure :443.

### Día 2 — Instalación

- Copiar zip del agente a servidor del cliente.
- Crear usuario de servicio (Windows: `DataBisionSvc`; Linux: `databision`).
- Setear env vars: `DBI_HANA_PASSWORD`, `DBI_CLOUD_API_KEY`.
- Editar `appsettings.Customer.json` con hostname HANA + schema.
- Arrancar servicio en modo `--dry-run`.
- Validar: health check verde, conexión HANA OK, conexión Azure OK.

### Día 3 — Carga inicial

- Confirmar ventana de mantenimiento con cliente.
- Triggerar carga inicial (modo `--initial-load`).
- Monitorear progreso desde dashboard cloud.
- Validar tabla por tabla: count en raw vs count en SAP.
- Reconciliación: sumas DocTotal en OINV últimos 24 meses.

### Día 4 — Incremental + monitoreo

- Apagar dry-run, activar modo normal.
- Observar 2–3 ciclos incrementales (1h–2h).
- Validar: lag <30 min, sin errores.
- Habilitar alertas: heartbeat, run failed, queue size.
- Entregar credenciales de health check al admin del cliente.

### Día 5 — Sign-off + documentación

- Reconciliación final: lista de top 20 facturas en raw = lista en SAP B1 client.
- Demo al cliente del dashboard de salud + runbook básico.
- Sign-off escrito del customer admin (calidad de datos OK).
- Documentación de la instalación específica (hostnames, versiones) archivada.
- Cliente queda en operación.

---

## 40. Criterios de aceptación

### Funcionales

1. ✅ Carga inicial de 24 meses completa en ≤4 horas para 1M filas OINV/INV1.
2. ✅ Incremental cada 30 min, lag p95 ≤ 30 min.
3. ✅ Reconciliación: rows en raw = rows en SAP ± 0.
4. ✅ Reconciliación: SUM(DocTotal) en raw = en SAP ± 0.01.
5. ✅ Agente sobrevive a 6h de corte de Azure sin perder datos (cola offline).
6. ✅ Recuperación automática desde corte de HANA (3 reintentos, alerta si persiste).
7. ✅ Dry-run reporta volumen estimado sin escribir a Azure.

### Operacionales

8. ✅ Health check expone estado JSON consumible.
9. ✅ Logs locales rotados, retención 90 días.
10. ✅ Heartbeat detectable en cloud con lag <5 min.
11. ✅ Alertas: agente caído, run failed 3×, queue size > 100MB.
12. ✅ Runbook documenta los 10 errores más comunes.

### Seguridad

13. ✅ Cero credenciales en logs (verificado por scan de strings).
14. ✅ Password HANA almacenada vía DPAPI (Windows) o EnvironmentFile 0600 (Linux).
15. ✅ Transporte HTTPS TLS 1.2+.
16. ✅ Usuario HANA `DBI_READER` con permisos mínimos (SELECT en 7 tablas, no admin).
17. ✅ Sin telemetría de PII al cloud.

### Performance

18. ✅ Throughput HANA: ≥10k rows/s en query single-table.
19. ✅ Compresión gzip: ratio ≥3:1 sobre JSON.
20. ✅ Memoria del agente: <512 MB sostenido bajo carga normal.

---

## 41. Riesgos y mitigaciones

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| **Driver HANA requiere licencia adicional cliente** | Alta | Alto | Validar en pre-onboarding; ODBC default; tener Service Layer como contingencia para casos extremos |
| Usuario `DBI_READER` no autorizado por partner SAP | Media | Alto | Documentación clara de permisos requeridos; ofrecer revisión de query plan |
| HANA caída prolongada (reboot, mantenimiento) | Media | Bajo | Reintentos automáticos; alerta tras 30 min |
| Azure caída prolongada | Baja | Medio | Offline queue 1GB; circuit breaker; alerta |
| Disco del cliente lleno (logs + queue) | Media | Alto | Rotación logs 90d; alerta queue > 80%; parar al 100% |
| Usuario SAP modifica/elimina tabla observada (UDT) | Baja | Crítico | Validación de schema al startup; alerta si columnas no coinciden |
| Cambio de UpdateDate por bug SAP (no se actualiza) | Baja | Alto | Lookback amplio nocturno; reconciliación diaria detecta divergencia |
| Reloj cliente desincronizado vs Azure | Media | Medio | NTP requerido en runbook; lookback absorbe ±10 min |
| Encoding LATIN1 vs UTF8 corrompe nombres con tilde | Media | Medio | Forzar UTF-8 en conexión ODBC; tests con dataset con tildes |
| Volumen explosivo por carga histórica de cliente | Baja | Alto | Alerta si delta > 100× promedio; pausar y revisar |
| Cliente apaga el servidor donde corre el agente | Media | Medio | Heartbeat absent → alerta; runbook de restart |
| Partner SAP upgrade B1 → query falla | Media | Alto | Tests automatizados contra dataset sintético; matriz de versiones soportadas |
| Cliente quiere agregar UDF a las queries | Alta | Bajo | Config-driven: extensible sin recompilar |
| Bug en upsert deja raw inconsistente | Baja | Crítico | Job de reconciliación diario; alerta divergencia > 0.01% |
| Caída del servicio Windows tras reinicio | Baja | Medio | Service recovery configurada; auto-start; tests en VM staging |

---

## Pseudocódigo del ciclo de extracción

```
function ExtractionCycle(table, schedule):
    if not AcquireLock(tenant, table):
        log("Run skipped — previous still running")
        return

    run = StartRun(table, trigger=SCHEDULE)
    try:
        cp = LocalStore.GetCheckpoint(tenant, table)
        // cp = { last_update_(date,ts,key), last_create_(date,ts,key), initial_loaded, ... }

        if not cp.initial_loaded and not run.is_initial:
            run.MarkFailed("Initial load not completed")
            return

        lookback = PickLookback(now=Now(), table=table)

        // Watermark inicial de esta corrida = triples del checkpoint menos lookback
        window_start = SubtractLookback(cp.last_update_date, cp.last_update_ts, lookback)
        window_end   = NormalizeNow(Now() - SafetyBuffer(60s))   // (date, ts)

        last_update_date = cp.last_update_date
        last_update_ts   = cp.last_update_ts
        last_update_key  = cp.last_update_key
        last_create_date = cp.last_create_date
        last_create_ts   = cp.last_create_ts
        last_create_key  = cp.last_create_key
        rows_in_run = 0

        // === MODIFIED SINCE ===
        loop_modified:
        while True:
            hana_rows = HanaQuery.GetHeaderBatchByUpdate(
                table=table,
                window_start=window_start,
                window_end=window_end,
                last_update_date=last_update_date,
                last_update_ts=last_update_ts,
                last_update_key=last_update_key,
                top=Config.MaxBatchRows
            )
            if hana_rows.empty: break

            success = PushBatch(table, hana_rows, strategy="upsert")
            if not success:
                LocalStore.EnqueueOffline(table, hana_rows)
                run.MarkPartial(); break
            rows_in_run += hana_rows.count
            last_update_date = hana_rows.last().UpdateDate
            last_update_ts   = hana_rows.last().UpdateTS
            last_update_key  = hana_rows.last().NaturalKey
            LocalStore.UpdateCheckpointPartial(
                tenant, table,
                update_triple=(last_update_date, last_update_ts, last_update_key)
            )

        // === CREATED SINCE ===
        // (idéntico patrón con CreateDate/CreateTS y last_create_*)
        while True:
            hana_rows = HanaQuery.GetHeaderBatchByCreate(...)
            if hana_rows.empty: break
            // dedupe contra los ya procesados en la query anterior
            new_rows = FilterAlreadySeen(hana_rows, processed_keys_in_this_run)
            if new_rows.any():
                PushBatch(table, new_rows, strategy="upsert")
                rows_in_run += new_rows.count
            last_create_date = hana_rows.last().CreateDate
            last_create_ts   = hana_rows.last().CreateTS
            last_create_key  = hana_rows.last().NaturalKey
            LocalStore.UpdateCheckpointPartial(
                tenant, table,
                create_triple=(last_create_date, last_create_ts, last_create_key)
            )

        // === HEADER → LINES por chunks de DocEntry ===
        if table in [OINV, ORIN] and rows_in_run > 0:
            doc_entries = ExtractedDocEntriesFromRun(run, table)
            for chunk in ChunksOf(doc_entries, 1000):
                ExtractLinesChunk(table.Lines, chunk)

        run.MarkSuccess(rows_in_run)
        LocalStore.UpdateCheckpoint(
            tenant, table,
            update_triple=(last_update_date, last_update_ts, last_update_key),
            create_triple=(last_create_date, last_create_ts, last_create_key),
            status=SUCCESS
        )

    catch HanaConnectionError as e:
        run.MarkFailed(error=e); AlertSupport("HANA_UNREACHABLE", tenant)
    catch Exception as e:
        run.MarkFailed(error=e); log_error(e)
    finally:
        ReleaseLock(tenant, table); ReportRunToCloud(run)


function ExtractLinesChunk(lines_table, doc_entries_chunk):
    // doc_entries_chunk: máximo 1000 elementos
    line_rows = HanaQuery.GetLinesByDocEntries(lines_table, doc_entries_chunk)
    payload = Serialize(line_rows)
    compressed = Gzip(payload)
    AzurePusher.PushBatch(
        table=lines_table,
        compressed=compressed,
        strategy="delete_by_parent_then_insert"
    )


function PickLookback(now, table):
    if IsFirstRunOfMonth(table, now):
        return Days(7)
    if IsFirstRunAfterLocal(2am, table, now):
        return Hours(24)
    return Hours(2)


function AzurePusher.PushBatch(...):
    return PollyExecute(
        retry: 3,
        backoff: exponential(1s, 2x, jitter=0.2),
        circuit_breaker: open_after(5_failures, for=5min),
        timeout: 60s,
        body: () => HttpClient.Post(IngestUrl, compressed, headers)
    )
```

---

## Ejemplos de consultas HANA

### OINV — modificadas desde watermark

```sql
SELECT TOP 5000
   "DocEntry", "DocNum", "CardCode", "CardName",
   "DocDate", "DocDueDate", "TaxDate",
   "DocTotal", "DocTotalFc", "DocCur",
   "DocStatus", "Canceled", "CancelDate",
   "SlpCode", "ObjType", "GroupNum",
   "VatSum", "VatSumFc", "DiscPrcnt", "DiscSum",
   "PaidToDate", "Comments",
   "CreateDate", "CreateTS",
   "UpdateDate", "UpdateTS"
FROM "SBO_ACME"."OINV"
WHERE
       (    "UpdateDate" >  :window_start_date
         OR ("UpdateDate" = :window_start_date AND "UpdateTS" >= :window_start_ts) )
  AND  (    "UpdateDate" <  :window_end_date
         OR ("UpdateDate" = :window_end_date   AND "UpdateTS" <  :window_end_ts) )
  AND  (    "UpdateDate" >  :last_update_date
         OR ("UpdateDate" = :last_update_date AND "UpdateTS" >  :last_update_ts)
         OR ("UpdateDate" = :last_update_date AND "UpdateTS" =  :last_update_ts
                                              AND "DocEntry"  >  :last_update_key) )
ORDER BY "UpdateDate", "UpdateTS", "DocEntry";
```

### OINV — creadas desde watermark

```sql
SELECT TOP 5000 /* mismas columnas */
FROM "SBO_ACME"."OINV"
WHERE
       (    "CreateDate" >  :window_start_date
         OR ("CreateDate" = :window_start_date AND "CreateTS" >= :window_start_ts) )
  AND  (    "CreateDate" <  :window_end_date
         OR ("CreateDate" = :window_end_date   AND "CreateTS" <  :window_end_ts) )
  AND  (    "CreateDate" >  :last_create_date
         OR ("CreateDate" = :last_create_date AND "CreateTS" >  :last_create_ts)
         OR ("CreateDate" = :last_create_date AND "CreateTS" =  :last_create_ts
                                              AND "DocEntry"  >  :last_create_key) )
ORDER BY "CreateDate", "CreateTS", "DocEntry";
```

### INV1 (líneas de factura)

```sql
SELECT
   "DocEntry", "LineNum",
   "ItemCode", "Dscription",
   "Quantity", "Price", "PriceAfVAT", "Currency", "Rate",
   "DiscPrcnt", "LineTotal", "TotalFrgn",
   "WhsCode", "VatGroup", "VatPrcnt",
   "SlpCode", "Project", "OcrCode",
   "BaseEntry", "BaseRef", "BaseType", "BaseLine",
   "VendorNum", "GrossBuyPr"
FROM "SBO_ACME"."INV1"
WHERE "DocEntry" IN ( :doc_entry_list )
ORDER BY "DocEntry", "LineNum";
```

### ORIN (cabeceras nota de crédito)

Misma forma que OINV (modificadas + creadas). Sustituir `OINV` → `ORIN`. Mantener `DocEntry` como natural key.

```sql
SELECT TOP 5000
   "DocEntry", "DocNum", "CardCode", "CardName",
   "DocDate", "DocDueDate",
   "DocTotal", "DocTotalFc", "DocCur",
   "DocStatus", "Canceled", "CancelDate",
   "SlpCode", "ObjType",
   "VatSum", "DiscPrcnt", "DiscSum",
   "Comments",
   "CreateDate", "CreateTS",
   "UpdateDate", "UpdateTS"
FROM "SBO_ACME"."ORIN"
WHERE  /* mismos predicados compuestos que OINV, sustituyendo nombre de tabla */
  ...
ORDER BY "UpdateDate", "UpdateTS", "DocEntry";
```

### RIN1 (líneas nota de crédito)

```sql
SELECT
   "DocEntry", "LineNum",
   "ItemCode", "Dscription",
   "Quantity", "Price", "Currency", "Rate",
   "DiscPrcnt", "LineTotal", "TotalFrgn",
   "WhsCode", "VatGroup", "VatPrcnt",
   "SlpCode", "Project",
   "BaseEntry", "BaseRef", "BaseType", "BaseLine"
FROM "SBO_ACME"."RIN1"
WHERE "DocEntry" IN ( :doc_entry_list )
ORDER BY "DocEntry", "LineNum";
```

### OCRD (business partners) — modificados desde watermark

Natural key: `CardCode`.

```sql
SELECT TOP 5000
   "CardCode", "CardName", "CardFName",
   "CardType", "GroupCode", "LicTradNum",
   "Address", "ZipCode", "MailAddres", "MailZipCod",
   "Phone1", "Phone2", "E_Mail",
   "Balance", "BalanceFC", "BalanceSys",
   "OrdersBal", "DNotesBal",
   "DiscountP", "VatStatus",
   "frozenFor", "validFor",
   "Currency", "ListNum",
   "SlpCode", "Territory",
   "CreateDate", "CreateTS",
   "UpdateDate", "UpdateTS"
FROM "SBO_ACME"."OCRD"
WHERE
       (    "UpdateDate" >  :window_start_date
         OR ("UpdateDate" = :window_start_date AND "UpdateTS" >= :window_start_ts) )
  AND  (    "UpdateDate" <  :window_end_date
         OR ("UpdateDate" = :window_end_date   AND "UpdateTS" <  :window_end_ts) )
  AND  (    "UpdateDate" >  :last_update_date
         OR ("UpdateDate" = :last_update_date AND "UpdateTS" >  :last_update_ts)
         OR ("UpdateDate" = :last_update_date AND "UpdateTS" =  :last_update_ts
                                              AND "CardCode"  >  :last_update_key) )
ORDER BY "UpdateDate", "UpdateTS", "CardCode";
```

Query "creados desde watermark": misma estructura con `CreateDate/CreateTS/CardCode` y `last_create_*`.

### OITM (items) — modificados desde watermark

Natural key: `ItemCode`.

```sql
SELECT TOP 5000
   "ItemCode", "ItemName",
   "ItmsGrpCod", "UgpEntry",
   "VatGourpSa", "VATLiable",
   "ManSerNum", "ManBtchNum",
   "SellItem", "PrchseItem", "InvntItem",
   "ItemType", "validFor", "frozenFor",
   "BuyUnitMsr", "SalUnitMsr",
   "ListNum",
   "CreateDate", "CreateTS",
   "UpdateDate", "UpdateTS"
FROM "SBO_ACME"."OITM"
WHERE
       (    "UpdateDate" >  :window_start_date
         OR ("UpdateDate" = :window_start_date AND "UpdateTS" >= :window_start_ts) )
  AND  (    "UpdateDate" <  :window_end_date
         OR ("UpdateDate" = :window_end_date   AND "UpdateTS" <  :window_end_ts) )
  AND  (    "UpdateDate" >  :last_update_date
         OR ("UpdateDate" = :last_update_date AND "UpdateTS" >  :last_update_ts)
         OR ("UpdateDate" = :last_update_date AND "UpdateTS" =  :last_update_ts
                                              AND "ItemCode"  >  :last_update_key) )
ORDER BY "UpdateDate", "UpdateTS", "ItemCode";
```

Query "creados desde watermark": misma estructura con `CreateDate/CreateTS/ItemCode` y `last_create_*`.

### OSLP (salespersons)

```sql
-- Full snapshot (tabla chica, refresh nocturno)
SELECT
   "SlpCode", "SlpName",
   "Memo", "Commission", "Active",
   "CreateDate", "CreateTS",
   "UpdateDate", "UpdateTS"
FROM "SBO_ACME"."OSLP"
ORDER BY "SlpCode";
```

---

## Estrategias específicas

### Carga inicial últimos 24 meses

```
1. Confirmar ventana con cliente.
2. Setear flag agente: --initial-load
3. Por cada tabla, en orden:
     OSLP    → full snapshot
     OCRD    → full snapshot (master chico)
     OITM    → full snapshot (master chico)
     OINV    → WHERE DocDate >= ADD_MONTHS(CURRENT_DATE, -24)
                paginado 5000 cabeceras por página
     INV1    → POR CADA PÁGINA de OINV ya extraída:
                 doc_entries = página.DocEntry
                 for chunk in chunks_of(doc_entries, 1000):
                     SELECT * FROM INV1 WHERE DocEntry IN chunk
                     PUSH batch
                NUNCA full scan de INV1.
     ORIN    → WHERE DocDate >= ADD_MONTHS(CURRENT_DATE, -24)
                paginado 5000 cabeceras por página
     RIN1    → mismo patrón que INV1, sobre ORIN.
4. Al cierre por tabla, setear checkpoint:
     last_update_(date, ts, key) = MAX triple de UpdateDate/TS/key visto
     last_create_(date, ts, key) = MAX triple de CreateDate/TS/key visto
     initial_loaded = true
5. Reconciliación final (count + sums + MAX(DocEntry) + MAX triples).
6. Apagar flag --initial-load; pasar a modo incremental.
```

### Incremental cada 30/60 minutos

```
Cron dispara → ExtractionCycle(table) → ver pseudocódigo §38.
- Watermark + lookback (§16).
- Keyset paginación (§12).
- Headers + lines junto (§19).
- Upsert idempotente (§22).
- Telemetría a ctl.* (§37).
```

### Reconciliación diaria contra SAP

Job programado en agente (03:00 local). **Compara cuatro métricas en paralelo** y, ante cualquier divergencia, reprocesa **solo el rango afectado** (no full re-extract de la semana).

```
PARA cada tabla MVP, PARA cada fecha en últimos N días (default N=7):

  // ── Métrica 1: count por fecha ─────────────────────────────────────
  count_hana = SELECT COUNT(*) FROM "table"
               WHERE  ( "UpdateDate" = :date )            -- modificaciones del día
                  OR ( "CreateDate" = :date )             -- altas del día
  count_raw  = GET /api/recon/{table}?date=:date
               → SELECT COUNT(*) FROM raw.table
                  WHERE _company_id = :cid
                    AND (UpdateDate = :date OR CreateDate = :date)

  // ── Métrica 2: suma DocTotal (solo OINV/ORIN) ──────────────────────
  sum_hana = SELECT SUM("DocTotal") FROM "table" WHERE "UpdateDate" = :date
  sum_raw  = SELECT SUM(DocTotal)   FROM raw.table
              WHERE _company_id = :cid AND UpdateDate = :date

  // ── Métrica 3: MAX(DocEntry / NaturalKey) ──────────────────────────
  max_key_hana = SELECT MAX("DocEntry") FROM "table" WHERE "CreateDate" = :date
  max_key_raw  = SELECT MAX(DocEntry)   FROM raw.table
                  WHERE _company_id = :cid AND CreateDate = :date

  // ── Métrica 4: MAX(UpdateDate, UpdateTS) ───────────────────────────
  max_update_hana = SELECT MAX("UpdateDate"),
                           MAX(CASE WHEN "UpdateDate"=(SELECT MAX("UpdateDate")...)
                                    THEN "UpdateTS" END)
                   FROM "table" WHERE "UpdateDate" = :date
  max_update_raw  = idem en raw.table

  // ── Evaluación ─────────────────────────────────────────────────────
  diff_count   = abs(count_hana - count_raw)
  diff_sum     = abs(sum_hana - sum_raw)
  diff_max_key = max_key_hana > max_key_raw
  diff_max_upd = (max_update_hana_date, max_update_hana_ts)
                 > (max_update_raw_date, max_update_raw_ts)

  if diff_count > tolerance_count
     OR diff_sum > 0.01
     OR diff_max_key
     OR diff_max_upd:

      LogAndAlert("RECON_DIVERGENCE", table, date,
                  diff_count, diff_sum, diff_max_key, diff_max_upd)

      // ── Reproceso quirúrgico — SOLO el rango con diferencia ───────
      TriggerTargetedReExtract(
          table=table,
          window_start=(date, 0),
          window_end=(date + 1d, 0),
          reason="recon_divergence"
      )
      // Esto re-corre la pipeline normal con esa ventana acotada.
      // No se hace full re-extract de 7d salvo que el conteo de
      // divergencias en la misma corrida supere un umbral (5 días).

PERSISTE a ops.recon_log:
  - tenant, table, date, count_hana, count_raw, sum_hana, sum_raw,
    max_key_hana, max_key_raw, max_upd_hana, max_upd_raw, status,
    reprocessed (bool), reprocessed_run_id
```

### Reglas

- Reproceso **siempre acotado por ventana de día**. No por tabla entera.
- Si 5+ días divergen en la misma corrida de reconciliación → escalar a alerta crítica + sugerir `--force-full` manual (no automático).
- Reconciliación se ejecuta tras drenar la cola offline y antes del nightly de 24h lookback.
- Tolerancia de count: 0 para documentos (OINV/ORIN), 0 para INV1/RIN1, hasta 2 para maestros (típicamente nulo).
- Tolerancia de sum: 0.01 (centavos por redondeo decimal).

### Recuperación ante fallos

Catálogo de fallos y procedimientos:

| Falla | Detección | Acción |
|---|---|---|
| HANA inaccesible | Connection error | Retry 3×; tras fallo persistente: pausar, alerta |
| HANA timeout en query grande | Query timeout | Reducir page size 50%; retry |
| Azure 5xx | HTTP response | Polly retry; offline queue si persiste |
| Azure 401 (API key inválida) | HTTP 401 | Alerta crítica; pausar agente; sin retry |
| SQLite corrupto | Open fails | Backup file; recrear; recovery checkpoint desde cloud |
| Disco lleno | Write fails | Alerta crítica; pausar |
| Crash del proceso | Service Recovery | Auto-restart por SC/systemd; recoge checkpoint del último commit |
| Schema HANA cambió | Validación startup | Alerta; modo seguro: no extraer hasta validación manual |
| Reloj fuera de sync | Drift detection | NTP forzado; lookback expandido temporalmente |
| Cola offline > 80% | Periodic check | Alerta; aumentar prioridad de drainer; si 100%: pausar |

Modo recovery completo (cuando hay sospecha de corrupción):

```
1. Detener agente.
2. Desde cloud: leer ctl.extraction_checkpoint del tenant.
3. Borrar SQLite local.
4. Setear flag --recover-from-cloud.
5. Arrancar agente: descarga checkpoint cloud, marca todas las tablas
   con lookback expandido (24h) para próxima corrida.
6. Primera corrida re-extrae 24h; reconciliación posterior detecta y corrige.
7. Si divergencia persiste: --force-rebuild para esa tabla (re-extrae 30d).
```

---

---

## 42. SQL Server: soporte futuro

### Contexto

SAP B1 corre sobre SQL Server en instalaciones heredadas y en muchos partners cloud. ADR-004 contempla Modalidad A sobre SQL Server via ODBC. Esta sección define el diseño para ese escenario.

### Driver y conexión

| Aspecto | HANA (actual) | SQL Server (futuro) |
|---|---|---|
| Driver .NET recomendado | `System.Data.Odbc` + HDBODBC | `Microsoft.Data.SqlClient` (NuGet estándar Microsoft) |
| Puerto default | 30015 | 1433 |
| Encrypt | `ENCRYPT=TRUE` en conn string | `Encrypt=True;TrustServerCertificate=False` |
| Auth | UID/PWD HANA nativo | SQL Auth (`User ID`/`Password`) o Windows Integrated |
| Schema SAP | `SBO_<EMPRESA>` | `SBO<EMPRESA>` (sin guion bajo en SQL Server) |
| Parámetros | `:nombre` | `@nombre` |
| Comillas identificadores | `"NombreColumna"` | `[NombreColumna]` |

**Recomendación SQL Server:** `Microsoft.Data.SqlClient` en lugar de ODBC puro — mejor soporte async, mejor tipado, activamente mantenido por Microsoft, sin driver externo a instalar.

### Diferencias de sintaxis T-SQL vs HANA SQL

| Constructo | HANA SQL | SQL Server T-SQL |
|---|---|---|
| Paginación top-N | `SELECT TOP 5000 ... ORDER BY` | `SELECT TOP 5000 ... ORDER BY` (igual) |
| Fecha aritmética (24 meses) | `ADD_MONTHS(CURRENT_DATE, -24)` | `DATEADD(MONTH, -24, GETDATE())` |
| Timestamp UTC | `CURRENT_TIMESTAMP` | `SYSUTCDATETIME()` |
| IN list grande | `WHERE DocEntry IN (:p1, :p2)` | `WHERE DocEntry IN (@p1, @p2)` o TVP |

### Columnas SAP en SQL Server vs HANA

SAP B1 mantiene las mismas columnas lógicas en ambas plataformas. Las columnas `UpdateDate`, `UpdateTS`, `CreateDate`, `CreateTS` existen y tienen el mismo significado. El watermark compuesto `(UpdateDate, UpdateTS, NaturalKey)` (§14) **funciona sin cambios conceptuales** en SQL Server.

| Columna | HANA tipo | SQL Server tipo | Compatible |
|---|---|---|---|
| `UpdateDate` | DATE | `date` o `datetime` | Sí — granularidad día |
| `UpdateTS` | INT (HHMMSS) | `int` (HHMMSS) | Sí — mismo formato SAP |
| `DocTotal` | DECIMAL | `numeric(19,6)` | Sí |
| `DocEntry` | INT | `int` | Sí |

### Abstracción de la capa de acceso

La interface `IHanaQuery` se renombra a `ISourceQuery` con implementaciones:

- `HanaOdbcSourceQuery` — HANA via HDBODBC (actual).
- `SqlServerSourceQuery` — SQL Server via `Microsoft.Data.SqlClient` (futuro).

La `ExtractionPipeline` no cambia; solo el `ISourceQuery` concreto varía por cliente.

### Configuración por cliente (SQL Server)

```json
"Source": {
  "Driver": "SqlServer",
  "ServerNode": "sqlserver-host,1433",
  "Database": "SBOACME",
  "Username": "DBI_READER",
  "PasswordSource": "env:DBI_SOURCE_PASSWORD",
  "Encrypt": true,
  "TrustServerCertificate": false
}
```

La sección `Hana` del config actual se reemplaza por `Source` con un campo `Driver` discriminante. Backward compat: si `Driver` ausente, default `HANA`.

---

## 43. SQL Server Change Tracking

### Qué es

SQL Server Change Tracking (CT) es una feature nativa que registra qué filas cambiaron (INSERT/UPDATE/DELETE) por tabla, asociando cada cambio a una `sync_version` — número incremental global. El extractor almacena la última `sync_version` procesada, equivalente funcional al watermark compuesto de HANA.

### Mecanismo

1. El DBA habilita CT por base de datos y por tabla.
2. SQL Server mantiene una versión global que incrementa con cada commit que toca tablas rastreadas.
3. En cada corrida: `CHANGETABLE(CHANGES tabla, @last_sync_version)` retorna solo los PKs de filas que cambiaron desde esa versión, más el tipo de operación (I/U/D).
4. El extractor hace JOIN de los PKs con la tabla principal para obtener los datos completos.
5. Al confirmar el batch en Azure, se guarda la nueva `sync_version` como checkpoint.

### CT vs watermark SAP UpdateDate/TS

| Aspecto | Watermark UpdateDate/TS | SQL Server CT |
|---|---|---|
| Detecta DELETEs reales | No (solo si SAP setea `Canceled='Y'`) | Sí — explícitamente como tipo 'D' |
| Cobertura de cambios | Solo filas donde SAP actualizó `UpdateDate` | Todos los commits DML |
| Granularidad de cursor | (date, int, naturalkey) compuesto | `sync_version` — entero simple |
| Requiere config DB previa | No | Sí (`ALTER DATABASE ... SET CHANGE_TRACKING = ON`) |
| Compatibilidad con upgrades SAP B1 | Alta — SAP gestiona UpdateDate | Media — SAP podría deshabilitar CT |
| Retention de cambios | Ilimitada (datos en tabla) | Configurable (default 2 días, mínimo recomendado 7) |
| Riesgo de "versión expirada" | No aplica | Sí — si el agente no corre por > retention → forzar full reload |
| Performance en tablas grandes | Buena (index sobre UpdateDate) | Excelente (change store interno separado de la tabla) |
| Aplica a SAP B1 HANA | N/A | No |

### Riesgos

1. **Permiso ALTER DATABASE** — el partner SAP puede restringirlo. Frecuente en instalaciones gestionadas.
2. **Retention expiry** — si el agente para por > N días configurados, la `sync_version` queda inválida. Protocolo de fallback: forzar full reload de la tabla afectada con lookback de 30 días.
3. **Ruptura silenciosa en upgrades** — SAP B1 upgrades pueden incluir scripts que reconstruyen tablas y pierden el estado CT. Validar en cada upgrade.
4. **Solo rastreo de PKs** — implica dos queries por page (CT + JOIN) en lugar de una. Marginal en performance pero mayor complejidad.

### Cuándo usar CT vs watermark

| Escenario | Recomendación |
|---|---|
| SQL Server + partner permite `ALTER DATABASE` + volumen alto | CT (mejor exactitud de DELETEs) |
| SQL Server + partner restringe `ALTER DATABASE` | Watermark UpdateDate/TS |
| SQL Server + UpdateDate confiable para tablas objetivo | Watermark (más simple, sin permisos adicionales) |
| SAP HANA (cualquier caso) | Watermark compuesto (CT no existe en HANA) |

**Recomendación MVP SQL Server:** comenzar con **watermark UpdateDate/TS** (mismo mecanismo que HANA, cero permisos adicionales, cero riesgo de expiry). Implementar CT como opción avanzada cuando el cliente y el partner lo autoricen formalmente.

### Config para activar CT (opcional, SQL Server únicamente)

```json
"ChangeTracking": {
  "Enabled": false,
  "Tables": ["OINV", "ORIN", "OCRD", "OITM"],
  "MinRetentionDays": 7,
  "FallbackToWatermarkOnExpiry": true
}
```

- `FallbackToWatermarkOnExpiry: true` — si la `sync_version` expiró, el agente cae automáticamente al modo watermark con full reload de la tabla, alerta al soporte, y reanuda CT desde la nueva versión base.

---

## 44. source_hash

### Propósito

`source_hash` es un hash criptográfico de la representación canónica de una fila, calculado en el agente antes de comprimir el batch y almacenado en `raw.*._source_hash`.

ADR-004 ya menciona `source_hash_hex` como campo que el Ingest API calcula. Esta sección especifica el diseño completo: dónde se calcula, cómo, y para qué.

**Usos:**

| Uso | Descripción |
|---|---|
| **Write-skipping** | Si el hash entrante = hash en raw → skip UPDATE. Reduce escrituras en Azure SQL en corridas de lookback amplio donde la mayoría de filas no cambiaron. |
| **Idempotencia perfecta** | Enviar el mismo batch dos veces produce el mismo hash → segundo upsert no modifica nada → operación segura sin verificación extra. |
| **Detección de divergencia** | En reconciliación (§ estrategias): recalcular hash sobre datos SAP y comparar con `_source_hash` en raw. Divergencia = corrupción en tránsito o bug de serialización. |
| **Auditoría forense** | Dado un `_batch_id` + `_source_hash`, soporte puede verificar que raw contiene exactamente lo que SAP tenía al momento del batch. |

### Punto de cálculo

El hash se calcula en el **agente**, sobre la fila normalizada, antes de comprimir el batch (gzip). No se calcula en el Ingest API — el API confía en el hash que recibe y lo almacena tal cual.

Alternativa descartada: calcular en el API — implica deserializar el batch antes de hashear, duplicando el trabajo de parsing; además el agente no podría hacer write-skipping sin el hash.

### Algoritmo

- **Función hash:** SHA-256.
- **Input:** representación canónica de la fila como string.
- **Orden de columnas:** alfabético por nombre de columna, case-insensitive.
- **Columnas excluidas** (técnicas, no originan en SAP): `_company_id`, `_ingested_at`, `_source_hash`, `_batch_id`, `_is_deleted`.
- **Serialización por tipo:**

| Tipo SAP | Serialización |
|---|---|
| NULL | `\N` (literal dos chars) |
| DATE | `YYYY-MM-DD` |
| INT | Decimal sin ceros leading, sin notación científica |
| DECIMAL / NUMERIC | Notación decimal fija, 6 decimales |
| VARCHAR / NVARCHAR | UTF-8, sin trim |
| BOOLEAN | `0` o `1` |
| DATETIME / SECONDDATE | `YYYY-MM-DDTHH:MM:SS` UTC |

- **Separador entre campos:** `|` (pipe).
- **Output:** hex string 64 caracteres (256 bits).

Ejemplo esquemático (OINV, DocEntry=12345):
```
CardCode=ACME01|CancelDate=\N|Canceled=N|CreateDate=2026-01-15|CreateTS=90000|...
→ SHA-256 → "a1b2c3d4e5f6..."
```

### Almacenamiento

Columna adicional en cada tabla `raw.*`:

| Columna | Tipo | Notas |
|---|---|---|
| `_source_hash` | `CHAR(64) NOT NULL` | SHA-256 hex, calculado por agente |

Índice sobre `_source_hash`: **no recomendado** para tablas grandes. El campo tiene alta cardinalidad pero las queries de reconciliación usan PK natural. Solo útil en scans forenses ad hoc.

### Lógica de skip en upsert (write-skipping)

En el MERGE del Ingest API, la condición de UPDATE incluye:

```
WHEN MATCHED
  AND src._source_hash <> tgt._source_hash       -- datos cambiaron
  AND (src.UpdateDate > tgt.UpdateDate
       OR (src.UpdateDate = tgt.UpdateDate
           AND src.UpdateTS >= tgt.UpdateTS))     -- versión más reciente
THEN UPDATE SET ...
```

Si `_source_hash` coincide (fila no cambió a pesar de entrar al delta por lookback), el UPDATE se omite. Counters del API retornan la fila como `skipped`.

### Trade-offs

| Aspecto | Con source_hash | Sin source_hash |
|---|---|---|
| CPU agente | +2–5% (SHA-256 por fila) | Baseline |
| Escrituras Azure SQL | Reducción significativa en corridas de lookback 24h/7d | Todas las filas matcheadas ejecutan UPDATE |
| Complejidad upsert | Mayor (condición adicional) | Menor |
| Idempotencia | Perfecta | Buena (watermark compuesto previene duplicados, no re-writes inofensivos) |
| Auditoría forense | Habilitada | No disponible |
| Detección corrupción en tránsito | Sí | Solo a nivel count/sum de reconciliación |

**Recomendación:** habilitar `source_hash` desde MVP. Costo CPU marginal; beneficio en write amplification sostenida y capacidad forense significativo para una plataforma multi-tenant.

### Configuración

```json
"SourceHash": {
  "Enabled": true,
  "Algorithm": "SHA256",
  "ExcludedColumns": ["_company_id", "_ingested_at", "_source_hash", "_batch_id", "_is_deleted"]
}
```

---

## 45. Auto-actualización del agente

### Contexto

§3 menciona la política de update como proceso manual coordinado por soporte. Esta sección define el diseño completo: protocolo técnico, canales, gates de seguridad y modo MVP.

### Filosofía

| Tipo de update | Comportamiento |
|---|---|
| Patch de seguridad crítica | Auto-update silencioso (si habilitado por cloud) |
| Fix de corrupción de datos | Auto-update silencioso (si habilitado) |
| Feature / mejora / refactor | Solo notificación → soporte coordina con cliente |
| Breaking change / major version | Siempre manual, requiere ventana con cliente |

### Version manifest

El agente consulta `GET /api/config/agent/version` (autenticado con API key del tenant) cada 1 hora. Respuesta:

```json
{
  "current_version": "1.0.3",
  "latest_stable": {
    "version": "1.1.0",
    "released_at": "2026-05-20T00:00:00Z",
    "channel": "stable",
    "breaking_changes": false,
    "min_compatible_from": "1.0.0",
    "download_url": "https://storage.databision.app/agent/...",
    "sha256": "abc123...",
    "changelog_url": "https://databision.app/changelog/1.1.0"
  },
  "latest_security": null,
  "auto_update_allowed": false
}
```

- `auto_update_allowed` lo controla el cloud según el canal y el tier. En **MVP siempre `false`** — solo notificaciones.
- `min_compatible_from` define desde qué versión se puede auto-actualizar. Versiones más viejas requieren intervención manual.

### Protocolo de auto-update (cuando `auto_update_allowed = true`)

Gates de seguridad que deben pasar ANTES de iniciar la actualización:

| Gate | Condición requerida |
|---|---|
| Sin corrida activa | Esperar fin de corrida en curso (timeout: 5 min; si supera → abortar update) |
| Cola offline vacía | No actualizar con batches pendientes de envío |
| No en carga inicial | Nunca interrumpir `--initial-load` |
| Espacio en disco | ≥ 2× tamaño del binario libre en `C:\DataBision\` / `/opt/databision/` |
| No breaking change | `breaking_changes: false` en manifest |
| No major bump | Versión actual y nueva tienen el mismo major (1.x → 1.y) |
| Firma válida | HMAC-SHA256 del manifest verificado con API key del tenant |
| SHA-256 binario válido | Hash del descargado coincide con `manifest.sha256` |

Secuencia del update:

```
1. Descargar binario a  update/<version>.new  via HTTPS
2. Verificar SHA-256     → si falla: borrar .new, alertar, abortar
3. Verificar firma HMAC  → si falla: borrar .new, alertar, abortar
4. Esperar fin de corrida activa (timeout 5 min)
5. Backup:  binario_actual  →  update/<version_actual>.bak
6. Mover:   update/<version>.new  →  binario_actual
7. Parar servicio
     Windows: SC stop DataBisionExtractor
     Linux:   systemctl stop databision-extractor
8. Iniciar servicio
9. Health check: GET :8081/health — máx. 3 intentos, 30s entre intentos
10. Si 200 OK:  borrar .bak → reportar success a /api/config/agent/version/ack
11. Si falla:   rollback (.bak → binario), reiniciar, alerta crítica
```

### Rollback

Si el health check post-update falla (3 intentos):

1. Renombrar `.bak` → binario actual.
2. Iniciar servicio.
3. Health check post-rollback.
4. Si rollback OK: loguear warning, reportar failure al cloud, continuar con versión anterior.
5. Si rollback también falla: alerta crítica — intervención manual requerida.

### Modo MVP (solo notificación)

Con `auto_update_allowed = false`:

1. Agente detecta nueva versión en el manifest.
2. Registra en log local: `INFO UpdateAvailable version=1.1.0 current=1.0.3`.
3. Envía evento `agent_update_available` al cloud via `/api/logs`.
4. Dashboard de soporte muestra indicador "N agentes desactualizados".
5. Soporte contacta cliente, coordina ventana de mantenimiento, ejecuta update manual (descargar zip, reemplazar binario, reiniciar servicio).

Este modo es deliberado — en MVP preferimos control total sobre el servidor del cliente.

---

## 46. Configuración remota

### Propósito

Permite a DataBision ajustar parámetros operacionales del agente sin acceso al servidor del cliente. Casos de uso:

- Forzar el tier comercial correcto (frecuencias según contrato) sin depender del cliente para editar config.
- Debug temporal: elevar loglevel a `Debug` para troubleshooting sin ir al servidor.
- Throttling: reducir `MaxBatchRows` si un cliente satura el Ingest API.
- Rollout gradual: habilitar nuevas tablas (OINM, UDF) cliente por cliente.
- Ajustar lookback o circuit breaker sin actualizar el binario.

### Endpoint

```
GET /api/config/agent
Authorization: X-DataBision-ApiKey {key}
```

El cloud devuelve un payload JSON firmado con HMAC-SHA256 usando la API key del tenant. El agente verifica la firma antes de aplicar cualquier cambio. Un payload con firma inválida es rechazado en su totalidad.

### Parámetros modificables remotamente

| Parámetro | Descripción |
|---|---|
| `Schedule.Default` | Frecuencia cron default para tablas sin override |
| `Schedule.Overrides.*` | Frecuencia por tabla — enforcement de tier comercial |
| `Cloud.MaxBatchRows` | Throttling / tuning de batch size |
| `Cloud.CompressionLevel` | Ajuste de compresión |
| `Lookback.DefaultHours` | Ventana lookback normal |
| `Lookback.NightlyHours` | Ventana lookback nocturno |
| `Logging.Cloud.MinLevel` | Subir a `Debug` para troubleshooting temporal |
| `OfflineQueue.MaxBytes` | Si disco del cliente es limitado |
| `CircuitBreaker.OpenDurationSec` | Tuning de resiliencia |
| `Retry.MaxAttempts` | Tuning de reintentos |
| `EnabledTables` | Array de tablas activas (phase-in de nuevas tablas) |

### Parámetros NO modificables remotamente

| Parámetro | Razón |
|---|---|
| Credenciales HANA / connection string | Seguridad — datos en infra del cliente |
| API key del agente | Seguridad — auto-referencial |
| `Hana.ValidateCertificate` | Seguridad — no se puede bajar configuración TLS remotamente |
| `SourceHash.Enabled` | Cambiar en caliente rompe integridad del hash en raw |
| Paths de disco | Seguridad — no navegar el FS del cliente |
| `Tenant.Slug` / `Tenant.CompanyId` | Identidad del tenant — inmutable después de onboarding |

La config remota **no puede sobreescribir env vars ni la config local del cliente** (ver jerarquía abajo). El cliente siempre puede ganar si setea un valor localmente — útil cuando el cliente tiene restricciones operacionales propias.

### Jerarquía de override

```
env vars                        ← máxima prioridad (secretos, overrides urgentes)
  ↑
appsettings.Customer.json       ← config local del cliente (lo que soporte instaló)
  ↑
remote config del cloud         ← override comercial DataBision
  ↑
appsettings.json                ← defaults del binario (mínima prioridad)
```

### Polling y caché

| Aspecto | Valor |
|---|---|
| Frecuencia de poll | Cada 1 hora |
| Storage local de la cache | SQLite: tabla `remote_config_cache (config_version, payload, applied_at, expires_at)` |
| Comportamiento offline | Continúa con la cache anterior sin degradar operación |
| TTL de cache | 7 días. Después de 7 días sin refresh: alerta `Warning` (no se detiene la extracción) |
| Si firma HMAC inválida | Rechazar payload, loguear error, conservar config anterior |

### Config version y idempotencia

El payload incluye `config_version` (entero incremental). El agente aplica únicamente si `config_version > config_version_local`. Garantiza que un polling repetido no re-aplica la misma config innecesariamente.

### Audit trail de cambios remotos

Cada vez que se aplica un cambio de config remota, el agente registra:

- Log local: `INFO RemoteConfigApplied param=Schedule.Default old="0 * * * *" new="*/30 * * * *" config_version=42`.
- Evento al cloud (`/api/logs`): `{ event: "config_updated", param, old_value, new_value, applied_at, config_version }`.

El equipo de soporte puede ver en el dashboard exactamente cuándo y qué cambió en cada agente, sin acceder al servidor del cliente.

---

## Glosario

- **Watermark:** triple `(UpdateDate, UpdateTS, NaturalKey)` ya procesado para una tabla. Se mantiene también el triple equivalente sobre `CreateDate/CreateTS` para capturar altas. Cursor del incremental. Ver §14.0–14.3.
- **Lookback:** ventana hacia atrás añadida al watermark en cada corrida para capturar cambios retroactivos.
- **Keyset pagination:** paginación que usa el último valor del orden en lugar de OFFSET; preserva performance.
- **Checkpoint:** estado persistido del progreso por (tenant, tabla).
- **Singleton lock:** mecanismo para evitar dos corridas concurrentes de la misma tabla.
- **Offline queue:** persistencia local de batches que no pudieron ser enviados a Azure.
- **Circuit breaker:** patrón que detiene intentos a un servicio caído para no martillarlo y recuperarlo más rápido.
- **Dry-run:** ejecución que lee de origen sin escribir destino, para validar.
- **Header table / line table:** OINV vs INV1; cabecera tiene UpdateDate confiable, líneas no.
- **Safety buffer:** margen `now() - 60s` para no leer documentos siendo escritos.
- **Recovery mode:** arranque del agente reconstruyendo estado local desde cloud.
