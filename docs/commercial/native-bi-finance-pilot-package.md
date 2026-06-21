# DataBision Native BI Finance — Paquete Piloto Vendible

**Sprint:** 21F  
**Fecha:** 2026-06-20  
**Versión:** 1.0

---

## Descripción del producto

**DataBision Native BI Finance** es un módulo de reportería financiera automática para empresas que usan SAP Business One. Conecta directamente a SAP B1 vía Service Layer REST API (solo lectura) y genera automáticamente:

- Estado de Resultados (P&L) por período, clasificado según PCGE/Plan Contable local
- Balance General con activos, pasivos y patrimonio
- EBITDA con tendencia mensual
- Plan de Cuentas con clasificación automática

Sin importaciones manuales. Sin Excel intermediario. Sin modificar SAP.

---

## Tres opciones de piloto

---

### Opción A — Diagnóstico SAP B1 (5 días) · S/ 1,500 + IGV

**¿Para quién?** Empresas que quieren evaluar si DataBision puede conectar a su SAP B1 antes de comprometerse.

**Entregables:**
- Validación técnica: conectividad SAP SL, permisos, versión
- Primera extracción de datos (OACT + OJDT)
- Dashboard financiero con datos reales del cliente (acceso de 7 días)
- Informe de diagnóstico: cuentas detectadas, clasificación PCGE automática, gaps identificados
- Recomendación Go/No-Go para piloto completo

**Lo que NO incluye:**
- Clasificación refinada con el contador
- Configuración de scheduler
- Capacitación de usuarios
- Soporte post-entrega

**Duración:** 5 días hábiles desde el inicio del trabajo técnico  
**Prerequisito:** Acceso a SAP B1 Service Layer (URL, usuario, password)

---

### Opción B — Piloto 2 semanas · S/ 4,500 + IGV

**¿Para quién?** Empresas que quieren validar el producto con sus datos reales y usuarios internos antes de contratar el servicio anual.

**Entregables:**
- Todo lo de Opción A
- Clasificación PCGE refinada con el contador del cliente
- Dashboard financiero limpio: IS, Balance, EBITDA, Plan de Cuentas
- Export CSV configurado para sus reportes mensuales
- Configuración de scheduler semanal (Windows o Linux)
- 2 sesiones de capacitación (CFO/Controller + TI)
- Soporte durante las 2 semanas

**Lo que NO incluye:**
- Módulos adicionales (ventas, inventario, compras)
- Personalización de UI/branding avanzada
- Integración Power BI (disponible en tier anual)
- Soporte mensual post-piloto

**Duración:** 10 días hábiles  
**Prerequisito:** Acceso a SAP B1 + contador disponible para validación (2h en Día 2)

---

### Opción C — Implementación producción (4-8 semanas) · Desde S/ 12,000 + IGV

**¿Para quién?** Empresas que ya validaron el producto (vía Opción A o B) y quieren la implementación completa de producción.

**Entregables:**
- Todo lo de Opción B
- Todos los módulos contratados (finanzas, ventas, inventario)
- Branding completo (logo, colores corporativos)
- Scheduler automático configurado y monitoreado
- Integración Power BI (si el cliente tiene Power BI Pro)
- Usuarios configurados con roles (CFO, Controller, Gerente de Ventas, etc.)
- 4 semanas de soporte post-implementación
- SLA de disponibilidad y actualización de datos
- Manual de usuario personalizado

**Lo que NO incluye:**
- Modificaciones al código fuente de DataBision
- Reemplazo de contabilidad oficial (DataBision es reportería, no sistema contable)
- Auditoría contable formal
- Acceso al código fuente

**Duración:** 4-8 semanas según alcance y número de módulos  
**Prerequisito:** Opción A o B completada, o entorno SAP B1 productivo ya validado

---

## Tabla comparativa

| Feature | Opción A | Opción B | Opción C |
|---|---|---|---|
| Conectividad SAP validada | ✅ | ✅ | ✅ |
| Dashboard financiero (7 días) | ✅ | ✅ | ✅ |
| Dashboard financiero (continuo) | ❌ | ✅ | ✅ |
| Clasificación PCGE refinada | ❌ | ✅ | ✅ |
| Export CSV | ❌ | ✅ | ✅ |
| Scheduler automático | ❌ | ✅ | ✅ |
| Capacitación usuarios | ❌ | ✅ | ✅ |
| Módulos adicionales | ❌ | ❌ | ✅ |
| Branding personalizado | ❌ | ❌ | ✅ |
| Integración Power BI | ❌ | ❌ | ✅ |
| Soporte post-entrega (semanas) | — | 2 | 4 |
| **Precio (S/ + IGV)** | **1,500** | **4,500** | **desde 12,000** |

---

## Disclaimers obligatorios

Incluir en todos los contratos y propuestas:

1. **No reemplaza SAP FI:** DataBision es una herramienta de reportería y análisis. No es un sistema contable ni reemplaza el módulo FI de SAP.
2. **No modifica datos SAP:** DataBision solo lee datos vía Service Layer REST API. No tiene acceso de escritura a SAP B1.
3. **No es auditoría contable:** Los reportes generados por DataBision requieren validación por el contador o auditor del cliente. DataBision no certifica la exactitud contable.
4. **Requiere validación del contador:** La clasificación automática PCGE es una aproximación. El contador del cliente debe revisar y aprobar las clasificaciones antes de usar los reportes para toma de decisiones.
5. **Datos de producción vs. prueba:** Los datos de un ambiente SAP TST o desarrollo son distintos a los de producción. Los reportes reflejan exactamente los datos que están en SAP.
6. **Balance puede no cuadrar sin cierre de ejercicio:** Si SAP no tiene asientos de cierre (distribución de utilidades, capital), el balance no cuadrará contablemente. Esto es comportamiento esperado de SAP, no un error de DataBision.
