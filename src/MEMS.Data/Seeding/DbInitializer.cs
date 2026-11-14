using MEMS.Data.Entities;
using MEMS.Data.Tenancy;
using MEMS.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MEMS.Data.Seeding;

public static class DbInitializer
{
    public static async Task MigrateAndSeedAsync(
        IServiceProvider services, string initialPassword, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbInitializer));

        provider.GetRequiredService<TenantContext>().Unrestrict();

        var db = provider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        await SeedMunicipalitiesAsync(db, logger, cancellationToken);
        await SeedAccountsAsync(provider, db, initialPassword, logger, cancellationToken);
    }

    private static async Task SeedMunicipalitiesAsync(ApplicationDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        if (await db.Municipalities.AnyAsync(cancellationToken))
        {
            return;
        }

        foreach (var seed in SeedData.Municipalities)
        {
            var municipality = new Municipality
            {
                Name = seed.Name,
                Slug = seed.Slug,
                Province = "British Columbia",
                ElectionDate = SeedData.ElectionDate,
                Description = seed.Description,
                Tagline = seed.Tagline,
                ThemeColor = seed.ThemeColor
            };

            foreach (var seedPosition in seed.Positions)
            {
                var position = new Position
                {
                    Name = seedPosition.Name,
                    SeatsAvailable = seedPosition.Seats,
                    Municipality = municipality
                };

                foreach (var seedCandidate in seedPosition.Candidates)
                {
                    var candidate = new Candidate
                    {
                        Name = seedCandidate.Name,
                        Profile = seedCandidate.Profile,
                        Email = seedCandidate.Email,
                        Website = seedCandidate.Website,
                        SocialX = seedCandidate.SocialX,
                        DateCreated = DateTime.UtcNow,
                        Municipality = municipality
                    };

                    if (seedCandidate.Endorsement is { } endorsement)
                    {
                        candidate.Endorsements.Add(new Endorsement
                        {
                            EndorserName = endorsement.EndorserName,
                            Organization = endorsement.Organization,
                            Text = endorsement.Text
                        });
                    }

                    position.Candidates.Add(candidate);
                }

                municipality.Positions.Add(position);
            }

            db.Municipalities.Add(municipality);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {MunicipalityCount} municipalities", SeedData.Municipalities.Length);
    }

    private static async Task SeedAccountsAsync(
        IServiceProvider provider, ApplicationDbContext db, string initialPassword, ILogger logger, CancellationToken cancellationToken)
    {
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (string role in new[] { AppRoles.SuperAdmin, AppRoles.MunicipalityAdmin })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await EnsureUserAsync(userManager, "super", AppRoles.SuperAdmin, municipalityId: null, initialPassword, logger);

        foreach (var seed in SeedData.Municipalities.Where(m => m.AdminUserName is not null))
        {
            int? municipalityId = await db.Municipalities
                .Where(m => m.Slug == seed.Slug)
                .Select(m => (int?)m.MunicipalityId)
                .FirstOrDefaultAsync(cancellationToken);

            if (municipalityId is not null)
            {
                await EnsureUserAsync(userManager, seed.AdminUserName!, AppRoles.MunicipalityAdmin, municipalityId, initialPassword, logger);
            }
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager, string userName, string role, int? municipalityId, string password, ILogger logger)
    {
        if (await userManager.FindByNameAsync(userName) is not null)
        {
            return;
        }

        var user = new ApplicationUser { UserName = userName, MunicipalityId = municipalityId };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create the seed account '{userName}': {string.Join(" ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(user, role);
        logger.LogInformation("Seeded administrator {UserName} with role {Role}", userName, role);
    }
}
