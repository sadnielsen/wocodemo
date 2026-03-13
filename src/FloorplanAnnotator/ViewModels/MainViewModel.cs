using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FloorplanAnnotator.Commands;
using FloorplanAnnotator.Models;
using FloorplanAnnotator.Resources;
using FloorplanAnnotator.Services;
using FloorplanAnnotator.Views;

namespace FloorplanAnnotator.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IProjectRepository _repository;
        private Project? _selectedProject;
        private bool _isLoading;
        private string _statusMessage = Strings.Status_Ready;

        public ObservableCollection<Project> Projects { get; } = new();

        public Project? SelectedProject
        {
            get => _selectedProject;
            set
            {
                SetProperty(ref _selectedProject, value);
                OnPropertyChanged(nameof(HasSelectedProject));
                OnPropertyChanged(nameof(SelectedProjectAnnotations));
            }
        }

        public bool HasSelectedProject => _selectedProject != null;

        public ObservableCollection<Annotation> SelectedProjectAnnotations =>
            _selectedProject != null
                ? new ObservableCollection<Annotation>(_selectedProject.Annotations)
                : new ObservableCollection<Annotation>();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand NewProjectCommand { get; }
        public ICommand DeleteProjectCommand { get; }
        public ICommand SelectProjectCommand { get; }

        public MainViewModel(IProjectRepository repository)
        {
            _repository = repository;
            NewProjectCommand = new RelayCommand((Action)OpenNewProjectDialog);
            DeleteProjectCommand = new RelayCommand((Action<object?>)DeleteSelectedProject, parameter => HasSelectedProject);
            SelectProjectCommand = new RelayCommand<Project>(SelectProject);

            _ = LoadProjectsAsync();
        }

        private void DeleteSelectedProject(object? parameter) => DeleteSelectedProject();

        private async Task LoadProjectsAsync()
        {
            IsLoading = true;
            StatusMessage = Strings.Status_LoadingProjects;
            try
            {
                var projects = await _repository.GetAllProjectsAsync();
                Projects.Clear();
                foreach (var p in projects)
                    Projects.Add(p);

                StatusMessage = string.Format(Strings.Status_ProjectsLoaded, Projects.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(Strings.Status_ErrorLoading, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OpenNewProjectDialog()
        {
            var viewModel = new CreateProjectViewModel();
            var dialog = new CreateProjectView(viewModel);
            viewModel.RequestClose += (_, _) => dialog.Close();

            dialog.ShowDialog();

            if (viewModel.DialogResult && viewModel.CreatedProject != null)
            {
                _ = SaveNewProjectAsync(viewModel.CreatedProject);
            }
        }

        private async Task SaveNewProjectAsync(Project project)
        {
            IsLoading = true;
            StatusMessage = Strings.Status_SavingProject;
            try
            {
                var saved = await _repository.AddProjectAsync(project);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Projects.Insert(0, saved);
                    SelectedProject = saved;
                    StatusMessage = string.Format(Strings.Status_ProjectCreated, saved.Name);
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = string.Format(Strings.Status_ErrorSaving, ex.Message);
                });
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => IsLoading = false);
            }
        }

        private void DeleteSelectedProject()
        {
            if (_selectedProject == null) return;

            var result = MessageBox.Show(
                string.Format(Strings.DeleteProject_Confirm, _selectedProject.Name),
                Strings.DeleteProject_Title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _ = DeleteProjectAsync(_selectedProject.Id);
            }
        }

        private async Task DeleteProjectAsync(int id)
        {
            IsLoading = true;
            StatusMessage = Strings.Status_DeletingProject;
            try
            {
                await _repository.DeleteProjectAsync(id);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var toRemove = Projects.FirstOrDefault(p => p.Id == id);
                    if (toRemove != null)
                        Projects.Remove(toRemove);

                    if (SelectedProject?.Id == id)
                        SelectedProject = null;

                    StatusMessage = Strings.Status_ProjectDeleted;
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = string.Format(Strings.Status_ErrorDeleting, ex.Message);
                });
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => IsLoading = false);
            }
        }

        private void SelectProject(Project? project)
        {
            SelectedProject = project;
        }
    }

    // Generic RelayCommand for typed parameters
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) =>
            _canExecute?.Invoke(parameter is T t ? t : default) ?? true;

        public void Execute(object? parameter) =>
            _execute(parameter is T t ? t : default);
    }
}
