# DataBision — BI Operativo Conectado a SAP Business One
## Guion Comercial — Demo KSDEPOR

**Duración sugerida:** 15 a 20 minutos  
**Audiencia:** Gerencia, administración, usuario operativo, consultor SAP  
**Ambiente:** Demo local o staging con datos reales KSDEPOR

---

## 1. Problema del Cliente (2 min)

> **Qué decir:**
> "Hoy, para saber cómo están las ventas del mes, ¿cuántos pasos necesitan? ¿Abren SAP directamente? ¿Exportan a Excel? ¿Esperan que alguien les mande un reporte?"

**Puntos clave a mencionar:**
- SAP Business One tiene toda la información, pero no está diseñado para que gerencia navegue datos fácilmente.
- Los reportes nativos de SAP son buenos para operación diaria pero no para análisis visual rápido.
- Exportar a Excel es manual, no trazable, y se desactualiza.
- Las decisiones se toman tarde porque el dato no está disponible en tiempo real.

---

## 2. Qué Propone DataBision (2 min)

> **Qué decir:**
> "DataBision conecta su SAP Business One a un portal web moderno. Sin exportar a Excel. Sin abrir SAP. La gerencia ve en tiempo real lo que necesita: ventas, compras, inventario, finanzas y el estado de los datos — todo en un solo lugar."

**Puntos clave:**
- Conexión directa a SAP B1 vía Service Layer (la API oficial de SAP).
- Datos en PostgreSQL (Supabase), transformados y listos para visualizar.
- Portal web accesible desde cualquier browser, sin instalar nada.
- Multiempresa: cada cliente tiene su propio subdominio y sus propios datos, completamente separados.
- Los datos son de SAP, no inventados: lo que muestra DataBision es exactamente lo que hay en SAP.

---

## 3. Arquitectura en Lenguaje Simple (2 min)

> **Qué decir:**
> "El proceso es simple: SAP tiene los datos. Nuestro extractor los lee y los guarda en una base de datos propia. Luego los transformamos en indicadores listos para visualizar. Y el portal web los muestra."

```
SAP Business One (HANA)
        ↓ Service Layer (API oficial SAP)
   Extractor DataBision
        ↓ Procesamiento y transformación
   Base de datos (Supabase/PostgreSQL)
        ↓ API REST (.NET)
   Portal Web (Browser)
```

**Mensajes clave para cliente:**
- SAP no se toca. No hay riesgo de alterar datos de producción.
- La extracción se hace en horarios controlados.
- Los datos en DataBision son una copia procesada, no la fuente original.
- Logs completos de cada extracción: qué se leyó, cuándo, cuántas filas.

---

## 4. Demo: Ventas (3 min)

> Abrir `/client/bi/sales`

**Qué mostrar:**
- KPIs de cabecera: Ventas netas, Ventas brutas, Facturas, Ticket promedio.
- Tab "Clientes": ranking de clientes por monto, con última fecha de factura.
- Tab "Productos": ítems más vendidos, cantidad y monto.
- Tab "Vendedores": performance por vendedor.
- Tab "Fulfillment": tasa de cumplimiento de órdenes vs entregas.

**Qué decir:**
> "Esto viene directamente de los documentos de SAP: facturas (OINV), notas de crédito (ORIN) y órdenes de venta (ORDR). No hay intermediarios."

**Punto comercial:**
> "¿Cuánto tiempo le toma hoy a su equipo armar esto en Excel?"

---

## 5. Demo: Compras (2 min)

> Abrir `/client/bi/purchasing`

**Qué mostrar:**
- KPIs: total de órdenes de compra, monto, recepciones, proveedores activos.
- Tab "Proveedores": ranking por monto de compra.
- Tab "Recepciones": detalle de entregas recibidas.

**Qué decir:**
> "Compras usa las órdenes de compra (OPOR) y recepciones de mercadería (OPDN) de SAP. Puedo ver qué proveedor me abastece más y cuándo llegaron los pedidos."

---

## 6. Demo: Inventario (2 min)

> Abrir `/client/bi/inventory`

**Qué mostrar:**
- KPIs: ítems de movimiento rápido, normal, lento y sin movimiento.
- Tab "Rotación": cada ítem con su estado (FAST/NORMAL/SLOW/NO_MOVEMENT).
- Tab "Almacenes": stock por bodega.

**Qué decir:**
> "La rotación de inventario identifica automáticamente qué ítems se mueven bien y cuáles están detenidos. Eso ayuda a tomar decisiones de compra y de espacio de bodega."

---

## 7. Demo: Finanzas (2 min)

> Abrir `/client/bi/finance`

**Qué mostrar:**
- KPIs: AR vencido, % vencido, facturas emitidas, monto facturado 30 días.
- Tab "Cuentas por cobrar (AR)": aging por cliente — qué deben, cuánto tiene más de 90 días.
- Tab "Cuentas por pagar (AP)": en preparación para próxima versión.

**Qué decir:**
> "El aging de cuentas por cobrar viene directo de los documentos de SAP. Se pueden ver cuántos clientes tienen deuda vencida y de qué antigüedad. Fundamental para cobranza."

---

## 8. Demo: Operaciones (2 min)

> Abrir `/client/bi/operations`

**Qué mostrar:**
- Health score del pipeline (0-100).
- Estado del extractor y del transform, con fecha/hora del último run.
- Tab "Alertas": alertas activas con severidad (critical/warning/info).
- Tab "Calidad de datos": problemas detectados en los datos extraídos de SAP.

**Qué decir:**
> "Este módulo es para el equipo técnico o consultoría SAP. Muestra el estado del sistema: cuándo fue la última extracción, si hubo errores, y si los datos tienen problemas de calidad. Todo trazable."

---

## 9. Beneficio para Gerencia

- Ver indicadores clave sin abrir SAP ni pedir reportes.
- Datos actualizados con la frecuencia que el negocio necesita.
- Decisiones más rápidas basadas en datos reales, no en "lo que alguien recuerda".
- Un solo lugar para ventas, compras, inventario y finanzas.

---

## 10. Beneficio para Usuario Operativo

- No tiene que exportar a Excel ni preparar reportes manuales.
- Los datos están disponibles apenas se hace la extracción.
- Puede filtrar por período, ordenar por columnas, paginar resultados.
- Trazabilidad: sabe de dónde viene cada número.

---

## 11. Beneficio para TI / Consultoría SAP

- Integración via Service Layer oficial: sin acceso directo a HANA, sin riesgo.
- Logs completos de extracción: qué objeto, cuántas páginas, cuántas filas, errores.
- Calidad de datos visible: problemas detectados antes de que lleguen al usuario.
- Arquitectura limpia: SAP → Extractor → PostgreSQL → API → Frontend. Separación clara.
- Extensible: nuevos objetos SAP, nuevos dashboards, sin tocar SAP.

---

## 12. Próximo Paso Comercial

> **Qué decir:**
> "Lo que vieron hoy es DataBision conectado a datos reales de KSDEPOR. Esto está funcionando en ambiente de desarrollo. El siguiente paso es definir el ambiente de producción, confirmar los objetos SAP que quieren cubrir, y acordar el modelo de acceso."

**Opciones de cierre:**
- Propuesta formal con precio mensual por empresa.
- Reunión técnica con consultor SAP para revisar Service Layer en producción.
- Prueba de concepto en ambiente productivo con extracción controlada.

---

## Puntos de Conversación de Respaldo

| Pregunta frecuente | Respuesta sugerida |
|---|---|
| "¿Afecta el rendimiento de SAP?" | No. Service Layer es la API oficial. La extracción se hace en horarios controlados y no modifica datos. |
| "¿Qué pasa si SAP cambia?" | Los objetos SAP que usamos (OINV, OPOR, etc.) son estándar de SAP B1. Si hay cambios, el extractor se actualiza. |
| "¿Los datos son seguros?" | Sí. Cada empresa tiene su base de datos separada. Los datos no se comparten entre clientes. JWT con RS256 para autenticación. |
| "¿Puedo agregar más indicadores?" | Sí. El sistema está diseñado para agregar nuevos objetos SAP y nuevos dashboards sin romper lo existente. |
| "¿Y Power BI?" | Power BI se puede integrar sobre los mismos datos de Supabase. Está en el roadmap. DataBision y Power BI no son excluyentes. |
