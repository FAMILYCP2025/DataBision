# Native BI — Seguridad del Endpoint Test Connection

**Sprint:** 22B  
**Fecha:** 2026-06-21

---

## Endpoint

```
POST /api/admin/companies/{companyId}/native-bi/connection-profiles/{profileId}/test
```

Requiere: `[Authorize(Roles = "SuperAdmin")]`

---

## Flujo de ejecución

```
Controller
  → INativeBiConnectionProfileService.TestAsync(companyId, profileId)
    → Carga NativeBiConnectionProfile desde AppDB
    → INativeBiSapConnectionTester.TestAsync(profile)
      → SecretRefResolver.Resolve(profile.SecretRef)  ← password en memoria
      → POST Login SAP SL                              ← password enviado por HTTPS
      → GET ChartOfAccounts?$top=1                     ← B1SESSION en cookie
      → GET JournalEntries?$top=1                      ← B1SESSION en cookie
      → POST Logout
      ← TestNativeBiConnectionProfileResult (sin password, sin cookie)
```

---

## Reglas de seguridad implementadas

| Regla | Implementación |
|---|---|
| Password nunca persiste | Solo en memoria durante el test, no asignado a variable con nombre sensible |
| B1SESSION nunca se retorna | Solo existe en `sessionCookie` local, no en DTO |
| B1SESSION nunca se loguea | Los logs solo registran profile.Id, latencia, y resultado bool |
| URL se enmascara | `ServiceLayerBaseUrlMasked` = `scheme://host:port` (sin path ni query) |
| Certificado SSL controlado | Solo bypasa si `IgnoreSslErrors = true` en el perfil |
| SecretRef solo `env:` por ahora | `SecretRefResolver` lanza `NotSupportedException` para esquemas no implementados |
| Error SAP sanitizado | El mensaje de error no incluye el response body completo de SAP |

---

## Response shape

```json
{
  "data": {
    "success": true,
    "latencyMs": 234,
    "checkedAt": "2026-06-21T10:00:00Z",
    "serviceLayerBaseUrlMasked": "https://sap-host:50000",
    "companyDb": "CLIENT_DB",
    "message": "Conexión exitosa en 234ms. JournalEntries: OK.",
    "capabilities": {
      "loginOk": true,
      "chartOfAccountsOk": true,
      "journalEntriesOk": true
    }
  }
}
```

---

## Casos de error

| Situación | `success` | `message` |
|---|---|---|
| `env:VAR` no configurada | false | `SecretRef error: Environment variable 'VAR' not set.` |
| Login SAP HTTP 401 | false | `Login failed (HTTP 401). Check CompanyDB, user, and password.` |
| Timeout de conexión | false | `Login timed out after 60s.` |
| Login OK pero sin B1SESSION | false | `Login succeeded (HTTP 200) but no B1SESSION found.` |
| Login OK, ChartOfAccounts falla | false | `Conexión establecida pero ChartOfAccounts falló. Verificar permisos.` |
| Todo OK, JournalEntries no disponible | true | `Conexión exitosa en Xms. JournalEntries: no disponible.` |
| Perfil no encontrado | false | `Profile not found.` |

---

## Decisión de diseño: NativeBiSapConnectionTester en Infrastructure

El tester usa `HttpClient` directamente (no reutiliza `ServiceLayerClient` del extractor) para:

1. Evitar dependencia entre `DataBision.Api` y `DataBision.Extractor`
2. El tester es un smoke-test mínimo (login + GET x2 + logout), no una extracción completa
3. `ServiceLayerClient` del extractor es un proceso de larga vida con sesión persistente; el tester es efímero

**Duplicación aceptada:** ~50 líneas de código HTTP vs. dependencia de proyecto incorrecta.

---

## Deploy checklist (producción)

- [ ] Variable de entorno `DATABISION_SAP_PASSWORD_{SLUG}` configurada en servidor de la API
- [ ] Servidor de la API tiene acceso de red a SAP Service Layer del cliente
- [ ] `IgnoreSslErrors=false` como default; solo `true` con aprobación escrita del cliente
- [ ] Logs no retienen B1SESSION (validado en revisión de Serilog sinks)
- [ ] Endpoint solo accesible para SuperAdmin (JWT con `role=SuperAdmin`)
