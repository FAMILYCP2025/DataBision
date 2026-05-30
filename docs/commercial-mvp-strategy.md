# DataBision — Commercial MVP Strategy

**Versión:** 1.1  
**Fecha:** 2026-05-30  
**Estado:** ✅ Actualizado — ver cambios a continuación

> **✅ ACTUALIZADO (2026-05-30 — resolución C-04 auditoría):**
>
> **Precios canónicos actualizados ([ADR-005](adr/ADR-005-pricing-model.md)):**
> | Plan | Precio oficial | Nota |
> |---|---|---|
> | Starter | **USD 350/mes** | |
> | Business | **USD 600/mes** | Piloto primeros clientes: USD 500 por 3 meses |
> | Advanced | **USD 1.000+/mes** | |
> | Enterprise | A medida | Azure SQL, compliance estricto, multi-región |
>
> **Nota comercial para primeros clientes:** Para el cliente piloto inicial, se puede ofrecer Plan Business a USD 500/mes durante los primeros 3 meses como precio de lanzamiento. El precio oficial desde el cuarto mes es USD 600/mes. Esto NO cambia el precio oficial del plan.
>
> **Motor de visualización:** DataBision **Native BI** (React + ECharts) es el núcleo del producto — NO Power BI. Los clientes no necesitan licencias Power BI Pro. Power BI queda como add-on opcional para clientes que ya lo tienen contratado con Microsoft. Ver [ADR-002](adr/ADR-002-bi-layer.md) y [ADR-011](adr/ADR-011-powerbi-as-addon.md).
>
> El resto del documento contiene análisis de estructura de planes y costos que se mantiene válido, pero los precios específicos de USD 300/500 deben leerse como USD 350/600, y todas las referencias a "Reportes Power BI" se interpretan como dashboards DataBision Native BI.

---

## 1. Contexto y Posicionamiento

DataBision es una plataforma SaaS de reportería empresarial para clientes de SAP Business One. Su diferenciador no es el reporte en sí —que cualquiera puede hacer en Power BI— sino el ecosistema completo:

- Extracción automatizada de SAP B1 (sin intervención manual)
- Normalización y staging del dato
- Portal propio por empresa con subdominio y branding
- Visibilidad operativa: estado del extractor, última actualización, historial
- Soporte y continuidad operativa gestionada por DataBision

El cliente compra tranquilidad de datos actualizada, no solo un dashboard.

---

## 2. Segmento Objetivo

**Empresa tipo:**
- SAP B1 HANA o SQL (versión 9.x–10.x)
- 10–200 empleados
- 2–15 usuarios que necesitan reportes
- No tienen equipo de BI propio
- Pagan actualmente por consultoría BI ad-hoc o usan Excel/Crystal Reports

**Pain principal:** Los reportes de SAP B1 nativo son rígidos. Contratar un consultor para cada dashboard cuesta CLP 200.000–600.000 por entrega y tarda semanas. DataBision entrega reportes actualizados automáticamente por una fracción del costo.

---

## 3. Planes Comerciales

### Plan Starter — USD 300/mes

**Dirigido a:** 1 empresa SAP B1, operación estable, reportería básica.

| Componente | Detalle |
|---|---|
| Empresas SAP | 1 |
| Objetos SAP activos | OINV, INV1, OCRD, OITM (4 objetos) |
| Frecuencia de extracción | Cada 2 horas |
| Reportes Power BI | Hasta 5 reportes predefinidos |
| Usuarios en portal | Hasta 5 |
| Acceso a reportes | Enlace portal (usuario necesita Power BI Pro propio) |
| Branding | Logo + colores del cliente |
| Soporte | Email, 48h hábiles |
| SLA uptime | Best effort |

**Margen estimado:**
- Costo infraestructura: ~USD 50/mes
- Margen bruto: ~USD 250/mes (83%)

---

### Plan Business — USD 500/mes

**Dirigido a:** 1 empresa con mayor volumen o 2 empresas del mismo grupo.

| Componente | Detalle |
|---|---|
| Empresas SAP | 1–2 |
| Objetos SAP activos | OINV, INV1, ORIN, RIN1, OCRD, OITM, OSLP (7 objetos MVP) |
| Frecuencia de extracción | Cada hora |
| Reportes Power BI | Hasta 15 reportes (predefinidos + 2 custom/año) |
| Usuarios en portal | Hasta 15 |
| Branding | Logo, colores, favicon, mensaje de bienvenida |
| Soporte | Email + videoconferencia mensual |
| SLA uptime | 99% mensual |
| Widgets portal | KPIs básicos (ventas mes, top clientes, stock crítico) |

**Margen estimado:**
- Costo infraestructura: ~USD 80/mes
- Margen bruto: ~USD 420/mes (84%)

---

### Plan Advanced — USD 1.000+/mes

**Dirigido a:** Grupos empresariales, múltiples empresas SAP, requerimientos custom.

| Componente | Detalle |
|---|---|
| Empresas SAP | Hasta 5 |
| Objetos SAP activos | Todos los MVP + OITW stock + adicionales por contrato |
| Frecuencia de extracción | 30 minutos o según capacidad |
| Reportes Power BI | Ilimitados predefinidos + 5 custom/año |
| Usuarios en portal | Hasta 50 |
| Branding | White-label completo |
| Soporte | Dedicado, SLA 4h hábiles |
| SLA uptime | 99.5% mensual |
| Extras | Exportación Excel, alertas email básicas, historial de actualizaciones |

**Margen estimado:**
- Costo infraestructura: ~USD 150–200/mes (depende del volumen)
- Margen bruto: ~USD 800–850/mes (80–85%)

---

## 4. Estructura de Costos por Plan

### Costos de Infraestructura Estimados (proveedor)

| Servicio | Starter | Business | Advanced |
|---|---|---|---|
| Supabase Pro (compartido) | USD 8 | USD 15 | USD 25 |
| Azure App Service (API + portal) | USD 15 | USD 20 | USD 30 |
| Power BI Pro — 1 licencia DataBision | USD 10 | USD 10 | USD 10 |
| Azure Blob Storage (branding) | USD 1 | USD 2 | USD 3 |
| Dominio + DNS | USD 2 | USD 2 | USD 2 |
| Extractor hosting (VM ligera) | USD 10 | USD 15 | USD 25 |
| Soporte operativo (tiempo estimado) | USD 15 | USD 20 | USD 80 |
| **Total estimado** | **~USD 61** | **~USD 84** | **~USD 175** |
| **Margen** | **USD 239 (80%)** | **USD 416 (83%)** | **USD 825 (83%)** |

**Nota sobre Power BI:** Los usuarios del cliente necesitan sus propias licencias Power BI Pro (USD 10/usuario/mes, pagadas a Microsoft directamente). Esto se comunica como un pre-requisito en la propuesta comercial. En fases futuras, DataBision puede incluirlas y subir precio.

---

## 5. Propuesta de Valor vs. Alternativas

| Solución | Costo mensual | Tiempo de entrega | Actualización |
|---|---|---|---|
| Consultor BI (Crystal Reports) | USD 500–2.000/proyecto | Semanas | Manual |
| Power BI con integración manual | USD 200–500 setup + tiempo | Días | Manual |
| DataBision Starter | USD 300 | 3–5 días | Automática cada 2h |
| DataBision Business | USD 500 | 3–5 días | Automática cada hora |
| Solución enterprise (SAP Analytics) | USD 2.000–10.000+ | Meses | Integrada |

---

## 6. Roadmap Comercial

### Fase 1 — First Client (MVP, hoy → 3 meses)
- 1 cliente piloto en Plan Business a tarifa reducida (USD 250–300)
- Validar flujo completo: extractor → Supabase → Power BI → portal
- Objetivo: facturar USD 250–300/mes, 0 costo de marketing
- Aprender: qué reportes pide el cliente, qué falla, qué valor percibe

### Fase 2 — Product-Market Fit (3–9 meses)
- 3–5 clientes activos
- Plan Starter y Business operativos
- Portal con branding, widgets KPI, historial de actualizaciones
- Alertas email básicas
- Facturación: USD 1.200–2.500/mes

### Fase 3 — Scale (9–18 meses)
- 10–20 clientes
- Plan Advanced con multi-empresa
- Recomendaciones simples (reglas fijas, no IA todavía)
- Objetivo: USD 5.000–10.000/mes MRR

### Fase 4 — Enterprise (18+ meses)
- Power BI Embedded / Fabric (cuando justifique inversión)
- ChatBot analítico sobre los datos (RAG sobre PostgreSQL)
- Alertas inteligentes y S&OP
- Escritura controlada a SAP
- Plan Enterprise: USD 2.000+/mes

---

## 7. Acceso a Reportes — Modelo Sin Embedded

En el MVP sin Power BI Embedded, el acceso a reportes funciona así:

**Opción A (recomendada para MVP):** Publicar a Web (temporal)
- DataBision publica reportes en modo "Publicar en web" de Power BI (solo para datos no confidenciales)
- Los embeds son públicos — NO apropiado para datos de clientes

**Opción B (correcta para producción):** Workspace compartido
- DataBision crea un workspace de Power BI por cliente
- El cliente tiene 1 usuario Power BI Pro (DataBision o del cliente)
- Los reportes se comparten como enlace autenticado dentro del workspace
- El portal de DataBision muestra el enlace / iframe protegido

**Opción C (recomendada para clientes con AD):** Embed for Organization
- Usa Azure AD del cliente
- Solo funciona si el cliente es parte del mismo tenant de Azure
- Complejo para MVP

**Decisión recomendada para MVP:** Opción B. Un workspace Power BI por cliente. El enlace al reporte se embebe en el portal con autenticación Microsoft del usuario. El cliente necesita al menos 1 licencia Power BI Pro.

---

## 8. Decisiones Pendientes de Confirmar

1. ¿Incluimos la licencia Power BI Pro del cliente en el precio o es pre-requisito externo?
2. ¿El primer cliente piloto usa Plan Business o Starter?
3. ¿Los reportes iniciales son los mismos para todos los clientes (predefinidos) o custom?
4. ¿Usamos Opción B para embeds (workspace compartido) y aceptamos que el cliente tenga Pro?
5. ¿El extractor se instala en el servidor del cliente o en un servidor DataBision con VPN?

---

## 9. Azure SQL como Opción Enterprise Futura

Azure SQL no fue un error de diseño — es la opción natural cuando:
- El cliente requiere residencia de datos en Azure (compliance, industria)
- El volumen supera lo que Supabase puede manejar sin Plan de alto costo
- Se necesita integración directa con Azure Synapse / Fabric para reportería avanzada
- El cliente ya tiene contrato Azure Enterprise

Azure SQL se documenta en `docs/azure-sql-staging-design.md` como Plan Enterprise a partir de USD 1.500/mes.

---

*Documento vivo — actualizar cuando se cierre el primer cliente o se tome decisión sobre licencias Power BI.*
