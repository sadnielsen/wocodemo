using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
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
        private double _imageWidth = 800;
        private double _imageHeight = 600;

        public static readonly DependencyProperty FloorplanPathProperty =
            DependencyProperty.Register(
                nameof(FloorplanPath),
                typeof(string),
                typeof(AnnotationCanvas),
                new PropertyMetadata(null, OnFloorplanChanged));

        public static readonly DependencyProperty AnnotationsProperty =
            DependencyProperty.Register(
                nameof(Annotations),
                typeof(ObservableCollection<AnnotationRevision>),
                typeof(AnnotationCanvas),
                new PropertyMetadata(null, OnAnnotationsChanged));

        public string? FloorplanPath
        {
            get => (string?)GetValue(FloorplanPathProperty);
            set => SetValue(FloorplanPathProperty, value);
        }

        public ObservableCollection<AnnotationRevision>? Annotations
        {
            get => (ObservableCollection<AnnotationRevision>?)GetValue(AnnotationsProperty);
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

            _imageWidth = 800;
            _imageHeight = 600;

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

                    _imageWidth = bmp.PixelWidth;
                    _imageHeight = bmp.PixelHeight;
                }
                catch
                {
                }
            }

            FloorplanCanvas.Children.Add(FloorplanImage);
            FloorplanCanvas.Width = _imageWidth;
            FloorplanCanvas.Height = _imageHeight;

            if (Annotations == null)
                return;

            foreach (var annotation in Annotations.Where(a => !a.IsDeleted))
            {
                DrawAnnotation(annotation);
            }
        }

        private void DrawAnnotation(AnnotationRevision annotation)
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

        private void DrawRectangle(AnnotationRevision annotation, SolidColorBrush brush)
        {
            var parts = ParseCoords(annotation.NormalizedCoordinates);
            if (parts.Length < 4) return;

            double x = parts[0] * _imageWidth;
            double y = parts[1] * _imageHeight;
            double w = parts[2] * _imageWidth;
            double h = parts[3] * _imageHeight;

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

        private void DrawPolygon(AnnotationRevision annotation, SolidColorBrush brush)
        {
            var parts = ParseCoords(annotation.NormalizedCoordinates);
            if (parts.Length < 6 || parts.Length % 2 != 0) return;

            var polygon = new Polygon
            {
                Stroke = brush,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(brush.Color) { Opacity = 0.15 }
            };

            for (int i = 0; i < parts.Length - 1; i += 2)
            {
                polygon.Points.Add(new Point(
                    parts[i] * _imageWidth,
                    parts[i + 1] * _imageHeight));
            }

            FloorplanCanvas.Children.Add(polygon);

            if (polygon.Points.Count > 0)
                AddLabel(annotation.Label, polygon.Points[0].X, polygon.Points[0].Y, brush);
        }

        private void DrawPoint(AnnotationRevision annotation, SolidColorBrush brush)
        {
            var parts = ParseCoords(annotation.NormalizedCoordinates);
            if (parts.Length < 2) return;

            double x = parts[0] * _imageWidth;
            double y = parts[1] * _imageHeight;
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

        private void DrawLabel(AnnotationRevision annotation, SolidColorBrush brush)
        {
            var parts = ParseCoords(annotation.NormalizedCoordinates);

            double x = parts.Length >= 1 ? parts[0] * _imageWidth : 0;
            double y = parts.Length >= 2 ? parts[1] * _imageHeight : 0;

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
                if (double.TryParse(p.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
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