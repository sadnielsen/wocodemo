namespace WoCo.Core.Services;

public interface IAnnotationService
{
    Task UpdateAnnotationCoordinatesAsync(Guid annotationRevisionId, double[] newRawCoordinates);
}
