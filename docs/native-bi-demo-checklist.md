# DataBision Native BI — Pre-Demo Checklist

Completar este checklist antes de cada demo comercial.

---

## 24 horas antes

- [ ] Confirmar que el extractor corrió en las últimas 24 h para el tenant demo
- [ ] Verificar que `bi.DashboardSummary` tiene valores > 0 en el tenant demo
- [ ] Verificar que `bi.TopCustomers` tiene al menos 5 filas
- [ ] Verificar que `bi.SalesByCustomer` y `bi.SalesByItem` tienen datos del último mes
- [ ] Confirmar que el backend API está desplegado y respondiendo (`GET /health`)
- [ ] Confirmar que el frontend está en producción (`{slug}.databision.app`)

## 1 hora antes

- [ ] Abrir el browser con la sesión activa — no cerrarla ni limpiar cookies
- [ ] Navegar a `/client/bi/dashboard` y confirmar que los 4 KPI cards muestran valores
- [ ] Confirmar que el `SyncStatusWidget` muestra estado **verde** (ok)
- [ ] Navegar a `/client/bi/sales` y confirmar que el gráfico de barras tiene datos
- [ ] Cambiar el DateRangePicker y confirmar que los KPIs se actualizan
- [ ] Navegar a `/client/bi/diagnostics` (con cuenta CompanyAdmin) y confirmar verde
- [ ] Verificar que la latencia de las páginas es < 2 segundos en la red de la reunión
- [ ] Cerrar otras pestañas del browser que puedan distraer

## En la demo

- [ ] **No** mostrar la pantalla de login ni el flujo de auth
- [ ] **No** abrir el SuperAdmin panel durante la demo
- [ ] **No** mostrar datos reales de otra empresa en la misma sesión
- [ ] Tener el script de demo a mano (`docs/native-bi-commercial-demo-script.md`)
- [ ] Tener la lista de limitaciones conocidas a mano por si preguntan (`docs/native-bi-demo-known-limitations.md`)

## Si algo falla durante la demo

| Problema | Acción |
|---|---|
| KPIs muestran `—` | Recargar la página. Si persiste, abrir Network tab y mostrar que el API responde — el valor es `null` en SAP. |
| `SyncStatusWidget` en rojo | Ir a Diagnósticos, mostrar el error, explicar que el sistema detecta y notifica proactivamente. |
| Frontend no carga | Tener captura de pantalla de respaldo en el celular. |
| Preguntan por algo no implementado | Ver `docs/native-bi-demo-known-limitations.md` — ser honesto con el roadmap. |

---

*Checklist versión 1.0 — 2026-06-08*
