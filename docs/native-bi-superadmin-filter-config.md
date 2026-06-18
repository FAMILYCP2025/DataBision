# Native BI — Configuración avanzada (SuperAdmin)

Sprint 13F implementa una sección en el panel SuperAdmin para configurar por empresa:
- Filtros activos en dashboards Native BI
- Campos UDF de artículos (OITM) expuestos como filtros
- Dimensiones de centros de costo SAP B1 habilitadas (1–5)

---

## Entidades y tablas

### cfg.native_bi_filter_configs
| Columna | Tipo | Descripción |
|---|---|---|
| CompanyId | INTEGER PK FK | ID de la empresa (companies.Id) |
| FilterKey | TEXT PK | Clave del filtro (ej. "warehouseCode") |
| Label | TEXT? | Etiqueta visible en la UI |
| IsEnabled | BOOL | Si aparece en el dashboard |
| IsAdvanced | BOOL | Si aparece en panel expandido de filtros |
| DisplayOrder | INT | Orden de aparición |
| DefaultValue | TEXT? | Valor por defecto opcional |

### cfg.native_bi_item_udf_filter_configs
| Columna | Tipo | Descripción |
|---|---|---|
| CompanyId | INTEGER PK FK | ID de la empresa |
| UdfFieldName | TEXT PK | Nombre del campo UDF en SAP (ej. "U_Category") |
| Label | TEXT? | Etiqueta visible |
| IsEnabled | BOOL | Si aparece como filtro |
| IsMultiSelect | BOOL | Permitir selección múltiple |
| DisplayOrder | INT | Orden de aparición |

### cfg.native_bi_dimension_configs
| Columna | Tipo | Descripción |
|---|---|---|
| CompanyId | INTEGER PK FK | ID de la empresa |
| DimensionNumber | INTEGER PK | Número de dimensión SAP (1–5) |
| Label | TEXT? | Nombre de la dimensión (ej. "Región", "Proyecto") |
| IsEnabled | BOOL | Si se usa en esta empresa |

---

## Endpoints API (SuperAdmin)

Todos requieren rol `SuperAdmin` y JWT válido.

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/admin/companies/{id}/native-bi/filters` | Lista filtros configurados |
| PUT | `/api/admin/companies/{id}/native-bi/filters/{filterKey}` | Crea o actualiza un filtro |
| GET | `/api/admin/companies/{id}/native-bi/item-udf-filters` | Lista campos UDF configurados |
| PUT | `/api/admin/companies/{id}/native-bi/item-udf-filters/{udfFieldName}` | Crea o actualiza un campo UDF |
| GET | `/api/admin/companies/{id}/native-bi/dimensions` | Lista configuración de dimensiones |
| PUT | `/api/admin/companies/{id}/native-bi/dimensions/{dimensionNumber}` | Crea o actualiza una dimensión (1–5) |

---

## Flujo de uso

1. SuperAdmin abre **CompanyDetailPage** → tab "Native BI — Configuración avanzada"
2. Sección **Filtros activos**: agrega claves de filtro (ej. `warehouseCode`, `itemGroupCode`) con etiquetas opcionales
3. Sección **Campos UDF**: agrega campos SAP (ej. `U_Category`) para que aparezcan en dropdowns de inventario/ventas
4. Sección **Dimensiones**: habilita las dimensiones SAP B1 que usa la empresa y les asigna nombre

### Importante
- Las claves de filtro deben coincidir con columnas/campos reales en las tablas MART de Supabase
- Los campos UDF deben existir en la tabla `raw.sap_oitm` (campo `u_*`) para que se extraigan del ETL
- Las dimensiones se mapean a los campos `occ1`–`occ5` de `raw.sap_ojdt`
- Esta configuración está almacenada en la DB de la aplicación (AppDbContext / SQLite/SQL Server), no en Supabase

---

## Arquitectura

```
NativeBiAdminController (Api)
  → INativeBiAdminConfigService (Application/Interfaces)
  → NativeBiAdminConfigService (Infrastructure/Repositories) — usa AppDbContext

Domain entities:
  NativeBiFilterConfig, NativeBiItemUdfFilterConfig, NativeBiDimensionConfig
  → src/DataBision.Domain/Entities/

EF Configurations:
  → src/DataBision.Infrastructure/Data/Configurations/NativeBiFilter*Configuration.cs

Migration:
  20260617200000_AddNativeBiAdvancedConfig (AppDbContext)
```

---

## Estado actual (Sprint 13F)

- La configuración se guarda correctamente pero **aún no es consumida por el frontend del portal**.
- El portal Native BI muestra filtros hardcodeados. La integración de esta config con el portal (leer desde el backend cuáles filtros mostrar) es trabajo futuro.
- Las claves UDF y dimensiones habilitadas tampoco modifican los queries MART todavía — son metadata preparatoria para el siguiente sprint.
