# Native BI Connection Profile — Implementación Sprint 22A

**Sprint:** 22A  
**Fecha:** 2026-06-21

---

## Entidad implementada

`DataBision.Domain.Entities.NativeBiConnectionProfile`

| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | int | PK, auto-increment |
| `CompanyId` | int | FK → companies.Id (CASCADE DELETE) |
| `ProfileName` | string | Nombre del perfil ("produccion", "tst") — único por empresa |
| `EnvironmentName` | string | Descripción del entorno ("Production", "Development") |
| `ServiceLayerBaseUrl` | string | URL completa: `https://host:50000/b1s/v1` |
| `CompanyDb` | string | Nombre exacto de la CompanyDB en SAP |
| `SapUserName` | string | Usuario de SAP Service Layer |
| `SecretRef` | string | Referencia al secreto — nunca el password directo |
| `IsActive` | bool | Si el perfil está activo |
| `IgnoreSslErrors` | bool | Si ignorar errores de certificado SSL |
| `TimeoutSeconds` | int | Timeout HTTP (10–300) |
| `FetchConcurrency` | int | Concurrencia de requests (1–10) |
| `CreatedAt` | DateTime | Timestamp de creación UTC |
| `UpdatedAt` | DateTime | Timestamp de última modificación UTC |

**Regla crítica:** `SecretRef` es obligatorio. El campo `Password` no existe en la entidad.

---

## Migración EF Core

**Nombre:** `AddNativeBiConnectionProfiles`  
**Tabla:** `native_bi_connection_profiles` (sin schema en SQLite, schema `cfg` en producción SQL Server si aplica)  
**Constraint:** UNIQUE (CompanyId, ProfileName)

---

## API endpoints admin

Todos requieren `[Authorize(Roles = "SuperAdmin")]`.

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/admin/companies/{companyId}/native-bi/connection-profiles` | Lista perfiles de la empresa |
| GET | `/api/admin/companies/{companyId}/native-bi/connection-profiles/{profileId}` | Obtiene un perfil |
| POST | `/api/admin/companies/{companyId}/native-bi/connection-profiles` | Crea perfil |
| PUT | `/api/admin/companies/{companyId}/native-bi/connection-profiles/{profileId}` | Actualiza perfil |
| DELETE | `/api/admin/companies/{companyId}/native-bi/connection-profiles/{profileId}` | Elimina perfil |
| POST | `/api/admin/companies/{companyId}/native-bi/connection-profiles/{profileId}/test` | Test de conexión SAP |

### Response DTO

`NativeBiConnectionProfileDto` retorna `SecretRefHint` en lugar de `SecretRef`:

```json
{
  "id": 1,
  "companyId": 5,
  "profileName": "produccion",
  "environmentName": "Production",
  "serviceLayerBaseUrl": "https://sap-host:50000/b1s/v1",
  "companyDb": "CLIENT_DB",
  "sapUserName": "databision_readonly",
  "secretRefHint": "env:***",
  "isActive": true,
  "ignoreSslErrors": false,
  "timeoutSeconds": 60,
  "fetchConcurrency": 3,
  "createdAt": "2026-06-21T10:00:00Z",
  "updatedAt": "2026-06-21T10:00:00Z"
}
```

---

## Validaciones

| Campo | Regla |
|---|---|
| `ProfileName` | Requerido, max 100 chars, único por CompanyId |
| `ServiceLayerBaseUrl` | Requerido |
| `CompanyDb` | Requerido |
| `SapUserName` | Requerido |
| `SecretRef` | Requerido |
| `TimeoutSeconds` | 10–300 |
| `FetchConcurrency` | 1–10 |

---

## Errores de API

| Código | HTTP | Descripción |
|---|---|---|
| `company_not_found` | 404 | La empresa no existe |
| `profile_not_found` | 404 | El perfil no existe |
| `profile_name_taken` | 409 | Ya existe un perfil con ese nombre para esta empresa |
| `profile_name_required` | 400 | ProfileName vacío |
| `secret_ref_required` | 400 | SecretRef vacío |
| `timeout_seconds_out_of_range` | 400 | TimeoutSeconds fuera de 10–300 |

---

## Fase siguiente (Sprint 22B+)

- Extractor lee perfil desde API en startup
- `SecretRef` se resuelve en runtime via `SecretRefResolver`
- Cifrado AES-256-GCM como alternativa a `env:` para credenciales en DB
