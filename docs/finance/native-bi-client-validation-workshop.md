# Native BI Finance — Taller de Validación Financiera con Cliente

**Sprint 30 · DataBision · Junio 2026**  
**Duración:** 90 minutos  
**Participantes:** Contador / Jefe Contabilidad + CFO / Gerente Financiero + Consultor DataBision

---

## Objetivo del taller

Que el contador y el gerente financiero del cliente revisen y aprueben los estados financieros generados por DataBision, comparándolos con los reportes SAP que ya conocen. Al finalizar, el contador firma el documento de validación.

---

## Preparación previa (DataBision — 1 hora antes)

- [ ] Dashboard DataBision con datos del período de referencia acordado
- [ ] Todos los endpoints respondiendo HTTP 200
- [ ] refresh-status con healthScore ≥ 95
- [ ] Sin cuentas sin clasificar (`unclassifiedAccounts = 0`)
- [ ] Template de validación impreso o abierto: [native-bi-financial-validation-template.md](native-bi-financial-validation-template.md)
- [ ] Período de validación confirmado (ej: Diciembre 2025)
- [ ] SAP B1 disponible para comparación (del lado del cliente)

---

## Agenda del taller (90 minutos)

### Bloque 1 — Contexto y período (10 min)

**Facilitador:** Consultor DataBision

Puntos:
- Confirmar período de validación: ¿cuál es el mes de referencia?
- Confirmar que el contador tiene los reportes SAP del mismo período
- Explicar qué se va a revisar: P&L, Balance, EBITDA
- Explicar que las diferencias son esperadas hasta clasificar correctamente

**Pregunta de apertura:**
> "¿Tienen el Estado de Resultados de [período] a mano para que podamos comparar?"

---

### Bloque 2 — Revisión P&L (25 min)

**Pantalla compartida:** DataBision → Finance → Estado de Resultados

**Secuencia:**

**Paso 1 — Estructura general:**
- ¿Las categorías del P&L (Ingresos, COGS, Gastos, Utilidad) hacen sentido para la empresa?
- ¿El formato es comparable al que usa el contador?

**Paso 2 — Total de ingresos:**
- DataBision muestra: [MONTO INGRESOS]
- Contador/SAP muestra: [MONTO REFERENCIA]
- Diferencia: [DIFERENCIA]
- Si hay diferencia: ¿qué cuenta podría explicarla?

**Paso 3 — Costo de ventas:**
- DataBision muestra: [MONTO COGS]
- Contador/SAP muestra: [MONTO REFERENCIA]
- Preguntar: ¿todas las cuentas de costo están incluidas?

**Paso 4 — Gastos operativos:**
- Revisar las categorías principales: personal, servicios, otros
- ¿Hay gastos que no aparecen o que aparecen donde no corresponden?

**Paso 5 — Utilidad neta:**
- Comparar utilidad neta con el reporte del contador
- Calcular diferencia porcentual
- Si < 0.01%: aceptable
- Si > 0.01%: identificar causa antes de continuar

**Acuerdos a registrar en este bloque:**
- Diferencias encontradas y sus causas
- Cuentas a reclasificar
- Fecha de resolución de ajustes

---

### Bloque 3 — Revisión Balance (20 min)

**Pantalla compartida:** DataBision → Finance → Balance General

**Paso 1 — Cuadre general:**
- ¿Total Activos = Total Pasivos + Patrimonio?
- Si no cuadra: ¿hay diferencia grande o pequeña?

**Paso 2 — Activos corrientes:**
- ¿El efectivo y cuentas por cobrar parecen correctos?
- ¿Inventarios incluidos correctamente?

**Paso 3 — Activos no corrientes:**
- ¿Inmuebles, maquinaria y equipos con su depreciación acumulada?
- ¿Intangibles correctamente incluidos?

**Paso 4 — Pasivos:**
- ¿Cuentas por pagar a proveedores correctas?
- ¿Préstamos bancarios incluidos?
- ¿Obligaciones tributarias?

**Paso 5 — Patrimonio:**
- ¿Capital y reservas correctos?
- ¿Resultado del ejercicio coincide con el del P&L?

**Regla de oro del balance:**
> Si el resultado del ejercicio en el Balance es diferente a la Utilidad Neta del P&L → hay una cuenta que está en un estado y no en el otro. El contador identifica cuál.

---

### Bloque 4 — Revisión EBITDA (15 min)

**Pantalla compartida:** DataBision → Finance → EBITDA

**Paso 1 — Fórmula aplicada:**
- DataBision calcula: Utilidad Operativa + Depreciación + Amortización
- ¿Las cuentas de depreciación (681x) y amortización (682x) están incluidas?

**Paso 2 — Comparación:**
- EBITDA DataBision: [MONTO]
- EBITDA del contador (si lo tiene calculado): [MONTO]
- ¿La diferencia es razonable?

**Paso 3 — Si el cliente no tiene EBITDA previo:**
- El contador confirma si el EBITDA parece razonable en relación al tamaño y tipo de negocio
- Si la empresa no usa EBITDA: documentar y omitir en el dashboard si el cliente lo prefiere

---

### Bloque 5 — Identificación y resolución de diferencias (10 min)

**Para cada diferencia identificada:**

1. Anotar en el template de validación
2. Identificar causa probable:
   - Cuenta sin clasificar → clasificar y re-ejecutar
   - Cuenta en categoría incorrecta → reclasificar
   - Período diferente → verificar rango de fechas
   - Asiento en SAP no extraído → re-extraer
3. Acordar con el contador la acción correctiva
4. Acordar plazo de resolución: ¿en esta sesión o en 24 horas?

**Si los ajustes son simples (reclasificación):**
- DataBision los aplica durante la sesión y re-ejecuta el MART en vivo
- Se revisa el resultado inmediatamente

**Si los ajustes requieren más análisis:**
- Documentar y resolver en las siguientes 24–48 horas
- Agendar sesión de revisión de resultado

---

### Bloque 6 — Acuerdos y firma de aceptación (10 min)

**Documentar:**
- Diferencias resueltas durante la sesión
- Diferencias pendientes con fecha de resolución
- Cuentas que el contador decidió excluir
- Clasificaciones especiales acordadas

**Firma del documento de validación:**
- El contador firma el [native-bi-financial-validation-template.md](native-bi-financial-validation-template.md)
- La firma indica que la clasificación contable es aceptada — no que los datos son perfectos
- Si hay diferencias pendientes: firma condicional con nota de pendientes

**Mensaje de cierre:**
> "Con esto queda la clasificación contable aprobada. Los ajustes pendientes los resolvemos en [plazo]. A partir de mañana el dashboard se actualiza automáticamente todos los días."

---

## Post-taller (DataBision)

- [ ] Aplicar todos los ajustes de clasificación acordados
- [ ] Re-ejecutar MART refresh completo
- [ ] Verificar que unclassifiedAccounts = 0
- [ ] Enviar captura del refresh-status con healthScore actualizado
- [ ] Enviar documento de validación firmado digitalizado
- [ ] Confirmar fecha de go-live del dashboard

---

## Registro de diferencias del taller

*(Completar durante la sesión)*

| Línea | DataBision | SAP/Contador | Diferencia | Causa | Acción | Responsable | Plazo |
|---|---|---|---|---|---|---|---|
| Ingresos | | | | | | | |
| COGS | | | | | | | |
| Gastos | | | | | | | |
| Utilidad Neta | | | | | | | |
| Total Activos | | | | | | | |
| Total Pasivos | | | | | | | |
| Patrimonio | | | | | | | |
| EBITDA | | | | | | | |
