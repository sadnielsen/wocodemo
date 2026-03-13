namespace FloorplanAnnotator.Models
{
    public enum AnnotationType
    {
        Rectangle,
        Polygon,
        Point,
        Label
    }

    public class Annotation
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Label { get; set; } = string.Empty;
        public AnnotationType Type { get; set; } = AnnotationType.Rectangle;
        public string Coordinates { get; set; } = string.Empty;
        public string Color { get; set; } = "#FF0000";
        public Project? Project { get; set; }
    }
}
