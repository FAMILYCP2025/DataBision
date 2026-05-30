# DataBision — Implementation Roadmap v1

> **For agentic workers:** Use `superpowers:executing-plans` or `superpowers:subagent-driven-development` to implement tasks phase by phase. Steps use checkbox syntax for tracking.

**Goal:** Convertir la arquitectura aprobada en un MVP vendible con el menor riesgo de reescritura posible.

**Architecture ref:** `docs/master-architecture.md` v4.0 — fuente de verdad. En caso de contradicción con este documento, `master-architecture.md` tiene precedencia.

**Estado actual del código (2026-05-29):**
- Ingest API: controladores, filtros, servicios, repositorios y `DataBision.Shared` creados pero apuntando a SQL Server
- `TenantMiddleware`, `ApiKeyAuthFilter`, `IngestService`, `SapRawRepository` presentes
- Tests unitarios para filters, services y shared: presentes
- BD objetivo: aún **SQL Server / Azure SQL** — migración a **Supabase PostgreSQL pendiente**
- Power BI: referencias activas en docs y posiblemente en configuración — deben limpiarse

---

## Índice

1. [Estado Actual del Código](#estado-actual)
2. [Fases del Roadmap](#fases)
3. [Sprint 0 — Limpieza y Alineación Técnica](#sprint-0)
4. [Sprint 1 — Supabase + Ingest API Funcional](#sprint-1)
5. [Sprint 2 — Dedicated Extractor MVP](#sprint-2)
6. [Sprint 3 — Native BI Mínimo](#sprint-3)
7. [Fases Post-Sprint 3](#post-sprint-3)
8. [Qué NO Construir Todavía](#no-construir)
9. [Definition of Done General](#dod)
10. [Mapa de Riesgos](#riesgos)
11. [Recomendación Final](#recomendacion)

---

## 1. Estado Actual del Código {#estado-actual}

### Ya construido (no tocar en Sprint 0 — solo alinear)

| Componente | Archivos | Estado |
|---|---|---|
| Ingest API controller | `SapB1IngestController.cs` | Presente — validar que compila |
| Checkpoint controller | `IngestCheckpointController.cs` | Presente |
| API Key filter | `ApiKeyAuthFilter.cs` | Presente — tests OK |
| IngestService | `Application/Services/IngestService.cs` | Presente — tests OK |
| Interfaces | `Application/Interfaces/Ingest/` | Presente |
| DTOs | `Application/DTOs/Ingest/` | Presente |
| SapRawRepository | `Infrastructure/Repositories/Ingest/` | Presente — usa SQL Server → **migrar** |
| StagingDbContext | `Infrastructure/Data/Staging/` | Presente — usa SQL Server → **migrar** |
| Shared (hash/normalize) | `DataBision.Shared/` | Presente — tests OK |
| TenantMiddleware | `Api/Middleware/TenantMiddleware.cs` | Presente — modificado |

### Pendiente de creación

- Supabase PostgreSQL schema (DDL completo)
- Npgsql como provider (reemplaza SqlServer)
- `INSERT ON CONFLICT` reemplazando T-SQL MERGE en `SapRawRepository`
- EF Migrations para Supabase (las actuales son para SQL Server)
- Dedicated Extractor (proyecto separado)
- Frontend (databision-frontend/)
- Background Workers
- Analytics API endpoints

---

## 2. Fases del Roadmap {#fases}

```
Sprint 0   Limpieza + alineación técnica              1 semana      OBLIGATORIO MVP
Sprint 1   Supabase + Ingest API funcional            2 semanas     OBLIGATORIO MVP
Sprint 2   Dedicated Extractor MVP                    2 semanas     OBLIGATORIO MVP
Sprint 3   Native BI mínimo                           3 semanas     OBLIGATORIO MVP
─────────────────────────────────────────────────────
CHECKPOINT: MVP VENDIBLE ────────────────────────────────────── Semana 8
─────────────────────────────────────────────────────
Fase 4     Sync Center + Operational Intelligence     2 semanas     Post-MVP / Fase 1
Fase 5     Portal comercial MVP (branding + auth)     2 semanas     Post-MVP / Fase 1
Fase 6     Mode C — Service Layer Polling             1.5 semanas   Post-MVP / Fase 2
Fase 7     Mode B — Service Layer Delta               2.5 semanas   Post-MVP / Fase 2
Fase 8     Operational Live Layer                     2 semanas     Post-MVP / Fase 2
Fase 9     Alerting + Recommendations                 3 semanas     Fase 1.5
```

**Principio de ordenamiento:**
1. Primero, lo que no se puede reemplazar sin reescribir (Supabase schema, Ingest API contract)
2. Segundo, lo que permite tener datos reales (Extractor)
3. Tercero, lo que permite demostrar valor (BI mínimo)
4. Cuarto, el resto en orden de valor comercial

---

## 3. Sprint 0 — Limpieza y Alineación Técnica {#sprint-0}

**Duración:** 1 semana  
**MVP:** Obligatorio (sin esto el código no es coherente con la arquitectura aprobada)  
**Objetivo:** Dejar el repo y la solución en estado limpio, buildable y honesto antes de hacer cualquier trabajo de implementación.

### Criterios de Aceptación

- [ ] `dotnet build` pasa sin errores ni warnings sobre referencias rotas
- [ ] `dotnet test` pasa al 100%
- [ ] `.gitignore` cubre todos los archivos de secretos
- [ ] No hay credenciales en código ni en archivos tracked
- [ ] Todos los docs de arquitectura superseded están marcados como tal
- [ ] Power BI no aparece como componente central en ningún archivo de configuración activo
- [ ] El archivo `appsettings.Development.template.json` no contiene valores reales

---

### Tarea S0-1: Auditoría de Secretos y .gitignore

**Archivos:**
- Revisar: `.gitignore`
- Revisar: `src/DataBision.Api/appsettings.json`
- Revisar: `src/DataBision.Api/appsettings.Development.template.json`
- Revisar: `src/DataBision.Api/appsettings.Development.json` (si existe — NO debe estar en git)

**Riesgo:** Credenciales en git comprometen la seguridad de todos los tenants.

- [ ] Verificar que `.gitignore` incluye:
  ```
  appsettings.Development.json
  appsettings.*.json
  !appsettings.json
  !appsettings.Development.template.json
  *.pfx
  *.pem
  .env
  .env.*
  databision_dev.db
  databision_dev.db-shm
  databision_dev.db-wal
  ```

- [ ] Verificar que `appsettings.json` tiene solo placeholders o valores no sensibles:
  ```json
  {
    "ConnectionStrings": {
      "DefaultConnection": "PLACEHOLDER",
      "StagingConnection": "PLACEHOLDER"
    },
    "Jwt": {
      "PrivateKey": "PLACEHOLDER",
      "PublicKey": "PLACEHOLDER"
    }
  }
  ```

- [ ] Verificar que `appsettings.Development.template.json` tiene el formato Supabase correcto y no valores reales:
  ```json
  {
    "ConnectionStrings": {
      "DefaultConnection": "Data Source=databision_dev.db",
      "StagingConnection": "Host=db.YOUR_SUPABASE_ID.supabase.co;Port=6543;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true;Pooling=true;"
    },
    "Ingest": {
      "ApiKeys": {
        "dev-key-001": "tenant-dev:company-dev-001"
      }
    }
  }
  ```

- [ ] Buscar en todos los archivos .cs, .json y .md la cadena `Password=` — si existe fuera del template, es una fuga.

- [ ] Verificar que `appsettings.Development.json` NO está trackeado en git (`git ls-files | grep Development.json` debe retornar vacío).

- [ ] Commit si hay cambios en `.gitignore`:
  ```
  git add .gitignore
  git commit -m "chore: harden .gitignore for secrets and local db"
  ```

---

### Tarea S0-2: Validar Build Backend

**Archivos:**
- `DataBision.sln`
- Todos los `.csproj`

- [ ] Ejecutar `dotnet build DataBision.sln` y registrar errores.

- [ ] Si hay errores de dependencias SQL Server que aún no se han actualizado, documentarlos sin arreglarlos todavía — serán el trabajo del Sprint 1. El objetivo aquí es saber exactamente qué está roto.

- [ ] Si hay warnings de nullable, deprecated APIs o ambiguous references que bloquean el build, arreglarlos ahora.

- [ ] Ejecutar `dotnet test` y verificar que todos los tests existentes pasan. Si alguno falla, arreglarlo antes de continuar.

- [ ] Documentar en este mismo archivo bajo "Estado de Build Sprint 0" qué compila y qué no.

- [ ] Commit:
  ```
  git commit -m "chore: baseline build — all tests green before migration"
  ```

---

### Tarea S0-3: Marcar Documentos Superseded

**Archivos:**
- `docs/phase-3-bi-architecture.md`
- `docs/databision-product-architecture.md`
- `docs/azure-sql-staging-design.md`
- `docs/powerbi-pro-import-mode-strategy.md`
- `docs/commercial-mvp-strategy.md`

Para cada uno agregar al inicio del archivo:

```markdown
> ⚠️ **ESTADO: SUPERSEDED / REFERENCIA HISTÓRICA**
> 
> Este documento ha sido reemplazado por `docs/master-architecture.md` v4.0.
> Se mantiene como referencia histórica únicamente. No usar para tomar decisiones técnicas.
> Fecha de supersesión: 2026-05-29
```

Los documentos que se marcan y su razón:

| Documento | Estado | Razón |
|---|---|---|
| `phase-3-bi-architecture.md` | SUPERSEDED | Power BI no es el núcleo. ADR-002. |
| `databision-product-architecture.md` | PARCIALMENTE SUPERSEDED | Extracción SAP válida; Azure SQL no. |
| `azure-sql-staging-design.md` | REFERENCIA ENTERPRISE | Válido solo para Plan Enterprise futuro. |
| `powerbi-pro-import-mode-strategy.md` | REPOSICIONADO | Add-on opcional, no producto principal. |
| `commercial-mvp-strategy.md` | REQUIERE ACTUALIZACIÓN | Precios USD 300/500 → USD 350/600. |

- [ ] Agregar nota SUPERSEDED a cada doc listado.
- [ ] Commit:
  ```
  git commit -m "docs: mark superseded architecture documents"
  ```

---

### Tarea S0-4: Limpiar Referencias Power BI como Núcleo

**Archivos:**
- `src/DataBision.Api/appsettings.json`
- `src/DataBision.Api/Program.cs`
- Cualquier `.csproj` que tenga referencia a Power BI SDK como dependencia principal

- [ ] Buscar en todos los `.cs` y `.json`: `PowerBI`, `EmbedToken`, `powerbi-client`, `MasterUser`.
- [ ] Si alguna referencia está en código de producción activo (no en docs), marcarla con `// TODO PHASE-4: Power BI as add-on` y documentar aquí.
- [ ] Si algún paquete NuGet de Power BI SDK está referenciado como dependencia core, moverlo a comentario o eliminarlo con nota.
- [ ] El objetivo no es eliminar el conocimiento de Power BI — es que no aparezca como dependencia técnica del MVP.

- [ ] Commit si hay cambios:
  ```
  git commit -m "chore: remove power bi as core dependency (moved to phase 4 add-on)"
  ```

---

### Tarea S0-5: Estabilizar Solución y Preparar Ramas

- [ ] Verificar `git status` — no debe haber archivos sin intención clara.
  - Archivos `??` que son artefactos de herramientas (`.vs/`, `bin/`, `obj/`) → agregar al `.gitignore`.
  - Archivos `??` que son código nuevo → hacer commit o documentar por qué están sin commit.
  - Archivo `nul` en el status: eliminar y agregar al `.gitignore`.

- [ ] Crear rama `feat/sprint-1-supabase` desde `main` limpio.

- [ ] Registrar en este documento bajo "Estado Sprint 0 al cierre":
  - SHA del commit de baseline
  - Tests que pasan
  - Errores de build pendientes de Sprint 1

**Duración estimada S0:** 4–5 días  
**Riesgo:** Bajo — solo limpieza. El riesgo principal es encontrar más deuda técnica de la esperada.

---

## 4. Sprint 1 — Supabase + Ingest API Funcional {#sprint-1}

**Duración:** 2 semanas  
**MVP:** Obligatorio  
**Dependencias:** Sprint 0 completo (build limpio)  
**Objetivo:** Tener datos SAP (sintéticos) llegando a Supabase PostgreSQL de forma idempotente, con checkpoints y auditoría funcional.

### Criterios de Aceptación

- [ ] `dotnet build` pasa con Npgsql como provider (sin referencias a SQL Server en la ruta de producción)
- [ ] Schema completo creado en Supabase: `raw.*`, `stg.*` (vacío), `dim.*` (vacío), `fact.*` (vacío), `ctl.*`, `audit.*`
- [ ] Las 7 tablas `raw.sap_*` existen con sus PKs e índices
- [ ] POST `/api/ingest/sap-b1` con payload de OINV real retorna `{ inserted: N, updated: 0, skipped: 0 }`
- [ ] Segundo POST idéntico retorna `{ inserted: 0, updated: 0, skipped: N }` (idempotencia)
- [ ] POST con un campo modificado retorna `{ inserted: 0, updated: 1, skipped: N-1 }` (hash guard)
- [ ] `ctl.ingest_checkpoint` registra la última ejecución por tabla y tenant
- [ ] `audit.events` registra cada operación de ingest
- [ ] Tests de integración con base PostgreSQL de test pasan

---

### Tarea S1-1: Migrar Provider SQL Server → Npgsql

**Archivos:**
- `src/DataBision.Infrastructure/DataBision.Infrastructure.csproj`
- `src/DataBision.Infrastructure/Data/Staging/StagingDbContext.cs`
- `src/DataBision.Infrastructure/Data/Staging/StagingDatabaseExtensions.cs`
- `src/DataBision.Infrastructure/Data/AppDbContext.cs`
- `src/DataBision.Api/Program.cs`

- [ ] En `Infrastructure.csproj`:
  - Eliminar: `<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />`
  - Eliminar: `<PackageReference Include="Microsoft.Data.SqlClient" />`
  - Agregar: `<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.4" />`
  - Agregar: `<PackageReference Include="Npgsql" Version="8.0.3" />`
  - Mantener: `<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />` (dev local)

- [ ] En `StagingDbContext.cs`: `UseSqlServer(...)` → `UseNpgsql(...)`

- [ ] En `StagingDatabaseExtensions.cs`: `UseSqlServer(...)` → `UseNpgsql(...)`

- [ ] En `AppDbContext.cs` (ruta de producción): `UseSqlServer(...)` → `UseNpgsql(...)`

- [ ] Ejecutar `dotnet build` — debe pasar sin errores relacionados con SqlServer.

- [ ] Commit:
  ```
  git commit -m "feat: migrate database provider from sql server to npgsql/supabase"
  ```

---

### Tarea S1-2: Eliminar Migraciones SQL Server y Crear Schema DDL PostgreSQL

**Archivos:**
- `src/DataBision.Infrastructure/Data/Staging/Migrations/` → eliminar completamente
- Crear: `src/DataBision.Infrastructure/Data/Staging/Migrations/` (nuevo, vacío antes de `dotnet ef`)

**Nota:** Las migraciones de SQL Server tienen tipos incompatibles con PostgreSQL (`NVARCHAR`, `MERGE`, `ISNULL`, `GETUTCDATE()`). Deben regenerarse desde cero.

- [ ] **Decidir antes de actuar:** ¿existen migraciones de SQL Server que se aplicaron en algún entorno productivo?

  **Caso A — Aún no hay datos en producción (situación actual del proyecto en Sprint 0):**
  ```powershell
  # Reset controlado — solo si las migraciones existentes son de SQL Server y NO se aplicaron en producción
  Remove-Item -Recurse -Force src/DataBision.Infrastructure/Data/Staging/Migrations
  ```

  **Caso B — Ya hay datos reales en producción:**
  NO eliminar las migraciones existentes. Crear una migración correctiva nueva:
  ```
  dotnet ef migrations add CorrectivePostgresSchema ...
  ```
  Nunca eliminar migraciones que se hayan aplicado en un entorno con datos reales de clientes.

  > **Política inmutable:** las migraciones aplicadas en producción son parte del historial del sistema. Eliminarlas equivale a perder la trazabilidad del schema. Si hay una migración incorrecta, se corrige creando una nueva migración correctiva.

- [ ] Crear migración inicial vacía para que EF genere el scaffolding correcto:
  ```
  dotnet ef migrations add InitialStagingSchema \
    --project src/DataBision.Infrastructure \
    --startup-project src/DataBision.Api \
    --context StagingDbContext
  ```

- [ ] El `Up()` de esta migración debe incluir el DDL completo. Reemplazar el contenido generado automáticamente con el DDL de schemas y tablas detallado en Tarea S1-3.

---

### Tarea S1-3: DDL Completo Supabase PostgreSQL

**Archivo:** `src/DataBision.Infrastructure/Data/Staging/Migrations/[timestamp]_InitialStagingSchema.cs`

El método `Up()` debe ejecutar el siguiente DDL como `migrationBuilder.Sql(...)`:

```sql
-- Schemas
CREATE SCHEMA IF NOT EXISTS raw;
CREATE SCHEMA IF NOT EXISTS stg;
CREATE SCHEMA IF NOT EXISTS dim;
CREATE SCHEMA IF NOT EXISTS fact;
CREATE SCHEMA IF NOT EXISTS ctl;
CREATE SCHEMA IF NOT EXISTS oper;
CREATE SCHEMA IF NOT EXISTS audit;

-- =====================
-- CTL tables
-- =====================

CREATE TABLE IF NOT EXISTS ctl.companies (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    slug            VARCHAR(50) UNIQUE NOT NULL,
    display_name    VARCHAR(200) NOT NULL,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS ctl.ingest_checkpoint (
    id              BIGSERIAL PRIMARY KEY,
    company_id      UUID NOT NULL REFERENCES ctl.companies(id),
    table_name      VARCHAR(50) NOT NULL,
    last_watermark  DATE,
    last_ts_norm    CHAR(6),
    last_run_utc    TIMESTAMPTZ,
    rows_inserted   INT NOT NULL DEFAULT 0,
    rows_updated    INT NOT NULL DEFAULT 0,
    rows_skipped    INT NOT NULL DEFAULT 0,
    UNIQUE (company_id, table_name)
);

CREATE TABLE IF NOT EXISTS ctl.run_log (
    id              BIGSERIAL PRIMARY KEY,
    company_id      UUID NOT NULL REFERENCES ctl.companies(id),
    table_name      VARCHAR(50) NOT NULL,
    started_at_utc  TIMESTAMPTZ NOT NULL,
    finished_at_utc TIMESTAMPTZ,
    status          VARCHAR(20) NOT NULL, -- running | success | failed
    rows_processed  INT,
    error_message   TEXT
);

CREATE TABLE IF NOT EXISTS ctl.quality_issues (
    id              BIGSERIAL PRIMARY KEY,
    company_id      UUID NOT NULL REFERENCES ctl.companies(id),
    table_name      VARCHAR(50) NOT NULL,
    issue_type      VARCHAR(50) NOT NULL,
    description     TEXT,
    row_pk          TEXT,
    detected_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS ctl.alert_rules (
    id              BIGSERIAL PRIMARY KEY,
    company_id      UUID NOT NULL REFERENCES ctl.companies(id),
    alert_type      VARCHAR(50) NOT NULL, -- operational | business_metric | data_quality
    condition_sql   TEXT NOT NULL,
    threshold       NUMERIC,
    cooldown_min    INT NOT NULL DEFAULT 60,
    channels        JSONB NOT NULL DEFAULT '["in_portal"]',
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS ctl.recommendation_rules (
    id              BIGSERIAL PRIMARY KEY,
    company_id      UUID,  -- NULL = regla global aplicable a todos
    domain          VARCHAR(30) NOT NULL, -- sales | inventory | purchases | customers | collections
    rule_id         VARCHAR(50) NOT NULL,
    title_template  TEXT NOT NULL,
    body_template   TEXT NOT NULL,
    action_template TEXT NOT NULL,
    condition_sql   TEXT NOT NULL,
    attribution_sql TEXT,
    severity        VARCHAR(20) NOT NULL DEFAULT 'warning', -- info | warning | critical
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    UNIQUE (company_id, rule_id)
);

-- =====================
-- AUDIT table
-- =====================

CREATE TABLE IF NOT EXISTS audit.events (
    id              BIGSERIAL PRIMARY KEY,
    company_id      UUID,
    user_id         UUID,
    event_type      VARCHAR(50) NOT NULL, -- INGEST | VIEW_REPORT | LOGIN | EXPORT | ACTION
    entity_type     VARCHAR(50),
    entity_id       TEXT,
    payload         JSONB,
    created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_audit_events_company_created 
    ON audit.events (company_id, created_at_utc DESC);

-- =====================
-- RAW tables (réplica SAP)
-- =====================

-- raw.sap_oinv — Facturas de venta
CREATE TABLE IF NOT EXISTS raw.sap_oinv (
    company_id          UUID NOT NULL,
    "DocEntry"          INTEGER NOT NULL,
    "DocNum"            INTEGER,
    "DocDate"           DATE,
    "DocDueDate"        DATE,
    "CardCode"          VARCHAR(15),
    "CardName"          VARCHAR(100),
    "DocTotal"          NUMERIC(19,6),
    "DocTotalFC"        NUMERIC(19,6),
    "DocTotalSy"        NUMERIC(19,6),
    "PaidToDate"        NUMERIC(19,6),
    "GrosProfit"        NUMERIC(19,6),
    "DiscPrcnt"         NUMERIC(19,6),
    "Cancelled"         CHAR(1),
    "DocStatus"         CHAR(1),
    "SlpCode"           INTEGER,
    "Comments"          TEXT,
    "NumAtCard"         VARCHAR(100),
    "UpdateDate"        DATE,
    "UpdateTS"          VARCHAR(10),
    "UpdateTSNorm"      CHAR(6),
    "DocCur"            VARCHAR(3),
    source_hash_hex     CHAR(64) NOT NULL,
    raw_created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    raw_updated_at_utc  TIMESTAMPTZ,
    PRIMARY KEY (company_id, "DocEntry")
);

-- raw.sap_inv1 — Líneas de factura
CREATE TABLE IF NOT EXISTS raw.sap_inv1 (
    company_id          UUID NOT NULL,
    "DocEntry"          INTEGER NOT NULL,
    "LineNum"           INTEGER NOT NULL,
    "ItemCode"          VARCHAR(20),
    "Dscription"        VARCHAR(100),
    "Quantity"          NUMERIC(19,6),
    "Price"             NUMERIC(19,6),
    "LineTotal"         NUMERIC(19,6),
    "GrssProfit"        NUMERIC(19,6),
    "DiscPrcnt"         NUMERIC(19,6),
    "WhsCode"           VARCHAR(8),
    "UpdateDate"        DATE,
    "UpdateTS"          VARCHAR(10),
    "UpdateTSNorm"      CHAR(6),
    source_hash_hex     CHAR(64) NOT NULL,
    raw_created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    raw_updated_at_utc  TIMESTAMPTZ,
    PRIMARY KEY (company_id, "DocEntry", "LineNum")
);

-- raw.sap_orin — Notas de crédito
CREATE TABLE IF NOT EXISTS raw.sap_orin (
    company_id          UUID NOT NULL,
    "DocEntry"          INTEGER NOT NULL,
    "DocNum"            INTEGER,
    "DocDate"           DATE,
    "CardCode"          VARCHAR(15),
    "CardName"          VARCHAR(100),
    "DocTotal"          NUMERIC(19,6),
    "DocTotalFC"        NUMERIC(19,6),
    "DocTotalSy"        NUMERIC(19,6),
    "Cancelled"         CHAR(1),
    "DocStatus"         CHAR(1),
    "SlpCode"           INTEGER,
    "Comments"          TEXT,
    "UpdateDate"        DATE,
    "UpdateTS"          VARCHAR(10),
    "UpdateTSNorm"      CHAR(6),
    "DocCur"            VARCHAR(3),
    source_hash_hex     CHAR(64) NOT NULL,
    raw_created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    raw_updated_at_utc  TIMESTAMPTZ,
    PRIMARY KEY (company_id, "DocEntry")
);

-- raw.sap_rin1 — Líneas nota de crédito
CREATE TABLE IF NOT EXISTS raw.sap_rin1 (
    company_id          UUID NOT NULL,
    "DocEntry"          INTEGER NOT NULL,
    "LineNum"           INTEGER NOT NULL,
    "ItemCode"          VARCHAR(20),
    "Dscription"        VARCHAR(100),
    "Quantity"          NUMERIC(19,6),
    "Price"             NUMERIC(19,6),
    "LineTotal"         NUMERIC(19,6),
    "UpdateDate"        DATE,
    "UpdateTS"          VARCHAR(10),
    "UpdateTSNorm"      CHAR(6),
    source_hash_hex     CHAR(64) NOT NULL,
    raw_created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    raw_updated_at_utc  TIMESTAMPTZ,
    PRIMARY KEY (company_id, "DocEntry", "LineNum")
);

-- raw.sap_ocrd — Clientes / Business Partners
CREATE TABLE IF NOT EXISTS raw.sap_ocrd (
    company_id          UUID NOT NULL,
    "CardCode"          VARCHAR(15) NOT NULL,
    "CardName"          VARCHAR(100),
    "CardType"          CHAR(1),
    "Phone1"            VARCHAR(20),
    "E_Mail"            VARCHAR(100),
    "CntctPrsn"         VARCHAR(90),
    "Balance"           NUMERIC(19,6),
    "BalDueDeb"         NUMERIC(19,6),
    "SlpCode"           INTEGER,
    "GroupCode"         SMALLINT,
    "CreditLine"        NUMERIC(19,6),
    "Discount"          NUMERIC(19,6),
    "VatLiable"         CHAR(1),
    "Country"           CHAR(3),
    "City"              VARCHAR(100),
    "ZipCode"           VARCHAR(20),
    "Notes"             TEXT,
    "UpdateDate"        DATE,
    "UpdateTS"          VARCHAR(10),
    "UpdateTSNorm"      CHAR(6),
    source_hash_hex     CHAR(64) NOT NULL,
    raw_created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    raw_updated_at_utc  TIMESTAMPTZ,
    PRIMARY KEY (company_id, "CardCode")
);

-- raw.sap_oitm — Items
CREATE TABLE IF NOT EXISTS raw.sap_oitm (
    company_id          UUID NOT NULL,
    "ItemCode"          VARCHAR(20) NOT NULL,
    "ItemName"          VARCHAR(100),
    "ItmsGrpCod"        SMALLINT,
    "OnHand"            NUMERIC(19,6),
    "IsCommited"        NUMERIC(19,6),
    "OnOrder"           NUMERIC(19,6),
    "MinLevel"          NUMERIC(19,6),
    "MaxLevel"          NUMERIC(19,6),
    "PrchseItem"        CHAR(1),
    "SellItem"          CHAR(1),
    "InvntItem"         CHAR(1),
    "AvgPrice"          NUMERIC(19,6),
    "LastPurPrc"        NUMERIC(19,6),
    "UpdateDate"        DATE,
    "UpdateTS"          VARCHAR(10),
    "UpdateTSNorm"      CHAR(6),
    source_hash_hex     CHAR(64) NOT NULL,
    raw_created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    raw_updated_at_utc  TIMESTAMPTZ,
    PRIMARY KEY (company_id, "ItemCode")
);

-- raw.sap_oslp — Vendedores
CREATE TABLE IF NOT EXISTS raw.sap_oslp (
    company_id          UUID NOT NULL,
    "SlpCode"           INTEGER NOT NULL,
    "SlpName"           VARCHAR(50),
    "Commission"        NUMERIC(19,6),
    "GroupCode"         SMALLINT,
    "Active"            CHAR(1),
    "UpdateDate"        DATE,
    "UpdateTS"          VARCHAR(10),
    "UpdateTSNorm"      CHAR(6),
    source_hash_hex     CHAR(64) NOT NULL,
    raw_created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    raw_updated_at_utc  TIMESTAMPTZ,
    PRIMARY KEY (company_id, "SlpCode")
);

-- Índices para queries de analítica frecuentes
CREATE INDEX IF NOT EXISTS idx_sap_oinv_company_date 
    ON raw.sap_oinv (company_id, "DocDate" DESC);
CREATE INDEX IF NOT EXISTS idx_sap_oinv_company_card 
    ON raw.sap_oinv (company_id, "CardCode");
CREATE INDEX IF NOT EXISTS idx_sap_inv1_company_item 
    ON raw.sap_inv1 (company_id, "ItemCode");
CREATE INDEX IF NOT EXISTS idx_sap_ocrd_company 
    ON raw.sap_ocrd (company_id);
CREATE INDEX IF NOT EXISTS idx_sap_orin_company_date 
    ON raw.sap_orin (company_id, "DocDate" DESC);

-- =====================
-- OPER tables (vacías en Sprint 1, creadas para no reescribir schema)
-- =====================

CREATE TABLE IF NOT EXISTS oper.freshness_scores (
    id              BIGSERIAL PRIMARY KEY,
    company_id      UUID NOT NULL,
    table_name      VARCHAR(50) NOT NULL,
    score           NUMERIC(5,2),
    computed_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS oper.alerts (
    id              BIGSERIAL PRIMARY KEY,
    company_id      UUID NOT NULL,
    alert_type      VARCHAR(50) NOT NULL,
    title           TEXT NOT NULL,
    body            TEXT,
    severity        VARCHAR(20) NOT NULL DEFAULT 'warning',
    status          VARCHAR(20) NOT NULL DEFAULT 'active', -- active | dismissed | resolved
    triggered_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resolved_at_utc TIMESTAMPTZ,
    dismissed_by    UUID
);

CREATE TABLE IF NOT EXISTS oper.recommendations (
    id              BIGSERIAL PRIMARY KEY,
    company_id      UUID NOT NULL,
    rule_id         VARCHAR(50) NOT NULL,
    domain          VARCHAR(30) NOT NULL,
    title           TEXT NOT NULL,
    body            TEXT NOT NULL,
    suggested_action TEXT NOT NULL,
    severity        VARCHAR(20) NOT NULL DEFAULT 'warning',
    generated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    status          VARCHAR(20) NOT NULL DEFAULT 'active', -- active | dismissed | handled
    dismissed_by    UUID,
    handled_at      TIMESTAMPTZ,
    handling_note   TEXT
);

CREATE TABLE IF NOT EXISTS oper.business_targets (
    id              BIGSERIAL PRIMARY KEY,
    company_id      UUID NOT NULL,
    metric          VARCHAR(50) NOT NULL,
    period          VARCHAR(7) NOT NULL, -- YYYY-MM
    target_value    NUMERIC(19,6) NOT NULL,
    created_by      UUID,
    created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS oper.annotations (
    id              BIGSERIAL PRIMARY KEY,
    company_id      UUID NOT NULL,
    entity_type     VARCHAR(50),
    entity_id       TEXT,
    note            TEXT NOT NULL,
    created_by      UUID,
    created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

- [ ] Pegar DDL completo en `Up()` de la migración generada.
- [ ] Agregar `Down()` que hace `DROP SCHEMA raw CASCADE` etc. (solo para dev — en prod nunca se ejecuta Down).
- [ ] Aplicar migración contra Supabase dev:
  ```
  dotnet ef database update --project src/DataBision.Infrastructure --startup-project src/DataBision.Api --context StagingDbContext
  ```
- [ ] Verificar en Supabase Dashboard que los 7 schemas existen y las tablas tienen los tipos correctos.
- [ ] Commit:
  ```
  git commit -m "feat: initial supabase postgresql schema — all raw, ctl, oper, audit tables"
  ```

---

### Tarea S1-4: Reescribir SapRawRepository — INSERT ON CONFLICT

**Archivo:** `src/DataBision.Infrastructure/Repositories/Ingest/SapRawRepository.cs`

Este es el cambio más crítico del Sprint 1. T-SQL MERGE no existe en PostgreSQL.

**Equivalencias:**

| T-SQL (actual) | PostgreSQL (nuevo) |
|---|---|
| `MERGE INTO tgt USING src ON (pk)` | `INSERT INTO ... ON CONFLICT (pk) DO UPDATE SET ...` |
| `WHEN MATCHED AND hash_changed THEN UPDATE` | `ON CONFLICT DO UPDATE ... WHERE source_hash_hex != EXCLUDED.source_hash_hex` |
| `OUTPUT $action` | `RETURNING (xmax = 0)::int AS is_insert` |
| `GETUTCDATE()` | `NOW()` |
| `ISNULL(x, y)` | `COALESCE(x, y)` |
| `NpgsqlConnection` | `NpgsqlConnection` (ya era Npgsql) |

**Patrón para cada tabla (ejemplo OINV):**

```csharp
private const string UpsertOinvSql = """
    INSERT INTO "raw"."sap_oinv" (
        company_id, "DocEntry", "DocNum", "DocDate", "DocDueDate",
        "CardCode", "CardName", "DocTotal", "DocTotalFC", "DocTotalSy",
        "PaidToDate", "GrosProfit", "DiscPrcnt", "Cancelled", "DocStatus",
        "SlpCode", "Comments", "NumAtCard", "UpdateDate", "UpdateTS",
        "UpdateTSNorm", "DocCur", source_hash_hex, raw_created_at_utc
    )
    VALUES (
        @company_id, @DocEntry, @DocNum, @DocDate, @DocDueDate,
        @CardCode, @CardName, @DocTotal, @DocTotalFC, @DocTotalSy,
        @PaidToDate, @GrosProfit, @DiscPrcnt, @Cancelled, @DocStatus,
        @SlpCode, @Comments, @NumAtCard, @UpdateDate, @UpdateTS,
        @UpdateTSNorm, @DocCur, @source_hash_hex, NOW()
    )
    ON CONFLICT (company_id, "DocEntry") DO UPDATE SET
        "DocNum"         = EXCLUDED."DocNum",
        "DocDate"        = EXCLUDED."DocDate",
        "DocDueDate"     = EXCLUDED."DocDueDate",
        "CardCode"       = EXCLUDED."CardCode",
        "CardName"       = EXCLUDED."CardName",
        "DocTotal"       = EXCLUDED."DocTotal",
        "DocTotalFC"     = EXCLUDED."DocTotalFC",
        "DocTotalSy"     = EXCLUDED."DocTotalSy",
        "PaidToDate"     = EXCLUDED."PaidToDate",
        "GrosProfit"     = EXCLUDED."GrosProfit",
        "DiscPrcnt"      = EXCLUDED."DiscPrcnt",
        "Cancelled"      = EXCLUDED."Cancelled",
        "DocStatus"      = EXCLUDED."DocStatus",
        "SlpCode"        = EXCLUDED."SlpCode",
        "Comments"       = EXCLUDED."Comments",
        "NumAtCard"      = EXCLUDED."NumAtCard",
        "UpdateDate"     = EXCLUDED."UpdateDate",
        "UpdateTS"       = EXCLUDED."UpdateTS",
        "UpdateTSNorm"   = EXCLUDED."UpdateTSNorm",
        "DocCur"         = EXCLUDED."DocCur",
        source_hash_hex  = EXCLUDED.source_hash_hex,
        raw_updated_at_utc = NOW()
    WHERE
        "raw"."sap_oinv".source_hash_hex != EXCLUDED.source_hash_hex
        AND (
            EXCLUDED."UpdateDate" > "raw"."sap_oinv"."UpdateDate"
            OR (
                EXCLUDED."UpdateDate" = "raw"."sap_oinv"."UpdateDate"
                AND COALESCE(EXCLUDED."UpdateTSNorm", '000000')
                    >= COALESCE("raw"."sap_oinv"."UpdateTSNorm", '000000')
            )
        )
    RETURNING (xmax = 0)::int AS is_insert;
    """;
```

**Conteo de resultados:**
```csharp
// xmax = 0 → INSERT nuevo
// xmax != 0 → UPDATE ejecutado
// Sin RETURNING (fila sin cambio) → skipped por WHERE condition
var results = await conn.QueryAsync<int>(sql, parameters, cancellationToken: ct);
var inserted = results.Count(x => x == 1);
var updated = results.Count(x => x == 0);
// skipped = totalRowsInBatch - inserted - updated
```

- [ ] Reescribir el método para cada una de las 7 tablas con el patrón INSERT ON CONFLICT.
- [ ] Mantener el mismo interface público — los cambios son solo internos.
- [ ] Ejecutar tests existentes — deben seguir pasando (usan mocks, no BD real).
- [ ] Commit:
  ```
  git commit -m "feat: rewrite SapRawRepository with INSERT ON CONFLICT for postgresql"
  ```

---

### Tarea S1-5: Payload Sintético y Tests de Integración

**Archivos:**
- Crear: `tests/DataBision.Infrastructure.Tests/Ingest/SapRawRepositoryIntegrationTests.cs`
- Crear: `tests/DataBision.Infrastructure.Tests/Fixtures/TestSapPayloads.cs`

**Nota:** Tests de integración requieren conexión real a Supabase. Usar una instancia Supabase separada para tests, o la misma de dev con cleanup automático.

**Payload sintético OINV:**
```json
{
  "table": "OINV",
  "rows": [
    {
      "DocEntry": 1001,
      "DocNum": 5001,
      "DocDate": "2026-01-15",
      "DocDueDate": "2026-02-15",
      "CardCode": "C0001",
      "CardName": "Cliente Test SA",
      "DocTotal": 1190000.00,
      "DocTotalFC": 0,
      "DocTotalSy": 1190000.00,
      "PaidToDate": 0,
      "GrosProfit": 350000.00,
      "DiscPrcnt": 0,
      "Cancelled": "N",
      "DocStatus": "O",
      "SlpCode": 3,
      "Comments": "Factura test",
      "NumAtCard": "OC-9001",
      "UpdateDate": "2026-01-15",
      "UpdateTS": "143052",
      "DocCur": "CLP"
    }
  ]
}
```

**Tests obligatorios:**

```csharp
[Fact] // Primera inserción → inserted = 1
public async Task UpsertOinv_FirstInsert_ReturnsInserted()

[Fact] // Mismo payload → skipped = 1
public async Task UpsertOinv_SamePayload_ReturnsSkipped()

[Fact] // Payload modificado → updated = 1
public async Task UpsertOinv_ModifiedPayload_ReturnsUpdated()

[Fact] // Payload más antiguo → skipped (watermark guard)
public async Task UpsertOinv_OlderUpdateDate_ReturnsSkipped()

[Fact] // Batch de 100 filas → todas insertadas
public async Task UpsertOinv_BatchOf100_AllInserted()

[Fact] // Batch enviado 2 veces → segunda vez todas skipped
public async Task UpsertOinv_BatchSentTwice_SecondAllSkipped()
```

- [ ] Crear `TestSapPayloads.cs` con payloads sintéticos para las 7 tablas.
- [ ] Crear tests de integración para OINV, OCRD y OITM (mínimo).
- [ ] Tests de INV1, ORIN, RIN1 y OSLP: al menos un test de smoke por tabla.
- [ ] Configurar connection string de test desde variable de entorno `TEST_STAGING_CONNECTION`.
- [ ] Ejecutar y verificar que todos pasan.
- [ ] Commit:
  ```
  git commit -m "test: integration tests for SapRawRepository with synthetic SAP payloads"
  ```

---

### Tarea S1-6: Validar Ingest API End-to-End

**Archivos:**
- `src/DataBision.Api/Controllers/SapB1IngestController.cs`
- `src/DataBision.Api/appsettings.Development.json` (local, no trackeado)

- [ ] Levantar API: `dotnet run --project src/DataBision.Api`

- [ ] POST con curl o Postman al endpoint de ingest con header `X-DataBision-ApiKey: dev-key-001`:
  ```
  POST http://localhost:5000/api/ingest/sap-b1
  X-DataBision-ApiKey: dev-key-001
  Content-Type: application/json
  
  { "table": "OINV", "rows": [...payload sintético...] }
  ```

- [ ] Verificar respuesta: `{ "data": { "inserted": 1, "updated": 0, "skipped": 0 } }`

- [ ] Segunda ejecución con mismo payload → `{ "data": { "inserted": 0, "updated": 0, "skipped": 1 } }`

- [ ] Verificar en Supabase Dashboard que la fila existe en `raw.sap_oinv` con `source_hash_hex` poblado.

- [ ] Verificar que `ctl.ingest_checkpoint` tiene registro actualizado.

- [ ] Verificar que `audit.events` tiene registro del evento INGEST.

- [ ] Commit (solo si hubo cambios en controllers/services):
  ```
  git commit -m "feat: ingest api end-to-end validated with supabase postgresql"
  ```

**Duración estimada S1:** 10–12 días  
**Riesgo principal:** Diferencias sutiles entre SQL Server y PostgreSQL en manejo de NULL, tipos de datos, y encoding de strings SAP.

---

## 5. Sprint 2 — Dedicated Extractor MVP {#sprint-2}

**Duración:** 2 semanas  
**MVP:** Obligatorio  
**Dependencias:** Sprint 1 completo (Ingest API funcional y validado con Supabase)  
**Objetivo:** Tener datos reales de SAP B1 HANA fluyendo automáticamente hacia Supabase, con tolerancia a fallos de red básica.

### Criterios de Aceptación

- [ ] Extractor compila como `dotnet publish` para Windows x64
- [ ] Extractor conecta a HANA ODBC y ejecuta query de test en < 5 segundos
- [ ] Full load de OINV de los últimos 12 meses: completa sin error, con progreso en logs
- [ ] Segundo full load del mismo rango: `skipped` = N (idempotente)
- [ ] Incremental por `UpdateDate`: solo captura filas modificadas desde el watermark
- [ ] Cola offline SQLite local: si Ingest API no responde, encola localmente y reintenta
- [ ] Reintentos con backoff exponencial: 3 intentos, 10s / 30s / 90s
- [ ] Heartbeat enviado a `/api/ingest/heartbeat` cada 5 minutos
- [ ] Logs estructurados (Serilog) a archivo local y stdout

---

### Tarea S2-1: Crear Proyecto Dedicated Extractor

**Archivos:**
- Crear: `src/DataBision.Extractor/DataBision.Extractor.csproj`
- Crear: `src/DataBision.Extractor/Program.cs`
- Crear: `src/DataBision.Extractor/Worker.cs`
- Crear: `src/DataBision.Extractor/Configuration/ExtractorConfig.cs`

**Estructura del proyecto:**
```
src/DataBision.Extractor/
├── DataBision.Extractor.csproj
├── Program.cs
├── Worker.cs
├── Configuration/
│   ├── ExtractorConfig.cs        ← config de BD SAP + API + schedule
│   └── TableExtractionConfig.cs  ← watermark, lookback, batch size por tabla
├── Extraction/
│   ├── IHanaExtractor.cs
│   ├── HanaExtractor.cs          ← queries HANA ODBC
│   └── SqlServerExtractor.cs     ← alternativa ODBC SQL Server
├── Queue/
│   ├── LocalQueue.cs             ← SQLite cola offline
│   └── QueueEntry.cs
├── Ingest/
│   └── IngestApiClient.cs        ← HTTP POST al Ingest API
└── appsettings.json
```

**`.csproj` mínimo:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>false</SelfContained>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="8.0.0" />
    <PackageReference Include="Serilog.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="5.0.0" />
    <PackageReference Include="Dapper" Version="2.1.28" />
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.4" />
    <!-- ODBC para HANA — el driver debe estar instalado en la máquina del cliente -->
    <PackageReference Include="System.Data.Odbc" Version="8.0.0" />
    <PackageReference Include="Polly" Version="8.3.0" />
    <PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />
  </ItemGroup>
</Project>
```

- [ ] Crear proyecto con estructura de directorios.
- [ ] Agregar a `DataBision.sln`.
- [ ] Verificar que `dotnet build src/DataBision.Extractor` compila.
- [ ] Commit:
  ```
  git commit -m "feat: create dedicated extractor project structure"
  ```

---

### Tarea S2-2: Configuración Local Segura del Extractor

**Archivos:**
- `src/DataBision.Extractor/appsettings.json` (plantilla, sin secretos)
- Crear: `src/DataBision.Extractor/appsettings.Development.json` (NO trackeado)

**`appsettings.json` (plantilla — sí va a git):**
```json
{
  "Extractor": {
    "HanaConnectionString": "PLACEHOLDER",
    "IngestApiBaseUrl": "PLACEHOLDER",
    "IngestApiKey": "PLACEHOLDER",
    "ScheduleIntervalMinutes": 30,
    "FullLoadMonthsBack": 24,
    "BatchSize": 500,
    "HeartbeatIntervalMinutes": 5
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/extractor-.log", "rollingInterval": "Day" } }
    ]
  }
}
```

**`appsettings.Development.json` (no va a git — agregar al .gitignore del proyecto):**
```json
{
  "Extractor": {
    "HanaConnectionString": "Driver={HANA Native};ServerNode=192.168.1.10:30015;UID=SYSTEM;PWD=REAL_PASSWORD;",
    "IngestApiBaseUrl": "http://localhost:5000",
    "IngestApiKey": "dev-key-001",
    "ScheduleIntervalMinutes": 5,
    "FullLoadMonthsBack": 12,
    "BatchSize": 100,
    "HeartbeatIntervalMinutes": 1
  }
}
```

- [ ] Agregar `appsettings.Development.json` al `.gitignore` del extractor.
- [ ] Crear `ExtractorConfig.cs` como clase tipada de configuración.
- [ ] Registrar en `Program.cs` con `services.Configure<ExtractorConfig>(...)`.

---

### Tarea S2-3: Conexión HANA ODBC y Queries de Extracción

**Archivo:** `src/DataBision.Extractor/Extraction/HanaExtractor.cs`

**Queries de extracción full load y delta:**

```csharp
// Full load — carga inicial con filtro de fecha
private const string FullLoadOinvSql = """
    SELECT 
        T."DocEntry", T."DocNum", T."DocDate", T."DocDueDate",
        T."CardCode", T."CardName", T."DocTotal", T."DocTotalFC", T."DocTotalSy",
        T."PaidToDate", T."GrosProfit", T."DiscPrcnt", T."Cancelled", T."DocStatus",
        T."SlpCode", T."Comments", T."NumAtCard", T."UpdateDate", T."UpdateTS", T."DocCur"
    FROM OINV T
    WHERE T."DocDate" >= ADDDAYS(NOW(), -:days_back)
    ORDER BY T."UpdateDate" ASC, T."DocEntry" ASC
    LIMIT :batch_size OFFSET :offset
    """;

// Delta incremental
private const string DeltaOinvSql = """
    SELECT 
        T."DocEntry", T."DocNum", T."DocDate", T."DocDueDate",
        T."CardCode", T."CardName", T."DocTotal", T."DocTotalFC", T."DocTotalSy",
        T."PaidToDate", T."GrosProfit", T."DiscPrcnt", T."Cancelled", T."DocStatus",
        T."SlpCode", T."Comments", T."NumAtCard", T."UpdateDate", T."UpdateTS", T."DocCur"
    FROM OINV T
    WHERE T."UpdateDate" >= :watermark_date
       OR (T."UpdateDate" = :watermark_date AND T."UpdateTS" >= :watermark_ts)
    ORDER BY T."UpdateDate" ASC, T."DocEntry" ASC
    """;
```

**Nota:** Para SQL Server ODBC, las queries son similares pero con sintaxis SQL Server. `HanaExtractor` y `SqlServerExtractor` implementan `IHanaExtractor`.

- [ ] Implementar `HanaExtractor` con conexión ODBC y queries para las 7 tablas.
- [ ] Implementar normalización de `UpdateTS` (CHAR(6) HHMMSS) localmente antes de enviar.
- [ ] Test manual: conectar a HANA dev y ejecutar `SELECT TOP 10 "DocEntry" FROM OINV` → debe retornar filas.
- [ ] Commit:
  ```
  git commit -m "feat: hana odbc extractor with full load and delta queries"
  ```

---

### Tarea S2-4: Cola Offline SQLite

**Archivo:** `src/DataBision.Extractor/Queue/LocalQueue.cs`

Cuando el Ingest API no responde (red caída, API en mantenimiento), el extractor debe encolar localmente y reintentar.

```csharp
// Schema de la cola SQLite local
private const string CreateQueueTableSql = """
    CREATE TABLE IF NOT EXISTS outbox (
        id          INTEGER PRIMARY KEY AUTOINCREMENT,
        table_name  TEXT NOT NULL,
        payload_json TEXT NOT NULL,
        created_at  TEXT NOT NULL,
        attempts    INTEGER NOT NULL DEFAULT 0,
        last_error  TEXT
    );
    """;

// Operaciones:
// Enqueue(tableName, payloadJson) → INSERT INTO outbox
// Dequeue(batchSize) → SELECT ... WHERE attempts < 3 ORDER BY id LIMIT n
// MarkSent(id) → DELETE FROM outbox WHERE id = @id
// MarkFailed(id, error) → UPDATE outbox SET attempts = attempts + 1, last_error = @error
```

**Política de reintentos:**
- Máximo 3 intentos por batch
- Backoff: 10s / 30s / 90s entre intentos
- Después de 3 fallos: log de error + mantenimiento en cola hasta revisión manual
- Heartbeat incluye `{ pending_in_queue: N }` para que el SuperAdmin vea si hay acumulación

- [ ] Implementar `LocalQueue.cs` con SQLite (path configurable).
- [ ] Implementar reintento en `Worker.cs` usando Polly.
- [ ] Commit:
  ```
  git commit -m "feat: offline sqlite queue with retry policy for extractor"
  ```

---

### Tarea S2-5: IngestApiClient con Polly

**Archivo:** `src/DataBision.Extractor/Ingest/IngestApiClient.cs`

```csharp
public class IngestApiClient
{
    // POST /api/ingest/sap-b1 con retry automático
    public async Task<IngestResult> PostBatchAsync(string tableName, IEnumerable<object> rows, CancellationToken ct);

    // POST /api/ingest/heartbeat cada HeartbeatIntervalMinutes
    public async Task SendHeartbeatAsync(string extractorVersion, int pendingInQueue, CancellationToken ct);
}
```

**Política Polly:**
```csharp
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(3, attempt)),
        onRetry: (ex, delay, attempt, ctx) => logger.LogWarning(...)
    );

var circuitBreaker = Policy
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromMinutes(2),
        onBreak: (ex, duration) => logger.LogError("Circuit open — switching to local queue"),
        onReset: () => logger.LogInformation("Circuit closed — resuming normal operation")
    );
```

Cuando el circuit breaker está abierto → encolar en `LocalQueue`.

- [ ] Implementar `IngestApiClient` con HttpClient + Polly.
- [ ] Implementar fallback a `LocalQueue` cuando el circuito está abierto.
- [ ] Commit:
  ```
  git commit -m "feat: ingest api client with polly retry and circuit breaker"
  ```

---

### Tarea S2-6: Worker Principal y Loop de Extracción

**Archivo:** `src/DataBision.Extractor/Worker.cs`

```csharp
public class ExtractorWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1. Full load inicial si no hay watermark almacenado
        // 2. Loop periódico: delta extraction → POST → update watermark
        // 3. Cola local: si hay pending items, enviarlos antes del próximo delta
        // 4. Heartbeat: enviar cada HeartbeatIntervalMinutes
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_config.ScheduleIntervalMinutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunExtractionCycleAsync(stoppingToken);
        }
    }
}
```

**Orden del ciclo:**
1. Enviar items pendientes en `LocalQueue`
2. Para cada tabla configurada: leer watermark → query delta → POST → actualizar watermark
3. Si es primera ejecución: full load paginado
4. Cada N ciclos: enviar heartbeat

- [ ] Implementar `Worker.cs` con el loop completo.
- [ ] Watermark almacenado en SQLite local (no depende de red).
- [ ] Commit:
  ```
  git commit -m "feat: extraction worker loop with delta, full load and heartbeat"
  ```

---

### Tarea S2-7: Test End-to-End con SAP Dev

- [ ] Apuntar extractor a SAP dev (o dataset sintético si no hay SAP disponible).
- [ ] Ejecutar full load inicial de OINV 24 meses → verificar en Supabase.
- [ ] Esperar 30 minutos → verificar que incremental captura solo las filas nuevas/modificadas.
- [ ] Desconectar red → verificar que datos van a la cola SQLite.
- [ ] Reconectar → verificar que la cola se vacía y los datos llegan a Supabase.
- [ ] Revisar logs → deben ser limpios (no stack traces, no secretos en logs).

**Duración estimada S2:** 10–12 días  
**Riesgo principal:** Driver HANA ODBC en la máquina de dev. Si no hay HANA disponible, usar dataset sintético de 1.000 filas para validar el flujo end-to-end.

---

## 6. Sprint 3 — Native BI Mínimo {#sprint-3}

**Duración:** 3 semanas  
**MVP:** Obligatorio  
**Dependencias:** Sprint 1 completo (datos en Supabase), Sprint 2 al menos en validación  
**Objetivo:** Dashboard básico funcional con datos reales de SAP, autenticación, branding por tenant, y al menos Ventas y Clientes.

### Criterios de Aceptación

- [ ] Frontend buildea con `npm run build` sin errores TypeScript
- [ ] Login funciona con JWT + refresh token
- [ ] `{slug}.databision.com` resuelve al portal del tenant correcto
- [ ] Dashboard Home muestra: ventas del mes, top 5 clientes, vendedor del mes (datos de Supabase)
- [ ] Módulo Ventas: gráfico de barras mensual + tabla de facturas paginada
- [ ] Módulo Clientes: tabla de clientes con últimas facturas
- [ ] Sync Center básico: estado de última sincronización por tabla
- [ ] Branding por tenant: logo + colores desde `/api/tenant/config`
- [ ] Permisos por módulo: `module_ids[]` en JWT controla acceso
- [ ] Todos los gráficos con ECharts (sin Power BI, sin Recharts)

---

### Tarea S3-1: Analytics API Endpoints

**Archivos:**
- Crear: `src/DataBision.Api/Controllers/AnalyticsController.cs`
- Crear: `src/DataBision.Application/Services/AnalyticsService.cs`
- Crear: `src/DataBision.Application/Interfaces/IAnalyticsService.cs`
- Crear: `src/DataBision.Application/DTOs/Analytics/`

**Endpoints mínimos:**

```
GET /api/analytics/summary?period=YYYY-MM
  → { totalSales, totalInvoices, totalClients, grossProfit, delta vs prev month }

GET /api/analytics/sales/monthly?months=12
  → [{ month: "2026-01", total: 1500000, invoiceCount: 45 }]

GET /api/analytics/sales/top-clients?period=YYYY-MM&limit=10
  → [{ cardCode, cardName, total, invoiceCount, delta }]

GET /api/analytics/sales/top-sellers?period=YYYY-MM&limit=10
  → [{ slpCode, slpName, total, clientCount }]

GET /api/analytics/clients/list?page=1&pageSize=50&search=
  → { items: [...], total, page }

GET /api/analytics/clients/{cardCode}/invoices?limit=10
  → [{ docEntry, docNum, docDate, docTotal, docStatus }]

GET /api/operational/sync-status
  → [{ tableName, lastRunUtc, rowsLastRun, status }]
```

**Queries contra `raw.*`:**

Fase 1 consulta directamente `raw.sap_oinv` join `raw.sap_inv1` join `raw.sap_oslp`. Las capas `stg.*` y `fact.*` se construyen en Fase 4 cuando el `StagingTransformWorker` esté operativo.

```sql
-- Ventas del mes (ejemplo)
SELECT 
    SUM(i."DocTotal") AS total_sales,
    COUNT(*) AS invoice_count,
    COUNT(DISTINCT i."CardCode") AS client_count,
    SUM(i."GrosProfit") AS gross_profit
FROM raw.sap_oinv i
WHERE i.company_id = @company_id
  AND i."Cancelled" = 'N'       -- excluir anuladas (Cancelled='Y', DocStatus='Y' en SAP B1)
  AND i."DocDate" >= @period_start
  AND i."DocDate" < @period_end
  -- NOTA SAP B1: NO usar DocStatus != 'C' para excluir "canceladas".
  -- En SAP B1, DocStatus='C' significa CERRADO (factura totalmente pagada/entregada), NO anulado.
  -- Las facturas cerradas SON ventas válidas y deben incluirse en los totales.
  -- Para identificar anuladas: usar Cancelled='Y' (anulado) o DocStatus='Y' (cancelado).
  -- DocStatus='O' = abierto, DocStatus='C' = cerrado, DocStatus='L' = empaquetado, DocStatus='Y' = cancelado (SAP 9.3+).
```

- [ ] Implementar `AnalyticsService` con queries parametrizadas (Dapper sobre NpgsqlConnection).
- [ ] Todos los endpoints validan `company_id` del JWT. Sin excepciones.
- [ ] Respuesta siempre `{ "data": T }`.
- [ ] Tests unitarios con mock de `IAnalyticsService` para los controladores.
- [ ] Commit:
  ```
  git commit -m "feat: analytics api endpoints — summary, monthly, top clients, sync status"
  ```

---

### Tarea S3-2: Frontend — Setup y Estructura

**Directorio:** `databision-frontend/`

Si el frontend no existe: crear con Vite + React + TypeScript.

```bash
npm create vite@latest databision-frontend -- --template react-ts
cd databision-frontend
npm install
npm install echarts echarts-for-react
npm install @tanstack/react-query axios zustand
npm install react-router-dom
npm install -D tailwindcss postcss autoprefixer @types/node
npx tailwindcss init -p
```

**Estructura mínima:**
```
databision-frontend/
├── src/
│   ├── apps/
│   │   ├── admin/        ← SuperAdmin (Sprint 5)
│   │   └── portal/       ← Portal del tenant (Sprint 3)
│   │       ├── pages/
│   │       │   ├── Dashboard.tsx
│   │       │   ├── SalesModule.tsx
│   │       │   ├── ClientsModule.tsx
│   │       │   └── SyncCenter.tsx
│   │       ├── components/
│   │       │   ├── KpiCard.tsx
│   │       │   ├── SalesChart.tsx      ← ECharts
│   │       │   ├── TopClientsChart.tsx ← ECharts
│   │       │   └── DataTable.tsx
│   │       └── PortalApp.tsx
│   ├── shared/
│   │   ├── api/
│   │   │   ├── axios.ts          ← interceptor refresh
│   │   │   └── analytics.ts      ← endpoints
│   │   ├── auth/
│   │   │   └── useAuth.ts        ← Zustand store
│   │   └── theme/
│   │       └── ThemeProvider.tsx
│   └── App.tsx               ← detecta subdomain, renderiza portal o admin
```

- [ ] Crear estructura de directorios.
- [ ] Configurar Tailwind con tokens `brand-*`.
- [ ] Configurar TanStack Query con `QueryClient`.
- [ ] Configurar Axios con interceptor de refresh automático en 401.
- [ ] `npm run build` debe pasar sin errores.
- [ ] Commit:
  ```
  git commit -m "feat: frontend scaffold — vite react typescript echarts tanstack"
  ```

---

### Tarea S3-3: Auth Flow Frontend

**Archivos:**
- `src/shared/auth/useAuth.ts` → Zustand store
- `src/shared/api/axios.ts` → interceptor
- `src/apps/portal/pages/LoginPage.tsx`

```typescript
// useAuth.ts — Zustand store
interface AuthState {
  user: { email: string; role: string; companyId: string; moduleIds: string[] } | null;
  accessToken: string | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refresh: () => Promise<string>;  // retorna nuevo token
}
```

```typescript
// axios.ts — interceptor de refresh
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config;
    if (error.response?.status === 401 && !original._retry) {
      original._retry = true;
      const newToken = await useAuthStore.getState().refresh();
      original.headers['Authorization'] = `Bearer ${newToken}`;
      return api(original);
    }
    return Promise.reject(error);
  }
);
```

- [ ] Login page con branding del tenant (logo + colores desde localStorage cache).
- [ ] Al login exitoso: guardar accessToken en memory (no localStorage), cookie httpOnly manejada por el browser.
- [ ] Ruta protegida: si no hay token → redirect a `/login`.
- [ ] Tests: `npm test -- --testNamePattern="Auth"`.
- [ ] Commit:
  ```
  git commit -m "feat: frontend auth flow with jwt and refresh token interceptor"
  ```

---

### Tarea S3-4: Dashboard Home con ECharts

**Archivos:**
- `src/apps/portal/pages/Dashboard.tsx`
- `src/apps/portal/components/KpiCard.tsx`
- `src/apps/portal/components/SalesChart.tsx`
- `src/apps/portal/components/TopClientsChart.tsx`

**Dashboard Home — componentes:**

```typescript
// KpiCard.tsx
interface KpiCardProps {
  title: string;
  value: string;      // formateado: "$1.190.000" o "45 facturas"
  delta?: string;     // "+12% vs mes anterior" en verde/rojo
  loading?: boolean;
}

// SalesChart.tsx — ECharts bar chart
// Usa echarts-for-react, datos de /api/analytics/sales/monthly?months=12

// TopClientsChart.tsx — ECharts bar horizontal
// Datos de /api/analytics/sales/top-clients?period=YYYY-MM&limit=5
```

**TanStack Query para data fetching:**
```typescript
const { data: summary } = useQuery({
  queryKey: ['analytics', 'summary', period],
  queryFn: () => api.get(`/api/analytics/summary?period=${period}`).then(r => r.data.data),
  staleTime: 5 * 60 * 1000,  // 5 minutos
});
```

**Reglas de diseño (del CLAUDE.md):**
- Sidebar: `#0F172A`
- Background: `#F8FAFC`
- Filas de tabla: 44px
- Tipografía: Inter 14px body, tabular-nums para números
- Sin hardcoding de brand colors — solo `var(--brand-primary)`

- [ ] Implementar Dashboard con 4 KPI cards + gráfico mensual + top 5 clientes.
- [ ] Loading state: skeleton loaders (no spinner genérico).
- [ ] Error state: mensaje con botón retry.
- [ ] Commit:
  ```
  git commit -m "feat: dashboard home with kpi cards and echarts sales chart"
  ```

---

### Tarea S3-5: Módulos Ventas y Clientes

**Archivos:**
- `src/apps/portal/pages/SalesModule.tsx`
- `src/apps/portal/pages/ClientsModule.tsx`
- `src/apps/portal/components/DataTable.tsx`

**Módulo Ventas:**
- Gráfico de barras mensual (últimos 12 meses)
- Tabla de facturas: DocNum, Fecha, Cliente, Total, Estado, Vendedor — paginada
- Filtros: rango de fechas, vendedor, estado (abierta/cerrada/cancelada)

**Módulo Clientes:**
- Tabla de clientes: CardCode, CardName, Vendedor, Saldo, Última factura, Días sin compra
- Click en cliente → panel lateral con últimas 5 facturas

**DataTable — comportamiento:**
- Paginación server-side (no cargar todo en memoria)
- Sort por columna clickeable en headers
- 44px altura de fila (CLAUDE.md)
- Export a CSV: solo datos de la página actual en MVP

- [ ] Implementar `DataTable` reutilizable (lo usará también Sync Center y módulos futuros).
- [ ] Implementar SalesModule y ClientsModule usando DataTable y ECharts.
- [ ] Permisos: si `module_ids` del JWT no incluye el módulo, redirigir a `/unauthorized`.
- [ ] Commit:
  ```
  git commit -m "feat: sales and clients modules with data table and charts"
  ```

---

### Tarea S3-6: Sync Center Básico

**Archivo:** `src/apps/portal/pages/SyncCenter.tsx`

```typescript
// Muestra estado de la última sincronización por tabla
// Datos de GET /api/operational/sync-status

interface SyncTableStatus {
  tableName: string;        // "OINV", "OCRD", etc.
  lastRunUtc: string;
  rowsLastRun: number;
  status: 'ok' | 'warning' | 'error';
  lagMinutes: number;       // minutos desde lastRunUtc
}
```

**Visual:**
- Badge de estado: Verde (lag < 60 min), Amarillo (60–240 min), Rojo (> 240 min)
- Timestamp legible: "Hace 23 minutos" / "Hace 2 horas"
- Sin funcionalidad de configuración en Sprint 3 — solo lectura

- [ ] Implementar SyncCenter con polling cada 60 segundos (`refetchInterval: 60000`).
- [ ] Commit:
  ```
  git commit -m "feat: sync center basic — table status with freshness indicators"
  ```

---

### Tarea S3-7: Branding por Tenant y ThemeProvider

**Archivos:**
- `src/shared/theme/ThemeProvider.tsx`
- Endpoint backend: `GET /api/tenant/config` (ya debe existir o crear)

```typescript
// ThemeProvider.tsx
const ThemeProvider = ({ children }: { children: ReactNode }) => {
  useEffect(() => {
    // 1. Intentar desde localStorage (evitar color flash)
    const cached = localStorage.getItem('tenant_config');
    if (cached) applyTheme(JSON.parse(cached));

    // 2. Fetch y actualizar
    fetch('/api/tenant/config')
      .then(r => r.json())
      .then(config => {
        localStorage.setItem('tenant_config', JSON.stringify(config.data));
        applyTheme(config.data);
      });
  }, []);

  const applyTheme = (config: TenantConfig) => {
    document.documentElement.style.setProperty('--brand-primary', config.brandPrimary);
    document.documentElement.style.setProperty('--brand-secondary', config.brandSecondary);
    document.documentElement.style.setProperty('--brand-sidebar', config.brandSidebar ?? '#0F172A');
  };

  return <>{children}</>;
};
```

**Tailwind config para brand tokens:**
```javascript
// tailwind.config.js
theme: {
  extend: {
    colors: {
      'brand-primary': 'var(--brand-primary)',
      'brand-secondary': 'var(--brand-secondary)',
      'brand-sidebar': 'var(--brand-sidebar)',
    }
  }
}
```

- [ ] Implementar ThemeProvider y aplicar en `PortalApp.tsx`.
- [ ] Verificar que con `?tenant=slug` el branding cambia sin recarga.
- [ ] Commit:
  ```
  git commit -m "feat: dynamic branding with css vars and tenant config cache"
  ```

**Duración estimada S3:** 15 días  
**Riesgo principal:** Complejidad subestimada del diseño de la DataTable reutilizable. Si se complica, simplificar — la tabla puede ser dumb en Sprint 3 y hacerse más sofisticada en Sprint 5.

---

## 7. Fases Post-Sprint 3 {#post-sprint-3}

### Fase 4 — Sync Center + Operational Intelligence (2 semanas)

**Prioridad:** Alta — Fase 1 del roadmap de producto  
**Objetivo:** Dashboards ejecutivos más ricos, workers de transformación, estado de datos consolidado.

**Tareas principales:**
- `StagingTransformWorker`: `raw.*` → `stg.*` → `dim.*` → `fact.*`
- Queries analíticas migradas desde `raw.*` a `fact.*` (mejor performance)
- `DataFreshnessWorker`: calcula `oper.freshness_scores` por tabla
- `HeartbeatMonitorWorker`: detecta extractores sin heartbeat
- `/data-status` página con detalle por tabla (lag, calidad, última sincronización)
- Reconciliación nocturna: compara conteos `raw.*` vs `fact.*`

**Dependencias:** Sprint 1 (datos en raw.*), Sprint 3 (portal funcionando)

---

### Fase 5 — Portal Comercial MVP (2 semanas)

**Prioridad:** Alta — requerido para primer cliente real  
**Objetivo:** Portal listo para demo y primer contrato.

**Tareas principales:**
- SuperAdmin Panel (`admin.databision.com`): crear/gestionar tenants, ver estado global
- Gestión de usuarios: invitación por email, roles (CompanyAdmin, Analyst, Viewer)
- Módulo de configuración de tenant: logo, colores, dominio
- Exportación: CSV desde tablas, PDF del dashboard actual
- Business Action: "Solicitar actualización manual" (throttled 30 min)
- Onboarding flow: wizard para configurar extractor + validar primera extracción

**Dependencias:** Sprint 3 (portal base), Sprint 4 (operational intelligence)

---

### Fase 6 — Mode C: Service Layer Polling (1.5 semanas)

**Prioridad:** Media-Alta — desbloquea clientes cloud que no permiten instalar agente  
**MVP post-Sprint 3:** Sí, para segundo cliente

**Tareas principales:**
- `SLPollingWorker`: BackgroundService que hace GET OData por tabla con watermark
- Configuración por tenant: SL URL, credenciales (cifradas), tablas, intervalo
- Staggered scheduling: no ejecutar todos los tenants en el mismo tick
- Circuit breaker por tenant: si SL no responde, aislar sin afectar a otros tenants

**Restricción:** Solo para clientes con < 50k filas/año y como puente hacia Mode B. Documentar en contrato.

---

### Fase 7 — Mode B: Service Layer Delta (2.5 semanas)

**Prioridad:** Media — modo de producción para clientes cloud  
**Dependencias:** Fase 6 (infraestructura SL básica), cliente con partner SAP que pueda crear UDTs

**Tareas principales:**
- `SLDeltaWorker`: pull desde `@DBS_QUEUE` UDT en SAP (nombre canónico — ADR-010)
- PATCH al UDT para marcar registros procesados
- Guía técnica para partner SAP: cómo crear UDT + FMS triggers
- Carga inicial: script de importación CSV para datos históricos
- Tests con SAP B1 sandbox de desarrollo

---

### Fase 8 — Operational Live Layer (2 semanas)

**Prioridad:** Media — diferenciador del Plan Business  
**Dependencias:** Fase 7 o cliente con Service Layer activo

**Tareas principales:**
- `OperationalLiveController`: endpoints `/api/live/*` con HttpClient → SL OData
- Rate limiting por tenant (máximo N req/min a SL del cliente)
- Páginas `/live/*` en portal: dispatch, blocked documents, picking, stock alerts
- TanStack Query con `refetchInterval` por caso de uso (30s / 60s / 120s)
- Sin persistencia en PostgreSQL — datos efímeros

---

### Fase 9 — Alerting + Recommendations (3 semanas)

**Prioridad:** Alta para retención — diferenciador de producto  
**Dependencias:** Fase 4 (fact.* disponibles), Fase 5 (usuarios con email)

**Tareas principales:**
- `AlertingWorker`: evalúa `ctl.alert_rules`, escribe `oper.alerts`
- `RecommendationWorker`: evalúa 5 reglas iniciales, escribe `oper.recommendations`
- Centro de notificaciones en portal (badge + lista)
- Email de alerta via Resend.com
- Cooldown configurable por regla
- Sección "Insights" en portal (Fase 1.5)
- Métricas de dismissal rate para afinar reglas

---

## 8. Qué NO Construir Todavía {#no-construir}

Esta lista es tan importante como el roadmap. Construir estas cosas antes de tiempo garantiza reescrituras.

| Qué | Por qué no ahora | Cuándo |
|---|---|---|
| Power BI Embedded | No es el núcleo. ADR-002. Es un add-on para clientes que ya lo usen. | Fase 4+ solo si un cliente lo exige contractualmente |
| Apache Superset | Overhead operacional innecesario cuando estamos construyendo BI propio | Nunca en el MVP. Decisión de producto post-Fase 3 |
| IA avanzada / LLM / RAG | Sin datos históricos consolidados, la IA no tiene base para razonar | Fase 4 año 2 con datos de mínimo 6 meses |
| Editor de reportes drag-and-drop | Complejidad de producto muy alta. El cliente no lo pide en el MVP | Fase 4+ si hay demanda documentada |
| Escritura a SAP | Requiere modelo de permisos maduro, testing exhaustivo con SAP. Un bug puede corromper datos del cliente | Fase 3 Business Actions avanzadas |
| Multiempresa / Holdings | Modelo de datos significativamente más complejo. Un holding puede representar 5-10 tenants | Fase 3 si hay cliente específico |
| Operational Live avanzado | Solo 3 vistas predefinidas en Plan Business. Vistas custom en Plan Advanced | Fase 8 |
| Service Layer Delta completo | Solo si hay cliente cloud con partner SAP que pueda crear UDTs en el timeline | Fase 7 — solo cuando el primer cliente lo requiera |
| Azure Service Bus / Redis Streams | Innecesario con ≤ 10 clientes. Channel<T> es suficiente | Fase 3+ cuando el volumen lo justifique |
| Multi-región | Un solo App Service y Supabase en US-East es suficiente para América Latina | Año 2+ |
| WebSockets para alertas | Polling activo cada 30s es suficiente y mucho más simple | Nunca, a menos que escale a 100+ usuarios simultáneos |
| Row Level Security en Supabase | La API ya filtra por company_id. RLS añade complejidad sin beneficio en MVP | Fase 2 si hay acceso directo a BD para clientes |

---

## 9. Definition of Done General {#dod}

Aplica a **toda** tarea técnica, en todos los sprints.

### Build y Tests

- [ ] `dotnet build DataBision.sln` pasa sin errores ni warnings nuevos
- [ ] `dotnet test` pasa al 100% — ningún test roto o ignorado sin justificación documentada
- [ ] `npm run build` pasa sin errores TypeScript (modo strict)
- [ ] `npm run lint` sin errores (warnings aceptables si son pre-existentes)
- [ ] No hay `any` nuevo en código TypeScript

### Seguridad

- [ ] Sin secretos en código (conexion strings, API keys, passwords, tokens)
- [ ] Sin credenciales en archivos trackeados por git
- [ ] `git ls-files | xargs grep -l "password\|Password\|secret\|Secret\|apikey\|ApiKey"` retorna solo archivos de plantilla o configuración sin valores reales
- [ ] Toda query a tablas de datos incluye `WHERE company_id = @company_id` explícito
- [ ] Sin concatenación de strings en queries SQL — solo parámetros (`@param` en Dapper)
- [ ] Sin `any` de TypeScript en código de frontend

### Calidad de Código

- [ ] Sin código comentado en el diff — el código muerto se elimina, no se comenta
- [ ] Sin `TODO URGENT` ni `FIXME` introducidos en el diff (los pre-existentes están permitidos si están en el backlog)
- [ ] DTOs en el boundary de todos los controllers — sin exponer entidades de dominio
- [ ] Respuesta API: `{ "data": T }` en éxito, `{ "error": "code", "message": "..." }` en error
- [ ] Business Actions registradas en `audit.events`

### Git y Entorno

- [ ] Git status limpio — sin archivos sin commit que no sean locales intencionales
- [ ] Rama `feat/*` mergeada a `main` via PR (nunca push directo a main salvo Sprint 0)
- [ ] Mensaje de commit en forma imperativa: `feat:`, `fix:`, `test:`, `chore:`, `docs:`
- [ ] El cambio ha sido probado en entorno DEV (no solo en tests locales)

### Documentación

- [ ] Si el cambio modifica el contrato del Ingest API → actualizar `master-architecture.md` sección 3.2
- [ ] Si el cambio introduce una nueva decisión arquitectónica → crear ADR en `docs/adr/`
- [ ] Si el cambio modifica variables de entorno → actualizar tabla en `CLAUDE.md`

### Evidencias de Prueba

- [ ] Capturas de Supabase Dashboard mostrando datos reales (para Sprint 1 y 2)
- [ ] Log de extractor mostrando ciclo completo sin errores (para Sprint 2)
- [ ] Screenshot del dashboard con datos (para Sprint 3)
- [ ] Resultado de `dotnet test --logger trx` adjunto al PR

---

## 10. Mapa de Riesgos de Implementación {#riesgos}

### Riesgos Técnicos de Alta Probabilidad

| ID | Riesgo | Impacto | Probabilidad | Mitigación |
|---|---|---|---|---|
| R-T01 | Driver HANA ODBC no disponible en la máquina de dev del equipo | Alto | Alta | Usar dataset sintético para validar el flujo; pedir acceso a ambiente HANA del cliente piloto en Sprint 2 |
| R-T02 | INSERT ON CONFLICT tiene edge cases distintos al T-SQL MERGE original | Medio | Media | Tests de integración exhaustivos (Tarea S1-5) antes de declarar Sprint 1 completo |
| R-T03 | Supabase Free pausa tras 7 días de inactividad | Alto | Alta (si se usa Free) | Usar Supabase Pro desde el inicio (USD 25/mes). No negociable. |
| R-T04 | Conexiones Supabase Pro saturadas (máximo 15 via PgBouncer) | Alto | Media (con 10+ tenants) | Monitorear conexiones activas desde el primer cliente. PgBouncer + pool size en Npgsql |
| R-T05 | UpdateDate no actualiza en cancelaciones de SAP B1 | Medio | Media | Lookback de 24h en el watermark: `UpdateDate >= watermark - 1 day`. Reconciliación nocturna en Fase 4 |
| R-T06 | Diferencias de encoding en strings SAP (Latin1 vs UTF-8) | Medio | Alta | Normalizar encoding en HanaExtractor antes de serializar a JSON |
| R-T07 | HANA ODBC vs SQL Server ODBC tienen APIs ODBC distintas | Alto | Alta | `IHanaExtractor` abstracta con dos implementaciones — no mezclar |
| R-T08 | Migraciones EF con `MigrationsHistoryTable` en schema `ctl` — comportamiento distinto en Npgsql | Medio | Baja | Probar migración apply contra Supabase fresh antes de Sprint 1 en marcha |

### Riesgos de Producto

| ID | Riesgo | Impacto | Probabilidad | Mitigación |
|---|---|---|---|---|
| R-P01 | Primer cliente exige Power BI en negociación | Medio | Media | Preparar respuesta: "Power BI disponible como add-on en Q3/Q4. El portal nativo tiene más contexto operacional." |
| R-P02 | ECharts no cubre algún gráfico específico que el cliente pide | Bajo | Media | ECharts tiene 30+ tipos. Documentar upfront en el onboarding los tipos disponibles. |
| R-P03 | Partner SAP del cliente no puede instalar el extractor (IT policy) | Alto | Alta | Mode B o C como plan B documentado. Nunca bloquear un onboarding por no poder usar Mode A. |
| R-P04 | Cliente cloud-only sin UDT → Mode C como única opción → luego no escala | Medio | Media | Aceptar Mode C con compromiso documentado de migrar a Mode B en 90 días. Medir lag. |
| R-P05 | Recomendaciones de Fase 1.5 generan ruido → cliente percibe DataBision como spam | Medio | Alta | Comenzar con SOLO 5 reglas de alta confianza. Medir dismissal rate. No agregar más reglas hasta que el primero esté estable. |

### Riesgos de Proyecto

| ID | Riesgo | Impacto | Probabilidad | Mitigación |
|---|---|---|---|---|
| R-J01 | Sprint 0 revela más deuda técnica de lo esperado (e.g., migraciones SQL Server complejas) | Alto | Media | Presupuestar 2 días de buffer en Sprint 0. Si supera 7 días, escalar. |
| R-J02 | Sprint 3 (Native BI) subestimado — los gráficos ECharts toman más de lo estimado | Alto | Alta | Limitar Sprint 3 a 4 tipos de gráficos: barra, barra horizontal, línea temporal, tabla. Sin customización avanzada. |
| R-J03 | Dependencias paralelas — Sprint 2 (extractor) y Sprint 3 (frontend) pueden avanzar en paralelo si hay dos personas, o se convierten en cuello de botella con una | Alto | Alta | Con una persona: Sprint 2 → Sprint 3. Con dos personas: paralelizar desde la semana 4 si Sprint 1 está completo. |
| R-J04 | Claude Code genera código que compila pero no es idiomático del proyecto | Medio | Media | Aplicar `mvp-hardening-check` skill al final de cada sprint antes del merge. |

---

## 11. Recomendación Final {#recomendacion}

### Primer Sprint a Ejecutar

**Sprint 0 — Limpieza y Alineación.** No saltarlo.

El build actual no está alineado con la arquitectura aprobada (Npgsql vs SqlServer, Power BI como núcleo). Comenzar Sprint 1 sin completar Sprint 0 garantiza que trabajarás sobre un foundation roto. El Sprint 0 tarda 4–5 días y elimina toda ambigüedad.

### Sobre el Commit de Arquitectura

**Hacer commit de arquitectura YA, antes de iniciar Sprint 0.** Los documentos `docs/master-architecture.md`, `docs/adr/`, y este roadmap son decisiones tomadas. Deben estar en git ahora para que sean la referencia oficial durante la implementación.

```bash
git add docs/master-architecture.md docs/adr/ docs/implementation-roadmap-v1.md
git commit -m "docs: lock architecture v4.0 and implementation roadmap v1"
```

### Manejo de Ramas

```
main
├── feat/sprint-0-cleanup         (Sprint 0)
├── feat/sprint-1-supabase        (Sprint 1)
├── feat/sprint-2-extractor       (Sprint 2)
└── feat/sprint-3-native-bi       (Sprint 3)
```

- Cada sprint trabaja en su propia rama.
- PR a `main` al completar cada sprint, con code review.
- No hacer merge hasta que todos los criterios de aceptación del sprint estén verificados.
- Sprint 2 y Sprint 3 pueden ser ramas paralelas desde `main` si hay dos desarrolladores disponibles desde la semana 4.

### Cómo Manejar Prompts con Claude Code

1. **Un sprint a la vez.** No pasar el roadmap completo como contexto en cada sesión. Cada sesión de Claude Code debe tener solo el sprint activo como contexto.

2. **Usar skills al inicio de cada sesión:**
   - `mvp-hardening-check` antes del merge de cada sprint
   - `backend-clean-architecture` cuando se trabaje en servicios y repositorios
   - `context-mode:tdd` cuando se implementen nuevas funcionalidades

3. **El prompt de sesión ideal:**
   ```
   Tarea: [Tarea S1-4] Reescribir SapRawRepository — INSERT ON CONFLICT
   
   Contexto:
   - Arquitectura: docs/master-architecture.md v4.0
   - Roadmap: docs/implementation-roadmap-v1.md Sprint 1 Tarea S1-4
   - Estado: Sprint 0 completo, build verde, Npgsql instalado
   
   Archivo a modificar: src/DataBision.Infrastructure/Repositories/Ingest/SapRawRepository.cs
   Test a mantener verde: tests/DataBision.Application.Tests/Services/
   ```

4. **Criterio de sesión completada:** La tarea tiene su checkbox marcado, el commit está hecho, y `dotnet test` pasa.

5. **Al final de cada sprint:** ejecutar `mvp-hardening-check` antes del PR. Nunca omitirlo.

---

## Apéndice: Estado de Build Sprint 0 al Cierre

*Completar al finalizar Sprint 0*

| Ítem | Estado | Notas |
|---|---|---|
| SHA del commit de baseline | — | |
| `dotnet build` | — | |
| `dotnet test` | — | |
| Tests que pasan | — | |
| Tests que fallan | — | |
| Errores de build pendientes para Sprint 1 | — | |
| Supabase instancia creada | — | URL: |
| `.gitignore` verificado | — | |
| Secretos en código: ninguno | — | |

---

*Roadmap v1 — 2026-05-29 — DataBision Technical Delivery*  
*Arquitectura base: `docs/master-architecture.md` v4.0*  
*Revisión prevista: al completar Sprint 3 o cuando cambie el scope del primer cliente*
