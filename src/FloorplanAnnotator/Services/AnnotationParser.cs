using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using FloorplanAnnotator.Models;

namespace FloorplanAnnotator.Services
{
    /// <summary>
    /// Parses annotation files in JSON format.
    /// Expected JSON structure:
    /// [
    ///   { "label": "Room A", "type": "rectangle", "coordinates": "10,20,100,80", "color": "#FF0000" },
    ///   { "label": "Door", "type": "point", "coordinates": "50,30", "color": "#00FF00" }
    /// ]
    /// </summary>
    public static class AnnotationParser
    {
        private class AnnotationDto
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

        public static List<Annotation> ParseFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return new List<Annotation>();

            var result = new List<Annotation>();

            try
            {
                var json = File.ReadAllText(filePath);
                var dtos = JsonSerializer.Deserialize<List<AnnotationDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (dtos == null) return result;

                foreach (var dto in dtos)
                {
                    result.Add(new Annotation
                    {
                        Label = dto.Label,
                        Type = ParseAnnotationType(dto.Type),
                        Coordinates = dto.Coordinates,
                        Color = dto.Color
                    });
                }
            }
            catch (Exception)
            {
                // Return whatever was parsed so far; caller can handle empty list
            }

            return result;
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
}
