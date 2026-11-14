using MEMS.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithDataVolume("mems-sql-data");

var database = sql.AddDatabase("memsdb");

var seedPassword = builder.AddParameter("seed-admin-password", secret: true);

var jwtSigningKey = builder.AddParameter(
    "jwt-signing-key",
    new GenerateParameterDefault { MinLength = 64, Special = false },
    secret: true,
    persist: true);

var api = builder.AddProject<Projects.MEMS_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("Seed__AdminPassword", seedPassword)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("https", url => url.DisplayText = "API (Swagger)");

var publicSite = builder.AddProject<Projects.MEMS_Public>("public")
    .WithBrowserServiceReference(api)
    .WaitFor(api)
    .WithUrlForEndpoint("https", url => url.DisplayText = "Public site");

api.WithEnvironment("Cors__AllowedOrigins__0", publicSite.GetEndpoint("https"))
   .WithEnvironment("Cors__AllowedOrigins__1", publicSite.GetEndpoint("http"));

builder.AddProject<Projects.MEMS_Admin>("admin")
    .WithReference(database)
    .WithReference(api)
    .WithReference(publicSite)
    .WaitFor(api)
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("https", url =>
    {
        url.Url += "/admin";
        url.DisplayText = "Admin site (/admin)";
    });

builder.Build().Run();
