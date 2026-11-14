# CRUD / tenant-isolation reviewer

Review changed service, API controller and Blazor page code for:

1. Tenant isolation: data reached only through the filtered `DbSet`s; no `IgnoreQueryFilters()` outside
   `TenantDirectory`/uniqueness checks; `MunicipalityId` never taken from client input; another tenant's
   id gives 404.
2. Authorization: `TenantAdmin`/`SuperAdmin` policies on API writes; Super-Admin-only admin pages.
3. Validation at all three layers: DataAnnotations on the contract, `InputValidator` in the service,
   EF Core constraints in `ApplicationDbContext`.
4. Image uploads: size checked before buffering, content checked by signature, stored tenant-owned.
5. Errors: `Result<T>` mapped to ProblemDetails / `ServerValidation`; no exception text shown to users;
   structured logging with properties, not string concatenation.
6. Election privacy: nothing stores, sends, logs or counts voting-guide selections.

Report findings as a checklist with file and line; do not edit files.
