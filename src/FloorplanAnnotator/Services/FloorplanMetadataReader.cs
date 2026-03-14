using System.Windows.Media.Imaging;

namespace FloorplanAnnotator.Services;

public sealed class FloorplanMetadata
{
    public double Width { get; init; }
    public double Height { get; init; }
}

public static class FloorplanMetadataReader
{
    public static FloorplanMetadata Read(string floorplanPath)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(floorplanPath, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();

        return new FloorplanMetadata
        {
            Width = bitmap.PixelWidth,
            Height = bitmap.PixelHeight
        };
    }
}