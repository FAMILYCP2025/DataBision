# DataBision — Developer Setup

Local dev guide for the DataBision platform.

## Prerequisites

- .NET 8 SDK (`dotnet --version` ≥ 8.0)
- Node.js 20+ and npm (`node --version`, `npm --version`)
- `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef --version 8.0.4`
- ASP.NET Core dev certificate trusted:
  `dotnet dev-certs https --trust`

## URLs

| Service  | URL                                |
|----------|------------------------------------|
| Backend  | `https://localhost:5001` (HTTPS, dev cert) |
|          | `http://localhost:5000` (redirects to HTTPS) |
| Frontend | `http://localhost:5173`            |
| Swagger  | `https://localhost:5001/swagger`   |
| Health   | `https://localhost:5001/api/health` |

In local dev, subdomains are simulated via the `?tenant=<slug>` query string
(`TenantMiddleware` reads it when `Host` is `localhost`).

## Starting the backend

```powershell
dotnet run --project src/DataBision.Api
```

On startup the API:
1. Applies pending EF migrations against `databision_dev.db` (SQLite).
2. Runs `DatabaseSeeder` (idempotent — only seeds missing rows).
3. Listens on both `http://localhost:5000` and `https://localhost:5001`.

Validate it is alive:
```powershell
# from PowerShell
Invoke-RestMethod https://localhost:5001/api/health
# from the frontend (via vite proxy)
Invoke-RestMethod http://localhost:5173/api/health
```
Both should return `{ status = ok, timestamp = ... }`.

## Starting the frontend

```powershell
cd databision-frontend
npm install   # first run only, or after pulling new deps
npm run dev
```

Vite proxies `/api/*` to `https://localhost:5001` (`secure: false` accepts the
local dev cert). See `databision-frontend/vite.config.ts`.

## Seed credentials

`DatabaseSeeder` creates these accounts on first run:

### SuperAdmin (admin portal)

| Field    | Value                       |
|----------|-----------------------------|
| URL      | `http://localhost:5173/admin/login` |
| Email    | `admin@databision.app`      |
| Password | `Admin@DataBision2026!`     |

### Demo company (client portal)

URL pattern: `http://localhost:5173/client/login?tenant=demo`

| Role         | Email             | Password           |
|--------------|-------------------|--------------------|
| CompanyAdmin | `admin@demo.com`  | `Demo@Admin2026!`  |
| Viewer       | `viewer@demo.com` | `Demo@Viewer2026!` |

The Viewer is granted access to two specific reports (Resumen de Ventas,
Cuentas por Cobrar). CompanyAdmin bypasses report-level checks by role.

## Common dev commands

```powershell
# Backend
dotnet build
dotnet test
dotnet ef migrations add <Name> --project src/DataBision.Infrastructure --startup-project src/DataBision.Api
dotnet ef database update      --project src/DataBision.Infrastructure --startup-project src/DataBision.Api

# Frontend (run from databision-frontend/)
npm run dev
npm run build
npm run lint
npx tsc --noEmit          # type-check only
```

## Troubleshooting

### `ERR_NETWORK` in the browser on login
- Confirm the backend is up: `Invoke-RestMethod https://localhost:5001/api/health`.
- Confirm the dev cert is trusted: `dotnet dev-certs https --check --trust`.
- The vite proxy must target `https://localhost:5001` (not 5000) because
  `Program.cs` enables `UseHttpsRedirection()`. A target of `http://localhost:5000`
  returns a 301 the proxy will not follow, and the browser then tries the
  HTTPS URL directly with the untrusted cert.

### Build fails with `MSB3027` / file locked
A running `DataBision.Api` process is holding the DLLs. Stop it:
```powershell
Get-Process DataBision.Api -ErrorAction SilentlyContinue | Stop-Process -Force
```

### Reset the local database
```powershell
Remove-Item src/DataBision.Api/databision_dev.db
dotnet run --project src/DataBision.Api   # migrations + seeder rebuild it
```
