# Native BI Finance — Informe de Cierre de Piloto

**Template — completar al finalizar el piloto**

---

## Información general

| Campo | Valor |
|---|---|
| Cliente | |
| RUC | |
| Contacto principal | |
| Consultor DataBision | |
| Fecha de inicio | |
| Fecha de cierre | |
| Opción de piloto | A (5 días) / B (2 semanas) / C (4–8 semanas) |
| Resultado final | ✅ GO / ❌ NO-GO |

---

## Resumen ejecutivo

*(2–3 párrafos: qué se hizo, qué se validó, resultado)*

---

## Datos técnicos

| Dato | Valor |
|---|---|
| SAP B1 versión | |
| CompanyDB | |
| Año fiscal extraído | |
| URL Service Layer | (enmascarada) |
| Certificado SSL | Válido / IgnoreSSL aprobado |

### Volumen de datos

| Tabla | Filas extraídas |
|---|---|
| OACT (Chart of Accounts) | |
| OJDT (Journal Entries header) | |
| JDT1 (Journal Entry lines) | |
| mart.gl_accounts | |
| mart.income_statement_summary | |
| mart.balance_sheet_summary | |
| mart.ebitda_summary | |

---

## Validación financiera

### P&L (Estado de Resultados)

| Línea | DataBision | Sistema cliente | Diferencia |
|---|---|---|---|
| Ingresos | | | |
| COGS | | | |
| Utilidad bruta | | | |
| Gastos operativos | | | |
| Utilidad operativa | | | |
| Utilidad neta | | | |

**Mes validado:** _______________  
**Aprobado por:** _______________ (cargo)  
**Fecha de aprobación:** _______________

### Balance General

| Sección | DataBision | Sistema cliente | Diferencia |
|---|---|---|---|
| Activos corrientes | | | |
| Activos no corrientes | | | |
| Total activos | | | |
| Pasivos corrientes | | | |
| Pasivos no corrientes | | | |
| Patrimonio | | | |
| Total pasivos + patrimonio | | | |

**¿Balance cuadra (Activos = Pasivos + Patrimonio)?** ☐ Sí / ☐ No (documentar diferencia)

### EBITDA

| Componente | Valor DataBision |
|---|---|
| EBITDA mensual (mes validado) | |
| EBITDA acumulado año | |

---

## Incidencias y resoluciones

| # | Incidencia | Causa | Resolución |
|---|---|---|---|
| 1 | | | |
| 2 | | | |

---

## Ajustes realizados

| # | Ajuste | Detalle |
|---|---|---|
| 1 | Reclasificación de cuenta | Cuenta XXX movida de "unclassified" a "opex" |
| 2 | | |

---

## Criterios de aceptación

| Criterio | Resultado |
|---|---|
| P&L validado con diferencia ≤ 5% | ☐ Cumplido / ☐ No cumplido |
| Balance cuadrado | ☐ Cumplido / ☐ No cumplido |
| 0 cuentas críticas sin clasificar | ☐ Cumplido / ☐ No cumplido |
| Capacitación completada | ☐ Cumplido / ☐ No cumplido |
| Scheduler configurado | ☐ Cumplido / ☐ N/A |

---

## Decisión go/no-go

**Decisión:** ☐ GO — El cliente acepta el servicio y procede a suscripción mensual  
**Decisión:** ☐ NO-GO — El piloto no cumplió los criterios (ver observaciones)

**Firma cliente:** ___________________________ Fecha: ___________  
**Firma DataBision:** ________________________ Fecha: ___________

---

## Próximos pasos

### Si GO
- [ ] Propuesta de suscripción mensual enviada (precio acordado: S/. _____/mes)
- [ ] Frecuencia de actualización definida: _______________
- [ ] Usuarios adicionales configurados: _______________
- [ ] Scheduler automático activado: ☐ Sí / ☐ No
- [ ] Fecha de inicio de facturación: _______________

### Si NO-GO
- [ ] Razones documentadas: _______________
- [ ] ¿Pendientes para re-intentar?: _______________
- [ ] Fecha de seguimiento: _______________

---

## Lecciones aprendidas (uso interno)

*(No se comparte con el cliente)*

**¿Qué funcionó bien?**

**¿Qué mejorar para el próximo piloto?**

**¿Cuánto tiempo tomó realmente?**

| Actividad | Horas estimadas | Horas reales |
|---|---|---|
| Configuración inicial | | |
| Primera extracción | | |
| Validación contable | | |
| Ajustes | | |
| Capacitación | | |
| **Total** | **13h (estimado)** | |

---

## Archivos adjuntos

- [ ] Captura de test-connection exitoso
- [ ] Captura de dashboard P&L validado
- [ ] Captura de Balance General validado
- [ ] Email de aprobación del contador
