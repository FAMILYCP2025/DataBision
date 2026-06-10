# Native BI Frontend — E2E Dev Runbook

Procedimiento para levantar el entorno local completo y validar el módulo Native BI end-to-end.

---

## Prerrequisitos

- .NET 8 SDK instalado
- Node.js ≥ 20 + npm
- SQL Server local (o Azure SQL con allowlist de IP local)
- `appsettings.Development.json` configurado (ver plantilla `appsettings.Development.template.json`)
- Al menos una empresa con datos extraídos en la base de datos (`stg.*` y `bi.*` schemas)

---

## 1. Levantar los 3 terminales

### Terminal 1 — Backend API

```powershell
cd src/DataBision.Api
dotnet run
# Escucha en http://localhost:5000 (o el puerto configurado en launchSettings.json)
```

Verificar: `GET http://localhost:5000/health` devuelve `200 OK`.

### Terminal 2 — Extractor (opcional para datos frescos)

```powershell
cd src/DataBision.Extractor
dotnet run
# El extractor corre en modo manual o scheduled según appsettings
```

Solo necesario si querés que los datos se actualicen durante la sesión de prueba. Para validar el frontend con datos existentes, este terminal no es obligatorio.

### Terminal 3 — Frontend Dev Server

```powershell
cd databision-frontend
npm run dev
# Escucha en http://localhost:5173
```

Verificar: `http://localhost:5173/?tenant=<slug>` carga sin errores de consola.

---

## 2. Script de smoke tests automatizados

```powershell
# Desde la raíz del repo
.\scripts\dev\test-native-bi-frontend.ps1 -CompanySlug <slug>
```

El script valida:
- Rutas del frontend devuelven 200 (Vite SPA)
- Endpoint `/api/tenant/config` responde sin auth
- Endpoints `/api/client/bi/*` responden 401 (confirma que las rutas están registradas)

Resultado esperado: `All tests passed.`

---

## 3. Checklist de validación manual

Realizar en el browser (`http://localhost:5173/?tenant=<slug>`), logueado como usuario de empresa.

### 3.1. Dashboard (`/client/bi/dashboard`)

- [ ] KPI Strip carga 4 tarjetas con valores numéricos (no `—`)
- [ ] `SalesBarChart` muestra barras de los últimos 30 días
- [ ] `TopCustomersTable` muestra al menos 1 fila
- [ ] `SyncStatusWidget` en el header muestra estado (ok / warning / error)
- [ ] Al recargar la página, los datos cargan sin FOUC (flash of unstyled content)
- [ ] En viewport < 900px: KPI Strip colapsa a 2 columnas
- [ ] En viewport < 540px: KPI Strip colapsa a 1 columna, header se apila

### 3.2. Ventas (`/client/bi/sales`)

- [ ] Overview cards (4 KPIs) cargan correctamente
- [ ] DateRangePicker funciona: cambiar rango actualiza los KPIs
- [ ] Tab "Clientes" muestra SortableTable paginada
- [ ] Tab "Productos" muestra SortableTable paginada
- [ ] Tab "Vendedores" muestra SortableTable paginada
- [ ] Click en cabecera de columna ordena la tabla (sort ascendente/descendente)
- [ ] Paginación funciona (botones Anterior / Siguiente)
- [ ] En mobile: tab bar tiene scroll horizontal sin cortar labels

### 3.3. Diagnósticos (`/client/bi/diagnostics`)

- [ ] Visible solo para `CompanyAdmin` — usuarios no-admin ven pantalla de acceso restringido
- [ ] Tabla "Verificaciones del sistema" carga con status badges coloreados
- [ ] Tabla "Conteo de filas por tabla" carga con row counts reales
- [ ] Botón "Actualizar" recarga los datos
- [ ] Badge de estado global (`ok` / `warning` / `error`) visible en el card header
- [ ] Footer "Generado: <datetime>" visible bajo la tabla de checks

### 3.4. Responsivo y accesibilidad

- [ ] Ningún contenido se corta horizontalmente en viewport 375px
- [ ] Tab bar de ventas tiene `role="tablist"` y tabs tienen `role="tab"` (verificar con DevTools)
- [ ] Botón "Actualizar" en diagnósticos tiene `aria-label`
- [ ] No hay errores de consola en ninguna de las 3 páginas

---

## 4. Flujo de datos end-to-end (para entender qué valida qué)

```
SAP B1 Service Layer
      ↓
DataBision.Extractor  →  stg.* (staging tables)
                      →  bi.*  (transformed tables)
                              ↓
                     DataBision.Api  →  /api/client/bi/*
                                              ↓
                              databision-frontend
                              /client/bi/dashboard
                              /client/bi/sales
                              /client/bi/diagnostics
```

Si los KPIs muestran `—` o cero, verificar:
1. `bi.*` tables tienen datos (`scripts/dev/test-native-bi-frontend.ps1` valida 401 en API, no datos)
2. El extractor corrió al menos una vez para el tenant
3. El JWT del usuario logeado incluye el `company_id` correcto

---

## 5. Notas de debugging

| Síntoma | Causa probable | Verificación |
|---|---|---|
| Página en blanco en `/client/bi/*` | Ruta no registrada en React Router | Ver `src/client/ClientApp.tsx` o `src/App.tsx` |
| KPIs muestran `—` siempre | API devuelve 401 o 403 | Abrir Network tab, verificar JWT en Authorization header |
| `SyncStatusWidget` muestra "Desconocido" | `/api/client/bi/diagnostics` devuelve error | Ver logs de API |
| Tab bar no hace scroll en mobile | Falta `overflow-x: auto` en `.nb-tab-bar` | Verificar `src/index.css` |
| Error "Cannot read properties of undefined" | Tipo de API response no coincide con tipo TS | Ver `src/client/types/nativeBi.ts` |

---

*Runbook versión 1.0 — 2026-06-08*  
*Relacionado: `native-bi-screen-specs.md` · `frontend-ux-architecture.md`*
