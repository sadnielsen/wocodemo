using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using WoCo.Core.DataAccess;
using WoCo.Core.DependencyInjection;
using WoCo.Wpf.ViewModels;

namespace WoCo.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Services
                services.ConfigureWoCoServices();

                // ViewModels
                services.AddTransient<ProjectTreeViewModel>();
                services.AddTransient<MainViewModel>();

                // Views
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        // Apply migrations on startup
        using (var scope = _host.Services.CreateScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = factory.CreateDbContext();
            db.Database.Migrate();
        }

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}

