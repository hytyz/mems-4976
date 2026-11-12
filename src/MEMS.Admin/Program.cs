using MEMS.Admin.Components;
using MEMS.Admin.Infrastructure;
using MEMS.Data;
using MEMS.Data.Entities;
using MEMS.Data.Identity;
using MEMS.Data.Tenancy;
using MEMS.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// .NET Aspire: OpenTelemetry logs/traces/metrics, health checks, service discovery, resilience.
builder.AddServiceDefaults();

// SQL Server through the Aspire-managed connection string, plus the shared service layer.
builder.AddMemsData();
builder.Services.AddScoped<ITenantContextInitializer, CircuitTenantInitializer>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = false);

// ---- ASP.NET Core Identity with cookies ----
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "MEMS.Admin";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/access-denied";
});

// The account is re-checked on every request, so a deleted account, a reset password or a
// deleted municipality ends the session immediately (one small query per page load).
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);

builder.Services.AddIdentityCore<ApplicationUser>(IdentityDefaults.Configure)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<TenantClaimsPrincipalFactory>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AdminPolicies.AnyAdmin, policy => policy.RequireRole(AppRoles.SuperAdmin, AppRoles.MunicipalityAdmin))
    .AddPolicy(AdminPolicies.SuperAdmin, policy => policy.RequireRole(AppRoles.SuperAdmin));

builder.Services.AddScoped<FlashService>();
builder.Services.AddSingleton<ServiceLinks>();

var app = builder.Build();

// The admin site lives under /admin (https://localhost:port/admin). The bare root redirects there.
app.Use((context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/admin/");
        return Task.CompletedTask;
    }

    return next(context);
});
app.UsePathBase("/admin");

// Friendly error page in every environment: exception details go to the logs, never to the browser.
app.UseExceptionHandler("/error", createScopeForErrors: true);
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Routing must run after UsePathBase so endpoints are matched without the /admin prefix.
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Sign-out is a POST protected by an antiforgery token (the [FromForm] parameter turns validation on).
app.MapPost("/account/logout", async (SignInManager<ApplicationUser> signInManager, ILogger<Program> logger,
        HttpContext context, [FromForm] string? returnUrl) =>
    {
        logger.LogInformation("Administrator {UserName} signed out", context.User.Identity?.Name);
        await signInManager.SignOutAsync();
        return TypedResults.LocalRedirect("~/account/login");
    })
    .RequireAuthorization();

// Logos and photographs for the admin pages, read through the same tenant boundary:
// a Municipality Administrator cannot load another municipality's images by guessing ids.
app.MapGet("/images/{id:guid}", async (Guid id, HttpContext context, TenantContext tenant,
        IDbContextFactory<ApplicationDbContext> dbFactory, CancellationToken cancellationToken) =>
    {
        tenant.SetFromUser(context.User);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var image = await db.Images.AsNoTracking()
            .Where(i => i.StoredImageId == id)
            .Select(i => new { i.ContentType, i.Data })
            .FirstOrDefaultAsync(cancellationToken);

        if (image is null)
        {
            return Results.NotFound();
        }

        context.Response.Headers.CacheControl = "private, max-age=86400, immutable";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        return Results.File(image.Data, image.ContentType);
    })
    .RequireAuthorization(AdminPolicies.AnyAdmin);

app.MapDefaultEndpoints();

app.Run();
