# Native BI Finance — Matriz de Responsabilidades RACI

**Sprint 29 · DataBision · Junio 2026**

---

## Roles involucrados

| Código | Rol | Descripción |
|---|---|---|
| **DB** | Consultor DataBision | Implementación, extracción, soporte técnico |
| **TI** | TI Cliente | Acceso SAP, firewall, infraestructura, usuario SAP |
| **FIN** | Finanzas / Gerente Financiero | Aprobación de resultados, decisiones financieras |
| **CONT** | Contador / Jefe Contabilidad | Clasificación contable, validación PCGE, aprobación |
| **BASIS** | SAP Basis / Administrador SAP | Creación de usuarios SAP, permisos, Service Layer |

---

## Leyenda RACI

| Letra | Significado |
|---|---|
| **R** | Responsible — quien ejecuta la tarea |
| **A** | Accountable — quien aprueba y rinde cuentas |
| **C** | Consulted — quien aporta información o criterio |
| **I** | Informed — quien recibe notificación del resultado |

---

## Matriz RACI por actividad

### FASE 1 — Configuración de acceso

| Actividad | DB | TI | FIN | CONT | BASIS |
|---|---|---|---|---|---|
| Crear usuario SAP solo lectura (`DATABISION_RO`) | I | C | I | — | **R/A** |
| Asignar permisos OACT + OJDT al usuario SAP | I | C | I | — | **R/A** |
| Proporcionar URL del SAP Service Layer | I | **R/A** | I | — | C |
| Proporcionar CompanyDB | I | **R/A** | I | — | C |
| Abrir firewall extractor → SAP Service Layer | I | **R/A** | I | — | C |
| Abrir firewall extractor → DataBision API | I | **R/A** | I | — | — |
| Verificar certificado SSL del Service Layer | **R** | **A** | I | — | C |
| Confirmar horario de mantenimiento SAP | **R** | C | I | — | **A** |

### FASE 2 — Configuración técnica DataBision

| Actividad | DB | TI | FIN | CONT | BASIS |
|---|---|---|---|---|---|
| Crear perfil de conexión (`NativeBiConnectionProfile`) | **R/A** | I | — | — | — |
| Registrar cliente en base de datos DataBision | **R/A** | I | — | — | — |
| Ejecutar test de conexión E2E | **R/A** | C | I | — | — |
| Configurar API Key de ingest por cliente | **R/A** | I | — | — | — |
| Almacenar credenciales como SecretRef | **R/A** | I | — | — | — |

### FASE 3 — Extracción de datos

| Actividad | DB | TI | FIN | CONT | BASIS |
|---|---|---|---|---|---|
| Ejecutar extracción OACT (plan de cuentas) | **R/A** | I | I | C | — |
| Ejecutar extracción OJDT (asientos) | **R/A** | I | I | C | — |
| Verificar integridad de datos en RAW | **R/A** | — | — | C | — |
| Ejecutar MART refresh inicial | **R/A** | — | I | I | — |
| Reportar resultado de extracción al cliente | **R/A** | I | **A** | I | — |

### FASE 4 — Clasificación contable

| Actividad | DB | TI | FIN | CONT | BASIS |
|---|---|---|---|---|---|
| Exportar lista de cuentas para revisión | **R/A** | — | — | I | — |
| Clasificar cuentas por categoría PCGE | C | — | C | **R/A** | — |
| Resolver cuentas con clasificación ambigua | C | — | C | **R/A** | — |
| Aplicar clasificación en el sistema | **R/A** | — | — | I | — |
| Re-ejecutar MART con clasificación aplicada | **R/A** | — | I | I | — |
| Confirmar que no hay cuentas sin clasificar | **R** | — | — | **A** | — |

### FASE 5 — Validación financiera

| Actividad | DB | TI | FIN | CONT | BASIS |
|---|---|---|---|---|---|
| Preparar presentación para taller de validación | **R/A** | — | I | I | — |
| Revisar P&L en sesión de validación | R | — | **A** | **R** | — |
| Revisar Balance en sesión de validación | R | — | **A** | **R** | — |
| Revisar EBITDA en sesión de validación | R | — | **A** | **R** | — |
| Identificar diferencias vs. reportes contador | C | — | C | **R/A** | — |
| Aprobar clasificación final | I | — | **A** | **R** | — |
| Firmar documento de aceptación financiera | I | — | **A** | **R** | — |

### FASE 6 — Go-live y capacitación

| Actividad | DB | TI | FIN | CONT | BASIS |
|---|---|---|---|---|---|
| Configurar scheduler automático (cron / Task Scheduler) | **R/A** | C | — | — | — |
| Capacitar usuarios finales en el uso del dashboard | **R/A** | I | **A** | I | — |
| Entregar runbook operativo | **R/A** | I | **A** | I | — |
| Confirmar que el refresh diario está operativo | **R/A** | I | **A** | — | — |
| Reunión Go/No-Go día 30 | R | I | **A** | C | — |

### FASE 7 — Operación continua

| Actividad | DB | TI | FIN | CONT | BASIS |
|---|---|---|---|---|---|
| Monitorear refresh-status diariamente | **R/A** | — | I | — | — |
| Ejecutar retry manual si extractor falla | **R/A** | C | I | — | — |
| Responder consultas de usuarios del dashboard | **R/A** | — | **A** | C | — |
| Ajustar clasificación de cuentas nuevas | C | — | C | **R/A** | — |
| Aplicar ajustes de clasificación en el sistema | **R/A** | — | I | A | — |
| Renovar credenciales SAP si expiran | I | C | — | — | **R/A** |
| Actualizar versión del extractor | **R/A** | C | I | — | — |

---

## Resumen de responsabilidades críticas

| Rol | Responsabilidades principales |
|---|---|
| **DataBision** | Implementación técnica, extracción, clasificación técnica, soporte, monitoreo |
| **TI Cliente** | Acceso SAP, firewall, usuario SAP, infraestructura |
| **Finanzas / CFO** | Aprobación de resultados, decisión de continuidad, acceso de usuarios |
| **Contador** | Clasificación PCGE, validación de P&L y Balance, firma de aceptación |
| **SAP Basis** | Creación y gestión del usuario SAP `DATABISION_RO`, permisos |

---

## Escalamiento

Si una actividad está bloqueada más de 2 días hábiles:

1. Consultor DataBision identifica el bloqueo
2. Notifica al Gerente Financiero (FIN) — quien es el Accountable del proyecto
3. El Gerente Financiero desbloquea internamente (TI, Basis, Contador)
4. Si persiste → llamada de emergencia entre DataBision + FIN en 24 horas
