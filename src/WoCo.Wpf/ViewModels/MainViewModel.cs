using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WoCo.Wpf.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private ImageSource? _currentFloorplanImage;

    public MainViewModel(ProjectTreeViewModel projectTreeViewModel)
    {
        ProjectTreeViewModel = projectTreeViewModel;
        
        // Subscribe to selection changes
        ProjectTreeViewModel.PropertyChanged += OnProjectTreeSelectionChanged;
    }

    public ProjectTreeViewModel ProjectTreeViewModel { get; }

    public object? SelectedProjectItem => ProjectTreeViewModel.SelectedProjectItem;

    public ImageSource? CurrentFloorplanImage
    {
        get => _currentFloorplanImage;
        set
        {
            if (_currentFloorplanImage != value)
            {
                _currentFloorplanImage = value;
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
            
            // Load floorplan if a revision is selected
            if (SelectedProjectItem is RevisionViewModel revision)
            {
                LoadFloorplanImage(revision);
            }
            else
            {
                CurrentFloorplanImage = null;
            }
        }
    }

    private void LoadFloorplanImage(RevisionViewModel revision)
    {
        try
        {
            if (revision.FileContent.Length > 0)
            {
                var image = new BitmapImage();
                using (var stream = new MemoryStream(revision.FileContent))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze(); // Important for cross-thread access
                }
                CurrentFloorplanImage = image;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading floorplan image: {ex.Message}");
            CurrentFloorplanImage = null;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
