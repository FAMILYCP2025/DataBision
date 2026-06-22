# Native BI Finance — Agenda Kickoff con Cliente Real

**Sprint 29 · DataBision · Junio 2026**  
**Duración:** 90 minutos  
**Participantes:** CFO / Gerente Financiero + TI cliente + Contador + Consultor DataBision

---

## Objetivo de la reunión

Alinear expectativas, confirmar información técnica, acordar el cronograma de 10 días y resolver dudas antes de iniciar la implementación.

---

## Agenda (90 minutos)

### Bloque 1 — Bienvenida y objetivos (10 min)

**Facilitador:** Consultor DataBision

Puntos a cubrir:
- Agradecimiento por la confianza
- Objetivo del piloto: dashboards financieros operativos en 10 días
- Resultado esperado al día 30: P&L, Balance y EBITDA validados por el contador
- Reglas de trabajo: DataBision solo lee SAP, nunca modifica

**Entregable de este bloque:** Todos los participantes alineados en el objetivo.

---

### Bloque 2 — Revisión del alcance (15 min)

**Facilitador:** Consultor DataBision

Puntos a cubrir:
- Módulo Finance: extracción de OACT, OJDT y JDT1 de SAP B1
- Dashboards incluidos: P&L, Balance General, EBITDA
- Dashboards excluidos en esta fase: Compras, Inventario, Ventas
- Clasificación contable: definida con el contador, configurable
- Refresh diario: automático, sin intervención manual
- Acceso: navegador web — sin instalar nada en equipos del cliente

**Entregable:** Alcance confirmado y exclusiones aceptadas por el cliente.

---

### Bloque 3 — Revisión técnica de acceso SAP (20 min)

**Facilitador:** Consultor DataBision  
**Participante clave:** TI / SAP Basis

Puntos a cubrir:
1. Confirmar URL de SAP Service Layer: `https://[host]:[puerto]/b1s/v1`
2. Confirmar CompanyDB
3. Estado del usuario `DATABISION_RO`: ¿creado? ¿permisos correctos?
4. Estado del certificado SSL
5. Firewall: ¿extractor puede alcanzar SAP y DataBision API?
6. Horario sin usuarios activos en SAP (para programar extracción)

**Entregable:** Checklist técnico completo o lista de pendientes con fecha de resolución.

**Si hay pendientes técnicos:** acordar fecha límite (no más de 2 días hábiles).

---

### Bloque 4 — Seguridad y protección de datos (10 min)

**Facilitador:** Consultor DataBision  
**Participante clave:** TI / Gerencia

Puntos a cubrir:
- El extractor solo hace peticiones GET al Service Layer — nunca POST/PUT/DELETE
- Las credenciales SAP no viajan al navegador ni al dashboard
- Los datos del cliente están aislados por company_id — otros clientes no pueden ver sus datos
- Al finalizar el piloto, se pueden eliminar todos los datos extraídos
- Recomendación de NDA si el cliente lo requiere
- ¿Tiene el cliente políticas de seguridad específicas que DataBision deba conocer?

**Entregable:** Inquietudes de seguridad documentadas y respondidas.

---

### Bloque 5 — Revisión contable (20 min)

**Facilitador:** Consultor DataBision  
**Participante clave:** Contador / Jefe de Finanzas

Puntos a cubrir:
1. ¿Cuántas cuentas tiene el plan de cuentas SAP?
2. ¿Hay cuentas de orden, auxiliares o cuentas analíticas (9X)?
3. ¿El cliente usa PCGE Perú estándar o tiene variaciones?
4. ¿Hay cuentas con clasificación ambigua (ej: 42 que va a gastos)?
5. Período de validación: ¿qué mes usa el contador como referencia?
6. ¿Existe un reporte P&L previo del contador para comparar?
7. Agendar sesión de clasificación: 2 horas, preferiblemente días 5–6

**Entregable:** Sesión de clasificación agendada, período de validación confirmado.

---

### Bloque 6 — Cronograma de 10 días (10 min)

**Facilitador:** Consultor DataBision

Presentar el cronograma (ver [native-bi-implementation-timeline-10-days.md](native-bi-implementation-timeline-10-days.md)):

| Día | Actividad | Participante |
|---|---|---|
| 1 | Acceso SAP, test de conexión | TI + DataBision |
| 2 | Perfil de conexión, validación end-to-end | DataBision |
| 3–4 | Extracción OACT + OJDT + JDT1 | DataBision |
| 5–6 | Sesión de clasificación contable | Contador + DataBision |
| 7–8 | Validación interna P&L, Balance, EBITDA | DataBision |
| 9–10 | Validación con cliente, ajustes, capacitación | Cliente + DataBision |
| 11–30 | Operación piloto, monitoreo, soporte | DataBision |

Confirmar disponibilidad del contador para días 5–6.

**Entregable:** Cronograma aceptado, fechas confirmadas.

---

### Bloque 7 — Entregables del piloto y criterios de éxito (5 min)

Confirmar qué entrega DataBision al finalizar el piloto:
- Dashboard P&L, Balance, EBITDA accesible en navegador
- Refresh diario automatizado
- Runbook operativo
- Documento de validación financiera firmado por el contador

Criterios de éxito del piloto (ver propuesta firmada):
- healthScore ≥ 95
- Datos de < 24 horas de antigüedad
- Balance cuadra con tolerancia < 0.01%
- Contador aprueba clasificación contable

**Entregable:** Criterios de éxito confirmados por el cliente.

---

### Preguntas y cierre (10 min)

- ¿Hay preguntas del equipo del cliente?
- Próximos pasos inmediatos: TI completa accesos, DataBision inicia Día 1
- Canal de comunicación durante la implementación: ¿WhatsApp / email / Slack?
- Frecuencia de actualización de estado: diaria por email / al final de cada fase

---

## Notas de la reunión

*(Completar durante la reunión)*

**Asistentes:**  
_______________________________

**URL Service Layer confirmada:**  
_______________________________

**CompanyDB confirmada:**  
_______________________________

**Período de validación:**  
_______________________________

**Sesión clasificación agendada:**  
_______________________________

**Pendientes técnicos:**  
_______________________________

**Fecha de inicio (Día 1):**  
_______________________________

**Canal de comunicación acordado:**  
_______________________________
