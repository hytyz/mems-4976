namespace MEMS.Data.Seeding;

internal sealed record SeedEndorsement(string EndorserName, string Text, string? Organization = null);

internal sealed record SeedCandidate(
    string Name,
    string Profile,
    string? Email = null,
    string? Website = null,
    string? SocialX = null,
    SeedEndorsement? Endorsement = null);

internal sealed record SeedPosition(string Name, int Seats, SeedCandidate[] Candidates);

internal sealed record SeedMunicipality(
    string Name,
    string Slug,
    string ThemeColor,
    string Tagline,
    string Description,
    string? AdminUserName,
    SeedPosition[] Positions);

internal static class SeedData
{
    public static readonly DateOnly ElectionDate = new(2026, 10, 17);

    public static readonly SeedMunicipality[] Municipalities =
    [
        new("Burnaby", "burnaby", "#1d4e89", "The geographic heart of Metro Vancouver.",
            "Burnaby sits between Vancouver and the Tri-Cities, home to Burnaby Mountain, Deer Lake and four SkyTrain town centres.",
            "burnabyadmin",
        [
            new("Mayor", 1,
            [
                new("Helen Zhao", "Community organizer prioritizing tenant protections and non-market housing around SkyTrain stations.",
                    "helen.zhao@example.com", "https://helenzhao.example.com", "https://x.com/helenzhao",
                    new("Burnaby Tenants Coalition", "Helen has stood with renters for a decade.")),
                new("Peter Kowalski", "Engineer and business owner focused on infrastructure renewal and fiscal discipline.", "peter.kowalski@example.com"),
                new("Amara Diallo", "Environmental lawyer running to protect Burnaby Mountain and expand urban tree cover.")
            ]),
            new("Councillor", 8,
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
        new("Surrey", "surrey", "#0f766e", "The future lives here.",
            "One of Canada's fastest-growing cities, stretching from the Fraser River to Boundary Bay.",
            "surreyadmin",
        [
            new("Mayor", 1,
            [
                new("Harpreet Gill", "Three-term councillor campaigning on new schools, hospital capacity and rapid transit south of the Fraser.",
                    "harpreet.gill@example.com", "https://harpreetgill.example.com",
                    Endorsement: new("Surrey Board of Trade", "A steady hand for a fast-growing city.", "SBOT")),
                new("Claire Donovan", "Former school principal focused on public safety and youth programs.", "claire.donovan@example.com")
            ]),
            new("Councillor", 8,
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
        ]),
        new("Richmond", "richmond", "#b3261e", "Island city, by nature.",
            "An island city at the mouth of the Fraser River, known for Steveston, its dykes and farmland, and the airport next door.",
            "richmondadmin",
        [
            new("Mayor", 1,
            [
                new("Vivian Leung", "Two-term councillor campaigning on flood protection, dyke upgrades and housing near the Canada Line.",
                    "vivian.leung@example.com", "https://vivianleung.example.com", "https://x.com/vivianleung",
                    new("Richmond Chamber of Commerce", "Vivian plans for the long term and delivers.", "RCC")),
                new("Graham Sterling", "Steveston harbour business owner focused on farmland protection and small-business taxes.", "graham.sterling@example.com")
            ]),
            new("Councillor", 8,
            [
                new("Aisha Farouk", "Emergency physician advocating for a new acute-care tower and seniors' housing.", "aisha.farouk@example.com"),
                new("Benjamin Cho", "Cycling advocate who wants a connected, protected bike network across Lulu Island."),
                new("Lorraine Pascal", "Third-generation farmer defending the Agricultural Land Reserve.", Website: "https://lorrainepascal.example.com"),
                new("Dev Malhotra", "Accountant promising transparent budgets and stable utility rates.")
            ]),
            new("School Trustee", 7,
            [
                new("Catherine Yuen", "Former PAC president focused on seismic upgrades for older schools.", "catherine.yuen@example.com"),
                new("Tobias Reinhardt", "Music teacher championing arts programs and inclusive classrooms.")
            ])
        ]),
        new("Pitt Meadows", "pitt-meadows", "#3f6212", "The natural place.",
            "A family-friendly community where the mountains meet the Fraser River.",
            "pm",
        [
            new("Mayor", 1,
            [
                new("Jordan Lee", "Jordan has served on council for eight years and is committed to affordable housing and climate resilience.",
                    "jordan.lee@example.com", "https://jordanlee.example.com", "https://x.com/jordanlee",
                    new("Pitt Meadows Chamber of Commerce", "A proven leader for our community.", "PMCC")),
                new("Morgan Chen", "A small-business owner championing local economic growth and public safety.", "morgan.chen@example.com")
            ]),
            new("Councillor", 6,
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
        new("Maple Ridge", "maple-ridge", "#6d28d9", "Deep roots, greater heights.",
            "A growing riverside city known for its parks and trails.",
            null,
        [
            new("Mayor", 1,
            [
                new("Dana Whitfield", "Two-term councillor running on downtown revitalization and a faster development approvals process.",
                    "dana.whitfield@example.com", "https://danawhitfield.example.com",
                    Endorsement: new("Ridge Meadows Business Association", "Dana understands what local employers need.", "RMBA")),
                new("Ravi Dhillon", "Former fire captain focused on emergency preparedness and wildfire protection along the urban edge.", "ravi.dhillon@example.com")
            ]),
            new("Councillor", 6,
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
        ])
    ];
}
