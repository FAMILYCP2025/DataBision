# Native BI Finance — Propuesta de Piloto Pagado

**DataBision · Junio 2026**  
**Versión:** 1.0

---

## Resumen ejecutivo

DataBision Native BI Finance convierte el libro diario de SAP Business One en estados financieros ejecutivos actualizados diariamente. Este documento describe el alcance, entregables, precio y condiciones del piloto inicial de 30 días.

---

## 1. Alcance del piloto

### Incluido

| Entregable | Descripción |
|---|---|
| **Perfil de conexión SAP** | Configuración segura de acceso al Service Layer del cliente |
| **Extracción inicial** | OACT (plan de cuentas) + OJDT (asientos) + JDT1 (líneas) de los últimos 12 meses |
| **Clasificación contable** | Mapeo de cuentas PCGE con el contador del cliente |
| **Dashboard P&L** | Estado de Resultados por período, agrupado por categorías PCGE |
| **Dashboard Balance** | Activos, Pasivos, Patrimonio con validación de cuadre |
| **Dashboard EBITDA** | Cálculo automático desde cuentas operativas |
| **Refresh diario** | Proceso automatizado Windows Task Scheduler o cron Linux |
| **Endpoint refresh-status** | Trazabilidad del último proceso de extracción |
| **Runbook operativo** | Documento con procedimientos diarios, errores comunes y retry manual |
| **Capacitación** | 1 sesión de 2 horas con usuarios finales |
| **Validación financiera** | 1 taller de 90 min con contador del cliente |

---

## 2. Duración

**30 días calendario** desde la fecha de firma y entrega de credenciales SAP.

| Fase | Días | Actividad |
|---|---|---|
| Acceso y configuración | 1–2 | Perfil SAP, test de conexión end-to-end |
| Extracción inicial | 3–4 | OACT + OJDT + JDT1, primera carga completa |
| Clasificación | 5–6 | Sesión con contador, mapeo de cuentas |
| Validación interna | 7–8 | Verificación P&L, Balance, EBITDA |
| Validación con cliente | 9–10 | Taller financiero, ajustes |
| Capacitación | 11–12 | Usuarios finales, runbook |
| Operación piloto | 13–30 | Dashboard en vivo, monitoreo, soporte |

---

## 3. Exclusiones

El piloto **no incluye**:

- Módulos de Compras, Inventario o Ventas (fuera del alcance Finance)
- Integración con Power BI Service externo
- Desarrollo de KPIs personalizados no estándar
- Reportes con firma digital o valor legal
- Modificaciones al sistema SAP Business One del cliente
- Soporte fuera del horario comercial (L–V 9am–6pm PET)
- Más de 2 rondas de ajuste de clasificación contable

---

## 4. Requisitos del cliente

Para iniciar el piloto, el cliente debe proveer:

| Requisito | Responsable | Fecha límite |
|---|---|---|
| URL del SAP Service Layer | TI cliente | Antes del Día 1 |
| CompanyDB (base de datos SAP) | TI cliente | Antes del Día 1 |
| Usuario SAP solo lectura (OACT + OJDT) | SAP Basis / TI | Antes del Día 1 |
| Apertura de firewall al servidor extractor | TI cliente | Antes del Día 1 |
| Disponibilidad del contador (2 horas) | Finanzas cliente | Días 5–6 |
| Disponibilidad de usuarios finales (2 horas) | Gerencia cliente | Días 11–12 |
| Certificado SSL válido en Service Layer | TI / SAP Basis | Antes del Día 1 |

---

## 5. Precio sugerido

### Opción A — Piloto básico

**USD 800** — pago único antes del inicio

Incluye todo el alcance definido en la sección 1. Sin suscripción mensual en esta fase.

### Opción B — Piloto + primer mes de suscripción

**USD 1,200** — pago único

Incluye el piloto completo + primer mes de operación con soporte activo + reunión de revisión de resultados al día 30.

### Condiciones de pago

- Pago previo al inicio de implementación
- Transferencia bancaria o plataforma acordada con el cliente
- Factura electrónica emitida al inicio del piloto
- Sin reembolso parcial una vez iniciada la extracción

---

## 6. Criterios de éxito del piloto

El piloto se considera **exitoso** si al día 30 se cumplen todos:

| Criterio | Métrica |
|---|---|
| Dashboard operativo | P&L, Balance y EBITDA accesibles en navegador |
| Datos actualizados | Última extracción < 24 horas |
| Refresh automático | Tarea programada corriendo sin intervención manual |
| Clasificación aprobada | Contador del cliente firma validación financiera |
| Cuadre contable | Balance activos = pasivos + patrimonio (tolerancia < 0.01%) |
| Aceptación usuario | Al menos 1 usuario no técnico usa el dashboard de forma autónoma |

---

## 7. Go / No-Go al día 30

Al finalizar los 30 días, se realiza una reunión de Go/No-Go:

**Go (continuar con suscripción mensual):**
- Todos los criterios de éxito cumplidos
- Cliente firma autorización para continuar
- Se acuerda plan de suscripción mensual (desde USD 300/mes)

**No-Go:**
- Si algún criterio crítico no se cumplió por causas técnicas de DataBision → se extiende el piloto 15 días sin costo adicional
- Si el cliente decide no continuar → se entrega el runbook completo, se desconecta el extractor, se eliminan los datos del cliente

---

## 8. Propiedad y confidencialidad

- Los datos SAP extraídos son propiedad exclusiva del cliente
- DataBision no comparte ni vende datos de clientes con terceros
- Las credenciales SAP del cliente se eliminan al finalizar el servicio
- Se recomienda firmar un NDA simple antes del inicio

---

## Firma de aceptación

| | Cliente | DataBision |
|---|---|---|
| Nombre | _________________________ | Jonathan Campillay |
| Cargo | _________________________ | Founder |
| Fecha | _________________________ | _____________ |
| Firma | _________________________ | _____________ |

---

**Contacto:** campillayparedes@gmail.com
