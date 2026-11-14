Municipal Elections Management System - Assignment 2
====================================================

Multi-tenant .NET 10 solution orchestrated by .NET Aspire (solution file: MEMS.slnx).

  src/MEMS.AppHost          Aspire orchestration (SQL Server container, API, Admin, Public)
  src/MEMS.ServiceDefaults  OpenTelemetry, health checks, service discovery, resilience
  src/MEMS.Admin            Blazor Server admin site, shared by all municipalities  (/admin)
  src/MEMS.Public           Blazor WebAssembly public site, one per tenant via URL (/burnaby, /surrey, ...)
  src/MEMS.Api              ASP.NET Core Web API (/api/{tenant}/...), JWT, Swagger (/swagger)
  src/MEMS.Data             EF Core (SQL Server), migrations, seed data, service layer
  src/MEMS.Shared           DTOs and validated input contracts

Documentation: docs/Assignment2-Documentation.pdf (source: Assignment2-Documentation.html)
  docs/api.md                API reference: endpoints, request/response and error examples
  docs/erd.md                ERD (Mermaid)
  docs/deficiency-report.md  Assignment 1 deficiency resolution report
  docs/copilot-usage.md      GitHub Copilot usage
  docs/screenshots/a2/       Screenshots (Aspire dashboard, admin site, public site, API testing, SQL Server)

The Assignment 1 MVC project (src/MunicipalElections, MunicipalElections.slnx) is kept for reference
and is not part of MEMS.slnx.

Requirements
------------
- .NET 10 SDK
- Docker Desktop (or Podman) running: Aspire starts SQL Server 2022 in a container
- Trusted HTTPS development certificate: dotnet dev-certs https --trust

Running
-------
  dotnet run --project src/MEMS.AppHost

Open the Aspire dashboard link printed in the console. From there:
  Admin site    https://localhost:7196/admin
  Public site   https://localhost:7180          (e.g. https://localhost:7180/burnaby)
  API / Swagger https://localhost:7045/swagger

On the first run the API applies the EF Core migration and seeds the data. The SQL Server data is kept in
the Docker volume "mems-sql-data". Aspire generates the SQL Server password and the JWT signing key and
stores them in the AppHost's user secrets.

Accounts (initial password P@$$w0rd, the AppHost parameter "seed-admin-password")
----------------------------------------------------------------------------------
  super          Super Admin                 all municipalities
  burnabyadmin   Municipality Administrator  Burnaby
  surreyadmin    Municipality Administrator  Surrey
  richmondadmin  Municipality Administrator  Richmond
  pm             Municipality Administrator  Pitt Meadows (Assignment 1 account)

Rules worth knowing
-------------------
- Tenant isolation: a Municipality Administrator sees and changes only their municipality, in the admin
  site and in the API. Another tenant's record is reported as "not found"; writes to another tenant's
  API URL return 403.
- Only a Super Admin creates or deletes municipalities, changes URL slugs and manages accounts.
- Voting-guide selections are stored only in the browser's sessionStorage. They are never sent to the
  API or written to SQL Server, and they disappear when the browser session ends.
- For each position a visitor may choose as many candidates as there are seats.

Team
----
Polina Omelyantseva
