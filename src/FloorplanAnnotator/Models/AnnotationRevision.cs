namespace FloorplanAnnotator.Models;

public class AnnotationRevision
{
    public int Id { get; set; }

    public int AnnotationId { get; set; }
    public int FloorplanRevisionId { get; set; }

    // Mag gelijk lopen met floorplan revision, maar expliciet opslaan maakt debuggen en queryen makkelijker
    public int RevisionNumber { get; set; }

    public AnnotationRevisionSource Source { get; set; }

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

    public Annotation? Annotation { get; set; }
    public FloorplanRevision? FloorplanRevision { get; set; }
}