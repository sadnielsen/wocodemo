using System;
using System.Collections.Generic;

namespace FloorplanAnnotator.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<FloorplanRevision> FloorplanRevisions { get; set; } = new();
    public List<Annotation> Annotations { get; set; } = new();
}