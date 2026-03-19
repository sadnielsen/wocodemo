using WoCo.Core.Types;

namespace WoCo.Core.Models;

public class FloorplanRevision
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public int RevisionNumber { get; set; }
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string FileType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public CoordinateSystemType CoordinateSystem { get; set; } = CoordinateSystemType.Pixels;
    public CoordinateOriginType Origin { get; set; } = CoordinateOriginType.TopLeft;
    public double Width { get; set; }
    public double Height { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Transformation relative to initial revision (RevisionNumber == 1)
    public double ScaleDenominator { get; set; } = 1.0;
    public double OffsetX { get; set; } = 0.0;
    public double OffsetY { get; set; } = 0.0;

    // Navigation properties

    public Project? Project { get; set; }
    public List<AnnotationRevision> AnnotationRevisions { get; set; } = new();
}