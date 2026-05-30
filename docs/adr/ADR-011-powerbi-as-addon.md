# ADR-011 — Power BI como Add-on Futuro: Condiciones, Scope y Governance

**Fecha:** 2026-05-30  
**Estado:** Aceptado  
**Autor:** Chief Architect  

---

## Contexto

Múltiples documentos describían Power BI Pro como la forma principal de entrega de reportes al cliente. La decisión arquitectónica central (ADR-002, ADR-006) reemplazó Power BI con DataBision Native BI como núcleo del producto.

Sin embargo, quedaba sin definir formalmente qué significa "Power BI como add-on futuro": cuándo se ofrece, bajo qué condiciones, y cómo se integra sin que contamine la arquitectura del MVP.

---

## Decisión

### Power BI NO es parte del MVP ni de las Fases 1–3

Power BI no aparece en el stack tecnológico del MVP. No hay:
- Service Principal de Azure AD para Power BI
- Workspaces de Power BI creados automáticamente
- Embed tokens generados por DataBision
- Endpoints específicos de Power BI en la API del MVP
- Referencias a Power BI en la UI del portal MVP

Esto es deliberado y permanente hasta que se cumplan las condiciones de activación definidas abajo.

### Cuándo Power BI se convierte en opción real

**Trigger obligatorio:** al menos 3 clientes activos solicitan explícitamente Power BI en una negociación comercial, y ese requerimiento está documentado en el contrato.

Un solo cliente solicitándolo es insuficiente. El costo de implementar y mantener Power BI como add-on (Service Principal, workspaces, embed tokens, RLS en DAX) es significativo y solo se justifica con demanda comprobada.

### Cómo se ofrece (cuando aplique)

**Opción A — Cliente con Power BI Pro propio:**
1. DataBision crea un workspace Power BI conectado a Supabase
2. Publica un .pbix con los datos del cliente
3. El portal puede mostrar un iframe del reporte Power BI como tab adicional
4. El cliente accede directamente a su workspace con sus credenciales Microsoft

**Opción B — DataBision gestiona la licencia:**
1. DataBision incluye una cuenta de servicio Power BI Pro (USD 10/mes adicional)
2. Se usa "Embed for Organization" para embeber sin requerir licencia del usuario final
3. Solo viable para Plan Business o superior

**Opción C — Power BI Embedded (Fase 4+):**
Solo cuando el número de clientes activos justifica la capacidad Premium:
- Fabric F2 (~USD 262/mes) viable con 10–15 clientes Business simultáneos
- Eliminación de la dependencia de licencias por usuario
- Requiere diseño adicional de RLS en DAX y Service Principal

### Lo que Power BI NUNCA reemplaza en DataBision

Independientemente de si un cliente usa Power BI como add-on, DataBision siempre provee:
- Portal operacional nativo con Native BI
- Operational Intelligence Layer (estado del extractor, frescura, anomalías)
- Recommendation Engine
- Alerting Engine
- Business Actions Module
- Operational Live Layer

Power BI puede ser una capa de visualización adicional para clientes que lo prefieren. No puede reemplazar la capa operacional de DataBision.

### Propuesta de venta cuando el cliente insiste en Power BI

> "DataBision es totalmente compatible con Power BI. Si tu empresa ya tiene licencias Power BI Pro, podemos conectar tu workspace directamente a los datos que DataBision extrae de SAP. Pero hay algo que Power BI no puede darte: saber en tiempo real si tus datos están actualizados, recibir alertas cuando un cliente deja de comprar, o tener acceso operacional directo a tu SAP desde el portal. Eso es exclusivo de DataBision."

---

## Consecuencias

### Para el equipo de desarrollo

No implementar nada relacionado con Power BI en Sprints 0–3. Si aparece en el código, eliminar con la nota `// PHASE-4: Power BI add-on — not part of MVP`.

### Para el equipo comercial

Power BI es compatible con DataBision pero no es el producto. Reforzar el valor del Native BI: no requiere licencias adicionales del cliente, branding 100% propio, datos operacionales en tiempo real.

### Para la documentación

`docs/powerbi-pro-import-mode-strategy.md` se reposiciona como guía técnica para cuando el add-on se implemente. No es una estrategia de producto activa.

---

## Documentos relacionados

- [ADR-002](ADR-002-bi-layer.md) — Power BI → Native BI como decisión central
- [ADR-006](ADR-006-native-bi-vs-powerbi.md) — Por qué construir BI propio
- [powerbi-pro-import-mode-strategy.md](../powerbi-pro-import-mode-strategy.md) — Guía técnica cuando el add-on se active
