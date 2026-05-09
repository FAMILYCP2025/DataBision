---
name: powerbi-embedded-mvp
description: Implementa la capa MVP de Power BI Embedded en DataBision usando el blueprint como contrato: servicio backend, endpoint embed-token, componente React y validación previa de permisos.
---

# Power BI Embedded MVP

Objetivo:
Implementar lo mínimo funcional para embebido de reportes en el MVP.

Reglas:
1. Nunca exponer credenciales de Power BI al frontend.
2. El frontend solo consume embed token generado por backend.
3. Backend debe validar permisos antes de generar token.
4. Preparar integración real con Service Principal, pero si aún no hay credenciales, dejar implementación segura y desacoplada.
5. Registrar VIEW_REPORT en auditoría.

Entregables esperados:
- servicio backend de embed token
- endpoint de reports/{id}/embed-token
- componente React EmbedReport
- hook useEmbedToken
- manejo básico de expiración