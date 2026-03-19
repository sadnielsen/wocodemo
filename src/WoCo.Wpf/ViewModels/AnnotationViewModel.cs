using System.ComponentModel;
using WoCo.Core.Models;
using WoCo.Core.Types;

namespace WoCo.Wpf.ViewModels;

public class AnnotationViewModel : INotifyPropertyChanged
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
    public double[] RawCoordinates => _annotationRevision.RawCoordinates;
    public bool IsDeleted => _annotationRevision.IsDeleted;
    public DateTime CreatedAtUtc => _annotationRevision.CreatedAtUtc;

    // For rendering
    public bool IsVisible => !IsDeleted;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Updates the raw coordinates of this annotation.
    /// This should be called after the coordinates are persisted to the database.
    /// </summary>
    public void UpdateCoordinates(double[] newRawCoordinates)
    {
        _annotationRevision.RawCoordinates = newRawCoordinates;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RawCoordinates)));
    }
}
