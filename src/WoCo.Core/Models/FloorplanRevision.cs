using WoCo.Core.Types;

namespace WoCo.Core.Models;

public class FloorplanRevision
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public int RevisionNumber { get; set; }
    public string FloorplanPath { get; set; } = string.Empty;
    public CoordinateSystemType SourceCoordinateSystem { get; set; } = CoordinateSystemType.Pixels;
    public CoordinateOriginType SourceOrigin { get; set; } = CoordinateOriginType.TopLeft;
    public double SourceWidth { get; set; }
    public double SourceHeight { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties

    public Project? Project { get; set; }
    public List<AnnotationRevision> AnnotationRevisions { get; set; } = new();
}