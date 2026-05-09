---
name: backend-clean-architecture
description: Implementa backend de DataBision respetando Clean Architecture y las reglas de tenancy, auth y auditoría definidas en CLAUDE.md y databision-blueprint.md.
---

# Backend Clean Architecture

Objetivo:
Construir backend .NET 8 respetando estrictamente:
- Domain sin dependencias externas
- Application con lógica de negocio
- Infrastructure con EF Core, Blob, Power BI
- Api con controllers y middleware

Reglas:
1. Nunca exponer entidades de dominio directamente.
2. Usar DTOs en controllers.
3. Toda query de datos debe respetar company_id.
4. Toda mutación importante debe registrar auditoría.
5. No usar raw SQL salvo necesidad extrema y parametrizada.
6. Respetar exactamente nombres y capas definidas por el blueprint.

Antes de implementar:
- revisar si ya existe clase equivalente
- no duplicar servicios
- mantener nombres consistentes

Al terminar:
- ejecutar build
- listar errores pendientes
- proponer siguiente bloque lógico