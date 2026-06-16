# Demo KSDEPOR — Limitaciones Conocidas

Sprint 8L — Junio 2026

Este documento lista las limitaciones conocidas del ambiente de demo. Ninguna es bloqueante para la demo comercial, pero el equipo debe conocerlas para responder preguntas del cliente con honestidad.

---

## Extracción SAP

### OITW — Stock por almacén (no activo)

- **Estado:** Preparado en catálogo pero no activo.
- **Causa:** Service Layer de KSDEPOR responde "Unrecognized resource path" para el endpoint `/StockTransferLines` o similar usado para OITW.
- **Impacto:** Los datos de stock por almacén en el dashboard de Inventario provienen de un cálculo alternativo, no del objeto OITW directo.
- **Mitigación:** El dashboard de almacenes muestra datos del transform sobre OPDN/OWTR. En producción se revisará el endpoint correcto con el consultor SAP.

### OPDN — Recepciones de compra (error en última ejecución)

- **Estado:** Objeto activo, pero la última ejecución registró ERROR.
- **Causa probable:** Timeout transitorio del Service Layer o respuesta 4xx durante la última extracción nocturna.
- **Impacto:** `purchase_receiving_dashboard` puede mostrar datos del penúltimo run exitoso, no de hoy.
- **Mitigación:** El histórico de recepciones sigue disponible. Para demo: ejecutar un nuevo run antes de la sesión si es necesario.

---

## Datos

### Datos de desarrollo, no productivos definitivos

- Los datos en Supabase DEV/TST son extracciones controladas del ambiente SAP de KSDEPOR.
- Se extrajeron con límites de paginación (máx. 2 páginas por objeto en algunos casos) para no saturar el sistema.
- Los volúmenes son menores a los que habría en una extracción completa de producción.
- **Para demo:** Los datos son reales de KSDEPOR, pero el volumen no refleja la base completa de clientes/productos históricos.

### AP Aging puede estar en cero

- El módulo de Cuentas por Pagar (AP aging) puede mostrar tabla vacía si los datos de `OPCH` (facturas de proveedor) no completaron el transform correctamente.
- **Mitigación:** Ejecutar `--transform --include-mart` antes de demo. Si persiste vacío, mencionar que AP aging está en validación y mostrar AR aging que sí tiene datos.

### KPIs de inventario dependen del volumen extraído

- La rotación (FAST/NORMAL/SLOW/NO_MOVEMENT) se calcula con los movimientos de stock disponibles en DEV/TST.
- Si el período de extracción es corto, muchos ítems aparecerán como NO_MOVEMENT aunque en producción se muevan regularmente.

---

## Alertas y Operaciones

### Alertas OPS pueden repetirse

- Las alertas en `ops.alert_event` no tienen deduplicación activa aún.
- Si una regla se evalúa en cada transform run, puede aparecer múltiples veces la misma alerta.
- **Impacto en demo:** El contador de alertas puede ser alto. Explicar que en producción se implementará deduplicación con ventana temporal.

### Health score calculado con datos parciales

- `ops.pipeline_health.health_score` se calcula con los runs disponibles. En ambiente de desarrollo con pocas ejecuciones, puede mostrar un score conservador aunque el sistema esté funcionando bien.

---

## Frontend

### Hardening UX pendiente

- El frontend es funcional para demo pero no ha pasado por hardening visual final.
- Posibles áreas de mejora post-demo: animaciones de transición, responsive en móvil, accesibilidad ARIA completa.
- **Impacto en demo:** Ninguno si se usa en laptop/desktop con browser moderno.

### Branding por tenant sin configurar

- El tenant KSDEPOR en el ambiente de desarrollo usa los colores por defecto (`--brand-primary: #2563EB`).
- En producción, cada empresa puede tener su propio logo, colores y nombre en el portal.
- **Para demo:** Mencionar esta capacidad aunque no esté visible en el ambiente actual.

---

## Seguridad

### JWT en desarrollo puede tener validaciones relajadas

- En `appsettings.Development.json` algunas validaciones de JWT (issuer, audience) pueden estar deshabilitadas para facilitar el desarrollo local.
- En producción, la configuración de seguridad es estricta: RS256, validación completa de claims, refresh tokens rotados.
- **Para demo:** No mostrar la configuración local. Describir el modelo de seguridad de producción.

---

## Power BI

### Power BI Embedded no implementado en este sprint

- Los dashboards de Power BI embebidos (`/client/modules/:slug`) son la funcionalidad de la plataforma original.
- Los dashboards por proceso (Ventas, Compras, etc.) son la nueva capa de BI nativo desarrollada en Sprint 8K.
- Ambas capas coexisten en la plataforma, pero para la demo de KSDEPOR se muestra el BI nativo.
- **Mensaje comercial:** "Power BI se puede integrar como capa adicional sobre los mismos datos."

---

## Resumen para Presentador

| Limitación | Impacto demo | Qué decir si preguntan |
|---|---|---|
| OITW no activo | Bajo | "Estamos validando el endpoint con SAP. Los almacenes se muestran desde datos derivados." |
| OPDN error último run | Bajo | "La extracción es automática. Si hay un error puntual, el sistema lo registra y reintenta." |
| Datos DEV/TST | Medio | "Estos son datos reales de su SAP en ambiente de prueba. Producción tendrá el historial completo." |
| AP aging vacío | Medio | "AP aging está en validación. AR aging (cuentas por cobrar) está completo." |
| Alertas repetidas | Bajo | "Las alertas se deduplicarán en la versión de producción." |
| UX no final | Bajo | "El diseño visual puede ajustarse con los colores y logo de su empresa." |
| JWT laxo en dev | Sin impacto | No es visible en demo. Solo aplica si alguien pregunta por seguridad. |
| Power BI | Bajo | "Power BI está en el roadmap como capa adicional." |
