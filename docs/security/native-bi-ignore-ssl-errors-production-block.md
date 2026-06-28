# Native BI — Bloqueo de IgnoreSslErrors en Production

**DataBision · Junio 2026**  
**Versión:** 1.0 — Implementado como Gate 1 pre-deployment Modalidad A  
**Estado:** IMPLEMENTADO — bloqueo programático activo en tres puntos del stack

---

## 1. Regla de bloqueo

**Regla:** Si un perfil de conexión SAP tiene `IgnoreSslErrors=true` Y el proceso corre con `ASPNETCORE_ENVIRONMENT=Production`, la operación es rechazada antes de realizar cualquier conexión SAP.

**Aplica a:**

| Operación | Componente | Acción si bloqueado |
|---|---|---|
| Resolución de credenciales del extractor | `ApiConnectionProfileResolver.ResolveAsync` | Retorna `null` — extractor aborta con exit code 2 |
| Entrega de credenciales por la API | `NativeBiConnectionProfileService.ResolveForExtractorAsync` | Retorna `(null, "ignore_ssl_blocked_in_production")` |
| Test de conexión SAP desde el panel admin | `NativeBiSapConnectionTester.TestAsync` | Retorna `Success=false` sin conectarse a SAP |

---

## 2. Comportamiento esperado por ambiente

### DEV / TST (permitido con restricción)

```
ASPNETCORE_ENVIRONMENT = Development | TST | DEV | Staging
IgnoreSslErrors = true en el perfil
```

Resultado: **PERMITIDO** — el extractor y el test de conexión continúan normalmente.

Condición obligatoria: esto solo debe usarse en entornos controlados donde el certificado SSL del SAP Service Layer es autofirmado o no tiene cadena de CA válida. Nunca en el servidor del cliente en producción.

### Production (bloqueado)

```
ASPNETCORE_ENVIRONMENT = Production
IgnoreSslErrors = true en el perfil
```

Resultado: **RECHAZADO** — log de error emitido, no se retornan credenciales SAP, no se realiza ninguna conexión.

Log emitido (extractor):
```
SECURITY BLOCK: Profile 'ksdepor-prd' (id=3) has IgnoreSslErrors=true
but ASPNETCORE_ENVIRONMENT=Production. Credential load refused.
Disable IgnoreSslErrors on the profile or run in a DEV/TST environment.
```

Log emitido (API — endpoint de resolución):
```
SECURITY BLOCK: Profile 3 for company '1' has IgnoreSslErrors=true
but ASPNETCORE_ENVIRONMENT=Production. Credential resolution refused.
```

Log emitido (API — test de conexión):
```
SECURITY BLOCK: Profile 3 has IgnoreSslErrors=true but ASPNETCORE_ENVIRONMENT=Production.
TestConnection refused.
```

### Sin variable de entorno configurada

Si `ASPNETCORE_ENVIRONMENT` no está configurada, el sistema asume `Production` por defecto y **bloquea** cualquier perfil con `IgnoreSslErrors=true`.

---

## 3. Archivos modificados

| Archivo | Cambio |
|---|---|
| [src/DataBision.Extractor/DataBision/ApiConnectionProfileResolver.cs](../../src/DataBision.Extractor/DataBision/ApiConnectionProfileResolver.cs) | Agrega `IsIgnoreSslBlockedInProduction()` + bloqueo en `ResolveAsync` |
| [src/DataBision.Infrastructure/Repositories/NativeBiSapConnectionTester.cs](../../src/DataBision.Infrastructure/Repositories/NativeBiSapConnectionTester.cs) | Agrega bloqueo previo a la construcción del `HttpClientHandler` |
| [src/DataBision.Infrastructure/Repositories/NativeBiConnectionProfileService.cs](../../src/DataBision.Infrastructure/Repositories/NativeBiConnectionProfileService.cs) | Agrega bloqueo en `ResolveForExtractorAsync` antes de resolver el SecretRef |

---

## 4. Tests agregados

| Archivo de test | Cobertura |
|---|---|
| [tests/DataBision.Extractor.Tests/Security/ProductionSslBlockTests.cs](../../tests/DataBision.Extractor.Tests/Security/ProductionSslBlockTests.cs) | `IsIgnoreSslBlockedInProduction`: 5 casos — Production block, non-Production pass-through, false flag pass-through, missing env var |
| [tests/DataBision.Api.Tests/Security/NativeBiSapConnectionTesterProductionBlockTests.cs](../../tests/DataBision.Api.Tests/Security/NativeBiSapConnectionTesterProductionBlockTests.cs) | `NativeBiSapConnectionTester.TestAsync`: 3 casos — Production block retorna failure, TST pasa, false flag pasa |

---

## 5. Pruebas manuales de verificación

### Verificar bloqueo en Production

```bash
# En el servidor cliente, configurar:
ASPNETCORE_ENVIRONMENT=Production

# Intentar extracción con perfil que tiene IgnoreSslErrors=true
dotnet DataBision.Extractor.dll --profile ksdepor-prd --run-once

# Resultado esperado: exit code 2, log "SECURITY BLOCK", sin conexión a SAP
```

### Verificar que TST funciona

```bash
# En servidor de test:
ASPNETCORE_ENVIRONMENT=TST

dotnet DataBision.Extractor.dll --profile ksdepor-tst --dry-run

# Resultado esperado: perfil resuelto correctamente, "SAP credentials loaded from profile"
```

### Verificar bloqueo en test de conexión (Panel Admin)

```http
POST /api/admin/companies/{companyId}/native-bi/connection-profiles/{profileId}/test
```

- Con `ASPNETCORE_ENVIRONMENT=Production` y perfil `IgnoreSslErrors=true`:
  - Respuesta: `{ "data": { "success": false, "message": "Conexión rechazada: IgnoreSslErrors=true no está permitido en entornos Production..." } }`
  - Sin hit al SAP Service Layer

---

## 6. Criterios GO / NO-GO

| Criterio | Estado |
|---|---|
| En Production, perfil con `IgnoreSslErrors=true` no puede conectarse a SAP | ✅ Implementado |
| En TST/DEV, puede conectarse si `IgnoreSslErrors=true` y está documentado como excepción | ✅ Implementado |
| El error no expone credenciales SAP ni sesión | ✅ Los logs solo muestran `profile.Id` y `company.Id`, nunca passwords |
| El comportamiento por defecto (sin env var) es bloqueante | ✅ Implementado — `?? "Production"` |
| Tests automatizados cubren los casos principales | ✅ 8 tests en 2 archivos |

**Estado Gate 1: GO ✅**

---

## 7. Notas operativas

- El flag `IgnoreSslErrors=true` en la base de datos **no se elimina** al activar este bloqueo. Es un bloqueo en tiempo de ejecución, no en tiempo de persistencia.
- Si un cliente PRD necesita `IgnoreSslErrors=true` (certificado autofirmado), el proceso correcto es:
  1. Obtener un certificado SSL válido para el SAP Service Layer del cliente
  2. Configurarlo en SAP B1 Administration → Service Layer → SSL
  3. Cambiar `IgnoreSslErrors=false` en el perfil de conexión
  4. Re-probar la conexión desde el panel Admin
- No existe una excepción "de emergencia" para usar `IgnoreSslErrors=true` en Production.
