# Native BI Finance — Plan de Implementación 10 Días Hábiles

**Sprint 29 · DataBision · Junio 2026**

---

## Resumen del cronograma

| Días | Fase | Actividad principal | Participantes |
|---|---|---|---|
| 1 | Acceso | Credenciales SAP + test de conexión | TI Cliente + DataBision |
| 2 | Configuración | Perfil de conexión + validación E2E | DataBision |
| 3–4 | Extracción | OACT + OJDT + JDT1 primera carga | DataBision |
| 5–6 | Clasificación | Mapeo contable con el contador | Contador + DataBision |
| 7–8 | Validación interna | P&L, Balance, EBITDA validados internamente | DataBision |
| 9–10 | Validación cliente | Taller financiero + ajustes + capacitación | Cliente + DataBision |
| 11–30 | Operación | Dashboard en vivo, monitoreo, soporte | DataBision |

---

## Día 1 — Acceso y test de conexión

**Objetivo:** DataBision puede conectarse exitosamente a SAP Service Layer del cliente.

**Actividades:**
- Recibir credenciales SAP por canal seguro
- Almacenar password en variable de entorno `SAP_PASSWORD_[CLIENTE]`
- Crear perfil de conexión `NativeBiConnectionProfile` para el cliente
- Ejecutar test de conexión contra SAP Service Layer
- Verificar que `GET /ChartOfAccounts?$top=1` retorna datos
- Verificar que `GET /JournalEntries?$top=1` retorna datos
- Confirmar que el extractor puede alcanzar la DataBision API

**Entregable del día:** Email al cliente confirmando que la conexión SAP está operativa.

**Bloqueos posibles:**
- Firewall no abierto → TI cliente lo resuelve (máximo 1 día)
- SSL inválido → documentar excepción si es TST, escalar si es PRD
- Credenciales incorrectas → solicitar nuevo password por canal seguro

---

## Día 2 — Perfil de conexión y validación end-to-end

**Objetivo:** Pipeline completo funcionando: SAP → extractor → API → Supabase.

**Actividades:**
- Configurar `NativeBiConnectionProfile` completo en el sistema
- Registrar el cliente en la base de datos (`company_id`, `slug`, `company_name`)
- Ejecutar extracción de prueba OACT: `--profile [cliente] --object OACT --top 5`
- Verificar que los datos llegan a la tabla RAW de Supabase
- Ejecutar extracción de prueba OJDT: `--profile [cliente] --object OJDT --top 5`
- Verificar GET individual `JournalEntries(N)` para 3 asientos de prueba
- Llamar endpoint `/api/client/bi/finance/refresh-status` y verificar HTTP 200
- Confirmar que `resolve-profile` retorna el perfil correcto

**Entregable del día:** Pipeline E2E validado. Captura de pantalla del refresh-status con status "healthy".

---

## Días 3–4 — Extracción inicial completa

**Objetivo:** Todos los datos históricos del período de validación cargados en Supabase.

**Día 3 — Extracción OACT + OJDT:**
- Extracción OACT completa (todas las cuentas)
- Verificar número de cuentas extraídas vs. plan de cuentas SAP (aproximado)
- Extracción OJDT del período de validación acordado (ej: 12 meses)
- Monitorear progreso — para períodos largos puede tomar varias horas
- Verificar en Supabase: `raw.sap_oact` y `raw.sap_ojdt` con datos

**Día 4 — JDT1 y primera MART:**
- Confirmar que JDT1 fue extraído correctamente via GET individual JournalEntries(N)
- Verificar `raw.sap_jdt1` con líneas de asientos
- Ejecutar MART refresh: `POST /api/admin/bi/finance/refresh-mart`
- Verificar que `mart.finance_income_statement`, `balance_sheet`, `ebitda` tienen datos
- Revisar endpoint `refresh-status`: esperar healthScore ≥ 90

**Entregable del día 4:** Primer P&L generado automáticamente. Enviar captura al cliente.

---

## Días 5–6 — Clasificación contable con el contador

**Objetivo:** Todas las cuentas del plan de cuentas clasificadas correctamente según PCGE.

**Preparación:**
- Exportar lista de todas las cuentas OACT extraídas con su clasificación actual
- Identificar cuentas sin clasificar y cuentas con clasificación dudosa
- Preparar vista en spreadsheet para la sesión

**Sesión con el contador (2 horas — Día 5):**
- Revisar categorías PCGE disponibles
- Clasificar cuenta por cuenta las cuentas sin clasificar
- Verificar clasificación de cuentas de ingresos (70–79)
- Verificar clasificación de cuentas de gastos (60–69)
- Verificar cuentas de activos (10–39) y pasivos (40–49)
- Documentar cuentas de orden (80–89) y analíticas (90–99) — no suelen ir al P&L
- Anotar cualquier cuenta que requiera criterio especial del contador

**Día 6 — Aplicar clasificación:**
- Cargar la clasificación acordada en la tabla de reglas del sistema
- Re-ejecutar MART refresh
- Verificar que el P&L muestra todas las líneas correctamente
- Verificar que no hay cuentas sin clasificar en el refresh-status

**Entregable del día 6:** Clasificación completa aplicada. healthScore = 100.

---

## Días 7–8 — Validación interna DataBision

**Objetivo:** DataBision valida internamente que los estados financieros son correctos antes de presentarlos al cliente.

**Día 7:**
- Revisar P&L generado: ¿los montos tienen sentido para el tipo de empresa?
- Verificar que ingresos > 0 y gastos > 0 (si hay asientos del período)
- Verificar que el Balance cuadra: Activos = Pasivos + Patrimonio (tolerancia < 0.01%)
- Verificar EBITDA: ¿el cálculo es razonable?
- Buscar anomalías: montos negativos donde no corresponden, líneas vacías

**Día 8:**
- Documentar hallazgos internos
- Si hay inconsistencias: revisar clasificación con el contador antes de presentar al cliente
- Preparar presentación del taller de validación con el cliente
- Confirmar fecha del taller financiero (Día 9)

**Entregable del día 8:** Validación interna completa. Dashboard listo para presentar al cliente.

---

## Días 9–10 — Validación con cliente y capacitación

**Día 9 — Taller de validación financiera (90 minutos):**
- Ver [native-bi-client-validation-workshop.md](../finance/native-bi-client-validation-workshop.md)
- Revisar P&L con el contador y gerente financiero
- Revisar Balance con el contador
- Revisar EBITDA
- Documentar diferencias y resolver en sesión o en 24 horas
- Contador firma documento de aceptación de clasificación

**Día 10 — Capacitación y entrega:**
- Sesión de capacitación para usuarios finales (2 horas)
- Demostrar cómo leer el P&L, Balance y EBITDA
- Mostrar cómo interpretar el refresh-status
- Entregar runbook operativo
- Configurar scheduler automático (Windows o Linux)
- Confirmar que el refresh diario está programado y corriendo
- Entrega formal: email de "go-live" al cliente

**Entregable del día 10:** Dashboard en producción. Scheduler configurado. Runbook entregado.

---

## Días 11–30 — Operación del piloto

- Dashboard en vivo — cliente accede diariamente
- DataBision monitorea refresh-status diariamente
- DataBision responde consultas por email en máximo 24 horas hábiles
- Si hay ajustes de clasificación: se aplican en 48 horas
- Al día 30: reunión Go/No-Go para evaluar continuidad con suscripción mensual

---

## Notas de seguimiento

| Día | Estado | Entregable | Bloqueadores |
|---|---|---|---|
| 1 | Pendiente | Conexión SAP OK | |
| 2 | Pendiente | Pipeline E2E OK | |
| 3 | Pendiente | OACT + OJDT extraídos | |
| 4 | Pendiente | Primer MART OK | |
| 5–6 | Pendiente | Clasificación completa | |
| 7–8 | Pendiente | Validación interna OK | |
| 9 | Pendiente | Taller validación OK | |
| 10 | Pendiente | Go-live + capacitación | |
