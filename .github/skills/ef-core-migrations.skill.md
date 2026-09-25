# Skill: EF Core Code First Migrations

## When to use
Adding or changing a domain entity, relationship, or constraint in this project.

## Ground rules
- The model classes in `src/MunicipalElections/Models` are the single source of truth.
  Never hand-edit the SQLite database or the migration files to change schema.
- All `dotnet` commands run inside the Podman dev container via `./scripts/dotnet.sh`.
  Never assume a host .NET SDK is installed.

## Workflow
1. Change the entity in `Models/` and, when needed, the fluent configuration in
   `Data/ApplicationDbContext.cs` (max lengths, delete behaviour, indexes).
2. Add the migration:
   `./scripts/dotnet.sh ef migrations add <PascalCaseName> --project src/MunicipalElections`
3. Review the generated `Data/Migrations/*.cs`: confirm only the intended tables,
   columns, and foreign keys changed.
4. Apply it:
   `./scripts/dotnet.sh ef database update --project src/MunicipalElections`
5. Build to confirm the model snapshot and entities still agree:
   `./scripts/dotnet.sh build src/MunicipalElections`

## Conventions
- Migration names are PascalCase and describe the change (`InitialCreate`,
  `AddCandidateVideoUrl`).
- Prefer `dotnet ef` scaffolding over editing migration code by hand; edit only when
  the generator cannot express the intent (for example a data backfill).
- Keep delete behaviour explicit: child data of a municipality cascades.
