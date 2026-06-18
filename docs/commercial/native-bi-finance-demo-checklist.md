# Native BI Finance — Demo Checklist

Sprint 15F — 2026-06-18

Use this checklist before every demo of the Finance accounting module.

---

## T-24h: Datos (día anterior a la demo)

- [ ] Verificar `GET /api/client/bi/finance/readiness` → `readinessStatus = "ready"`
- [ ] Verificar `GET /api/client/bi/finance/validations` → `healthScore >= 80`, `criticalIssues = 0`
- [ ] Balance cuadra: `isBalanced = true` en validaciones
- [ ] Revenue positivo en los últimos 3 períodos
- [ ] Sin cuentas `unclassified` visibles en Plan de Cuentas (o < 3 con justificación)
- [ ] EBITDA trend visible para al menos 6 meses
- [ ] MART refresh ejecutado recientemente (< 24h)

## T-1h: Navegador y entorno

- [ ] Navegador limpio (no hay datos de otra empresa en caché)
- [ ] Portal abierto en la empresa correcta: `{slug}.databision.app` o `?tenant={slug}` local
- [ ] Login con credenciales del CFO de la empresa (no usar SuperAdmin)
- [ ] Todas las pestañas de Finance cargan sin error:
  - [ ] Resumen
  - [ ] Estado de Resultados
  - [ ] Balance General
  - [ ] EBITDA
  - [ ] Plan de Cuentas
  - [ ] Validaciones
- [ ] Filtro de año configurado al período más relevante para la demo
- [ ] Segunda pantalla o proyector probado

## T-5min: Punto de partida de la demo

- [ ] Abrir en: `Finanzas → Resumen`
- [ ] Tener listo el dato de revenue del último mes para mencionar en apertura
- [ ] Conocer el margen bruto actual (para el comentario de "sin DataBision vs. con")
- [ ] Identificar si hay algún warning de validación que sea útil mostrar en el demo (educativo, no bloqueante)

---

## Durante la demo: señales de alerta

| Señal | Acción |
|---|---|
| FinancialDataPending visible | PAUSAR — no hacer la demo de contabilidad. Revisar readiness panel. |
| Balance no cuadra | Ir directo a Validaciones, mostrar el diagnóstico como feature de transparencia |
| Revenue negativo | Pausar, verificar clasificación. No mostrar como dato real. |
| Tiempo de carga > 5s | Verificar que la API está corriendo y el Supabase no tiene cold start |
| `readinessStatus = "blocked"` | CANCELAR demo de finanzas — ofrecer demo con datos genéricos |

---

## Post-demo: acciones de seguimiento

- [ ] Identificar cuentas que el CFO cuestionó (para refinar clasificación)
- [ ] Notar períodos donde el CFO hizo preguntas (área de valor)
- [ ] Si el CFO pidió drill-down a asiento específico → agregar a roadmap
- [ ] Si el CFO quiso ver más períodos históricos → planificar re-extracción desde fecha mayor
- [ ] Verificar que las credenciales de demo no quedaron en el portapapeles / historial

---

## Escenarios de preguntas frecuentes

| Pregunta | Respuesta clave |
|---|---|
| "¿Esto reemplaza mi ERP?" | DataBision lee SAP pero no escribe. Es reporting, no transaccional. |
| "¿Puedo agregar una cuenta específica?" | Sí. Admin → Clasificación Contable → agregar regla exacta por código. |
| "¿Con qué frecuencia se actualiza?" | Configurable: diario, semanal, o bajo demanda. Incremental. |
| "¿Qué pasa si hacemos un ajuste en SAP?" | La próxima extracción lo refleja. No hay delay mayor al ciclo configurado. |
| "¿Puedo ver el detalle de un asiento?" | Actualmente a nivel de cuenta. Drill-down a asiento está en roadmap. |
| "¿El balance está cuadrado?" | Sí — el badge verde lo confirma en tiempo real. Si no cuadra, la alerta se muestra automáticamente. |
| "¿Qué es el EBITDA ajustado?" | Requiere clasificar D&A explícitamente. Lo hacemos con su contador en la sesión de configuración. |

---

## Guión abreviado (5 minutos)

1. **Apertura (30s):** "Esto es el libro diario de SAP, presentado como estados financieros ejecutivos."
2. **Resumen (1min):** Mostrar readiness verde. "Datos actualizados, balance cuadrado."
3. **P&L (1.5min):** Revenue → COGS → Margen → OPEX. Señalar tendencia.
4. **EBITDA (1min):** "Rentabilidad operacional — el número que usan los bancos y los M&A."
5. **Cierre (1min):** "¿Qué período le gustaría explorar? ¿Hay alguna cuenta específica que quiera revisar?"

---

## Referencias

- Demo script completo: `docs/commercial/native-bi-finance-demo-script.md`
- One-pager: `docs/commercial/native-bi-finance-one-pager.md`
- Readiness validation: `docs/native-bi-accounting-production-checklist.md`
