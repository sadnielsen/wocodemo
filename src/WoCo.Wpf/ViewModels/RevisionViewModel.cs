using System.Collections.ObjectModel;
using WoCo.Core.Models;

namespace WoCo.Wpf.ViewModels;

public class RevisionViewModel
{
    private readonly FloorplanRevision _revision;

    public RevisionViewModel(FloorplanRevision revision, ProjectViewModel? parentProject = null)
    {
        _revision = revision;
        ParentProject = parentProject;

        // Load annotations for this revision
        foreach (var annotationRevision in revision.AnnotationRevisions.Where(a => !a.IsDeleted))
        {
            Annotations.Add(new AnnotationViewModel(annotationRevision));
        }
    }

    // Reference to parent project
    public ProjectViewModel? ParentProject { get; internal set; }

    public Guid Id => _revision.Id;
    public int Version => _revision.RevisionNumber;
    public DateTime Date => _revision.CreatedAtUtc;
    public string FileName => _revision.FileName;

    // Annotations for rendering
    public ObservableCollection<AnnotationViewModel> Annotations { get; } = new();

    // Properties for MetaInfoPanel binding
    public string ProjectName => "";
    public string Revision => $"Revision {Version}";
    public string Description => $"Floorplan file: {FileName}";
    public string Status => "Published";
    public string CreatedBy => "System";
    public string Notes => $"File type: {_revision.FileType}, Size: {_revision.Width}x{_revision.Height}, Annotations: {Annotations.Count}";

    // For floorplan display
    public byte[] FileContent => _revision.FileContent;
    public string FileType => _revision.FileType;
    public double Width => _revision.Width;
    public double Height => _revision.Height;
}
