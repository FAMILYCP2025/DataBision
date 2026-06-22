# Native BI Finance — Registro de Cambios de Clasificación Contable

**Cliente:** _______________________________  
**CompanyDB:** _______________________________

---

## Propósito

Registrar todos los cambios en la clasificación de cuentas contables. Este log permite:
- Auditar qué cambios se hicieron y por qué
- Revertar una clasificación si fue incorrecta
- Demostrar al cliente que los cambios fueron validados por el contador
- Servir como base para la sesión de validación anual

---

## Formato de registro

Cada cambio debe registrarse con:
- Fecha y hora
- Cuenta afectada (código y nombre en SAP)
- Clasificación anterior
- Clasificación nueva
- Motivo
- Solicitado por
- Aprobado por (contador)
- Aplicado por (DataBision)
- Impacto en estados financieros

---

## Registro de cambios

### Cambio #001

| Campo | Valor |
|---|---|
| **Fecha** | _______________________________  |
| **Cuenta SAP** | Código: _______ Nombre: _______________________ |
| **Clasificación anterior** | _______________________ |
| **Clasificación nueva** | _______________________ |
| **Motivo** | _______________________ |
| **Solicitado por** | _______________________ |
| **Aprobado por (contador)** | _______________________ |
| **Aplicado por (DataBision)** | Jonathan Campillay |
| **Fecha aplicación** | _______________________ |
| **Impacto** | P&L: ±S/ ___ | Balance: ±S/ ___ |
| **MART re-ejecutado** | ☐ Sí — fecha: _______ |

---

### Cambio #002

| Campo | Valor |
|---|---|
| **Fecha** | _______________________________  |
| **Cuenta SAP** | Código: _______ Nombre: _______________________ |
| **Clasificación anterior** | _______________________ |
| **Clasificación nueva** | _______________________ |
| **Motivo** | _______________________ |
| **Solicitado por** | _______________________ |
| **Aprobado por (contador)** | _______________________ |
| **Aplicado por (DataBision)** | Jonathan Campillay |
| **Fecha aplicación** | _______________________ |
| **Impacto** | P&L: ±S/ ___ | Balance: ±S/ ___ |
| **MART re-ejecutado** | ☐ Sí — fecha: _______ |

---

### Cambio #003

| Campo | Valor |
|---|---|
| **Fecha** | _______________________________  |
| **Cuenta SAP** | Código: _______ Nombre: _______________________ |
| **Clasificación anterior** | _______________________ |
| **Clasificación nueva** | _______________________ |
| **Motivo** | _______________________ |
| **Solicitado por** | _______________________ |
| **Aprobado por (contador)** | _______________________ |
| **Aplicado por (DataBision)** | Jonathan Campillay |
| **Fecha aplicación** | _______________________ |
| **Impacto** | P&L: ±S/ ___ | Balance: ±S/ ___ |
| **MART re-ejecutado** | ☐ Sí — fecha: _______ |

---

## Tipos de clasificación disponibles

| Código | Descripción | Aparece en |
|---|---|---|
| `revenue` | Ingresos por ventas | P&L — Ingresos |
| `other_income` | Otros ingresos | P&L — Ingresos |
| `financial_income` | Ingresos financieros | P&L — Ingresos financieros |
| `cogs` | Costo de ventas | P&L — COGS |
| `opex` | Gasto operativo | P&L — Gastos |
| `financial_expense` | Gasto financiero | P&L — Financieros |
| `depreciation` | Depreciación | P&L + EBITDA |
| `amortization` | Amortización | P&L + EBITDA |
| `other_expense` | Otros gastos | P&L — Otros |
| `current_asset` | Activo corriente | Balance — Activos |
| `inventory` | Inventarios | Balance — Activos |
| `non_current_asset` | Activo no corriente | Balance — Activos |
| `current_liability` | Pasivo corriente | Balance — Pasivos |
| `long_term_liability` | Pasivo no corriente | Balance — Pasivos |
| `equity` | Patrimonio | Balance — Patrimonio |
| `analytical` | Cuenta analítica | Excluida de estados principales |
| `exclude` | Excluida | No aparece en ningún estado |

---

## Proceso para solicitar un cambio de clasificación

1. El contador del cliente identifica una cuenta mal clasificada
2. Comunica a DataBision: cuenta + clasificación correcta
3. DataBision confirma con el contador antes de aplicar
4. DataBision aplica el cambio en la tabla `account_classification_rules`
5. DataBision ejecuta MART refresh completo
6. DataBision envía captura del estado financiero actualizado al contador
7. Contador confirma que el cambio es correcto
8. Se registra en este log

---

## Resumen de cambios por período

| Mes | Número de cambios | Aprobados por | Fecha de revisión |
|---|---|---|---|
| [Mes de implementación] | | | |
| [Mes 2] | | | |
| [Mes 3] | | | |

---

## Revisión anual de clasificación

Se recomienda revisar la clasificación completa una vez al año, preferiblemente al inicio del ejercicio fiscal:

- ¿Se agregaron cuentas nuevas en SAP que no están clasificadas?
- ¿Cambió el plan de cuentas del cliente?
- ¿Hay cuentas que el contador quiere reclasificar por cambios de criterio?
- ¿Los rangos PCGE de la empresa son consistentes con el año anterior?

Fecha de última revisión anual: _______________________________  
Próxima revisión anual programada: _______________________________
