# Native BI — Clasificación Contable por Cliente

Sprint 14B — 2026-06-18

---

## Qué es la clasificación contable

Las cuentas SAP B1 se clasifican en `statement_line` para que el ETL pueda construir:
- Estado de Resultados (P&L)
- Balance General
- EBITDA

Sin clasificación, todas las cuentas aparecen como `unclassified` y los informes financieros quedan vacíos o incorrectos.

---

## Valores permitidos de `statement_line`

| Valor | Informe | Descripción |
|---|---|---|
| `revenue` | P&L | Ingresos por ventas y servicios |
| `cogs` | P&L | Costo de ventas (costo de bienes vendidos) |
| `opex` | P&L | Gastos operacionales (sueldos, arriendos, servicios) |
| `other_income` | P&L | Ingresos no operacionales |
| `other_expense` | P&L | Gastos no operacionales |
| `financial` | P&L | Resultado financiero neto (ingresos/gastos financieros) |
| `tax` | P&L | Impuesto a la renta y similares |
| `depreciation` | P&L | Depreciación (habilita D&A en EBITDA) |
| `amortization` | P&L | Amortización (habilita A en EBITDA) |
| `current_assets` | Balance | Activos corrientes (efectivo, cuentas por cobrar, inventario) |
| `non_current_assets` | Balance | Activos fijos, intangibles |
| `current_liabilities` | Balance | Pasivos corrientes (proveedores, cuentas por pagar) |
| `non_current_liabilities` | Balance | Deuda largo plazo |
| `equity` | Balance | Patrimonio neto |
| `unclassified` | — | Sin clasificar (no aparece en informes) |

---

## Cómo clasificar — 3 niveles de prioridad

### Nivel 1: Código de cuenta exacto
Aplica cuando se conoce el código exacto de la cuenta.

Ejemplo: `account_code = "41000001"`, `statement_line = "revenue"`

### Nivel 2: Prefijo de FormatCode
Aplica a todas las cuentas cuyo FormatCode empieza con el prefijo dado.

Ejemplo: `format_code = "41"`, `statement_line = "revenue"` → clasifica todas las cuentas con FormatCode que comience en "41"

### Nivel 3: Fallback por tipo SAP (automático)
Si no hay regla, el ETL aplica el fallback según `AccountType`:

| AccountType | statement_line asignado |
|---|---|
| A | current_assets |
| L | current_liabilities |
| E | equity |
| R | revenue |
| X | opex |
| N | cogs |

⚠️ El fallback por tipo SAP es solo un punto de partida. Muchos clientes tienen cuentas mal tipificadas o con tipos no estándar. Siempre revisar con contador.

---

## Clasificación típica P&L (ejemplo Chile — NO usar como verdad universal)

```
Prefijo   → statement_line
------    ----------------
41*       → revenue
51*       → cogs
52*–59*   → opex
71*       → other_income
81*       → financial
91*       → tax
```

⚠️ **Este es un ejemplo genérico.** El plan de cuentas varía entre empresas, países e industrias. Siempre obtener el plan de cuentas real del cliente y validar con su contador o CFO.

---

## Clasificación típica Balance (ejemplo)

```
Prefijo   → statement_line
------    ----------------
11*       → current_assets
12*       → non_current_assets
21*       → current_liabilities
22*       → non_current_liabilities
31*       → equity
```

---

## API Endpoints (SuperAdmin)

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/admin/companies/{id}/native-bi/account-classification-rules` | Lista reglas |
| POST | `/api/admin/companies/{id}/native-bi/account-classification-rules` | Crea regla |
| PUT | `/api/admin/companies/{id}/native-bi/account-classification-rules/{ruleId}` | Actualiza regla |
| DELETE | `/api/admin/companies/{id}/native-bi/account-classification-rules/{ruleId}` | Elimina regla |
| POST | `/api/admin/companies/{id}/native-bi/account-classification-rules/import-template` | Sugiere reglas desde OACT |

**Validaciones del endpoint:**
- `statement_line` debe ser uno de los valores de la allowlist
- `account_code` o `format_code` debe estar presente (no ambos vacíos)
- No se permiten valores SQL arbitrarios
- Solo SuperAdmin puede administrar reglas

---

## Flujo de trabajo recomendado

1. Extraer OACT del cliente: `dotnet run -- --object OACT --send`
2. Abrir SuperAdmin → CompanyDetailPage → Native BI → Clasificación contable
3. Usar "Sugerencias desde OACT" para ver cuentas sin clasificar
4. Revisar con contador del cliente
5. Aplicar reglas aprobadas
6. Ejecutar `mart.refresh_accounting_all('company_id')` en Supabase
7. Verificar en FinanceDashboardPage que los informes muestran datos reales

---

## Checklist con contador del cliente

- [ ] Solicitar plan de cuentas completo (reporte SAP B1 → Contabilidad → Plan de Cuentas)
- [ ] Identificar cuentas de ingreso principal
- [ ] Identificar cuentas de costo de ventas
- [ ] Identificar cuentas de gastos operacionales
- [ ] Identificar cuentas de resultado financiero
- [ ] Identificar cuentas de impuesto
- [ ] Identificar cuentas de activo corriente / no corriente
- [ ] Identificar cuentas de pasivo corriente / largo plazo
- [ ] Identificar cuentas de patrimonio
- [ ] Confirmar que EBITDA es correcto antes de presentar a gerencia
- [ ] Confirmar que Balance cuadra (activos = pasivos + patrimonio)

---

## Limitaciones actuales

- D&A (depreciación y amortización) son 0 en EBITDA hasta que el cliente configure cuentas con `statement_line = 'depreciation'` / `'amortization'`
- Las reglas son por cliente, no globales — ninguna configuración se comparte entre empresas
- El ETL debe re-ejecutarse manualmente después de cambiar reglas
