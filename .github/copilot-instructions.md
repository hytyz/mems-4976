# Copilot Instructions

## Project
Municipal Elections Management System (MEMS), Assignment 2: a multi-tenant .NET 10 solution
orchestrated by .NET Aspire. Each municipality is a tenant identified by a URL slug (`/burnaby`).

| Project | Role |
|---|---|
| `MEMS.AppHost` | Aspire orchestration: SQL Server container, API, Admin, Public; parameters and service discovery |
| `MEMS.ServiceDefaults` | OpenTelemetry, health checks, service discovery, HTTP resilience |
| `MEMS.Admin` | Blazor Server admin site under `/admin`, ASP.NET Core Identity cookies |
| `MEMS.Public` | Blazor WebAssembly public site, one app for every tenant (`/{tenant}/...`) |
| `MEMS.Api` | ASP.NET Core Web API (controllers), JWT bearer, OpenAPI + Swagger UI |
| `MEMS.Data` | EF Core (SQL Server) DbContext, migrations, seed data, service layer |
| `MEMS.Shared` | DTOs/input contracts with DataAnnotations, shared constants |

## Conventions
- File-scoped namespaces, nullable enabled, primary constructors for services.
- Code First: change entities in `MEMS.Data/Entities`, then
  `dotnet ef migrations add <Name> --project src/MEMS.Data --startup-project src/MEMS.Data -o Migrations`.
- Never add a connection string to a project. Aspire supplies `ConnectionStrings:memsdb`.
- Never reference `MEMS.Data` from `MEMS.Public`; the public site reads data only through the API.
- Put validation on the contracts in `MEMS.Shared` (client), re-run it in the service layer (API and
  Admin), and back it with EF Core constraints (max length, unique indexes, check constraints).
- Service methods return `Result<T>`; the API maps it to ProblemDetails, the Admin to `ServerValidation`.

## Multi-tenancy rules (must hold for every change)
- Tenant-owned entities (`Municipality`, `Position`, `Candidate`, `Endorsement`, `StoredImage`) have
  global query filters on `ApplicationDbContext` driven by `ITenantContext`. Do not call
  `IgnoreQueryFilters()` outside `TenantDirectory` and uniqueness checks.
- Every service method starts with `await tenantInitializer.EnsureInitializedAsync()`.
- API routes for tenant data are `api/{tenant}/...`; `TenantResolutionMiddleware` restricts the request
  to that tenant. Write endpoints use the `TenantAdmin` policy. Take the tenant from the URL, never from
  the request body.
- A record outside the caller's tenant is reported as 404, not 403, so ids cannot be probed.
- `Candidate.MunicipalityId` must equal its position's municipality (composite FK enforces it).

## Roles
- `SuperAdmin`: everything, including creating/deleting tenants and administrator accounts.
- `MunicipalityAdmin`: exactly one tenant (`ApplicationUser.MunicipalityId`), cannot change slugs.
- No self-registration. Never add a Register page or `AddDefaultUI()`.

## Election privacy
- Voting-guide selections live only in browser `sessionStorage` (`VotingGuide` in `MEMS.Public`).
- Never add an entity, table, endpoint, log statement or telemetry attribute that carries ballot
  selections, candidate choices or vote totals.

## UI
- Bootstrap 5, responsive, dark mode through `data-bs-theme`.
- Admin forms: `EditForm` + `DataAnnotationsValidator` + `ServerValidation` + `ValidationMessage`.
- Public tenant pages wrap their content in `<TenantShell Slug="@Tenant">` and inherit `TenantPage`.
- Show friendly messages; log exceptions with structured logging, never show them.
