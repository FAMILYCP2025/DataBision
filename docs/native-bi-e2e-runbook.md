# Native BI — E2E Validation Runbook

How to execute and interpret end-to-end endpoint validation for the Native BI backend.

---

## Prerequisites

- .NET 8 SDK installed
- PowerShell 5.1+
- DataBision API running locally or accessible via URL
- Supabase/PostgreSQL MART tables populated (at least one company with data)

---

## 1. Start the API locally

Open a terminal and run:

```powershell
dotnet run --project src\DataBision.Api
# API starts on https://localhost:7xxx / http://localhost:5103 (check console output)
```

Wait for the line:
```
Application started. Press Ctrl+C to shut down.
```

Keep this terminal running. Open a second terminal for the validation script.

---

## 2. Run E2E validation (DEV mode — no JWT)

When `Jwt:PublicKey` is not configured in `appsettings.Development.json`, the API accepts `?companyId` as query param:

```powershell
cd scripts\dev
.\test-native-bi-endpoints.ps1 -BaseUrl http://localhost:5103 -CompanyId company-dev-001
```

Expected output: all endpoints show `[PASS]`.

---

## 3. Run E2E validation (Production mode — JWT Bearer)

When JWT is configured, endpoints require a valid `Authorization: Bearer <token>` header. Obtain a token from login, then:

```powershell
$token = "eyJ..."   # paste token — NOT printed to console

.\test-native-bi-endpoints.ps1 `
    -BaseUrl http://localhost:5103 `
    -CompanyId company-dev-001 `
    -BearerToken $token
```

The company is resolved from the JWT `company_slug` claim — `CompanyId` param is used only for display.

---

## 4. Save results to file

```powershell
$date = Get-Date -Format "yyyyMMdd-HHmm"
.\test-native-bi-endpoints.ps1 `
    -CompanyId company-dev-001 `
    -OutputPath "..\..\docs\e2e-results\native-bi-e2e-$date.txt"
```

Results are saved to `docs/e2e-results/` (folder created automatically).  
Files in that folder are gitignored — commit only representative results manually.

---

## 5. Interpret output

### [PASS] lines

```
  [PASS] GET /dashboard/top-customers  HTTP 200  meta(limit=10,offset=0,count=3,hasMore=false)
```

For paginated endpoints, `meta` fields are validated explicitly. If any of `limit`, `offset`, `count`, or `hasMore` is missing from the response, the test fails.

### [FAIL] lines

```
  [FAIL] GET /dashboard/summary  HTTP 400  missing_company_id
```

Common causes:
- API not running (connection refused)
- MART tables empty (data is null, but that is a [PASS])
- `StagingConnection` not configured → 503 from DI
- Wrong `CompanyId` slug

### Error validation section (DEV mode only)

Section 6 tests that invalid parameters return the correct 4xx status. These are expected failures that should all show `[PASS]`.

---

## 6. Acceptance criteria

All checkboxes must be green before go-live:

- [ ] All 17 endpoints return HTTP 200
- [ ] Paginated endpoints (`top-customers`, `top-items`, `salespersons`, `sales/customers`, `sales/items`, `sales/salespersons`) return valid `meta` object
- [ ] `meta.hasMore = true` when data exceeds page size
- [ ] Error validation: 8 bad-param cases all return 400
- [ ] Script exits with code 0

---

## 7. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| All endpoints → connection refused | API not started | `dotnet run --project src\DataBision.Api` |
| All endpoints → 503 | `StagingConnection` not configured | Add `ConnectionStrings:StagingConnection` to dev settings |
| Endpoints → 200 but `data = null` | MART tables empty | Run `--transform --include-mart --company <id>` |
| Diagnostics → `staging_connection = error` | Supabase unreachable | Check Supabase project status, VPN |
| JWT mode → 401 on all | Token expired or invalid | Refresh token via `POST /api/auth/refresh` |
| JWT mode → 403 `forbidden_no_company` | Token has no `company_slug` claim | Log in again from correct tenant subdomain |
| Paginated → `[FAIL] meta missing` | Response format changed | Check `OkPaged` extension — `meta` must be top-level |

---

## 8. CI integration (future)

When a CI pipeline is in place, add this step:

```yaml
- name: E2E Native BI validation
  run: |
    pwsh scripts/dev/test-native-bi-endpoints.ps1 `
      -BaseUrl ${{ env.API_URL }} `
      -CompanyId ${{ secrets.TEST_COMPANY_ID }} `
      -BearerToken ${{ secrets.TEST_BEARER_TOKEN }} `
      -OutputPath docs/e2e-results/ci-run-${{ github.run_number }}.txt
```

Exit code 1 will fail the CI step automatically.
