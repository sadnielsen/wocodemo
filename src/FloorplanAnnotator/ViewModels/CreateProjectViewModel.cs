using System.Windows.Input;
using FloorplanAnnotator.Commands;
using FloorplanAnnotator.Models;
using FloorplanAnnotator.Resources;
using FloorplanAnnotator.Services;
using Microsoft.Win32;

namespace FloorplanAnnotator.ViewModels;

public class CreateProjectViewModel : BaseViewModel
{
    private readonly CreateProjectService _createProjectService;

    private string _projectName = string.Empty;
    private string _floorplanPath = string.Empty;
    private string _annotationsPath = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;

    public string ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value);
    }

    public string FloorplanPath
    {
        get => _floorplanPath;
        set => SetProperty(ref _floorplanPath, value);
    }

    public string AnnotationsPath
    {
        get => _annotationsPath;
        set => SetProperty(ref _annotationsPath, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            SetProperty(ref _errorMessage, value);
            HasError = !string.IsNullOrWhiteSpace(value);
        }
    }

    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    public ICommand BrowseFloorplanCommand { get; }
    public ICommand BrowseAnnotationsCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand CancelCommand { get; }

    public bool DialogResult { get; private set; }
    public Project? CreatedProject { get; private set; }

    public event EventHandler? RequestClose;

    public CreateProjectViewModel(CreateProjectService createProjectService)
    {
        _createProjectService = createProjectService;

        BrowseFloorplanCommand = new RelayCommand((Action)BrowseFloorplan);
        BrowseAnnotationsCommand = new RelayCommand((Action)BrowseAnnotations);
        CreateCommand = new RelayCommand(async () => await CreateAsync(), () => CanCreate());
        CancelCommand = new RelayCommand((Action)Cancel);
    }

    private void BrowseFloorplan()
    {
        var dialog = new OpenFileDialog
        {
            Title = Strings.BrowseFloorplan_DialogTitle,
            Filter = Strings.BrowseFloorplan_Filter,
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            FloorplanPath = dialog.FileName;
            ErrorMessage = string.Empty;
        }
    }

    private void BrowseAnnotations()
    {
        var dialog = new OpenFileDialog
        {
            Title = Strings.BrowseAnnotations_DialogTitle,
            Filter = Strings.BrowseAnnotations_Filter,
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            AnnotationsPath = dialog.FileName;
            ErrorMessage = string.Empty;
        }
    }

    private bool CanCreate()
    {
        return !string.IsNullOrWhiteSpace(ProjectName)
            && !string.IsNullOrWhiteSpace(FloorplanPath)
            && !string.IsNullOrWhiteSpace(AnnotationsPath);
    }

    private async Task CreateAsync()
    {
        ErrorMessage = string.Empty;

        try
        {
            var request = new CreateProjectRequest
            {
                ProjectName = ProjectName,
                FloorplanPath = FloorplanPath,
                AnnotationsPath = AnnotationsPath
            };

            CreatedProject = await _createProjectService.CreateAsync(request);

            DialogResult = true;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            var message = ex.ToString();

            if (ex.InnerException != null)
            {
                message += Environment.NewLine + Environment.NewLine;
                message += "INNER EXCEPTION:" + Environment.NewLine;
                message += ex.InnerException.ToString();
            }

            ErrorMessage = message;
        }
    }

    private void Cancel()
    {
        DialogResult = false;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}