# DataBision — Power BI Pro + Import Mode Strategy

**Versión:** 1.0  
**Fecha:** 2026-05-28  
**Estado:** Aprobado para MVP

---

## 1. Decisión

**No implementar Power BI Embedded en el MVP.**

Power BI Embedded requiere capacidad Premium (desde USD 4.995/mes) o Premium Per User (PPU, USD 20/usuario/mes solo para uso interno). Ninguna opción es viable para un primer cliente a USD 300–500/mes.

En su lugar, el MVP usa:
- **Power BI Pro** para el desarrollador/analista de DataBision (USD 10/mes)
- **Import Mode** para los datasets (refrescos programados, hasta 8/día con Pro)
- **Workspaces compartidos** para entregar reportes a los clientes

---

## 2. Cómo Funciona Import Mode

```
Supabase PostgreSQL
      │
      │  Conector PostgreSQL de Power BI Desktop
      ▼
Power BI Desktop (máquina DataBision)
      │  1. Importa datos desde Supabase
      │  2. Construye modelo de datos y medidas DAX
      │  3. Crea reportes interactivos
      ▼
Power BI Service (workspace por cliente)
      │  1. Publica el .pbix
      │  2. Configura credencial de Supabase (1 vez)
      │  3. Programa refresh automático
      ▼
Refresco automático (hasta 8x/día con Pro)
      │  Llama a Supabase directamente
      │  Actualiza el dataset en memoria de Power BI
      ▼
Usuario final accede al reporte actualizado
```

---

## 3. Opciones de Entrega al Cliente

### Opción A — Workspace Compartido (Recomendada MVP)

1. DataBision crea un **workspace Power BI** por cliente (ej. "DataBision - ACME")
2. Agrega al usuario del cliente como **Viewer** del workspace
3. El usuario del cliente necesita **Power BI Pro** o **PPU** (pagada a Microsoft directamente)
4. El portal de DataBision embebe el reporte via iframe usando el enlace de Power BI Service

**Ventaja:** Costo mínimo para DataBision  
**Desventaja:** El cliente DEBE tener licencia Power BI Pro propia

---

### Opción B — Licencia Incluida (Plan Business+)

DataBision compra 1 licencia Power BI Pro por cliente (USD 10/mes) y la incluye en el precio del plan.

- Crear 1 cuenta de servicio Power BI por cliente (ej. `viewer-acme@databision.com`)
- Asignarle licencia Pro y rol Viewer en el workspace
- El portal DataBision usa esa cuenta para generar el token de embed (Embed for Organization)

**Ventaja:** Cliente no necesita gestionar licencias  
**Desventaja:** USD 10 extra por cliente + complejidad de gestionar cuentas de servicio

---

### Opción C — Enlace Directo (Solo para reportes no confidenciales)

Power BI tiene "Publicar en la web" que genera un iframe público sin autenticación.  
**No usar con datos de clientes reales.** Solo útil para demos o reportes de marketing.

---

**Recomendación para MVP:** Opción A para primeros clientes (el cliente tiene Pro). Cambiar a Opción B cuando se consolide el Plan Business.

---

## 4. Límites de Import Mode con Power BI Pro

| Límite | Valor con Pro |
|---|---|
| Refreshes programados por dataset/día | 8 |
| Tamaño máximo del dataset | 1 GB |
| Refresh manual adicional (via API) | Sí, cuenta dentro de los 8 |
| Número de workspaces | Sin límite (Pro) |
| Refresh via API REST | Sí (con credenciales configuradas) |
| Scheduled refresh granularidad | 30 minutos mínimo |

**Para el MVP con extracción cada 1–2 horas, 8 refreshes/día son suficientes:**
- 8 refreshes / 24h = refresh cada 3 horas
- Con extracción cada hora: los datos en Power BI tienen máximo 3–4h de retraso

---

## 5. Configuración de Refresh Automático

### En Power BI Service:

1. Publicar `.pbix` al workspace del cliente
2. Dataset → Settings → Data Source Credentials → configurar Supabase
   - Servidor: `db.xxxx.supabase.co:5432`
   - Base de datos: `postgres`
   - Usuario/contraseña: credenciales de Supabase
3. Dataset → Settings → Scheduled refresh:
   - Enable: On
   - Time zone: Zona horaria del cliente
   - Frequency: Daily (con hasta 8 horarios)
   - Horarios recomendados: 07:00, 10:00, 13:00, 16:00, 19:00 (5 refreshes cubriendo día hábil)

### Refresh via API (opcional, para botón "Actualizar" en portal):

```bash
POST https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/datasets/{datasetId}/refreshes
Authorization: Bearer {token}
Content-Type: application/json
{}
```

El portal puede exponer un endpoint `POST /api/reports/{id}/refresh` que llame a la API de Power BI con el token del Service Principal. Esto permite al cliente solicitar actualización bajo demanda (controlada, no en tiempo real).

---

## 6. Gateway Requerimiento

Para que Power BI Service pueda refrescar datos desde Supabase:

**Supabase en la nube:** Requiere **On-premises Data Gateway** solo si la BD está en red privada. Para Supabase cloud (acceso público por IP), **no se requiere gateway**.

**Configuración de IP en Supabase:**
- Ir a Supabase → Settings → Database → Connection Pooling
- En Network Restrictions: permitir IPs de Power BI (rangos públicos Microsoft)
- O desactivar restricciones de IP para el Plan MVP

---

## 7. Power BI Desktop — Conector PostgreSQL

Para conectar Power BI Desktop a Supabase:

1. Obtener conector: **Bases de datos → PostgreSQL**
2. Servidor: `db.xxxx.supabase.co:5432` (o `6543` para pooler)
3. Base de datos: `postgres`
4. Usar `Direct Query` durante desarrollo para exploración rápida
5. Cambiar a **Import Mode** antes de publicar (performance y sin límite de consultas)

**Tablas a importar para reportes MVP:**
```
raw.sap_oinv    → Facturas
raw.sap_inv1    → Líneas de factura
raw.sap_orin    → Notas de crédito
raw.sap_ocrd    → Clientes
raw.sap_oitm    → Items
raw.sap_oslp    → Vendedores
ctl.ingest_checkpoint  → Estado de extracción (última actualización)
```

---

## 8. Modelo de Datos Recomendado en Power BI

### Star Schema básico para MVP:

```
              dim_cliente (OCRD)
                    │
                    │
fact_ventas ────────┤──── dim_item (OITM)
(OINV + INV1)       │
                    │──── dim_vendedor (OSLP)
                    │
                    └──── dim_fecha (tabla calculada)
```

**Medidas DAX clave:**
```dax
// Ventas totales período
Ventas Totales = SUM(fact_ventas[DocTotal])

// Margen bruto (si hay costo disponible)
// Facturación del mes actual
Ventas Mes Actual = CALCULATE([Ventas Totales], DATESMTD(dim_fecha[Fecha]))

// Top N clientes
// Última actualización del dataset
Última Actualización = MAX(ctl_checkpoint[last_run_utc])
```

---

## 9. Reportes Predefinidos MVP

### R01 — Dashboard Ejecutivo de Ventas
- KPIs: Ventas mes, ventas año, variación vs. período anterior
- Tendencia mensual (12 meses)
- Top 10 clientes por ventas
- Ventas por vendedor

### R02 — Análisis de Clientes
- Ranking de clientes por facturación
- Clientes sin compra en 60/90 días
- Distribución por zona/país

### R03 — Catálogo de Items
- Items más vendidos (cantidad y monto)
- Items sin movimiento (60/90 días)
- Stock actual vs. comprometido (si OITM)

### R04 — Notas de Crédito y Devoluciones
- Volumen de notas crédito vs. facturas
- Clientes con mayor tasa de devolución
- Motivos de devolución (Comments field)

### R05 — Estado de Cartera (opcional)
- Facturas abiertas (DocStatus = 'O')
- Monto pendiente por cliente
- Antigüedad de saldos (requiere PaidToDate)

---

## 10. Roadmap hacia Power BI Embedded

| Fase | Trigger | Acción |
|---|---|---|
| MVP (hoy) | 1–3 clientes | Workspace compartido, Import Mode |
| Fase 2 | 5–10 clientes | Evaluar PPU ($20/user) o automatizar provisioning de workspaces |
| Fase 3 | 10–20 clientes | Implementar Embed for Your Customers con capacidad compartida (F2 en Fabric, ~USD 262/mes) |
| Fase 4 | 30+ clientes | Premium Per Capacity o Fabric F8 (~USD 700/mes), ROI positivo |

**Nota:** El código del portal ya está estructurado para soportar Embedded en el futuro. Solo se necesita activar el feature flag y configurar el Service Principal.

---

## 11. Limitaciones Conocidas del MVP

| Limitación | Impacto | Mitigación |
|---|---|---|
| 8 refreshes/día máximo | Datos con 3h de retraso | Aceptable para gestión; refrescar en horarios clave |
| Usuario necesita Power BI Pro | Fricción de venta | Incluir licencia en Plan Business |
| Dataset en memoria de Power BI | Límite 1GB | Suficiente para 2–3 años de datos SAP típico |
| No hay RLS en Power BI | — | Workspace por cliente ya garantiza aislamiento |
| Reportes no se actualizan en tiempo real | — | Comunicar claramente en contrato |
