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

        var initialFloorplan = project.FloorplanRevisions
            .First(fr => fr.RevisionNumber == 0);

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
            var baseAnnotationRevision = annotation.Revisions
                .FirstOrDefault(r => r.RevisionNumber == 0 && !r.IsDeleted);

            if (baseAnnotationRevision == null)
                continue;

            var transformedRawCoordinates = TransformCoordinatesFromInitial(
                baseAnnotationRevision.Type,
                baseAnnotationRevision.RawCoordinates,
                initialFloorplan.Width,
                initialFloorplan.Height,
                initialFloorplan.ScaleDenominator,
                request.ScaleDenominator,
                request.OffsetX,
                request.OffsetY);

            var normalizedCoordinates = NormalizeCoordinates(
                baseAnnotationRevision.Type,
                transformedRawCoordinates,
                request.Width,
                request.Height);

            var newAnnotationRevision = new AnnotationRevision
            {
                FloorplanRevision = newFloorplanRevision,
                RevisionNumber = newRevisionNumber,
                Source = AnnotationRevisionSourceType.AutoTransformed,
                Label = baseAnnotationRevision.Label,
                Description = baseAnnotationRevision.Description,
                Type = baseAnnotationRevision.Type,
                Color = baseAnnotationRevision.Color,
                RawCoordinates = transformedRawCoordinates,
                NormalizedCoordinates = normalizedCoordinates,
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

    private static double[] TransformCoordinatesFromInitial(
        AnnotationType type,
        double[] initialRawCoordinates,
        double initialWidth,
        double initialHeight,
        double initialScaleDenominator,
        double newScaleDenominator,
        double offsetX = 0,
        double offsetY = 0)
    {
      
        var scaleFactor = initialScaleDenominator / newScaleDenominator;

        return TransformRawCoordinates(
            type,
            initialRawCoordinates,
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

    private static double[] DenormalizeCoordinates(
        AnnotationType type,
        double[] normalizedCoordinates,
        double width,
        double height)
    {
        var numbers = normalizedCoordinates;

        switch (type)
        {
            case AnnotationType.Point:
            case AnnotationType.Label:
                {
                    if (numbers.Length != 2)
                        throw new InvalidOperationException("Point/Label requires x,y.");

                    var x = numbers[0] * width;
                    var y = numbers[1] * height;
                    return [x, y];
                }

            case AnnotationType.Rectangle:
                {
                    if (numbers.Length != 4)
                        throw new InvalidOperationException("Rectangle requires x,y,width,height.");

                    var x = numbers[0] * width;
                    var y = numbers[1] * height;
                    var w = numbers[2] * width;
                    var h = numbers[3] * height;
                    return [x, y, w, h];
                }

            case AnnotationType.Polygon:
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

    private static double[] NormalizeCoordinates(
        AnnotationType type,
        double[] pixelCoordinates,
        double width,
        double height)
    {
        var numbers = pixelCoordinates;

        switch (type)
        {
            case AnnotationType.Point:
            case AnnotationType.Label:
                {
                    if (numbers.Length != 2)
                        throw new InvalidOperationException("Point/Label requires x,y.");

                    var x = numbers[0] / width;
                    var y = numbers[1] / height;
                    return [x, y];
                }

            case AnnotationType.Rectangle:
                {
                    if (numbers.Length != 4)
                        throw new InvalidOperationException("Rectangle requires x,y,width,height.");

                    var x = numbers[0] / width;
                    var y = numbers[1] / height;
                    var w = numbers[2] / width;
                    var h = numbers[3] / height;
                    return [x, y, w, h];
                }

            case AnnotationType.Polygon:
                {
                    if (numbers.Length < 6 || numbers.Length % 2 != 0)
                        throw new InvalidOperationException("Polygon requires x,y pairs.");

                    var normalized = new List<double>();

                    for (var i = 0; i < numbers.Length; i += 2)
                    {
                        normalized.Add(numbers[i] / width);
                        normalized.Add(numbers[i + 1] / height);
                    }

                    return normalized.ToArray();
                }

            default:
                throw new NotSupportedException($"Unsupported annotation type: {type}");
        }
    }
}