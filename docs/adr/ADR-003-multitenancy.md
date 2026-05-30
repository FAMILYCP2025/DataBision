# ADR-003 — Modelo Multi-Tenant: DB por Tenant → Supabase Compartida con company_id

**Fecha:** 2026-05-29  
**Estado:** Aceptado  
**Autor:** Chief Architect  

---

## Contexto

El diseño original usaba una Azure SQL Database independiente por cada tenant DataBision. Este modelo ofrece el mayor nivel de aislamiento pero implica un costo operacional significativo: una DB mínima en Azure SQL cuesta USD 15–35/mes incluso sin datos.

Con el cambio a Supabase PostgreSQL y el objetivo de soportar planes desde USD 350/mes, el modelo de DB por tenant es inviable comercialmente.

---

## Opciones Evaluadas

### Opción A — Una base de datos por tenant (diseño original)
**Aislamiento:** máximo. Las queries de un tenant no pueden acceder físicamente a datos de otro.

**Costo por tenant:**
- Azure SQL Basic: ~USD 5/mes (limitado, no apto producción real)
- Azure SQL S1: ~USD 15/mes
- Supabase por tenant: USD 25/mes × N tenants (escala mal)

**Ventajas:**
- Aislamiento contractual claro
- PITR independiente por tenant
- Scaling independiente

**Desventajas:**
- Costo prohibitivo para planes USD 350/mes
- Setup complejo: N bases de datos a gestionar
- Migraciones de schema deben aplicarse a N bases (orquestación)

### Opción B — Schema por tenant en una sola base
**Aislamiento:** lógico por schema. `acme.sap_oinv`, `constructora.sap_oinv`.

**Costo:** una sola instancia de BD.

**Ventajas:** aislamiento más fuerte que Opción C sin costo de Opción A.

**Desventajas:**
- PostgreSQL no tiene "schema-level connection": todas las queries pueden ver todos los schemas si hay error de permiso
- Migrations deben crear/modificar N schemas
- Supabase no facilita este modelo

### Opción C — Instancia compartida + company_id por fila (decisión tomada)
**Aislamiento:** lógico por fila. Toda tabla tiene `company_id` como columna y parte del PK.

**Costo:** una instancia Supabase Pro (USD 25/mes) para todos los tenants.

**Ventajas:**
- Costo mínimo
- Setup simple
- Queries con `company_id` explícito en la API son el patrón de seguridad establecido
- Supabase RLS puede añadirse como capa de defensa adicional en Fase 2

**Desventajas:**
- Un bug en la API que omita el filtro `company_id` puede exponer datos de otro tenant
- No hay aislamiento físico
- Un tenant con volumen muy grande puede impactar la performance del servidor compartido

---

## Decisión

**Opción C — Instancia compartida con `company_id` como filtro de seguridad.**

### Análisis de riesgo

El riesgo principal (filtro omitido → data leak) se mitiga en múltiples capas:

1. **TenantMiddleware:** extrae `company_id` del subdomain y lo añade a `HttpContext`. No es una opción que el código pueda omitir accidentalmente.
2. **Principio en CLAUDE.md:** "Every query to data tables must include an explicit `company_id` filter — never rely on upstream filtering."
3. **Code review:** todo PR que toque queries es revisado con foco en filtros de tenant.
4. **Tests:** los tests de integración deben incluir un caso "tenant A no puede ver datos de tenant B".
5. **Supabase RLS (Fase 2):** añadir Row Level Security como defensa adicional. No en MVP para no incrementar complejidad inicial.

### Cuándo escalar a aislamiento mayor

| Trigger | Acción |
|---|---|
| Cliente requiere aislamiento contractual explícito (bancario, salud) | Schema separado en misma instancia o instancia Supabase propia |
| Un tenant supera el 40% del storage de Supabase Pro | Evaluar instancia dedicada para ese tenant |
| 50+ tenants activos con alto volumen | Evaluar sharding o instancias separadas por región |
| Cliente Enterprise con requerimiento de residencia de datos | Azure DB for PostgreSQL en región específica |

---

## Especificación Técnica

### PK compuesta con company_id

```sql
-- Ejemplo: raw.sap_oinv
CREATE TABLE "raw"."sap_oinv" (
    company_id   UUID         NOT NULL REFERENCES companies(id),
    "DocEntry"   INTEGER      NOT NULL,
    ...
    PRIMARY KEY (company_id, "DocEntry")
);
```

### Índices

Índice clusterizado en `(company_id, NaturalPK)` — garantiza que las queries por tenant sean seek, no scan.

### INSERT ON CONFLICT con company_id

```sql
INSERT INTO "raw"."sap_oinv" (company_id, "DocEntry", ...)
VALUES (@company_id, @DocEntry, ...)
ON CONFLICT (company_id, "DocEntry") DO UPDATE SET
    ...
WHERE "raw"."sap_oinv".source_hash_hex != EXCLUDED.source_hash_hex
  AND (temporal guard condition)
RETURNING (xmax = 0)::int AS is_insert;
```

El `company_id` es parte del conflict target, por lo que filas de tenants distintos con mismo `DocEntry` no interfieren entre sí.

---

## Documentos Afectados

- `databision-product-architecture.md` — sección "una Azure SQL DB por tenant" SUPERSEDED
- `azure-sql-staging-design.md` — "MVP: una DB por tenant" SUPERSEDED para MVP; válido para Enterprise
- `two-client-production-roadmap.md` — referencias a "DB dedicada por tenant" aplican al modelo Enterprise futuro, no al MVP
