# GitHub Copilot usage

## 1. Copilot customization files in this repository

| File | Purpose |
|---|---|
| `.github/copilot-instructions.md` | Repository-wide instructions: project map, multi-tenancy rules, roles, election-privacy rules, validation and UI conventions. Loaded by Copilot Chat for every request in this repository. |
| `.github/skills/multi-tenancy.skill.md` | How the tenant boundary is set in each host, plus a checklist for adding a tenant-aware feature. |
| `.github/skills/identity-authorization.skill.md` | Roles, tenant claims, Admin and API authorization, password policy. |
| `.github/skills/ef-core-migrations.skill.md` | How to add a SQL Server migration and the cascade-path rule. |
| `.github/prompts/scaffold-crud.prompt.md` | Reusable prompt that generates a contract, service, API controller and Blazor admin pages for an entity. |
| `.github/prompts/seed-data.prompt.md` | Reusable prompt for adding a seeded municipality. |
| `.github/agents/crud-reviewer.agent.md` | Review agent that checks tenant isolation, authorization, validation, uploads, error handling and privacy. |

## 2. How Copilot was used

Copilot was used in VS Code through Copilot Chat, as the assignment requires.

- **Copilot instructions.** `copilot-instructions.md` was carried over from Assignment 1 and rewritten
  for the new architecture once the project structure was settled, so that Chat would stop suggesting
  MVC controllers and SQLite.
- **Inline completions** were enabled at the start and switched off after the first two days. They
  mostly completed boilerplate (`ProducesResponseType` attributes, DTO properties, `using` lines). When
  a suggestion went beyond boilerplate, it usually had to be deleted.
- **Copilot Chat** was used for specific questions, mostly about Aspire APIs and Blazor render modes,
  where the documentation is spread across several versions. Answers were checked against the
  official documentation before they were used.
- **Prompt files.** `scaffold-crud` was used once, for the first draft of the Position contract,
  service and admin pages. The Candidate and Municipality pages were written by hand from the
  corrected Position version. `seed-data` was used to draft the Maple Ridge seed data.
- **Reviewer agent.** `crud-reviewer` was run once against the finished services and controllers
  before submission.
- **Skill files** were written to document the rules that Chat kept getting wrong (tenant boundary,
  roles, cascade paths). They were referenced in Chat requests where they were relevant.

All generated code was read line by line before it was committed. Nothing was accepted without changes.

## 3. Example prompts

| Prompt | What Copilot produced | What was changed |
|---|---|---|
| `/scaffold-crud entity=Position route=positions` | A contract, service, controller and three Razor pages. | The service read `MunicipalityId` from the request body instead of the URL tenant, and the edit page let the user change a position's municipality. Both were removed. The `TenantAdmin` policy was missing on the PUT and DELETE endpoints and was added. |
| "How do I pass the API URL from the Aspire AppHost to a Blazor WebAssembly project?" | `WithReference(api)` plus reading `services__api__https__0` from the environment. | That does not work, because a WASM app runs in the browser and has no environment variables. Replaced by `BrowserServiceReference`, which writes the address into the app's `wwwroot/appsettings.json`. |
| "Add a global query filter so municipality admins only see their own candidates" | A filter on `Candidate` using a tenant id captured once in the `DbContext` constructor. | The value has to be read per query from `ITenantContext`, and Super Admins need a bypass. Rewritten as `IsUnrestricted \|\| c.MunicipalityId == CurrentTenantId`, and the same filter was added to every tenant-owned entity. |
| "Store the voting guide selections in the browser" | A service using `localStorage`. | Changed to `sessionStorage`, as the election-privacy section requires. |
| `/seed-data` for Maple Ridge | A municipality with positions and candidates. | Shortened descriptions that exceeded the column lengths and fixed duplicate candidate names. |
| `@crud-reviewer` on `MEMS.Data/Services` and `MEMS.Api/Controllers` | Six findings. | Two were valid (a missing size check before buffering an upload, and a log message built with string interpolation). The other four described problems the code does not have. |

## 4. Productivity improvements and problems encountered

### Productivity improvements

- Repetitive code was faster to write: response-type attributes, DTO mapping and Bootstrap table markup.
- Chat was quicker than searching when a question had one clear answer, such as an EF Core method
  name or a Bootstrap class.
- The scaffold prompt produced a working first draft of one CRUD feature. Correcting it took about as
  long as writing it would have, so it was not used for the other two entities.

Overall the time saved was small, and most of it was spent again reviewing and correcting suggestions.

### Problems encountered

- **Tenant isolation.** Copilot repeatedly suggested `IgnoreQueryFilters()` to "fix" queries that
  correctly returned no rows, and took the municipality id from request bodies. Adding the
  multi-tenancy rules to `copilot-instructions.md` reduced this but did not stop it.
- **Outdated APIs.** Suggestions mixed Aspire 8, 9 and 13 APIs, and .NET 8 Blazor templates with
  .NET 10 ones. Several suggested methods did not exist in the installed packages.
- **Blazor render modes.** Copilot suggested making the login page interactive, which breaks cookie
  sign-in because Blazor Server cannot set cookies over a SignalR circuit. The page was moved to
  static rendering (`[ExcludeFromInteractiveRouting]`).
- **Ignored instructions.** Despite the instructions file, it suggested a Register page and
  `AddDefaultUI()` twice, both of which Assignment 1's feedback asked to remove.
- **Unreliable review.** The reviewer agent's findings had to be checked one by one, because most of
  them were wrong.
