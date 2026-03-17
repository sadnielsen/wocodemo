using WoCo.Core.Types;

namespace WoCo.Core.Models;

public class AnnotationRevision
{
    public Guid Id { get; set; }

    public Guid AnnotationId { get; set; }
    public Guid FloorplanRevisionId { get; set; }

    // Redundant value, but easy, comes from FloorplanRevision.
    public int RevisionNumber { get; set; }

    public AnnotationRevisionSourceType Source { get; set; }

    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AnnotationType Type { get; set; } = AnnotationType.Rectangle;
    public string Color { get; set; } = "#FF0000";

    // Audit / import
    public string RawCoordinates { get; set; } = string.Empty;

    // Canonieke interne opslag
    public string NormalizedCoordinates { get; set; } = string.Empty;

    // Semantisch verwijderen in een revisie
    public bool IsDeleted { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties

    public Annotation? Annotation { get; set; }
    public FloorplanRevision? FloorplanRevision { get; set; }
}