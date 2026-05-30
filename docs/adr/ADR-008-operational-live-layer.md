# ADR-008 — Operational Live Layer: Consulta Directa a SAP vs Analytics Layer

**Fecha:** 2026-05-30  
**Estado:** Aceptado  
**Autor:** Chief Architect  

---

## Contexto

El pipeline de datos de DataBision tiene una latencia inherente de 15–120 minutos (dependiendo del modo de extracción). Esta latencia es aceptable para decisiones gerenciales basadas en tendencias y datos históricos. Sin embargo, existen escenarios operacionales donde una latencia de 30+ minutos hace que el dato sea inútil para el propósito.

Ejemplos:
- Un jefe de bodega que necesita la lista actualizada de pedidos para despacho
- Un gerente de crédito que necesita saber qué documentos están bloqueados en este momento
- Un supervisor de entregas verificando rutas activas

La pregunta arquitectónica es: ¿cómo se sirven estos datos operacionales sin comprometer el modelo de datos central?

---

## Opciones Evaluadas

### Opción A — Aumentar la frecuencia del ETL

Reducir el intervalo de extracción de 30–60 min a 5 min para los objetos operacionales.

**Problema:** Service Layer no soporta polling cada 5 minutos a escala. Para 10 tenants, implica 10 requests concurrentes al Service Layer del cliente cada 5 minutos = saturación del SL y potencial impacto en la operación SAP del cliente.

**Rechazado.**

### Opción B — WebSockets desde SAP

Implementar push en tiempo real desde SAP B1 hacia el portal.

**Problema:** SAP B1 no tiene capacidad nativa de WebSockets. PTN (Post Transaction Notification) se aproxima, pero no resuelve el caso de "muéstrame el estado actual de los pedidos" — solo notifica cambios puntuales.

**Rechazado como solución universal.** PTN puede usarse para notificaciones de cambio específico (ver Mode B), no para vistas de estado operacional.

### Opción C — Consulta directa a SAP Service Layer en tiempo real (decisión tomada)

Para escenarios operacionales específicos, el portal consulta el SAP Service Layer directamente via una capa de API dedicada, sin pasar por el pipeline ETL.

**Topología:**
```
Portal (/live/*) → Operational Live API → SAP Service Layer → SAP B1
```

**El dato se muestra en pantalla y no se persiste en PostgreSQL.**

---

## Decisión

**Opción C — Operational Live Layer con consulta directa a Service Layer.**

Esta capa es arquitectónicamente distinta del Analytics Layer y opera con principios diferentes:

| Dimensión | Analytics Layer (ETL + PostgreSQL) | Operational Live Layer (SL directo) |
|---|---|---|
| Latencia | 15–120 min | 30–120 segundos |
| Persistencia | Supabase PostgreSQL | Ninguna — efímero |
| Fuente | `fact.*`, `stg.*` | SAP Service Layer OData |
| Uso | Decisiones gerenciales, tendencias | Operación diaria, acción inmediata |
| Offline tolerance | Total | Ninguna — SAP debe estar disponible |

---

## Consecuencias

### Casos de uso cubiertos

| Caso de uso | Frecuencia de actualización |
|---|---|
| Pedidos listos para despacho | 60 segundos |
| Documentos bloqueados | 30 segundos |
| Picking pendiente | 60 segundos |
| Stock alerts (pedidos sin stock) | 120 segundos |
| Entregas pendientes de facturar | 120 segundos |
| Integraciones con error | 30 segundos |

### Restricciones

1. **Solo disponible si el cliente tiene Service Layer activo** — Mode A (HANA directo) no activa esta capa automáticamente.
2. **Rate limiting obligatorio por tenant** — máximo N requests/minuto al SL del cliente para no saturarlo.
3. **No se persiste en PostgreSQL** — los datos son efímeros. Para historización, el Analytics Layer es la fuente.
4. **Solo para operaciones, no para analítica** — no construir dashboards ejecutivos sobre esta capa.

### Planes y acceso

| Plan | Acceso al Live Layer |
|---|---|
| Starter | No incluido |
| Business | Sí — 3 vistas predefinidas |
| Advanced | Sí — vistas configurables |

### Implementación

- Controlador `.NET` dedicado: `OperationalLiveController`
- Endpoints: `GET /api/live/{resource}`
- Frontend: TanStack Query con `refetchInterval` por caso de uso
- Sin WebSockets — polling activo es suficiente y más simple

### Documentos relacionados

- [master-architecture.md §3.7](../master-architecture.md) — Operational Live Layer completo
- [ADR-004](ADR-004-extractor-modes.md) — Modos de extracción (Mode A/B/C)
