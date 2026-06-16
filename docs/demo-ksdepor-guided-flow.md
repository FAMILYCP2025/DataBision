# Demo KSDEPOR — Guided Flow

Sprint 8P — Junio 2026

Script narrativo de la demo comercial de DataBision para KSDEPOR. Cubre 8 secciones (A–H) con el mensaje comercial de cada pantalla, transiciones suaves y puntos de conversación para el presentador.

---

## Antes de la demo

- Ambiente levantado y validado (ver `demo-ksdepor-visual-review-checklist.md`)
- Browser en pantalla de login en modo de presentación (F11 para fullscreen)
- Credenciales del usuario demo disponibles
- Transform ejecutado hace menos de 2h

---

## A — Apertura: El problema que KSDEPOR tiene hoy

**Duración estimada:** 2–3 min (sin pantallas todavía)

**Mensaje:**

> "Ustedes tienen SAP Business One, y eso es muy bueno — tiene todos los datos. El problema no es la falta de datos; es que sacar esos datos para tomar decisiones requiere tiempo, reportes manuales o consultores que generan archivos de Excel que ya están desactualizados cuando llegan a sus manos."

> "DataBision conecta directo con su SAP, extrae la información, la transforma y la presenta en un portal web que cualquier gerente puede abrir en el browser — sin Excel, sin esperar a IT, sin scripts manuales."

**Transición:** "Déjame mostrárselos en vivo con sus propios datos."

---

## B — Login: Acceso al portal de KSDEPOR

**Pantalla:** `http://localhost:5173/client/login?tenant=ksdepor`

**Acción:** Mostrar la pantalla de login brevemente antes de ingresar.

**Mensaje:**

> "Cada empresa tiene su propio portal con su propio subdominio. En producción sería `ksdepor.databision.app`. El acceso es por usuario y contraseña — con roles, así que un gerente de ventas solo ve ventas, y el directorio ve todo."

**Acción:** Ingresar credenciales y hacer login.

**Mensaje post-login:**

> "Al entrar, el portal ya cargó sus datos desde SAP. No hay que hacer nada manual. Vean el menú lateral — cinco módulos: Ventas, Compras, Inventario, Finanzas y Operaciones."

---

## C — Ventas: El corazón del negocio

**Pantalla:** `/client/bi/sales`

**Secuencia:**

1. **KPI cards** → "Estos cuatro números son el resumen ejecutivo de ventas. Netas, brutas, facturas emitidas, ticket promedio. Seleccionables por rango de fechas."

2. **DateRangePicker** → "Puedo cambiar el período. ¿Quieren ver el último trimestre? ¿El año?" → Cambiar las fechas en vivo.

3. **Tab Clientes** → "Aquí están sus clientes ordenados por monto de ventas. ¿Quién es el cliente más importante? ¿Cuándo fue la última factura? ¿Cuántas facturas en el período?" → Hacer scroll si hay datos.

4. **Tab Productos** → "Lo mismo por producto — cuáles se venden más, cuánto se factura, cuándo fue la última venta."

5. **Tab Vendedores** → "Y por vendedor — ventas netas, facturas, clientes activos, ticket promedio. Permite comparar el desempeño del equipo de ventas sin pedir reportes."

6. **Tab Fulfillment** → "Este es el que más diferencia hace: la tasa de cumplimiento de pedidos. ¿Cuántos pedidos se convirtieron en entregas? ¿Cuántos quedaron pendientes? El naranja significa menos del 80% de cumplimiento — una señal de alerta para el área de logística."

**Mensaje de cierre de sección:**

> "Todo esto viene de su SAP — facturas (OINV), pedidos (ORDR), entregas (ODLN). No hay carga manual."

---

## D — Compras: Control de la cadena de suministro

**Pantalla:** `/client/bi/purchasing`

**Secuencia:**

1. **KPI cards** → "Órdenes de compra emitidas, monto total, monto recibido y proveedores activos en los últimos 30 días."

2. **Tab Proveedores** → "Por proveedor: cuántas órdenes, monto total, monto recibido, última orden. Pueden ver cuáles proveedores concentran el mayor volumen de compras."

3. **Tab Recepciones** → "Y las recepciones de mercadería — cuándo llegó, de qué proveedor, monto recibido. Trazabilidad completa de la recepción sin salir del portal."

**Mensaje:**

> "En operaciones de distribución deportiva como KSDEPOR, gestionar bien a los proveedores es crítico. Esto les da visibilidad en tiempo real sin tener que abrir SAP o llamar a alguien."

---

## E — Inventario: Rotación y movimientos

**Pantalla:** `/client/bi/inventory`

**Secuencia:**

1. **KPI cards** → "Cuántos artículos tienen alta rotación, rotación normal, baja rotación y sin movimiento. Una sola pantalla para saber qué se mueve y qué no."

2. **Tab Rotación** → "Por artículo — código, nombre, categoría de rotación con color, cantidad vendida en 30 y 90 días, cobertura de días, última venta. El color es inmediato: verde es lo que se mueve bien, gris es lo que está parado."

3. **Tab Almacenes** → "Los movimientos de almacén — entradas, salidas, traspasos. Le da visibilidad al jefe de bodega sin tener que correr reportes de SAP."

**Mensaje:**

> "La rotación de inventario determina el capital de trabajo. Ver qué artículos están parados durante 90 días o más es información accionable — ¿hay que hacer una promoción? ¿Descontinuar? Eso requería consultores antes; ahora está en el portal."

---

## F — Finanzas: Cobranzas y crédito

**Pantalla:** `/client/bi/finance`

**Secuencia:**

1. **KPI cards** → "AR vencido total, porcentaje de cartera vencida, facturas emitidas en 30 días, monto facturado."

2. **Tab AR (Cuentas por cobrar)** → "Por cliente — saldo total, cuánto está vencido, distribución por tramo: 0-30 días, 31-60 días, más de 90 días. Los números en rojo son la alerta: ese cliente tiene deuda vencida de más de 90 días."

3. **Tab AP** → "Las cuentas por pagar les da el mismo nivel de detalle sobre sus proveedores. ¿Cuánto deben? ¿Cuándo vence? Esto ayuda a planificar el flujo de caja."

**Mensaje:**

> "El área de crédito y cobranzas puede trabajar desde este portal directamente. Sin pedir informes, sin esperar a fin de mes. Cuando el gerente financiero necesita saber si pueden extenderle crédito a un cliente, esta pantalla da la respuesta en segundos."

---

## G — Operaciones: La garantía técnica

**Pantalla:** `/client/bi/operations`

**Secuencia:**

1. **Health score** → "Esto es el pulso del sistema. Un número del 0 al 100 que resume si el pipeline de datos está funcionando bien."

2. **Status Extractor/Transform** → "El extractor conecta a su SAP todas las noches, extrae los datos, y los transforma. Aquí pueden ver cuándo fue la última ejecución exitosa."

3. **Tab Alertas** → "Cualquier anomalía — si un objeto de SAP falla, si hay datos duplicados, si hay una demora — genera una alerta aquí. El equipo de DataBision la ve antes que ustedes y la resuelve."

4. **Tab Calidad de datos** → "La calidad de datos detecta inconsistencias automáticamente. Por ejemplo, si un artículo vendido no tiene item master, o si hay una factura sin cliente asignado."

**Mensaje:**

> "Esto es lo que diferencia DataBision de un dashboard cualquiera. No solo les mostramos los datos — nos aseguramos de que los datos sean correctos, y cuando no lo son, lo sabemos antes de que impacte en la operación."

---

## H — Cierre: El siguiente paso

**Pantalla:** Volver a Ventas o dejar en Operaciones (la más técnica impresiona bien al final)

**Mensaje:**

> "Lo que acaban de ver son datos reales de su SAP en un ambiente de prueba. Todo lo que necesitan en producción es autorizar el acceso a su Service Layer y en 2-3 semanas tienen este portal operando con su historial completo."

> "El pilot que les proponemos cubre los 5 módulos que vieron, con una carga inicial de datos históricos y soporte técnico incluido durante los primeros 90 días."

**Preguntas para cerrar la conversación:**
- "¿Qué módulo les parece más útil para empezar?"
- "¿Tienen alguna métrica específica que hoy les cuesta mucho trabajo obtener?"
- "¿Quién en su equipo usaría esto diariamente?"

---

## Tiempos sugeridos

| Sección | Duración | Acumulado |
|---|---|---|
| A — Apertura | 3 min | 3 min |
| B — Login | 2 min | 5 min |
| C — Ventas | 8 min | 13 min |
| D — Compras | 4 min | 17 min |
| E — Inventario | 4 min | 21 min |
| F — Finanzas | 4 min | 25 min |
| G — Operaciones | 4 min | 29 min |
| H — Cierre | 3 min | 32 min |
| Preguntas y objeciones | 10-15 min | 47 min |

**Demo total: ~30 min de presentación + 15 min de Q&A = 45 min**
