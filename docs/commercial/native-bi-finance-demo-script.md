# Native BI — Guión de Demo Financiero (10 minutos)

Sprint 14E — 2026-06-18

Audiencia: CFO, Gerente de Finanzas, Controller

---

## Antes de la demo

**Pre-requisitos técnicos:**
- [ ] OACT, OJDT, JDT1 extraídos (verificar `readinessStatus = "ready"` en panel de resumen)
- [ ] `mart.refresh_accounting_all('company-id')` ejecutado exitosamente
- [ ] Reglas de clasificación contable validadas con contador del cliente
- [ ] Balance cuadra (Activos = Pasivos + Patrimonio)
- [ ] Revenue positivo en períodos recientes

**Abrir en el navegador:**
1. Portal del cliente → Finanzas
2. SuperAdmin → Empresa → Native BI (para mostrar configuración si preguntan)

---

## Guión de demo

### Minuto 0–1 · Apertura

> "Vamos a ver el módulo financiero de DataBision para [Empresa Cliente]. Lo que están viendo es data en tiempo real de su SAP Business One — sin exportaciones a Excel, sin consolidaciones manuales, sin espera."

**Mostrar:** Tab `Resumen` → KPIs principales (CxC, CxP, riesgo +90d).

**Punto clave:** "Estos números se actualizan automáticamente cuando SAP registra una factura o pago."

---

### Minuto 1–3 · Cartera de cobranzas

> "Empecemos por lo que más impacta el flujo de caja: la cartera."

**Navegar:** Tab `Cuentas por cobrar`

1. Mostrar tabla de clientes ordenada por `Vencido DESC`
2. Filtrar por aging > 90 días → Tab `Riesgo +90d`
3. Señalar clientes críticos con exposición alta

> "¿Ven este cliente aquí? Tiene [X] pesos con más de 90 días. Sin DataBision esto requería un reporte manual del área de crédito. Aquí lo ven en tiempo real."

**Punto clave:** El color rojo/amarillo/verde es automático — no hay configuración manual.

---

### Minuto 3–5 · Estado de Resultados

> "Ahora vamos al P&L."

**Navegar:** Tab `Estado de Resultados`

1. Mostrar últimos 3 períodos
2. Señalar Revenue, Costo de Ventas, Utilidad Bruta, Margen
3. Cambiar filtro de año si hay varios períodos disponibles

> "Este estado de resultados se construye directamente desde el libro diario SAP, no desde un reporte estático. Cualquier ajuste contable en SAP aparece aquí en la próxima extracción."

**Si el margen bruto es bajo:** "Ven que el margen está en [X]%. Podemos navegar directo a las cuentas de costo para entender por qué."

---

### Minuto 5–7 · EBITDA y rentabilidad

> "Para el análisis de rentabilidad operacional, tenemos el módulo de EBITDA."

**Navegar:** Tab `EBITDA`

1. Mostrar líneas EBITDA y Utilidad Neta (últimos 12 meses)
2. Señalar tendencia
3. Si hay depreciación configurada, mostrar separación D&A

> "El EBITDA es fundamental para valorización de empresa y covenants bancarios. Normalmente esto requería semanas con el área de finanzas. Con DataBision lo tienen actualizado mensualmente, de forma automática."

---

### Minuto 7–8 · Balance General

> "Pasemos al balance."

**Navegar:** Tab `Balance General`

1. Mostrar total activos, pasivos, patrimonio
2. Verificar que cuadra (badge verde "Cuadra")
3. Mostrar detalle de categorías

> "El balance se calcula acumulando todos los asientos contables hasta la fecha de corte. Es el mismo libro mayor SAP, presentado de forma clara para la gerencia."

---

### Minuto 8–9 · Plan de Cuentas

> "Si quieren ver la granularidad completa…"

**Navegar:** Tab `Plan de Cuentas`

1. Mostrar tabla de cuentas con clasificación y saldos
2. Señalar cuentas de ingreso (badge azul) vs cuentas de gasto

> "Cada cuenta tiene su clasificación contable asignada. Esto es lo que conecta el maestro SAP con los estados financieros."

---

### Minuto 9–10 · Cierre

> "En resumen: CxC en tiempo real, P&L mensual, EBITDA por período, Balance cuadrado — todo sin salir de DataBision."

**Punto de valor diferencial:**
- "Sin Excel: los números vienen directo de SAP"
- "Sin delay: la extracción es diaria/semanal según lo configuren"
- "Sin discrepancias: un solo source of truth"

**Call to action:**
> "¿Qué período les gustaría ver con más detalle? ¿Tienen cuentas específicas que quieran revisar?"

---

## Respuestas a preguntas frecuentes

**"¿Qué pasa si hay un error contable en SAP?"**
→ El próximo ciclo de extracción lo refleja. DataBision es un espejo de SAP, no una copia fija.

**"¿Podemos personalizar las clasificaciones?"**
→ Sí, desde el panel de administración. Las clasificaciones son por empresa y validadas con el contador.

**"¿Con qué frecuencia se actualiza?"**
→ Configurable: diario, semanal, o bajo demanda. La extracción es incremental (solo cambios desde el último run).

**"¿Podemos ver el detalle de un asiento específico?"**
→ Actualmente la granularidad es a nivel de cuenta. El drill-down a nivel de asiento está en el roadmap.

**"¿Esto reemplaza nuestro ERP?"**
→ No. DataBision lee SAP pero no escribe en él. Es una capa de reporting y análisis sobre el ERP.

---

## Notas para el sales engineer

- Si el readiness panel muestra estado "bloqueado", NO hacer la demo de contabilidad — ofrecer demo con datos genéricos
- Si el balance no cuadra, ir directo a `Validaciones` para mostrar el diagnóstico y proponer resolución
- El CFO suele preguntar por EBITDA ajustado — esto requiere que el cliente configure cuentas de D&A explícitas
- En Chile: revenue suele ser negativo en el SAP en cuentas de tipo ingreso (signo contable) — verificar que el ETL invierte el signo correctamente
