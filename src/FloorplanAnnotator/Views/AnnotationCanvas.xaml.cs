using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FloorplanAnnotator.Models;

namespace FloorplanAnnotator.Views
{
    public partial class AnnotationCanvas : UserControl
    {
        // Dependency property: path to the floorplan image
        public static readonly DependencyProperty FloorplanPathProperty =
            DependencyProperty.Register(
                nameof(FloorplanPath),
                typeof(string),
                typeof(AnnotationCanvas),
                new PropertyMetadata(null, OnFloorplanChanged));

        // Dependency property: list of annotations to render
        public static readonly DependencyProperty AnnotationsProperty =
            DependencyProperty.Register(
                nameof(Annotations),
                typeof(ObservableCollection<Annotation>),
                typeof(AnnotationCanvas),
                new PropertyMetadata(null, OnAnnotationsChanged));

        public string? FloorplanPath
        {
            get => (string?)GetValue(FloorplanPathProperty);
            set => SetValue(FloorplanPathProperty, value);
        }

        public ObservableCollection<Annotation>? Annotations
        {
            get => (ObservableCollection<Annotation>?)GetValue(AnnotationsProperty);
            set => SetValue(AnnotationsProperty, value);
        }

        public AnnotationCanvas()
        {
            InitializeComponent();
        }

        private static void OnFloorplanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnnotationCanvas canvas)
                canvas.Render();
        }

        private static void OnAnnotationsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnnotationCanvas canvas)
                canvas.Render();
        }

        private void Render()
        {
            FloorplanCanvas.Children.Clear();
            FloorplanImage.Source = null;

            // Load floorplan image
            double imgWidth = 800, imgHeight = 600;
            if (!string.IsNullOrEmpty(FloorplanPath) && File.Exists(FloorplanPath))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(FloorplanPath, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();

                    FloorplanImage.Source = bmp;
                    FloorplanImage.Width = bmp.PixelWidth;
                    FloorplanImage.Height = bmp.PixelHeight;
                    imgWidth = bmp.PixelWidth;
                    imgHeight = bmp.PixelHeight;
                }
                catch
                {
                    // Image load failed; canvas stays empty
                }
            }

            FloorplanCanvas.Children.Add(FloorplanImage);
            FloorplanCanvas.Width = imgWidth;
            FloorplanCanvas.Height = imgHeight;

            // Draw annotations
            if (Annotations == null) return;

            foreach (var annotation in Annotations)
            {
                DrawAnnotation(annotation);
            }
        }

        private void DrawAnnotation(Annotation annotation)
        {
            var brush = ParseBrush(annotation.Color);

            switch (annotation.Type)
            {
                case AnnotationType.Rectangle:
                    DrawRectangle(annotation, brush);
                    break;
                case AnnotationType.Polygon:
                    DrawPolygon(annotation, brush);
                    break;
                case AnnotationType.Point:
                    DrawPoint(annotation, brush);
                    break;
                case AnnotationType.Label:
                    DrawLabel(annotation, brush);
                    break;
            }
        }

        private void DrawRectangle(Annotation annotation, SolidColorBrush brush)
        {
            var parts = ParseCoords(annotation.Coordinates);
            if (parts.Length < 4) return;

            double x = parts[0], y = parts[1], w = parts[2] - parts[0], h = parts[3] - parts[1];
            if (w < 0) { x += w; w = -w; }
            if (h < 0) { y += h; h = -h; }

            var rect = new Rectangle
            {
                Width = w,
                Height = h,
                Stroke = brush,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(brush.Color) { Opacity = 0.15 }
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            FloorplanCanvas.Children.Add(rect);

            AddLabel(annotation.Label, x, y, brush);
        }

        private void DrawPolygon(Annotation annotation, SolidColorBrush brush)
        {
            var parts = ParseCoords(annotation.Coordinates);
            if (parts.Length < 4 || parts.Length % 2 != 0) return;

            var polygon = new Polygon
            {
                Stroke = brush,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(brush.Color) { Opacity = 0.15 }
            };

            for (int i = 0; i < parts.Length - 1; i += 2)
                polygon.Points.Add(new Point(parts[i], parts[i + 1]));

            FloorplanCanvas.Children.Add(polygon);

            if (polygon.Points.Count > 0)
                AddLabel(annotation.Label, polygon.Points[0].X, polygon.Points[0].Y, brush);
        }

        private void DrawPoint(Annotation annotation, SolidColorBrush brush)
        {
            var parts = ParseCoords(annotation.Coordinates);
            if (parts.Length < 2) return;

            double x = parts[0], y = parts[1];
            const double radius = 5;

            var ellipse = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = brush,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            Canvas.SetLeft(ellipse, x - radius);
            Canvas.SetTop(ellipse, y - radius);
            FloorplanCanvas.Children.Add(ellipse);

            AddLabel(annotation.Label, x + radius + 2, y - radius, brush);
        }

        private void DrawLabel(Annotation annotation, SolidColorBrush brush)
        {
            var parts = ParseCoords(annotation.Coordinates);
            double x = parts.Length >= 1 ? parts[0] : 0;
            double y = parts.Length >= 2 ? parts[1] : 0;
            AddLabel(annotation.Label, x, y, brush);
        }

        private void AddLabel(string text, double x, double y, SolidColorBrush brush)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var tb = new TextBlock
            {
                Text = text,
                Foreground = brush,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Colors.White) { Opacity = 0.7 },
                Padding = new Thickness(2)
            };
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);
            FloorplanCanvas.Children.Add(tb);
        }

        private static double[] ParseCoords(string coordinates)
        {
            if (string.IsNullOrWhiteSpace(coordinates))
                return Array.Empty<double>();

            var parts = coordinates.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<double>();
            foreach (var p in parts)
            {
                if (double.TryParse(p.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                    result.Add(val);
            }
            return result.ToArray();
        }

        private static SolidColorBrush ParseBrush(string color)
        {
            try
            {
                var c = (Color)ColorConverter.ConvertFromString(color);
                return new SolidColorBrush(c);
            }
            catch
            {
                return Brushes.Red;
            }
        }
    }
}
