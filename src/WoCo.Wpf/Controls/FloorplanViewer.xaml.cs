using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WoCo.Wpf.Controls;

/// <summary>
/// Interaction logic for FloorplanViewer.xaml
/// </summary>
public partial class FloorplanViewer : UserControl
{
    private double _currentZoom = 1.0;

    public FloorplanViewer()
    {
        InitializeComponent();
        
        // Wire up button events
        ZoomInButton.Click += (s, e) => ZoomIn();
        ZoomOutButton.Click += (s, e) => ZoomOut();
        FitButton.Click += (s, e) => FitToWindow();
    }

    public static readonly DependencyProperty FloorplanSourceProperty =
        DependencyProperty.Register(
            nameof(FloorplanSource),
            typeof(ImageSource),
            typeof(FloorplanViewer),
            new PropertyMetadata(null, OnFloorplanSourceChanged));

    public ImageSource? FloorplanSource
    {
        get => (ImageSource?)GetValue(FloorplanSourceProperty);
        set => SetValue(FloorplanSourceProperty, value);
    }

    private static void OnFloorplanSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FloorplanViewer viewer)
        {
            viewer.FloorplanImage.Source = e.NewValue as ImageSource;
            viewer.PlaceholderText.Visibility = e.NewValue == null ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void ZoomIn()
    {
        _currentZoom *= 1.2;
        ApplyZoom();
    }

    private void ZoomOut()
    {
        _currentZoom /= 1.2;
        ApplyZoom();
    }

    private void FitToWindow()
    {
        _currentZoom = 1.0;
        ApplyZoom();
    }

    private void ApplyZoom()
    {
        var scaleTransform = new ScaleTransform(_currentZoom, _currentZoom);
        FloorplanViewbox.LayoutTransform = scaleTransform;
        ZoomLevelText.Text = $"{_currentZoom * 100:F0}%";
    }
}
