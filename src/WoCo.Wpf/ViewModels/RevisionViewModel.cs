using WoCo.Core.Models;

namespace WoCo.Wpf.ViewModels;

public class RevisionViewModel
{
    private readonly FloorplanRevision _revision;

    public RevisionViewModel(FloorplanRevision revision)
    {
        _revision = revision;
    }

    public Guid Id => _revision.Id;
    public int Version => _revision.RevisionNumber;
    public DateTime Date => _revision.CreatedAtUtc;
    public string FileName => _revision.FileName;
    
    // Properties for MetaInfoPanel binding
    public string ProjectName => "";
    public string Revision => $"Revision {Version}";
    public string Description => $"Floorplan file: {FileName}";
    public string Status => "Published";
    public string CreatedBy => "System";
    public string Notes => $"File type: {_revision.FileType}, Size: {_revision.Width}x{_revision.Height}";
    
    // For floorplan display
    public byte[] FileContent => _revision.FileContent;
    public string FileType => _revision.FileType;
}
