# Native BI Finance — Metodología de Validación Financiera

**Sprint 30 · DataBision · Junio 2026**

---

## Propósito

Definir cómo validar que los estados financieros generados por DataBision son consistentes con los datos de SAP B1 y los reportes del contador. Esta metodología es la base del taller de validación con el cliente.

---

## Principios de validación

1. **El contador es la autoridad.** DataBision no decide qué es correcto contablemente — el contador del cliente decide.
2. **Tolerancia de redondeo aceptable.** Diferencias menores a 0.01% del total del período son aceptables por redondeo.
3. **Las diferencias se documentan, no se ocultan.** Si hay una diferencia, se registra con su causa y se decide con el cliente.
4. **No se fuerza la clasificación.** Si una cuenta es ambigua, el contador decide — DataBision implementa.

---

## 1. Validación del Estado de Resultados (P&L)

### Qué comparar

| Línea DataBision | Fuente SAP B1 para comparar |
|---|---|
| Total Ingresos | SAP B1 → Libro Mayor → Cuentas 70–79 |
| Costo de Ventas | SAP B1 → Libro Mayor → Cuentas 69 |
| Utilidad Bruta | Ingresos − Costo de Ventas |
| Gastos Operativos | SAP B1 → Libro Mayor → Cuentas 60–68 |
| Utilidad Operativa | Utilidad Bruta − Gastos Operativos |
| Ingresos/Gastos Financieros | SAP B1 → Libro Mayor → Cuentas 67, 77 |
| Utilidad antes de impuestos | Utilidad Operativa ± Financieros |
| Impuesto a la renta | SAP B1 → Cuentas de provisión IR |
| Utilidad Neta | Utilidad AI − Impuestos |

### Procedimiento

1. Generar el P&L en DataBision para el período de referencia acordado
2. Abrir en SAP B1: **Informes → Financiero → Estado de Ganancias y Pérdidas**
3. Comparar los totales línea por línea
4. Calcular diferencia absoluta y porcentual para cada línea
5. Si diferencia > 0.01%: identificar la causa (cuenta sin clasificar, reclasificación, etc.)

### Tolerancias

| Tipo de diferencia | Acción |
|---|---|
| < 0.01% del total del período | Aceptable — redondeo |
| 0.01% – 1% | Documentar causa, validar con contador |
| > 1% | Investigar causa, ajustar clasificación antes de aceptar |
| Diferencia exacta = monto de una cuenta | Cuenta sin clasificar — clasificar y re-ejecutar |

---

## 2. Validación del Balance General

### Qué comparar

| Sección DataBision | Fuente SAP B1 |
|---|---|
| Activos Corrientes | SAP → Libro Mayor → Cuentas 10–14 |
| Activos No Corrientes | SAP → Libro Mayor → Cuentas 15–39 |
| Total Activos | Suma de todos los activos |
| Pasivos Corrientes | SAP → Libro Mayor → Cuentas 40–44 |
| Pasivos No Corrientes | SAP → Libro Mayor → Cuentas 45–49 |
| Total Pasivos | Suma de todos los pasivos |
| Patrimonio | SAP → Libro Mayor → Cuentas 50–59 |
| Total Pasivos + Patrimonio | Debe ser igual a Total Activos |

### Validación de cuadre

El sistema DataBision valida automáticamente:
```
Total Activos = Total Pasivos + Patrimonio
```

Si la diferencia > 0.01%:
1. Identificar qué cuentas tienen clasificación incorrecta
2. Verificar que las cuentas 80–89 (cuentas de orden) NO estén en el balance
3. Verificar que las cuentas analíticas 90–99 NO estén en el balance
4. Revisar con el contador las cuentas que más contribuyen a la diferencia

### Indicadores de alerta en el balance

- Patrimonio negativo: posible si la empresa tiene pérdidas acumuladas — verificar con contador
- Activos negativos: muy inusual — revisar clasificación de esa cuenta
- Pasivos negativos: posible anticipo de clientes — verificar clasificación

---

## 3. Validación del EBITDA

### Qué validar

```
EBITDA = Utilidad Operativa + Depreciación + Amortización
```

| Componente | Fuente en DataBision | Fuente en SAP B1 |
|---|---|---|
| Utilidad Operativa | Tomado del P&L | Libro Mayor / Estado de Resultados |
| Depreciación | Cuentas de depreciación (68.1x PCGE) | Informe de Activos Fijos |
| Amortización | Cuentas de amortización (68.2x PCGE) | Informe de Activos Intangibles |

### Procedimiento

1. El contador identifica qué cuentas son depreciación y amortización en el PCGE del cliente
2. Se clasifican esas cuentas como `depreciation` o `amortization` en DataBision
3. Se re-ejecuta el MART y se verifica el EBITDA calculado
4. Se compara con el EBITDA que el contador calcularía manualmente

---

## 4. Validación del Chart of Accounts (OACT)

### Qué validar

1. Número de cuentas extraídas ≈ número de cuentas en SAP B1
2. No hay cuentas duplicadas
3. La jerarquía de cuentas es correcta (cuentas padre / hija)
4. Todas las cuentas de nivel hoja tienen clasificación asignada

### Cómo verificar en SAP B1

- SAP B1 → **Maestros → Plan de Cuentas**
- Comparar cantidad total de cuentas
- Buscar cuentas que DataBision lista como "sin clasificar" y verificar en SAP qué categoría corresponde

---

## 5. Validación de la clasificación PCGE

Ver [native-bi-pcge-classification-playbook.md](native-bi-pcge-classification-playbook.md) para las reglas de clasificación.

### Procedimiento de revisión de cuentas

1. Exportar todas las cuentas de DataBision con su clasificación actual
2. Ordenar por rango de código (10–19, 20–29, etc.)
3. Revisar con el contador que cada rango está correctamente clasificado
4. Marcar cuentas que necesitan reclasificación
5. Aplicar cambios en el sistema
6. Re-ejecutar MART y re-validar

---

## 6. Reconciliación contra SAP B1

### Método de reconciliación

Para reconciliar un período específico:

1. En DataBision: exportar movimientos del período por cuenta
2. En SAP B1: Informe → Libro Mayor → filtrar mismo período
3. Comparar saldo final de cada cuenta mayor
4. Identificar diferencias > PEN/USD 100 o > 0.01%

### Causas comunes de diferencias

| Causa | Síntoma | Solución |
|---|---|---|
| Cuenta sin clasificar | Diferencia exactamente igual al saldo de una cuenta | Clasificar la cuenta |
| Asiento manual en SAP no extraído | Diferencia en cuenta específica | Re-extraer OJDT del período |
| Período de extracción diferente | Diferencias en varios totales | Verificar fechas de extracción vs. período SAP |
| Diferencia de signo | Ingresos negativos o gastos positivos inesperados | Revisar convención de signo PCGE |
| Cuentas de orden incluidas | Balance no cuadra | Revisar que cuentas 80–89 estén excluidas |

---

## 7. Tratamiento de cuentas sin clasificar

Las cuentas sin clasificar (`unclassifiedAccounts > 0` en refresh-status):

1. **No se pueden ignorar:** una cuenta sin clasificar significa que sus movimientos no aparecen en ningún estado financiero
2. **El contador decide:** DataBision presenta la cuenta, el contador decide dónde va
3. **Opciones:** Income / COGS / OpEx / Financial / Asset / Liability / Equity / Order / Analytical / Exclude
4. **Exclude:** para cuentas que el contador decide no incluir (ej: cuentas temporales, ajustes internos)

---

## 8. Documentación de diferencias

Usar el template en [native-bi-financial-validation-template.md](native-bi-financial-validation-template.md).

Cada diferencia documentada debe incluir:
- Período
- Línea del estado financiero
- Monto DataBision
- Monto SAP / Contador
- Diferencia absoluta y porcentual
- Causa identificada
- Acción correctiva
- Responsable
- Fecha de resolución
