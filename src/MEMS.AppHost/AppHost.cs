using MEMS.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// ---- SQL Server (container) -------------------------------------------------------------
// The data volume keeps the database between runs. Aspire generates the sa password and
// builds the connection string; no project contains one.
var sql = builder.AddSqlServer("sql")
    .WithDataVolume("mems-sql-data");

var database = sql.AddDatabase("memsdb");

// ---- Parameters (configuration managed by Aspire) ---------------------------------------
// Initial password of the seeded administrator accounts (see appsettings.json > Parameters).
var seedPassword = builder.AddParameter("seed-admin-password", secret: true);

// Signing key for the API's bearer tokens: generated once, kept in the AppHost's user secrets.
var jwtSigningKey = builder.AddParameter(
    "jwt-signing-key",
    new GenerateParameterDefault { MinLength = 64, Special = false },
    secret: true,
    persist: true);

// ---- ASP.NET Web API --------------------------------------------------------------------
// Owns the schema: applies EF Core migrations and seeds on start.
var api = builder.AddProject<Projects.MEMS_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("Seed__AdminPassword", seedPassword)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("https", url => url.DisplayText = "API (Swagger)");

// ---- Public site: Blazor WebAssembly, one app for every tenant (/burnaby, /surrey, ...) ---
// It runs in the browser, so it cannot read environment variables. The API reference is
// delivered as service-discovery configuration in the app's wwwroot/appsettings.json.
var publicSite = builder.AddProject<Projects.MEMS_Public>("public")
    .WithBrowserServiceReference(api)
    .WaitFor(api)
    .WithUrlForEndpoint("https", url => url.DisplayText = "Public site");

// The browser calls the API cross-origin: allow exactly the public site's origins.
api.WithEnvironment("Cors__AllowedOrigins__0", publicSite.GetEndpoint("https"))
   .WithEnvironment("Cors__AllowedOrigins__1", publicSite.GetEndpoint("http"));

// ---- Admin site: Blazor Server, shared by all municipalities, served under /admin -------
builder.AddProject<Projects.MEMS_Admin>("admin")
    .WithReference(database)
    .WithReference(api)          // service discovery: links to the API documentation
    .WithReference(publicSite)   // service discovery: "view public site" links per tenant
    .WaitFor(api)                // the schema and seed data exist once the API is healthy
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("https", url =>
    {
        url.Url += "/admin";
        url.DisplayText = "Admin site (/admin)";
    });

builder.Build().Run();
