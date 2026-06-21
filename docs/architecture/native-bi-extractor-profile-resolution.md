# Native BI Extractor — Resolución de Perfil por Tenant

**Sprint:** 22C  
**Fecha:** 2026-06-21  
**Estado:** CLI args implementados. Resolución desde API: backlog Sprint 23.

---

## Estado actual (Sprint 22C)

El extractor lee las credenciales SAP desde `appsettings.Development.json`:

```json
{
  "SapServiceLayer": {
    "BaseUrl": "https://...",
    "CompanyDB": "...",
    "UserName": "...",
    "Password": "...",
    "TimeoutSeconds": 60,
    "IgnoreSslCertificateErrors": false
  }
}
```

Este archivo es gitignoreado y vive en el servidor de cada cliente.

---

## CLI args agregados (Sprint 22C)

Los siguientes flags son parseados pero aún no conectados a resolución desde API:

```
--profile <nombre>   Perfil por nombre (ej: "produccion")
--profile-id <id>    Perfil por ID numérico (ej: 7)
```

Al usar estos flags, el extractor muestra un warning y continúa con appsettings:

```
WARN: --profile / --profile-id are parsed but profile resolution from DataBision API 
      is not yet implemented. Credentials are still loaded from appsettings.
```

---

## Nuevas opciones en ExtractorOptions

```csharp
public string? ProfileName         { get; init; } = null;
public int?    ConnectionProfileId { get; init; } = null;
public bool    RunMartRefreshAfterExtraction { get; init; } = false;
public string? MartRefreshCompanyId          { get; init; } = null;
```

---

## Diseño de resolución (Sprint 23 — pendiente)

### Opción A — Pull desde API en startup (recomendada)

```
Extractor startup (si --profile o --profile-id)
  → GET /api/internal/native-bi/connection-profile/resolve
      ?companyId={analyticsCompanyId}&profileName={name}
  → Headers: X-Api-Key: {Ingest:ApiKey}
  → Response:
    {
      "serviceLayerBaseUrl": "...",
      "companyDb": "...",
      "sapUserName": "...",
      "sapPassword": "..."   ← solo en este endpoint seguro, HTTPS
    }
  → Overwrites SapServiceLayerOptions en memoria
  → NO guarda en disco
```

**Ventajas:**
- El consultor configura desde Admin UI, no necesita SSH
- Rotación de credenciales desde Admin
- Un solo punto de verdad

**Prerequisitos:**
- Endpoint `/api/internal/...` con autenticación por API key (no JWT)
- El servidor del extractor tiene acceso HTTPS a la API
- API tiene acceso al AppDB para leer el perfil

### Opción B — Variables de entorno (compatible con Sprint 22)

El Admin configura las ENV vars en el servidor a través de un proceso externo (Ansible, Terraform, UI futura).

```bash
export DATABISION_SAP_URL_KSDEPOR=https://...
export DATABISION_SAP_DB_KSDEPOR=KSDEPOR_PRD
export DATABISION_SAP_USER_KSDEPOR=databision_ro
export DATABISION_SAP_PASSWORD_KSDEPOR=****
```

El extractor lee estas ENV vars en lugar de appsettings, con `--profile ksdepor`:

```csharp
var slOptions = new SapServiceLayerOptions
{
    BaseUrl  = env("DATABISION_SAP_URL_" + slug.ToUpper()),
    CompanyDB = env("DATABISION_SAP_DB_" + slug.ToUpper()),
    UserName  = env("DATABISION_SAP_USER_" + slug.ToUpper()),
    Password  = env("DATABISION_SAP_PASSWORD_" + slug.ToUpper()),
};
```

**Esta es la Opción elegida para Sprint 22** (sin código adicional en runtime).

---

## Comando futuro esperado

```bash
dotnet DataBision.Extractor.exe --profile produccion --company company-client-001 --object OJDT --send
```

Con Opción A implementada, este comando:
1. Carga credenciales desde DataBision API usando el nombre "produccion" y company ID
2. No necesita appsettings.Development.json con credenciales
3. No necesita acceso SSH al servidor para cambiar configuración

---

## Decisión para Sprint 22

Opción B (ENV vars + SecretRef) es suficiente para el primer piloto real:
- El consultor configura las ENV vars manualmente una vez
- El scheduler usa el perfil configurado
- El Admin puede ver y testear la conexión sin SSH

Opción A (pull desde API) se implementa cuando hay ≥3 clientes activos.

---

## RunMartRefreshAfterExtraction (implementado en Sprint 22E)

```json
{
  "Extractor": {
    "RunMartRefreshAfterExtraction": true,
    "MartRefreshCompanyId": "company-client-001"
  }
}
```

Con esta configuración, el `ExtractorWorkerService` en `--service` mode:
1. Ejecuta el ciclo de extracción (OACT + OJDT)
2. Si todo exitoso, ejecuta `TransformationRunner.RefreshMartAsync(companyId)`
3. Si MART falla, registra el error pero no detiene el worker

Requisito: `Staging:ConnectionString` configurado.
