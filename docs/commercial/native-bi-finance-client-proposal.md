# DataBision Finance — Propuesta Comercial

Sprint 15F — 2026-06-18

---

## Propuesta de implementación del Módulo de Finanzas Contables

**Para:** [Cliente — reemplazar]  
**Preparado por:** DataBision  
**Fecha:** 2026-06-18

---

## 1. Situación actual

[Describir brevemente el proceso actual del cliente: exportaciones manuales, reportes SAP estáticos, consolidación en Excel, demoras en el cierre mensual.]

**Impacto estimado del proceso manual:**
- [X] horas/mes del equipo de contabilidad en preparación de reportes
- [X] días de demora en el cierre mensual
- Riesgo de discrepancias por versiones múltiples de Excel

---

## 2. Solución propuesta

Implementación del módulo de **Finanzas Contables** de DataBision sobre su instalación actual de SAP Business One.

### Módulos incluidos

| Módulo | Descripción |
|---|---|
| Estado de Resultados | P&L mensual con desglose revenue/COGS/OPEX/utilidad |
| EBITDA | Rentabilidad operacional con tendencia 12 meses |
| Balance General | Activos, Pasivos y Patrimonio por período de cierre |
| Plan de Cuentas | Maestro completo con clasificación contable y saldos |
| Validaciones Contables | Health score automático, balance cuadra, alertas |
| Panel de Disponibilidad | Estado de datos en tiempo real (RAW → STG → MART) |

### Arquitectura de seguridad

- Datos aislados por empresa (tenant isolation)
- Acceso por subdomain o JWT autenticado
- Sin acceso de escritura a SAP — solo lectura
- Logs de auditoría en todos los accesos

---

## 3. Plan de implementación

### Fase 1 — Configuración (1–2 días)

| Actividad | Responsable | Duración |
|---|---|---|
| Configurar acceso extractor a SAP B1 (credenciales de lectura) | Cliente + DataBision | 2h |
| Extraer catálogo de cuentas (OACT) | DataBision | 30min |
| Sesión de clasificación contable con contador | Cliente (contador) + DataBision | 2–4h |
| Aplicar reglas de clasificación | DataBision | 30min |

### Fase 2 — Extracción histórica (1 día)

| Actividad | Responsable | Duración |
|---|---|---|
| Extraer 24 meses de asientos contables (OJDT + JDT1) | DataBision | 1–4h (depende del volumen) |
| Ejecutar ETL (RAW → STG → MART) | DataBision | < 30min |
| Validación técnica: balance cuadra, revenue positivo | DataBision | 30min |

### Fase 3 — Validación con el cliente (1 sesión)

| Actividad | Responsable | Duración |
|---|---|---|
| Revisión del P&L con CFO/Controller | Cliente + DataBision | 1–2h |
| Ajuste de clasificaciones si hay discrepancias | DataBision + contador | 30min |
| Re-ejecución ETL y validación final | DataBision | 30min |

### Fase 4 — Go-live y capacitación (1 día)

| Actividad | Responsable | Duración |
|---|---|---|
| Configurar extracción diaria automática | DataBision | 1h |
| Capacitación de usuarios (CFO, Controller, Gerencia) | DataBision | 1h |
| Documentación de clasificaciones aprobadas | DataBision | Incluido |

**Total estimado: 3–5 días hábiles** desde firma hasta go-live.

---

## 4. Requisitos del cliente

| Requisito | Tipo | Notas |
|---|---|---|
| SAP Business One activo con Service Layer (SL) habilitado | Técnico | Versiones B1 9.3+ |
| Usuario de lectura en SAP (sin permisos de escritura) | Acceso | Para extracción |
| Acceso del contador por 2–4 horas (clasificación) | Tiempo | Una sola sesión |
| Revisión de estados financieros por CFO (1 sesión) | Tiempo | Validación final |
| Rango de fechas para extracción histórica (recomendado 24m) | Datos | Mínimo 3 meses para demo útil |

---

## 5. Entregables

- [ ] Dashboard Finance en DataBision con datos reales del cliente
- [ ] Reglas de clasificación contable documentadas y aprobadas por contador
- [ ] Manual de usuario para CFO/Controller
- [ ] Documentación técnica de la configuración
- [ ] Acceso a SuperAdmin para gestión autónoma de clasificaciones

---

## 6. Modelo de servicio post-implementación

| Servicio | Frecuencia | Incluido |
|---|---|---|
| Extracción incremental de asientos | Diaria | ✅ |
| Refresh de estados financieros (MART) | Diaria | ✅ |
| Re-extracción OACT (cuando cambia el plan de cuentas) | Bajo demanda | ✅ |
| Revisión mensual de clasificaciones con contador | Mensual (opcional) | A cotizar |
| Reportes adicionales o visualizaciones custom | Bajo demanda | A cotizar |
| Soporte técnico ante incidencias | 8x5 | ✅ |

---

## 7. Garantías

- **Calidad de datos:** Si el balance no cuadra en el primer mes post-implementación, sesión adicional de clasificación sin costo.
- **Disponibilidad:** SLA de 99.5% para los dashboards. Extractor con retry automático.
- **Seguridad:** Datos del cliente nunca compartidos con otras empresas. Acceso restringido al equipo DataBision.

---

## 8. Siguientes pasos

1. Firma de contrato / orden de servicio
2. Envío de credenciales SAP de lectura (formato provisto por DataBision)
3. Coordinación de sesión de clasificación contable (video llamada de 2–4h con contador)
4. Inicio de Fase 1 dentro de 5 días hábiles post-firma

---

**¿Preguntas?** Contactar a campillayparedes@gmail.com
