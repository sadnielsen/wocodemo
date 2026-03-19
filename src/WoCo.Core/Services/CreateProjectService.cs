using System.Globalization;
using WoCo.Core.DataAccess;
using WoCo.Core.Models;
using WoCo.Core.Types;

namespace WoCo.Core.Services;

public sealed class CreateProjectRequest
{
    public string ProjectName { get; init; } = string.Empty;
    public string FloorplanPath { get; init; } = string.Empty;
    public string AnnotationsPath { get; init; } = string.Empty;
    public double Width { get; set; }
    public double Height { get; set; }
    public CoordinateSystemType SourceCoordinateSystem { get; init; } = CoordinateSystemType.Pixels;
    public CoordinateOriginType SourceOrigin { get; init; } = CoordinateOriginType.TopLeft;
}

public sealed class CreateProjectService : ICreateProjectService
{
    private readonly IProjectRepository _projectRepository;

    public CreateProjectService(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Project> CreateAsync(CreateProjectRequest request)
    {
        Validate(request);

        var importedAnnotations = AnnotationImportParser.ParseFromFile(request.AnnotationsPath);
        var floorplanFileContent = File.ReadAllBytes(request.FloorplanPath);
        var floorplanFileName = Path.GetFileName(request.FloorplanPath);
        var floorplanFileType = Path.GetExtension(request.FloorplanPath).TrimStart('.').ToLowerInvariant();

        var now = DateTime.UtcNow;

        var project = new Project
        {
            Name = request.ProjectName.Trim(),
            CreatedAtUtc = now
        };

        var floorplanRevision = new FloorplanRevision
        {
            RevisionNumber = 0,
            FileContent = floorplanFileContent,
            FileType = floorplanFileType,
            FileName = floorplanFileName,
            CoordinateSystem = request.SourceCoordinateSystem,
            Origin = request.SourceOrigin,
            Width = request.Width,
            Height = request.Height,
            CreatedAtUtc = now
        };

        project.FloorplanRevisions.Add(floorplanRevision);

        foreach (var imported in importedAnnotations)
        {
            var annotation = new Annotation
            {
                Id = Guid.NewGuid()
            };

            var normalizedCoordinates = NormalizeCoordinates(
                imported.Type,
                imported.RawCoordinates,
                floorplanRevision.Width,
                floorplanRevision.Height,
                request.SourceCoordinateSystem,
                request.SourceOrigin);

            var annotationRevision = new AnnotationRevision
            {
                FloorplanRevision = floorplanRevision,
                RevisionNumber = floorplanRevision.RevisionNumber,
                Source = AnnotationRevisionSourceType.Imported,
                Label = imported.Label,
                Type = imported.Type,
                Color = imported.Color,
                RawCoordinates = imported.RawCoordinates,
                NormalizedCoordinates = normalizedCoordinates,
                IsDeleted = false,
                CreatedAtUtc = now
            };

            annotation.Revisions.Add(annotationRevision);
            project.Annotations.Add(annotation);
        }

        return await _projectRepository.AddProjectAsync(project);
    }

    private static void Validate(CreateProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectName))
            throw new InvalidOperationException("Project name is required.");

        if (!File.Exists(request.FloorplanPath))
            throw new FileNotFoundException("Floorplan file not found.", request.FloorplanPath);

        if (!File.Exists(request.AnnotationsPath))
            throw new FileNotFoundException("Annotations file not found.", request.AnnotationsPath);
    }

    private static double[] NormalizeCoordinates(
        AnnotationType type,
        double[] rawCoordinates,
        double sourceWidth,
        double sourceHeight,
        CoordinateSystemType coordinateSystem,
        CoordinateOriginType origin)
    {
        var numbers = rawCoordinates;

        if (coordinateSystem == CoordinateSystemType.Normalized &&
            origin == CoordinateOriginType.TopLeft)
        {
            return numbers;
        }

        switch (type)
        {
            case AnnotationType.Point:
            case AnnotationType.Label:
            {
                if (numbers.Length != 2)
                    throw new InvalidOperationException("Point/Label requires x,y.");

                var x = NormalizeX(numbers[0], sourceWidth, coordinateSystem);
                var y = NormalizeY(numbers[1], sourceHeight, coordinateSystem, origin);
                return [x, y];
            }

            case AnnotationType.Rectangle:
            {
                if (numbers.Length != 4)
                    throw new InvalidOperationException("Rectangle requires x,y,width,height.");

                var x = NormalizeX(numbers[0], sourceWidth, coordinateSystem);
                var y = NormalizeY(numbers[1], sourceHeight, coordinateSystem, origin);
                var w = NormalizeLength(numbers[2], sourceWidth, coordinateSystem);
                var h = NormalizeLength(numbers[3], sourceHeight, coordinateSystem);
                return [x, y, w, h];
            }

            case AnnotationType.Polygon:
            {
                if (numbers.Length < 6 || numbers.Length % 2 != 0)
                    throw new InvalidOperationException("Polygon requires x,y pairs.");

                var normalized = new List<double>();

                for (var i = 0; i < numbers.Length; i += 2)
                {
                    normalized.Add(NormalizeX(numbers[i], sourceWidth, coordinateSystem));
                    normalized.Add(NormalizeY(numbers[i + 1], sourceHeight, coordinateSystem, origin));
                }

                return normalized.ToArray();
            }

            default:
                throw new NotSupportedException($"Unsupported annotation type: {type}");
        }
    }

    private static double NormalizeX(double value, double width, CoordinateSystemType system)
        => system switch
        {
            CoordinateSystemType.Pixels => value / width,
            CoordinateSystemType.Millimeters => value / width,
            CoordinateSystemType.Normalized => value,
            _ => throw new NotSupportedException()
        };

    private static double NormalizeY(double value, double height, CoordinateSystemType system, CoordinateOriginType origin)
    {
        var normalized = system switch
        {
            CoordinateSystemType.Pixels => value / height,
            CoordinateSystemType.Millimeters => value / height,
            CoordinateSystemType.Normalized => value,
            _ => throw new NotSupportedException()
        };

        return origin switch
        {
            CoordinateOriginType.TopLeft => normalized,
            CoordinateOriginType.BottomLeft => 1.0 - normalized,
            _ => throw new NotSupportedException()
        };
    }

    private static double NormalizeLength(double value, double size, CoordinateSystemType system)
        => system switch
        {
            CoordinateSystemType.Pixels => value / size,
            CoordinateSystemType.Millimeters => value / size,
            CoordinateSystemType.Normalized => value,
            _ => throw new NotSupportedException()
        };
}