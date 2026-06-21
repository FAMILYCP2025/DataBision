# Native BI Finance — Diseño Robusto del Scheduler de Extracción

**Sprint:** 21C  
**Fecha:** 2026-06-20

---

## Problema actual

El extractor DataBision es un proceso CLI que se ejecuta manualmente. Para pilotos de clientes reales, la actualización de datos debe ser:
1. Automática (sin intervención manual)
2. Confiable (reintentos en caso de fallo)
3. Observable (logs claros, alertas en caso de error)
4. No destructiva (fallo parcial no corrompe datos existentes)

---

## Diseño actual (Sprint 21C — scripts)

Scripts de shell/PowerShell registrados en el scheduler del OS:

```
[Scheduler OS]
    │
    └── run-nativebi-finance-refresh.sh/.ps1
            │
            ├── Step 1: DataBision.Extractor --object OACT --send
            │       └── SAP SL → raw.sap_oact
            │
            ├── Step 2: DataBision.Extractor --object OJDT --send
            │       └── SAP SL → raw.sap_ojdt + raw.sap_jdt1
            │
            └── Step 3: DataBision.Extractor --transform-mart
                    └── Supabase: refresh_accounting_all(company_id)
                           └── 8 pasos, idempotente
```

**Garantías del diseño actual:**
- OJDT falla → transform no se ejecuta (datos RAW incompletos no llegan a MART)
- OACT falla → extracción aborta (chart of accounts siempre debe estar fresco)
- Transform falla → datos MART del ciclo anterior siguen disponibles (no se borran)
- Idempotencia: re-ejecutar el script no duplica datos (DELETE+INSERT en MART)

**Limitaciones del diseño actual:**
- Sin reintentos automáticos por paso
- Sin notificaciones push a UI
- Un proceso por cliente (escala horizontalmente, no automáticamente)
- Sin detección de solapamiento (si un ciclo dura más que el intervalo, dos instancias corren simultáneamente)

---

## Diseño robusto (roadmap — Sprint 22+)

Para uso en producción con múltiples clientes, reemplazar los scripts de OS por un scheduler integrado:

### Opción A — Windows Service nativo (ya existe: `--service` mode)

El extractor ya soporta `--service` mode via Generic Host + `UseWindowsService`. Activar el scheduler interno:

```csharp
// ExtractorOptions
public string[] Objects { get; init; } = ["OACT", "OJDT"];
public int IntervalMinutes { get; init; } = 60;
```

```
dotnet DataBision.Extractor.exe --service
```

El proceso corre indefinidamente, extrae según `IntervalMinutes`, y Windows Service Manager lo reinicia si cae.

**Limitación:** No ejecuta `--transform-mart` automáticamente después de la extracción. Requiere extender `ExtractorWorkerService` para llamar a `TransformationRunner.RefreshMartAsync` después de cada ciclo.

### Opción B — Hangfire o Quartz.NET en la API

Integrar un job scheduler en `DataBision.Api` que orqueste extracción + transform por empresa:

```
DataBision.Api
    └── Hangfire JobStorage (SqlServer o Postgres)
            └── RecurringJob.AddOrUpdate("finance-refresh-[companyId]", ...)
                    ├── ExtractorService.RunOactAsync(companyId)
                    ├── ExtractorService.RunOjdtAsync(companyId)
                    └── TransformationService.RefreshMartAsync(companyId)
```

**Ventajas:** UI de monitoring integrada en Hangfire Dashboard, reintentos configurable, history de ejecuciones.

**Desventajas:** Acoplamiento de la extracción (que toca SAP) al proceso API. Requiere credenciales SAP en la API (security concern).

### Opción C — Microservicio extractor multi-tenant (recomendado largo plazo)

Convertir el extractor en un servicio independiente con:
- API de control: `POST /extractors/{companyId}/run`, `GET /extractors/{companyId}/status`
- Scheduler interno por empresa: cada empresa tiene su propio ciclo configurado
- Conexión a DataBision.Api para notificar cuando el refresh está listo

---

## Decisión Sprint 21C

**Implementar: Opción scripts OS (Opción base).** Es la más simple, más segura, y no agrega dependencias cloud nuevas. Los scripts son suficientes para el piloto de 1-5 clientes.

**Evolución recomendada:** Opción A (Windows Service con transform integrado) cuando el número de clientes supere 5. La infraestructura del `--service` mode ya existe.

---

## Garantías de idempotencia (por diseño del pipeline)

Todos los pasos del pipeline son seguros para re-ejecutar:

| Paso | Idempotencia | Por qué |
|---|---|---|
| OACT → raw | ON CONFLICT DO UPDATE | Mismo código = actualiza, no duplica |
| OJDT → raw | ON CONFLICT DO UPDATE | Mismo TransId = actualiza |
| stg.refresh_gl_accounts | ON CONFLICT DO UPDATE | — |
| stg.refresh_journal_entries | ON CONFLICT DO UPDATE | — |
| mart.refresh_gl_accounts | ON CONFLICT DO UPDATE | — |
| mart.refresh_gl_accounts_from_journal_lines | ON CONFLICT DO UPDATE | — |
| mart.refresh_account_balances | ON CONFLICT DO UPDATE | — |
| mart.refresh_income_statement | DELETE+INSERT | Sprint 20: stale data fix |
| mart.refresh_balance_sheet | DELETE+INSERT | Sprint 20: stale data fix |
| mart.refresh_ebitda | DELETE+INSERT | Sprint 20: stale data fix |

Consecuencia: si el script falla a mitad del transform y se re-ejecuta, el resultado final es correcto.

---

## Consideraciones de seguridad del scheduler

1. **Credenciales SAP en el script:** Nunca. El script llama al exe que lee de appsettings.json (en disco protegido).
2. **Cuenta de servicio OS:** Usar cuenta con permisos mínimos — solo acceso a la carpeta del extractor y red al host SAP.
3. **Log file:** No contiene credenciales. El extractor enmascara URLs y no loguea passwords.
4. **IgnoreSslCertificateErrors:** Solo en clientes donde el certificado SAP es autofirmado. Documentar en el onboarding del cliente.
