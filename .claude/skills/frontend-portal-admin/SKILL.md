---
name: frontend-portal-admin
description: Construye el frontend de DataBision separando claramente admin.databision.app y el portal cliente por subdominio, con React, TypeScript, Tailwind y theming por tenant.
---

# Frontend Portal Admin

Objetivo:
Implementar frontend del MVP respetando la separación entre:
- admin databision
- portal cliente por subdominio

Reglas:
1. Mantener apps separadas en:
   - src/apps/admin
   - src/apps/portal
2. App.tsx detecta subdominio y renderiza la app correcta.
3. Usar TypeScript strict.
4. No usar any.
5. Los colores de branding deben usar CSS variables.
6. El login del portal debe ser branded por empresa.
7. Las rutas protegidas deben depender de auth real, no solo de frontend state.

No hacer todavía:
- diseño complejo innecesario
- animaciones
- features fuera del MVP

Al terminar:
- mostrar rutas creadas
- mostrar componentes clave
- confirmar si compila