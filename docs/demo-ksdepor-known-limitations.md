# Demo KSDEPOR — Limitaciones Conocidas

Sprint 8M — Junio 2026 (actualizado desde Sprint 8L)

Este documento lista las limitaciones conocidas del ambiente de demo. Ninguna es bloqueante para la demo comercial, pero el equipo debe conocerlas para responder preguntas del cliente con honestidad.

---

## Extracción SAP

### OITW — Stock por almacén (no activo)

- **Estado:** Preparado en catálogo pero no activo.
- **Causa:** Service Layer de KSDEPOR responde "Unrecognized resource path" para el endpoint de OITW.
- **Impacto:** El dashboard de Inventario (tab Almacenes) muestra datos derivados de transferencias de stock (OWTR), no del stock directo por almacén.
- **Mensaje en UI:** "Stock por almacén pendiente de habilitar según endpoint disponible en Service Layer. Actualmente se muestran movimientos y rotación."
- **Mitigación para producción:** Verificar el endpoint correcto de stock por almacén con el consultor SAP. El objeto `OITW` puede requerir una ruta diferente en esta versión de Service Layer.

### OPDN — Recepciones de compra (error en runs anteriores — CORREGIDO)

- **Estado:** ✅ CORREGIDO en Sprint 8M. Último run: SUCCESS (2026-06-16 03:22 UTC, 6 filas, 1 página).
- **Causa del error anterior:** La API en `localhost:5103` no estaba activa durante runs programados. El extractor lee de SAP correctamente, pero envía los datos vía API REST. Si la API está apagada, el run registra ERROR aunque SAP respondió bien.
- **Prevención:** Siempre levantar la API antes de correr extracciones con `--send`. Ver runbook en `demo-ksdepor-local-runbook.md`.
- **Nota histórica:** Los 7 runs de OPDN en `extractor_run` incluyen 4 ERRORs históricos (API apagada) y 3 SUCCESS. El historial no se limpia — es parte de la trazabilidad del sistema.

---

## Datos

### Datos de desarrollo, no productivos definitivos

- Los datos en Supabase DEV/TST son extracciones controladas del ambiente SAP de KSDEPOR.
- Se extrajeron con límites de paginación (máx. 2 páginas por objeto en algunos casos) para no saturar el sistema.
- Los volúmenes son menores a los que habría en una extracción completa de producción.
- **Para demo:** Los datos son reales de KSDEPOR, pero el volumen no refleja la base completa de clientes/productos históricos.

### AP Aging puede estar vacío

- El módulo de Cuentas por Pagar (AP aging) puede mostrar tabla vacía.
- **Causa:** Los datos de `OPCH` (facturas de proveedor) pueden no estar completamente transformados.
- **Mensaje en UI:** "Sin datos de cuentas por pagar en el ambiente de demo. Este indicador queda disponible al completar la carga histórica de facturas de proveedor."
- **Mitigación:** Ejecutar `--transform --include-mart` antes de demo. Si persiste vacío, mencionar que AP aging requiere carga histórica completa.

### KPIs de inventario dependen del volumen extraído

- La rotación (FAST/NORMAL/SLOW/NO_MOVEMENT) se calcula con los movimientos de stock disponibles en DEV/TST.
- Si el período de extracción es corto, muchos ítems aparecerán como NO_MOVEMENT aunque en producción se muevan regularmente.

---

## Alertas y Operaciones

### Alertas OPS — DEDUPLICACIÓN IMPLEMENTADA

- **Estado:** ✅ MITIGADO en Sprint 8M. Migración `20260616010000_DeduplicateOpsAlerts` aplicada.
- **Qué cambió:** `ops.evaluate_alert_rules` ahora verifica `NOT EXISTS` antes de insertar. Si ya existe una alerta activa (no resuelta) para la misma company_id + rule_code, no se inserta otra igual.
- **Evidencia:** `alert_event` permaneció en 44 eventos después de múltiples runs de prueba post-migración.
- **Historial:** Los 44 eventos previos son históricos y no se eliminaron. Es trazabilidad válida del sistema.

### Health score calculado con datos parciales

- `ops.pipeline_health.health_score` se calcula con los runs disponibles. En ambiente de desarrollo con pocas ejecuciones, puede mostrar un score conservador aunque el sistema esté funcionando bien.
- Si `extractor_status` es ERROR (por OPDN fallida en el historial), el score puede reducirse hasta 60. Con OPDN SUCCESS, debería recuperarse en el próximo cálculo de pipeline health.

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

## Backend

### Warnings menores eliminados

- **Estado:** ✅ CORREGIDO en Sprint 8M. Backend build: 0 errores, **0 warnings**.
- Corregido: `DiagnosticsService.cs` — tipo explícito en `SafeCheck<IReadOnlyList<TableCountDto>?>`.
- Corregido: `SapRawRepository.cs` — `ILogger` almacenado en campo `_log` en lugar de parámetro primario sin usar.

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

## Resumen para Presentador (actualizado Sprint 8M)

| Limitación | Estado | Qué decir si preguntan |
|---|---|---|
| OITW no activo | ⚠️ Pendiente | "Estamos validando el endpoint con SAP. Los almacenes se muestran desde datos derivados." |
| OPDN error anterior | ✅ Corregido | "La extracción es automática. Si la API está apagada, el extractor lo registra y reintenta. Último run: SUCCESS." |
| Datos DEV/TST | ⚠️ Esperado | "Estos son datos reales de su SAP en ambiente de prueba. Producción tendrá el historial completo." |
| AP aging vacío | ⚠️ Pendiente datos | "AP aging está en validación. AR aging (cuentas por cobrar) está completo." |
| Alertas repetidas | ✅ Mitigado | "Implementamos deduplicación. Las alertas no se repiten mientras estén activas." |
| UX no final | ⚠️ Esperado | "El diseño visual puede ajustarse con los colores y logo de su empresa." |
| JWT laxo en dev | Sin impacto | No es visible en demo. Solo aplica si alguien pregunta por seguridad. |
| Power BI | ⚠️ En roadmap | "Power BI está en el roadmap como capa adicional." |
| Build warnings | ✅ Corregido | N/A — sin impacto en demo. |
