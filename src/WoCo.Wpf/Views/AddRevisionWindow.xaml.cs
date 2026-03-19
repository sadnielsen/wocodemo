using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using WoCo.Core.Services;
using WoCo.Wpf.ViewModels;

namespace WoCo.Wpf.Views;

public partial class AddRevisionWindow : Window
{
    private readonly ProjectViewModel _project;
    private string? _selectedFilePath;
    private double _imageWidth;
    private double _imageHeight;
    private BitmapImage? _previewBitmap;

    public AddRevisionWindow(ProjectViewModel project)
    {
        InitializeComponent();
        _project = project;

        ProjectNameTextBlock.Text = project.Name;
        RevisionNumberTextBlock.Text = $"Revision {project.Revisions.Count + 1}";
    }

    public CreateRevisionRequest? Request { get; private set; }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select Floorplan Image",
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _selectedFilePath = openFileDialog.FileName;
            FilePathTextBox.Text = _selectedFilePath;

            LoadPreview();
        }
    }

    private void LoadPreview()
    {
        if (string.IsNullOrEmpty(_selectedFilePath))
            return;

        try
        {
            var fileInfo = new FileInfo(_selectedFilePath);
            var fileSize = fileInfo.Length / 1024.0; // KB

            // Load image for preview
            _previewBitmap = new BitmapImage();
            _previewBitmap.BeginInit();
            _previewBitmap.UriSource = new Uri(_selectedFilePath, UriKind.Absolute);
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

            // Only show transformation parameters if there are existing revisions
            if (_project.Revisions.Count > 0)
            {
                TransformationPanel.Visibility = Visibility.Visible;
                PreviewInfoTextBlock.Text = "Click on the image to set the offset point.";
            }
            else
            {
                TransformationPanel.Visibility = Visibility.Collapsed;
                PreviewInfoTextBlock.Text = "This is the initial revision - no transformation needed.";
            }

            OkButton.IsEnabled = true;

            // Reset offset marker
            OffsetMarker.Visibility = Visibility.Collapsed;
            CrosshairH.Visibility = Visibility.Collapsed;
            CrosshairV.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            FileInfoTextBlock.Text = $"Error reading file: {ex.Message}";
            PreviewImage.Source = null;
            PreviewPlaceholder.Visibility = Visibility.Visible;
            PreviewCanvas.Visibility = Visibility.Collapsed;
            TransformationPanel.Visibility = Visibility.Collapsed;
            OkButton.IsEnabled = false;
        }
    }

    private void PreviewImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_previewBitmap == null)
            return;

        // Get click position relative to the image
        var position = e.GetPosition(PreviewImage);

        // Update offset textboxes
        OffsetXTextBox.Text = Math.Round(position.X).ToString();
        OffsetYTextBox.Text = Math.Round(position.Y).ToString();

        // Update visual marker
        UpdateOffsetMarker(position.X, position.Y);

        // Update info text
        PreviewInfoTextBlock.Text = $"Offset point set to: ({Math.Round(position.X)}, {Math.Round(position.Y)})";
    }

    private void UpdateOffsetMarker(double x, double y)
    {
        if (_previewBitmap == null)
            return;

        // Position marker (centered on the point)
        Canvas.SetLeft(OffsetMarker, x - 6);
        Canvas.SetTop(OffsetMarker, y - 6);
        OffsetMarker.Visibility = Visibility.Visible;

        // Position crosshairs
        CrosshairH.X1 = 0;
        CrosshairH.Y1 = y;
        CrosshairH.X2 = _imageWidth;
        CrosshairH.Y2 = y;
        CrosshairH.Visibility = Visibility.Visible;

        CrosshairV.X1 = x;
        CrosshairV.Y1 = 0;
        CrosshairV.X2 = x;
        CrosshairV.Y2 = _imageHeight;
        CrosshairV.Visibility = Visibility.Visible;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedFilePath))
        {
            MessageBox.Show("Please select a floorplan file.", "No File Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Parse scale denominator
            if (!double.TryParse(ScaleDenominatorTextBox.Text, out var scaleDenominator) || scaleDenominator <= 0)
            {
                MessageBox.Show("Invalid Scale Denominator value. Please enter a valid positive number.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Parse offset values
            if (!double.TryParse(OffsetXTextBox.Text, out var offsetX))
            {
                MessageBox.Show("Invalid Offset X value. Please enter a valid number.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(OffsetYTextBox.Text, out var offsetY))
            {
                MessageBox.Show("Invalid Offset Y value. Please enter a valid number.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create request
            Request = new CreateRevisionRequest
            {
                ProjectId = _project.Id,
                FloorplanPath = _selectedFilePath,
                Width = _imageWidth,
                Height = _imageHeight,
                ScaleDenominator = scaleDenominator,
                OffsetX = offsetX,
                OffsetY = offsetY
            };

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating revision request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
