# Skill: Tenant isolation in MEMS

## When to use
Any change that reads or writes positions, candidates, endorsements, images or municipalities,
adds an API endpoint, or adds an admin page.

## How the boundary is set
| Host | Who sets `TenantContext` | From what |
|---|---|---|
| API, `/api/{tenant}/...` | `TenantResolutionMiddleware` | the slug in the URL (404 if unknown) |
| API, other routes | `TenantResolutionMiddleware` | the JWT (`role`, `tenant_id`) |
| Admin (Blazor Server) | `CircuitTenantInitializer` | the signed-in user's cookie claims |
| Seeder | `DbInitializer` | `Unrestrict()` (system task) |

`TenantContext` fails closed: if nobody sets it, the query filters return no tenant rows.

## Checklist for a new feature
1. Contract in `MEMS.Shared/Contracts` with DataAnnotations.
2. Service in `MEMS.Data/Services`:
   - call `EnsureInitializedAsync()` first;
   - create the DbContext from `IDbContextFactory<ApplicationDbContext>`;
   - look records up through the filtered `DbSet` (another tenant's id → `Result.NotFound`);
   - derive `MunicipalityId` from a filtered parent (e.g. the position), never from input.
3. API controller under `api/{tenant}/...`, `[Authorize(Policy = ApiPolicies.TenantAdmin)]` on writes,
   `ProducesResponseType` for every status, `this.ToProblem(result)` for failures.
4. Admin page in `MEMS.Admin/Components/Pages`, inject the same service; it is already tenant-scoped.
5. Test the negative case: Burnaby admin token against `/api/surrey/...` must give 403;
   a Surrey id through `/api/burnaby/...` must give 404.
