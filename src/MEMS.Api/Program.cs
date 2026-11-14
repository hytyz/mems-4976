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

builder.AddServiceDefaults();

builder.AddMemsData();

builder.Services.AddScoped<ITenantContextInitializer, NoOpTenantContextInitializer>();
builder.Services.AddScoped<CurrentTenant>();
builder.Services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<CurrentTenant>());

builder.Services.AddHttpContextAccessor();
builder.Services.AddIdentityCore<ApplicationUser>(IdentityDefaults.Configure)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddClaimsPrincipalFactory<TenantClaimsPrincipalFactory>();

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

builder.Services.AddScoped<IAuthorizationHandler, TenantAdminHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(ApiPolicies.SuperAdmin, policy => policy.RequireRole(AppRoles.SuperAdmin))
    .AddPolicy(ApiPolicies.AnyAdmin, policy => policy.RequireRole(AppRoles.SuperAdmin, AppRoles.MunicipalityAdmin))
    .AddPolicy(ApiPolicies.TenantAdmin, policy => policy
        .RequireRole(AppRoles.SuperAdmin, AppRoles.MunicipalityAdmin)
        .AddRequirements(new TenantAdminRequirement()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AuthController.LoginRateLimit, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins.Select(origin => origin.TrimEnd('/')).ToArray())
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<ApiDocumentTransformer>());

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

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
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

string seedPassword = builder.Configuration["Seed:AdminPassword"]
    ?? throw new InvalidOperationException(
        "Seed:AdminPassword is not configured. It is supplied by the Aspire AppHost parameter 'seed-admin-password'.");
await DbInitializer.MigrateAndSeedAsync(app.Services, seedPassword);

app.Run();
