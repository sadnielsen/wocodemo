using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WoCo.Wpf.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private RevisionViewModel? _currentRevision;

    public MainViewModel(ProjectTreeViewModel projectTreeViewModel)
    {
        ProjectTreeViewModel = projectTreeViewModel;

        // Subscribe to selection changes
        ProjectTreeViewModel.PropertyChanged += OnProjectTreeSelectionChanged;
    }

    public ProjectTreeViewModel ProjectTreeViewModel { get; }

    public object? SelectedProjectItem => ProjectTreeViewModel.SelectedProjectItem;

    public RevisionViewModel? CurrentRevision
    {
        get => _currentRevision;
        set
        {
            if (_currentRevision != value)
            {
                _currentRevision = value;
                OnPropertyChanged();
            }
        }
    }

    public async Task InitializeAsync()
    {
        await ProjectTreeViewModel.LoadProjectsAsync();
    }

    private void OnProjectTreeSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProjectTreeViewModel.SelectedProjectItem))
        {
            OnPropertyChanged(nameof(SelectedProjectItem));

            // Update CurrentRevision - let FloorplanViewer handle loading image and annotations
            if (SelectedProjectItem is RevisionViewModel revision)
            {
                CurrentRevision = revision;
            }
            else
            {
                CurrentRevision = null;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
