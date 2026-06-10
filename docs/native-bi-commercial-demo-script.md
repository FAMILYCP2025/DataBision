# DataBision Native BI — Script de Demo Comercial

**Duración estimada:** 10–12 minutos  
**Audiencia:** Dueños de empresa, gerentes comerciales, directores de operaciones (usuarios SAP B1 no técnicos)  
**Objetivo:** Demostrar que DataBision convierte datos de SAP Business One en dashboards listos para tomar decisiones, sin Excel y sin esperar al contador.

---

## Preparación (antes de la reunión)

1. Tener el browser con la sesión activa en `/?tenant=<demo-slug>` — no mostrar pantalla de login.
2. Datos del tenant demo frescos (extractor corrió en las últimas 24 h).
3. Viewport en 1280px mínimo para que se vean los 4 KPI cards en una fila.
4. Cerrar notificaciones y Slack.
5. Tener preparada la URL de diagnósticos por si el cliente pregunta sobre los datos en tiempo real.

---

## Script

### Apertura (1 min)

> *"Antes de mostrarte DataBision, una pregunta: ¿cuántas horas a la semana dedica alguien de tu equipo a sacar reportes de SAP y armarlo en Excel?"*

Escuchar respuesta. Luego:

> *"Lo que voy a mostrarte toma esas horas y las reemplaza por un portal que se actualiza solo, desde tu propio SAP B1, sin Excel, sin macros, sin esperar que IT o el contador prepare el reporte."*

Navegar a `/client/bi/dashboard`.

---

### Bloque 1 — Dashboard ejecutivo (3 min)

> *"Esto es lo primero que ves al entrar. Los cuatro números más importantes de tu negocio: ventas netas del mes, cantidad de facturas, clientes activos y ticket promedio."*

Señalar los KPI cards.

> *"Estos números vienen directo de tu SAP — no hay carga manual, no hay posibilidad de error humano. El sistema los actualiza automáticamente."*

Señalar el timestamp de actualización en el header y el `SyncStatusWidget`.

> *"Acá arriba ves cuándo fue la última sincronización. Verde significa que todo está funcionando. Si hubiera algún problema de conexión con SAP, lo verías acá antes de tomar una decisión basada en datos viejos."*

Señalar el gráfico de barras.

> *"Este gráfico muestra cómo evolucionaron tus ventas día a día en los últimos 30 días. En 2 segundos ves si la semana fue buena o mala, sin abrir SAP."*

Señalar la tabla de top clientes.

> *"Y acá están tus mejores clientes por volumen de ventas. Útil para reuniones de equipo comercial — quién está comprando, cuánto, con qué frecuencia."*

---

### Bloque 2 — Módulo de Ventas (4 min)

Navegar a `/client/bi/sales`.

> *"Cuando quiero profundizar, entro a Ventas. Acá puedo filtrar cualquier rango de fechas."*

Cambiar el DateRangePicker a "este mes".

> *"El período se actualiza al instante — ventas brutas, netas, cantidad de facturas, ticket promedio para esas fechas exactas."*

Señalar las tabs.

> *"Y debajo tengo tres vistas: por cliente, por producto, y por vendedor. Empecemos por clientes."*

Click en tab "Clientes".

> *"Ordenado por ventas netas de mayor a menor. Puedo ver quiénes son mis 20 mejores clientes, cuántas facturas emitieron, cuál es su ticket promedio y cuándo fue la última compra."*

Click en la columna "Ticket prom." para ordenar.

> *"Puedo reordenar por cualquier columna. Si quiero ver quiénes tienen el ticket más alto, un click."*

Click en tab "Productos".

> *"Mismo análisis para productos: qué se vendió más, cuántas unidades, a qué precio."*

Click en tab "Vendedores".

> *"Y por vendedor: cuánto vendió cada uno, cuántos clientes atendió, cuál es su ticket promedio. Perfecta para reunión de equipo comercial del lunes."*

---

### Bloque 3 — Cierre de valor (2 min)

> *"Lo que acabas de ver no requiere ninguna configuración de tu parte ni de IT. Se conecta a tu SAP existente. Los datos son los mismos que ya tenés — solo que ahora están presentados de una forma que los podés usar sin ser consultor SAP."*

> *"Todo esto se ve igual en tu celular — podés revisar las ventas del día camino a una reunión, sin abrir SAP ni pedirle nada a nadie."*

> *"¿Esto resuelve algún problema concreto que tenés hoy con tus reportes?"*

Escuchar. Si pregunta por datos adicionales (inventario, cuentas por cobrar, compras):

> *"Esos módulos están en hoja de ruta — el primero que activamos es siempre ventas porque es donde el dolor es más inmediato. Los siguientes módulos se activan sin costo de implementación adicional."*

---

### Si preguntan por la seguridad de los datos (opcional)

> *"Cada empresa tiene su propio espacio aislado — tus datos no son visibles para otros clientes. La conexión con SAP es de solo lectura: DataBision lee tus datos pero nunca los modifica."*

---

### Si preguntan por la instalación

> *"Hay un pequeño agente que se instala en tu servidor donde corre SAP — toma menos de 30 minutos. Una vez configurado, no hay nada más que hacer. Se encarga solo."*

---

## Cierre

> *"¿Querés que lo dejemos configurado con tus datos reales esta semana?"*

---

*Script versión 1.0 — 2026-06-08*
