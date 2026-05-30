# DataBision — Tenant Subdomain Portal Strategy

**Versión:** 1.1  
**Fecha:** 2026-05-30  
**Estado:** ✅ Actualizado

> **Cambios v1.1 (resolución auditoría):**
>
> **Dominio canónico:** `databision.com` (no `databision.app`). Subdomios: `admin.databision.com`, `{slug}.databision.com`.
>
> **Motor de reportes:** Los reportes son dashboards **DataBision Native BI** (React + ECharts). Power BI no es el mecanismo principal de reportería. El botón "Actualizar" (`POST /api/reports/{id}/refresh`) que aparece en §3.3 era para Power BI REST API — en la arquitectura actual ese endpoint dispara extracción inmediata via Ingest API. Ver [ADR-002](adr/ADR-002-bi-layer.md).
>
> **Estructura de portal:** Los módulos del portal han evolucionado. Ver [docs/frontend-ux-architecture.md](frontend-ux-architecture.md) para la estructura completa con Cockpit, Live Layer, Alertas, Recomendaciones y Business Actions.

---

## 1. Concepto Central

El portal de DataBision no es solo un contenedor de reportes Power BI. Es la plataforma operativa completa del cliente. El cliente debería ver el portal y pensar "este es mi sistema de reportería empresarial", no "este es Power BI con nuestro logo".

**Diferenciador clave:** DataBision entrega contexto operativo que Power BI no da:
- ¿Cuándo se actualizaron los datos?
- ¿Está el extractor corriendo bien?
- ¿Puedo pedir una actualización ahora?
- ¿Qué pasó ayer con los datos?

---

## 2. Arquitectura de Subdominios

```
databision.com              → landing page / marketing
admin.databision.com        → SuperAdmin (DataBision staff)
acme.databision.com         → Portal empresa ACME
constructora.databision.com → Portal empresa Constructora XYZ
```

**Configuración DNS:**
- `*.databision.com` → CNAME a Azure App Service / Vercel
- Certificado wildcard `*.databision.com` via Let's Encrypt o Azure App Service Managed

**Resolución de tenant:**
- Producción: el `Host` header del request contiene el subdominio → `TenantMiddleware` resuelve `company_id`
- Desarrollo local: `?tenant=acme` como query param (ya implementado en CLAUDE.md)

---

## 3. Portal por Tenant — Estructura de Páginas

### 3.1 Página de Login (`/login`)

```
┌─────────────────────────────────┐
│  [Logo del cliente]              │
│                                  │
│  Bienvenido a ACME Analytics     │
│                                  │
│  [Email    ________________]    │
│  [Password ________________]    │
│                                  │
│  [       Ingresar        ]      │
│                                  │
│  Powered by DataBision           │
└─────────────────────────────────┘
```

- Logo, colores y mensaje de bienvenida configurados por tenant
- "Powered by DataBision" con enlace discreto (opcional, puede ocultarse en Plan Advanced)

---

### 3.2 Dashboard Principal (`/`)

```
┌──────────────────────────────────────────────────────┐
│ [Logo] ACME Analytics          [Nombre usuario ▼]    │
├──────────────────────────────────────────────────────┤
│ Sidebar           │ Contenido principal               │
│ ──────────────   │                                   │
│ 📊 Ventas        │  ┌─────────────────────────────┐  │
│ 👥 Clientes      │  │  Resumen Ejecutivo          │  │
│ 📦 Inventario    │  │  Ventas mes: $12.4M         │  │
│ 💳 Créditos      │  │  vs. mes anterior: +8.2%    │  │
│ 👤 Vendedores    │  └─────────────────────────────┘  │
│ ──────────────   │                                   │
│ ⚡ Estado datos  │  ┌─────────┐  ┌─────────────────┐ │
│ ❓ Soporte       │  │ Top 5   │  │ Último refresh  │ │
│                  │  │ Clientes│  │ Hoy 14:35       │ │
│                  │  └─────────┘  └─────────────────┘ │
└──────────────────────────────────────────────────────┘
```

---

### 3.3 Página de Reporte (`/reports/{id}`)

```
┌──────────────────────────────────────────────────────┐
│ [Sidebar]  │  Dashboard de Ventas          [Actualizar] │
│            │  Última actualización: Hoy 14:35           │
│            │  ─────────────────────────────────────────  │
│            │                                             │
│            │  ┌─────────────────────────────────────┐  │
│            │  │                                     │  │
│            │  │     [IFRAME Power BI Report]        │  │
│            │  │                                     │  │
│            │  │                                     │  │
│            │  └─────────────────────────────────────┘  │
└──────────────────────────────────────────────────────┘
```

**Botón "Actualizar":**
- Llama a `POST /api/reports/{id}/refresh`
- Backend llama a Power BI REST API
- UI muestra "Actualizando..." → polling hasta completar
- Throttle: solo 1 solicitud manual por hora por usuario

---

### 3.4 Página de Estado del Extractor (`/data-status`)

Esta página es uno de los mayores diferenciadores de DataBision.

```
┌──────────────────────────────────────────────────────┐
│ Estado de Datos — ACME Chile                          │
├──────────────────────────────────────────────────────┤
│ Objeto SAP    │ Última sync  │ Registros │ Estado      │
│ ─────────────┼──────────────┼───────────┼─────────── │
│ Facturas      │ Hoy 14:35    │ 12.451    │ ✅ OK      │
│ Líneas Fact.  │ Hoy 14:35    │ 48.230    │ ✅ OK      │
│ Notas Crédito │ Hoy 14:35    │  1.892    │ ✅ OK      │
│ Clientes      │ Hoy 09:02    │  3.104    │ ✅ OK      │
│ Items         │ Hoy 09:02    │  8.541    │ ✅ OK      │
│ Vendedores    │ Hoy 09:02    │     24    │ ✅ OK      │
├──────────────────────────────────────────────────────┤
│ Última ejecución exitosa: Hoy 14:35 (duración: 2m 14s)│
│ Próxima ejecución programada: Hoy 15:35               │
└──────────────────────────────────────────────────────┘
```

**Datos desde:**
- `ctl.ingest_checkpoint`: watermark y última sync por objeto SAP
- `ctl.extraction_run`: historial de ejecuciones, duración, estado
- `raw.sap_*`: COUNT por empresa

---

### 3.5 Historial de Actualizaciones (`/data-status/history`)

```
┌──────────────────────────────────────────────────────┐
│ Historial de actualizaciones (últimos 30 días)        │
├──────────────────────────────────────────────────────┤
│ Fecha/Hora       │ Duración │ Registros nuevos │ Estado │
│ ─────────────────┼──────────┼──────────────────┼────── │
│ 2026-05-28 14:35 │  2m 14s  │ +47 facturas     │ ✅    │
│ 2026-05-28 12:35 │  1m 58s  │ +12 facturas     │ ✅    │
│ 2026-05-28 10:35 │  2m 02s  │ +89 facturas     │ ✅    │
│ 2026-05-27 14:35 │  2m 31s  │ +134 facturas    │ ✅    │
│ 2026-05-26 14:35 │    —     │ —                │ ⚠️ Timeout │
└──────────────────────────────────────────────────────┘
```

---

## 4. Catálogo de Reportes

Los reportes se organizan por área funcional. El portal muestra solo los reportes que el usuario tiene acceso.

```
/reports → catálogo por área

Ventas
  └── R01: Dashboard Ejecutivo de Ventas
  └── R02: Análisis por Vendedor
  └── R03: Pipeline Clientes (futuro)

Clientes
  └── R04: Ranking de Clientes
  └── R05: Clientes Inactivos

Inventario
  └── R06: Catálogo de Items
  └── R07: Stock Crítico (requiere OITW)

Finanzas
  └── R08: Notas de Crédito y Devoluciones
  └── R09: Estado de Cartera
```

**Metadata por reporte:**
- Nombre y descripción
- Área funcional (ícono + categoría)
- Última actualización del dataset
- Objetos SAP que usa (para que el cliente entienda qué datos alimenta el reporte)
- Acceso: por rol (Admin, Analyst, Viewer)

---

## 5. Roles de Usuario en el Portal

| Rol | Permisos |
|---|---|
| **CompanyAdmin** | Ve todos los reportes, gestiona usuarios, ve estado extractor, solicita actualización |
| **Analyst** | Ve todos los reportes asignados, solicita actualización |
| **Viewer** | Ve reportes asignados, sin acción |

**Gestión desde portal (Phase 2+):**
- CompanyAdmin puede invitar usuarios por email
- Asignar reportes a usuarios
- Ver logs de acceso

---

## 6. Configuración de Branding por Tenant

Almacenado en la tabla `companies` o `tenant_configs`:

```json
{
  "company_id": "acme-cl",
  "slug": "acme",
  "display_name": "ACME Chile S.A.",
  "logo_url": "https://blob.databision.com/branding/acme/logo.png",
  "favicon_url": "https://blob.databision.com/branding/acme/favicon.ico",
  "brand_primary": "#E63946",
  "brand_secondary": "#457B9D",
  "brand_sidebar": "#1D3557",
  "welcome_message": "Bienvenido al portal de reportería de ACME Chile",
  "powered_by_visible": true
}
```

**Endpoint (ya implementado):**
```
GET /api/tenant/config
→ { data: TenantConfig }
```

---

## 7. SuperAdmin Panel (`admin.databision.com`)

El SuperAdmin gestiona todos los tenants desde una vista centralizada:

```
/admin/tenants        → Lista de clientes activos
/admin/tenants/new    → Crear nuevo cliente
/admin/tenants/{id}   → Detalle: usuarios, reportes, estado extractor
/admin/reports        → Biblioteca global de reportes
/admin/extractors     → Estado de todos los extractores
/admin/billing        → Resumen de facturación (futuro)
/admin/audit          → Log de auditoría global
```

---

## 8. Valor del Portal Más Allá de los Reportes

El portal justifica el precio mensual por sí solo gracias a:

| Feature | Valor para el cliente |
|---|---|
| Estado del extractor en tiempo real | "Sé cuándo se actualizaron mis datos" |
| Historial de actualizaciones | "Puedo auditar cuándo cambió un número" |
| Botón actualizar controlado | "No dependo de un horario fijo" |
| Soporte integrado | "Si algo falla, tengo un lugar donde reportarlo" |
| Branding propio | "Se ve como nuestro sistema, no como Power BI" |
| Roles y usuarios | "Controlo quién ve qué" |
| Widgets KPI en portal | "No necesito abrir el reporte para ver el número clave" |
| Última actualización visible | "Sé que el dato que veo es de hoy" |

---

## 9. Roadmap de Features del Portal

### Fase 1 — MVP (Semanas 1–8)
- [ ] Login con JWT + refresh token
- [ ] Branding por tenant (logo, colores)
- [ ] Catálogo de reportes por área
- [ ] Iframe de reporte Power BI (autenticado)
- [ ] Última actualización visible en cada reporte
- [ ] Estado del extractor (tabla básica)
- [ ] SuperAdmin: crear tenant, asignar reportes

### Fase 2 — Operacional (Semanas 9–20)
- [ ] Historial de actualizaciones (30 días)
- [ ] Botón "Actualizar ahora" (throttled)
- [ ] Gestión de usuarios por CompanyAdmin
- [ ] Widgets KPI en dashboard (3–5 métricas rápidas)
- [ ] Alertas email: extractor sin datos > 4h
- [ ] Exportación a Excel desde portal

### Fase 3 — Inteligencia Básica (Meses 6–12)
- [ ] Alertas configurables (ej. "ventas < X en el mes")
- [ ] Comparativas automáticas vs. período anterior
- [ ] Recomendaciones simples (reglas fijas)
- [ ] Notificaciones push/email por evento de negocio

### Fase 4 — Enterprise (Año 2+)
- [ ] Power BI Embedded (sin iframe, token propio)
- [ ] Chat analítico (RAG sobre PostgreSQL)
- [ ] S&OP básico (planificación de demanda)
- [ ] Multi-empresa en misma sesión
- [ ] Escritura controlada a SAP

---

## 10. Implementación Técnica del Subdomain Routing

### Backend: TenantMiddleware (ya implementado)

```csharp
// Lee Host header → resuelve company_id
// Excluye /api/ingest/* (ya implementado)
// Excluye /api/admin/* (SuperAdmin no requiere tenant)
```

### Frontend: App.tsx

```typescript
// Detecta subdominio → renderiza portal o admin
const subdomain = window.location.hostname.split('.')[0];
if (subdomain === 'admin') {
  return <AdminApp />;
} else if (subdomain !== 'databision' && subdomain !== 'www') {
  return <PortalApp tenant={subdomain} />;
} else {
  return <LandingPage />;
}
```

### DNS / Infraestructura

```
Opción A: Azure App Service con wildcard custom domain
  - databision.com + *.databision.com → mismo App Service
  - SSL: Azure App Service Managed Certificate (gratis para wildcards con plan Standard+)

Opción B: Vercel (más simple para React frontend)
  - Vercel soporta wildcard subdomains en plan Pro ($20/mes)
  - Backend .NET en Azure App Service separado

Recomendación MVP: Vercel (frontend) + Azure App Service (backend)
```

---

## 11. Consideraciones de Seguridad

1. **Aislamiento de datos:** Todo query al backend incluye `company_id` del JWT. El TenantMiddleware asegura que el JWT coincida con el subdominio.

2. **Branding:** El CSS de branding se aplica solo en el portal del tenant. No puede "contaminar" otro tenant.

3. **Iframe de Power BI:** El embed usa el enlace del workspace del cliente. Un usuario de otro tenant no puede acceder aunque conozca la URL.

4. **Tokens:** JWT incluye `company_id` y `company_slug`. Cualquier endpoint protegido valida ambos.
