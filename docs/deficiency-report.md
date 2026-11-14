# Assignment 1 deficiency resolution report

> **To complete before submission:** add every issue from the instructor's Assignment 1 marking
> feedback to Part A, and say how it was resolved. The feedback is not in the repository, so
> Part A cannot be filled in for you. Part B lists the problems found by reviewing the Assignment 1
> code while migrating it. Each fix is in the Assignment 2 code.

## Part A: Issues from the instructor's Assignment 1 marking

| # | Issue | Original problem | Resolution |
|---|---|---|---|
| A1 | _(from marking feedback)_ | | |
| A2 | | | |

## Part B: Issues found by reviewing the Assignment 1 code

| # | Issue | Original problem (Assignment 1) | Resolution (Assignment 2) |
|---|---|---|---|
| B1 | Data integrity | A candidate's municipality was only implied through its position, and nothing in the database stopped a candidate being linked to a position of another municipality. | `Candidates.MunicipalityId` plus a composite foreign key `(PositionId, MunicipalityId) → Positions`. The database rejects a cross-tenant candidate. |
| B2 | Data integrity | Position edit (`_context.Update(position)` with a posted `MunicipalityId`) could move a position, and its candidates, to another municipality. | A position's municipality cannot be changed (`PositionService.UpdateAsync` updates only name and seats). A candidate can change position only within its own municipality. |
| B3 | Data integrity | No uniqueness for position names within a municipality, no range check on election dates, and no database-level constraints beyond lengths. | Unique index `(MunicipalityId, Name)` on positions. Unique `Slug` and `Name` on municipalities. CHECK constraints on slug format, theme colour, election date (2000–2100), seats (1–100) and image size. |
| B4 | Authorization design | Tenant access depended on per-municipality Identity roles named after the municipality (`"<Name> Admin"`), which were renamed when the municipality was renamed. A user could belong to several of these roles, and every controller action had to remember to call `MunicipalityAccessService`. | Two fixed roles (`SuperAdmin`, `MunicipalityAdmin`) and `AspNetUsers.MunicipalityId`, so a Municipality Administrator has exactly one tenant. Isolation is enforced centrally by EF Core global query filters that fail closed, then again in the API (`TenantResolutionMiddleware` + `TenantAdmin` policy) and in the service layer. |
| B5 | Security | Initial seed password hard-coded in `DbInitializer` source. Password policy was 6 characters with no complexity. | Seed password is an Aspire secret parameter (`seed-admin-password`). Policy is 8+ characters with upper, lower, digit and symbol. The JWT signing key is a generated, persisted Aspire secret. |
| B6 | Security | Sessions stayed valid after an account was deleted or its password reset (default 30-minute stamp validation). | Security stamp validated on every admin request. Blazor circuits revalidate every minute. A deleted or reset account is signed out immediately (verified by test). |
| B7 | Security | `[Url]` accepted `ftp://` links. | `[HttpUrl]` accepts only absolute `http`/`https` URLs, so `javascript:` and `data:` links are impossible, at every layer. |
| B8 | Election privacy | The voting guide was stored in server-side ASP.NET session state (distributed memory cache, 7-day idle timeout), so the server held every visitor's choices. | Selections live only in the browser's `sessionStorage`. They are never sent to the API or written to SQL Server, and the browser clears them when the session ends. |
| B9 | Broken / incomplete cleanup | Deleting a position removed its candidates by cascade but left their photo files on disk. Delete actions called `SaveChangesAsync` even when nothing was found. | Images are stored in SQL Server as tenant-owned rows and removed in the same transaction as their owner. Deletes return 404 when the record is not found. |
| B10 | Architecture | Uploaded images were written to a local folder, which another application (API, second instance) could not see. | `Images` table, served by the API (`/api/{tenant}/images/{id}`) and the Admin app through the same tenant boundary. Responses are cacheable because ids are immutable. |
| B11 | Error handling | Developer exception page in Development, MVC error view elsewhere, no central logging of unhandled errors. | `IExceptionHandler` + ProblemDetails in the API, `UseExceptionHandler` and a logging `ErrorBoundary` in both Blazor apps, structured logging exported to the Aspire dashboard, user-facing messages with a trace id only. |
| B12 | Validation | Validation lived in MVC model attributes only; anything that bypassed the form also bypassed most checks. | Three layers: DataAnnotations on shared contracts (Blazor client validation), the same rules re-run in the service layer for every caller (API and Admin), and EF Core / SQL Server constraints. |
| B13 | Usability | Municipality administrators saw a restricted public site when signed in, and could not use the voting guide. | The public site is a separate anonymous application. Administrators manage data in `/admin` only. |
| B14 | Code quality | Data access and authorization logic were spread across MVC controllers. | Service layer in `MEMS.Data` (`IMunicipalityService`, `IPositionService`, `ICandidateService`, `IAdminUserService`) returning `Result<T>`, shared by the API and the Admin app. |
| B15 | Database | SQLite. | SQL Server in an Aspire-managed container with a persistent volume. The connection string is supplied by Aspire. |
