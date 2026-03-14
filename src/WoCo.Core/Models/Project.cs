namespace WoCo.Core.Models;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties

    public List<FloorplanRevision> FloorplanRevisions { get; set; } = new();
    public List<Annotation> Annotations { get; set; } = new();
}