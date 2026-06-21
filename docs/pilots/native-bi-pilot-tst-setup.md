# Native BI — Setup Piloto TST (24A)

**Sprint:** 24A  
**Fecha:** 2026-06-21  
**Empresa piloto:** ksdepor (SAP CLTSTKSDEPOR)  
**Ambiente:** TST / DEV local

---

## Estado de la empresa piloto en AppDB

| Campo | Valor | Estado |
|---|---|---|
| Slug | `ksdepor` | ✅ Existe en AppDB |
| Name | KS Deporte | ✅ |
| AnalyticsCompanyId | `company-dev-001` | ✅ Configurado por seeder (`NativeBi:CompanySlugMap`) |
| Status | Active | ✅ |
| PlanName | Basic | ✅ |

**Cómo se configura AnalyticsCompanyId automáticamente:**

`DatabaseSeeder.SeedAnalyticsCompanyIdsAsync()` lee `NativeBi:CompanySlugMap` de `appsettings.Development.json`:

```json
"NativeBi": {
  "CompanySlugMap": {
    "ksdepor": "company-dev-001"
  }
}
```

Al arrancar la API en Development, el seeder setea `Company.AnalyticsCompanyId = "company-dev-001"` donde sea NULL. Idempotente.

---

## API Key configurada

| Campo | Valor | Archivo |
|---|---|---|
| Key (nombre) | `dev-key-001` | `appsettings.Development.json` |
| Valor mapeado | `tenant-dev:company-dev-001` | `Ingest:ApiKeys` |
| TenantId | `tenant-dev` | Extraído del valor |
| AnalyticsCompanyId | `company-dev-001` | Extraído del valor |

**Verificación:** `GET /api/internal/native-bi/connection-profile/resolve?companyId=company-dev-001&profileName=tst` con `X-DataBision-ApiKey: dev-key-001` debe retornar 200.

---

## SAP TST

| Campo | Valor |
|---|---|
| CompanyDB | `CLTSTKSDEPOR` |
| Service Layer URL | `https://161.153.200.53:50000/b1s/v1` (URL enmascarada: `https://161.153.200.53:50000`) |
| Usuario SAP | `dgoto` |
| IgnoreSslErrors | `true` (autofirmado en TST) |
| TimeoutSeconds | `60` |
| FetchConcurrency | `3` |

**IMPORTANTE:** Credenciales solo en TST. No usar en productivo. No imprimir password en logs.

---

## Variables de entorno requeridas (API server)

Antes de crear el perfil de conexión, la variable de entorno debe existir en el servidor donde corre la API:

```powershell
# Windows (PowerShell) — solo para esta sesión
$env:SAP_PASSWORD_KSDEPOR = "<password del usuario dgoto en CLTSTKSDEPOR>"

# Para persistir en el sistema:
[Environment]::SetEnvironmentVariable("SAP_PASSWORD_KSDEPOR", "<password>", "Machine")
```

```bash
# Linux / macOS
export SAP_PASSWORD_KSDEPOR=<password>
```

**SecretRef a configurar en el perfil:** `env:SAP_PASSWORD_KSDEPOR`

**Regla de seguridad:** La variable debe existir en el servidor de la API ANTES de crear el perfil. El extractor nunca ve la variable — solo la recibe resuelta como password en la respuesta del endpoint interno.

---

## Extractor appsettings.Development.json — configuración requerida

El extractor (`src\DataBision.Extractor\appsettings.Development.json`) debe tener:

```json
{
  "DataBisionApi": {
    "BaseUrl": "http://localhost:5103",
    "ApiKey": "dev-key-001"
  },
  "Extractor": {
    "TenantId": "tenant-dev",
    "CompanyId": "company-dev-001"
  },
  "Staging": {
    "ConnectionString": "<supabase connection string>"
  }
}
```

**Nota:** `SapServiceLayer` en appsettings es ignorado cuando se usa `--profile tst`. El profile override lo reemplaza.

---

## Cómo iniciar la API localmente

```powershell
# En terminal 1 (desde raíz del repo):
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5103"
dotnet run --project src/DataBision.Api --no-launch-profile
```

La API debe mostrar:
```
info: DataBision.Infrastructure.Seed.DatabaseSeeder[0]
      AnalyticsCompanyId seeded for 1 company(ies): ksdepor
```

---

## Checklist 24A

- [ ] Variable de entorno `SAP_PASSWORD_KSDEPOR` configurada en el servidor API
- [ ] API iniciada en modo Development (puerto 5103)
- [ ] Seeder confirma `AnalyticsCompanyId` en logs
- [ ] `GET /api/admin/companies` retorna empresa `ksdepor` con `analyticsCompanyId = "company-dev-001"`

---

## Riesgos

| Riesgo | Mitigación |
|---|---|
| AnalyticsCompanyId NULL (primer arranque sin DB limpia) | Seeder corre automáticamente — verificar en logs |
| Env var no disponible en API | Error 500 al hacer test connection — la API loguea `SecretRef error` |
| JWT keys no configuradas | API no arranca — verificar `Jwt:PrivateKey` en appsettings.Development.json o user secrets |
| Port 5103 ocupado | Cambiar `ASPNETCORE_URLS` o detener proceso previo |
| SAP TST inaccesible | Test connection falla — no avanzar a extracción |
