# DataBision — Debugging de visibilidad de datos en la web (KSDEPOR)

Sprint 8R — Diagnóstico profundo — Junio 2026

---

## 0. Causa final corregida: doble /api + URLs del frontend

**Evidencia del navegador (DevTools Console):**
```
GET http://localhost:5173/api/api/client/sales/overview?companyId=ksdepor... → 404
GET http://localhost:5173/api/api/client/dashboard/summary?companyId=ksdepor → 404
```

**Causa raíz definitiva:** el cliente Axios en `src/lib/api.ts` tiene `baseURL = '/api'`. Pero `nativeBiApi.ts` y `processBiApi.ts` construían las rutas **ya con el prefijo `/api/`** (ej. `/api/client/dashboard/summary`). Resultado: `/api` (baseURL) + `/api/client/...` (ruta) = **`/api/api/client/...`** → 404.

Por eso el **login funcionaba** (`clientApi.ts` usa rutas sin prefijo: `/auth/login`, `/modules`) pero **todas las llamadas Native BI fallaban** (31 llamadas con doble `/api`).

> En las validaciones anteriores probé la API **directamente** (`http://localhost:5103/api/client/...`), no a través del Axios del frontend — por eso devolvían 200 y el doble `/api` no se detectó hasta tener la evidencia del navegador.

### Regla de baseURL (definitiva)

- `baseURL = '/api'` (en `lib/api.ts`, vía `VITE_API_URL ?? '/api'`).
- **Todas** las rutas en los api clients deben empezar con `/client/...`, `/auth/...`, `/modules`, etc. — **NUNCA con `/api/`**.
- El proxy de Vite reenvía `/api/*` → `http://localhost:5103/api/*`.
- URL final correcta: `http://localhost:5173/api/client/...` → proxy → `http://localhost:5103/api/client/...`.
- URL incorrecta (corregida): `http://localhost:5173/api/api/client/...` → 404.

### Endpoints corregidos (doble /api → simple)

`nativeBiApi.ts` (16) y `processBiApi.ts` (15): se reemplazó `` `/api/client/... `` por `` `/client/... ``. Ninguno era obsoleto — `sales/overview`, `dashboard/sales-daily`, `dashboard/top-customers` **existen** en el backend; solo estaban mal prefijados.

### Evidencia del fix (API en vivo, 2026-06-16)

```
=== Path viejo (doble /api) ===
404  /api/api/client/dashboard/summary
404  /api/api/client/sales/overview

=== Path nuevo (corregido) ===
200  rows=1   /api/client/dashboard/summary
200  rows=39  /api/client/dashboard/sales-daily   (gráfico con datos)
200  rows=10  /api/client/dashboard/top-customers
200  rows=1   /api/client/sales/overview          (rango 365d)
200  rows=21  /api/client/sales/customers
200  rows=18  /api/client/bi/sales/customers-dashboard
200  rows=18  /api/client/bi/purchasing/suppliers
200  rows=41  /api/client/bi/inventory/rotation
200  rows=18  /api/client/bi/finance/ar-aging
200  rows=1   /api/client/bi/operations/pipeline-health
200  rows=1   /api/client/diagnostics/native-bi
```

### Cómo detectar `/api/api` en consola

En DevTools → Network/Console, buscar requests con `/api/api/`. Si aparecen, hay una ruta en un api client con prefijo `/api/` duplicado. También el logger DEV de `lib/api.ts` imprime `[api] FAIL <status> <endpoint>`.

### Cómo validar después del fix

1. Reiniciar API en 5103 y frontend (`npm run dev`).
2. Login `?tenant=ksdepor` → abrir DevTools Network.
3. Confirmar: **no** hay `/api/api/`, los endpoints devuelven 200, la UI muestra filas.

---

## 1. Causa raíz real (iteraciones previas)

El problema NO era un único bug, sino tres causas distintas que se confundían bajo el mismo síntoma ("la web no muestra datos"):

### Causa A — Backend ya correcto, pero proceso API potencialmente stale

Tras aplicar `IAnalyticsCompanyResolver` a los 6 servicios (`ProcessDashboardService`, `DashboardService`, `SalesService`, `SyncStatusService`, `DiagnosticsService`, `ProcessService`), **el backend compilado devuelve 200 con datos reales en los 19 endpoints** — verificado en vivo contra `http://localhost:5103` con el mismo patrón de llamada que usa la web (token Bearer + `?companyId=<tenant>`).

Por lo tanto, si la web seguía sin datos, la causa más probable era un **proceso de API stale**: la instancia de la API que la web consume seguía corriendo el binario anterior a las correcciones. **Solución: reiniciar la API tras cada rebuild.**

### Causa B — Ruta inicial incorrecta (bug real de frontend)

Al hacer login, el frontend navegaba a `/client`, cuya página `ClientHomePage` consultaba `useClientModules()` y, si existían módulos (los 6 módulos Power BI existen con 0 reportes), **redirigía automáticamente al primer módulo**: `/client/modules/comercial` → "Sin informes disponibles".

Esto hacía que la primera pantalla de la demo fuera un módulo Power BI vacío en lugar del dashboard Native BI.

### Causa C — Rango de fechas por defecto fuera del rango de datos

La pantalla de Ventas usaba un rango por defecto de **últimos 30 días** (17/05–16/06). Los datos de demo en MART tienen fechas anteriores (ej. `last_invoice_date = 2026-02-01`), por lo que los KPIs del overview salían vacíos aunque las tablas (paginadas, sin filtro de fecha) sí tenían datos.

---

## 2. Por qué Supabase tenía data pero la web no

| Capa | Estado | Detalle |
|---|---|---|
| Supabase / MART | ✅ Con datos | `company_id = 'company-dev-001'` (18 customers, 18 suppliers, 41 rotation, 18 AR) |
| Backend (código compilado) | ✅ Correcto | Resolver mapea `ksdepor`/`demo` → `company-dev-001`; 19/19 endpoints 200 con datos |
| API en ejecución | ⚠️ Posible stale | Si no se reinicia tras el rebuild, sirve el binario antiguo |
| Frontend — auth/proxy | ✅ Correcto | Login funciona → proxy `:5103` y token Bearer OK |
| Frontend — ruta inicial | ❌ Bug | Redirigía a `/client/modules/comercial` (módulo Power BI vacío) |
| Frontend — rango fecha ventas | ❌ Bug | Default 30 días dejaba los KPIs fuera de rango |

---

## 3. Endpoints validados (evidencia en vivo, 2026-06-16)

API en `http://localhost:5103` (build actual), login `admin@demo.com` (slug `demo` → `company-dev-001`, mapeo idéntico a `ksdepor`). Llamados **igual que la web**: token Bearer + `?companyId=demo`.

```
LOGIN OK (company=1, role=CompanyAdmin)

200  rows=1     /api/client/dashboard/summary           (legacy DashboardService)
200  rows=21    /api/client/sales/customers             (legacy SalesService)
200  rows=11    /api/client/sales/items                 (legacy SalesService)
200  rows=4     /api/client/sales/salespersons          (legacy SalesService)
200  rows=18    /api/client/bi/sales/customers-dashboard
200  rows=11    /api/client/bi/sales/items-dashboard
200  rows=7     /api/client/bi/sales/fulfillment
200  rows=3     /api/client/bi/purchasing/executive
200  rows=18    /api/client/bi/purchasing/suppliers
200  rows=10    /api/client/bi/purchasing/receiving
200  rows=41    /api/client/bi/inventory/rotation
200  rows=12    /api/client/bi/inventory/warehouses
200  rows=2     /api/client/bi/finance/executive
200  rows=18    /api/client/bi/finance/ar-aging
200  rows=0     /api/client/bi/finance/ap-aging         (sin datos AP — empty state)
200  rows=1     /api/client/bi/operations/pipeline-health
200  rows=44    /api/client/bi/operations/alerts
200  rows=0     /api/client/bi/operations/data-quality  (sin issues — empty state)
200  rows=1     /api/client/diagnostics/native-bi
```

**0 errores 500, 0 errores 401, 0 errores 404.** El endpoint de diagnósticos responde 200 — la ruta correcta es `/api/client/diagnostics/native-bi` (no `/api/client/diagnostics`).

---

## 4. Servicios / rutas corregidos

### Frontend

| Archivo | Cambio |
|---|---|
| `src/client/pages/ClientHomePage.tsx` | `/client` ahora redirige siempre a `/client/bi/dashboard` (Native BI), no al primer módulo Power BI |
| `src/client/pages/ModulePage.tsx` | Empty state de módulo sin reportes ahora explica la diferencia Power BI vs Native BI y ofrece botón "Ir a Análisis Native BI" |
| `src/client/pages/NativeBiSalesPage.tsx` | Rango de fechas por defecto ampliado de 30 a 365 días para cubrir datos históricos de la demo |
| `src/lib/api.ts` | Logging DEV seguro (endpoint + status + nº filas), sin token ni body, para diagnóstico en consola del navegador |

### Backend

Sin cambios en esta fase — ya estaba correcto (Sprint 8R aplicó el resolver a los 6 servicios). Validado en vivo.

---

## 5. Cómo validar por Postman / API

1. Levantar la API en 5103 (reiniciar si estaba corriendo):
   ```powershell
   $root = "C:\Users\user\Documents\Claude_dev\DataBision"
   $env:ASPNETCORE_ENVIRONMENT = "Development"
   $env:ASPNETCORE_URLS = "http://localhost:5103"
   dotnet run --project src\DataBision.Api --no-launch-profile
   ```
2. `POST http://localhost:5103/api/auth/login?tenant=ksdepor` con `demo@ksdepor.com` → copiar `data.accessToken`.
3. Con header `Authorization: Bearer <token>`, llamar cualquier endpoint de la sección 3.
4. Esperado: 200 con filas. AP aging y data-quality pueden devolver 0 (empty state válido).

> El JWT claim `company_slug` tiene prioridad sobre el query param `companyId`. El resolver mapea el slug al `company_id` analítico.

---

## 6. Cómo validar visualmente

1. **Reiniciar la API** (clave — evita el binario stale) en 5103.
2. `cd databision-frontend && npm run dev` → `http://localhost:5173`.
3. Login: `http://localhost:5173/client/login?tenant=ksdepor` con `demo@ksdepor.com`.
4. Verificar redirección inicial → **`/client/bi/dashboard`** (ya no a `/client/modules/comercial`).
5. Recorrer:
   - **Dashboard**: KPIs (ventas netas, facturas, clientes, ticket) + gráfico + top clientes.
   - **Ventas**: tabs Clientes/Productos/Vendedores/Fulfillment con datos; KPIs del overview con rango 365 días.
   - **Compras**: Proveedores/Recepciones/Evolución OC.
   - **Inventario**: Rotación/Almacenes/Sin movimiento.
   - **Finanzas**: AR con datos; AP con empty state explicativo.
   - **Operaciones**: pipeline health + alertas (44).
   - **Diagnósticos** (CompanyAdmin): verificaciones + conteo de filas, sin 404.
6. Si una pantalla muestra "error al cargar": abrir consola del navegador (F12) y revisar los logs `[api] FAIL <status> <endpoint>` para identificar el endpoint y status exacto.

---

## 7. Pendientes para producción

1. **`Company.AnalyticsCompanyId`**: mover el mapping de `appsettings` a una columna en la tabla `companies` (migración + UI SuperAdmin). Reemplazar `AnalyticsCompanyResolver` por lookup en DB.
2. **`[AllowAnonymous]` en controllers BI**: en producción, sustituir por `[Authorize]` para forzar autenticación a nivel de pipeline (el `CompanyContextResolver` ya valida JWT y company claim).
3. **Rango de fechas**: el default de 365 días es para la demo. En producción, considerar `max(last_invoice_date)` desde backend o un selector de período más explícito.
4. **Reinicio de API**: documentar en el runbook de demo que tras cada rebuild se debe reiniciar la API para evitar binarios stale.

---

## 8. Diferencia: Módulos Power BI vs Análisis Native BI

| Sección | Qué es | Fuente de datos | Visibilidad |
|---|---|---|---|
| **Módulos** (Power BI) | Reportes Power BI embebidos asignados por el SuperAdmin | Power BI workspace (requiere reportes asignados) | Oculta en sidebar si todos los módulos tienen 0 reportes |
| **Análisis** (Native BI) | Dashboards nativos: Ventas, Compras, Inventario, Finanzas, Operaciones, Diagnósticos | MART de Supabase (vía `ProcessDashboardService` / `DashboardService` con el resolver) | Siempre visible; es la experiencia principal de la demo |

Para KSDEPOR demo: no hay reportes Power BI asignados, por lo que la sección Módulos se oculta y la página inicial es el dashboard Native BI. Los links antiguos a `/client/modules/*` muestran un empty state con salida hacia Native BI.
