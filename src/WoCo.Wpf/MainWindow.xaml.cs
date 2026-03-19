using System.Windows;
using WoCo.Core.Services;
using WoCo.Wpf.Controls;
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
    private readonly ICreateProjectService _createProjectService;
    private readonly IAnnotationService _annotationService;

    public MainWindow(MainViewModel viewModel, ICreateRevisionService createRevisionService, ICreateProjectService createProjectService, IAnnotationService annotationService)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _createRevisionService = createRevisionService;
        _createProjectService = createProjectService;
        _annotationService = annotationService;
        DataContext = _viewModel;

        Loaded += OnLoaded;

        // Wire up annotation coordinate change event
        FloorplanViewer.AnnotationCoordinatesChanged += OnAnnotationCoordinatesChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private async void SaveChanges_Click(object sender, RoutedEventArgs e)
    {

    }

    private async void NewProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CreateProjectWindow
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true && dialog.Request != null)
        {
            try
            {
                // Use service to create project
                var newProject = await _createProjectService.CreateAsync(dialog.Request);

                // Reload the view model to include the new project
                await _viewModel.InitializeAsync();

                MessageBox.Show($"Project '{newProject.Name}' created successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
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

    private async void OnAnnotationCoordinatesChanged(object? sender, AnnotationCoordinatesChangedEventArgs e)
    {
        try
        {
            // Persist to database
            await _annotationService.UpdateAnnotationCoordinatesAsync(
                e.AnnotationId,
                e.NewNormalizedCoordinates);

            // Update the ViewModel to reflect the change in memory
            e.Annotation.UpdateCoordinates(e.NewNormalizedCoordinates);

            System.Diagnostics.Debug.WriteLine($"Successfully persisted and updated ViewModel for annotation {e.AnnotationId}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving annotation coordinates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}