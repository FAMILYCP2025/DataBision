# DataBision — Propuesta Técnica Resumida para Cliente

**Versión:** 1.0 — Sprint 8L — Junio 2026  
**Contexto:** SAP Business One HANA sobre Linux — Cliente piloto KSDEPOR

---

## 1. Qué Problema Resuelve

Las empresas con SAP Business One tienen sus datos operacionales en SAP, pero extraerlos para análisis requiere:
- Exportaciones manuales a Excel.
- Reportes nativos de SAP que no son visuales ni dinámicos.
- Dependencia de consultor SAP para cada reporte nuevo.
- Datos desactualizados porque la exportación es puntual.

DataBision resuelve esto conectando SAP B1 directamente a un portal web analítico, con datos actualizados automáticamente y dashboards listos para gerencia y equipo operativo.

---

## 2. Flujo de Negocio Cubierto

| Proceso | Indicadores |
|---|---|
| **Ventas** | Ventas netas/brutas, facturas, ticket promedio, ranking clientes/productos/vendedores, tasa de fulfillment |
| **Compras** | Órdenes de compra, montos, proveedores activos, recepciones de mercadería |
| **Inventario** | Rotación de ítems (FAST/NORMAL/SLOW/NO_MOVEMENT), stock por almacén |
| **Finanzas** | AR aging por cliente, % vencido, AP aging por proveedor |
| **Operaciones** | Estado del pipeline de datos, alertas, calidad de datos SAP |

---

## 3. Arquitectura Recomendada

```
SAP Business One (HANA/Linux)
        ↓
   SAP Service Layer (API REST oficial)
        ↓
   DataBision Extractor (.NET 8 — Windows Service o CLI)
        ↓
   PostgreSQL / Supabase (schemas: raw → stg → mart)
        ↓
   DataBision API (.NET 8 — REST)
        ↓
   DataBision Portal (React + TypeScript — browser)
```

**Principios de diseño:**
- SAP no se modifica. Solo lectura vía Service Layer.
- Extracción incremental: solo registros nuevos o modificados desde el último run.
- Capas separadas: raw (dato crudo), stg (dato limpio), mart (indicador calculado).
- Multiempresa: cada cliente tiene su tenant aislado.

---

## 4. Stack Tecnológico

| Capa | Tecnología |
|---|---|
| Fuente de datos | SAP Business One HANA (Linux) |
| API SAP | SAP Service Layer v1 (REST/OData) |
| Extractor | .NET 8 (Windows Service / CLI) |
| Base de datos | PostgreSQL vía Supabase (cloud o self-hosted) |
| API backend | ASP.NET Core 8 (REST, JWT RS256) |
| Frontend | React 18 + TypeScript + Vite + TanStack Query |
| Autenticación | JWT (access token 15 min) + refresh token httpOnly (7 días) |
| Multitenancy | Subdominio por empresa (`{slug}.databision.app`) |

---

## 5. Objetos SAP Involucrados (KSDEPOR)

| Objeto SAP | Descripción | Estado |
|---|---|---|
| OSLP | Vendedores | ✅ Activo |
| OCRD | Clientes y proveedores | ✅ Activo |
| OITM | Maestro de ítems | ✅ Activo |
| OINV | Facturas de venta | ✅ Activo |
| ORIN | Notas de crédito de venta | ✅ Activo |
| OPOR | Órdenes de compra | ✅ Activo |
| OPDN | Recepciones de mercadería | ✅ Activo |
| OPCH | Facturas de proveedor | ✅ Activo |
| ORDR | Órdenes de venta | ✅ Activo |
| ODLN | Entregas | ✅ Activo |
| OWTR | Transferencias de stock | ✅ Activo |
| OITW | Stock por almacén | ⚠️ Preparado, no activo (endpoint SAP no reconocido aún) |

---

## 6. Flujo de Datos

```
1. Extractor lee objetos SAP vía Service Layer (paginación controlada)
2. Guarda datos crudos en schema RAW (sin transformar)
3. Transform STG: limpia, normaliza y aplica reglas de calidad
4. Transform MART: calcula KPIs y agrega en tablas de dashboards
5. API sirve datos del MART al frontend
6. Frontend muestra dashboards por proceso con paginación, filtros y ordenamiento
```

Frecuencia recomendada: extracción cada 4–8 horas. Transform inmediato post-extracción.

---

## 7. Seguridad

- Autenticación JWT RS256 (clave asimétrica — la clave privada nunca sale del servidor).
- Refresh tokens almacenados hasheados en DB, rotados en cada uso.
- Multi-tenancy por subdominio: el JWT incluye `company_id` y `company_slug`, verificados en cada request.
- Service Layer: solo lectura. El extractor nunca escribe en SAP.
- Sin acceso directo a la base HANA de SAP: toda comunicación vía Service Layer.
- Separación de datos entre empresas garantizada a nivel de query (filtro `company_id` explícito en toda consulta).

---

## 8. Logs y Monitoreo

| Log | Contenido |
|---|---|
| `ops.extractor_run` | Cada ejecución de extractor: objeto, estado, páginas, filas, tiempo |
| `ops.extractor_page_log` | Cada página paginada: número de página, filas recibidas, ms, status |
| `ops.transform_run` | Cada transform STG/MART: objeto, estado, filas insertadas/actualizadas, ms |
| `ops.alert_event` | Alertas disparadas por reglas configuradas |
| `ops.data_quality_issue` | Problemas de calidad detectados en datos SAP |
| `ops.pipeline_health` | Snapshot de salud: health score, extractor status, transform status |

Todo visible en el dashboard de Operaciones del portal.

---

## 9. Riesgos

| Riesgo | Mitigación |
|---|---|
| Service Layer no disponible | Extractor reintenta con backoff; datos previos siguen disponibles en MART |
| Volumen de datos mayor al esperado | Paginación controlada (50–100 filas/página); extracción incremental |
| Cambios en objetos SAP | Objetos estándar de SAP B1; impacto bajo. Catálogo de objetos en `cfg.sap_object_catalog` |
| Latencia de Supabase | Queries indexados; datos sirven desde MART (tablas agregadas, no raw) |
| Datos inconsistentes en SAP | Calidad de datos detectada en STG y reportada en OPS. No bloquea la demo |

---

## 10. MVP Recomendado

**Para primer cliente productivo (3–4 semanas desde firma):**

1. Configurar ambiente producción (Supabase cloud, API en servidor cliente o cloud).
2. Extracción controlada de objetos prioritarios (OINV, OPOR, OITM, OCRD).
3. Dashboards de Ventas y Compras.
4. 1 usuario CompanyAdmin + 3 usuarios lectura.
5. Extracción programada diaria (madrugada).
6. Acceso vía subdominio `{cliente}.databision.app`.

---

## 11. Versión Vendible Actual

**DataBision v0.9 — Demo-Ready**

Incluye:
- Portal web con 5 módulos analíticos (Ventas, Compras, Inventario, Finanzas, Operaciones).
- Conexión real a SAP B1 vía Service Layer.
- Extracción de 11 objetos SAP.
- 18 tablas MART pobladas.
- Logs completos de trazabilidad.
- Multi-tenancy funcional.
- Autenticación segura.

No incluye aún:
- Power BI embedded (en roadmap).
- Alertas por email/Slack (en roadmap).
- Dashboard de Alertas por WhatsApp (en roadmap).
- AP Aging completo (datos pendientes).
- Stock por almacén vía OITW (endpoint SAP pendiente).

---

## 12. Próximos Pasos

| Paso | Responsable | Plazo sugerido |
|---|---|---|
| Reunión técnica con TI cliente | DataBision + TI KSDEPOR | Semana 1 |
| Verificar Service Layer en producción | Consultor SAP | Semana 1 |
| Definir objetos SAP prioritarios | Gerencia cliente | Semana 1 |
| Configurar ambiente producción | DataBision | Semana 2 |
| Extracción controlada en producción | DataBision + TI | Semana 2–3 |
| Capacitación usuarios | DataBision | Semana 3–4 |
| Go-live | Todos | Semana 4 |

**Modelo comercial sugerido:** SaaS mensual por empresa. Precio depende de número de usuarios y objetos SAP activos.
