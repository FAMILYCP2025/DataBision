# Native BI Finance para SAP Business One — One Pager

**DataBision · Junio 2026**

---

## El problema

Los gerentes financieros de empresas con SAP Business One no pueden ver su P&L, Balance o EBITDA en tiempo real sin depender del área de TI, el contador o reportes manuales en Excel.

SAP B1 guarda toda la información contable —cuentas, asientos, saldos— pero no la entrega como dashboard ejecutivo. Configurar Power BI tradicional sobre SAP B1 requiere licencias adicionales, connectors, especialistas, y semanas de desarrollo. El resultado suele ser un tablero que nadie actualiza.

---

## Para quién es

- Empresas que operan SAP Business One (on-premise o HANA cloud)
- CFO / Gerente Financiero que necesita visibilidad diaria sin esperar al cierre mensual
- Empresas con contabilidad bajo PCGE (Perú) que necesitan clasificación automática
- Organizaciones que NO quieren instalar nada adicional en SAP ni contratar especialistas Power BI

---

## Qué datos extrae de SAP B1

| Objeto SAP | Tabla | Qué contiene |
|---|---|---|
| Chart of Accounts | `OACT` | Plan de cuentas con jerarquía y clasificación |
| Journal Entry Headers | `OJDT` | Cabeceras de asientos contables |
| Journal Entry Lines | `JDT1` | Líneas con débitos, créditos, cuenta y monto |

Extracción 100% de solo lectura. Sin modificar ningún registro SAP. Sin acceso a datos operativos (órdenes, facturas, inventario).

---

## Qué dashboards entrega

| Dashboard | Qué muestra |
|---|---|
| **Estado de Resultados (P&L)** | Ingresos, costos, gastos, utilidad bruta, utilidad operativa |
| **Balance General** | Activos, pasivos, patrimonio con saldos actualizados |
| **EBITDA** | Utilidad antes de impuestos, depreciación y amortización |
| **Flujo de Efectivo** | Movimientos de caja por período |
| **Clasificación Contable** | Distribución de cuentas por categoría PCGE |

---

## Qué diferencia a Native BI Finance de Power BI tradicional

| Aspecto | Power BI tradicional | Native BI Finance |
|---|---|---|
| Conector SAP | Requiere licencia + configuración | Integrado — sin licencias adicionales |
| Tiempo implementación | 4–12 semanas | 10 días hábiles |
| Modificación SAP | Puede requerir cambios | **Cero cambios en SAP** |
| Clasificación contable | Manual, cada cliente | Automática PCGE + ajustable |
| Actualización datos | Manual o batch no confiable | Diaria automática programada |
| Seguridad | Credenciales en Power BI Service | SecretRef — credenciales nunca expuestas |
| Costo mensual | Power BI Pro + SAP connector + desarrollo | Suscripción flat mensual |

---

## Tiempo de implementación

**10 días hábiles** desde la entrega de credenciales SAP:

- Días 1–2: Configuración de perfil de conexión y validación de acceso
- Días 3–4: Extracción inicial OACT + OJDT + JDT1
- Días 5–6: Clasificación contable y validación interna
- Días 7–8: Validación financiera con cliente (P&L, Balance, EBITDA)
- Días 9–10: Capacitación y entrega

---

## Resultado esperado

Al finalizar el piloto, el cliente tendrá:

- Dashboard financiero actualizado diariamente desde SAP B1
- P&L, Balance y EBITDA clasificados según PCGE Perú
- Historial de asientos contables consultable por período
- Acceso vía navegador — sin instalar nada
- Proceso de refresh diario automatizado (Windows Task Scheduler o cron Linux)
- Runbook operativo entregado para operación autónoma

---

## Precio piloto sugerido

**USD 800 – 1,200 pago único** (piloto de 30 días)

Incluye: implementación, clasificación contable, validación financiera, capacitación, runbook operativo.

Si el piloto es exitoso → transición a suscripción mensual desde USD 300/mes.

---

## Próximo paso

1. Llamada de 30 minutos para revisar acceso SAP B1 disponible
2. Firma de acuerdo de confidencialidad simple
3. Solicitud de credenciales SAP solo lectura
4. Inicio de implementación en 48 horas

**Contacto:** Jonathan Campillay · campillayparedes@gmail.com

---

*Evidencia técnica: validado en entorno TST SAP B1 HANA. OACT: 20 cuentas. OJDT: 20 asientos. JDT1: 68 líneas. healthScore: 100/100. 7/7 endpoints HTTP 200. Decisión: GO.*
