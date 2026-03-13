using System;
using System.Collections.Generic;

namespace FloorplanAnnotator.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FloorplanPath { get; set; } = string.Empty;
        public string AnnotationsPath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<Annotation> Annotations { get; set; } = new();
    }
}
