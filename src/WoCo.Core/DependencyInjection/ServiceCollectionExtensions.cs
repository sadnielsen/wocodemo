using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WoCo.Core.DataAccess;
using WoCo.Core.Services;

namespace WoCo.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureWoCoServices(this IServiceCollection services)
    {
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WoCo");

        Directory.CreateDirectory(appDataFolder);

        var dbPath = Path.Combine(appDataFolder, "woco.db");

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped<IProjectRepository, ProjectRepository>();

        services.AddTransient<CreateProjectService>();

        return services;
    }
}
