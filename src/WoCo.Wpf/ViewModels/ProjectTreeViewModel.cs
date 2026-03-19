using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WoCo.Core.DataAccess;

namespace WoCo.Wpf.ViewModels;

public class ProjectTreeViewModel : INotifyPropertyChanged
{
    private readonly IProjectRepository _projectRepository;
    private object? _selectedItem;
    private bool _isLoading;

    public ProjectTreeViewModel(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public ObservableCollection<ProjectViewModel> Projects { get; } = new();

    public object? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != value)
            {
                _selectedItem = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedProjectItem));
            }
        }
    }

    public object? SelectedProjectItem => SelectedItem;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    public async Task LoadProjectsAsync()
    {
        IsLoading = true;
        try
        {
            Projects.Clear();
            var projects = await _projectRepository.GetAllProjectsAsync();
            
            foreach (var project in projects)
            {
                Projects.Add(new ProjectViewModel(project));
            }
        }
        catch (Exception ex)
        {
            // TODO: Add proper error handling/logging
            System.Diagnostics.Debug.WriteLine($"Error loading projects: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
