# Scaffold tenant-aware CRUD (service + API + Blazor admin pages)

Generate CRUD for the entity `${input:entity}` following `.github/copilot-instructions.md` and the
`multi-tenancy` skill.

1. `MEMS.Shared/Contracts`: `${input:entity}Dto` and `${input:entity}Input` with DataAnnotations and
   user-friendly error messages.
2. `MEMS.Data/Services`: `I${input:entity}Service` + implementation returning `Result<T>`; call
   `EnsureInitializedAsync()` first; run `InputValidator.Validate(input)`; log create/update/delete
   with structured properties.
3. `MEMS.Api/Controllers`: route `api/{tenant}/${input:route}`, anonymous GETs, `TenantAdmin` policy on
   writes, `ProducesResponseType` for 200/201/204/400/401/403/404/409, XML summary for Swagger.
4. `MEMS.Admin/Components/Pages/${input:entity}s`: list (with confirm-delete dialog), editor page with two
   routes (`/create` and `/{Id:int}/edit`) using `EditForm`, `DataAnnotationsValidator` and
   `ServerValidation`, and a details page. Bootstrap 5, responsive tables (`table-responsive`).
5. Register the service in `DataServiceExtensions.AddMemsData`.
