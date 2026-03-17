using WoCo.Core.Models;
using WoCo.Core.Types;

namespace WoCo.Wpf.ViewModels;

public class AnnotationViewModel
{
    private readonly AnnotationRevision _annotationRevision;

    public AnnotationViewModel(AnnotationRevision annotationRevision)
    {
        _annotationRevision = annotationRevision;
    }

    public Guid Id => _annotationRevision.Id;
    public string Label => _annotationRevision.Label;
    public string? Description => _annotationRevision.Description;
    public AnnotationType Type => _annotationRevision.Type;
    public string Color => _annotationRevision.Color;
    public string NormalizedCoordinates => _annotationRevision.NormalizedCoordinates;
    public string RawCoordinates => _annotationRevision.RawCoordinates;
    public bool IsDeleted => _annotationRevision.IsDeleted;
    public DateTime CreatedAtUtc => _annotationRevision.CreatedAtUtc;
    
    // For rendering
    public bool IsVisible => !IsDeleted;
}
