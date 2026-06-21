# Native BI — Dry-Run Results TST (24C)

**Sprint:** 24C  
**Fecha:** 2026-06-21  
**Pre-requisito:** 24B completo — perfil `tst` creado y test connection exitoso

---

## Comandos a ejecutar

```powershell
# Desde la raíz del repo:

# Opción 1: con company implícito (usa Extractor:CompanyId de appsettings)
dotnet run --project src\DataBision.Extractor -- --profile tst --dry-run

# Opción 2: con company explícito
dotnet run --project src\DataBision.Extractor -- --profile tst --company company-dev-001 --dry-run
```

**Nota:** `--company` en dry-run no afecta la resolución del perfil — se usa solo para `--transform-mart`. El perfil se resuelve usando `Extractor:CompanyId` del appsettings.

---

## Salida esperada

```
[HH:mm:ss INF] DataBision Extractor starting...
[HH:mm:ss INF] Resolving SAP credentials from API: company=company-dev-001 profile=tst
[HH:mm:ss INF] Profile resolved: id=1 name=tst db=CLTSTKSDEPOR concurrency=3
[HH:mm:ss INF] SAP credentials loaded from profile. DB=CLTSTKSDEPOR
[HH:mm:ss INF] SapServiceLayer: https://161.153.200.53:50000/b1s/v1 / DB=CLTSTKSDEPOR
[HH:mm:ss INF] === DRY-RUN: configuration check ===
[HH:mm:ss INF] SapServiceLayer.BaseUrl:    https://161.153.200.53:50000/b1s/v1
[HH:mm:ss INF] SapServiceLayer.CompanyDB:  [set]
[HH:mm:ss INF] SapServiceLayer.UserName:   [set]
[HH:mm:ss INF] SapServiceLayer.Password:   [set]
[HH:mm:ss INF] DataBisionApi.BaseUrl:       http://localhost:5103
[HH:mm:ss INF] DataBisionApi.ApiKey:        [set]
[HH:mm:ss INF] Extractor.TenantId:          [set]
[HH:mm:ss INF] Extractor.CompanyId:         [set]
[HH:mm:ss INF] === DRY-RUN: configuration OK ===
```

**Claves de validación:**
- `Profile resolved: id=... name=tst db=CLTSTKSDEPOR` — resolución exitosa
- `[set]` en Password — no impreso en logs ✅
- NO `B1SESSION` en logs ✅
- NO connection string en logs ✅
- Exit code 0

---

## Resultado obtenido

```
[Pegar aquí la salida del dry-run — redactar antes de pegar: 
 reemplazar cualquier IP visible con [SL-URL], cualquier password con [REDACTED]]
```

### Verificación línea por línea

| Línea esperada | ¿Presente? | Observación |
|---|---|---|
| `Resolving SAP credentials from API` | ☐ | |
| `Profile resolved: id=... name=tst db=CLTSTKSDEPOR` | ☐ | |
| `SAP credentials loaded from profile` | ☐ | |
| `SapServiceLayer.Password: [set]` | ☐ | |
| `=== DRY-RUN: configuration OK ===` | ☐ | |
| Exit code = 0 | ☐ | |
| NO secretos en output | ☐ | |

---

## Errores conocidos y resolución

| Error | Causa | Resolución |
|---|---|---|
| `HTTP 401` al resolver | API key inválida | Verificar `DataBisionApi:ApiKey` = `dev-key-001` |
| `company_not_found` | AnalyticsCompanyId no seteado | Verificar seeder en logs de la API |
| `profile_not_found` | Nombre `tst` incorrecto | Verificar nombre exacto en Admin UI |
| `secret_resolution_failed` | ENV var ausente | `$env:SAP_PASSWORD_KSDEPOR = "..."` + restart API |
| `DataBision API ... must be configured` | BaseUrl o ApiKey vacíos | Verificar appsettings.Development.json del extractor |

---

## Decisión

☐ **Continuar a 24D** — dry-run OK, todos los checks verdes  
☐ **DETENER** — ver errores y resoluciones arriba

---

## Fecha y hora de ejecución

| Ejecución | Fecha/Hora | Exit Code | Resultado |
|---|---|---|---|
| 1 | | | |
