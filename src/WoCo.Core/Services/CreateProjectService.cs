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
    public double ScaleDenominator { get; set; } = 1.0;
    public double OffsetX { get; set; } = 0.0;
    public double OffsetY { get; set; } = 0.0;
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
            ScaleDenominator = request.ScaleDenominator,
            OffsetX = request.OffsetX,
            OffsetY = request.OffsetY,
            CreatedAtUtc = now
        };

        project.FloorplanRevisions.Add(floorplanRevision);

        foreach (var imported in importedAnnotations)
        {
            var annotation = new Annotation
            {
                Id = Guid.NewGuid()
            };

            var annotationRevision = new AnnotationRevision
            {
                FloorplanRevision = floorplanRevision,
                RevisionNumber = floorplanRevision.RevisionNumber,
                Source = AnnotationRevisionSourceType.Imported,
                Label = imported.Label,
                Type = imported.Type,
                Color = imported.Color,
                RawCoordinates = imported.RawCoordinates,
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
}