# DataBision — Roadmap Post-Demo

Sprint 8Q — Junio 2026

Roadmap de producto orientado a conversaciones comerciales. Este documento muestra hacia dónde va DataBision y permite responder preguntas de roadmap con confianza durante y después de la demo.

---

## Estado actual (Junio 2026)

### Disponible en el pilot

| Característica | Estado |
|---|---|
| Extracción automática desde SAP Business One via Service Layer | ✅ Productivo |
| Pipeline de transformación y mart de datos | ✅ Productivo |
| 5 módulos de BI nativo: Ventas, Compras, Inventario, Finanzas, Operaciones | ✅ Productivo |
| Multi-tenancy: cada empresa tiene su propio portal y datos aislados | ✅ Productivo |
| Branding por tenant (logo, colores, nombre) | ✅ Productivo |
| Roles de usuario con acceso por módulo | ✅ Productivo |
| Health score del pipeline y alertas automáticas | ✅ Productivo |
| Calidad de datos automática | ✅ Productivo |
| Auth JWT con refresh tokens rotados (RS256) | ✅ Productivo |
| Panel de SuperAdmin para gestión de tenants | ✅ Productivo |

---

## Roadmap Q3 2026 (Julio–Septiembre)

### Prioridad Alta

**Módulo de alertas por email**
- Las alertas del módulo de Operaciones también se envían por email al equipo de KSDEPOR
- Configurable por tipo de alerta y destinatario
- Elimina la necesidad de revisar el portal para saber si hay un problema

**Drill-down en tablas**
- Click en un cliente en la tabla de Ventas → ver el detalle de facturas de ese cliente
- Click en un artículo → ver el historial de ventas
- Mejora la utilidad del portal para usuarios operativos (no solo gerencial)

**Filtros por categoría de artículo / familia de producto**
- En módulos de Ventas e Inventario, filtrar por grupo de artículo de SAP
- Para KSDEPOR: filtrar por categoría de deporte (fútbol, natación, etc.)

### Prioridad Media

**Módulo de comparación de períodos**
- Comparar ventas de este mes vs mismo mes del año anterior
- Comparar performance por trimestre
- KPIs con flechas de tendencia: ↑ 12% vs mes anterior

**Exportación a Excel/PDF**
- Cualquier tabla del portal exportable a Excel con un click
- Informes ejecutivos en PDF generados automáticamente

---

## Roadmap Q4 2026 (Octubre–Diciembre)

### Power BI como capa adicional

- Embedido de dashboards de Power BI dentro del mismo portal
- Los datos del pipeline de DataBision alimentan también los reportes de Power BI del cliente
- Las dos capas (BI nativo + Power BI) coexisten en el mismo portal con el mismo login

### Integración con otras fuentes de datos

- **E-commerce / Shopify / WooCommerce:** cruce de datos de ventas online con SAP
- **Google Sheets:** carga de datos de presupuesto y targets para comparar con real
- **CRM (Salesforce, HubSpot):** cruce de pipeline comercial con facturación de SAP

### Módulo de presupuesto vs real

- Carga de presupuesto de ventas mensual
- Comparación automática presupuesto vs real por vendedor, producto, región
- Semáforo de cumplimiento de cuota

---

## Roadmap 2027

### DataBision Analytics API

- API REST para que clientes avanzados puedan consumir los datos del mart directamente
- Útil para equipos de BI que quieren construir reportes propios sobre los datos de DataBision
- Documentada y con autenticación por API key

### Módulo de forecast

- Predicción de ventas para los próximos 30-90 días basada en histórico
- Modelos de machine learning entrenados con datos de SAP del cliente
- Alertas predictivas: "según tendencia, este cliente puede no cumplir objetivo de Q4"

### Marketplace de módulos

- Módulos adicionales creados por DataBision o por partners
- Clientes pueden activar módulos específicos según su industria
- Módulo de RR.HH., módulo de proyectos, módulo de producción (para manufactura)

---

## Notas para la conversación de roadmap

**Si preguntan "¿cuándo tendrán X?"**
> "El roadmap es orientativo. Las prioridades se ajustan según el feedback de clientes como KSDEPOR. Si X es importante para ustedes, eso acelera su prioridad."

**Si preguntan "¿están integrados con Y?"**
> "Las integraciones con sistemas distintos a SAP están en el roadmap Q4 2026. El primer paso es estabilizar el pipeline de SAP — que es donde está el 80% del valor para la mayoría de las empresas."

**Si preguntan sobre Power BI**
> "Power BI está planificado para Q4 2026 como capa adicional opcional. Si ya tienen dashboards de Power BI que quieren mantener, DataBision puede ser el pipeline de datos que los alimenta — sin que tengan que abandonar sus reportes actuales."

**Si preguntan sobre mobile**
> "El portal actual está optimizado para laptop/desktop — que es el contexto de uso de un gerente o analista. Una versión mobile optimizada para acceso rápido a KPIs ejecutivos está en evaluación para 2027."
