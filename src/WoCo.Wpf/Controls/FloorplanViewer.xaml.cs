using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WoCo.Core.Types;
using WoCo.Wpf.ViewModels;

namespace WoCo.Wpf.Controls;

/// <summary>
/// Interaction logic for FloorplanViewer.xaml
/// </summary>
public partial class FloorplanViewer : UserControl
{
    private double _currentZoom = 1.0;
    private bool _isDragging = false;
    private UIElement? _draggedElement = null;
    private Point _dragStartPoint;
    private AnnotationViewModel? _draggedAnnotation = null;

    public FloorplanViewer()
    {
        InitializeComponent();

        // Wire up button events
        ZoomInButton.Click += (s, e) => ZoomIn();
        ZoomOutButton.Click += (s, e) => ZoomOut();
        FitButton.Click += (s, e) => FitToWindow();

        // Wire up canvas click to unselect when clicking outside annotations
        AnnotationsCanvas.MouseLeftButtonDown += OnCanvasMouseLeftButtonDown;
        AnnotationsCanvas.MouseWheel += OnCanvasMouseWheel;
    }

    public static readonly DependencyProperty CurrentRevisionProperty =
        DependencyProperty.Register(
            nameof(CurrentRevision),
            typeof(RevisionViewModel),
            typeof(FloorplanViewer),
            new PropertyMetadata(null, OnCurrentRevisionChanged));

    public RevisionViewModel? CurrentRevision
    {
        get => (RevisionViewModel?)GetValue(CurrentRevisionProperty);
        set => SetValue(CurrentRevisionProperty, value);
    }

    public static readonly DependencyProperty SelectedAnnotationProperty =
        DependencyProperty.Register(
            nameof(SelectedAnnotation),
            typeof(AnnotationViewModel),
            typeof(FloorplanViewer),
            new PropertyMetadata(null, OnSelectedAnnotationChanged));

    public AnnotationViewModel? SelectedAnnotation
    {
        get => (AnnotationViewModel?)GetValue(SelectedAnnotationProperty);
        set => SetValue(SelectedAnnotationProperty, value);
    }

    public event EventHandler<AnnotationViewModel>? AnnotationSelected;
    public event EventHandler<AnnotationCoordinatesChangedEventArgs>? AnnotationCoordinatesChanged;

    private static void OnSelectedAnnotationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FloorplanViewer viewer)
        {
            viewer.HighlightSelectedAnnotation();
        }
    }

    private static void OnCurrentRevisionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"OnCurrentRevisionChanged called! Old: {e.OldValue}, New: {e.NewValue}");

        if (d is FloorplanViewer viewer)
        {
            var revision = e.NewValue as RevisionViewModel;

            if (revision != null)
            {
                // Load floorplan image
                viewer.LoadFloorplanImage(revision);

                // Render annotations
                viewer.RenderAnnotations(revision.Annotations);

                System.Diagnostics.Debug.WriteLine($"Loaded revision {revision.Version} with {revision.Annotations.Count} annotations");
            }
            else
            {
                // Clear everything
                viewer.FloorplanImage.Source = null;
                viewer.AnnotationsCanvas.Children.Clear();
                viewer.PlaceholderText.Visibility = Visibility.Visible;

                System.Diagnostics.Debug.WriteLine("Cleared floorplan and annotations");
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
                using (var stream = new System.IO.MemoryStream(revision.FileContent))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                }

                FloorplanImage.Source = image;
                PlaceholderText.Visibility = Visibility.Collapsed;

                // Update canvas size to match image
                UpdateCanvasSize(image.PixelWidth, image.PixelHeight);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading floorplan image: {ex.Message}");
            FloorplanImage.Source = null;
            PlaceholderText.Visibility = Visibility.Visible;
        }
    }

    private void RenderAnnotations(IEnumerable<AnnotationViewModel> annotations)
    {
        AnnotationsCanvas.Children.Clear();

        if (annotations == null)
        {
            System.Diagnostics.Debug.WriteLine("Annotations is null");
            return;
        }

        var annotationsList = annotations.ToList();
        System.Diagnostics.Debug.WriteLine($"Rendering {annotationsList.Count} annotations");

        foreach (var annotation in annotationsList.Where(a => a.IsVisible))
        {
            System.Diagnostics.Debug.WriteLine($"Rendering annotation: {annotation.Label} at {annotation.RawCoordinates}");
            RenderAnnotation(annotation);
        }

        System.Diagnostics.Debug.WriteLine($"AnnotationsCanvas now has {AnnotationsCanvas.Children.Count} children");
    }

    private void RenderAnnotation(AnnotationViewModel annotation)
    {
        try
        {
            var coordinates = annotation.RawCoordinates;
            if (coordinates.Length == 0) return;

            // RawCoordinates are already in pixel coordinates
            var pixelCoordinates = coordinates;

            var color = (Color)ColorConverter.ConvertFromString(annotation.Color);
            var brush = new SolidColorBrush(color) { Opacity = 0.3 };
            var stroke = new SolidColorBrush(color);

            // Create a container to group shape and label together
            var annotationContainer = new Canvas
            {
                Tag = annotation,
                Background = Brushes.Transparent // Needed for hit testing
            };

            Shape? shape = annotation.Type switch
            {
                AnnotationType.Rectangle => CreateRectangle(pixelCoordinates, brush, stroke),
                AnnotationType.Polygon => CreatePolygon(pixelCoordinates, brush, stroke),
                AnnotationType.Point => CreatePoint(pixelCoordinates, stroke),
                AnnotationType.Label => null, // Labels handled separately
                _ => null
            };

            if (shape != null)
            {
                // Add shape to container (no Canvas positioning needed, shape handles it internally)
                annotationContainer.Children.Add(shape);
            }

            // Add label if present
            if (!string.IsNullOrEmpty(annotation.Label))
            {
                var label = CreateLabel(pixelCoordinates, annotation.Label, stroke);
                annotationContainer.Children.Add(label);
            }

            // Make the entire container interactive
            MakeAnnotationContainerInteractive(annotationContainer, annotation);

            // Position the container at origin (0,0) since children have their own positions
            Canvas.SetLeft(annotationContainer, 0);
            Canvas.SetTop(annotationContainer, 0);

            AnnotationsCanvas.Children.Add(annotationContainer);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error rendering annotation {annotation.Id}: {ex.Message}");
        }
    }

    private Rectangle CreateRectangle(double[] coords, Brush fill, Brush stroke)
    {
        // Expecting 4 values: x, y, width, height
        if (coords.Length < 4) return new Rectangle();

        return new Rectangle
        {
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = 2,
            Width = coords[2],
            Height = coords[3]
        }.SetPosition(coords[0], coords[1]);
    }

    private Polygon CreatePolygon(double[] coords, Brush fill, Brush stroke)
    {
        var points = new PointCollection();
        for (int i = 0; i < coords.Length; i += 2)
        {
            if (i + 1 < coords.Length)
            {
                points.Add(new Point(coords[i], coords[i + 1]));
            }
        }

        return new Polygon
        {
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = 2,
            Points = points
        };
    }

    private Ellipse CreatePoint(double[] coords, Brush stroke)
    {
        if (coords.Length < 2) return new Ellipse();

        const double radius = 5;
        return new Ellipse
        {
            Fill = stroke,
            Width = radius * 2,
            Height = radius * 2
        }.SetPosition(coords[0] - radius, coords[1] - radius);
    }

    private TextBlock CreateLabel(double[] coords, string text, Brush foreground)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            Foreground = foreground,
            Background = new SolidColorBrush(Colors.White) { Opacity = 0.8 },
            Padding = new Thickness(4, 2, 4, 2),
            FontWeight = FontWeights.Bold
        };

        Canvas.SetLeft(textBlock, coords[0]);
        Canvas.SetTop(textBlock, coords.Length > 1 ? coords[1] : 0);

        return textBlock;
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
        ScaleTransform.ScaleX = _currentZoom;
        ScaleTransform.ScaleY = _currentZoom;
        ZoomLevelText.Text = $"{_currentZoom * 100:F0}%";
    }

    private void UpdateCanvasSize(double width, double height)
    {
        FloorplanCanvas.Width = width;
        FloorplanCanvas.Height = height;
        FloorplanImage.Width = width;
        FloorplanImage.Height = height;
        AnnotationsCanvas.Width = width;
        AnnotationsCanvas.Height = height;

        System.Diagnostics.Debug.WriteLine($"Canvas updated to size: {width}x{height}");
    }

    private void MakeAnnotationContainerInteractive(Canvas container, AnnotationViewModel annotation)
    {
        container.Cursor = Cursors.Hand;

        // Tooltip with annotation details (shows on hover)
        container.ToolTip = CreateAnnotationTooltip(annotation);

        // Left-click to select/unselect
        container.MouseLeftButtonDown += OnAnnotationLeftClick;

        // Right-click to start drag
        container.MouseRightButtonDown += OnAnnotationRightMouseDown;
        container.MouseRightButtonUp += OnAnnotationRightMouseUp;
        container.MouseMove += OnAnnotationMouseMove;

        // Hover effects - apply to all shapes within the container
        container.MouseEnter += (s, e) =>
        {
            if (s is Canvas c)
            {
                foreach (var child in c.Children)
                {
                    if (child is Shape shape)
                    {
                        shape.Opacity = 0.6;
                        shape.StrokeThickness = 3;
                    }
                    else if (child is TextBlock tb)
                    {
                        tb.FontWeight = FontWeights.ExtraBold;
                        tb.Background = new SolidColorBrush(Colors.Yellow) { Opacity = 0.5 };
                    }
                }
            }
        };

        container.MouseLeave += (s, e) =>
        {
            if (s is Canvas c)
            {
                foreach (var child in c.Children)
                {
                    if (child is Shape shape)
                    {
                        shape.Opacity = 1.0;
                        shape.StrokeThickness = 2;
                    }
                    else if (child is TextBlock tb)
                    {
                        tb.FontWeight = FontWeights.Bold;
                        tb.Background = new SolidColorBrush(Colors.White) { Opacity = 0.8 };
                    }
                }
            }
        };
    }

    private ToolTip CreateAnnotationTooltip(AnnotationViewModel annotation)
    {
        var tooltip = new ToolTip();
        var stackPanel = new StackPanel();

        stackPanel.Children.Add(new TextBlock 
        { 
            Text = annotation.Label, 
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 5)
        });

        if (!string.IsNullOrEmpty(annotation.Description))
        {
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = annotation.Description,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 300,
                Margin = new Thickness(0, 0, 0, 5)
            });
        }

        stackPanel.Children.Add(new TextBlock 
        { 
            Text = $"Type: {annotation.Type}",
            FontSize = 11,
            Foreground = Brushes.Gray
        });

        stackPanel.Children.Add(new TextBlock
        {
            Text = $"Coordinates: {string.Join(", ", annotation.RawCoordinates)}",
            FontSize = 11,
            Foreground = Brushes.Gray
        });

        stackPanel.Children.Add(new TextBlock 
        { 
            Text = $"Created: {annotation.CreatedAtUtc:g}",
            FontSize = 11,
            Foreground = Brushes.Gray
        });

        tooltip.Content = stackPanel;
        return tooltip;
    }

    private void OnAnnotationLeftClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Canvas container && container.Tag is AnnotationViewModel annotation)
        {
            // Toggle selection: if already selected, unselect; otherwise select
            if (SelectedAnnotation?.Id == annotation.Id)
            {
                SelectedAnnotation = null;
                System.Diagnostics.Debug.WriteLine($"Unselected annotation: {annotation.Label}");
            }
            else
            {
                SelectedAnnotation = annotation;
                AnnotationSelected?.Invoke(this, annotation);
                System.Diagnostics.Debug.WriteLine($"Selected annotation: {annotation.Label}");
            }

            e.Handled = true;
        }
    }

    private void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // If clicking directly on the canvas (not on an annotation), unselect
        if (e.Source == AnnotationsCanvas)
        {
            if (SelectedAnnotation != null)
            {
                SelectedAnnotation = null;
                System.Diagnostics.Debug.WriteLine("Unselected annotation (clicked on canvas)");
            }
            e.Handled = true;
        }
    }

    private void OnCanvasMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta > 0)
        {
            ZoomIn();
        }
        else
        {
            ZoomOut();
        }

        e.Handled = true;
    }

    private void OnAnnotationRightMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Canvas container && container.Tag is AnnotationViewModel annotation)
        {
            // Start drag operation with right mouse button
            _isDragging = true;
            _draggedElement = container;
            _draggedAnnotation = annotation;
            _dragStartPoint = e.GetPosition(AnnotationsCanvas);

            container.CaptureMouse();
            container.Cursor = Cursors.SizeAll; // Change cursor to indicate dragging
            e.Handled = true;

            System.Diagnostics.Debug.WriteLine($"Started dragging annotation: {annotation.Label}");
        }
    }

    private void OnAnnotationMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && _draggedElement is Canvas container && e.RightButton == MouseButtonState.Pressed)
        {
            var currentPosition = e.GetPosition(AnnotationsCanvas);
            Vector totalOffset = currentPosition - _dragStartPoint;

            // Move all children in the container together
            foreach (var child in container.Children)
            {
                if (child is Polygon polygon)
                {
                    // For polygons, recalculate points with offset
                    var coordinates = _draggedAnnotation!.RawCoordinates;
                    var pixelCoordinates = coordinates;

                    var newPoints = new PointCollection();
                    for (int i = 0; i < pixelCoordinates.Length; i += 2)
                    {
                        if (i + 1 < pixelCoordinates.Length)
                        {
                            newPoints.Add(new Point(
                                pixelCoordinates[i] + totalOffset.X,
                                pixelCoordinates[i + 1] + totalOffset.Y
                            ));
                        }
                    }
                    polygon.Points = newPoints;
                }
                else if (child is Shape shape)
                {
                    // For other shapes, get original position and add offset
                    var coordinates = _draggedAnnotation!.RawCoordinates;
                    var pixelCoordinates = coordinates;

                    Canvas.SetLeft(shape, pixelCoordinates[0] + totalOffset.X);
                    Canvas.SetTop(shape, pixelCoordinates.Length > 1 ? pixelCoordinates[1] + totalOffset.Y : totalOffset.Y);
                }
                else if (child is TextBlock textBlock)
                {
                    // Move label with the shape
                    var coordinates = _draggedAnnotation!.RawCoordinates;
                    var pixelCoordinates = coordinates;

                    Canvas.SetLeft(textBlock, pixelCoordinates[0] + totalOffset.X);
                    Canvas.SetTop(textBlock, pixelCoordinates.Length > 1 ? pixelCoordinates[1] + totalOffset.Y : totalOffset.Y);
                }
            }

            e.Handled = true;
        }
    }

    private void OnAnnotationRightMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging && _draggedElement is Canvas container)
        {
            container.ReleaseMouseCapture();
            container.Cursor = Cursors.Hand; // Restore normal cursor

            // Save the new position
            if (_draggedAnnotation != null)
            {
                var currentPosition = e.GetPosition(AnnotationsCanvas);
                Vector totalOffset = currentPosition - _dragStartPoint;

                var newRawCoords = CalculateNewCoordinates(
                    _draggedAnnotation.RawCoordinates, 
                    totalOffset);

                UpdateAnnotationCoordinates(_draggedAnnotation, newRawCoords);
            }

            // Reset drag state
            _isDragging = false;
            _draggedElement = null;
            _draggedAnnotation = null;

            e.Handled = true;
        }
    }

    private void HighlightSelectedAnnotation()
    {
        // Reset all annotation containers to normal state
        foreach (var child in AnnotationsCanvas.Children)
        {
            if (child is Canvas container && container.Tag is AnnotationViewModel annotation)
            {
                // Restore original color from annotation
                var color = (Color)ColorConverter.ConvertFromString(annotation.Color);
                var originalStroke = new SolidColorBrush(color);

                foreach (var innerChild in container.Children)
                {
                    if (innerChild is Shape shape)
                    {
                        shape.StrokeThickness = 2;
                        shape.Stroke = originalStroke; // Restore original color
                    }
                }
            }
        }

        // Highlight the selected annotation
        if (SelectedAnnotation != null)
        {
            foreach (var child in AnnotationsCanvas.Children)
            {
                if (child is Canvas container && container.Tag is AnnotationViewModel annotation)
                {
                    if (annotation.Id == SelectedAnnotation.Id)
                    {
                        foreach (var innerChild in container.Children)
                        {
                            if (innerChild is Shape shape)
                            {
                                shape.StrokeThickness = 4;
                                shape.Stroke = Brushes.Blue; // Highlight color
                            }
                        }
                    }
                }
            }
        }
    }

    private double[] CalculateNewCoordinates(double[] originalRaw, Vector pixelOffset)
    {
        var newRaw = new double[originalRaw.Length];

        for (int i = 0; i < originalRaw.Length; i++)
        {
            if (i % 2 == 0) // X coordinate
            {
                newRaw[i] = originalRaw[i] + pixelOffset.X;
            }
            else // Y coordinate
            {
                newRaw[i] = originalRaw[i] + pixelOffset.Y;
            }
        }

        return newRaw;
    }

    private void UpdateAnnotationCoordinates(AnnotationViewModel annotation, double[] newRawCoordinates)
    {
        System.Diagnostics.Debug.WriteLine(
            $"Annotation {annotation.Label} coordinates changed from " +
            $"{string.Join(", ", annotation.RawCoordinates)} to " +
            $"{string.Join(", ", newRawCoordinates)}");

        // Raise event for parent ViewModel to handle persistence
        AnnotationCoordinatesChanged?.Invoke(this, 
            new AnnotationCoordinatesChangedEventArgs(annotation, newRawCoordinates));
    }
}

// Event args class
public class AnnotationCoordinatesChangedEventArgs : EventArgs
{
    public AnnotationViewModel Annotation { get; }
    public Guid AnnotationId => Annotation.Id;
    public double[] NewRawCoordinates { get; }

    public AnnotationCoordinatesChangedEventArgs(AnnotationViewModel annotation, double[] newCoordinates)
    {
        Annotation = annotation;
        NewRawCoordinates = newCoordinates;
    }
}

// Helper extension method
internal static class ShapeExtensions
{
    public static T SetPosition<T>(this T shape, double left, double top) where T : UIElement
    {
        Canvas.SetLeft(shape, left);
        Canvas.SetTop(shape, top);
        return shape;
    }
}
