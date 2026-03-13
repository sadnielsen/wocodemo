using System;
using System.IO;
using System.Windows;
using FloorplanAnnotator.Converters;
using FloorplanAnnotator.Services;
using FloorplanAnnotator.ViewModels;

namespace FloorplanAnnotator;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        Resources["BoolToVisConverter"] = new BoolToVisibilityConverter();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FloorplanAnnotator",
            "projects.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var repository = new ProjectRepository(dbPath);
        var viewModel = new MainViewModel(repository);

        var mainWindow = new MainWindow(viewModel);
        mainWindow.Show();
    }
}

