# Native BI Finance — Roadmap del Producto

**DataBision · Junio 2026**  
*Este roadmap es orientativo. Las fechas y el orden pueden cambiar según demanda de clientes y capacidad del equipo.*

---

## Estado actual — Finance MVP (✅ Disponible)

**Disponible desde junio 2026**

| Funcionalidad | Estado |
|---|---|
| Extracción OACT (plan de cuentas) | ✅ Disponible |
| Extracción OJDT + JDT1 (libro diario) | ✅ Disponible |
| Clasificación PCGE automatizada | ✅ Disponible |
| Estado de Resultados (P&L) | ✅ Disponible |
| Balance General | ✅ Disponible |
| EBITDA | ✅ Disponible |
| Flujo de Efectivo (básico) | ✅ Disponible |
| Refresh diario automatizado | ✅ Disponible |
| Scheduler Windows / Linux | ✅ Disponible |
| Multi-tenant por company_id | ✅ Disponible |
| refresh-status con healthScore | ✅ Disponible |
| Perfil de conexión por cliente | ✅ Disponible |
| SecretRef para credenciales SAP | ✅ Disponible |

**Evidencia de validación técnica:** TST ksdepor / CLTSTKSDEPOR — healthScore 100, 7/7 endpoints HTTP 200.

---

## Q3 2026 — Módulo Compras

**Objetivo:** Conectar las órdenes de compra y facturas de proveedores con el análisis financiero.

| Funcionalidad | Descripción |
|---|---|
| Extracción OPOR (Purchase Orders) | Órdenes de compra por proveedor |
| Extracción OPCH (AP Invoices) | Facturas de proveedores con vencimientos |
| Dashboard Compras | Top proveedores, tendencia de compras, cumplimiento |
| Aging de cuentas por pagar | Vencimientos próximos y vencidos |
| Integración con P&L | Compras como componente del COGS |

**Prerequisito:** Módulo Finance MVP activo y estable.

---

## Q3 2026 — Módulo Inventario

**Objetivo:** Visibilidad de stock, rotación y valorización desde SAP B1.

| Funcionalidad | Descripción |
|---|---|
| Extracción OITM (Items) | Maestro de artículos |
| Extracción OIBT (Inventory Tracking) | Movimientos de inventario |
| Dashboard Inventario | Stock actual, rotación, ítems sin movimiento |
| Valorización de inventario | FIFO / Costo promedio según config SAP |
| Integración con P&L | Variación de inventario en el margen |

---

## Q4 2026 — Módulo Ventas

**Objetivo:** Pipeline, facturación y cobranza desde SAP B1.

| Funcionalidad | Descripción |
|---|---|
| Extracción ORDR (Sales Orders) | Órdenes de venta |
| Extracción OINV (AR Invoices) | Facturas emitidas |
| Dashboard Ventas | Top clientes, tendencia de ventas, canal |
| Aging de cuentas por cobrar | Vencimientos próximos y vencidos |
| Integración con P&L | Ingresos con detalle por cliente/producto |

---

## Q4 2026 — Alertas inteligentes

**Objetivo:** Notificaciones proactivas basadas en umbrales financieros.

| Funcionalidad | Descripción |
|---|---|
| Alerta margen bruto bajo umbral | Email/WhatsApp cuando margen < X% |
| Alerta EBITDA negativo | Notificación automática |
| Alerta cuentas por cobrar vencidas | Aging > 30/60/90 días |
| Alerta cuentas sin clasificar | Cuando OACT trae cuentas nuevas |
| Alerta extractor inactivo | Si el refresh no corrió en 25 horas |
| Configuración por cliente | Cada cliente define sus umbrales |

---

## Q1 2027 — Agente Financiero IA

**Objetivo:** Consultar los datos financieros en lenguaje natural.

| Funcionalidad | Descripción |
|---|---|
| Chat sobre P&L | "¿Cómo fue el margen de enero comparado con enero anterior?" |
| Análisis de tendencias | "¿Qué gastos crecieron más en el último trimestre?" |
| Comparación de períodos | "Muéstrame el EBITDA de los últimos 6 meses" |
| Alertas explicadas | "¿Por qué bajó el margen este mes?" |
| Contexto SAP nativo | El agente entiende la estructura contable del cliente |

Basado en Claude API (Anthropic) con contexto del MART financiero del cliente.

---

## Q1 2027 — Export Power BI

**Objetivo:** Permitir que los clientes que ya usan Power BI consuman los datos de DataBision.

| Funcionalidad | Descripción |
|---|---|
| API dataset para Power BI | Endpoint compatible con Power BI dataflows |
| Refresh automático en Power BI | Trigger desde el extractor DataBision |
| Plantilla .pbix | Template de P&L/Balance/EBITDA listo para importar |
| Documentación de integración | Guía paso a paso para el equipo TI del cliente |

---

## Q2 2027 — Multiempresa

**Objetivo:** Un solo dashboard que consolida múltiples empresas SAP B1 del mismo grupo.

| Funcionalidad | Descripción |
|---|---|
| Consolidación de P&L | Suma de ingresos/gastos de N empresas SAP |
| Balance consolidado | Eliminación de intercompañías (manual) |
| Selector de empresa | Ver consolidado o individual |
| Múltiples profiles SAP | Un extractor puede manejar N empresas |

---

## Q2 2027 — Azure Key Vault

**Objetivo:** Almacenamiento seguro de credenciales SAP en Azure Key Vault para clientes Enterprise.

| Funcionalidad | Descripción |
|---|---|
| SecretRef `azure-kv://` | Soporte completo de prefix azure-kv |
| Rotación automática de secrets | Azure Key Vault rotation policy |
| Auditoría de acceso a secrets | Log completo en Azure Monitor |
| Sin credenciales en variables de entorno | Migración total a Key Vault para Enterprise |

---

## Q3 2027 — Scheduler administrable

**Objetivo:** El cliente puede configurar el horario de refresh sin depender de DataBision.

| Funcionalidad | Descripción |
|---|---|
| UI de scheduler en admin panel | Configurar hora de OACT, OJDT, MART |
| Historial de ejecuciones | Ver últimas N ejecuciones con estado |
| Retry desde UI | Reintentar un proceso fallido desde el panel |
| Notificaciones configurables | Email/WhatsApp al terminar o fallar |
| Logs accesibles | Ver logs del extractor desde el panel |

---

## Funcionalidades en evaluación (sin fecha)

- **Certificado SSL automático:** Gestión automática de SSL para Service Layer autofirmados
- **Extractor cloud-hosted:** Extractor en cloud de DataBision sin instalar nada en el cliente
- **Connectors alternativos:** SAP Cloud ERP, SAP S/4HANA, Sage Contabilidad
- **Reportes PDF:** Generación de P&L y Balance en PDF para cierre mensual
- **Firma digital de reportes:** P&L firmado digitalmente por el contador

---

## Cómo influir en el roadmap

Los clientes con plan Professional o Enterprise tienen acceso prioritario para:
- Votar por funcionalidades del roadmap
- Participar en beta de módulos nuevos antes del lanzamiento oficial
- Sugerir nuevas funcionalidades directamente al equipo DataBision

Contacto para feedback de producto: campillayparedes@gmail.com
