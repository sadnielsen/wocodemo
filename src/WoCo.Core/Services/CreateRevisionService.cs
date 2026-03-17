using System.Globalization;
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
}

public sealed class CreateRevisionService
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

        var latestFloorplan = project.FloorplanRevisions
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
            CreatedAtUtc = now
        };

        project.FloorplanRevisions.Add(newFloorplanRevision);

        foreach (var annotation in project.Annotations)
        {
            var latestAnnotationRevision = annotation.Revisions
                .Where(r => r.RevisionNumber == latestRevisionNumber && !r.IsDeleted)
                .FirstOrDefault();

            if (latestAnnotationRevision == null)
                continue;

            var transformedCoordinates = TransformCoordinates(
                latestAnnotationRevision.Type,
                latestAnnotationRevision.NormalizedCoordinates,
                latestFloorplan.Width,
                latestFloorplan.Height,
                request.Width,
                request.Height);

            var newAnnotationRevision = new AnnotationRevision
            {
                FloorplanRevision = newFloorplanRevision,
                RevisionNumber = newRevisionNumber,
                Source = AnnotationRevisionSourceType.AutoTransformed,
                Label = latestAnnotationRevision.Label,
                Description = latestAnnotationRevision.Description,
                Type = latestAnnotationRevision.Type,
                Color = latestAnnotationRevision.Color,
                RawCoordinates = transformedCoordinates,
                NormalizedCoordinates = transformedCoordinates,
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
    }

    private static string TransformCoordinates(
        AnnotationType type,
        string normalizedCoordinates,
        double oldWidth,
        double oldHeight,
        double newWidth,
        double newHeight)
    {
        var numbers = normalizedCoordinates
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
            .ToArray();

        var scaleX = newWidth / oldWidth;
        var scaleY = newHeight / oldHeight;

        switch (type)
        {
            case AnnotationType.Point:
            case AnnotationType.Label:
            {
                if (numbers.Length != 2)
                    throw new InvalidOperationException("Point/Label requires x,y.");

                var x = numbers[0];
                var y = numbers[1];
                return $"{Fmt(x)},{Fmt(y)}";
            }

            case AnnotationType.Rectangle:
            {
                if (numbers.Length != 4)
                    throw new InvalidOperationException("Rectangle requires x,y,width,height.");

                var x = numbers[0];
                var y = numbers[1];
                var w = numbers[2];
                var h = numbers[3];
                return $"{Fmt(x)},{Fmt(y)},{Fmt(w)},{Fmt(h)}";
            }

            case AnnotationType.Polygon:
            {
                if (numbers.Length < 6 || numbers.Length % 2 != 0)
                    throw new InvalidOperationException("Polygon requires x,y pairs.");

                var transformed = new List<string>();

                for (var i = 0; i < numbers.Length; i += 2)
                {
                    transformed.Add(Fmt(numbers[i]));
                    transformed.Add(Fmt(numbers[i + 1]));
                }

                return string.Join(",", transformed);
            }

            default:
                throw new NotSupportedException($"Unsupported annotation type: {type}");
        }
    }

    private static string Fmt(double value)
        => value.ToString("0.######", CultureInfo.InvariantCulture);
}
