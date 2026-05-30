# ADR-010 — Mode B Canonical Design: Service Layer Delta via SAP UDT Queue

**Fecha:** 2026-05-30  
**Estado:** Aceptado  
**Autor:** Chief Architect  

---

## Contexto

Mode B (Service Layer Delta) tenía dos documentos de diseño incompatibles:

| Documento | UDT principal | Mecanismo disparo | Worker |
|---|---|---|---|
| `service-layer-delta-design.md` | `@DBS_CHANGES` + `@DBS_QUEUE` | PTN como primario | BackgroundService .NET |
| `cloud-connector-queue-mode-design.md` | `@DBI_SYNC_QUEUE` + `@DBI_SYNC_LOG` | TransactionNotification SP | Azure Function |

Esta contradicción (C-03 en la auditoría) bloqueaba el inicio de implementación. Se necesita una especificación canónica única.

---

## Decisión

### Naming Canónico de UDTs

| UDT | Nombre canónico | Propósito |
|---|---|---|
| Cola de procesamiento | **`@DBS_QUEUE`** | Un ítem por documento activo en la cola |
| Log de eventos | **`@DBS_CHANGES`** | Un registro por notificación recibida |

Los nombres `@DBI_SYNC_QUEUE` y `@DBI_SYNC_LOG` que aparecen en `cloud-connector-queue-mode-design.md` son **obsoletos**. No usar en implementación nueva.

### Mecanismo de Población: Jerarquía (no exclusividad)

El mecanismo para poblar `@DBS_QUEUE` se elige según lo que permite el ambiente del cliente:

| Prioridad | Mecanismo | Requisito |
|---|---|---|
| 1 | **Post Transaction Notification (PTN)** | SAP B1 ≥ 9.3, SL ≥ 1.3 habilitado. El cliente debe poder configurar suscripciones PTN. |
| 2 | **TransactionNotification (SP)** | Acceso para crear/modificar el SP en SAP B1. SP mínimo: INSERT en `@DBS_CHANGES` + RETURN 0. |
| 3 | **FMS (Formatted Search)** | Solo cubre operaciones desde UI del cliente (no bulk). Complementario, no suficiente solo. |
| 4 | **SP de reconciliación programado** | Barrido periódico de UpdateDate en tablas objetivo. Fallback universal, más latencia. |

> **PTN NO es una dependencia obligatoria.** Es el mecanismo preferido cuando está disponible. El sistema funciona correctamente con cualquier otro mecanismo. La reconciliación semanal (§10.4 de `service-layer-delta-design.md`) cubre los gaps de cualquier mecanismo.

### Worker: BackgroundService .NET

El `SLDeltaWorker` es un `BackgroundService` (.NET `IHostedService`) dentro de `DataBision.Workers`. No es una Azure Function.

Razón: ver [ADR-007](ADR-007-background-workers.md). Azure Functions tienen cold start, overhead de deployment separado, y contexto de DI aislado del resto del sistema. Un BackgroundService corre en el mismo proceso, comparte el mismo pool de conexiones y simplifica el debugging.

La excepción es alta escala (> 50 tenants en Mode B con volumen alto): en ese caso evaluar extraer el worker a un proceso separado o Azure Functions dedicadas por tenant.

### Documento Canónico

**`docs/service-layer-delta-design.md`** es el diseño técnico de referencia para Mode B. Contiene:
- Esquema completo de `@DBS_CHANGES` y `@DBS_QUEUE` con todos los campos
- Flujo completo del worker loop (§7)
- Retries y DLQ (§9)
- Idempotencia en 3 capas (§11)
- Auditoría (§12)
- Performance y rate limiting (§13)
- Seguridad (§14)
- Onboarding por tenant (§15)

`docs/cloud-connector-queue-mode-design.md` se mantiene como referencia histórica. Tiene el header de actualización que indica los cambios de naming y mecanismo.

---

## Consecuencias

### Para implementadores

1. Al crear UDTs en SAP B1: usar `@DBS_QUEUE` y `@DBS_CHANGES`.
2. Al programar el worker: `SLDeltaWorker : BackgroundService` en `DataBision.Workers`.
3. El mecanismo de disparo a usar: preguntar al partner SAP del cliente qué pueden configurar. Usar el primero disponible en la jerarquía.
4. Si PTN no está disponible, documentar en el onboarding qué mecanismo se usó y agregar la reconciliación semanal como compensación.

### Para la guía técnica al partner SAP

La guía de onboarding para el partner SAP del cliente debe cubrir los 4 mecanismos con instrucciones específicas para cada uno. El partner elige el que aplica a su ambiente.

### ADRs relacionados

- [ADR-004](ADR-004-extractor-modes.md) — Modos A + B + C: jerarquía y criterios de selección
- [ADR-007](ADR-007-background-workers.md) — BackgroundService vs Azure Functions
