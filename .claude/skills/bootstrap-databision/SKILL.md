---
name: bootstrap-databision
description: Inicializa la base del proyecto DataBision siguiendo databision-blueprint.md y CLAUDE.md. Úsala al crear la estructura inicial del repo, solución .NET, frontend React y configuración base del MVP.
---

# Bootstrap DataBision

Siempre usa como contrato:
- databision-blueprint.md
- CLAUDE.md

Objetivo:
Crear la base técnica inicial del proyecto sin desviarse del MVP.

Checklist:
1. Crear estructura del repositorio.
2. Crear solución .NET 8 con:
   - DataBision.Api
   - DataBision.Application
   - DataBision.Domain
   - DataBision.Infrastructure
3. Configurar EF Core.
4. Crear entidades base y AppDbContext.
5. Crear migración inicial.
6. Crear frontend con React + TypeScript + Vite.
7. Configurar Tailwind.
8. Crear README inicial.
9. Dejar integraciones de Azure, Power BI y ADF como placeholders o interfaces, no implementaciones reales.

Restricciones:
- No agregar features fuera del MVP.
- No redefinir la arquitectura.
- Priorizar compilación correcta y estructura limpia.
- Al finalizar, mostrar árbol de archivos y confirmar qué compila y qué falta.