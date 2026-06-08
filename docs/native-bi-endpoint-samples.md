# Native BI — Endpoint Samples

Ejemplos ejecutables para todos los grupos de endpoints. Requiere API corriendo en `http://localhost:5103`.

---

## DEV mode (sin JWT — usa ?companyId)

```powershell
$base    = "http://localhost:5103"
$company = "company-dev-001"
$q       = "?companyId=$company"
```

---

## 1. Dashboard

### Summary

```powershell
Invoke-RestMethod "$base/api/client/dashboard/summary$q" | ConvertTo-Json -Depth 3
```

```bash
curl -s "http://localhost:5103/api/client/dashboard/summary?companyId=company-dev-001" | jq .
```

Respuesta:
```json
{
  "data": {
    "companyId": "company-dev-001",
    "grossSalesAmount": 1450000.0,
    "creditMemoAmount": 85000.0,
    "netSalesAmount": 1365000.0,
    "invoiceCount": 72,
    "activeCustomers": 20,
    "transformedAtUtc": "2026-06-07T00:00:00Z"
  },
  "traceId": "0HMWPKFQDAV2H:00000003"
}
```

---

### Sales daily (últimos 30 días)

```powershell
Invoke-RestMethod "$base/api/client/dashboard/sales-daily${q}&days=30"
```

```bash
curl -s "http://localhost:5103/api/client/dashboard/sales-daily?companyId=company-dev-001&days=30" | jq '.data | length'
```

---

### Sales monthly (últimos 12 meses)

```powershell
Invoke-RestMethod "$base/api/client/dashboard/sales-monthly${q}&months=12"
```

---

### Top customers (paginado)

```powershell
# Primera página
Invoke-RestMethod "$base/api/client/dashboard/top-customers${q}&limit=10&offset=0&sortBy=netSalesAmount&sortDir=desc" | ConvertTo-Json -Depth 4

# Segunda página
Invoke-RestMethod "$base/api/client/dashboard/top-customers${q}&limit=10&offset=10"
```

Respuesta:
```json
{
  "data": [
    {
      "cardCode": "C001",
      "cardName": "Cliente SA",
      "netSalesAmount": 480000.0,
      "invoiceCount": 12
    }
  ],
  "meta": {
    "limit": 10,
    "offset": 0,
    "count": 10,
    "hasMore": true
  },
  "traceId": "..."
}
```

---

### Top items (ordenado por cantidad vendida)

```powershell
Invoke-RestMethod "$base/api/client/dashboard/top-items${q}&limit=10&sortBy=quantitySold&sortDir=desc"
```

---

### Salespersons

```powershell
Invoke-RestMethod "$base/api/client/dashboard/salespersons${q}&limit=20&sortBy=netSalesAmount&sortDir=desc"
```

---

## 2. Sales

### Overview (rango de fechas)

```powershell
Invoke-RestMethod "$base/api/client/sales/overview${q}&dateFrom=2026-01-01&dateTo=2026-06-30"
```

```bash
curl -s "http://localhost:5103/api/client/sales/overview?companyId=company-dev-001&dateFrom=2026-01-01&dateTo=2026-06-30" | jq .data
```

---

### Daily con rango específico

```powershell
Invoke-RestMethod "$base/api/client/sales/daily${q}&dateFrom=2026-05-01&dateTo=2026-05-31"
```

---

### Monthly con rango

```powershell
Invoke-RestMethod "$base/api/client/sales/monthly${q}&dateFrom=2026-01-01&dateTo=2026-12-31"
```

---

### Customers (paginado, con sort)

```powershell
# Default: limit=50, sortBy=netSalesAmount desc
Invoke-RestMethod "$base/api/client/sales/customers${q}&limit=20"

# Ordenado por cardCode
Invoke-RestMethod "$base/api/client/sales/customers${q}&limit=20&sortBy=cardCode&sortDir=asc"
```

---

### Items (paginado)

```powershell
Invoke-RestMethod "$base/api/client/sales/items${q}&limit=20&sortBy=grossSalesAmount&sortDir=desc"
```

---

### Salespersons (paginado)

```powershell
Invoke-RestMethod "$base/api/client/sales/salespersons${q}&limit=20"
```

---

## 3. Sync

### Status general

```powershell
Invoke-RestMethod "$base/api/client/sync/status$q" | ConvertTo-Json -Depth 5
```

Respuesta:
```json
{
  "data": {
    "companyId": "company-dev-001",
    "overallStatus": "ok",
    "lastSyncAtUtc": "2026-06-07T00:10:00Z",
    "lastTransformAtUtc": "2026-06-07T00:14:00Z",
    "objects": [
      { "sapObject": "OINV", "watermarkDate": "2026-06-05", "totalRowsIngested": 8430, "status": "ok" }
    ]
  },
  "traceId": "..."
}
```

---

### Objects (por SAP object)

```powershell
Invoke-RestMethod "$base/api/client/sync/objects$q" | ConvertTo-Json -Depth 3
```

---

### Transform status

```powershell
Invoke-RestMethod "$base/api/client/sync/transform-status$q" | ConvertTo-Json -Depth 4
```

---

## 4. Diagnostics

### Health check completo

```powershell
Invoke-RestMethod "$base/api/client/diagnostics/native-bi$q" | ConvertTo-Json -Depth 5
```

Respuesta esperada cuando todo está OK:
```json
{
  "data": {
    "companyId": "company-dev-001",
    "status": "ok",
    "checks": [
      { "name": "staging_connection", "status": "ok", "detail": "Supabase reachable." },
      { "name": "mart_data_freshness", "status": "ok", "detail": "Last transform: 2026-06-07T00:00:00Z (14m ago)." },
      { "name": "mart_tables_populated", "status": "ok", "detail": "6 MART tables checked. All have rows." },
      { "name": "checkpoints_exist", "status": "ok", "detail": "Ingest checkpoints found." },
      { "name": "last_extraction_run", "status": "ok", "detail": "Last extraction run: 2026-06-07T00:10:00Z (18m ago)." }
    ],
    "generatedAtUtc": "2026-06-07T00:28:00Z"
  },
  "traceId": "..."
}
```

---

### Table counts

```powershell
Invoke-RestMethod "$base/api/client/diagnostics/native-bi/tables$q" | ConvertTo-Json -Depth 4
```

---

## 5. Modo PROD (con Bearer token)

```powershell
# Obtener token (login)
$loginBody = @{ email = "user@tenant.com"; password = "..." } | ConvertTo-Json
$loginResp  = Invoke-RestMethod -Method POST -Uri "$base/api/auth/login" `
              -Body $loginBody -ContentType "application/json"
$token = $loginResp.data.accessToken   # NO imprimir

# Usar token en requests (companyId se ignora — viene del JWT)
$headers = @{ Authorization = "Bearer $token" }

Invoke-RestMethod "$base/api/client/dashboard/summary" -Headers $headers
Invoke-RestMethod "$base/api/client/dashboard/top-customers?limit=10" -Headers $headers
```

En producción, los query params `limit`, `offset`, `sortBy`, `sortDir`, `days`, `months`, `dateFrom`, `dateTo` siguen funcionando igual. Solo `companyId` es ignorado.

---

## 6. Ejemplos de errores esperados

### Sin companyId en DEV → 400

```powershell
try {
    Invoke-RestMethod "$base/api/client/dashboard/summary"
} catch {
    $_.Exception.Response.StatusCode  # 400
    ($_.ErrorDetails.Message | ConvertFrom-Json).error  # "missing_company_id"
}
```

### Invalid limit → 400

```powershell
try {
    Invoke-RestMethod "$base/api/client/dashboard/top-customers${q}&limit=0"
} catch {
    ($_.ErrorDetails.Message | ConvertFrom-Json).error  # "invalid_limit"
}
```

### Invalid sortBy → 400

```powershell
try {
    Invoke-RestMethod "$base/api/client/dashboard/top-customers${q}&sortBy=unknownField"
} catch {
    ($_.ErrorDetails.Message | ConvertFrom-Json)
    # { error: "invalid_sort_by", message: "sortBy must be one of: ...", traceId: "..." }
}
```

### Invalid sortDir → 400

```powershell
try {
    Invoke-RestMethod "$base/api/client/dashboard/top-customers${q}&sortDir=RANDOM"
} catch {
    ($_.ErrorDetails.Message | ConvertFrom-Json).error  # "invalid_sort_dir"
}
```

### JWT configurado + sin token → 401

```powershell
try {
    # (en environment con Jwt:PublicKey configurado)
    Invoke-RestMethod "$base/api/client/dashboard/summary"
} catch {
    $_.Exception.Response.StatusCode  # 401
    ($_.ErrorDetails.Message | ConvertFrom-Json).error  # "unauthorized"
}
```

---

## 7. Script E2E completo

Ejecutar validación de todos los endpoints:

```powershell
# DEV mode
.\scripts\dev\test-native-bi-endpoints.ps1 `
    -BaseUrl http://localhost:5103 `
    -CompanyId company-dev-001

# Con output a archivo
.\scripts\dev\test-native-bi-endpoints.ps1 `
    -CompanyId company-dev-001 `
    -OutputPath "docs\e2e-results\native-bi-e2e-$(Get-Date -Format 'yyyyMMdd-HHmm').txt"

# Verificar exit code
$LASTEXITCODE  # 0 = todos PASS, 1 = algún FAIL
```
