# Demo KSDEPOR — Runbook de Arranque Local

Sprint 8L — Junio 2026

---

## A. Pre-requisitos

| Requisito | Versión mínima | Verificar con |
|---|---|---|
| .NET SDK | 8.x | `dotnet --version` |
| Node.js | 18+ | `node --version` |
| npm | 9+ | `npm --version` |
| Acceso Supabase DEV/TST | — | `appsettings.Development.json` |
| Acceso Service Layer SAP B1 | — | `https://161.153.200.53:50000/b1s/v1` |
| Variables de entorno configuradas | — | `appsettings.Development.json` |

> **Nota:** No modificar `appsettings.Development.json`. Las credenciales ya están configuradas localmente.

---

## B. Arrancar API

```powershell
cd C:\Users\user\Documents\Claude_dev\DataBision

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5103"

dotnet run --project src\DataBision.Api --no-launch-profile
```

Esperar hasta ver en consola:
```
Now listening on: http://localhost:5103
Application started.
```

---

## C. Validar API

```powershell
Invoke-WebRequest http://localhost:5103/swagger -UseBasicParsing
```

Resultado esperado: `StatusCode: 200`

O abrir en browser: [http://localhost:5103/swagger](http://localhost:5103/swagger)

---

## D. Arrancar Frontend

En una terminal separada:

```powershell
cd C:\Users\user\Documents\Claude_dev\DataBision\databision-frontend
npm run dev
```

Esperar hasta ver:
```
  VITE v8.x  ready in Xms
  ➜  Local:   http://localhost:5173/
```

---

## E. Abrir en Browser

```
http://localhost:5173
```

Para simular el tenant KSDEPOR localmente (subdominios no disponibles en dev):

```
http://localhost:5173/client/login?tenant=ksdepor
```

---

## F. Rutas de la Demo

Después del login, navegar a cada una en el sidebar "Análisis":

| Página | Ruta | Descripción |
|---|---|---|
| Dashboard | `/client/bi/dashboard` | KPIs resumen |
| Ventas | `/client/bi/sales` | Clientes, Productos, Vendedores, Fulfillment |
| Compras | `/client/bi/purchasing` | Executive, Proveedores, Recepciones |
| Inventario | `/client/bi/inventory` | Rotación, Almacenes |
| Finanzas | `/client/bi/finance` | AR Aging, AP Aging |
| Operaciones | `/client/bi/operations` | Pipeline Health, Alertas, Calidad de datos |

---

## G. Actualizar Datos Antes de la Demo

Ejecutar transform completo (STG + MART) para asegurar datos frescos:

```powershell
cd C:\Users\user\Documents\Claude_dev\DataBision

dotnet run --project src\DataBision.Extractor --configuration Debug -- --transform --include-mart --company company-dev-001
```

Duración estimada: 30–90 segundos.

Esperar mensaje final:
```
[INF] Transform complete
```

---

## H. Validar Salud del Sistema Post-Transform

```powershell
dotnet run --project src\DataBision.Extractor --configuration Debug -- --validate-ops --company company-dev-001
```

Verificar:
- `transform_run` incrementó
- Sin nuevos errores bloqueantes

```powershell
dotnet run --project src\DataBision.Extractor --configuration Debug -- --validate-staging
```

Verificar:
- `=== --validate-staging: ALL PASS ===`

---

## I. Orden Recomendado para Demo

1. Terminal 1: API corriendo (`dotnet run ...`)
2. Terminal 2: Transform ejecutado (`--transform --include-mart`)
3. Terminal 3: Frontend corriendo (`npm run dev`)
4. Browser: `http://localhost:5173/client/login?tenant=ksdepor`
5. Mostrar dashboards en orden: Ventas → Compras → Inventario → Finanzas → Operaciones

---

## J. Credenciales Demo

> Usar las credenciales del usuario demo de KSDEPOR configuradas en la base de datos de desarrollo.  
> No hardcodear aquí. Ver `appsettings.Development.json` para base de datos de conexión.

---

## K. Apagado

```powershell
# En cada terminal: Ctrl+C
```

No hay procesos background que queden corriendo.
