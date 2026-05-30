# ADR-007 — Background Workers: .NET IHostedService como mecanismo principal

**Fecha:** 2026-05-29  
**Estado:** Aceptado  
**Autor:** Chief Architect  

---

## Contexto

Los documentos anteriores (`two-client-production-roadmap.md`, `databision-product-architecture.md`) definían Azure Functions como el mecanismo principal para procesamiento en background: transformaciones de staging, monitoring de heartbeats, y el Service Layer Delta connector (Modalidad B).

Con el cambio a un stack de menor costo y menor complejidad operacional, se reevalúa esta decisión.

---

## Opciones Evaluadas

### Opción A — Azure Functions (diseño original)

Cada worker es una Azure Function con trigger apropiado (Timer, Queue, HTTP).

**Ventajas:**
- Escalado horizontal automático
- Sin costo cuando no hay trabajo (consumption plan)
- Aislamiento por función

**Desventajas:**
- Cold start: 1–5s en consumption plan (inaceptable para workers de baja latencia)
- Requiere Azure Function App separado → un deployment adicional a gestionar
- Debugging complejo: requiere Azure Functions runtime local (`func` CLI)
- Sin contexto compartido con el API → deben reconectarse a la BD en cada invocación
- Costo real: aunque el consumption es "gratis", el Function App necesita un plan en producción para evitar cold starts (Plan B1: ~USD 13/mes)
- Complejidad de DI: inyección de dependencias diferente al stack ASP.NET Core estándar

### Opción B — .NET IHostedService Workers (decisión tomada)

Workers implementados como `BackgroundService` (hereda `IHostedService`) dentro del mismo proceso de la API o en un proceso separado `DataBision.Workers`.

**Ventajas:**
- Cero costo adicional: corren en el mismo App Service que ya pagamos
- Sin cold start: proceso siempre activo
- DI compartida con la API: mismo contexto, mismo pool de conexiones DB
- Debugging idéntico al resto del backend: `dotnet run` y punto
- Scheduling con `PeriodicTimer` nativo de .NET 6+
- Cancellation token propagado desde el runtime → shutdown limpio
- Fácil separación futura: si el volumen crece, mover a `DataBision.Workers` (proceso separado) con 0 cambios de código

**Desventajas:**
- No escala horizontalmente de forma automática
- Si el App Service cae, los workers también caen (aceptable para MVP)
- Memoria compartida con el API: un worker con memory leak afecta al API

### Opción C — Combinación (workers .NET + Azure Functions para casos específicos)

Workers .NET para la mayoría de los procesos. Azure Functions solo donde tiene sentido técnico.

**Decisión:** esta es la opción real adoptada:
- **Workers .NET** para: StagingTransform, HeartbeatMonitor, DataFreshness, Alerting, Recommendations
- **Azure Functions** (solo si se necesita) para: Service Layer Delta en casos de alto volumen de tenants donde se necesite escalado independiente del pull de SAP

---

## Decisión

**Workers .NET IHostedService como mecanismo principal.**  
Azure Functions se mantiene como opción para Mode B (Service Layer Delta) en escenarios de alto volumen donde el escalado independiente sea necesario.

---

## Especificación de Workers

### Patrón base de implementación

```
public abstract class TenantScopedWorker : BackgroundService
{
    // Itera sobre todos los tenants activos
    // Por cada tenant: ejecuta en scope DI propio
    // Si falla un tenant: loguea + continúa con el siguiente
    // Respeta CancellationToken del runtime
}
```

### Scheduling

```
StagingTransformWorker    → cada 15 min (o disparado por DataIngested)
HeartbeatMonitorWorker    → cada 5 min
DataFreshnessWorker       → cada 10 min
AlertingWorker            → cada 5 min
RecommendationWorker      → cada 60 min (o tras StagingTransformed)
```

### Gestión de errores

```
Por tenant:
  - Try/catch por tenant dentro del loop
  - Log.Error con tenant context
  - Continuar con siguiente tenant

Por worker completo:
  - Si el worker falla 3 veces consecutivas → PushNotification al SuperAdmin
  - BackgroundService.ExecuteAsync tiene try/catch externo que relanza
  - ASP.NET Core detiene el host si un IHostedService no manejado lanza

Métricas a capturar:
  - last_run_at por worker por tenant
  - last_error por worker por tenant
  - run_duration_ms
```

### Separación de procesos (Fase 3)

Si en Fase 3 se decide separar workers del proceso API:

```
DataBision.Api/          → solo endpoints HTTP
DataBision.Workers/      → solo IHostedService workers
DataBision.Application/  → lógica compartida (sin cambios)
DataBision.Infrastructure/ → BD compartida (sin cambios)
```

La separación es un cambio de `csproj` y `Program.cs`, no de lógica.

---

## Documentos Afectados

- `two-client-production-roadmap.md` — Referencias a Azure Functions para background processing. Reemplazadas por workers .NET en MVP; Azure Functions como opción para escenarios de alto volumen.
- `databision-product-architecture.md` — Azure Function TimerTrigger para Modalidad B. Se mantiene como opción para SL Delta en alta escala; para MVP, SL Delta corre como BackgroundService.
