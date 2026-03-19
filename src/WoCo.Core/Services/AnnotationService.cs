using Microsoft.EntityFrameworkCore;
using WoCo.Core.DataAccess;

namespace WoCo.Core.Services;

public class AnnotationService : IAnnotationService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public AnnotationService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task UpdateAnnotationCoordinatesAsync(Guid annotationRevisionId, double[] newNormalizedCoordinates)
    {
        using var context = _factory.CreateDbContext();

        var annotationRevision = await context.AnnotationRevisions
            .Include(ar => ar.FloorplanRevision)
            .FirstOrDefaultAsync(ar => ar.Id == annotationRevisionId);

        if (annotationRevision != null && annotationRevision.FloorplanRevision != null)
        {
            // Update NormalizedCoordinates
            annotationRevision.NormalizedCoordinates = newNormalizedCoordinates;

            // Also update RawCoordinates by denormalizing based on current floorplan dimensions
            // This ensures that when a new revision is created, it uses the updated position
            var rawCoordinates = DenormalizeCoordinates(
                annotationRevision.Type,
                newNormalizedCoordinates,
                annotationRevision.FloorplanRevision.Width,
                annotationRevision.FloorplanRevision.Height);

            annotationRevision.RawCoordinates = rawCoordinates;
            annotationRevision.Source = Types.AnnotationRevisionSourceType.ManualAdjusted;

            await context.SaveChangesAsync();

            System.Diagnostics.Debug.WriteLine(
                $"Updated annotation {annotationRevisionId} coordinates in database: " +
                $"Normalized: {string.Join(", ", newNormalizedCoordinates)}, " +
                $"Raw: {string.Join(", ", rawCoordinates)}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine(
                $"Warning: AnnotationRevision {annotationRevisionId} not found in database.");
        }
    }

    private static double[] DenormalizeCoordinates(
        Core.Types.AnnotationType type,
        double[] normalizedCoordinates,
        double width,
        double height)
    {
        var numbers = normalizedCoordinates;

        switch (type)
        {
            case Core.Types.AnnotationType.Point:
            case Core.Types.AnnotationType.Label:
                {
                    if (numbers.Length != 2)
                        throw new InvalidOperationException("Point/Label requires x,y.");

                    var x = numbers[0] * width;
                    var y = numbers[1] * height;
                    return [x, y];
                }

            case Core.Types.AnnotationType.Rectangle:
                {
                    if (numbers.Length != 4)
                        throw new InvalidOperationException("Rectangle requires x,y,width,height.");

                    var x = numbers[0] * width;
                    var y = numbers[1] * height;
                    var w = numbers[2] * width;
                    var h = numbers[3] * height;
                    return [x, y, w, h];
                }

            case Core.Types.AnnotationType.Polygon:
                {
                    if (numbers.Length < 6 || numbers.Length % 2 != 0)
                        throw new InvalidOperationException("Polygon requires x,y pairs.");

                    var denormalized = new List<double>();

                    for (var i = 0; i < numbers.Length; i += 2)
                    {
                        denormalized.Add(numbers[i] * width);
                        denormalized.Add(numbers[i + 1] * height);
                    }

                    return denormalized.ToArray();
                }

            default:
                throw new NotSupportedException($"Unsupported annotation type: {type}");
        }
    }
}
