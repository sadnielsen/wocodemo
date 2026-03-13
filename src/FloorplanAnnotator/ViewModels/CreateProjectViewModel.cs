using System;
using System.IO;
using System.Windows.Input;
using FloorplanAnnotator.Commands;
using FloorplanAnnotator.Models;
using FloorplanAnnotator.Services;
using Microsoft.Win32;

namespace FloorplanAnnotator.ViewModels
{
    public class CreateProjectViewModel : BaseViewModel
    {
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
                HasError = !string.IsNullOrEmpty(value);
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

        public CreateProjectViewModel()
        {
            BrowseFloorplanCommand = new RelayCommand((Action)BrowseFloorplan);
            BrowseAnnotationsCommand = new RelayCommand((Action)BrowseAnnotations);
            CreateCommand = new RelayCommand((Action)Create, (Func<bool>)(() => CanCreate(null)));
            CancelCommand = new RelayCommand((Action)Cancel);
        }

        private void BrowseFloorplan()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Floorplan Image",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.tiff;*.gif|All Files|*.*",
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
                Title = "Select Annotations File",
                Filter = "JSON Files|*.json|All Files|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                AnnotationsPath = dialog.FileName;
                ErrorMessage = string.Empty;
            }
        }

        private bool CanCreate(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(ProjectName)
                && !string.IsNullOrWhiteSpace(FloorplanPath)
                && !string.IsNullOrWhiteSpace(AnnotationsPath);
        }

        private void Create()
        {
            ErrorMessage = string.Empty;

            if (!File.Exists(FloorplanPath))
            {
                ErrorMessage = "Floorplan file does not exist.";
                return;
            }

            if (!File.Exists(AnnotationsPath))
            {
                ErrorMessage = "Annotations file does not exist.";
                return;
            }

            var annotations = AnnotationParser.ParseFromFile(AnnotationsPath);

            CreatedProject = new Project
            {
                Name = ProjectName.Trim(),
                FloorplanPath = FloorplanPath,
                AnnotationsPath = AnnotationsPath,
                CreatedAt = DateTime.UtcNow,
                Annotations = annotations
            };

            DialogResult = true;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void Cancel()
        {
            DialogResult = false;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
