# Native BI — Connection Profile TST (24B)

**Sprint:** 24B  
**Fecha:** 2026-06-21  
**Pre-requisito:** docs/pilots/native-bi-pilot-tst-setup.md (24A completado)

---

## Pre-requisito: variable de entorno

Antes de crear el perfil, configurar en el servidor de la API:

```powershell
$env:SAP_PASSWORD_KSDEPOR = "<password dgoto CLTSTKSDEPOR>"
```

Verificar (sin imprimir valor):
```powershell
if ($env:SAP_PASSWORD_KSDEPOR) { "ENV var: PRESENTE" } else { "ENV var: AUSENTE — configurar antes de continuar" }
```

---

## Opción A: Crear perfil desde Admin UI

1. Ir a `http://localhost:5103` (o admin.databision.app)
2. Login como SuperAdmin
3. Ir a **Empresas → ksdepor → Native BI — Configuración avanzada**
4. Click **+ Nuevo perfil**
5. Completar:

| Campo | Valor |
|---|---|
| Nombre | `tst` |
| Entorno | `TST` |
| SL Base URL | `https://161.153.200.53:50000/b1s/v1` |
| CompanyDB | `CLTSTKSDEPOR` |
| Usuario SAP | `dgoto` |
| SecretRef | `env:SAP_PASSWORD_KSDEPOR` |
| IgnoreSslErrors | ✅ (marcado) |
| TimeoutSeconds | `60` |
| FetchConcurrency | `3` |
| Activo | ✅ |

6. Click **Crear perfil**
7. En la fila del perfil creado, click **Test**

---

## Opción B: Crear perfil via API (curl)

Primero obtener token SuperAdmin:
```bash
# NO imprimir el token completo
TOKEN=$(curl -s -X POST http://localhost:5103/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@databision.com","password":"<admin-password>"}' \
  | jq -r '.data.accessToken')
echo "Token presente: $([ -n "$TOKEN" ] && echo 'SÍ' || echo 'NO')"
```

Obtener company ID:
```bash
COMPANY_ID=$(curl -s http://localhost:5103/api/admin/companies \
  -H "Authorization: Bearer $TOKEN" \
  | jq '.data[] | select(.slug=="ksdepor") | .id')
echo "Company ID: $COMPANY_ID"
```

Crear perfil:
```bash
curl -s -X POST "http://localhost:5103/api/admin/companies/${COMPANY_ID}/native-bi/connection-profiles" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "profileName": "tst",
    "environmentName": "TST",
    "serviceLayerBaseUrl": "https://161.153.200.53:50000/b1s/v1",
    "companyDb": "CLTSTKSDEPOR",
    "sapUserName": "dgoto",
    "secretRef": "env:SAP_PASSWORD_KSDEPOR",
    "ignoreSslErrors": true,
    "timeoutSeconds": 60,
    "fetchConcurrency": 3,
    "isActive": true
  }' | jq '{profileId: .data.id, profileName: .data.profileName, secretRefHint: .data.secretRefHint}'
```

**Salida esperada (sin password):**
```json
{
  "profileId": 1,
  "profileName": "tst",
  "secretRefHint": "env:SAP_PASSWORD_KSDEPOR → [set]"
}
```

---

## Test de conexión

### Via Admin UI
Click **Test** en la fila del perfil. Resultado esperado:
```
✓ Conexión exitosa en Xms. JournalEntries: OK.
loginOk: true | chartOfAccountsOk: true | journalEntriesOk: true
```

### Via API (curl)
```bash
PROFILE_ID=1  # Reemplazar con el ID retornado al crear el perfil

curl -s -X POST "http://localhost:5103/api/admin/companies/${COMPANY_ID}/native-bi/connection-profiles/${PROFILE_ID}/test" \
  -H "Authorization: Bearer $TOKEN" \
  | jq '{
      success: .data.success,
      latencyMs: .data.latencyMs,
      loginOk: .data.capabilities.loginOk,
      chartOfAccountsOk: .data.capabilities.chartOfAccountsOk,
      journalEntriesOk: .data.capabilities.journalEntriesOk,
      message: .data.message
    }'
```

**Regla:** Si `success = false` → **DETENER.** No avanzar a extracción.

---

## Resultado del test connection

| Campo | Valor obtenido | Estado |
|---|---|---|
| success | ___ | ☐ OK / ☐ FAIL |
| latencyMs | ___ | ☐ < 2000ms OK |
| loginOk | ___ | ☐ OK |
| chartOfAccountsOk | ___ | ☐ OK |
| journalEntriesOk | ___ | ☐ OK / ☐ N/A |
| message | ___ | |

**Decisión:** ☐ Continuar a 24C  /  ☐ DETENER — ver errores arriba

---

## Troubleshooting

| Síntoma | Causa | Acción |
|---|---|---|
| `secret_resolution_failed` | ENV var no configurada o API no relanzada | `$env:SAP_PASSWORD_KSDEPOR = "..."` + restart API |
| `HTTP 401` en login SAP | Password incorrecto | Verificar password en SAP Client |
| `HTTP 404` en perfil | companyId erróneo en URL | Verificar COMPANY_ID |
| Login OK, ChartOfAccounts falla | Usuario sin permisos en CLTSTKSDEPOR | Verificar permisos de `dgoto` |
| `journalEntriesOk: false` | JournalEntries bloqueado | No es bloqueante — documentar y continuar |
