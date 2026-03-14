using System;
using System.IO;
using System.Windows;
using FloorplanAnnotator.Services;
using FloorplanAnnotator.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FloorplanAnnotator;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        ConfigureServices(services);

        Services = services.BuildServiceProvider();

        var mainWindow = new MainWindow
        {
            DataContext = Services.GetRequiredService<MainViewModel>()
        };

        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FloorplanAnnotator");

        Directory.CreateDirectory(appDataFolder);

        var dbPath = Path.Combine(appDataFolder, "floorplans.db");

        services.AddSingleton<IProjectRepository>(_ => new ProjectRepository(dbPath));

        services.AddTransient<CreateProjectService>();
        services.AddTransient<CreateProjectViewModel>();

        services.AddSingleton<IViewModelFactory, ViewModelFactory>();

        services.AddSingleton<MainViewModel>();
    }

    private void App_DispatcherUnhandledException(object sender,
    System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            e.Exception.ToString(),
            "Unhandled exception",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }


}
