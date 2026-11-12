using MEMS.Data.Services;
using MEMS.Data.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MEMS.Data;

public static class DataServiceExtensions
{
    /// <summary>Name of the SQL Server database resource declared in the Aspire AppHost.</summary>
    public const string ConnectionName = "memsdb";

    /// <summary>
    /// Registers the tenant-aware DbContext and the service layer. The connection string is
    /// supplied by .NET Aspire (ConnectionStrings:memsdb); there is deliberately no fallback.
    /// The host must also register an <see cref="ITenantContextInitializer"/>.
    /// </summary>
    public static IHostApplicationBuilder AddMemsData(this IHostApplicationBuilder builder)
    {
        string connectionString = builder.Configuration.GetConnectionString(ConnectionName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionName}' was not found. Start the solution through MEMS.AppHost so .NET Aspire can supply it.");

        // Scoped factory: each context it creates is bound to the scope's ITenantContext,
        // which is what the global query filters read. (Pooling is off for the same reason.)
        builder.Services.AddDbContextFactory<ApplicationDbContext>(
            options => options.UseSqlServer(connectionString), ServiceLifetime.Scoped);

        // Aspire integration: health check, connection retries, tracing and metrics.
        builder.EnrichSqlServerDbContext<ApplicationDbContext>();

        builder.Services.AddScoped<TenantContext>();
        builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        builder.Services.AddScoped<ITenantDirectory, TenantDirectory>();
        builder.Services.AddScoped<IMunicipalityService, MunicipalityService>();
        builder.Services.AddScoped<IPositionService, PositionService>();
        builder.Services.AddScoped<ICandidateService, CandidateService>();
        builder.Services.AddScoped<IImageService, ImageService>();
        builder.Services.AddScoped<IAdminUserService, AdminUserService>();

        return builder;
    }
}

/// <summary>Lets <c>dotnet ef migrations add</c> build the model without a running database.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(design-time);Database=mems")
            .Options;

        return new ApplicationDbContext(options, new TenantContext());
    }
}
