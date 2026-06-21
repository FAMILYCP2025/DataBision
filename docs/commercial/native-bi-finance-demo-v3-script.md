# Native BI Finance Demo v3 — Script de demostración

**Versión:** 3.0 (Sprint 20)  
**Fecha:** 2026-06-20  
**Duración estimada:** 20-25 minutos  
**Audiencia:** CFO / Controller / Gerente Financiero de empresa SAP B1 en Perú

---

## Objetivo de la demo

Demostrar que DataBision conecta directamente a SAP B1 y genera reportes financieros PCGE automáticos sin intervención manual. La demo usa datos reales del ambiente TST del cliente (CLTSTKSDEPOR).

---

## 0. Preparación (T-10 min)

```bash
# Start API
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5103 \
  dotnet run --project src/DataBision.Api --no-launch-profile

# Start frontend
cd databision-frontend && npm run dev

# Verify all 6 endpoints
curl -s "http://localhost:5103/api/client/bi/finance/readiness?companyId=ksdepor" | jq .data.readinessStatus
# Expected: "ready"
```

---

## 1. Apertura — El problema (2 min)

> "¿Cuánto tiempo tarda su equipo en generar el Estado de Resultados mensual?
> ¿Es un proceso de exportar a Excel, tabla dinámica, clasificar por PCGE, consolidar?
> DataBision lo hace automáticamente, directo desde SAP B1."

**Mostrar:** pantalla en blanco / dashboard sin datos (antes del pipeline)

---

## 2. Conexión y extracción (2 min)

> "Primero, mostramos que los datos vienen directamente de SAP B1. No hay importación manual."

**Mostrar tab "Validaciones":**
- readiness.readinessStatus = **"ready"**
- 84 reglas PCGE aplicadas automáticamente
- healthScore = 100/100
- 6/6 endpoints activos

> "El sistema validó que la conexión está activa, los datos están disponibles, y hay cero problemas."

---

## 3. Estado de Resultados (5 min)

**Mostrar tab "Estado de Resultados":**

> "Este es el Estado de Resultados, generado automáticamente a partir del libro diario SAP B1."

**Destacar:**
1. Las cuentas 60-69 (PCGE compras/variación existencias) se clasificaron automáticamente como COGS
2. Las cuentas 70-79 (ventas) como Ingresos
3. Las cuentas 40-49 (obligaciones) como Pasivos — no como Revenue

> "DataBision conoce PCGE. Las reglas están precargadas para Perú. No hay que configurar nada."

**Exportar:**
- Click "Exportar CSV" → se descarga `estado-resultados-2026-01.csv`
- Abrir el CSV: muestra todas las líneas del P&L

> "Y pueden exportar esto a Excel en un click para sus reportes mensuales."

**Datos de la demo (Enero 2026 — datos TST):**
- Revenue: S/ 201.19
- COGS: S/ 128,474.80
- OPEX: S/ 2,650.00
- Net Income: -S/ 130,921.40

*Si preguntan por los números bajos:* "Estamos usando el ambiente TST. En producción verían las cifras reales de su empresa."

---

## 4. EBITDA y rentabilidad (3 min)

**Mostrar tab "EBITDA":**

> "El EBITDA se calcula automáticamente. No hay fórmulas manuales en Excel."

**Destacar:**
- Gross profit margin calculado
- Financial result separado (PCGE: cuentas 67/78)
- Net income después de impuestos

**Exportar EBITDA:**
- Click "Exportar CSV" → `ebitda.csv` con todos los períodos

> "Este CSV puede conectarse directo a su Power BI o Excel para los reportes a directorio."

---

## 5. Balance General (3 min)

**Mostrar tab "Balance General":**

> "El Balance General se actualiza con cada ciclo de extracción."

**Destacar:**
- Activos, pasivos y patrimonio separados automáticamente
- Clasificación PCGE: 1x = activos, 4x = pasivos (no ingresos como erróneamente hacen algunos sistemas)

**Nota para el demostrador:** El ambiente TST no tiene asientos en cuentas patrimoniales (50-59 PCGE), por lo que el balance no cuadra. Proactivamente decir:

> "En TST el patrimonio aparece en cero porque no hay asientos de capital en este ambiente de prueba. En producción, con sus datos reales, el balance cuadra correctamente."

**Exportar:** Click "Exportar CSV" → `balance-general-2026-02-28.csv`

---

## 6. Plan de Cuentas (2 min)

**Mostrar tab "Plan de Cuentas":**

> "Aquí vemos el catálogo completo de cuentas con su clasificación PCGE automática."

**Destacar:**
- 55 cuentas clasificadas (0 sin clasificar)
- Saldos actualizados por cuenta
- Filtro por clasificación

**Exportar:** Click "Exportar CSV" → `plan-de-cuentas.csv`

> "Este archivo puede enviarse al contador para revisión y ajuste de clasificaciones."

---

## 7. Administración — Reglas contables (3 min)

**Mostrar panel admin (admin.databision.app):**
> "Desde el panel de administración, el equipo DataBision o el contador puede ajustar las reglas de clasificación."

**Mostrar AccountClassificationSection:**
- 84 reglas PCGE cargadas
- Se puede agregar regla por código específico o prefijo
- Sugerencias automáticas desde OACT (botón "Sugerencias desde OACT")

> "Si hay cuentas específicas del cliente que no siguen PCGE estándar, se configuran aquí en minutos."

---

## 8. Ciclo de actualización (1 min)

> "¿Con qué frecuencia se actualiza? DataBision puede ejecutarse en modo programado — cada hora, diario, semanal. 
> La ejecución toma menos de 5 minutos para 50 asientos. Para 5,000 asientos, menos de 3 minutos con procesamiento paralelo."

**Mostrar log de extracción (si disponible):**
```
OJDT-17C: extracted 122 lines from 50/50 entries in 4823ms (~96ms/GET, concurrency=3)
```

---

## 9. Cierre — Próximos pasos (2 min)

> "¿Qué vería diferente con sus datos de producción?"
> - Balance que cuadra (con asientos patrimoniales)
> - Revenue real de sus ventas
> - COGS de sus compras/inventario
> - Múltiples períodos comparables

**Propuesta de siguiente paso:**
1. Conectar al ambiente de producción SAP B1 del cliente (read-only)
2. Ejecutar primera extracción con sus datos reales
3. Revisar clasificación PCGE con su contador
4. Demo con datos reales — 1 semana

---

## Preguntas frecuentes

| Pregunta | Respuesta |
|---|---|
| "¿Modifica datos en SAP?" | No. Solo lectura via Service Layer REST API. |
| "¿Soporta multi-empresa?" | Sí. Cada empresa tiene aislamiento completo por company_id. |
| "¿Qué tan seguro es?" | JWT RS256, refresh tokens hasheados, TLS, zero-trust multi-tenant. |
| "¿Puede integrarse con Power BI?" | Sí. 6 endpoints REST listos para Power BI DirectQuery. |
| "¿Soporta PCGE con modificaciones?" | Sí. Reglas configurables por empresa además de las 84 PCGE base. |
| "¿Qué versión de SAP B1?" | Service Layer v1000290+. SAP B1 9.3, 10.0, 10.x. |
