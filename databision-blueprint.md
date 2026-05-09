# DataBision — Blueprint

> Generado por The Architect el 2026-04-20
> Arquetipo: SaaS Portal + Admin Dashboard (multitenant, subdomain-based, white-label)

---

## 1. Visión General del Proyecto

### Visión

DataBision es un portal SaaS B2B de reportería empresarial para clientes de SAP Business One. Cada empresa cliente accede a su propio portal en un subdominio dedicado (`{empresa}.databision.app`), donde visualiza dashboards de Power BI embebidos alimentados con sus datos SAP sincronizados vía Azure Data Factory. El portal se ve y se siente como una herramienta propia de cada empresa gracias al sistema de white-label: logo, colores y marca personalizables por cada cliente.

La plataforma está operada por un SuperAdmin en `admin.databision.app` que gestiona empresas, usuarios, reportes y configuraciones globales. Cada empresa tiene su propio CompanyAdmin que gestiona sus propios usuarios y permisos sin acceso a otras empresas.

### Objetivos del MVP

- Permitir a empresas SAP B1 visualizar sus datos en dashboards profesionales sin infraestructura BI propia
- Entregar una primera versión comercialmente vendible y demostrable
- Aislar completamente los datos y accesos entre empresas (por subdominio + row-level security)
- Permitir white-label básico (logo + paleta de colores) para que el portal sea familiar al usuario final

### Criterios de Éxito del MVP

- Un cliente SAP puede ver sus datos de Ventas, Inventario y Finanzas en dashboards Power BI embebidos
- Un SuperAdmin puede crear una nueva empresa y dejarla operativa en menos de 30 minutos
- Un CompanyAdmin puede crear usuarios, asignar módulos y reportes sin intervención del SuperAdmin
- El portal muestra el logo y colores de la empresa cliente, no los de DataBision
- Los datos entre empresas son 100% inaccesibles entre sí (verificable por auditoría)

---

## 2. Tech Stack

| Capa | Tecnología | Decisión |
|------|-----------|----------|
| Backend framework | .NET 8 Web API | Especificado. Clean Architecture (Api / Application / Domain / Infrastructure) |
| ORM | Entity Framework Core 8 | Code-first, migrations automáticas, integración nativa con .NET |
| Frontend framework | React 18 + TypeScript + Vite | Vite por velocidad de build. React porque lo especificó el usuario |
| Estilos | Tailwind CSS v3 | CSS custom properties para theming dinámico por empresa |
| Componentes UI | shadcn/ui (Radix primitives) | Accesible, sin estilo forzado, ideal para theming |
| State management | Zustand | Ligero, sin boilerplate. Para auth y tenant state |
| Data fetching | TanStack Query (React Query) | Cache, retry, loading states. No re-inventar el wheel |
| Base de datos | Azure SQL Database (S2) | Especificado. Single DB, multitenancy por company_id |
| Auth | JWT custom — access (15min) + refresh (7 días) | Especificado. Sin dependencias externas, listo para migrar a Entra B2C |
| BI | Power BI Embedded — Service Principal | Más seguro que Master User. Sin rotación de contraseñas |
| PBI SDK | powerbi-client-react (frontend) + Microsoft.PowerBI.Api (backend) | Librerías oficiales de Microsoft |
| ETL | Azure Data Factory | Especificado. Pipelines parametrizados por empresa |
| Blob Storage | Azure Blob Storage | Logos e imágenes de branding por empresa |
| Routing subdominios | Azure Front Door (Standard) | Wildcard `*.databision.app` → App Service. SSL automático |
| Hosting backend | Azure App Service (B2) | Especificado |
| Hosting frontend | Azure Static Web Apps | Gratuito para SPAs, CDN global, integración con Front Door |
| Validación | FluentValidation | Validación de DTOs en el backend |
| Password hashing | BCrypt.Net | Estándar para contraseñas en .NET |
| CORS / Rate limiting | ASP.NET Core built-in | Sin dependencias externas |
| CI/CD | GitHub Actions | Free tier, integración directa con Azure |
| Package manager | npm (frontend) / NuGet (backend) | Estándar en ambos ecosistemas |

---

## 3. Arquitectura de Alto Nivel

```
┌─────────────────────────────────────────────────────────────┐
│                     SAP Business One                        │
│              SQL Server  ──  SAP HANA                       │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│              Azure Data Factory (ETL)                       │
│  Pipeline parametrizado por empresa (company_id)            │
│  Incremental load con watermark de timestamp                │
│  Ejecuta stored procedures de transformación                │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│              Azure SQL Database                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ [portal] companies, users, user_permissions,        │   │
│  │          company_branding, audit_logs,              │   │
│  │          modules, reports, etl_configs              │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │ [data] ventas_*, inventario_*, finanzas_*           │   │
│  │        Todas con company_id (row-level isolation)   │   │
│  └─────────────────────────────────────────────────────┘   │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│              .NET 8 Web API (Azure App Service)             │
│  TenantMiddleware → resuelve company_id desde Host header   │
│  JwtMiddleware → valida token, inyecta claims               │
│  PowerBIService → genera embed tokens (Service Principal)   │
│  AuditService → registra todas las acciones                 │
└───────────────────────┬─────────────────────────────────────┘
                        │
              ┌─────────┴─────────┐
              │                   │
              ▼                   ▼
┌─────────────────────┐  ┌─────────────────────────────────────┐
│ Azure Front Door    │  │         Power BI Service            │
│ *.databision.app    │  │  Workspace con RLS por empresa      │
│ Wildcard SSL cert   │  │  Service Principal (App Registration│
└────────┬────────────┘  └─────────────────────────────────────┘
         │
    ┌────┴────────────────────┐
    │                         │
    ▼                         ▼
admin.databision.app    {slug}.databision.app
(React SPA - Admin)     (React SPA - Portal Cliente)
SuperAdmin panel        Branded con logo + colores empresa
                        Power BI iframes embebidos
```

---

## 4. Modelo de Base de Datos

### Esquema Completo (SQL Server / Azure SQL)

```sql
-- =============================================
-- PORTAL SCHEMA
-- =============================================

CREATE TABLE companies (
    id            INT IDENTITY(1,1) PRIMARY KEY,
    name          NVARCHAR(200) NOT NULL,
    slug          NVARCHAR(100) NOT NULL UNIQUE,  -- usado en subdominio
    status        NVARCHAR(20) NOT NULL DEFAULT 'active',  -- active | suspended | inactive
    created_at    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at    DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE company_branding (
    id                INT IDENTITY(1,1) PRIMARY KEY,
    company_id        INT NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    primary_color     NVARCHAR(7) NOT NULL DEFAULT '#2563EB',  -- hex
    secondary_color   NVARCHAR(7) NOT NULL DEFAULT '#64748B',
    accent_color      NVARCHAR(7) NOT NULL DEFAULT '#0EA5E9',
    background_color  NVARCHAR(7) NOT NULL DEFAULT '#F8FAFC',
    sidebar_color     NVARCHAR(7) NOT NULL DEFAULT '#0F172A',
    logo_url          NVARCHAR(500) NULL,
    favicon_url       NVARCHAR(500) NULL,
    company_display_name NVARCHAR(200) NULL,   -- nombre mostrado en el portal (puede diferir de companies.name)
    updated_at        DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT uq_branding_company UNIQUE (company_id)
);

CREATE TABLE users (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    email           NVARCHAR(200) NOT NULL UNIQUE,
    password_hash   NVARCHAR(500) NOT NULL,
    first_name      NVARCHAR(100) NOT NULL,
    last_name       NVARCHAR(100) NOT NULL,
    role            NVARCHAR(20) NOT NULL,  -- SuperAdmin | CompanyAdmin | Viewer
    is_active       BIT NOT NULL DEFAULT 1,
    created_at      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    last_login_at   DATETIME2 NULL
);

CREATE TABLE user_companies (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    user_id     INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    company_id  INT NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    CONSTRAINT uq_user_company UNIQUE (user_id, company_id)
);

CREATE TABLE modules (
    id      INT IDENTITY(1,1) PRIMARY KEY,
    name    NVARCHAR(100) NOT NULL,  -- Ventas | Inventario | Finanzas
    slug    NVARCHAR(50) NOT NULL UNIQUE,  -- ventas | inventario | finanzas
    icon    NVARCHAR(50) NULL,  -- nombre de ícono Lucide
    sort_order INT NOT NULL DEFAULT 0
);

CREATE TABLE reports (
    id                      INT IDENTITY(1,1) PRIMARY KEY,
    module_id               INT NOT NULL REFERENCES modules(id),
    company_id              INT NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    name                    NVARCHAR(200) NOT NULL,
    description             NVARCHAR(500) NULL,
    powerbi_workspace_id    NVARCHAR(100) NOT NULL,
    powerbi_report_id       NVARCHAR(100) NOT NULL,
    powerbi_dataset_id      NVARCHAR(100) NOT NULL,
    is_active               BIT NOT NULL DEFAULT 1,
    sort_order              INT NOT NULL DEFAULT 0,
    created_at              DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Permisos: un registro = un usuario tiene acceso a un reporte específico en una empresa
-- Si solo existe permiso a nivel módulo (report_id NULL), el usuario ve todos los reportes del módulo
CREATE TABLE user_permissions (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    user_id     INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    company_id  INT NOT NULL REFERENCES companies(id),
    module_id   INT NOT NULL REFERENCES modules(id),
    report_id   INT NULL REFERENCES reports(id),  -- NULL = permiso a nivel módulo completo
    can_view    BIT NOT NULL DEFAULT 1,
    granted_by  INT NOT NULL REFERENCES users(id),
    granted_at  DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE refresh_tokens (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    user_id     INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    company_id  INT NULL REFERENCES companies(id),  -- NULL para SuperAdmin
    token_hash  NVARCHAR(500) NOT NULL UNIQUE,
    expires_at  DATETIME2 NOT NULL,
    created_at  DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    revoked_at  DATETIME2 NULL
);

CREATE TABLE audit_logs (
    id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id         INT NULL REFERENCES users(id),
    company_id      INT NULL REFERENCES companies(id),
    action          NVARCHAR(100) NOT NULL,  -- LOGIN | LOGOUT | VIEW_REPORT | CREATE_USER | etc.
    resource_type   NVARCHAR(100) NULL,  -- Company | User | Report | Permission
    resource_id     NVARCHAR(100) NULL,
    metadata        NVARCHAR(MAX) NULL,  -- JSON con detalles adicionales
    ip_address      NVARCHAR(45) NULL,
    user_agent      NVARCHAR(500) NULL,
    created_at      DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE etl_configs (
    id                      INT IDENTITY(1,1) PRIMARY KEY,
    company_id              INT NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    module_id               INT NOT NULL REFERENCES modules(id),
    sap_server              NVARCHAR(200) NULL,
    sap_database            NVARCHAR(200) NULL,
    sap_type                NVARCHAR(20) NULL,  -- sqlserver | hana
    adf_pipeline_name       NVARCHAR(200) NULL,
    schedule_cron           NVARCHAR(100) NULL DEFAULT '0 2 * * *',  -- 2am daily
    last_sync_at            DATETIME2 NULL,
    last_sync_status        NVARCHAR(20) NULL,  -- success | failed | running
    is_active               BIT NOT NULL DEFAULT 0,
    CONSTRAINT uq_etl_company_module UNIQUE (company_id, module_id)
);

-- =============================================
-- DATA SCHEMA (tablas de datos SAP sincronizados)
-- Todas tienen company_id para row-level isolation
-- =============================================

-- Ejemplo: Ventas
CREATE TABLE sales_orders (
    id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    company_id      INT NOT NULL REFERENCES companies(id),
    sap_doc_num     INT NOT NULL,
    doc_date        DATE NOT NULL,
    customer_code   NVARCHAR(50) NOT NULL,
    customer_name   NVARCHAR(200) NOT NULL,
    total_amount    DECIMAL(18,2) NOT NULL,
    currency        NVARCHAR(5) NOT NULL DEFAULT 'CLP',
    status          NVARCHAR(50) NULL,
    salesperson     NVARCHAR(100) NULL,
    synced_at       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    INDEX ix_sales_orders_company_date (company_id, doc_date)
);

-- Índice de auditoría para búsquedas frecuentes
CREATE INDEX ix_audit_logs_company_created ON audit_logs (company_id, created_at DESC);
CREATE INDEX ix_audit_logs_user_created ON audit_logs (user_id, created_at DESC);
CREATE INDEX ix_user_permissions_user_company ON user_permissions (user_id, company_id);

-- Seed: módulos iniciales
INSERT INTO modules (name, slug, icon, sort_order) VALUES
    ('Ventas', 'ventas', 'TrendingUp', 1),
    ('Inventario', 'inventario', 'Package', 2),
    ('Finanzas', 'finanzas', 'DollarSign', 3);
```

### Relaciones Clave

- `companies` → `company_branding` (1:1)
- `companies` ←→ `users` (M:N via `user_companies`)
- `companies` → `reports` (1:N, un reporte pertenece a una empresa)
- `reports` → `modules` (N:1)
- `user_permissions` → `users` + `companies` + `modules` + `reports` (permiso granular)
- `audit_logs` → `users` + `companies` (trazabilidad completa)

---

## 5. Autenticación y Autorización

### Auth Flow

```
1. Usuario accede a {slug}.databision.app
2. Frontend llama GET /api/tenant/config → recibe branding (público, sin auth)
3. Usuario ingresa email + password → POST /api/auth/login
4. Backend:
   a. Valida credenciales
   b. Verifica que el usuario pertenece a la empresa del subdominio
   c. Genera access_token (JWT, 15 min) + refresh_token (opaco, 7 días)
   d. Guarda refresh_token hasheado en DB
5. Frontend guarda tokens en memory (access) + httpOnly cookie (refresh)
6. Cada request incluye Authorization: Bearer {access_token}
7. TenantMiddleware valida que company_id del JWT coincide con subdominio del Host

Refresh flow:
- Access token expirado → frontend llama POST /api/auth/refresh con cookie
- Backend valida refresh_token en DB, genera nuevo par de tokens
- Si refresh_token expirado → redirect a login
```

### Claims del JWT

```json
{
  "sub": "42",
  "email": "usuario@empresa.com",
  "role": "CompanyAdmin",
  "company_id": "7",
  "company_slug": "empresax",
  "module_ids": [1, 2, 3],
  "exp": 1714000000,
  "iss": "databision-api"
}
```

### Roles y Permisos

| Rol | Scope | Puede hacer |
|-----|-------|-------------|
| `SuperAdmin` | Global (todas las empresas) | CRUD empresas, usuarios, reportes, branding, ver todos los audit logs |
| `CompanyAdmin` | Su empresa solamente | CRUD usuarios de su empresa, asignar permisos, editar branding de su empresa |
| `Viewer` | Su empresa, módulos asignados | Ver reportes con permiso explícito |

### Lógica de Permisos para Reportes

```
CanViewReport(userId, reportId):
  1. Obtener company_id del reporte
  2. Verificar que user pertenece a esa company (user_companies)
  3. Verificar que existe user_permissions con:
     - user_id = userId
     - company_id = company del reporte
     - module_id = módulo del reporte
     - (report_id = reportId) OR (report_id IS NULL)  ← acceso a módulo completo
     - can_view = true
  4. Si existe → autorizado. Si no → 403.
```

### Rutas Públicas vs Protegidas

| Ruta | Auth | Notas |
|------|------|-------|
| `GET /api/tenant/config` | Público | Solo devuelve branding, nunca datos sensibles |
| `POST /api/auth/login` | Público | Rate limited: 5 intentos / 15 min por IP |
| `POST /api/auth/refresh` | Cookie | Solo refresh_token válido y no revocado |
| Todas las demás | JWT requerido | TenantMiddleware + RoleAuthorize |

---

## 6. Estrategia Power BI Embedded

### Setup (una sola vez)

1. Registrar una App en Azure AD (Service Principal)
2. Asignar permisos de API: `Report.ReadAll`, `Dataset.ReadAll` en Power BI
3. Agregar el Service Principal como miembro del Workspace de Power BI
4. Configurar Row-Level Security (RLS) en el dataset de Power BI:
   - Role `CompanyRole` con filtro: `[company_id] = USERPRINCIPALNAME()`
   - El embed token enviará `username = company.slug` para activar este filtro

### Flujo de Generación de Token (por request)

```
Frontend                    Backend (.NET)                  Power BI Service
   │                              │                               │
   │── GET /api/reports/{id}/embed-token ──►                     │
   │                              │                               │
   │                     Verifica permiso                        │
   │                     (user + report + company)               │
   │                              │                               │
   │                              │── Authenticate Service Principal ──►│
   │                              │◄── Azure AD Token ─────────────────│
   │                              │                               │
   │                              │── GenerateTokenRequest ────────────►│
   │                              │   { reports: [reportId],     │
   │                              │     identities: [{           │
   │                              │       username: "empresax",  │
   │                              │       roles: ["CompanyRole"],│
   │                              │       datasets: [datasetId]  │
   │                              │     }] }                     │
   │                              │◄── EmbedToken (TTL 1h) ─────────────│
   │                              │                               │
   │◄── { embedUrl, accessToken, expiry } ──                     │
   │                              │                               │
   │── Render <PowerBIEmbed> ─►  │                               │
```

### Componente React

```tsx
// components/powerbi/EmbedReport.tsx
// Usa powerbi-client-react con el token del backend
// Maneja expiración: re-fetch token 5 min antes de expiry
// Loading skeleton mientras carga
// Error state si el token falla
```

---

## 7. Sistema de Theming por Empresa

### Cómo Funciona

```
1. App inicia → detecta subdominio (window.location.hostname)
2. Llama GET /api/tenant/config → recibe:
   { primaryColor, secondaryColor, logoUrl, companyDisplayName, ... }
3. ThemeProvider aplica CSS custom properties al :root:
   --brand-primary: #E74C3C;
   --brand-secondary: #2C3E50;
   --brand-accent: #3498DB;
   --brand-sidebar: #1A252F;
4. Tailwind usa estas variables para colores brand-*
5. Logo se muestra en sidebar y login page

Personalización por CompanyAdmin:
- Color picker en /settings/branding
- Upload de logo (PNG/SVG, max 2MB) → se sube a Azure Blob Storage
- Preview en tiempo real antes de guardar
- Reset a defaults disponible
```

### Configuración Tailwind

```js
// tailwind.config.js
colors: {
  brand: {
    primary:   'var(--brand-primary)',
    secondary: 'var(--brand-secondary)',
    accent:    'var(--brand-accent)',
    sidebar:   'var(--brand-sidebar)',
  }
}
```

---

## 8. Estructura de Carpetas / Proyecto

### Backend (.NET 8)

```
DataBision/
├── DataBision.sln
├── src/
│   ├── DataBision.Api/                      # Capa de presentación
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs            # /api/auth/*
│   │   │   ├── TenantController.cs          # /api/tenant/config
│   │   │   ├── AdminController.cs           # /api/admin/* (SuperAdmin)
│   │   │   ├── CompanyController.cs         # /api/company/* (CompanyAdmin)
│   │   │   ├── ModulesController.cs         # /api/modules
│   │   │   ├── ReportsController.cs         # /api/reports/{id}/embed-token
│   │   │   └── AuditController.cs           # /api/audit-logs
│   │   ├── Middleware/
│   │   │   ├── TenantMiddleware.cs          # Resuelve company_id del Host header
│   │   │   └── AuditMiddleware.cs           # Registra acciones en audit_logs
│   │   ├── Filters/
│   │   │   └── ValidationFilter.cs          # FluentValidation → 400 responses
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── DataBision.Application/              # Lógica de negocio
│   │   ├── Services/
│   │   │   ├── AuthService.cs               # Login, refresh, JWT generation
│   │   │   ├── TenantService.cs             # Resuelve empresa desde slug
│   │   │   ├── EmbedTokenService.cs         # Power BI embed token generation
│   │   │   ├── PermissionService.cs         # Valida acceso usuario → reporte
│   │   │   ├── BrandingService.cs           # Branding CRUD + blob upload
│   │   │   └── AuditService.cs              # Escribe audit_logs
│   │   ├── DTOs/
│   │   │   ├── Auth/
│   │   │   ├── Tenant/
│   │   │   ├── Admin/
│   │   │   └── Reports/
│   │   ├── Validators/                      # FluentValidation validators
│   │   └── Interfaces/
│   │       ├── IAuthService.cs
│   │       ├── IEmbedTokenService.cs
│   │       └── IPermissionService.cs
│   │
│   ├── DataBision.Domain/                   # Entidades y reglas de dominio
│   │   ├── Entities/
│   │   │   ├── Company.cs
│   │   │   ├── CompanyBranding.cs
│   │   │   ├── User.cs
│   │   │   ├── UserCompany.cs
│   │   │   ├── Module.cs
│   │   │   ├── Report.cs
│   │   │   ├── UserPermission.cs
│   │   │   ├── RefreshToken.cs
│   │   │   ├── AuditLog.cs
│   │   │   └── EtlConfig.cs
│   │   └── Enums/
│   │       ├── UserRole.cs
│   │       └── CompanyStatus.cs
│   │
│   └── DataBision.Infrastructure/           # Persistencia y servicios externos
│       ├── Data/
│       │   ├── AppDbContext.cs
│       │   ├── Configurations/              # EF Fluent API configs por entidad
│       │   └── Migrations/
│       ├── Repositories/
│       │   ├── CompanyRepository.cs
│       │   ├── UserRepository.cs
│       │   └── ReportRepository.cs
│       ├── PowerBI/
│       │   └── PowerBIService.cs            # Microsoft.PowerBI.Api integration
│       ├── Azure/
│       │   └── BlobStorageService.cs        # Azure.Storage.Blobs SDK
│       └── Seed/
│           └── DatabaseSeeder.cs            # Módulos + SuperAdmin inicial
│
├── tests/
│   ├── DataBision.Api.Tests/
│   └── DataBision.Application.Tests/
│
└── .github/
    └── workflows/
        └── deploy-backend.yml
```

### Frontend (React + TypeScript + Vite)

```
databision-frontend/
├── index.html
├── vite.config.ts
├── tailwind.config.js
├── tsconfig.json
├── package.json
│
├── src/
│   ├── main.tsx
│   ├── App.tsx                              # Router raíz + detección de subdominio
│   │
│   ├── apps/
│   │   ├── admin/                           # admin.databision.app
│   │   │   ├── AdminApp.tsx                 # Root del admin (layout + routes)
│   │   │   ├── pages/
│   │   │   │   ├── LoginPage.tsx
│   │   │   │   ├── DashboardPage.tsx        # Overview: métricas globales
│   │   │   │   ├── CompaniesPage.tsx        # Lista de empresas
│   │   │   │   ├── CompanyDetailPage.tsx    # Empresa: users, branding, reports
│   │   │   │   ├── UsersPage.tsx            # Todos los usuarios
│   │   │   │   ├── ReportsPage.tsx          # Gestión de reportes PBI por empresa
│   │   │   │   └── AuditPage.tsx            # Audit log viewer
│   │   │   └── components/
│   │   │       ├── CompanyForm.tsx
│   │   │       ├── BrandingEditor.tsx
│   │   │       └── ReportLinker.tsx         # Vincula report_id PBI a empresa
│   │   │
│   │   └── portal/                          # {slug}.databision.app
│   │       ├── PortalApp.tsx                # Root del portal (layout + routes)
│   │       ├── pages/
│   │       │   ├── LoginPage.tsx            # Branded con logo + colores empresa
│   │       │   ├── ModulePage.tsx           # Lista reportes del módulo activo
│   │       │   ├── ReportViewPage.tsx       # Report PBI embebido
│   │       │   └── settings/
│   │       │       ├── UsersPage.tsx        # CompanyAdmin: gestión usuarios
│   │       │       ├── PermissionsPage.tsx  # CompanyAdmin: asignar permisos
│   │       │       └── BrandingPage.tsx     # Editar logo y colores
│   │       └── components/
│   │           ├── ModuleSidebar.tsx        # Sidebar con módulos accesibles
│   │           └── PermissionMatrix.tsx     # Grid usuario × reporte
│   │
│   ├── components/
│   │   ├── ui/                              # shadcn/ui components
│   │   ├── layout/
│   │   │   ├── AdminLayout.tsx
│   │   │   ├── PortalLayout.tsx
│   │   │   ├── Sidebar.tsx
│   │   │   └── Header.tsx
│   │   ├── powerbi/
│   │   │   ├── EmbedReport.tsx             # Wrapper powerbi-client-react
│   │   │   └── EmbedSkeleton.tsx           # Loading state
│   │   └── branding/
│   │       ├── ThemeProvider.tsx           # Aplica CSS vars desde tenant config
│   │       ├── ColorPicker.tsx
│   │       └── LogoUpload.tsx
│   │
│   ├── hooks/
│   │   ├── useTenant.ts                    # Lee subdominio, carga branding
│   │   ├── useAuth.ts                      # Login, logout, refresh
│   │   ├── usePermissions.ts               # Check canViewReport, canViewModule
│   │   └── useEmbedToken.ts               # Fetch + auto-refresh embed token
│   │
│   ├── lib/
│   │   ├── api.ts                          # Axios instance + interceptors JWT
│   │   ├── auth.ts                         # JWT decode, token storage
│   │   ├── theme.ts                        # Apply CSS custom properties
│   │   └── utils.ts
│   │
│   ├── stores/
│   │   ├── authStore.ts                    # Zustand: user, tokens, login/logout
│   │   └── tenantStore.ts                  # Zustand: company slug, branding
│   │
│   └── types/
│       └── index.ts                        # Company, User, Report, BrandingConfig, etc.
│
└── .github/
    └── workflows/
        └── deploy-frontend.yml
```

### ETL (Azure Data Factory)

```
adf-templates/
├── linked_services/
│   ├── sap_sqlserver_template.json          # SAP B1 via SQL Server
│   ├── sap_hana_template.json               # SAP B1 via HANA
│   └── azure_sql_template.json              # Azure SQL destino
├── datasets/
│   ├── src_ventas_template.json
│   ├── src_inventario_template.json
│   ├── src_finanzas_template.json
│   └── dst_azure_sql_template.json
├── pipelines/
│   ├── pipeline_ventas_template.json        # Parametrizado: @pipeline().parameters.company_id
│   ├── pipeline_inventario_template.json
│   └── pipeline_finanzas_template.json
└── stored_procedures/
    ├── sp_transform_ventas.sql
    ├── sp_transform_inventario.sql
    └── sp_transform_finanzas.sql
```

---

## 9. APIs Principales

### Overview

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| GET | `/api/tenant/config` | Branding + nombre empresa por subdominio | Público |
| POST | `/api/auth/login` | Login → access + refresh tokens | Público |
| POST | `/api/auth/refresh` | Renovar access token | Cookie |
| POST | `/api/auth/logout` | Revocar refresh token | JWT |
| GET | `/api/modules` | Módulos accesibles para el usuario actual | JWT |
| GET | `/api/modules/{slug}/reports` | Reportes del módulo accesibles para el usuario | JWT |
| POST | `/api/reports/{id}/embed-token` | Generar embed token Power BI | JWT + permiso |
| GET | `/api/admin/companies` | Lista todas las empresas | SuperAdmin |
| POST | `/api/admin/companies` | Crear empresa | SuperAdmin |
| PUT | `/api/admin/companies/{id}` | Editar empresa | SuperAdmin |
| GET | `/api/admin/companies/{id}/users` | Usuarios de una empresa | SuperAdmin |
| POST | `/api/admin/users` | Crear usuario | SuperAdmin |
| PUT | `/api/admin/companies/{id}/branding` | Editar branding empresa | SuperAdmin |
| POST | `/api/admin/companies/{id}/reports` | Vincular reporte PBI a empresa | SuperAdmin |
| GET | `/api/company/users` | Usuarios de mi empresa | CompanyAdmin |
| POST | `/api/company/users` | Crear usuario en mi empresa | CompanyAdmin |
| PUT | `/api/company/users/{id}` | Editar usuario de mi empresa | CompanyAdmin |
| GET | `/api/company/permissions` | Permisos de usuarios en mi empresa | CompanyAdmin |
| PUT | `/api/company/permissions` | Actualizar permisos (batch) | CompanyAdmin |
| PUT | `/api/company/branding` | Editar mi branding | CompanyAdmin |
| POST | `/api/company/branding/logo` | Subir logo | CompanyAdmin |
| GET | `/api/audit-logs` | Ver audit logs (filtrado por company_id) | Admin+ |

### Detalle Endpoints Críticos

#### POST /api/auth/login
```json
// Request
{ "email": "user@empresa.com", "password": "...", "companySlug": "empresax" }

// Response 200
{
  "accessToken": "eyJ...",
  "user": { "id": 42, "name": "Juan García", "role": "CompanyAdmin", "companyId": 7 },
  "expiresIn": 900
}
// refresh_token en httpOnly cookie: databision_refresh

// Response 401
{ "error": "invalid_credentials" }

// Response 403
{ "error": "user_not_in_company" }
```

#### POST /api/reports/{id}/embed-token
```json
// Request: solo JWT en header, sin body
// Headers: Authorization: Bearer {token}

// Response 200
{
  "embedUrl": "https://app.powerbi.com/reportEmbed?reportId=...",
  "accessToken": "eyJ...",
  "tokenId": "abc123",
  "expiry": "2026-04-20T15:30:00Z"
}

// Response 403
{ "error": "report_access_denied" }
```

#### PUT /api/company/permissions (batch)
```json
// Request
{
  "permissions": [
    { "userId": 10, "moduleId": 1, "reportId": null, "canView": true },
    { "userId": 10, "moduleId": 2, "reportId": 5, "canView": true },
    { "userId": 11, "moduleId": 1, "reportId": null, "canView": false }
  ]
}

// Response 200
{ "updated": 3 }
```

---

## 10. Frontend — Páginas y Componentes Clave

### Rutas Admin (`admin.databision.app`)

| Ruta | Página | Descripción |
|------|--------|-------------|
| `/login` | LoginPage | Login SuperAdmin, sin branding personalizado |
| `/` | DashboardPage | KPIs globales: N° empresas, usuarios, reportes activos |
| `/companies` | CompaniesPage | Tabla de empresas con status, búsqueda, crear nueva |
| `/companies/:id` | CompanyDetailPage | Tabs: Info, Usuarios, Reportes, Branding, ETL |
| `/users` | UsersPage | Todos los usuarios del sistema |
| `/audit` | AuditPage | Tabla paginada audit logs con filtros |

### Rutas Portal (`{slug}.databision.app`)

| Ruta | Página | Descripción |
|------|--------|-------------|
| `/login` | LoginPage | Login con logo + colores de la empresa |
| `/` | Redirect | → primer módulo accesible |
| `/{modulo}` | ModulePage | Lista de reportes del módulo con cards |
| `/{modulo}/{reportId}` | ReportViewPage | Report PBI embebido full-width |
| `/settings/users` | UsersSettingsPage | CompanyAdmin: CRUD usuarios |
| `/settings/permissions` | PermissionsPage | CompanyAdmin: matriz permiso × usuario × reporte |
| `/settings/branding` | BrandingPage | CompanyAdmin: logo + color picker + preview live |

### Detección de Subdominio (App.tsx)

```tsx
const hostname = window.location.hostname;
const isAdmin = hostname.startsWith('admin.');
const slug = isAdmin ? null : hostname.split('.')[0];

// Renderiza AdminApp o PortalApp según corresponda
// En local dev: parámetro ?tenant=empresax para simular subdominios
```

---

## 11. Diseño Visual

### Sistema de Colores Base (DataBision Platform)

| Rol | Hex | Uso |
|-----|-----|-----|
| Sidebar | `#0F172A` | Fondo sidebar en admin y default portal |
| Sidebar active | `#1E293B` | Item activo en nav |
| Background | `#F8FAFC` | Fondo de página principal |
| Surface | `#FFFFFF` | Cards, paneles, modales |
| Border | `#E2E8F0` | Bordes de cards y separadores |
| Text primary | `#0F172A` | Texto principal |
| Text muted | `#64748B` | Labels, metadata, subtítulos |
| Brand primary | `var(--brand-primary)` | Default: `#2563EB` (azul corporativo) |
| Success | `#16A34A` | Confirmaciones, estados OK |
| Destructive | `#DC2626` | Errores, eliminar, alertas |
| Warning | `#D97706` | Alertas menores, pendientes |

### Tipografía

| Rol | Font | Tamaño | Peso |
|-----|------|--------|------|
| Headings H1 | Inter | 24px | 700 |
| Headings H2 | Inter | 20px | 600 |
| Body | Inter | 14px | 400 |
| Labels / UI | Inter | 13px | 500 |
| Números / data | Inter (tabular) | 14px–20px | 600 |
| Code | JetBrains Mono | 13px | 400 |

### Estilo de Componentes

- **Border radius:** 6px default, 8px cards, 12px modales
- **Sombras:** `shadow-sm` solo en cards y dropdowns — sin sombras dramáticas
- **Spacing base:** 4px — escala: 4, 8, 12, 16, 24, 32, 48, 64
- **Sidebar:** 240px ancho fijo, colapsable a 64px (solo íconos) en mobile
- **Max content width:** 1280px con padding lateral 24px
- **Densidad:** Compacto — tabla con rows de 44px, forms con gap-4
- **Animaciones:** Solo transiciones sutiles (150ms ease) en hover/focus — sin animaciones decorativas
- **Tablas:** Alternado sutil de filas (`#FAFAFA`), hover de fila

---

## 12. Seguridad

### Medidas Implementadas en MVP

| Área | Implementación |
|------|---------------|
| Autenticación | JWT firmado con RS256 (clave privada en env var, no en código) |
| Refresh tokens | Almacenados hasheados (SHA-256) en DB. Un solo uso — rotate on use |
| Passwords | BCrypt con cost factor 12 |
| Isolación de tenants | TenantMiddleware valida Host vs company_id del JWT en CADA request |
| CORS | Solo orígenes `*.databision.app` permitidos |
| Rate limiting | Login: 5 req/15min por IP. API general: 100 req/min por usuario |
| Input validation | FluentValidation en todos los DTOs, sanitización de strings |
| SQL injection | Solo EF Core con parámetros — zero raw queries sin parameterización |
| XSS | React escapa por default. No usar dangerouslySetInnerHTML |
| HTTPS | Azure Front Door fuerza HTTPS. HSTS habilitado |
| Secrets | Todas las credenciales en Azure App Service Configuration (no en código) |
| Logs | Nunca loguear passwords, tokens ni datos sensibles |
| Subida de archivos | Solo PNG/SVG/ICO, max 2MB, validación de content-type en servidor |

---

## 13. Auditoría

### Acciones Auditadas

```
LOGIN_SUCCESS, LOGIN_FAILED, LOGOUT
VIEW_REPORT
CREATE_COMPANY, UPDATE_COMPANY, DELETE_COMPANY
CREATE_USER, UPDATE_USER, DEACTIVATE_USER
UPDATE_PERMISSIONS
UPDATE_BRANDING
CREATE_REPORT_LINK, DELETE_REPORT_LINK
```

### Retención y Acceso

- Retención MVP: 90 días (configurable por empresa en versiones futuras)
- SuperAdmin: ve logs de todas las empresas
- CompanyAdmin: ve solo logs de su empresa
- Viewer: sin acceso
- Exportación CSV disponible desde el panel de administración

---

## 14. Roadmap de Implementación (Build Order)

**Step 1: Azure Infrastructure Setup**
- Crear resource group `databision-rg`
- Azure SQL Database (S2 tier): servidor + DB `databision-prod`
- Azure App Service (B2 plan, .NET 8): `databision-api`
- Azure Static Web Apps: `databision-frontend`
- Azure Blob Storage: container `branding` con acceso público de lectura
- Azure Front Door Standard: wildcard `*.databision.app` → App Service
  - Configurar custom domain `databision.app` con wildcard certificate
  - Route `/api/*` → App Service, `/*` → Static Web Apps
- App Registration en Azure AD para Power BI Service Principal
  - Permisos: `Report.ReadAll`, `Dataset.ReadAll`, `Dataset.Read.All`
  - Anotar: `tenantId`, `clientId`, `clientSecret`

**Step 2: Base de Datos + EF Core (Backend)**
- Crear solución .NET con 4 proyectos: Api, Application, Domain, Infrastructure
- Instalar packages: `Microsoft.EntityFrameworkCore.SqlServer`, `BCrypt.Net-Next`, `FluentValidation.AspNetCore`, `Microsoft.PowerBI.Api`, `Azure.Storage.Blobs`, `System.IdentityModel.Tokens.Jwt`
- Definir todas las entidades en Domain con anotaciones mínimas
- Configurar `AppDbContext` con Fluent API (índices, constraints, relaciones)
- Crear migration inicial: `dotnet ef migrations add InitialCreate`
- `dotnet ef database update`
- Ejecutar `DatabaseSeeder`: módulos (Ventas, Inventario, Finanzas) + SuperAdmin inicial

**Step 3: Auth Backend**
- Implementar `AuthService`: BCrypt verify, JWT generation (access + refresh)
- JWT con RS256: generar par de llaves RSA, llave pública en config
- `AuthController`: POST `/api/auth/login`, `/api/auth/refresh`, `/api/auth/logout`
- `TenantMiddleware`: extrae subdomain del `Host` header → busca empresa → inyecta `CompanyId` en HttpContext
- `JwtMiddleware`: valida token, inyecta claims en HttpContext.User
- Atributo `[TenantRequired]` para endpoints que requieren subdominio válido
- Rate limiting en login: `AddRateLimiter` con fixed window policy

**Step 4: Backend — Tenant + Admin APIs**
- `TenantController`: GET `/api/tenant/config` (público, solo branding)
- `AdminController` (rol SuperAdmin): CRUD companies, CRUD users, link reports
- `BrandingService`: upload logo a Blob Storage, retornar URL pública
- Validación: `CompanySlug` debe ser único y alfanumérico
- Seed de empresa demo con branding default

**Step 5: Backend — Company Admin APIs**
- `CompanyController` (rol CompanyAdmin): GET/POST/PUT users, GET/PUT permissions, PUT/POST branding
- `PermissionService`: lógica de `CanViewReport()`, batch update de permisos
- Validar en todo momento que CompanyAdmin solo opera sobre su propio `company_id`

**Step 6: Backend — Power BI Embed Token**
- `PowerBIService`: autenticar con Service Principal, llamar `GenerateTokenForReport`
- `ReportsController`: POST `/api/reports/{id}/embed-token`
  - Verificar permiso con `PermissionService`
  - Generar token con identidad RLS (username = company.slug)
  - Registrar en `audit_logs` (acción: VIEW_REPORT)
- Manejo de errores: PBI API down, workspace no encontrado, dataset sin RLS

**Step 7: Backend — Módulos y Auditoría**
- `ModulesController`: GET `/api/modules` (filtrado por permisos del usuario)
- `ModulesController`: GET `/api/modules/{slug}/reports`
- `AuditMiddleware`: registrar acciones en audit_logs automáticamente en writes
- `AuditController`: GET `/api/audit-logs` con paginación y filtros (fechas, usuario, acción)
- Tests unitarios de `PermissionService` y `AuthService`

**Step 8: Frontend — Scaffolding + Router + Auth**
- `npm create vite@latest databision-frontend -- --template react-ts`
- Instalar: `tailwindcss`, `shadcn-ui`, `react-router-dom`, `zustand`, `@tanstack/react-query`, `axios`, `powerbi-client-react`
- Configurar Tailwind con CSS custom properties para theming
- `App.tsx`: detectar subdominio → renderizar `AdminApp` o `PortalApp`
- `api.ts`: Axios instance con interceptor JWT auto-refresh
- `authStore.ts`: estado de usuario, tokens, login, logout
- `tenantStore.ts`: slug, branding cargado
- Rutas con `<ProtectedRoute>` que verifica JWT válido y rol

**Step 9: Frontend — ThemeProvider + Layouts**
- `ThemeProvider.tsx`: fetch `/api/tenant/config` → `theme.ts` aplica CSS vars
- `AdminLayout.tsx`: sidebar oscuro (no-brand), header, content area
- `PortalLayout.tsx`: sidebar con `var(--brand-sidebar)`, logo de empresa, módulos nav
- `Sidebar.tsx`: colapsable, indicador módulo activo
- `Header.tsx`: breadcrumbs + user menu + logout

**Step 10: Frontend — Admin Panel**
- Login page admin (sin branding especial)
- Dashboard: cards de métricas (fetch /api/admin/stats)
- Companies: tabla shadcn con búsqueda, crear/editar empresa (modal con form)
- CompanyDetail: tabs Info | Usuarios | Reportes | Branding | Config ETL
- BrandingEditor: color pickers + logo upload + preview live en miniatura
- ReportLinker: form para vincular powerbi_report_id + powerbi_workspace_id a empresa

**Step 11: Frontend — Portal Cliente**
- Login page: branded (logo + color primario de empresa)
- ModulePage: grid de cards de reportes del módulo con nombre, descripción
- ReportViewPage: `<EmbedReport>` full height, fetch embed-token, skeleton mientras carga
- `useEmbedToken`: fetch token, auto-refresh 5min antes de expiry
- Settings/Users: tabla de usuarios con create/edit (CompanyAdmin)
- Settings/Permissions: `<PermissionMatrix>` — tabla usuarios × reportes con toggles
- Settings/Branding: color picker + logo upload (CompanyAdmin)

**Step 12: ETL — Azure Data Factory Templates**
- Crear templates JSON parametrizados en `adf-templates/`
- Parámetro `company_id` en todos los pipelines
- Linked services: SAP SQL Server + SAP HANA (plantillas, no credenciales hardcodeadas)
- Datasets con path parametrizado
- Stored procedures de transformación para cada módulo
- Pipeline de testing con empresa demo
- Documentar cómo duplicar pipeline para nueva empresa

**Step 13: Seguridad + Hardening**
- Forzar HTTPS en ASP.NET Core (`UseHttpsRedirection`)
- Configurar CORS: solo orígenes `*.databision.app`
- Revisar todos los endpoints: ninguno accesible sin auth excepto los públicos declarados
- Validar que TenantMiddleware rechaza requests a empresas inactivas (`status != 'active'`)
- Sanitizar metadata en audit_logs (no guardar passwords ni tokens)
- Test de penetración básico: intentar acceder a reporte de otra empresa con JWT válido

**Step 14: Deploy + CI/CD**
- GitHub Actions: backend → `dotnet build` + `dotnet test` → `az webapp deploy`
- GitHub Actions: frontend → `npm run build` → deploy a Azure Static Web Apps
- Variables de entorno en Azure App Service Configuration (nunca en código)
- Staging slot en App Service para deploy sin downtime
- Health check endpoint: GET `/api/health`

---

## 15. Variables de Entorno

### Backend (Azure App Service Configuration)

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | Azure SQL connection string | `Server=...;Database=databision-prod;...` |
| `Jwt__PrivateKey` | RSA private key PEM para firmar JWT | `-----BEGIN RSA PRIVATE KEY-----...` |
| `Jwt__PublicKey` | RSA public key PEM para validar JWT | `-----BEGIN PUBLIC KEY-----...` |
| `Jwt__Issuer` | Issuer del JWT | `databision-api` |
| `PowerBI__TenantId` | Azure AD Tenant ID | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `PowerBI__ClientId` | App Registration Client ID | `xxxxxxxx-...` |
| `PowerBI__ClientSecret` | App Registration Client Secret | `xxxxx~xxxxx` |
| `PowerBI__WorkspaceId` | Power BI Workspace ID | `xxxxxxxx-...` |
| `Azure__BlobStorageConnectionString` | Azure Blob Storage | `DefaultEndpointsProtocol=https;...` |
| `Azure__BlobContainerName` | Container para logos | `branding` |
| `App__AdminSubdomain` | Subdominio del panel admin | `admin` |
| `App__BaseDomain` | Dominio base | `databision.app` |

### Frontend (Azure Static Web Apps / Build time)

| Variable | Descripción |
|----------|-------------|
| `VITE_API_URL` | URL base del backend API |
| `VITE_ADMIN_SUBDOMAIN` | Subdominio del admin (`admin`) |

---

## 16. Dependencies

### Backend NuGet

| Package | Propósito |
|---------|-----------|
| `Microsoft.EntityFrameworkCore.SqlServer` | ORM para Azure SQL |
| `Microsoft.EntityFrameworkCore.Tools` | CLI migrations |
| `BCrypt.Net-Next` | Hash de contraseñas |
| `System.IdentityModel.Tokens.Jwt` | Generación y validación JWT |
| `FluentValidation.AspNetCore` | Validación de DTOs |
| `Microsoft.PowerBI.Api` | SDK oficial Power BI |
| `Microsoft.Identity.Client` | MSAL para auth Service Principal |
| `Azure.Storage.Blobs` | SDK Azure Blob Storage |
| `Serilog.AspNetCore` | Logging estructurado |

### Frontend npm

| Package | Propósito |
|---------|-----------|
| `react-router-dom` | Routing SPA |
| `axios` | HTTP client |
| `@tanstack/react-query` | Data fetching y caché |
| `zustand` | Estado global (auth, tenant) |
| `powerbi-client-react` | Embed Power BI reports |
| `@radix-ui/*` | Primitivas UI (via shadcn) |
| `tailwindcss` | Estilos |
| `lucide-react` | Íconos |
| `react-hook-form` | Forms |
| `zod` | Validación schemas frontend |
| `date-fns` | Formateo de fechas |

---

## 17. Riesgos Técnicos y Decisiones

| Riesgo | Probabilidad | Decisión/Mitigación |
|--------|-------------|---------------------|
| Power BI RLS mal configurado (datos entre empresas) | Media | Testear con 2 empresas antes de ir a producción. Cada embed token debe incluir `identities` con el slug de la empresa |
| SAP B1 HANA requiere driver ODBC específico en ADF | Media | Provisionar HANA IR (Integration Runtime) en ADF. Documentar instalación del driver |
| Wildcard SSL en Azure Front Door | Baja | Front Door Standard soporta wildcard `*.databision.app` nativo. Usar managed certificate |
| Subdominio para desarrollo local | Alta (dev only) | Usar `?tenant=slug` como fallback en dev. El hook `useTenant()` lee este param en `localhost` |
| Refresh token rotation race condition | Baja | Usar `refresh_token` con campo `revoked_at`. Si llegan 2 requests simultáneos con el mismo token, el segundo falla → user hace login de nuevo |
| Número de embed tokens simultáneos (límite PBI) | Baja-Media | Power BI Embedded A1 soporta ~1000 reportes/hora. Para MVP es suficiente. Monitorear uso |
| Datos SAP no estandarizados entre clientes | Alta | Los stored procedures de transformación son por cliente. Los pipelines ADF deben ser parametrizables, no hardcodeados |

---

## 18. Skills para la Fase de Construcción

| Skill | Cuándo usar | Por qué |
|-------|-------------|---------|
| `/frontend-design` | Steps 9, 10, 11 (layouts, admin panel, portal) | UI profesional y consistente para el portal |
| `/shadcn-ui` | Step 8 (scaffolding) | Setup correcto de shadcn/ui con theming |
| `/playwright-cli` | Step 13 (seguridad) | E2E tests: verificar isolación de tenants, flujo de login, embed de reportes |

---

## 19. CLAUDE.md para el Proyecto Target

```markdown
# DataBision

Portal SaaS B2B de reportería empresarial. Clientes SAP Business One acceden a dashboards Power BI embebidos en subdominios propios (`{empresa}.databision.app`) con white-label por empresa.

## Comandos

- `dotnet run --project src/DataBision.Api` — Iniciar API backend
- `dotnet test` — Ejecutar tests
- `dotnet ef migrations add <Name> --project src/DataBision.Infrastructure --startup-project src/DataBision.Api` — Nueva migración
- `dotnet ef database update --project src/DataBision.Infrastructure --startup-project src/DataBision.Api` — Aplicar migraciones
- `npm run dev` — Frontend dev server (desde databision-frontend/)
- `npm run build` — Build producción frontend

## Tech Stack

.NET 8 Web API + EF Core 8 + Azure SQL | React 18 + TypeScript + Vite + Tailwind CSS + shadcn/ui + Zustand + TanStack Query | Power BI Embedded (Service Principal) | Azure Front Door + App Service + Static Web Apps + Blob Storage

## Arquitectura

### Multi-tenancy (CRÍTICO)
- Cada empresa tiene su subdominio: `{slug}.databision.app`
- `admin.databision.app` es el panel SuperAdmin
- `TenantMiddleware` extrae el slug del `Host` header en CADA request del backend
- Todos los endpoints protegidos validan que `company_id` del JWT coincide con el tenant del Host
- NUNCA devolver datos de una empresa en un request de otra empresa — verificar siempre

### Flujo de Auth
- Login → JWT access token (15min, RS256) + refresh token httpOnly cookie (7 días)
- Claims JWT: `sub`, `email`, `role`, `company_id`, `company_slug`, `module_ids[]`
- TanStack Query + Axios interceptor: auto-refresh cuando access token expira
- Refresh tokens: almacenados hasheados en DB, rotate on use

### Power BI Embedded
- Service Principal (NO Master User)
- Backend genera embed token en `/api/reports/{id}/embed-token`
- Token incluye identidad RLS: `username = company.slug`, `role = "CompanyRole"`
- Frontend usa `powerbi-client-react` con token del backend (nunca credenciales directas)
- Auto-refresh del embed token 5 min antes de expirar

### Theming Dinámico
- `GET /api/tenant/config` es PÚBLICO (sin auth). Devuelve branding de la empresa
- `ThemeProvider` aplica CSS custom properties en `:root`: `--brand-primary`, `--brand-sidebar`, etc.
- Tailwind configurado con colores `brand-*` que usan `var(--brand-primary)` etc.
- En localStorage se cachea el config del tenant para evitar flash de colores

### Estructura del Backend
- `DataBision.Domain` → solo entidades y enums. Sin dependencias externas.
- `DataBision.Application` → servicios e interfaces. Sin EF ni HTTP. Solo lógica de negocio.
- `DataBision.Infrastructure` → EF Core, PowerBI SDK, Azure Blob. Implementa interfaces de Application.
- `DataBision.Api` → Controllers, Middleware, Program.cs. Orquesta todo.

### Estructura del Frontend
- `src/apps/admin/` → todo lo de `admin.databision.app`
- `src/apps/portal/` → todo lo de `{slug}.databision.app`
- `App.tsx` detecta subdominio y renderiza el app correcto
- En dev local: `?tenant=slug` simula subdominios

## Reglas de Código

1. **Aislamiento de tenants:** Todo query a tablas de datos DEBE incluir filtro `company_id` — nunca confiar en que el dato ya está filtrado "más arriba"
2. **Una responsabilidad por servicio:** Controllers solo orquestan. Lógica de negocio en Application/Services.
3. **DTOs en la frontera:** Nunca exponer entidades de dominio directamente desde controllers. Siempre mapear a DTO.
4. **CSS vars para colores brand:** Nunca hardcodear colores de marca en componentes. Usar `var(--brand-primary)` o clase Tailwind `brand-*`.
5. **Audit logging:** Toda acción de escritura (create/update/delete) y VIEW_REPORT debe quedar en audit_logs. Usar AuditService, no escribir directo a DB.
6. **Error responses:** Siempre `{ "error": "snake_case_code", "message": "Human readable" }`. HTTP status correcto.
7. **No raw SQL:** Solo EF Core con parámetros. Si necesitas performance, usar FromSqlRaw con SqlParameter[], nunca interpolación de strings.

## Diseño Visual

### Colores base (platform)
- Sidebar: `#0F172A` | Sidebar active: `#1E293B` | Background: `#F8FAFC`
- Surface: `#FFFFFF` | Border: `#E2E8F0` | Text: `#0F172A` | Muted: `#64748B`
- Success: `#16A34A` | Error: `#DC2626` | Warning: `#D97706`

### Colores brand (sobreescritos por empresa)
- `--brand-primary`: default `#2563EB`
- `--brand-secondary`: default `#64748B`
- `--brand-sidebar`: default `#0F172A`

### Tipografía
- Font: Inter (Google Fonts)
- Body: 14px/400 | Labels: 13px/500 | Headings: Inter 600-700 | Numbers: tabular nums

### Estilo
- Border radius: 6px default, 8px cards | Sombras: solo shadow-sm | Spacing: base 4px
- Compacto: rows de tabla 44px, no hay espacios generosos — es una herramienta de datos

## Variables de Entorno (Backend)

| Variable | Descripción |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | Azure SQL connection string |
| `Jwt__PrivateKey` | RSA private key PEM |
| `Jwt__PublicKey` | RSA public key PEM |
| `PowerBI__TenantId` | Azure AD tenant |
| `PowerBI__ClientId` | Service Principal app ID |
| `PowerBI__ClientSecret` | Service Principal secret |
| `PowerBI__WorkspaceId` | Power BI workspace ID |
| `Azure__BlobStorageConnectionString` | Blob Storage |
| `App__BaseDomain` | `databision.app` |

## Reglas No Negociables

1. TypeScript strict mode activado. Sin `any`. Usar tipos explícitos siempre.
2. Cada request del backend valida que el usuario pertenece al tenant del Host header.
3. Los refresh tokens se guardan HASHEADOS en DB — nunca en texto plano.
4. Ninguna credencial, secret ni connection string en el código fuente.
5. Cada acción escrita (mutations) registra entrada en audit_logs.
```

---

## 20. Reglas No Negociables para el Builder

1. **Aislamiento de tenants es inviolable.** Antes de cada query a tablas de datos, verificar `company_id` del JWT. Un test automático debe verificar que el usuario de EmpresaA no puede acceder a datos de EmpresaB con un JWT válido de EmpresaA.
2. **Power BI RLS debe estar configurado ANTES de ir a producción.** El embed token siempre debe incluir `identities` con el `company.slug`. Sin RLS activo, los reportes de todas las empresas son visibles para cualquier usuario autenticado.
3. **Nunca almacenar tokens o passwords en texto plano.** Access tokens solo en memory del frontend (no localStorage). Refresh tokens hasheados en DB.
4. **El subdominio define el tenant, no un parámetro.** En producción, nunca aceptar `?company_id=X` para cambiar de contexto. Solo el Host header.
5. **Migrations son versionadas y nunca se editan retroactivamente.** Si hay un error en una migration, crear una nueva migration correctiva.
6. **Los pipelines ADF son plantillas parametrizadas.** Cada nueva empresa recibe su propia instancia del pipeline copiando la plantilla y sobreescribiendo los parámetros. Nunca un pipeline hardcodeado para una empresa.
7. **El SuperAdmin nunca pertenece a ninguna empresa.** `user_companies` vacío para SuperAdmin. El `company_id` en su JWT es null. Los endpoints `/api/admin/*` no requieren subdominio de empresa.
8. **Toda API response es consistente:** `{ data: T }` en éxito, `{ error: string, message: string }` en error. HTTP status correcto en todos los casos.
