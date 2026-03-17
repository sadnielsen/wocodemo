using System.Collections.ObjectModel;
using WoCo.Core.Models;

namespace WoCo.Wpf.ViewModels;

public class ProjectViewModel
{
    private readonly Project _project;

    public ProjectViewModel(Project project)
    {
        _project = project;
        
        // Convert FloorplanRevisions to RevisionViewModels
        foreach (var revision in project.FloorplanRevisions.OrderBy(r => r.RevisionNumber))
        {
            Revisions.Add(new RevisionViewModel(revision));
        }
    }

    public Guid Id => _project.Id;
    public string Name => _project.Name;
    public DateTime CreatedAtUtc => _project.CreatedAtUtc;
    
    public ObservableCollection<RevisionViewModel> Revisions { get; } = new();

    // Properties for MetaInfoPanel binding
    public string ProjectName => Name;
    public string? Revision => null;
    public DateTime Date => CreatedAtUtc;
    public string Description => $"Project containing {Revisions.Count} revision(s)";
    public string Status => "Active";
    public string CreatedBy => "System";
    public string Notes => string.Empty;
}
