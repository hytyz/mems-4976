using System.Text;
using System.Threading.RateLimiting;
using MEMS.Api.Controllers;
using MEMS.Api.Infrastructure;
using MEMS.Api.Tenancy;
using MEMS.Data;
using MEMS.Data.Entities;
using MEMS.Data.Identity;
using MEMS.Data.Seeding;
using MEMS.Data.Tenancy;
using MEMS.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// .NET Aspire: OpenTelemetry logs/traces/metrics, health checks, service discovery, resilience.
builder.AddServiceDefaults();

// SQL Server through the Aspire-managed connection string, plus the shared service layer.
builder.AddMemsData();

// The API sets the tenant boundary per request in TenantResolutionMiddleware.
builder.Services.AddScoped<ITenantContextInitializer, NoOpTenantContextInitializer>();
builder.Services.AddScoped<CurrentTenant>();
builder.Services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<CurrentTenant>());

// ---- Identity (accounts and roles only; the API issues tokens, not cookies) ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddIdentityCore<ApplicationUser>(IdentityDefaults.Configure)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddClaimsPrincipalFactory<TenantClaimsPrincipalFactory>();

// ---- Authentication: JWT bearer tokens ----
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
if (jwt.SigningKey.Length < JwtSettings.MinKeyLength)
{
    throw new InvalidOperationException(
        $"Jwt:SigningKey must be at least {JwtSettings.MinKeyLength} characters. It is supplied by the Aspire AppHost parameter 'jwt-signing-key'.");
}

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

// ---- Authorization: role + tenant ----
builder.Services.AddScoped<IAuthorizationHandler, TenantAdminHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(ApiPolicies.SuperAdmin, policy => policy.RequireRole(AppRoles.SuperAdmin))
    .AddPolicy(ApiPolicies.AnyAdmin, policy => policy.RequireRole(AppRoles.SuperAdmin, AppRoles.MunicipalityAdmin))
    .AddPolicy(ApiPolicies.TenantAdmin, policy => policy
        .RequireRole(AppRoles.SuperAdmin, AppRoles.MunicipalityAdmin)
        .AddRequirements(new TenantAdminRequirement()));

// ---- Errors: RFC 7807 ProblemDetails everywhere, nothing sensitive in the body ----
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();

// Sign-in throttling on top of Identity's account lockout.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AuthController.LoginRateLimit, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

// The Blazor WebAssembly site runs in the browser on another origin. Its address is injected
// by the AppHost (Cors:AllowedOrigins); nothing is allowed by default.
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins.Select(origin => origin.TrimEnd('/')).ToArray())
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<ApiDocumentTransformer>());

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(); // 401/403/404/405 without a body become ProblemDetails too.

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "MEMS API v1");
    options.DocumentTitle = "MEMS API";
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>(); // after authentication, before authorization
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// The API owns the schema: it applies migrations and seeds before it starts serving,
// and the AppHost makes the Admin app wait for it.
string seedPassword = builder.Configuration["Seed:AdminPassword"]
    ?? throw new InvalidOperationException(
        "Seed:AdminPassword is not configured. It is supplied by the Aspire AppHost parameter 'seed-admin-password'.");
await DbInitializer.MigrateAndSeedAsync(app.Services, seedPassword);

app.Run();
