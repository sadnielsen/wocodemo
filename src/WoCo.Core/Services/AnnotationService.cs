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

    public async Task UpdateAnnotationCoordinatesAsync(Guid annotationRevisionId, double[] newRawCoordinates)
    {
        using var context = _factory.CreateDbContext();

        var annotationRevision = await context.AnnotationRevisions
            .Include(ar => ar.FloorplanRevision)
            .FirstOrDefaultAsync(ar => ar.Id == annotationRevisionId);

        if (annotationRevision != null && annotationRevision.FloorplanRevision != null)
        {
            // Update raw pixel coordinates
            annotationRevision.RawCoordinates = newRawCoordinates;
            annotationRevision.Source = Types.AnnotationRevisionSourceType.ManualAdjusted;

            await context.SaveChangesAsync();

            System.Diagnostics.Debug.WriteLine(
                $"Updated annotation {annotationRevisionId} coordinates in database: " +
                $"Raw: {string.Join(", ", newRawCoordinates)}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine(
                $"Warning: AnnotationRevision {annotationRevisionId} not found in database.");
        }
    }

    }
