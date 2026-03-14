namespace FloorplanAnnotator.Models;

public class Annotation
{
    public int Id { get; set; }
    public int ProjectId { get; set; }

    // Stabiele identiteit
    public Guid PublicId { get; set; } = Guid.NewGuid();

    // Handig als je ooit een bron-id uit import hebt
    public string? ExternalSourceId { get; set; }

    public Project? Project { get; set; }
    public List<AnnotationRevision> Revisions { get; set; } = new();
}