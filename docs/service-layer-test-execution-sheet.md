# SAP B1 Service Layer — Test Execution Sheet

**Version:** 1.0  
**Ref:** `docs/service-layer-validation-plan.md`  
**Ejecutado por:** ___________________________  
**Cliente / CompanyDB:** ___________________________  
**SAP B1 versión:** ___________________________  
**SL Base URL:** ___________________________  
**Fecha:** ___________________________  
**Herramienta:** [ ] Postman  [ ] curl  [ ] Bruno  [ ] Otro: ___________

> **Cómo usar esta planilla:**
> - Reemplaza `{SL}` con la URL base del Service Layer (ej. `https://10.20.30.40:50000/b1s/v1`)
> - Reemplaza `{SESSION}` con el valor del cookie `B1SESSION` obtenido en V-LOGIN-01
> - Completa "Resultado obtenido" y "Estado" durante la ejecución
> - Registra cualquier desviación del resultado esperado en "Observaciones"
> - Estado: **PASS** | **FAIL** | **SKIP** (si no aplica) | **WARN** (pasa con observación)

---

## Sección 1 — Datos del Ambiente

> Completar ANTES de ejecutar cualquier prueba.

| Campo | Valor |
|---|---|
| SAP B1 versión (ej. 10.0.195) | |
| Service Layer versión (campo `Version` en Login) | |
| Tipo de base de datos (HANA / SQL Server) | |
| Puerto Service Layer (default: 50000) | |
| Certificado SSL (Válido / Auto-firmado / HTTP sin TLS) | |
| Timeout de sesión declarado (campo `SessionTimeout` en Login) | |
| Nombre del cookie de sesión (`B1SESSION` u otro) | |
| Formato de `UpdateDate` observado (ej. `"2026-01-15T00:00:00Z"`) | |
| Total filas OINV (`GET {SL}/Invoices?$count=true&$top=0`) | |
| Total filas OCRD (`GET {SL}/BusinessPartners?$count=true&$top=0`) | |
| Total filas OITM (`GET {SL}/Items?$count=true&$top=0`) | |
| Total filas OSLP (`GET {SL}/SalesPersons?$count=true&$top=0`) | |

---

## Sección 2 — Login y Sesión

---

### V-LOGIN-01 — Login con credenciales válidas

| Campo | Detalle |
|---|---|
| **Objetivo** | Verificar que el login retorna sesión válida, cookie y timeout |
| **Prerequisito** | Credenciales del usuario `DBI_READER` disponibles |

**Request:**
```
POST {SL}/Login
Content-Type: application/json

{
  "CompanyDB": "{company_db}",
  "UserName":  "{username}",
  "Password":  "{password}"
}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 200 | | |
| Campo `SessionId` en body | Presente, no vacío | | |
| Campo `SessionTimeout` en body | Presente, ≥ 28 | | |
| Cookie `B1SESSION` en response headers | Presente | | |
| Tiempo de respuesta | < 3 000 ms | | |

**Registrar:**

| Métrica | Valor |
|---|---|
| `SessionTimeout` exacto retornado | |
| Nombre exacto del cookie de sesión | |
| Formato del body (odata.metadata / @odata.context / otro) | |
| Tiempo de respuesta (ms) | |

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

> **⚠️ Guardar el valor de `B1SESSION`** — se usa en todos los tests siguientes como `{SESSION}`.

---

### V-LOGIN-02 — Login con credenciales inválidas

| Campo | Detalle |
|---|---|
| **Objetivo** | Confirmar que credenciales incorrectas retornan error controlado (no hang, no 500) |
| **Prerequisito** | Ninguno |

**Request:**
```
POST {SL}/Login
Content-Type: application/json

{
  "CompanyDB": "{company_db}",
  "UserName":  "invalid_user_dbi",
  "Password":  "wrong_password_123"
}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 401 o 400 (nunca 200 ni 500) | | |
| Body contiene mensaje de error legible | Sí | | |
| Cookie `B1SESSION` ausente en response | Ausente | | |
| Tiempo de respuesta | < 5 000 ms (no cuelga) | | |

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

### V-LOGIN-03 — Login con CompanyDB inexistente

| Campo | Detalle |
|---|---|
| **Objetivo** | Verificar que una base de datos incorrecta retorna error específico |
| **Prerequisito** | Ninguno |

**Request:**
```
POST {SL}/Login
Content-Type: application/json

{
  "CompanyDB": "NONEXISTENT_DB_DBI_TEST",
  "UserName":  "{username}",
  "Password":  "{password}"
}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 400, 404 o 401 (nunca 200) | | |
| Body contiene mensaje específico de DB no encontrada | Sí | | |

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

### V-LOGIN-04 — Logout e invalidación de sesión

| Campo | Detalle |
|---|---|
| **Objetivo** | Confirmar que el logout invalida el token y los requests posteriores retornan 401 |
| **Prerequisito** | `{SESSION}` válido de V-LOGIN-01 |

**Paso 1 — Ejecutar logout:**
```
POST {SL}/Logout
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 204 (algunas versiones: 200) | | |

**Paso 2 — Usar sesión invalidada:**
```
GET {SL}/Invoices?$top=1
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status tras logout | 401 | | |

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

### V-SESSION-01 — Reutilización de cookie de sesión (10 requests)

| Campo | Detalle |
|---|---|
| **Objetivo** | Confirmar que la sesión es estable para múltiples requests consecutivos sin re-autenticar |
| **Prerequisito** | `{SESSION}` válido |

**Request (repetir 10 veces):**
```
GET {SL}/Invoices?$top=1&$select=DocEntry
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| Requests 1–10 retornan HTTP 200 | Todos 200 | | |
| Ningún request retorna 401 inesperado | 0 errores 401 | | |
| Tiempo de respuesta estable | < 500 ms c/u tras el 1.° | | |

**Registrar tiempos (ms):**

| Req | T (ms) | Req | T (ms) |
|---|---|---|---|
| 1 | | 6 | |
| 2 | | 7 | |
| 3 | | 8 | |
| 4 | | 9 | |
| 5 | | 10 | |

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

### V-SESSION-02 — Detección de sesión expirada

| Campo | Detalle |
|---|---|
| **Objetivo** | Verificar que el HTTP status y body al usar sesión expirada son identificables por el extractor |
| **Prerequisito** | Solo ejecutar si SessionTimeout ≤ 5 min. **Omitir si SessionTimeout = 30 min.** |
| **Condición** | [ ] Ejecutar  [ ] SKIP — SessionTimeout > 5 min |

**Instrucción:** Esperar `SessionTimeout + 2 minutos` desde el último uso de `{SESSION}`, luego:
```
GET {SL}/Invoices?$top=1
Cookie: B1SESSION={SESSION_EXPIRADA}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 401 | | |
| Body indica sesión inválida/expirada | Sí — texto o código específico | | |

**Registrar:**

| Métrica | Valor |
|---|---|
| Cuerpo exacto de la respuesta 401 | |
| ¿Incluye código de error específico? | |

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] SKIP | |

---

## Sección 3 — Metadata y Descubrimiento de Schema

---

### V-META-01 — Metadata OData

| Campo | Detalle |
|---|---|
| **Objetivo** | Confirmar que los 4 entity sets están presentes y documentar tipos de `UpdateDate` |
| **Prerequisito** | `{SESSION}` válido |

**Request:**
```
GET {SL}/$metadata
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 200 | | |
| Tipo de contenido | XML o JSON | | |
| EntitySet `Invoices` presente | Sí | | |
| EntitySet `BusinessPartners` presente | Sí | | |
| EntitySet `Items` presente | Sí | | |
| EntitySet `SalesPersons` presente | Sí | | |
| Tiempo de respuesta | < 10 000 ms | | |

**Registrar tipos de `UpdateDate`:**

| Entidad | Tipo OData de `UpdateDate` | `UpdateTS` presente | Tipo de `UpdateTS` |
|---|---|---|---|
| Invoices | | | |
| BusinessPartners | | | |
| Items | | | |
| SalesPersons | | | |

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

### V-META-02 — Presencia de campos OINV

| Campo | Detalle |
|---|---|
| **Objetivo** | Confirmar que todos los campos del `$select` definido para OINV existen en esta versión de SL |
| **Prerequisito** | `{SESSION}` válido |

**Request:**
```
GET {SL}/Invoices?$top=1&$select=DocEntry,DocNum,CardCode,DocDate,DocDueDate,TaxDate,DocTotal,DocTotalSy,VatSum,PaidToDate,DocCur,DocStatus,SalesPersonCode,Comments,Cancelled,CreateDate,UpdateDate
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 200 | | |
| Ningún campo del `$select` causa 400 | Sin errores 400 | | |
| `DocEntry` presente y es entero | Sí | | |
| `UpdateDate` presente y parseable | Sí | | |
| `DocTotal` es numérico (no string) | Sí | | |

**Registrar discrepancias de nombres de campo:**

| Campo del `$select` | Estado | Nombre real en respuesta (si difiere) |
|---|---|---|
| DocEntry | [ ] OK  [ ] ERROR | |
| DocNum | [ ] OK  [ ] ERROR | |
| CardCode | [ ] OK  [ ] ERROR | |
| DocDate | [ ] OK  [ ] ERROR | |
| TaxDate | [ ] OK  [ ] ERROR | |
| DocTotal | [ ] OK  [ ] ERROR | |
| DocTotalSy | [ ] OK  [ ] ERROR | |
| VatSum | [ ] OK  [ ] ERROR | |
| PaidToDate | [ ] OK  [ ] ERROR | |
| DocStatus | [ ] OK  [ ] ERROR | |
| SalesPersonCode | [ ] OK  [ ] ERROR | |
| Cancelled | [ ] OK  [ ] ERROR | |
| CreateDate | [ ] OK  [ ] ERROR | |
| UpdateDate | [ ] OK  [ ] ERROR | |

**Registrar formato de fechas observado:**

| Campo | Valor real en respuesta |
|---|---|
| `UpdateDate` | |
| `CreateDate` | |
| `DocDate` | |

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

### V-META-03 — Presencia de campos OCRD / OITM / OSLP

| Campo | Detalle |
|---|---|
| **Objetivo** | Confirmar que los campos definidos en el `$select` de cada entidad existen |
| **Prerequisito** | `{SESSION}` válido |

**Request OCRD:**
```
GET {SL}/BusinessPartners?$top=1&$select=CardCode,CardName,CardType,GroupCode,ContactPerson,Phone1,Phone2,Currency,SalesPersonCode,VatLiable,FederalTaxID,FrozenFor,CurrentAccountBalance,CreditLimit,CreateDate,UpdateDate
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 200 | | |
| `CardCode` presente y es string | Sí | | |
| `CurrentAccountBalance` es numérico | Sí | | |
| `CreditLimit` presente | Sí | | |

**Campos OCRD con discrepancias:** ___________________________

---

**Request OITM:**
```
GET {SL}/Items?$top=1&$select=ItemCode,ItemName,ItemsGroupCode,QuantityOnStock,CommittedQuantity,OrderedQuantity,AverageCost,LastPurchasePrice,CreateDate,UpdateDate
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 200 | | |
| `ItemCode` presente y es string | Sí | | |
| `ItemsGroupCode` presente | Sí | | |
| Tipo de `ItemsGroupCode` | integer o string — **anotar** | | |

> ⚠️ **Crítico:** El DTO de DataBision tiene `ItmsGrpCod` como `string?` pero la columna en Supabase es `INTEGER`. Anotar el tipo exacto retornado aquí.

**Tipo exacto de `ItemsGroupCode` en respuesta:** ___________________________

**Campos OITM con discrepancias:** ___________________________

---

**Request OSLP:**
```
GET {SL}/SalesPersons?$top=1&$select=SalesEmployeeCode,SalesEmployeeName,CreateDate,UpdateDate
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 200 | | |
| `SalesEmployeeCode` presente | Sí | | |
| Tipo de `SalesEmployeeCode` | integer o string — **anotar** | | |

**Tipo exacto de `SalesEmployeeCode`:** ___________________________

| Estado OCRD | Estado OITM | Estado OSLP |
|---|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | [ ] PASS  [ ] FAIL  [ ] WARN | [ ] PASS  [ ] FAIL  [ ] WARN |

---

## Sección 4 — OINV Extracción Incremental

---

### V-OINV-01 — Lectura básica de facturas

| Campo | Detalle |
|---|---|
| **Objetivo** | Confirmar acceso básico a la entidad `Invoices` y documentar estructura de respuesta |
| **Prerequisito** | `{SESSION}` válido |

**Request:**
```
GET {SL}/Invoices?$top=10&$orderby=DocEntry asc&$select=DocEntry,UpdateDate
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 200 | | |
| Array `value` presente con ≤ 10 objetos | Sí | | |
| `DocEntry` es entero en cada objeto | Sí | | |
| `UpdateDate` presente y no nulo | Sí | | |
| Tiempo de respuesta | < 2 000 ms | | |

**Registrar:**

| Métrica | Valor |
|---|---|
| Formato exacto de `UpdateDate` | |
| Primer `DocEntry` retornado | |
| Tiempo de respuesta (ms) | |

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

### V-OINV-02 — Filtro incremental por `UpdateDate` (formato ISO)

| Campo | Detalle |
|---|---|
| **Objetivo** | Confirmar que el operador `$filter=UpdateDate ge 'YYYY-MM-DD'` funciona y retorna resultados esperados |
| **Prerequisito** | `{SESSION}` válido. Usar una fecha de hace 30 días que tenga registros. |

> Reemplaza `{FECHA_30_DIAS}` con la fecha de hace 30 días en formato `YYYY-MM-DD` (ej. `2026-05-01`).

**Request (intento 1 — formato ISO):**
```
GET {SL}/Invoices?$filter=UpdateDate ge '{FECHA_30_DIAS}'&$top=10&$select=DocEntry,UpdateDate
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 200 (no 400) | | |
| Al menos 1 resultado | Sí | | |
| `UpdateDate` de resultados ≥ fecha del filtro | Sí | | |

**Si retorna 400 — probar formatos alternativos:**

| Variante | Request | Status | ¿Funciona? |
|---|---|---|---|
| SAP compacto | `$filter=UpdateDate ge '20260501'` | | |
| datetime literal | `$filter=UpdateDate ge datetime'2026-05-01T00:00:00'` | | |
| Sin comillas | `$filter=UpdateDate ge 2026-05-01` | | |

**Formato que funciona:** ___________________________

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

### V-OINV-03 — Ordenamiento compuesto por `UpdateDate` y `DocEntry`

| Campo | Detalle |
|---|---|
| **Objetivo** | Verificar que el `$orderby` compuesto funciona (necesario para watermark + keyset pagination) |
| **Prerequisito** | `{SESSION}` válido. Formato de fecha confirmado en V-OINV-02. |

**Request:**
```
GET {SL}/Invoices?$filter=UpdateDate ge '{FECHA_30_DIAS}'&$top=20&$select=DocEntry,UpdateDate&$orderby=UpdateDate asc,DocEntry asc
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 200 | | |
| Resultados ordenados por `UpdateDate` asc | Sí | | |
| Dentro del mismo `UpdateDate`, ordenados por `DocEntry` asc | Sí | | |
| No retorna 400 ni 501 | Correcto | | |

**Si el orderby compuesto falla — probar orderby simple:**
```
GET {SL}/Invoices?$filter=UpdateDate ge '{FECHA_30_DIAS}'&$top=20&$select=DocEntry,UpdateDate&$orderby=DocEntry asc
```
| Orderby compuesto funciona | [ ] Sí  [ ] No — usar orderby simple |
|---|---|

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

### V-OINV-04 — Ventana incremental de 2 horas

| Campo | Detalle |
|---|---|
| **Objetivo** | Simular el filtro de un ciclo incremental real (lookback de 2 horas) y medir el volumen |
| **Prerequisito** | `{SESSION}` válido |

> Reemplaza `{FECHA_HOY}` con la fecha de hoy y `{HACE_2H}` con la hora de hace 2 horas. Usar fecha del día actual para simplificar si el sistema tiene actividad reciente.

**Request:**
```
GET {SL}/Invoices?$filter=UpdateDate ge '{FECHA_HOY}'&$top=500&$select=DocEntry,UpdateDate&$orderby=UpdateDate asc,DocEntry asc
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 200 | | |
| Tiempo de respuesta | < 5 000 ms | | |

**Registrar:**

| Métrica | Valor |
|---|---|
| Filas retornadas (ventana del día) | |
| Tiempo de respuesta (ms) | |
| Estimación filas en ventana 2h (filas_día / 12) | |

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

### V-OINV-05 — `$select` completo (todos los campos del extractor)

| Campo | Detalle |
|---|---|
| **Objetivo** | Confirmar que el payload completo de OINV se puede extraer sin errores y con tipos correctos |
| **Prerequisito** | `{SESSION}` válido |

**Request:**
```
GET {SL}/Invoices?$filter=UpdateDate ge '{FECHA_30_DIAS}'&$top=5&$select=DocEntry,DocNum,DocDate,DocDueDate,TaxDate,CardCode,CardName,DocTotal,DocTotalSy,VatSum,PaidToDate,DocCur,DocStatus,SalesPersonCode,Comments,Cancelled,CreateDate,UpdateDate
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 200 | | |
| Todos los campos presentes en cada objeto | Sí | | |
| `DocTotal` es número (no string) | Sí | | |
| `DocEntry` es entero | Sí | | |
| `DocStatus` contiene `"O"`, `"C"` o `"Y"` | Sí | | |

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

## Sección 5 — OCRD Extracción Incremental

---

### V-OCRD-01 — Lectura básica y filtro incremental

| Campo | Detalle |
|---|---|
| **Objetivo** | Confirmar acceso a BusinessPartners con filtro de UpdateDate y validar tipos de campos clave |
| **Prerequisito** | `{SESSION}` válido |

**Paso 1 — Básico:**
```
GET {SL}/BusinessPartners?$top=5&$select=CardCode,CardName,CardType,UpdateDate
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 200 | | |
| `CardCode` es string | Sí | | |
| `CardType` contiene `"C"`, `"S"` o `"L"` | Sí | | |

**Paso 2 — Filtro incremental:**
```
GET {SL}/BusinessPartners?$filter=UpdateDate ge '{FECHA_30_DIAS}'&$top=10&$select=CardCode,UpdateDate
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 200 | | |
| Retorna registros del período | Sí | | |
| Mismo formato de fecha que OINV | Sí | | |

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

## Sección 6 — OITM Extracción Incremental

---

### V-OITM-01 — Lectura básica y tipo de `ItemsGroupCode`

| Campo | Detalle |
|---|---|
| **Objetivo** | Confirmar acceso a Items y determinar el tipo exacto de `ItemsGroupCode` (crítico para el mapeo DTO→DB) |
| **Prerequisito** | `{SESSION}` válido |

**Request:**
```
GET {SL}/Items?$top=5&$select=ItemCode,ItemName,ItemsGroupCode,QuantityOnStock,UpdateDate
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 200 | | |
| `ItemCode` es string | Sí | | |
| `QuantityOnStock` es numérico | Sí | | |

> ⚠️ **Registrar obligatoriamente:**

| Métrica | Valor |
|---|---|
| Tipo JSON de `ItemsGroupCode` | `"integer"` o `"string"` |
| Ejemplo de valor de `ItemsGroupCode` | |

**Filtro incremental:**
```
GET {SL}/Items?$filter=UpdateDate ge '{FECHA_30_DIAS}'&$top=10&$select=ItemCode,UpdateDate
Cookie: B1SESSION={SESSION}
```

| HTTP status | Resultado |
|---|---|
| | |

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

## Sección 7 — OSLP Extracción Incremental

---

### V-OSLP-01 — Lectura completa y filtro UpdateDate

| Campo | Detalle |
|---|---|
| **Objetivo** | Confirmar acceso a SalesPersons, determinar si soporta filtro UpdateDate o requiere full-refresh |
| **Prerequisito** | `{SESSION}` válido |

**Paso 1 — Fetch completo:**
```
GET {SL}/SalesPersons?$select=SalesEmployeeCode,SalesEmployeeName,UpdateDate
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 200 | | |
| Total filas < 500 (tabla pequeña) | Sí | | |
| `SalesEmployeeCode` presente | Sí | | |

| Tipo de `SalesEmployeeCode` | `"integer"` o `"string"` |
|---|---|
| | |

**Paso 2 — Filtro UpdateDate:**
```
GET {SL}/SalesPersons?$filter=UpdateDate ge '{FECHA_30_DIAS}'&$select=SalesEmployeeCode,UpdateDate
Cookie: B1SESSION={SESSION}
```

| Resultado | Acción |
|---|---|
| HTTP 200 con resultados | Filtro UpdateDate soportado — usar incremental |
| HTTP 400 o sin resultados | Usar full nightly refresh |

| `$filter` UpdateDate soportado en OSLP | [ ] Sí — incremental  [ ] No — full-refresh nocturno |
|---|---|

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

## Sección 8 — UpdateTS

---

### V-UPDATETS-01 — Disponibilidad y formato de `UpdateTS`

| Campo | Detalle |
|---|---|
| **Objetivo** | Determinar si `UpdateTS` está disponible en Service Layer, su tipo, y si puede usarse como tie-breaker intra-día para el watermark |
| **Prerequisito** | `{SESSION}` válido |
| **Criticidad** | Alto — determina la estrategia de watermark intra-día del extractor |

**Paso 1 — Solicitar `UpdateTS` en OINV:**
```
GET {SL}/Invoices?$top=5&$select=DocEntry,UpdateDate,UpdateTS&$orderby=DocEntry asc
Cookie: B1SESSION={SESSION}
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 200 | | |
| `UpdateTS` presente en respuesta | Idealmente sí | | |
| `UpdateTS` no nulo en al menos algunos registros | Sí | | |

**Registrar el tipo y formato de `UpdateTS`:**

| Caso | ¿Aplica? | Ejemplo de valor |
|---|---|---|
| `UpdateTS` presente como string `"HHMMSS"` (ej. `"143052"`) | [ ] Sí  [ ] No | |
| `UpdateTS` presente como entero (ej. `143052`) | [ ] Sí  [ ] No | |
| `UpdateTS` presente como timestamp ISO | [ ] Sí  [ ] No | |
| `UpdateTS` ausente en la respuesta OData | [ ] Sí (campo no expuesto) | |
| `UpdateTS` presente pero siempre nulo | [ ] Sí | |

**Paso 2 — Si `UpdateTS` está ausente, verificar `U_UpdateTS` (campo UDT, algunos partners lo populan):**
```
GET {SL}/Invoices?$top=3&$select=DocEntry,UpdateDate,U_UpdateTS
Cookie: B1SESSION={SESSION}
```

| Resultado | Nota |
|---|---|
| HTTP 200 con `U_UpdateTS` | Campo UDT disponible — usar para tie-breaking |
| HTTP 400 | `U_UpdateTS` no existe — no hay resolución intra-día |

**Decisión de arquitectura basada en este test:**

| Escenario | Decisión |
|---|---|
| `UpdateTS` disponible y confiable | Watermark compuesto `(UpdateDate, UpdateTS, DocEntry)` |
| `UpdateTS` ausente o siempre nulo | Watermark simple `(UpdateDate, DocEntry)` — mayor riesgo de gap intra-día |
| Solo full-refresh viable | Aplica solo para OSLP |

**Decisión registrada:** ___________________________

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN  [ ] N/A | |

---

## Sección 9 — Paginación

---

### V-PAGING-01 — Continuidad y no-repetición entre páginas

| Campo | Detalle |
|---|---|
| **Objetivo** | Verificar que `$top`/`$skip` produce páginas continuas sin solapamiento ni gaps de `DocEntry` — la condición más crítica para una extracción completa y sin duplicados |
| **Prerequisito** | `{SESSION}` válido. OINV con al menos 15 filas disponibles. |

**Paso 1 — Página 1 (`$skip=0`):**
```
GET {SL}/Invoices?$top=5&$skip=0&$orderby=DocEntry asc&$select=DocEntry
Cookie: B1SESSION={SESSION}
```

Registrar DocEntry retornados: ___________________________

**Paso 2 — Página 2 (`$skip=5`):**
```
GET {SL}/Invoices?$top=5&$skip=5&$orderby=DocEntry asc&$select=DocEntry
Cookie: B1SESSION={SESSION}
```

Registrar DocEntry retornados: ___________________________

**Paso 3 — Página 3 (`$skip=10`):**
```
GET {SL}/Invoices?$top=5&$skip=10&$orderby=DocEntry asc&$select=DocEntry
Cookie: B1SESSION={SESSION}
```

Registrar DocEntry retornados: ___________________________

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| Ningún `DocEntry` se repite entre páginas 1, 2 y 3 | 0 repeticiones | | |
| Los `DocEntry` de pág 2 son todos mayores que los de pág 1 | Sí | | |
| Los `DocEntry` de pág 3 son todos mayores que los de pág 2 | Sí | | |
| HTTP 200 en las 3 páginas | Sí | | |

**Paso 4 — Señal de última página (página más allá del total):**
```
GET {SL}/Invoices?$top=5&$skip=999999&$orderby=DocEntry asc&$select=DocEntry
Cookie: B1SESSION={SESSION}
```

| Señal de última página | Valor observado |
|---|---|
| Retorna `value: []` (array vacío) | [ ] Sí  [ ] No |
| Retorna `value` con < 5 elementos cuando hay pocos registros | [ ] Sí  [ ] No |
| Retorna `odata.nextLink` en última página | [ ] Sí (siempre)  [ ] No (solo en páginas intermedias) |

**Criterio de terminación de paginación a usar:**

| Opción | ¿Aplica? |
|---|---|
| `if len(results) < page_size → last_page` | [ ] Sí  [ ] No |
| `if 'odata.nextLink' not in response → last_page` | [ ] Sí  [ ] No |

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

### V-PAGE-03 — Degradación de `$skip` a gran escala

| Campo | Detalle |
|---|---|
| **Objetivo** | Medir si el rendimiento de `$skip` se degrada significativamente con valores altos (O(n²) en algunos SL) |
| **Prerequisito** | `{SESSION}` válido. OINV con al menos 5 000 filas. Si hay < 5 000 filas: **SKIP**. |
| **Condición** | Total OINV ≥ 5 000: [ ] Ejecutar  [ ] SKIP — menos de 5 000 filas |

**Mediciones:**

| Request | Tiempo (ms) |
|---|---|
| `GET /Invoices?$top=500&$skip=0&$orderby=DocEntry asc&$select=DocEntry` | |
| `GET /Invoices?$top=500&$skip=2500&$orderby=DocEntry asc&$select=DocEntry` | |
| `GET /Invoices?$top=500&$skip=4500&$orderby=DocEntry asc&$select=DocEntry` | |

| Ratio degradación `skip=4500 / skip=0` | Evaluación |
|---|---|
| ≤ 1.5× | ✅ Sin degradación significativa — usar `$skip` para initial load |
| 1.5–3× | ⚠️ Degradación moderada — usar date-chunked para initial load (> 50k filas) |
| > 3× | 🔴 Degradación severa — evitar `$skip` alto, usar date-chunked siempre |

**Ratio observado:** ___________________________  **Decisión:** ___________________________

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN  [ ] SKIP | |

---

### V-PAGE-04 — Soporte de `$count`

| Campo | Detalle |
|---|---|
| **Objetivo** | Verificar si `$count` está soportado para estimar el total de páginas antes de extraer |
| **Prerequisito** | `{SESSION}` válido |

**Request:**
```
GET {SL}/Invoices/$count
Cookie: B1SESSION={SESSION}
```

| Resultado | Acción |
|---|---|
| HTTP 200 con integer | ✅ `$count` disponible — usar para estimaciones |
| HTTP 404 o 501 | ℹ️ Probar variante `?$count=true&$top=0` |

**Variante alternativa:**
```
GET {SL}/Invoices?$count=true&$top=0
Cookie: B1SESSION={SESSION}
```

| Soporte de `$count` | [ ] Sí  [ ] No |
|---|---|
| Valor retornado | |

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

## Sección 10 — Retry y Resiliencia

---

### V-RETRY-01 — Comportamiento ante burst de requests

| Campo | Detalle |
|---|---|
| **Objetivo** | Determinar el umbral de rate limiting y el comportamiento ante HTTP 429 |
| **Prerequisito** | `{SESSION}` válido |

**Instrucción:** Enviar 20 requests consecutivos sin pausa.

```
# Repetir 20 veces sin pausa:
GET {SL}/Invoices?$top=1&$select=DocEntry
Cookie: B1SESSION={SESSION}
```

**Registrar:**

| # Request | HTTP Status | T (ms) | # Request | HTTP Status | T (ms) |
|---|---|---|---|---|---|
| 1 | | | 11 | | |
| 2 | | | 12 | | |
| 3 | | | 13 | | |
| 4 | | | 14 | | |
| 5 | | | 15 | | |
| 6 | | | 16 | | |
| 7 | | | 17 | | |
| 8 | | | 18 | | |
| 9 | | | 19 | | |
| 10 | | | 20 | | |

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| La mayoría retorna 200 | Sí | | |
| Si hay 429: incluye `Retry-After` header | Idealmente sí | | |
| La sesión sigue válida tras el burst | Sí — ningún 401 | | |

| 429 recibido | [ ] Sí — en request nro. ___  [ ] No |
|---|---|
| `Retry-After` presente | [ ] Sí — valor: ___s  [ ] No |
| Degradación de latencia (req 20 vs req 1) | ___× |

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

### V-RETRY-04 — Respuesta exacta al usar sesión inválida

| Campo | Detalle |
|---|---|
| **Objetivo** | Documentar el HTTP status y body exactos cuando se usa un token inválido — necesarios para que el extractor detecte y reaccione al re-login |
| **Prerequisito** | Token inválido (usar uno expirado o ejecutar V-LOGIN-04 primero) |

**Request:**
```
GET {SL}/Invoices?$top=1
Cookie: B1SESSION=INVALID_SESSION_TOKEN_12345
```

| Verificación | Resultado esperado | Resultado obtenido | ✓ |
|---|---|---|---|
| HTTP status | 401 | | |
| Body contiene mensaje identificable | Sí | | |

**Registrar respuesta exacta:**

| Campo | Valor |
|---|---|
| HTTP status code | |
| Body completo (copiar) | |
| ¿Incluye código de error específico? | |
| Clave JSON del error (ej. `error.code`, `code`, `message`) | |

> Este valor se usa para configurar la detección de sesión expirada en el `SessionManager` del extractor.

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

## Sección 11 — Performance

---

### V-PERF-01 — Benchmark de throughput y latencia

| Campo | Detalle |
|---|---|
| **Objetivo** | Medir filas por segundo, latencia por tamaño de página y calcular la estimación de initial load para esta instancia |
| **Prerequisito** | `{SESSION}` válido. OINV con datos suficientes. |

**Medición 1 — Latencia por tamaño de página (OINV, `$select` mínimo):**

| Request | T (ms) |
|---|---|
| `GET /Invoices?$top=100&$orderby=DocEntry asc&$select=DocEntry,UpdateDate` | |
| `GET /Invoices?$top=500&$orderby=DocEntry asc&$select=DocEntry,UpdateDate` | |
| `GET /Invoices?$top=1000&$orderby=DocEntry asc&$select=DocEntry,UpdateDate` | |

**Medición 2 — Latencia `$select` completo vs mínimo (a `$top=500`):**

| Request | T (ms) |
|---|---|
| `GET /Invoices?$top=500&$select=DocEntry,UpdateDate` (mínimo) | |
| `GET /Invoices?$top=500&$select=DocEntry,DocNum,...,UpdateDate` (completo) | |

**Medición 3 — Throughput: 5 páginas consecutivas de 500 filas (solo `$skip`, sin filtro):**

| Página | `$skip` | T (ms) | Filas recibidas |
|---|---|---|---|
| 1 | 0 | | |
| 2 | 500 | | |
| 3 | 1000 | | |
| 4 | 1500 | | |
| 5 | 2000 | | |
| **Total** | | | **2 500** |

**Cálculos:**

| Métrica | Fórmula | Resultado |
|---|---|---|
| Tiempo total 5 páginas (s) | Suma T(ms) / 1000 | |
| Filas / segundo | 2500 / tiempo_total_s | |
| Latencia promedio por página (ms) | Suma T / 5 | |
| Overhead `$select` completo (%) | (T_completo - T_mínimo) / T_mínimo × 100 | |

**Evaluación de throughput:**

| Filas/seg | Estado | Implicación |
|---|---|---|
| ≥ 300 | ✅ Óptimo | Initial load de 100k filas < 6 min |
| 100–299 | ✅ Aceptable | Initial load de 100k filas 6–17 min |
| 50–99 | ⚠️ Lento | Initial load de 100k filas 17–33 min. Requiere ventana off-hours. |
| < 50 | 🔴 Crítico | Evaluar Mode A (HANA SQL) para este cliente |

**Filas/seg observado:** ___________________________

**Estimación de initial load:**

| Objeto | Total filas | Filas/seg medido | Tiempo estimado (min) |
|---|---|---|---|
| OINV | | | |
| OCRD | | | |
| OITM | | | |
| OSLP | | | |
| **Total** | | | |

| ¿Initial load < 8 horas en ventana off-hours? | [ ] Sí ✅  [ ] No 🔴 |
|---|---|

**Tamaño de página recomendado basado en mediciones:**

| Caso | Recomendación |
|---|---|
| T_500ms < 2 000ms | `$top=500` (default) |
| T_500ms 2 000–5 000ms | `$top=200` |
| T_500ms > 5 000ms | `$top=100` |

**Tamaño de página recomendado:** ___________________________

| Estado | Observaciones |
|---|---|
| [ ] PASS  [ ] FAIL  [ ] WARN | |

---

## Sección 12 — Resumen de Ejecución

### 12.1 Resultados por caso

| Caso | Descripción | Estado | Bloqueante |
|---|---|---|---|
| V-LOGIN-01 | Login credenciales válidas | | |
| V-LOGIN-02 | Login credenciales inválidas | | |
| V-LOGIN-03 | Login CompanyDB inválida | | |
| V-LOGIN-04 | Logout e invalidación | | |
| V-SESSION-01 | Reutilización de sesión | | |
| V-SESSION-02 | Detección de sesión expirada | | |
| V-META-01 | Metadata OData | | |
| V-META-02 | Campos OINV | | |
| V-META-03 | Campos OCRD/OITM/OSLP | | |
| V-OINV-01 | Lectura básica OINV | | |
| V-OINV-02 | Filtro UpdateDate OINV | | |
| V-OINV-03 | Orderby compuesto | | |
| V-OINV-04 | Ventana incremental 2h | | |
| V-OINV-05 | $select completo OINV | | |
| V-OCRD-01 | OCRD lectura y filtro | | |
| V-OITM-01 | OITM y tipo ItemsGroupCode | | |
| V-OSLP-01 | OSLP y filtro UpdateDate | | |
| V-UPDATETS-01 | Disponibilidad de UpdateTS | | |
| V-PAGING-01 | Continuidad páginas sin solapamiento | | |
| V-PAGE-03 | Degradación $skip a escala | | |
| V-PAGE-04 | Soporte $count | | |
| V-RETRY-01 | Burst / rate limiting | | |
| V-RETRY-04 | Respuesta sesión inválida | | |
| V-PERF-01 | Benchmark throughput | | |

**Totales:**

| Estado | Cantidad |
|---|---|
| PASS | |
| FAIL | |
| WARN | |
| SKIP | |

---

### 12.2 Decisión Go / No-Go

| Criterio Mandatory | Estado |
|---|---|
| M1 — Login retorna 200 con B1SESSION cookie | [ ] PASS  [ ] FAIL |
| M2 — Credenciales inválidas retornan 401/400 | [ ] PASS  [ ] FAIL |
| M3 — `$filter=UpdateDate ge 'YYYY-MM-DD'` funciona en OINV | [ ] PASS  [ ] FAIL |
| M4 — Paginación $top/$skip produce páginas sin solapamiento | [ ] PASS  [ ] FAIL |
| M5 — Última página detectable (rows < page_size o sin nextLink) | [ ] PASS  [ ] FAIL |
| M6 — `$select` completo de OINV retorna 200 | [ ] PASS  [ ] FAIL |
| M7 — Filas/segundo ≥ 50 | [ ] PASS  [ ] FAIL |
| M8 — Initial load estimado ≤ 8 horas | [ ] PASS  [ ] FAIL |
| M9 — Sesión inválida retorna 401 (no 200 ni 500) | [ ] PASS  [ ] FAIL |

**Decisión final:**

| Resultado | Acción |
|---|---|
| [ ] **GO** — todos los criterios M pasaron | Iniciar Sprint E1 |
| [ ] **NO-GO** — uno o más criterios M fallaron | Documentar bloqueos, ajustar arquitectura antes de implementar |

**Bloqueos identificados (si aplica):** ___________________________

---

### 12.3 Ajustes de implementación requeridos

> Basados en los hallazgos de esta sesión — actualizar `docs/sap-extractor-architecture.md` y los mappers antes de Sprint E1.

| # | Hallazgo | Ajuste requerido | Archivo afectado |
|---|---|---|---|
| 1 | Formato de `UpdateDate`: ___________ | Actualizar filter builder | `OinvExtractor.cs` |
| 2 | Nombre real de campo `SalesPersonCode`: ___________ | Actualizar field mapper | `SapFieldMapper.cs` |
| 3 | Tipo de `ItemsGroupCode`: ___________ | Conversión en mapper | `OitmExtractor.cs` |
| 4 | `UpdateTS` disponible: [ ] Sí  [ ] No | Watermark strategy | Architecture doc |
| 5 | Paginación termina por: rows < page_size / nextLink | Loop condition | `ExtractionPipeline.cs` |
| 6 | Tamaño de página recomendado: ___________ | Config default | `appsettings.json` |
| 7 | OSLP usa: [ ] incremental  [ ] full-refresh | Schedule strategy | Config + extractor |

---

### 12.4 Firma

| Rol | Nombre | Firma | Fecha |
|---|---|---|---|
| QA Integration Lead | | | |
| Tech Lead DataBision | | | |
| Contacto SAP B1 Cliente | | | |

---

*Test Execution Sheet v1.0 — DataBision SAP Extractor Mode C*  
*Ref: `docs/service-layer-validation-plan.md` · `docs/sap-extractor-architecture.md`*
