namespace FloorplanAnnotator.Models;

public class FloorplanRevision
{
    public int Id { get; set; }
    public int ProjectId { get; set; }

    public int RevisionNumber { get; set; }

    public string FloorplanPath { get; set; } = string.Empty;

    // Bronmetadata
    public CoordinateSystemType SourceCoordinateSystem { get; set; } = CoordinateSystemType.Pixels;
    public CoordinateOriginType SourceOrigin { get; set; } = CoordinateOriginType.TopLeft;

    public double SourceWidth { get; set; }
    public double SourceHeight { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Project? Project { get; set; }
    public List<AnnotationRevision> AnnotationRevisions { get; set; } = new();
}