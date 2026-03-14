using System.Text.Json;
using System.Text.Json.Serialization;
using WoCo.Core.Types;

namespace WoCo.Core.Services;

public sealed class ImportedAnnotation
{
    public string Label { get; set; } = string.Empty;
    public AnnotationType Type { get; set; } = AnnotationType.Rectangle;
    public string RawCoordinates { get; set; } = string.Empty;
    public string Color { get; set; } = "#FF0000";
}

public static class AnnotationImportParser
{
    private sealed class AnnotationDto
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "rectangle";

        [JsonPropertyName("coordinates")]
        public string Coordinates { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public string Color { get; set; } = "#FF0000";
    }

    public static List<ImportedAnnotation> ParseFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return new List<ImportedAnnotation>();

        try
        {
            var json = File.ReadAllText(filePath);
            var dtos = JsonSerializer.Deserialize<List<AnnotationDto>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dtos is null)
                return new List<ImportedAnnotation>();

            return dtos.Select(dto => new ImportedAnnotation
            {
                Label = dto.Label,
                Type = ParseAnnotationType(dto.Type),
                RawCoordinates = dto.Coordinates,
                Color = dto.Color
            }).ToList();
        }
        catch
        {
            return new List<ImportedAnnotation>();
        }
    }

    private static AnnotationType ParseAnnotationType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "rectangle" or "rect" => AnnotationType.Rectangle,
            "polygon" => AnnotationType.Polygon,
            "point" => AnnotationType.Point,
            "label" => AnnotationType.Label,
            _ => AnnotationType.Rectangle
        };
    }
}