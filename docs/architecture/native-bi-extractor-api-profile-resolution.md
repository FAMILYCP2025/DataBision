# Native BI Extractor — Resolución de Perfil desde API

**Sprint:** 23B  
**Fecha:** 2026-06-21  
**Estado:** Implementado y operativo.

---

## Descripción

Cuando el extractor recibe `--profile <nombre>` o `--profile-id <id>`, carga las credenciales SAP desde el endpoint interno de DataBision en lugar de `appsettings`. Esto elimina la necesidad de guardar passwords en archivos de configuración locales.

---

## Flujo

```
Extractor startup (CLI o --service mode)
  └─ Si --profile o --profile-id:
       1. Validar DataBisionApi:BaseUrl + ApiKey
       2. GET /api/internal/native-bi/connection-profile/resolve
            ?companyId={Extractor:CompanyId}
            &profileName={profileName}   (o &profileId={id})
          Header: X-DataBision-ApiKey: {DataBisionApi:ApiKey}
       3. Si HTTP 200: extraer serviceLayerBaseUrl, companyDb, sapUserName, sapPassword
       4. Si error: detener startup (exit 2 en CLI / throw en --service)
       5. Sobreescribir SapServiceLayerOptions en memoria
       6. Si fetchConcurrency > 0: sobreescribir JournalEntryLineFetchConcurrency
  └─ Extracción normal usando las credenciales resueltas
```

---

## Clases

| Clase | Ubicación | Rol |
|---|---|---|
| `IConnectionProfileResolver` | `DataBision.Extractor.DataBision` | Contrato |
| `ApiConnectionProfileResolver` | `DataBision.Extractor.DataBision` | Implementación HTTP |

---

## Configuración mínima

```json
{
  "DataBisionApi": {
    "BaseUrl": "https://api.databision.app",
    "ApiKey": "extractor-api-key"
  },
  "Extractor": {
    "TenantId": "tenant-client",
    "CompanyId": "company-client-001"
  }
}
```

`SapServiceLayer` en appsettings ya no es necesario cuando se usa `--profile`.

---

## Comandos operativos

```bash
# CLI: extraer OACT usando perfil "produccion"
dotnet DataBision.Extractor.exe --profile produccion --object OACT --send

# CLI: extraer OJDT usando perfil por ID
dotnet DataBision.Extractor.exe --profile-id 3 --object OJDT --send

# --service mode: perfil configurado en appsettings.json
# Extractor:ProfileName = "produccion" → se resuelve al inicio del servicio
```

---

## Fallback (sin --profile)

Si no se especifica `--profile` ni `--profile-id`, el extractor sigue usando las credenciales de `appsettings.json/SapServiceLayer` como antes. Compatibilidad total con Sprint 22 y anteriores.

---

## Seguridad

| Regla | Implementación |
|---|---|
| Password nunca en disco | Resuelto en memoria, no persiste |
| Password nunca en logs | `ApiConnectionProfileResolver` no loguea `sapPassword` |
| Endpoint protegido por API Key | `X-DataBision-ApiKey` validado por `ApiKeyAuthFilter` |
| companyId validado | El endpoint verifica que el companyId coincide con el API key |
| SSL requerido en producción | `BaseUrl` debe ser `https://` en producción |
| Timeout 30s | Si la API no responde en 30s, startup falla con error claro |

---

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `DataBision API options missing` | `DataBisionApi:BaseUrl` o `ApiKey` vacío | Configurar en appsettings |
| `HTTP 401` | API Key inválido | Verificar `Ingest:ApiKeys` en la API |
| `HTTP 403: company_mismatch` | CompanyId del API key no coincide | Verificar `Extractor:CompanyId` y config de la clave |
| `HTTP 404: company_not_found` | `AnalyticsCompanyId` no configurado en la empresa | Ir a Admin → Empresa → Editar → Analytics Company ID |
| `HTTP 404: profile_not_found` | Perfil con ese nombre no existe | Verificar nombre exacto en Admin |
| `HTTP 400: profile_inactive` | Perfil desactivado | Activar el perfil en Admin |
| `HTTP 500: secret_resolution_failed` | Variable de entorno del SecretRef no configurada en la API | Configurar `env:VAR` en el servidor de la API |

---

## Diferencia CLI vs --service mode

| Modo | Resolución | Si falla |
|---|---|---|
| CLI (`--object`, `--run-once`, etc.) | `await ResolveAsync()` antes del DI | `return 2` (exit code) |
| `--service` | `.GetAwaiter().GetResult()` en `ConfigureServices` | `throw InvalidOperationException` (crash al startup) |
