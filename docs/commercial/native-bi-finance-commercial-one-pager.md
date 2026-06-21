# DataBision Native BI Finance — Commercial One-Pager

**Producto:** DataBision Native BI — Módulo Financiero  
**Para:** SAP Business One customers en Perú / PCGE  
**Versión:** v2.0 (Sprint 19 — 2026-06-20)

---

## El Problema

Los clientes SAP B1 en Perú hoy siguen un proceso manual para obtener sus reportes financieros:

1. Exportar OJDT/JDT1 desde SAP a Excel
2. Aplicar tabla dinámica o fórmulas
3. Clasificar manualmente por PCGE
4. Consolidar Estado de Resultados y Balance
5. Distribuir por email

**Este proceso tarda horas, es propenso a error, y se repite cada mes.**

---

## La Solución DataBision Native BI Finance

Conexión directa a SAP B1 vía Service Layer. Pipeline automático end-to-end.

### Qué hace

| Módulo | Descripción |
|---|---|
| **Extracción automática** | Conecta a SAP B1 Service Layer. Extrae OACT (Catálogo), OJDT (Asientos) y JDT1 (Líneas) en modo incremental. |
| **Clasificación PCGE** | Aplica automáticamente las reglas del Plan Contable General Empresarial Peruano. Clasifica cada cuenta en revenue, cogs, opex, equity, assets, liabilities. |
| **MART financiero** | Consolida en tablas listas para reporte: Estado de Resultados, Balance General, EBITDA, Flujo por cuenta. |
| **Dashboard en tiempo real** | 6 endpoints de API listos para consumo desde Power BI, frontend web, o cualquier herramienta BI. |
| **Health scoring** | Motor de validación automática. Detecta asientos sin contrapartida, cuentas sin clasificar, imbalances. Score 0–100. |

### Qué obtiene el cliente

- **Estado de Resultados** por período con COGS, OPEX, utilidad bruta y neta
- **EBITDA** mensual con márgenes calculados automáticamente
- **Balance General** con activos, pasivos y patrimonio
- **Catálogo de Cuentas** clasificado con saldos actualizados
- **Score de calidad** del dato — siempre sabe si los datos son confiables

---

## Por qué PCGE Peru importa

DataBision implementa las reglas contables correctas para Perú:

- Prefijos 60-61 (Compras/Variación existencias) → COGS
- Prefijos 69 (Costo de ventas) → COGS
- Prefijos 70-79 (Ventas/Ingresos) → Revenue
- Prefijos 40-49 (Obligaciones) → Liabilities (no Revenue como en otros países)
- Prefijos 02 (Saldos iniciales) → Equity

**No hay que configurar nada.** Las reglas PCGE vienen precargadas.

---

## Resultado de la Demo (datos reales de SAP B1 TST)

Conexión a empresa CLTSTKSDEPOR — ambiente de prueba SAP B1 real:

| Métrica | Valor |
|---|---|
| Cuentas OACT | 20 |
| Asientos OJDT | 50 |
| Líneas JDT1 | 122 |
| Reglas PCGE aplicadas | 84 |
| Cuentas clasificadas | 55 (100%) |
| Health Score | 100/100 |
| Endpoints activos | 6/6 HTTP 200 |

Estado de Resultados (Enero 2026):
- Revenue: S/ 201.19
- COGS: S/ 128,474.80
- OPEX: S/ 2,650.00
- Net Income: -S/ 130,921.40

*(Números del ambiente TST — no representativos de un negocio en producción)*

---

## Stack Tecnológico

- **Backend:** .NET 8, C#, PostgreSQL (Supabase)
- **Extracción:** SAP B1 Service Layer REST API
- **MART:** PostgreSQL functions (idempotentes, auditables)
- **API:** ASP.NET Core REST — consumible desde Power BI, Excel, React
- **Seguridad:** Multi-tenant con JWT, aislamiento por company_id

---

## Próximos Pasos

1. Demo con datos del cliente (ambiente SAP B1 productivo)
2. Configuración de reglas PCGE específicas del cliente
3. Integración con dashboards Power BI existentes
4. Automatización del refresh (scheduled pipeline)

**Contacto:** campillayparedes@gmail.com
