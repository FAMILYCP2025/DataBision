# DataBision × KSDEPOR — Pilot Scope

Sprint 8Q — Junio 2026

Alcance técnico y comercial del pilot de 90 días para KSDEPOR.

---

## Resumen ejecutivo

El pilot de DataBision para KSDEPOR es un compromiso de 90 días que incluye todos los módulos de BI nativo activos, carga histórica de datos, soporte técnico y acceso ilimitado de usuarios. El objetivo del pilot es validar el valor de la plataforma en el contexto real de la operación de KSDEPOR antes de un contrato de largo plazo.

---

## Módulos incluidos

| Módulo | Estado | Descripción |
|---|---|---|
| Ventas | ✅ Incluido | Análisis por cliente, producto, vendedor y fulfillment de pedidos |
| Compras | ✅ Incluido | Proveedores, órdenes y recepciones |
| Inventario | ✅ Incluido | Rotación de artículos y movimientos por almacén |
| Finanzas | ✅ Incluido | AR aging y AP aging |
| Operaciones | ✅ Incluido | Health score del pipeline y calidad de datos |

---

## Objetos SAP que se extraen

| Objeto SAP | Descripción | Módulo(s) |
|---|---|---|
| OINV | Facturas de venta | Ventas, Finanzas |
| ORDR | Órdenes de venta | Ventas (Fulfillment) |
| ODLN | Entregas / notas de entrega | Ventas (Fulfillment) |
| OPOR | Órdenes de compra | Compras |
| OPDN | Recepciones de compra | Compras |
| OPCH | Facturas de proveedor | Finanzas (AP) |
| OITM | Maestro de artículos | Inventario, Ventas |
| OWTR | Transferencias de stock | Inventario (Almacenes) |
| OCRD | Business Partners | Ventas, Compras, Finanzas |
| OSLP | Maestro de vendedores | Ventas |
| OWHS | Maestro de almacenes | Inventario |

**Nota:** El acceso al SAP es de **solo lectura** — DataBision nunca escribe ni modifica datos.

---

## Datos históricos

- **Período de carga inicial:** Configurable — mínimo 12 meses, máximo el historial disponible en SAP
- **Duración de la carga inicial:** 1-3 días dependiendo del volumen de datos
- **Frecuencia de actualización posterior:** Nightly (una vez por día, automático)
- **Retención de datos en DataBision:** 36 meses (configurable a más en plan enterprise)

---

## Requisitos técnicos (KSDEPOR)

| Requisito | Descripción |
|---|---|
| SAP Business One | Versión 9.x o superior |
| Service Layer activo | Necesario para el acceso vía API. Puerto 50000 por defecto |
| Usuario de Service Layer | Usuario de lectura sin permisos de escritura. DataBision genera el catálogo de permisos necesarios |
| Conectividad | El servidor SAP debe tener acceso saliente a internet (HTTPS, puerto 443) |
| IP pública o VPN | Si el Service Layer no es accesible desde internet, se evalúa túnel o VPN según arquitectura de red |

**No requiere:**
- Instalación de software en servidores de KSDEPOR
- Acceso a base de datos de SAP (HANA o SQL Server)
- Modificaciones al SAP

---

## Usuarios y accesos

- **Sin límite de usuarios** — cualquier empleado de KSDEPOR puede tener acceso
- **Roles disponibles:**
  - `Gerencia`: acceso a todos los módulos
  - `Ventas`: acceso a módulo de Ventas únicamente
  - `Compras`: acceso a módulos de Compras e Inventario
  - `Finanzas`: acceso a módulo de Finanzas
  - `IT/Admin`: acceso a Operaciones + todos los módulos
- **Gestión de usuarios:** desde el panel de SuperAdmin de DataBision (el equipo DataBision gestiona durante el pilot)

---

## Entregables del pilot

| Entregable | Semana | Responsable |
|---|---|---|
| Ambiente de producción configurado | Semana 1 | DataBision |
| Carga histórica completada | Semana 2 | DataBision |
| Go-live del portal | Semana 2-3 | DataBision |
| Capacitación de usuarios (1 sesión, 90 min) | Semana 3 | DataBision |
| Revisión de resultados intermedia (30 días) | Mes 1 | DataBision + KSDEPOR |
| Revisión final (90 días) | Mes 3 | DataBision + KSDEPOR |
| Informe de uso y ROI | Mes 3 | DataBision |

---

## Lo que NO está incluido en el pilot

- Dashboards de Power BI (disponibles como add-on)
- Integración con sistemas distintos a SAP Business One
- Reportes completamente personalizados fuera del catálogo de módulos estándar
- Modificaciones al SAP Business One de KSDEPOR

---

## Modelo de precios

> **Nota:** Los precios exactos se confirman en la propuesta comercial según condiciones específicas de la negociación. Los rangos a continuación son orientativos.

| Concepto | Descripción |
|---|---|
| Setup fee (one-time) | Implementación, configuración, carga histórica y capacitación |
| Suscripción mensual | Acceso al portal, soporte técnico, actualizaciones de la plataforma |
| Modalidad | Por empresa (no por usuario) |
| Compromiso pilot | 3 meses (sin renovación automática) |
| Después del pilot | Contrato anual con precio negociado según uso |

---

## Criterios de éxito del pilot

El pilot se considera exitoso si al finalizar los 90 días:

1. **Adopción:** Al menos 3 usuarios activos de KSDEPOR acceden al portal semanalmente
2. **Datos:** Los módulos muestran datos actualizados y consistentes con SAP
3. **Valor identificado:** El equipo de KSDEPOR puede nombrar al menos un KPI que antes requería 30+ minutos de trabajo manual y ahora está disponible en el portal en tiempo real
4. **Satisfacción:** Valoración positiva en encuesta de fin de pilot

---

## Términos del acuerdo

- Acceso de solo lectura a SAP de KSDEPOR
- NDA mutuo firmado antes del inicio
- Portabilidad de datos garantizada: KSDEPOR puede solicitar export completo de sus datos en formato CSV en cualquier momento
- Datos de KSDEPOR almacenados en esquema separado — no accesibles por otros clientes
- Resolución de incidentes: SLA de 24h para critical, 72h para warning
