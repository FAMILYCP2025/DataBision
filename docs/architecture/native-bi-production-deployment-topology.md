# Native BI Finance — Topología de Deployment en Producción

**Sprint 27 · DataBision · Junio 2026**

---

## Componentes del sistema

| Componente | Descripción |
|---|---|
| **DataBision API** | Backend .NET 8 — autenticación, endpoints, MART queries |
| **DataBision Frontend** | React/Vite — dashboard en navegador |
| **Extractor** | Proceso CLI .NET — conecta a SAP, extrae datos, llama API ingest |
| **Supabase / PostgreSQL** | Base de datos de staging y MART |
| **SAP Service Layer** | API REST de SAP B1 — fuente de datos |

---

## Topología A — API + Extractor en el mismo servidor (on-premise cliente)

```
┌─────────────────────────────────────────┐
│         Servidor cliente                │
│                                         │
│  ┌──────────────┐  ┌──────────────────┐ │
│  │  DataBision  │  │    Extractor     │ │
│  │     API      │←─│  (CLI + Cron)    │ │
│  └──────┬───────┘  └────────┬─────────┘ │
│         │                   │           │
│  ┌──────▼───────┐    ┌──────▼─────────┐ │
│  │  Supabase    │    │  SAP Service   │ │
│  │  (Staging)   │    │     Layer      │ │
│  └──────────────┘    └────────────────┘ │
└─────────────────────────────────────────┘
           │
    ┌──────▼──────┐
    │  Navegador  │  (acceso remoto del CFO)
    │  Frontend   │
    └─────────────┘
```

### Pros
- Todo en la red del cliente — datos nunca salen de la empresa
- Latencia mínima entre extractor y SAP Service Layer
- Sin dependencia de internet para la extracción
- Más fácil de aprobar por TI conservadora

### Contras
- DataBision debe gestionar deployment en servidor del cliente
- Actualizaciones requieren acceso remoto al servidor
- El cliente necesita un servidor dedicado (o VM) con .NET runtime
- Monitoreo más complejo — sin visibilidad directa de DataBision

### Cuándo elegir
- Cliente con política de "datos no salen de la empresa"
- SAP Service Layer no expuesto a internet
- Cliente con servidor on-premise disponible
- Primeros pilotos donde la seguridad interna es prioridad

---

## Topología B — API cloud + Extractor local cliente (recomendada para piloto)

```
┌──────────────────────────────┐     ┌─────────────────────────┐
│      Cloud DataBision        │     │     Red cliente          │
│                              │     │                          │
│  ┌────────────────────────┐  │     │  ┌───────────────────┐  │
│  │    DataBision API      │◄─┼─────┼──│    Extractor      │  │
│  │  (Azure / VPS / etc.)  │  │     │  │   (CLI + Cron)    │  │
│  └──────────┬─────────────┘  │     │  └────────┬──────────┘  │
│             │                │     │           │              │
│  ┌──────────▼─────────────┐  │     │  ┌────────▼──────────┐  │
│  │       Supabase         │  │     │  │  SAP Service Layer │  │
│  │    (Staging + MART)    │  │     │  │   (on-premise)    │  │
│  └────────────────────────┘  │     │  └───────────────────┘  │
│                              │     │                          │
│  ┌────────────────────────┐  │     └─────────────────────────┘
│  │  Frontend (CDN/Vercel) │  │
│  └────────────────────────┘  │
└──────────────────────────────┘
```

### Pros
- API y frontend en cloud — fácil de actualizar sin tocar red del cliente
- Extractor corre en la red del cliente — SAP no necesita estar expuesto a internet
- DataBision controla el API y puede monitorear
- Escalable: múltiples clientes con el mismo API

### Contras
- Extractor envía datos al API cloud (datos salen de la red del cliente al API)
- Requiere API key segura para el push desde extractor
- Cliente necesita máquina local con .NET runtime para el extractor

### Cuándo elegir
- **Caso más común para piloto** — balance entre seguridad y operación
- SAP Service Layer no expuesto a internet pero extractor puede salir
- DataBision quiere mantener control del API

---

## Topología C — API cloud + Extractor en VM puente

```
┌──────────────────────────────┐     ┌─────────────────────────────────────┐
│      Cloud DataBision        │     │          Red cliente                 │
│                              │     │                                      │
│  ┌────────────────────────┐  │     │  ┌──────────┐    ┌───────────────┐  │
│  │    DataBision API      │◄─┼─────┼──│  VM      │───►│ SAP Service  │  │
│  │       (cloud)          │  │     │  │  Puente  │    │    Layer     │  │
│  └──────────┬─────────────┘  │     │  │(Extractor│    └───────────────┘  │
│             │                │     │  │ + Cron)  │                        │
│  ┌──────────▼─────────────┐  │     │  └──────────┘                        │
│  │       Supabase         │  │     │                                      │
│  └────────────────────────┘  │     │  La VM es provisionada por DataBision│
│                              │     │  o gestionada como servicio          │
└──────────────────────────────┘     └─────────────────────────────────────┘
```

### Pros
- DataBision gestiona la VM puente — mayor control
- Cliente no necesita instalar nada
- Actualizaciones del extractor centralizadas
- Posibilidad de monitoreo continuo desde DataBision

### Contras
- Costo adicional de la VM puente (USD 20–50/mes)
- DataBision necesita acceso SSH a la VM del cliente (o VM en cloud del cliente)
- Mayor complejidad de configuración inicial
- Depende de que el cliente permita una VM con acceso a SAP

### Cuándo elegir
- Cliente Enterprise con SAP HANA cloud
- Cliente que no quiere instalar nada en sus servidores
- DataBision quiere operación totalmente gestionada
- Contratos con SLA alto donde downtime no es aceptable

---

## Comparación de topologías

| Criterio | A (todo on-prem) | B (API cloud + extractor local) | C (VM puente) |
|---|---|---|---|
| Datos salen de la red | No | Sí (al API cloud) | Sí (al API cloud) |
| Facilidad de actualizar extractor | Baja | Media | Alta |
| Costo operativo DataBision | Bajo | Medio | Alto |
| Complejidad inicial | Media | Baja | Alta |
| Recomendado para piloto | No | **Sí** | No |
| Recomendado para Enterprise | Sí | Sí | Sí |

---

## Decisión recomendada para primer piloto

**Topología B: API cloud + Extractor local cliente**

Razones:
1. El cliente solo instala el extractor (.NET runtime + CLI + script de cron)
2. DataBision mantiene control del API y puede actualizarlo sin tocar al cliente
3. Los datos viajan encriptados (HTTPS) desde el extractor al API
4. Es el punto de partida más económico y más fácil de operar

Configuración mínima para Topología B:
- Servidor DataBision: VPS cloud con .NET 8, Nginx, HTTPS
- Cliente: máquina con .NET 8 runtime, acceso a SAP Service Layer, salida a internet HTTPS
