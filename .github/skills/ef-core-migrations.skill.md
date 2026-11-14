# Skill: EF Core migrations (SQL Server)

## When to use
Any change to an entity or to `ApplicationDbContext.OnModelCreating`.

## Steps
1. Edit the entity in `src/MEMS.Data/Entities` and its configuration in `ApplicationDbContext`
   (max lengths, indexes, check constraints, delete behaviour).
2. Generate the migration (no database needed, `DesignTimeDbContextFactory` builds the model):
   ```
   dotnet ef migrations add <Name> --project src/MEMS.Data --startup-project src/MEMS.Data -o Migrations
   ```
3. Read the generated `Up`/`Down`. SQL Server allows only one cascade path to a table: use
   `DeleteBehavior.NoAction` on the extra paths and delete explicitly in the service.
4. Run the AppHost. `MEMS.Api` applies migrations and seeds on start; the Admin app waits for it.

## Rules
- Never edit a migration that has been applied; add a new one.
- Seed data lives in `src/MEMS.Data/Seeding/SeedData.cs`; the seeder only adds what is missing.
- Never add tables or columns for ballot selections or vote counts.
