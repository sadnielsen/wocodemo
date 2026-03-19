using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using WoCo.Core.Services;

namespace WoCo.Wpf.Views;

public partial class CreateProjectWindow : Window
{
    private string? _selectedFloorplanPath;
    private string? _selectedAnnotationsPath;
    private double _imageWidth;
    private double _imageHeight;
    private BitmapImage? _previewBitmap;

    public CreateProjectWindow()
    {
        InitializeComponent();
    }

    public CreateProjectRequest? Request { get; private set; }

    private void ProjectNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        ValidateInputs();
    }

    private void BrowseFloorplanButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select Floorplan Image",
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _selectedFloorplanPath = openFileDialog.FileName;
            FloorplanFilePathTextBox.Text = _selectedFloorplanPath;

            LoadPreview();
            ValidateInputs();
        }
    }

    private void BrowseAnnotationsButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select Annotations File",
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _selectedAnnotationsPath = openFileDialog.FileName;
            AnnotationsFilePathTextBox.Text = _selectedAnnotationsPath;

            ValidateInputs();
        }
    }

    private void LoadPreview()
    {
        if (string.IsNullOrEmpty(_selectedFloorplanPath))
            return;

        try
        {
            var fileInfo = new FileInfo(_selectedFloorplanPath);
            var fileSize = fileInfo.Length / 1024.0; // KB

            // Load image for preview
            _previewBitmap = new BitmapImage();
            _previewBitmap.BeginInit();
            _previewBitmap.UriSource = new Uri(_selectedFloorplanPath, UriKind.Absolute);
            _previewBitmap.CacheOption = BitmapCacheOption.OnLoad;
            _previewBitmap.EndInit();

            _imageWidth = _previewBitmap.PixelWidth;
            _imageHeight = _previewBitmap.PixelHeight;

            // Display file info
            FileInfoTextBlock.Text = $"File: {fileInfo.Name}\nSize: {fileSize:F2} KB\nDimensions: {_imageWidth} x {_imageHeight} pixels";

            // Show preview
            PreviewImage.Source = _previewBitmap;
            PreviewPlaceholder.Visibility = Visibility.Collapsed;
            PreviewCanvas.Visibility = Visibility.Visible;

            PreviewInfoTextBlock.Text = "Floorplan loaded successfully.";
        }
        catch (Exception ex)
        {
            FileInfoTextBlock.Text = $"Error reading file: {ex.Message}";
            PreviewImage.Source = null;
            PreviewPlaceholder.Visibility = Visibility.Visible;
            PreviewCanvas.Visibility = Visibility.Collapsed;
        }
    }

    private void ValidateInputs()
    {
        var isProjectNameValid = !string.IsNullOrWhiteSpace(ProjectNameTextBox.Text);
        var isFloorplanSelected = !string.IsNullOrEmpty(_selectedFloorplanPath);
        var isAnnotationsSelected = !string.IsNullOrEmpty(_selectedAnnotationsPath);

        OkButton.IsEnabled = isProjectNameValid && isFloorplanSelected && isAnnotationsSelected;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProjectNameTextBox.Text))
        {
            MessageBox.Show("Please enter a project name.", "No Project Name", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(_selectedFloorplanPath))
        {
            MessageBox.Show("Please select a floorplan file.", "No Floorplan File Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(_selectedAnnotationsPath))
        {
            MessageBox.Show("Please select an annotations file.", "No Annotations File Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Create request
            Request = new CreateProjectRequest
            {
                ProjectName = ProjectNameTextBox.Text.Trim(),
                FloorplanPath = _selectedFloorplanPath,
                AnnotationsPath = _selectedAnnotationsPath,
                Width = _imageWidth,
                Height = _imageHeight
            };

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating project request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
