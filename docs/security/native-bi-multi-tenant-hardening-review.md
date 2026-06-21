# Native BI Finance — Revisión Hardening Multiempresa

**Sprint:** 21E  
**Fecha:** 2026-06-21  
**Alcance:** ClientBiFinanceController, ProcessDashboardRepository, MART functions, extractor, frontend

---

## Resumen ejecutivo

El sistema tiene un modelo de aislamiento correcto por diseño: todas las queries filtran por `company_id` parameterizado, la resolución de `company_id` está centralizada en `CompanyContextResolver` y `AnalyticsCompanyResolver`, y el extractor opera una empresa a la vez. Se encontró y corrigió un riesgo menor: slugs de desarrollo en `appsettings.json` base (commiteado a git).

**Estado post-revisión:** ✅ Sin vulnerabilidades críticas identificadas.

---

## 1. ClientBiFinanceController

**Patrón revisado:**
```csharp
[AllowAnonymous]  // ← bypasea el middleware global, pero...
public async Task<IActionResult> GetIncomeStatement(...) {
    var ctx = CompanyContextResolver.TryResolve(HttpContext, config);  // ← maneja auth aquí
    if (!ctx.IsSuccess) return ctx.Error!;
    // solo llega aquí si tiene company_id válido
}
```

**Evaluación:**
- `[AllowAnonymous]` no expone datos — `CompanyContextResolver` sigue siendo el guardián
- En PROD (Jwt:PublicKey configurado): unauthenticated → 401, authenticated sin company claim → 403
- En DEV (Jwt:PublicKey vacío): acepta `?companyId=` query param — comportamiento intencional

**Riesgo:** Si `Jwt:PublicKey` está vacío en PROD por error de configuración, todos los endpoints Native BI quedan abiertos con solo `?companyId=slug`. **Mitigación:** Deploy checklist debe verificar que `Jwt:PublicKey` está configurado. Variable de entorno, no en appsettings.json.

**Acción requerida:** Ninguna en código. Documentada en deploy checklist.

---

## 2. AnalyticsCompanyResolver — CompanySlugMap

**Problema encontrado (RESUELTO en Sprint 21E):**

`appsettings.json` (commiteado a git) contenía:
```json
"NativeBi": {
    "CompanySlugMap": {
        "ksdepor": "company-dev-001",
        "demo":    "company-dev-001"
    }
}
```

Estos son slugs reales de cliente expuestos en git. Aunque el mapping solo se usa en Development (guard `env.IsDevelopment()`), tenerlos en el archivo base commiteado:
1. Expone nombres de clientes en historial git
2. Si env detection falla, actúa como fallback no intencional

**Fix aplicado:**
```json
// appsettings.json (base, commiteado)
"NativeBi": {
    "DefaultAnalyticsCompanyId": "",
    "CompanySlugMap": {}  // ← vacío
}
```

Los valores reales siguen en `appsettings.Development.json` (gitignoreado).

---

## 3. ProcessDashboardRepository — Tenant isolation

**Revisión:** Todas las queries verificadas.

| Query | Filtro company_id | Parametrizado |
|---|---|---|
| GetSalesCustomersAsync | `WHERE company_id = @company_id` | ✅ |
| GetIncomeStatementAsync | `WHERE company_id = @company_id` | ✅ |
| GetBalanceSheetAsync | `WHERE company_id = @company_id` | ✅ |
| GetEbitdaAsync | `WHERE company_id = @company_id` | ✅ |
| GetChartOfAccountsAsync | `WHERE company_id = @company_id` | ✅ |
| GetFinanceValidationsAsync | `WHERE company_id = @company_id` | ✅ |
| GetFinanceReadinessAsync | `WHERE company_id = @company_id` (×12) | ✅ |
| GetFinanceRefreshStatusAsync | `WHERE company_id = @company_id` (×3) | ✅ |

**Sin raw SQL interpolation detectada** en queries con company_id. Regla 3 del CLAUDE.md cumplida.

---

## 4. MART functions (Supabase SQL)

```sql
-- Ejemplo refresh_income_statement
CREATE OR REPLACE FUNCTION mart.refresh_income_statement(p_company_id TEXT)
...
    DELETE FROM mart.income_statement_summary WHERE company_id = p_company_id;
    INSERT INTO mart.income_statement_summary (company_id, ...)
    SELECT p_company_id, ...
    FROM mart.account_balances
    WHERE company_id = p_company_id
    ...
```

**Evaluación:** Todas las funciones MART usan `p_company_id` como parámetro y filtran estrictamente. Ninguna función modifica datos de otra empresa. `refresh_accounting_all` propaga el mismo `p_company_id` a todos los sub-steps.

✅ Aislamiento correcto.

---

## 5. Extractor — company_id

El extractor opera con un único `Extractor:CompanyId` configurado en appsettings. No tiene acceso multi-empresa por diseño:

```json
{
  "Extractor": {
    "TenantId": "tenant-dev",
    "CompanyId": "company-dev-001"
  }
}
```

Cada cliente tiene su propia instancia del extractor con su propia config. No hay riesgo de cross-tenant en el extractor.

**Riesgo:** El código en `Program.cs` tiene `company-dev-001` en los ejemplos del `--help`. Esto es en strings de documentación, no en lógica de negocio. Aceptable.

---

## 6. Ingest endpoints

`SapB1IngestController` usa `ApiKeyAuthFilter`. El API key mapea a `tenantId:companyId` en appsettings:
```json
"Ingest": {
    "ApiKeys": {
        "dev-key-001": "tenant-dev:company-dev-001"
    }
}
```

El batch de ingest lleva `CompanyId` en el payload, y el endpoint lo valida contra el company_id del API key. Un API key no puede ingesar datos para otra empresa.

✅ Correcto.

---

## 7. Frontend — companyId / slug

El `companyId` en el frontend siempre viene del auth store:
```typescript
async function getTenant(): Promise<string | null> {
    const { useClientAuthStore } = await import('../store/useClientAuthStore')
    return useClientAuthStore.getState().tenant
}
```

No hay hardcoded slugs en el frontend (solo un placeholder `ksdepor-analytics` en un campo de texto de admin).

✅ Correcto.

---

## 8. Program.cs — comandos de ejemplo

Los comandos de ejemplo en el `--help` del extractor usan `company-dev-001`:
```
dotnet run -- --transform --company company-dev-001
```

Esto es solo texto de documentación en el ejecutable. No es lógica de negocio. Aceptable.

---

## Resumen de hallazgos

| # | Hallazgo | Severidad | Estado |
|---|---|---|---|
| 1 | `CompanySlugMap` con slugs reales en `appsettings.json` base | MEDIA | ✅ CORREGIDO |
| 2 | `AllowAnonymous` expone endpoints si `Jwt:PublicKey` no configurado | MEDIA | ✅ DOCUMENTADO |
| 3 | `company-dev-001` en `--help` examples de Program.cs | BAJA | Aceptable (docs) |
| 4 | Sin aislamiento de queries en repositorio | N/A | ✅ No encontrado |
| 5 | Raw SQL interpolation | N/A | ✅ No encontrado |
| 6 | Cross-tenant en MART functions | N/A | ✅ No encontrado |
| 7 | Frontend hardcoded companyId | N/A | ✅ No encontrado |

---

## Checklist producción (derivado de esta revisión)

- [ ] `Jwt:PublicKey` y `Jwt:PrivateKey` configurados como variables de entorno en servidor de producción
- [ ] `appsettings.json` base NO tiene datos de cliente (validado en Sprint 21E ✅)
- [ ] `appsettings.Production.json` tiene `CompanySlugMap: {}` vacío (fallback DEV desactivado en PROD por `env.IsDevelopment()`)
- [ ] `Ingest:ApiKeys` en variables de entorno, no en archivos commiteados
- [ ] Cada cliente tiene su propio API key de ingest
- [ ] `StagingConnection` (Supabase) en variables de entorno, no en archivos commiteados
