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
    public const string ConnectionName = "memsdb";

    public static IHostApplicationBuilder AddMemsData(this IHostApplicationBuilder builder)
    {
        string connectionString = builder.Configuration.GetConnectionString(ConnectionName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionName}' was not found. Start the solution through MEMS.AppHost so .NET Aspire can supply it.");

        builder.Services.AddDbContextFactory<ApplicationDbContext>(
            options => options.UseSqlServer(connectionString), ServiceLifetime.Scoped);

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
