# Native BI — Diseño de Perfil de Conexión por Tenant

**Sprint:** 21B  
**Fecha:** 2026-06-20

---

## Problema actual

La configuración de SAP Service Layer (URL, CompanyDB, usuario, password) vive en `appsettings.Development.json` del extractor — un archivo en disco, uno por instancia del proceso. Para escalar a múltiples clientes:

1. El consultor debe copiar y editar archivos de config manualmente
2. No hay UI para configurar conexiones sin acceso SSH/RDP al servidor
3. Las credenciales viven en texto plano en disco
4. No hay forma de saber qué extractor está asociado a qué cliente desde el panel admin

---

## Diseño propuesto: `NativeBiConnectionProfile`

### Entidad de dominio

```csharp
// DataBision.Domain/Entities/NativeBiConnectionProfile.cs
public sealed class NativeBiConnectionProfile
{
    public int    Id              { get; set; }
    public int    CompanyId       { get; set; }  // FK → Company
    public string ProfileName     { get; set; } = string.Empty;  // "produccion", "tst"
    public string EnvironmentName { get; set; } = string.Empty;  // "Production", "Development"

    // SAP Service Layer config
    public string ServiceLayerBaseUrl { get; set; } = string.Empty;
    public string CompanyDb           { get; set; } = string.Empty;
    public string SapUserName         { get; set; } = string.Empty;

    // Credential storage strategy (see docs/security/native-bi-credential-security.md)
    // Option A (MVP): password cifrado con AES-256-GCM
    public string? SapPasswordEncrypted { get; set; }

    // Option B (production): referencia a Azure Key Vault / AWS Secrets Manager
    public string? SecretRef { get; set; }  // ej: "azure-kv://databision-prod/sap-pwd-company-abc"

    public bool    IsActive            { get; set; } = true;
    public bool    IgnoreSslErrors     { get; set; } = false;
    public int     TimeoutSeconds      { get; set; } = 60;
    public int     FetchConcurrency    { get; set; } = 3;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Company Company { get; set; } = null!;
}
```

### Tabla en AppDB

```sql
CREATE TABLE native_bi_connection_profiles (
    id                     SERIAL PRIMARY KEY,
    company_id             INT NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    profile_name           VARCHAR(100) NOT NULL,
    environment_name       VARCHAR(50)  NOT NULL DEFAULT 'Production',
    service_layer_base_url TEXT NOT NULL,
    company_db             VARCHAR(200) NOT NULL,
    sap_user_name          VARCHAR(200) NOT NULL,
    sap_password_encrypted TEXT,          -- AES-256-GCM, base64
    secret_ref             TEXT,          -- Referencia a vault externo
    is_active              BOOLEAN NOT NULL DEFAULT true,
    ignore_ssl_errors      BOOLEAN NOT NULL DEFAULT false,
    timeout_seconds        INT NOT NULL DEFAULT 60,
    fetch_concurrency      INT NOT NULL DEFAULT 3,
    created_at             TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at             TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT chk_credential_strategy CHECK (
        sap_password_encrypted IS NOT NULL OR secret_ref IS NOT NULL
    ),
    UNIQUE (company_id, profile_name)
);
```

---

## Estrategia de credenciales

**Decisión Sprint 21B:** Implementar el diseño y documentar las estrategias. La implementación MVP usa `SecretRef` apuntando al appsettings.json actual (sin cambio de comportamiento). La migración a cifrado real se hace en Sprint 22.

Ver detalles: `docs/security/native-bi-credential-security.md`

---

## API del Admin (propuesta)

```
GET    /api/admin/companies/{id}/native-bi/connection-profiles
POST   /api/admin/companies/{id}/native-bi/connection-profiles
PUT    /api/admin/companies/{id}/native-bi/connection-profiles/{profileId}
DELETE /api/admin/companies/{id}/native-bi/connection-profiles/{profileId}
POST   /api/admin/companies/{id}/native-bi/connection-profiles/{profileId}/test
```

El endpoint `POST .../test` ejecuta: login SAP SL → GET ChartOfAccounts top 1 → logout, y retorna `{ "success": true, "latencyMs": 234 }`.

---

## Integración con el extractor

Cuando esta tabla existe, el extractor puede resolverla de dos formas:

**Opción A — Pull en startup (MVP):**
```
Extractor startup
  → GET /api/admin/internal/native-bi/connection-profile?companyId=[ID]
  → Response: { serviceLayerBaseUrl, companyDb, sapUserName, sapPasswordDecrypted }
  → Overrides SapServiceLayerOptions en memoria (no en disco)
```

**Opción B — Config vía ENV var (más simple, más segura):**
```
ASPNETCORE_ENVIRONMENT=Production
SAP_SL_URL=https://...
SAP_COMPANYDB=...
SAP_USER=...
SAP_PASSWORD=...
```
El extractor lee ENV vars. La UI de admin escribe los secrets en el gestor de secrets del OS o del cloud, que los inyecta como ENV vars al proceso.

---

## Fase de implementación

| Sprint | Acción |
|---|---|
| 21B (actual) | Diseño + documentación. Crear migración EF Core de la tabla (sin lógica). |
| 22A | UI SuperAdmin para CRUD de perfiles + endpoint test |
| 22B | Extractor lee perfil del API en startup (reemplaza appsettings SL config) |
| 22C | Cifrado AES-256-GCM para password en DB o integración Key Vault |

---

## Ventajas del diseño

1. **Sin appsettings manuales:** El consultor configura desde UI web
2. **Sin credenciales en disco del servidor:** Password en AppDB cifrado o en vault
3. **Multi-profile por empresa:** TST y Producción separados
4. **Auditable:** `created_at`, `updated_at` en DB
5. **Test de conexión integrado:** Sin necesidad de acceso SSH para validar
