using WoCo.Core.DataAccess;
using WoCo.Core.Models;
using WoCo.Core.Types;

namespace WoCo.Core.Services;

public sealed class CreateRevisionRequest
{
    public Guid ProjectId { get; init; }
    public string FloorplanPath { get; init; } = string.Empty;
    public double Width { get; set; }
    public double Height { get; set; }
    public CoordinateSystemType SourceCoordinateSystem { get; init; } = CoordinateSystemType.Pixels;
    public CoordinateOriginType SourceOrigin { get; init; } = CoordinateOriginType.TopLeft;

    public double ScaleDenominator { get; init; } = 1.0;
    public double OffsetX { get; init; } = 0;
    public double OffsetY { get; init; } = 0;
}

public sealed class CreateRevisionService : ICreateRevisionService
{
    private readonly IProjectRepository _projectRepository;

    public CreateRevisionService(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Project> CreateAsync(CreateRevisionRequest request)
    {
        Validate(request);

        var project = await _projectRepository.GetProjectByIdAsync(request.ProjectId);
        if (project == null)
            throw new InvalidOperationException($"Project with ID {request.ProjectId} not found.");

        var floorplanFileContent = File.ReadAllBytes(request.FloorplanPath);
        var floorplanFileName = Path.GetFileName(request.FloorplanPath);
        var floorplanFileType = Path.GetExtension(request.FloorplanPath).TrimStart('.').ToLowerInvariant();

        var now = DateTime.UtcNow;
        var latestRevisionNumber = project.FloorplanRevisions.Max(fr => fr.RevisionNumber);
        var newRevisionNumber = latestRevisionNumber + 1;

        // Transform from the previous revision (latest), not from initial
        var previousFloorplan = project.FloorplanRevisions
            .First(fr => fr.RevisionNumber == latestRevisionNumber);

        var newFloorplanRevision = new FloorplanRevision
        {
            RevisionNumber = newRevisionNumber,
            FileContent = floorplanFileContent,
            FileType = floorplanFileType,
            FileName = floorplanFileName,
            CoordinateSystem = request.SourceCoordinateSystem,
            Origin = request.SourceOrigin,
            Width = request.Width,
            Height = request.Height,
            ScaleDenominator = request.ScaleDenominator,
            OffsetX = request.OffsetX,
            OffsetY = request.OffsetY,
            CreatedAtUtc = now
        };

        project.FloorplanRevisions.Add(newFloorplanRevision);

        foreach (var annotation in project.Annotations)
        {
            var previousAnnotationRevision = annotation.Revisions
                .FirstOrDefault(r => r.RevisionNumber == latestRevisionNumber);

            if (previousAnnotationRevision == null)
                continue;

            // If annotation was deleted in previous revision, keep it deleted
            if (previousAnnotationRevision.IsDeleted)
            {
                var deletedAnnotationRevision = new AnnotationRevision
                {
                    FloorplanRevision = newFloorplanRevision,
                    RevisionNumber = newRevisionNumber,
                    Source = AnnotationRevisionSourceType.AutoTransformed,
                    Label = previousAnnotationRevision.Label,
                    Description = previousAnnotationRevision.Description,
                    Type = previousAnnotationRevision.Type,
                    Color = previousAnnotationRevision.Color,
                    RawCoordinates = previousAnnotationRevision.RawCoordinates,
                    IsDeleted = true,
                    CreatedAtUtc = now
                };
                annotation.Revisions.Add(deletedAnnotationRevision);
                continue;
            }

            var transformedRawCoordinates = TransformCoordinatesFromPrevious(
                previousAnnotationRevision.Type,
                previousAnnotationRevision.RawCoordinates,
                previousFloorplan.Width,
                previousFloorplan.Height,
                previousFloorplan.ScaleDenominator,
                request.ScaleDenominator,
                request.OffsetX,
                request.OffsetY);

            var newAnnotationRevision = new AnnotationRevision
            {
                FloorplanRevision = newFloorplanRevision,
                RevisionNumber = newRevisionNumber,
                Source = AnnotationRevisionSourceType.AutoTransformed,
                Label = previousAnnotationRevision.Label,
                Description = previousAnnotationRevision.Description,
                Type = previousAnnotationRevision.Type,
                Color = previousAnnotationRevision.Color,
                RawCoordinates = transformedRawCoordinates,
                IsDeleted = false,
                CreatedAtUtc = now
            };

            annotation.Revisions.Add(newAnnotationRevision);
        }

        await _projectRepository.UpdateProjectAsync(project);
        return project;
    }

    private static void Validate(CreateRevisionRequest request)
    {
        if (request.ProjectId == Guid.Empty)
            throw new InvalidOperationException("Project ID is required.");

        if (!File.Exists(request.FloorplanPath))
            throw new FileNotFoundException("Floorplan file not found.", request.FloorplanPath);

        if (request.Width <= 0)
            throw new InvalidOperationException("Width must be greater than zero.");

        if (request.Height <= 0)
            throw new InvalidOperationException("Height must be greater than zero.");

        if (request.ScaleDenominator <= 0)
            throw new InvalidOperationException("ScaleDenominator must be greater than zero.");
    }

    private static double[] TransformCoordinatesFromPrevious(
        AnnotationType type,
        double[] previousRawCoordinates,
        double previousWidth,
        double previousHeight,
        double previousScaleDenominator,
        double newScaleDenominator,
        double offsetX = 0,
        double offsetY = 0)
    {

        var scaleFactor = previousScaleDenominator / newScaleDenominator;

        return TransformRawCoordinates(
            type,
            previousRawCoordinates,
            scaleFactor,
            offsetX,
            offsetY);
    }

    private static double[] TransformRawCoordinates(
        AnnotationType type,
        double[] rawCoordinates,
        double scaleFactor,
        double offsetX = 0,
        double offsetY = 0)
    {
        var numbers = rawCoordinates;

        switch (type)
        {
            case AnnotationType.Point:
            case AnnotationType.Label:
                {
                    if (numbers.Length != 2)
                        throw new InvalidOperationException("Point/Label requires x,y.");

                    var x = (numbers[0] * scaleFactor) + offsetX;
                    var y = (numbers[1] * scaleFactor) + offsetY;
                    return [x, y];
                }

            case AnnotationType.Rectangle:
                {
                    if (numbers.Length != 4)
                        throw new InvalidOperationException("Rectangle requires x,y,width,height.");

                    var x = (numbers[0] * scaleFactor) + offsetX;
                    var y = (numbers[1] * scaleFactor) + offsetY;
                    var w = numbers[2] * scaleFactor;
                    var h = numbers[3] * scaleFactor;
                    return [x, y, w, h];
                }

            case AnnotationType.Polygon:
                {
                    if (numbers.Length < 6 || numbers.Length % 2 != 0)
                        throw new InvalidOperationException("Polygon requires x,y pairs.");

                    var transformed = new List<double>();

                    for (var i = 0; i < numbers.Length; i += 2)
                    {
                        var x = (numbers[i] * scaleFactor) + offsetX;
                        var y = (numbers[i + 1] * scaleFactor) + offsetY;

                        transformed.Add(x);
                        transformed.Add(y);
                    }

                    return transformed.ToArray();
                }

            default:
                throw new NotSupportedException($"Unsupported annotation type: {type}");
        }
    }

    }
