# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DataBision is a B2B SaaS reporting portal for SAP Business One customers. Companies access Power BI dashboards embedded at `{slug}.databision.app` with white-label branding per tenant. The SuperAdmin panel lives at `admin.databision.app`.

## Commands

**Backend (.NET 8):**
- `dotnet run --project src/DataBision.Api` — start API
- `dotnet test` — run all tests
- `dotnet test --filter "FullyQualifiedName~ClassName.MethodName"` — run a single test
- `dotnet ef migrations add <Name> --project src/DataBision.Infrastructure --startup-project src/DataBision.Api`
- `dotnet ef database update --project src/DataBision.Infrastructure --startup-project src/DataBision.Api`

**Frontend (React/TypeScript/Vite — from `databision-frontend/`):**
- `npm run dev` — dev server
- `npm run build` — production build
- `npm run lint` — lint
- `npm test -- --testNamePattern="TestName"` — run a single test (Vitest)

**Local tenant simulation:** use `?tenant=slug` query param (subdomains not available locally).

## Architecture

### Multi-Tenancy (Critical)

`TenantMiddleware` reads the `Host` header on every backend request and resolves `company_id`. All protected endpoints validate that the JWT's `company_id` matches the subdomain's tenant. In production, the subdomain is the sole source of tenant identity — never a query param.

### Auth Flow

Login returns a JWT access token (RS256, 15 min) + httpOnly refresh token cookie (7 days). JWT claims: `sub`, `email`, `role`, `company_id`, `company_slug`, `module_ids[]`. The Axios interceptor auto-refreshes on 401. Refresh tokens are stored **hashed** in the DB and rotated on use.

### Power BI Embedded

Backend generates embed tokens at `POST /api/reports/{id}/embed-token` using a Service Principal (never a Master User). Every embed token includes RLS identity: `username = company.slug`, `role = "CompanyRole"`. The frontend uses `powerbi-client-react` with the token from the backend and auto-refreshes 5 min before expiry.

### Dynamic Theming

`GET /api/tenant/config` is public (no auth) and returns branding for the current subdomain. `ThemeProvider` applies CSS custom properties to `:root`. Tailwind is configured with `brand-*` color tokens that map to `var(--brand-primary)`, etc. Tenant config is cached in `localStorage` to prevent color flash on load.

### Backend Layer Structure

```
DataBision.Domain          → entities and enums only; no external deps
DataBision.Application     → services and interfaces; business logic only (no EF, no HTTP)
DataBision.Infrastructure  → EF Core, Power BI SDK, Azure Blob; implements Application interfaces
DataBision.Api             → Controllers, Middleware, Program.cs; orchestrates layers
```

### Frontend Structure

```
src/apps/admin/    → admin.databision.app (SuperAdmin)
src/apps/portal/   → {slug}.databision.app (Company users)
App.tsx            → detects subdomain and renders the correct app
```

State: Zustand for auth and tenant. Data fetching: TanStack Query.

## Code Rules

1. **Tenant isolation:** Every query to data tables must include an explicit `company_id` filter — never rely on upstream filtering.
2. **DTOs at the boundary:** Never expose domain entities directly from controllers. Always map to a DTO.
3. **No raw SQL interpolation:** Use EF Core. If you need `FromSqlRaw`, use `SqlParameter[]` — never string interpolation.
4. **Brand colors via CSS vars:** Never hardcode brand colors in components. Use `var(--brand-primary)` or `brand-*` Tailwind classes.
5. **Audit every write:** All create/update/delete actions and `VIEW_REPORT` events must go through `AuditService`.
6. **Consistent API shape:** Success → `{ "data": T }`. Error → `{ "error": "snake_case_code", "message": "Human readable" }` with correct HTTP status.
7. **TypeScript strict mode:** No `any`. Explicit types everywhere.
8. **SuperAdmin has no company:** `company_id` is null in SuperAdmin JWTs. `/api/admin/*` endpoints do not require a company subdomain.
9. **Migrations are immutable:** Never edit an existing migration retroactively — create a new corrective migration instead.

## Design System

**Platform colors (fixed):**
- Sidebar: `#0F172A` | Active item: `#1E293B` | Background: `#F8FAFC`
- Surface: `#FFFFFF` | Border: `#E2E8F0` | Text: `#0F172A` | Muted: `#64748B`
- Success: `#16A34A` | Error: `#DC2626` | Warning: `#D97706`

**Brand colors (per-tenant overrides):**
- `--brand-primary`: default `#2563EB`
- `--brand-secondary`: default `#64748B`
- `--brand-sidebar`: default `#0F172A`

**Typography:** Inter (Google Fonts). Body: 14px/400. Labels: 13px/500. Headings: 600–700. Numbers: tabular-nums.

**Spacing/radius:** 4px base unit. 6px default radius, 8px for cards. `shadow-sm` only. Table rows: 44px height — this is a data tool, not a content site.

## Environment Variables (Backend)

| Variable | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | Azure SQL connection string |
| `Jwt__PrivateKey` | RSA private key PEM |
| `Jwt__PublicKey` | RSA public key PEM |
| `PowerBI__TenantId` | Azure AD tenant ID |
| `PowerBI__ClientId` | Service Principal app ID |
| `PowerBI__ClientSecret` | Service Principal secret |
| `PowerBI__WorkspaceId` | Power BI workspace ID |
| `Azure__BlobStorageConnectionString` | Blob Storage connection string |
| `App__BaseDomain` | `databision.app` |
