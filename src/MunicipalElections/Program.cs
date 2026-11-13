using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MunicipalElections.Data;
using MunicipalElections.Models;
using MunicipalElections.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor();

// The connection string must come from configuration (appsettings.json, an
// environment-specific appsettings file, or an environment variable). There is
// deliberately no hard-coded fallback.
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found. Define ConnectionStrings:DefaultConnection in configuration.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

// AddDefaultUI is intentionally not used: only the Login, Logout and AccessDenied
// pages exist. There is no self-registration; accounts are created by a SuperAdmin.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.User.RequireUniqueEmail = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.LogoutPath = "/Identity/Account/Logout";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.AdminArea, policy =>
        policy.RequireAuthenticatedUser().AddRequirements(new AdminAreaRequirement()));
});
builder.Services.AddScoped<IAuthorizationHandler, AdminAreaHandler>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Uploaded images are stored outside wwwroot so redeploying never deletes them.
var uploadStorage = new UploadStorage(UploadStorage.ResolveRootPath(builder.Configuration, builder.Environment));
builder.Services.AddSingleton(uploadStorage);

builder.Services.AddScoped<ImageFileService>();
builder.Services.AddScoped<MunicipalityAccessService>();
builder.Services.AddScoped<VotingGuideService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Serve uploaded images from the external upload folder under /uploads.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadStorage.RootPath),
    RequestPath = UploadStorage.RequestPath,
    OnPrepareResponse = context =>
        context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff"
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapStaticAssets();

app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

await DbInitializer.SeedAsync(app.Services);

app.Run();
