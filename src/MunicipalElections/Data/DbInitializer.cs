using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Models;
using MunicipalElections.Services;

namespace MunicipalElections.Data;

public static class DbInitializer
{
    private const string InitialPassword = "P@$$w0rd";

    private record SeedCandidate(string Name, string Profile, string? Email = null, string? Website = null, string? SocialX = null, Endorsement? Endorsement = null);

    private record SeedPosition(string Name, int Seats, SeedCandidate[] Candidates);

    private record SeedMunicipality(string Name, string Description, SeedPosition[] Positions);

    private static readonly SeedMunicipality[] SeedData =
    [
        new("Pitt Meadows", "A family-friendly community where the mountains meet the Fraser River.",
        [
            new("Mayor", 1,
            [
                new("Jordan Lee", "Jordan has served on council for eight years and is committed to affordable housing and climate resilience.",
                    "jordan.lee@example.com", "https://jordanlee.example.com", "https://x.com/jordanlee",
                    new Endorsement { EndorserName = "Pitt Meadows Chamber of Commerce", Organization = "PMCC", Text = "A proven leader for our community." }),
                new("Morgan Chen", "A small-business owner championing local economic growth and public safety.", "morgan.chen@example.com")
            ]),
            new("Councilor", 6,
            [
                new("Taylor Singh", "Youth advocate and lifelong Pitt Meadows resident focused on recreation and transit.", Website: "https://taylorsingh.example.com"),
                new("Alex Tran", "Urban planner with a passion for walkable neighbourhoods.", "alex.tran@example.com"),
                new("Priya Patel", "Teacher and parent advocating for schools and community services.")
            ]),
            new("School Trustee", 2,
            [
                new("Sam Nguyen", "Education advocate focused on student well-being and modern learning spaces.", "sam.nguyen@example.com")
            ])
        ]),
        new("Maple Ridge", "A growing riverside city known for its parks and trails.",
        [
            new("Mayor", 1,
            [
                new("Dana Whitfield", "Two-term councillor running on downtown revitalization and a faster development approvals process.",
                    "dana.whitfield@example.com", "https://danawhitfield.example.com",
                    Endorsement: new Endorsement { EndorserName = "Ridge Meadows Business Association", Organization = "RMBA", Text = "Dana understands what local employers need." }),
                new("Ravi Dhillon", "Former fire captain focused on emergency preparedness and wildfire protection along the urban edge.", "ravi.dhillon@example.com")
            ]),
            new("Councilor", 6,
            [
                new("Elena Markovic", "Trail-network volunteer who wants safer cycling routes between Albion and the town centre.", "elena.markovic@example.com"),
                new("Marcus Hale", "Accountant promising line-by-line budget reviews and stable property taxes."),
                new("Sophie Lam", "Housing-policy researcher advocating for more rental homes near transit.", Website: "https://sophielam.example.com"),
                new("Owen Fraser", "Farmer and agricultural-land advocate from the Whonnock area.")
            ]),
            new("School Trustee", 2,
            [
                new("Grace Okafor", "Parent advisory council chair focused on reducing portable classrooms.", "grace.okafor@example.com"),
                new("Liam Bouchard", "Retired teacher championing trades programs and inclusive education.")
            ])
        ]),
        new("Burnaby", "The geographic heart of Metro Vancouver.",
        [
            new("Mayor", 1,
            [
                new("Helen Zhao", "Community organizer prioritizing tenant protections and non-market housing around SkyTrain stations.",
                    "helen.zhao@example.com", "https://helenzhao.example.com", "https://x.com/helenzhao",
                    new Endorsement { EndorserName = "Burnaby Tenants Coalition", Text = "Helen has stood with renters for a decade." }),
                new("Peter Kowalski", "Engineer and business owner focused on infrastructure renewal and fiscal discipline.", "peter.kowalski@example.com"),
                new("Amara Diallo", "Environmental lawyer running to protect Burnaby Mountain and expand urban tree cover.")
            ]),
            new("Councilor", 8,
            [
                new("Kevin Park", "Small-restaurant owner who wants simpler permitting for local businesses.", "kevin.park@example.com"),
                new("Nadia Rahman", "Public-health nurse advocating for community centres and seniors' services."),
                new("Thomas Wright", "Transit planner pushing for bus-priority lanes on Hastings and Kingsway.", Website: "https://thomaswright.example.com"),
                new("Isabel Santos", "Youth soccer coach focused on parks, pools and affordable recreation.")
            ]),
            new("School Trustee", 7,
            [
                new("Mei Tanaka", "Speech-language pathologist focused on early-intervention supports.", "mei.tanaka@example.com"),
                new("Daniel Abara", "Software developer advocating for modern technology programs in every school.")
            ])
        ]),
        new("Surrey", "One of Canada's fastest-growing cities.",
        [
            new("Mayor", 1,
            [
                new("Harpreet Gill", "Three-term councillor campaigning on new schools, hospital capacity and rapid transit south of the Fraser.",
                    "harpreet.gill@example.com", "https://harpreetgill.example.com",
                    Endorsement: new Endorsement { EndorserName = "Surrey Board of Trade", Organization = "SBOT", Text = "A steady hand for a fast-growing city." }),
                new("Claire Donovan", "Former school principal focused on public safety and youth programs.", "claire.donovan@example.com")
            ]),
            new("Councilor", 8,
            [
                new("Jasmine Sandhu", "Civil engineer who wants road and sewer upgrades to keep pace with growth.", "jasmine.sandhu@example.com"),
                new("Michael Osei", "Non-profit director working on newcomer settlement and affordable childcare."),
                new("Rachel Kim", "Small-business advocate for the Newton and Guildford town centres.", Website: "https://rachelkim.example.com"),
                new("Brent Lawson", "Conservationist focused on protecting the Green Timbers urban forest.")
            ]),
            new("School Trustee", 6,
            [
                new("Anita Verma", "Parent of three pushing to end overcrowding and extended-day schedules.", "anita.verma@example.com"),
                new("Colin Mercer", "Education assistant advocating for classroom supports and safe schools.")
            ])
        ])
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync();

        if (!await db.Municipalities.AnyAsync())
        {
            foreach (var seed in SeedData)
            {
                db.Municipalities.Add(new Municipality
                {
                    Name = seed.Name,
                    Province = "British Columbia",
                    ElectionDate = new DateTime(2026, 10, 17),
                    Description = seed.Description,
                    AdminRoleName = Roles.ForMunicipality(seed.Name)
                });
            }

            await db.SaveChangesAsync();
        }

        foreach (var seed in SeedData)
        {
            var municipality = await db.Municipalities.FirstOrDefaultAsync(m => m.Name == seed.Name);
            if (municipality is null || await db.Positions.AnyAsync(p => p.MunicipalityId == municipality.MunicipalityId))
            {
                continue;
            }

            foreach (var seedPosition in seed.Positions)
            {
                var position = new Position
                {
                    Name = seedPosition.Name,
                    SeatsAvailable = seedPosition.Seats,
                    MunicipalityId = municipality.MunicipalityId
                };

                foreach (var c in seedPosition.Candidates)
                {
                    var candidate = new Candidate
                    {
                        Name = c.Name,
                        Profile = c.Profile,
                        Email = c.Email,
                        Website = c.Website,
                        SocialX = c.SocialX,
                        DateCreated = DateTime.UtcNow
                    };

                    if (c.Endorsement is not null)
                    {
                        candidate.Endorsements.Add(new Endorsement
                        {
                            EndorserName = c.Endorsement.EndorserName,
                            Organization = c.Endorsement.Organization,
                            Text = c.Endorsement.Text
                        });
                    }

                    position.Candidates.Add(candidate);
                }

                db.Positions.Add(position);
            }

            await db.SaveChangesAsync();
        }

        await EnsureRoleAsync(roleManager, Roles.SuperAdmin);
        foreach (var roleName in await db.Municipalities.Select(m => m.AdminRoleName).ToListAsync())
        {
            await EnsureRoleAsync(roleManager, roleName);
        }

        await EnsureUserAsync(userManager, "super", Roles.SuperAdmin);
        if (await roleManager.RoleExistsAsync(Roles.ForMunicipality("Pitt Meadows")))
        {
            await EnsureUserAsync(userManager, "pm", Roles.ForMunicipality("Pitt Meadows"));
        }
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, string userName, string role)
    {
        if (await userManager.FindByNameAsync(userName) is not null)
        {
            return;
        }

        var user = new ApplicationUser { UserName = userName, Email = $"{userName}@municipal.local", EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, InitialPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Could not create user '{userName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(user, role);
    }
}
