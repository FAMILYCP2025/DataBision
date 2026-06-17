# Native BI — Company Registry Productivo

Documentación del modelo de resolución de `analytics_company_id` por compañía.

---

## Conceptos clave

### 1. Company local (DataBision Azure SQL)

La entidad `Company` en la base de datos local de DataBision representa al cliente que contrata la plataforma.

```
companies table (Azure SQL):
  id                    INT PK
  name                  VARCHAR(200)
  slug                  VARCHAR(100) UNIQUE   ← usado en subdomain
  analytics_company_id  VARCHAR(200) NULL     ← ← ← NUEVO (Sprint 9)
  status                VARCHAR(20)
  plan_name             VARCHAR(100)
  user_limit            INT
  created_at            DATETIME
  updated_at            DATETIME
```

### 2. analytics_company_id (Supabase / MART)

Identificador de la compañía en la base de datos analítica (Supabase). Todas las tablas MART y STG usan este campo para particionar datos entre clientes.

No es el mismo que el `id` de Azure SQL ni el `slug` de la URL.

### 3. SAP CompanyDB

Identificador de la empresa en SAP Business One (ej: `KSDEPOR_TST`). Usado por el extractor SAP para conectarse al sistema de origen.

---

## Relación entre los tres identificadores

| Campo | Ejemplo | Dónde se usa |
|---|---|---|
| `slug` (DataBision) | `ksdepor` | Subdominio: `ksdepor.databision.app` |
| `analytics_company_id` | `company-dev-001` | Queries a Supabase/MART |
| `SAP CompanyDB` | `KSDEPOR_TST` | Conexión al extractor SAP |

Un mismo cliente DataBision puede tener:
- Un `slug` para el portal web
- Un `analytics_company_id` para sus datos analíticos en Supabase
- Un `SAP CompanyDB` por cada ambiente SAP (dev/prod)

---

## Flujo de resolución (Sprint 9)

```
Solicitud HTTP del frontend
  ↓
JWT: { company_slug: "ksdepor", ... }
  ↓
AnalyticsCompanyResolver.ResolveAsync("ksdepor")
  ↓
1. Buscar Company en Azure SQL WHERE slug = "ksdepor"
2. Si Company.AnalyticsCompanyId != null → usar ese valor
3. Si entorno Development → buscar en NativeBi:CompanySlugMap (appsettings)
4. Si no hay mapping → lanzar error "analytics_company_id_not_configured"
  ↓
analytics_company_id = "company-dev-001"
  ↓
Query Supabase: WHERE company_id = 'company-dev-001'
```

---

## Configuración demo (KSDEPOR)

```json
// appsettings.json (solo Development — fallback)
{
  "NativeBi": {
    "CompanySlugMap": {
      "ksdepor": "company-dev-001"
    }
  }
}
```

```sql
-- Configuración productiva (Sprint 9)
UPDATE companies
SET analytics_company_id = 'company-dev-001'
WHERE slug = 'ksdepor';
```

Con Sprint 9 implementado, el mapping en `appsettings.json` es solo un fallback de desarrollo. En producción, todos los clientes deben tener `analytics_company_id` configurado en la DB.

---

## Agregar nuevo cliente (productivo)

1. Crear empresa en SuperAdmin panel:
   - Name: `Empresa Ejemplo S.A.`
   - Slug: `empresaejemplo`
   - Analytics Company ID: `company-empresaejemplo-001`

2. Crear compañía en Supabase con el mismo ID.

3. Configurar extractor SAP con `SAP CompanyDB` del cliente.

4. Ejecutar carga inicial.

El portal en `empresaejemplo.databision.app` resolverá automáticamente a `company-empresaejemplo-001` en todas las queries analíticas.

---

## Backlog: company_sap_connections

Para ambientes productivos multi-cliente con SAP, se requiere una tabla adicional:

```
company_sap_connections (futuro):
  id                    INT PK
  company_id            INT FK → companies.id
  environment           VARCHAR(20)    -- "dev" | "prod"
  sap_company_db        VARCHAR(100)   -- "KSDEPOR_TST"
  service_layer_url     VARCHAR(500)
  username_secret_ref   VARCHAR(200)   -- referencia a Azure Key Vault
  password_secret_ref   VARCHAR(200)   -- referencia a Azure Key Vault
  is_active             BOOLEAN
  created_at            DATETIME
  updated_at            DATETIME
```

**Importante**: Las credenciales SAP NUNCA se guardan en texto plano. Se guardan referencias a secretos en Azure Key Vault.

Esta tabla está fuera del MVP de Sprint 9. Queda documentada como backlog.

---

## Error esperado si falta configuración

Si una compañía autenticada no tiene `analytics_company_id` configurado, el sistema devuelve:

```json
{
  "error": "analytics_company_id_not_configured",
  "message": "La empresa no tiene un analytics_company_id configurado. Contactar al administrador de DataBision."
}
```

HTTP 503 Service Unavailable.
