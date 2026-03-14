namespace WoCo.Core.Models;

public class Annotation
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }

    // Navigation properties

    public Project? Project { get; set; }
    public List<AnnotationRevision> Revisions { get; set; } = new();
}