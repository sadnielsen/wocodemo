using System.Windows;
using WoCo.Core.Services;
using WoCo.Wpf.ViewModels;
using WoCo.Wpf.Views;

namespace WoCo.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ICreateRevisionService _createRevisionService;

    public MainWindow(MainViewModel viewModel, ICreateRevisionService createRevisionService)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _createRevisionService = createRevisionService;
        DataContext = _viewModel;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private async void AddRevision_Click(object sender, RoutedEventArgs e)
    {
        var currentProject = _viewModel.CurrentProject;

        if (currentProject == null)
        {
            MessageBox.Show("Please select a project or revision first.", "No Project Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new AddRevisionWindow(currentProject)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true && dialog.Request != null)
        {
            try
            {
                // Use service to create revision with annotation transformation
                var updatedProject = await _createRevisionService.CreateAsync(dialog.Request);

                // Reload the project view model with the updated data
                currentProject.Revisions.Clear();
                foreach (var revision in updatedProject.FloorplanRevisions.OrderBy(r => r.RevisionNumber))
                {
                    var revisionViewModel = new RevisionViewModel(revision, currentProject);
                    currentProject.Revisions.Add(revisionViewModel);
                }

                var newRevisionNumber = updatedProject.FloorplanRevisions.Max(r => r.RevisionNumber);
                MessageBox.Show($"Revision {newRevisionNumber} added successfully with transformed annotations.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating revision: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}