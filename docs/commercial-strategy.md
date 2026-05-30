# DataBision — Commercial Strategy

**Versión:** 1.0  
**Fecha:** 2026-05-29  
**Estado:** Documento vivo — referencia comercial activa  
**Relacionado con:** [ADR-005 Pricing](adr/ADR-005-pricing-model.md) · [master-architecture.md](master-architecture.md) · [native-bi-architecture.md](native-bi-architecture.md)

---

## 1. Posicionamiento

### Qué es DataBision

DataBision es una **plataforma SaaS de inteligencia operacional para empresas que usan SAP Business One**.

No entrega solo reportes. Entrega:

- Extracción automática y continua de los datos SAP B1 del cliente.
- Una base de datos intermedia limpia, auditable y siempre actualizada.
- Dashboards ejecutivos y operacionales con BI nativo propio (React + ECharts).
- Visibilidad del estado de sincronización: el cliente siempre sabe qué tan fresco es su dato.
- Alertas proactivas por condición de negocio (stock crítico, mora vencida, margen caído).
- Recomendaciones accionables: qué reponer, a quién cobrar, qué producto empujar.
- Acciones de negocio desde el portal: decisiones sin entrar a SAP.
- Portal propio por empresa con subdominio `{slug}.databision.app` y branding white-label.

### Qué NO es DataBision

| No es | Por qué importa aclararlo |
|---|---|
| Power BI | No requiere licencias Microsoft, no depende de Service Principal ni de A-SKU Embedded |
| Superset / Metabase | No es self-service BI para analistas técnicos |
| Reportería genérica | Los módulos están construidos sobre el modelo de datos específico de SAP B1 |
| Un consultor de BI | No cobra por hora ni por entrega puntual |
| Crystal Reports cloud | No es solo replicar lo que ya tiene el cliente en SAP |
| Un conector de datos | No vende el tubo: vende la inteligencia construida sobre el tubo |

### Diferencia frente a alternativas

| Solución | Qué da | Qué no da |
|---|---|---|
| **Crystal Reports en SAP B1** | Reportes dentro del ERP | Sin acceso web, sin móvil, sin multi-empresa, sin alertas |
| **Excel ad-hoc** | Flexibilidad total | Manual, desactualizado, silo por persona |
| **Power BI interno** | Visualizaciones potentes | Requiere licencias, IT propio, mantenimiento, alguien que lo construya |
| **Consultoría BI** | Reporte a medida | Tarda semanas, cobra cada vez, no opera solo |
| **DataBision** | Ecosistema completo operando solo | Requiere conexión a SAP; no es analytics ad-hoc sin estructura |

### Mensaje principal para gerencia

> "Todos los datos de tu SAP Business One, actualizados automáticamente, accesibles desde cualquier dispositivo, con alertas cuando algo sale mal y recomendaciones de qué hacer. Sin instalar nada, sin depender de IT, sin pagar a un consultor cada vez que necesitas un número."

**Versión de una frase:**
> DataBision convierte SAP Business One en inteligencia operacional en tiempo real.

---

## 2. Buyer Personas

### Gerente General

**Rol:** Toma decisiones estratégicas. Raramente entra a SAP. Pide números a su equipo.

**Dolor:** Cuando necesita saber cómo va la empresa, llama a alguien. Ese alguien saca un Excel que tiene datos de ayer (o de antes de ayer). Las decisiones se toman sobre datos incompletos.

**Lo que quiere de DataBision:** Un dashboard que cargue en segundos, que muestre ventas del mes, margen, cobranza vencida y stock crítico. Sin login a SAP. Desde su celular si puede.

**Cómo venderle:** "Accedes en 30 segundos a los números que hoy te tardan 30 minutos en conseguir."

---

### Gerente Comercial

**Rol:** Maneja vendedores, clientes, pipeline de ventas.

**Dolor:** No sabe en tiempo real cuánto vendió cada vendedor esta semana. Descubre que un cliente importante dejó de comprar cuando ya es tarde. No puede ver el histórico de un cliente sin ayuda de TI.

**Lo que quiere de DataBision:** Ranking de vendedores actualizado, alertas de clientes sin compra reciente, vista de cartera por ejecutivo, evolución de pedidos por cliente.

**Cómo venderle:** "Tu equipo de ventas operando con datos del mismo día, no del mes pasado."

---

### Gerente de Operaciones

**Rol:** Inventario, logística, producción, cumplimiento de pedidos.

**Dolor:** El stock crítico lo descubre cuando el cliente llama a reclamar. Las órdenes atrasadas las ve en SAP pero no tiene una vista consolidada. Compras sin visibilidad del ritmo de reposición.

**Lo que quiere de DataBision:** Alertas de stock bajo punto de reorden, estado de pedidos pendientes, rotación por ítem, sugerencia de reposición.

**Cómo venderle:** "Alertas antes de que el problema llegue al cliente."

---

### Jefe de TI

**Rol:** Mantiene la infraestructura SAP, gestiona partners, toma decisiones técnicas de integración.

**Dolor:** Le piden más reportes de los que puede construir. El extractor instalado en el servidor es una caja negra que no puede monitorear. Si algo falla, no hay alertas.

**Motivación:** Que DataBision opere solo, sin que él tenga que mantenerlo. Que haya logs. Que el dato sea confiable.

**Preocupación:** Seguridad. Qué datos salen del servidor. Quién tiene acceso. Cómo se instala.

**Cómo venderle:** "El extractor usa un usuario SAP de solo lectura, todo viaja sobre HTTPS, los logs son auditables y hay monitoreo de heartbeat en tiempo real. TI recibe alertas si algo falla, no los usuarios."

---

### Controller / Finanzas

**Rol:** Cierre contable, flujo de caja, cobranza, presupuesto vs real.

**Dolor:** Depende del contador para exportar el estado de cuentas por cobrar. El cierre de mes requiere consolidar varias hojas Excel de distintas personas. La cobranza vencida se sabe tarde.

**Lo que quiere de DataBision:** Vista de deuda por cliente, alertas de mora vencida, evolución de cuentas por cobrar, comparativo mes vs mes.

**Cómo venderle:** "Cobranza vencida visible todos los días, no solo al final del mes."

---

### Partner SAP B1

**Rol:** Implementador y soporte del ERP del cliente. Tiene relación de confianza con el Gerente de TI y, a veces, con la Gerencia General.

**Motivación:** Ampliar el valor entregado al cliente sin hacer desarrollo a medida. Cobrar una comisión por referido o por acompañar el onboarding.

**Preocupación:** Que DataBision rompa algo en SAP. Que sea una competencia directa a sus servicios. Que no pueda controlar la calidad.

**Cómo venderle:** "DataBision es complementario a tu servicio: usamos solo lectura, nunca tocamos lógica de negocio de SAP, y tú quedas como el referente técnico de la cuenta. Hay un programa de partners con comisión."

---

## 3. Dolor por Perfil

### Falta de visibilidad en tiempo real

El gerente pregunta "¿cómo vamos este mes?" y la respuesta llega dos horas después en un Excel. DataBision resuelve esto con dashboards que se actualizan automáticamente desde SAP.

### Dependencia de Excel

Cada área tiene su "Excel de la verdad". Cuando el Controller y el Gerente Comercial tienen números distintos, alguien pierde tiempo reconciliando. DataBision es la fuente única, derivada directamente de SAP.

### Datos atrasados

Crystal Reports, los reportes estándar de SAP y los Excel manuales tienen datos de ayer o de antes. DataBision actualiza cada 30–120 minutos según el plan. El Operational Live Layer accede en tiempo casi real para datos críticos.

### Gerentes que no entran a SAP

Las licencias SAP profesional cuestan USD 1.500–3.000/usuario/año. No todas las empresas tienen una para cada gerente. DataBision entrega los datos sin necesidad de licencia SAP adicional.

### Integraciones poco monitoreadas

El extractor falla. La conexión al SAP del partner cloud se cae. El proceso de sincronización se detiene. Nadie lo sabe hasta que el cliente llama. DataBision tiene Sync Center: el cliente ve el estado de cada tabla, cuándo fue la última actualización y si hubo errores.

### Falta de alertas

Un cliente importante lleva 60 días sin comprar. El stock de un producto A está en cero. Una factura lleva 90 días vencida. Sin alertas, estas situaciones se descubren cuando ya hay un problema. DataBision tiene Alert Center configurable por umbral y condición.

### Decisiones reactivas

El equipo reacciona a los problemas después de que ocurren. DataBision introduce recomendaciones proactivas: "Reponer ítem X en bodega Y", "Llamar a cliente Z que no compra hace 45 días", "Revisar margen de línea de producto W que cayó 8 puntos."

---

## 4. Planes Comerciales

### Decisión: Business a USD 600/mes (no USD 550)

**Hipótesis validada:** USD 600 es el precio correcto para Business. Razonamiento:

1. **Diferencia psicológica irrelevante en B2B:** En una decisión de compra B2B de ERP reporting, USD 50/mes de diferencia ($600/año) no cambia la decisión. El decisor evalúa ROI, no si el precio termina en 50 o en 00.

2. **Margen:** USD 600 da ~USD 30.000/año por cliente. A 10 clientes Business son USD 300.000 ARR. USD 550 son USD 275.000. La diferencia de USD 25.000/año importa en una startup.

3. **Señal de valor:** USD 550 suena a "casi 500". USD 600 ancla mejor como eslabón entre Starter (350) y Advanced (1.000+). La escalera 350 → 600 → 1.000+ es más limpia que 350 → 550 → 1.000+.

4. **Flexibilidad comercial:** Con precio de USD 600, puedes ofrecer USD 500–550 como descuento de lanzamiento a los primeros clientes, o como precio especial para partners. Con USD 550 de precio base, no tienes dónde bajar sin perder credibilidad.

5. **Mercado LATAM:** El mercado SaaS B2B en Chile y LATAM está acostumbrado a precios en USD con precios "redondos". USD 600 es un precio reconocible; USD 550 no agrega nada.

**Recomendación: Business = USD 600/mes.**

---

### Plan Starter — USD 350/mes

**Cliente ideal:** PyME con SAP B1, 10–50 empleados, 1 empresa, sin equipo de BI, que quiere dejar Excel de ventas y tener visibilidad básica.

**Setup:** USD 990 (pago único)

| Componente | Detalle |
|---|---|
| **Empresas SAP** | 1 |
| **Frecuencia de actualización** | Cada 2 horas (Dedicated Extractor) / cada hora (Service Layer) |
| **Modalidad de extracción** | Service Layer Delta o Dedicated Extractor |
| **Usuarios en portal** | Hasta 5 |
| **Módulo ventas** | Sí — KPIs básicos: ventas mes, comparativo mes anterior, top 10 clientes |
| **Módulo clientes** | Sí — Listado, historial de compras básico |
| **Módulo productos** | Sí — Stock actual, top 10 ítems más vendidos |
| **Sync Center** | Sí — Estado de sincronización por tabla |
| **Operational Cockpit** | No |
| **Operational Live Layer** | No |
| **Alert Center** | Básico — 2 alertas predefinidas (stock bajo mínimo, cobranza vencida >90 días) |
| **Recomendaciones** | No |
| **Business Actions** | No |
| **Branding** | Logo + colores primarios del cliente |
| **Soporte** | Email, respuesta en 48h hábiles |
| **SLA uptime** | 99% mensual (best effort en primeros 90 días) |
| **Historial de datos** | 12 meses |
| **Exportación** | No |

**Add-ons disponibles:**
- Usuario adicional: USD 25/mes/usuario
- Empresa SAP adicional: USD 200/mes

**Costos internos estimados:**
| Servicio | Costo estimado |
|---|---|
| Supabase (plan compartido, fracción por tenant) | USD 12 |
| Hosting API + portal (fracción) | USD 15 |
| Email transaccional (Resend/Postmark) | USD 3 |
| DNS / subdominio | USD 2 |
| Soporte operativo estimado (horas/mes) | USD 20 |
| Monitoreo y alertas internas | USD 5 |
| **Total** | **~USD 57** |

**Margen bruto estimado:** USD 293/mes — **84%**

**Lo que NO incluye:**
- Alertas personalizadas
- Módulos de inventario avanzado, cobranza, márgenes
- Vistas configurables
- SLA garantizado
- Videoconferencias de soporte

---

### Plan Business — USD 600/mes

**Cliente ideal:** Empresa con SAP B1 de 30–150 empleados, con Gerente Comercial y Controller que necesitan visibilidad operativa semanal, y posiblemente 2 empresas en el mismo grupo.

**Setup:** USD 1.490 (pago único)

| Componente | Detalle |
|---|---|
| **Empresas SAP** | Hasta 2 |
| **Frecuencia de actualización** | Cada hora (Dedicated Extractor) / cada 30–45 min (Service Layer) |
| **Modalidad de extracción** | Cualquiera (A, B o C) |
| **Usuarios en portal** | Hasta 15 |
| **Módulo ventas** | Sí — completo: tendencia, por vendedor, por zona, por familia de producto |
| **Módulo clientes** | Sí — Segmentación, historial, clientes sin compra reciente |
| **Módulo productos** | Sí — Rotación, márgenes por ítem |
| **Módulo cobranza** | Sí — Cartera vencida por cliente, días promedio de pago |
| **Sync Center** | Sí — con historial de sincronizaciones |
| **Operational Cockpit** | Sí |
| **Operational Live Layer** | Sí — hasta 3 vistas configuradas por DataBision |
| **Alert Center** | Sí — hasta 10 alertas activas (predefinidas + 3 custom básicas) |
| **Recomendaciones** | Sí — básicas (reposición de stock, clientes en riesgo de fuga) |
| **Business Actions** | No (en roadmap) |
| **Branding** | Logo, colores, favicon, mensaje de bienvenida, dominio propio opcional |
| **Soporte** | Email 24h hábiles + videoconferencia mensual de revisión |
| **SLA uptime** | 99% mensual |
| **Historial de datos** | 24 meses |
| **Exportación** | Sí — Excel por módulo |

**Add-ons disponibles:**
- Usuario adicional: USD 25/mes/usuario
- Empresa SAP adicional: USD 200/mes
- Vista Live adicional: USD 75/mes
- Alerta personalizada adicional: USD 50/mes (lote de 5)
- Histórico extendido (+12 meses): USD 50/mes

**Costos internos estimados:**
| Servicio | Costo estimado |
|---|---|
| Supabase (mayor volumen por tenant) | USD 20 |
| Hosting API + portal | USD 20 |
| Email transaccional | USD 5 |
| DNS / subdominio | USD 2 |
| Soporte operativo estimado | USD 35 |
| Monitoreo | USD 8 |
| **Total** | **~USD 90** |

**Margen bruto estimado:** USD 510/mes — **85%**

**Lo que NO incluye:**
- Reglas de alerta completamente custom
- Módulo de inventario avanzado con lotes y series
- Recomendaciones de reposición con cálculo de lead time
- Soporte dedicado / SLA 4h
- Business Actions desde portal

---

### Plan Advanced — USD 1.000/mes (precio base, a confirmar por propuesta)

**Cliente ideal:** Holding o grupo empresarial con 2–5 empresas SAP B1, Gerente de Operaciones que necesita inventario y producción, Controller que consolida multi-empresa, o empresa con requisitos específicos.

**Setup:** USD 2.490 (pago único, puede subir según complejidad)

| Componente | Detalle |
|---|---|
| **Empresas SAP** | Hasta 5 |
| **Frecuencia de actualización** | 30 minutos (Dedicated Extractor) / 15 min para tablas críticas |
| **Modalidad de extracción** | Cualquiera — modalidad mixta posible (A para on-prem, B para cloud) |
| **Usuarios en portal** | Hasta 50 |
| **Módulo ventas** | Completo + consolidado multi-empresa |
| **Módulo clientes** | Completo + segmentación por criterio custom |
| **Módulo productos** | Completo + lotes, series, bodegas múltiples |
| **Módulo cobranza** | Completo + aging report + proyección de flujo |
| **Módulo inventario avanzado** | Sí — reposición, punto de quiebre, valorización |
| **Módulo márgenes** | Sí |
| **Sync Center** | Sí — completo con audit log exportable |
| **Operational Cockpit** | Sí — completo |
| **Operational Live Layer** | Sí — vistas configurables ilimitadas (razonable) |
| **Alert Center** | Sí — alertas ilimitadas, condiciones custom |
| **Recomendaciones** | Sí — completas: reposición, cobranza, fuga de clientes, márgenes |
| **Business Actions** | Sí — cuando esté disponible en roadmap |
| **Branding** | White-label completo |
| **Soporte** | Dedicado — SLA 4h hábiles, canal directo |
| **SLA uptime** | 99.5% mensual |
| **Historial de datos** | 36 meses |
| **Exportación** | Sí — Excel + CSV + API de lectura |

**Add-ons disponibles:**
- Empresa SAP adicional: USD 200/mes (hasta 10 total)
- Módulo custom a cotizar
- Integración WhatsApp/email para alertas: USD 100/mes
- Escritura controlada a SAP (Business Actions con escritura): a cotizar
- Recomendaciones avanzadas con ML: roadmap

**Costos internos estimados:**
| Servicio | Costo estimado |
|---|---|
| Supabase (volumen alto) | USD 40 |
| Hosting API + portal (mayor escala) | USD 35 |
| Email transaccional | USD 8 |
| DNS / dominios múltiples | USD 5 |
| Soporte dedicado (horas/mes) | USD 100 |
| Monitoreo avanzado | USD 15 |
| **Total** | **~USD 203** |

**Margen bruto estimado:** USD 797/mes — **80%**

---

### Plan Enterprise — A medida (≥ USD 2.000/mes)

**Cliente ideal:** Grupo con >5 empresas SAP, requisitos de compliance, integración con sistemas adicionales, SLA 24/7, base SAP compleja.

**Pricing:** Cotización por propuesta. Variables: número de empresas SAP, volumen de filas/mes, SLA requerido, módulos custom, integraciones externas.

**Setup:** A cotizar (puede superar USD 5.000 según complejidad).

**Incluye todo Advanced más:**
- Azure SQL como base de datos (compliance, integración con Azure Synapse / Fabric)
- SLA 24/7 con on-call
- Contrato de confidencialidad reforzado
- Auditoría de seguridad compartida
- Desarrollo de módulos custom bajo contrato
- Capacitación y onboarding extendido

---

## 5. Contenido de Módulos por Plan

### Módulo: Native BI Ventas

| Feature | Starter | Business | Advanced |
|---|---|---|---|
| KPIs del mes actual (ventas, ticket promedio, unidades) | ✅ | ✅ | ✅ |
| Comparativo mes anterior y año anterior | ✅ | ✅ | ✅ |
| Ranking top 10 clientes | ✅ | ✅ | ✅ |
| Ventas por vendedor | — | ✅ | ✅ |
| Ventas por zona/región | — | ✅ | ✅ |
| Ventas por familia de producto | — | ✅ | ✅ |
| Tendencia diaria/semanal con drill-down | — | ✅ | ✅ |
| Consolidado multi-empresa | — | — | ✅ |
| Exportación Excel | — | ✅ | ✅ |

### Módulo: Native BI Clientes

| Feature | Starter | Business | Advanced |
|---|---|---|---|
| Listado de clientes activos | ✅ | ✅ | ✅ |
| Historial de compras por cliente (últimos 3 meses) | ✅ | ✅ | ✅ |
| Historial extendido + tendencia | — | ✅ | ✅ |
| Clientes sin compra reciente (>N días configurable) | — | ✅ | ✅ |
| Segmentación por criterio custom | — | — | ✅ |
| Score de riesgo de fuga | — | — | ✅ |

### Módulo: Native BI Inventario y Productos

| Feature | Starter | Business | Advanced |
|---|---|---|---|
| Stock actual por ítem | ✅ | ✅ | ✅ |
| Top 10 ítems más vendidos | ✅ | ✅ | ✅ |
| Rotación por ítem | — | ✅ | ✅ |
| Margen por ítem | — | ✅ | ✅ |
| Stock por bodega (multi-bodega) | — | — | ✅ |
| Control de lotes y series | — | — | ✅ |
| Punto de reorden y sugerencia de reposición | — | — | ✅ |
| Valorización de inventario | — | — | ✅ |

### Módulo: Cobranza

| Feature | Starter | Business | Advanced |
|---|---|---|---|
| Alertas de cobranza >90 días (predefinida) | ✅ | ✅ | ✅ |
| Vista de cartera vencida por cliente | — | ✅ | ✅ |
| Aging report (0–30, 31–60, 61–90, 90+) | — | ✅ | ✅ |
| Días promedio de pago por cliente | — | ✅ | ✅ |
| Proyección de flujo de cobranza | — | — | ✅ |

### Módulo: Operational Cockpit

| Feature | Starter | Business | Advanced |
|---|---|---|---|
| Dashboard de estado operacional del día | — | ✅ | ✅ |
| KPIs operacionales en tiempo casi real | — | ✅ | ✅ |
| Vista consolidada multi-empresa | — | — | ✅ |

### Módulo: Operational Live Layer

| Feature | Starter | Business | Advanced |
|---|---|---|---|
| Acceso directo a SAP (datos de minutos) | — | Limitado (3 vistas) | Ilimitado |
| Configuración custom de vistas live | — | — | ✅ |

### Módulo: Alert Center

| Feature | Starter | Business | Advanced |
|---|---|---|---|
| Alertas predefinidas activas | 2 | 10 | Ilimitadas |
| Alertas custom | — | 3 | Ilimitadas |
| Delivery vía email | ✅ | ✅ | ✅ |
| Delivery vía WhatsApp | — | — | Add-on |
| Silencio programado / horario | — | ✅ | ✅ |

### Módulo: Recommendations / Insights

| Feature | Starter | Business | Advanced |
|---|---|---|---|
| Recomendaciones activas | — | Básicas | Completas |
| Reposición de stock sugerida | — | ✅ | ✅ con lead time |
| Clientes en riesgo de fuga | — | ✅ | ✅ con scoring |
| Caída de margen por producto | — | — | ✅ |
| Reglas custom de recomendación | — | — | ✅ |

### Sync Center (todos los planes)

| Feature | Starter | Business | Advanced |
|---|---|---|---|
| Estado de sincronización por tabla | ✅ | ✅ | ✅ |
| Última actualización por tabla | ✅ | ✅ | ✅ |
| Historial de sincronizaciones | Últimas 24h | Últimos 7 días | Últimos 30 días |
| Alertas internas si extractor falla | ✅ | ✅ | ✅ |
| Audit log exportable | — | — | ✅ |

---

## 6. Pricing de Setup

El setup es un pago único que cubre el trabajo de onboarding técnico y no es negociable. Incluye trabajo real que no puede absorberse en la mensualidad.

### Setup Starter — USD 990

| Actividad | Descripción |
|---|---|
| Instalación Dedicated Extractor | Instalación del servicio Windows en servidor del cliente, o configuración Service Layer |
| Configuración de conexión SAP | Usuario de solo lectura, tablas MVP (7 tablas), prueba de conectividad |
| Carga inicial histórica | Hasta 500.000 filas por tabla (últimos 12 meses de documentos) |
| Validación de datos | Reconciliación count + montos vs SAP (±0 tolerancia) |
| Configuración branding | Logo + colores en portal |
| Activación de portal | Subdominio `{slug}.databision.app`, acceso para 5 usuarios |
| Capacitación básica | 1 sesión de 60 min con usuarios clave |

**Tiempo estimado de onboarding:** 3–5 días hábiles desde acceso confirmado.

---

### Setup Business — USD 1.490

| Actividad | Descripción |
|---|---|
| Todo lo del Setup Starter | — |
| Carga histórica extendida | Hasta 2.000.000 filas (últimos 24 meses) |
| Configuración multi-empresa | Hasta 2 empresas SAP (si aplica) |
| Configuración branding avanzada | Logo, favicon, mensaje de bienvenida, colores secundarios |
| Configuración Alert Center | Configuración de las 10 alertas del plan |
| Configuración Operational Live | 3 vistas Operational Live según necesidad del cliente |
| Capacitación por rol | 2 sesiones de 60 min (gerencia + usuarios operativos) |

**Tiempo estimado de onboarding:** 5–8 días hábiles desde acceso confirmado.

---

### Setup Advanced — USD 2.490 (base, puede subir)

| Actividad | Descripción |
|---|---|
| Todo lo del Setup Business | — |
| Hasta 5 empresas SAP | Conexiones y carga histórica por cada empresa |
| Carga histórica 36 meses | Volumen puede ser elevado — cotizar si supera 10M filas/tabla |
| Configuración de módulos avanzados | Inventario avanzado, cobranza completa, recomendaciones |
| Reglas de alerta custom | Definición y configuración de reglas específicas del cliente |
| Capacitación extendida | 4 sesiones por área (comercial, operaciones, finanzas, TI) |
| Runbook entregado al cliente | Documentación de la instalación para el IT del cliente |

**Tiempo estimado de onboarding:** 2–4 semanas desde acceso confirmado.

**Nota:** Si el cliente tiene volumen de datos excepcional (>10M filas históricas), el setup puede requerir cotización adicional. Verificar antes de cerrar contrato.

---

## 7. Add-ons

| Add-on | Precio | Disponible desde |
|---|---|---|
| Usuario adicional | USD 25/mes | Starter |
| Empresa SAP adicional | USD 200/mes | Starter |
| Vista Operational Live adicional | USD 75/mes | Business |
| Paquete alertas custom (+5) | USD 50/mes | Business |
| Histórico extendido +12 meses | USD 50/mes | Starter |
| Módulo inventario avanzado (add-on Starter) | USD 100/mes | Starter |
| Exportación API de lectura | USD 150/mes | Business |
| Integración alertas WhatsApp | USD 100/mes | Business |
| Integración alertas email externo (webhook) | USD 50/mes | Business |
| Módulo custom (cotizar) | Desde USD 500 setup + USD 100/mes | Advanced |
| Power BI add-on (embed de reportes .pbix propios del cliente) | A cotizar | Advanced |
| Escritura controlada SAP (Business Actions con PATCH SL) | A cotizar | Enterprise |
| Recomendaciones ML avanzadas | Roadmap | Enterprise |
| Soporte prioritario SLA 4h (upgrade desde Business) | USD 200/mes | Business |
| SLA 24/7 (upgrade desde Advanced) | USD 400/mes | Advanced |

---

## 8. Costos Internos por Cliente (Referencia)

*Usados para calcular margen real y detectar clientes que no son rentables al precio vigente.*

| Servicio | Starter | Business | Advanced | Fuente de variación |
|---|---|---|---|---|
| Supabase Pro (fracción por tenant) | USD 12 | USD 20 | USD 40 | Volumen de filas y queries |
| Hosting API + portal (fracción) | USD 15 | USD 20 | USD 35 | Instancias, memoria |
| Email transaccional | USD 3 | USD 5 | USD 8 | Volumen de alertas |
| DNS / subdominios | USD 2 | USD 2 | USD 5 | Dominios adicionales |
| Soporte operativo (tiempo estimado) | USD 20 | USD 35 | USD 100 | Tickets, revisiones |
| Monitoreo y alertas internas | USD 5 | USD 8 | USD 15 | Complejidad del tenant |
| **Total estimado** | **~USD 57** | **~USD 90** | **~USD 203** | |
| **Margen bruto** | **USD 293 (84%)** | **USD 510 (85%)** | **USD 797 (80%)** | |

**Alerta de margen:** Si un cliente Starter requiere más de 3h/mes de soporte operativo, el plan ya no es rentable. Escalar a Business o revisar si la configuración es estable.

---

## 9. Propuesta de Valor Vendible

### Versión de una frase

> DataBision convierte SAP Business One en inteligencia operacional en tiempo real, sin licencias adicionales, sin consultores y sin configuración manual.

### Versión de un párrafo

> Las empresas que usan SAP Business One tienen sus datos encerrados en el ERP. La gerencia pide números, alguien los saca en Excel, y las decisiones se toman con datos de ayer. DataBision extrae automáticamente la información de SAP, la limpia, la actualiza cada hora y la entrega en un portal propio de la empresa — con alertas cuando algo sale mal, recomendaciones de qué hacer y visibilidad del estado de sincronización en todo momento. Sin Power BI, sin licencias Microsoft, sin consultor.

### Versión para correo comercial

> Asunto: Tus datos de SAP, actualizados cada hora, desde cualquier dispositivo

> Hola [Nombre]:
>
> Las empresas con SAP Business One tienen el mismo problema: los datos están en el ERP pero las decisiones se toman fuera de él, con Excel desactualizado.
>
> DataBision resuelve esto en 5 días:
>
> - Extrae automáticamente tus datos de SAP cada hora.
> - Los entrega en un portal propio de tu empresa con tu logo y colores.
> - Incluye alertas si el stock cae, si un cliente deja de comprar o si la cobranza se atrasa.
> - No requiere Power BI, no requiere licencias adicionales.
>
> Planes desde USD 350/mes, con setup de implementación incluido.
>
> ¿Tienes 30 minutos esta semana para una demo?
>
> [Firma]

### Versión para reunión comercial (opening de 2 minutos)

> "Cuéntame: cuando tu gerente general quiere saber cómo van las ventas esta semana, ¿cómo lo hace? [Esperar respuesta.]
>
> Exacto. En la mayoría de las empresas SAP que nosotros conocemos, alguien saca un Excel o corre un Crystal Report. Tarda tiempo, el dato no siempre es del día, y cada área tiene su versión.
>
> DataBision conecta directamente con tu SAP Business One, extrae los datos automáticamente cada hora —sin que nadie haga nada— y los muestra en un portal web con el logo y colores de tu empresa. Ventas, clientes, inventario, cobranza. Con alertas cuando algo sale mal y recomendaciones de qué hacer.
>
> No es Power BI. No requiere licencias Microsoft. No tienes que contratar a un consultor cada vez que necesitas un nuevo número.
>
> ¿Le muestro cómo se ve?"

---

## 10. Comparativa Contra Alternativas

| Criterio | Excel manual | Crystal Reports SAP | Power BI interno | Consultoría BI | DataBision |
|---|---|---|---|---|---|
| **Costo mensual** | Costo de tiempo humano | Incluido en SAP | USD 200–500 setup + tiempo IT + USD 10/user/mes | USD 500–2.000/entrega | USD 350–1.000/mes |
| **Actualización de datos** | Manual (diaria como mejor caso) | Manual o programada en SAP | Manual o gateway | Manual por entrega | Automática, cada hora |
| **Acceso sin SAP** | Sí (Excel por email) | No | Sí (requiere Pro) | Entrega por documento | Sí, portal web |
| **Alertas proactivas** | No | No | Solo con Premium | No | Sí, incluido |
| **Recomendaciones** | No | No | No | Subjetivo | Sí (Business+) |
| **Tiempo de entrega** | Inmediato (pero desactualizado) | Días si hay cambios | Semanas de desarrollo | Semanas | 5–8 días de onboarding |
| **Dependencia de persona** | Alta | Alta (consultor SAP) | Alta (IT o BI analyst) | Total | Ninguna |
| **Multi-empresa** | Manual (consolidas tú) | No | Con trabajo extra | Por entrega | Nativo en Business+ |
| **Monitoreo de integridad** | No | No | No | No | Sí — Sync Center |
| **White-label** | No aplica | No | Limitado | No | Sí, nativo |

---

## 11. Riesgos Comerciales y Mitigaciones

### El cliente lo percibe como "un dashboard más"

**Por qué ocurre:** El cliente ha visto muchas demos de Power BI y Tableau y no diferencia.

**Mitigación:** Demostrar el Sync Center en vivo. Mostrar que el dato tiene timestamp de actualización. Mostrar una alerta en tiempo real. El diferenciador no es el chart, es la frescura y la proactividad. Decir explícitamente: "No vendemos el gráfico, vendemos la operación detrás del gráfico."

---

### El cliente pide demasiada personalización en el onboarding

**Por qué ocurre:** Confunde DataBision con una consultoría a medida.

**Mitigación:** El setup tiene un scope definido y documentado antes de firmar. Módulos adicionales tienen precio. La respuesta estándar es "eso entra en Advanced o como add-on; ¿lo incluimos en la propuesta?"

---

### El cliente espera tiempo real (segundos)

**Por qué ocurre:** Confunde SAP con un sistema OLTP consultable en tiempo real.

**Mitigación:** Explicar que el Operational Live Layer accede directamente a SAP para datos críticos en minutos, no horas. Para transacciones del día, la frecuencia de cada hora es suficiente para el 95% de los casos de uso. Si el cliente insiste en segundos, es un requerimiento de integración custom fuera de alcance.

---

### El cliente no quiere pagar el setup

**Por qué ocurre:** Está acostumbrado a SaaS sin frictions donde se registra y ya funciona.

**Mitigación:** El setup no es opcional. Es el trabajo de instalar el extractor en su servidor, cargar el histórico y validar que los datos son correctos. Sin eso, no hay producto. Reencuadrar: "El setup es la implementación. Sin eso, no hay datos."

**Señal de alerta:** Si el cliente no quiere pagar setup, probablemente no es el cliente correcto todavía. No dar el setup gratis en el primer año.

---

### El partner SAP bloquea el acceso

**Por qué ocurre:** El partner del cliente controla el servidor SAP y puede negar la instalación del extractor o la activación de Service Layer.

**Mitigación:** Desarrollar relación con partners SAP B1 antes de entrar al cliente. Tener un programa de partners con comisión. Si el partner bloquea, la alternativa técnica es Service Layer Polling (Modo C) que requiere menos acceso. Si incluso eso está bloqueado, el cliente no es apto para MVP.

---

### Los datos SAP del cliente tienen mala calidad

**Por qué ocurre:** Maestros sucios, facturas mal cargadas, clientes duplicados, precios incorrectos.

**Mitigación:** La validación de datos del setup incluye reconciliación count + montos. Si los datos de SAP tienen problemas, se documenta en el sign-off. DataBision no es responsable de la calidad del dato fuente. Incluir cláusula en contrato. Ofrecer como add-on un análisis de calidad de datos SAP.

---

### El cliente cancela por percepción de que "no lo usa"

**Por qué ocurre:** Configuraron el portal pero los usuarios no entraron nunca.

**Mitigación:** Monitoreo de logins activos. Si un cliente no tiene logins en 2 semanas, contactar proactivamente. El Customer Success (aunque sea el founder) tiene que verificar adopción en los primeros 90 días.

---

## 12. Recomendación Final

### Precio recomendado

| Plan | Precio mensual | Setup |
|---|---|---|
| Starter | **USD 350** | **USD 990** |
| Business | **USD 600** | **USD 1.490** |
| Advanced | **USD 1.000** (base) | **USD 2.490** (base) |
| Enterprise | A medida | A cotizar |

### Plan foco para vender primero

**Business — USD 600/mes.**

Razones:
1. El cliente piloto necesita los módulos de Business para ver valor real (cobranza, Operational Cockpit, alertas).
2. Starter puede percibirse como "poco" y el cliente no adopta.
3. Business a USD 600 da margen suficiente para absorber el costo real del soporte en los primeros clientes (que siempre es más alto de lo esperado).
4. El primer cliente en Business es el caso de éxito que permite vender a los siguientes.

**Estrategia para primer cliente:** Ofrecer Business a USD 500/mes los primeros 3 meses (precio de lanzamiento) y luego normalizar a USD 600. Documentar esto en el contrato.

---

### Qué NO vender todavía

| Lo que no vender | Por qué |
|---|---|
| Advanced con 5 empresas | No está probado end-to-end con multi-empresa real |
| Business Actions (escritura a SAP) | Fuera de roadmap MVP; demasiado riesgo sin validación |
| Recomendaciones con ML | Roadmap futuro; las actuales son reglas fijas |
| SLA 24/7 | No hay equipo de on-call todavía |
| Plan Enterprise | No hay infraestructura Enterprise lista (Azure SQL, compliance docs) |
| Self-service onboarding | El onboarding es manual; aún no hay UI de configuración |
| Power BI add-on | Requiere trabajo adicional no priorizado |

---

### Qué prometer

- Extracción automática de SAP B1 sin intervención manual.
- Actualización de datos cada 1–2 horas según plan.
- Portal propio con subdominio y branding del cliente.
- Sync Center: visibilidad de cuándo fue la última actualización.
- Alert Center: alertas por email cuando algo supera umbrales definidos.
- Onboarding en 5–8 días hábiles desde acceso técnico confirmado.
- Sign-off de calidad de datos al cierre del onboarding.
- Soporte por email con tiempos de respuesta definidos por plan.

---

### Qué NO prometer

- Tiempo real (segundos). El mínimo es cada 15–30 minutos con Operational Live; el estándar es cada hora.
- Módulos fuera de los definidos en el plan contratado.
- Calidad de datos mejor que la calidad del SAP fuente.
- Cero impacto en el servidor SAP del cliente. El extractor usa recursos mínimos, pero no es cero.
- Integración con sistemas que no sean SAP B1.
- Disponibilidad 24/7 con SLA en planes Starter y Business.
- Desarrollo custom incluido en el setup.
- Que el partner SAP del cliente va a cooperar (hay casos donde no cooperan).

---

## 13. Hipótesis No Validadas

Las siguientes afirmaciones requieren validación con clientes reales antes de ser tratadas como hechos:

- ⚠️ **HIPÓTESIS:** "La gerencia de PyMEs SAP en Chile está dispuesta a pagar USD 350–600/mes sin negociación larga." No hay dato de mercado propio todavía.
- ⚠️ **HIPÓTESIS:** "El onboarding se completa en 5–8 días." Estimado en base a diseño técnico; puede ser más largo con clientes complejos.
- ⚠️ **HIPÓTESIS:** "Los costos de infraestructura por tenant están correctamente estimados." Validar con datos reales de los primeros 2 clientes.
- ⚠️ **HIPÓTESIS:** "El partner SAP no bloqueará la instalación del extractor en la mayoría de los casos." Depende del partner y del cliente.
- ⚠️ **HIPÓTESIS:** "USD 600 es el precio correcto para Business." Necesita validación con al menos 3 propuestas cerradas o perdidas.
- ⚠️ **HIPÓTESIS:** "El setup de USD 990–1.490 no es un blocker de decisión." Puede serlo para PyMEs pequeñas. Monitorear.
- ⚠️ **HIPÓTESIS:** "Las recomendaciones básicas (reglas fijas) son suficientes para diferenciarse en Business." Validar con feedback de usuarios.

---

*Documento vivo — actualizar cuando se cierre o pierda una propuesta comercial, o cuando los costos reales de infraestructura difieran de las estimaciones.*
