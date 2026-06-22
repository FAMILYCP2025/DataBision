# Native BI Finance — Playbook de Clasificación PCGE (Perú)

**Sprint 30 · DataBision · Junio 2026**

> **ADVERTENCIA:** Este documento es una guía general basada en el PCGE Perú estándar. Nunca aplicar una clasificación sin confirmación del contador responsable del cliente. Cada empresa puede tener particularidades en la aplicación de su plan de cuentas.

---

## Estructura del PCGE Perú

El Plan Contable General Empresarial (PCGE) de Perú organiza las cuentas en 9 elementos:

| Elemento | Rango | Categoría general | Aparece en |
|---|---|---|---|
| 1 | 10–19 | Activo corriente | Balance — Activos |
| 2 | 20–29 | Existencias | Balance — Activos |
| 3 | 30–39 | Activo no corriente e inversiones | Balance — Activos |
| 4 | 40–49 | Pasivos | Balance — Pasivos |
| 5 | 50–59 | Patrimonio | Balance — Patrimonio |
| 6 | 60–69 | Gastos por naturaleza | P&L — Gastos |
| 7 | 70–79 | Ingresos | P&L — Ingresos |
| 8 | 80–89 | Saldos intermediarios y resultados | Excluir / Cuadre |
| 9 | 90–99 | Contabilidad analítica de explotación | Excluir / Analítico |

---

## Elemento 1 — Activo Corriente (10–19)

| Cuentas | Descripción | Clasificación DataBision |
|---|---|---|
| 10 | Efectivo y equivalentes de efectivo | `current_asset` |
| 11 | Inversiones financieras | `current_asset` |
| 12 | Cuentas por cobrar comerciales – Terceros | `current_asset` |
| 13 | Cuentas por cobrar comerciales – Relacionadas | `current_asset` |
| 14 | Cuentas por cobrar al personal | `current_asset` |
| 16 | Cuentas por cobrar diversas – Terceros | `current_asset` |
| 17 | Cuentas por cobrar diversas – Relacionadas | `current_asset` |
| 18 | Servicios y otros contratados por anticipado | `current_asset` |
| 19 | Estimación de cuentas de cobranza dudosa | `current_asset` (negativo) |

> **Advertencia 12/13:** Si las cuentas por cobrar tienen más de 12 meses, pueden ir a activo no corriente. El contador decide.

---

## Elemento 2 — Existencias (20–29)

| Cuentas | Descripción | Clasificación DataBision |
|---|---|---|
| 20 | Mercaderías | `inventory` (dentro de `current_asset`) |
| 21 | Productos terminados | `inventory` |
| 22 | Subproductos, desechos y desperdicios | `inventory` |
| 23 | Productos en proceso | `inventory` |
| 24 | Materias primas | `inventory` |
| 25 | Materiales auxiliares, suministros y repuestos | `inventory` |
| 26 | Envases y embalajes | `inventory` |
| 27 | Activos no corrientes mantenidos para la venta | `non_current_asset` |
| 28 | Existencias por recibir | `inventory` |
| 29 | Desvalorización de existencias | `inventory` (negativo) |

---

## Elemento 3 — Activo No Corriente (30–39)

| Cuentas | Descripción | Clasificación DataBision |
|---|---|---|
| 30 | Inversiones mobiliarias | `non_current_asset` |
| 31 | Inversiones inmobiliarias | `non_current_asset` |
| 32 | Activos adquiridos en arrendamiento financiero | `non_current_asset` |
| 33 | Inmuebles, maquinaria y equipo | `non_current_asset` |
| 34 | Intangibles | `non_current_asset` |
| 35 | Activos biológicos | `non_current_asset` |
| 36 | Desvalorización de activo inmovilizado | `non_current_asset` (negativo) |
| 37 | Activo diferido | `non_current_asset` |
| 38 | Otros activos | `non_current_asset` |
| 39 | Depreciación, amortización y agotamiento acumulados | `non_current_asset` (negativo) |

---

## Elemento 4 — Pasivos (40–49)

| Cuentas | Descripción | Clasificación DataBision |
|---|---|---|
| 40 | Tributos, contraprestaciones y aportes por pagar | `current_liability` |
| 41 | Remuneraciones y participaciones por pagar | `current_liability` |
| 42 | Cuentas por pagar comerciales – Terceros | `current_liability` |
| 43 | Cuentas por pagar comerciales – Relacionadas | `current_liability` |
| 44 | Cuentas por pagar a los accionistas | `long_term_liability` o `current_liability` |
| 45 | Obligaciones financieras | `long_term_liability` |
| 46 | Cuentas por pagar diversas – Terceros | `current_liability` |
| 47 | Cuentas por pagar diversas – Relacionadas | `current_liability` |
| 48 | Provisiones | `current_liability` o `long_term_liability` |
| 49 | Pasivo diferido | `long_term_liability` |

> **Advertencia 44:** Cuentas por pagar a accionistas pueden ser corriente o no corriente según el plazo. El contador decide.
> **Advertencia 45:** Si tiene parte corriente (vence < 12 meses), debe separarse. El contador define el tratamiento.

---

## Elemento 5 — Patrimonio (50–59)

| Cuentas | Descripción | Clasificación DataBision |
|---|---|---|
| 50 | Capital | `equity` |
| 51 | Acciones de inversión | `equity` |
| 52 | Capital adicional | `equity` |
| 53 | Excedente de revaluación | `equity` |
| 56 | Resultados no realizados | `equity` |
| 57 | Excedente de revaluación (reservas) | `equity` |
| 58 | Reservas | `equity` |
| 59 | Resultados acumulados | `equity` |

---

## Elemento 6 — Gastos por Naturaleza (60–69)

| Cuentas | Descripción | Clasificación DataBision |
|---|---|---|
| 60 | Compras | `cogs` (costo de ventas directo) |
| 61 | Variación de existencias | `cogs` |
| 62 | Gastos de personal | `opex` |
| 63 | Gastos de servicios prestados por terceros | `opex` |
| 64 | Gastos por tributos | `opex` |
| 65 | Otros gastos de gestión | `opex` |
| 66 | Pérdida por medición de activos no financieros | `other_expense` |
| 67 | Gastos financieros | `financial_expense` |
| 68 | Valuación y deterioro de activos y provisiones | `opex` (depreciación: `depreciation`) |
| 69 | Costo de ventas | `cogs` |

> **Nota 68:** Las subcuentas de depreciación (681x) y amortización (682x) se clasifican como `depreciation`/`amortization` para el cálculo del EBITDA. Confirmar con el contador cuáles son exactamente.

> **ADVERTENCIA 60/69:** El PCGE usa cuentas 60–69 como gastos por naturaleza. Muchas empresas SAP B1 en Perú también usan 69 como costo de ventas directamente. El contador define cuál es COGS y cuál es OPEX en su empresa específica.

---

## Elemento 7 — Ingresos (70–79)

| Cuentas | Descripción | Clasificación DataBision |
|---|---|---|
| 70 | Ventas | `revenue` |
| 71 | Variación de la producción almacenada | `revenue` |
| 72 | Producción de activo inmovilizado | `other_income` |
| 73 | Descuentos, rebajas y bonificaciones obtenidos | `revenue` (positivo) |
| 74 | Descuentos, rebajas y bonificaciones concedidos | `revenue` (negativo) |
| 75 | Otros ingresos de gestión | `other_income` |
| 76 | Ganancia por medición de activos no financieros | `other_income` |
| 77 | Ingresos financieros | `financial_income` |
| 78 | Cargas cubiertas por provisiones | `other_income` |
| 79 | Cargas imputables a cuentas de costos | `exclude` (ver nota) |

> **Nota 79:** Las cuentas 79x son "cargas imputables a costos" — son contrapartida de cuentas 6x y 9x. NO deben sumarse al P&L directamente. Clasificar como `exclude` para evitar doble contabilización. Confirmar con el contador.

---

## Elemento 8 — Saldos Intermediarios (80–89)

| Cuentas | Descripción | Clasificación DataBision |
|---|---|---|
| 80–89 | Saldos y resultados intermediarios | **`exclude`** |

> **CRÍTICO:** Las cuentas del elemento 8 son cuentas de cuadre y resultados intermediarios del PCGE. Incluirlas en el P&L produciría doble contabilización. SIEMPRE clasificar como `exclude` salvo instrucción explícita del contador.

---

## Elemento 9 — Contabilidad Analítica (90–99)

| Cuentas | Descripción | Clasificación DataBision |
|---|---|---|
| 90 | Costo de producción | `analytical` / `exclude` |
| 91–98 | Centros de costo y distribución | `analytical` / `exclude` |
| 99 | Margen de contribución / resultados | `analytical` / `exclude` |

> **ADVERTENCIA:** Las cuentas 9x son analíticas y NO forman parte de los estados financieros principales. Clasificar como `exclude`. Si el cliente quiere un análisis de centros de costo, se agrega como módulo adicional en futuras versiones.

---

## Checklist de clasificación por elemento

- [ ] Elemento 1 (10–19): activos corrientes clasificados
- [ ] Elemento 2 (20–29): inventarios clasificados
- [ ] Elemento 3 (30–39): activos no corrientes clasificados
- [ ] Elemento 4 (40–49): pasivos clasificados (corriente vs. no corriente confirmado)
- [ ] Elemento 5 (50–59): patrimonio clasificado
- [ ] Elemento 6 (60–69): gastos — COGS vs. OPEX vs. Financiero confirmado con contador
- [ ] Elemento 7 (70–79): ingresos clasificados, cuenta 79 excluida
- [ ] Elemento 8 (80–89): excluidos completamente
- [ ] Elemento 9 (90–99): excluidos completamente
- [ ] Cuentas de depreciación/amortización (681x, 682x) etiquetadas para EBITDA
- [ ] Cero cuentas sin clasificar en refresh-status
