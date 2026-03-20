using NSubstitute;
using NUnit.Framework;
using WoCo.Core.DataAccess;
using WoCo.Core.Models;
using WoCo.Core.Services;
using WoCo.Core.Types;

namespace WoCo.Tests.Services;

[TestFixture]
public class CreateRevisionServiceTests
{
    private IProjectRepository _mockRepository = null!;
    private CreateRevisionService _service = null!;
    private string _testFloorplanPath = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = Substitute.For<IProjectRepository>();
        _service = new CreateRevisionService(_mockRepository);
        
        _testFloorplanPath = Path.Combine(Path.GetTempPath(), "test_floorplan.png");
        File.WriteAllBytes(_testFloorplanPath, [1, 2, 3, 4]);
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testFloorplanPath))
        {
            File.Delete(_testFloorplanPath);
        }
    }

    [Test]
    public async Task CreateAsync_WithValidRequest_CreatesNewRevision()
    {
        var projectId = Guid.NewGuid();
        var existingFloorplanRevision = new FloorplanRevision
        {
            Id = Guid.NewGuid(),
            RevisionNumber = 1,
            FileContent = [1, 2, 3],
            FileType = "png",
            FileName = "existing.png",
            CoordinateSystem = CoordinateSystemType.Pixels,
            Origin = CoordinateOriginType.TopLeft,
            Width = 1000,
            Height = 800,
            ScaleDenominator = 1.0,
            OffsetX = 0,
            OffsetY = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        var project = new Project
        {
            Id = projectId,
            Name = "Test Project",
            FloorplanRevisions = [existingFloorplanRevision],
            Annotations = []
        };

        _mockRepository.GetProjectByIdAsync(projectId).Returns(project);
        _mockRepository.UpdateProjectAsync(Arg.Any<Project>()).Returns(callInfo => callInfo.Arg<Project>());

        var request = new CreateRevisionRequest
        {
            ProjectId = projectId,
            FloorplanPath = _testFloorplanPath,
            Width = 1200,
            Height = 900,
            SourceCoordinateSystem = CoordinateSystemType.Pixels,
            SourceOrigin = CoordinateOriginType.TopLeft,
            ScaleDenominator = 1.0,
            OffsetX = 10,
            OffsetY = 20
        };

        var result = await _service.CreateAsync(request);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.FloorplanRevisions, Has.Count.EqualTo(2));
        Assert.That(result.FloorplanRevisions.Last().RevisionNumber, Is.EqualTo(2));
        Assert.That(result.FloorplanRevisions.Last().Width, Is.EqualTo(1200));
        Assert.That(result.FloorplanRevisions.Last().Height, Is.EqualTo(900));
        await _mockRepository.Received(1).UpdateProjectAsync(Arg.Any<Project>());
    }

    [Test]
    public void CreateAsync_WithEmptyProjectId_ThrowsInvalidOperationException()
    {
        var request = new CreateRevisionRequest
        {
            ProjectId = Guid.Empty,
            FloorplanPath = _testFloorplanPath,
            Width = 1000,
            Height = 800
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CreateAsync(request));
        Assert.That(ex!.Message, Does.Contain("Project ID is required"));
    }

    [Test]
    public void CreateAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        var request = new CreateRevisionRequest
        {
            ProjectId = Guid.NewGuid(),
            FloorplanPath = "nonexistent.png",
            Width = 1000,
            Height = 800
        };

        Assert.ThrowsAsync<FileNotFoundException>(async () => await _service.CreateAsync(request));
    }

    [Test]
    public void CreateAsync_WithZeroWidth_ThrowsInvalidOperationException()
    {
        var request = new CreateRevisionRequest
        {
            ProjectId = Guid.NewGuid(),
            FloorplanPath = _testFloorplanPath,
            Width = 0,
            Height = 800
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CreateAsync(request));
        Assert.That(ex!.Message, Does.Contain("Width must be greater than zero"));
    }

    [Test]
    public void CreateAsync_WithNegativeHeight_ThrowsInvalidOperationException()
    {
        var request = new CreateRevisionRequest
        {
            ProjectId = Guid.NewGuid(),
            FloorplanPath = _testFloorplanPath,
            Width = 1000,
            Height = -800
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CreateAsync(request));
        Assert.That(ex!.Message, Does.Contain("Height must be greater than zero"));
    }

    [Test]
    public void CreateAsync_WithZeroScaleDenominator_ThrowsInvalidOperationException()
    {
        var request = new CreateRevisionRequest
        {
            ProjectId = Guid.NewGuid(),
            FloorplanPath = _testFloorplanPath,
            Width = 1000,
            Height = 800,
            ScaleDenominator = 0
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CreateAsync(request));
        Assert.That(ex!.Message, Does.Contain("ScaleDenominator must be greater than zero"));
    }

    [Test]
    public async Task CreateAsync_WithNonExistentProject_ThrowsInvalidOperationException()
    {
        var projectId = Guid.NewGuid();
        _mockRepository.GetProjectByIdAsync(projectId).Returns((Project?)null);

        var request = new CreateRevisionRequest
        {
            ProjectId = projectId,
            FloorplanPath = _testFloorplanPath,
            Width = 1000,
            Height = 800
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CreateAsync(request));
        Assert.That(ex!.Message, Does.Contain($"Project with ID {projectId} not found"));
    }

    [Test]
    public async Task CreateAsync_TransformsPointAnnotationCorrectly()
    {
        var projectId = Guid.NewGuid();
        var existingFloorplanRevision = new FloorplanRevision
        {
            Id = Guid.NewGuid(),
            RevisionNumber = 1,
            FileContent = [1, 2, 3],
            FileType = "png",
            FileName = "existing.png",
            CoordinateSystem = CoordinateSystemType.Pixels,
            Origin = CoordinateOriginType.TopLeft,
            Width = 1000,
            Height = 800,
            ScaleDenominator = 1.0,
            OffsetX = 0,
            OffsetY = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        var annotation = new Annotation
        {
            Id = Guid.NewGuid(),
            Revisions = 
            [
                new AnnotationRevision
                {
                    RevisionNumber = 1,
                    Type = AnnotationType.Point,
                    Label = "Test Point",
                    Description = "Test Description",
                    Color = "#FF0000",
                    RawCoordinates = [100, 200],
                    IsDeleted = false,
                    Source = AnnotationRevisionSourceType.ManualAdded,
                    CreatedAtUtc = DateTime.UtcNow
                }
            ]
        };

        var project = new Project
        {
            Id = projectId,
            Name = "Test Project",
            FloorplanRevisions = [existingFloorplanRevision],
            Annotations = [annotation]
        };

        _mockRepository.GetProjectByIdAsync(projectId).Returns(project);
        _mockRepository.UpdateProjectAsync(Arg.Any<Project>()).Returns(callInfo => callInfo.Arg<Project>());

        var request = new CreateRevisionRequest
        {
            ProjectId = projectId,
            FloorplanPath = _testFloorplanPath,
            Width = 1000,
            Height = 800,
            ScaleDenominator = 2.0,
            OffsetX = 10,
            OffsetY = 20
        };

        var result = await _service.CreateAsync(request);

        var newAnnotationRevision = result.Annotations.First().Revisions.Last();
        Assert.That(newAnnotationRevision.RevisionNumber, Is.EqualTo(2));
        Assert.That(newAnnotationRevision.RawCoordinates[0], Is.EqualTo(60).Within(0.001));
        Assert.That(newAnnotationRevision.RawCoordinates[1], Is.EqualTo(120).Within(0.001));
        Assert.That(newAnnotationRevision.Source, Is.EqualTo(AnnotationRevisionSourceType.AutoTransformed));
    }

    [Test]
    public async Task CreateAsync_TransformsRectangleAnnotationCorrectly()
    {
        var projectId = Guid.NewGuid();
        var existingFloorplanRevision = new FloorplanRevision
        {
            Id = Guid.NewGuid(),
            RevisionNumber = 1,
            FileContent = [1, 2, 3],
            FileType = "png",
            FileName = "existing.png",
            CoordinateSystem = CoordinateSystemType.Pixels,
            Origin = CoordinateOriginType.TopLeft,
            Width = 1000,
            Height = 800,
            ScaleDenominator = 1.0,
            OffsetX = 0,
            OffsetY = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        var annotation = new Annotation
        {
            Id = Guid.NewGuid(),
            Revisions = 
            [
                new AnnotationRevision
                {
                    RevisionNumber = 1,
                    Type = AnnotationType.Rectangle,
                    Label = "Test Rectangle",
                    Description = "Test Description",
                    Color = "#00FF00",
                    RawCoordinates = [100, 200, 50, 75],
                    IsDeleted = false,
                    Source = AnnotationRevisionSourceType.ManualAdded,
                    CreatedAtUtc = DateTime.UtcNow
                }
            ]
        };

        var project = new Project
        {
            Id = projectId,
            Name = "Test Project",
            FloorplanRevisions = [existingFloorplanRevision],
            Annotations = [annotation]
        };

        _mockRepository.GetProjectByIdAsync(projectId).Returns(project);
        _mockRepository.UpdateProjectAsync(Arg.Any<Project>()).Returns(callInfo => callInfo.Arg<Project>());

        var request = new CreateRevisionRequest
        {
            ProjectId = projectId,
            FloorplanPath = _testFloorplanPath,
            Width = 1000,
            Height = 800,
            ScaleDenominator = 2.0,
            OffsetX = 10,
            OffsetY = 20
        };

        var result = await _service.CreateAsync(request);

        var newAnnotationRevision = result.Annotations.First().Revisions.Last();
        Assert.That(newAnnotationRevision.RevisionNumber, Is.EqualTo(2));
        Assert.That(newAnnotationRevision.RawCoordinates[0], Is.EqualTo(60).Within(0.001));
        Assert.That(newAnnotationRevision.RawCoordinates[1], Is.EqualTo(120).Within(0.001));
        Assert.That(newAnnotationRevision.RawCoordinates[2], Is.EqualTo(25).Within(0.001));
        Assert.That(newAnnotationRevision.RawCoordinates[3], Is.EqualTo(37.5).Within(0.001));
    }

    [Test]
    public async Task CreateAsync_TransformsPolygonAnnotationCorrectly()
    {
        var projectId = Guid.NewGuid();
        var existingFloorplanRevision = new FloorplanRevision
        {
            Id = Guid.NewGuid(),
            RevisionNumber = 1,
            FileContent = [1, 2, 3],
            FileType = "png",
            FileName = "existing.png",
            CoordinateSystem = CoordinateSystemType.Pixels,
            Origin = CoordinateOriginType.TopLeft,
            Width = 1000,
            Height = 800,
            ScaleDenominator = 1.0,
            OffsetX = 0,
            OffsetY = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        var annotation = new Annotation
        {
            Id = Guid.NewGuid(),
            Revisions = 
            [
                new AnnotationRevision
                {
                    RevisionNumber = 1,
                    Type = AnnotationType.Polygon,
                    Label = "Test Polygon",
                    Description = "Test Description",
                    Color = "#0000FF",
                    RawCoordinates = [100, 200, 150, 250, 200, 200],
                    IsDeleted = false,
                    Source = AnnotationRevisionSourceType.ManualAdded,
                    CreatedAtUtc = DateTime.UtcNow
                }
            ]
        };

        var project = new Project
        {
            Id = projectId,
            Name = "Test Project",
            FloorplanRevisions = [existingFloorplanRevision],
            Annotations = [annotation]
        };

        _mockRepository.GetProjectByIdAsync(projectId).Returns(project);
        _mockRepository.UpdateProjectAsync(Arg.Any<Project>()).Returns(callInfo => callInfo.Arg<Project>());

        var request = new CreateRevisionRequest
        {
            ProjectId = projectId,
            FloorplanPath = _testFloorplanPath,
            Width = 1000,
            Height = 800,
            ScaleDenominator = 2.0,
            OffsetX = 10,
            OffsetY = 20
        };

        var result = await _service.CreateAsync(request);

        var newAnnotationRevision = result.Annotations.First().Revisions.Last();
        Assert.That(newAnnotationRevision.RevisionNumber, Is.EqualTo(2));
        Assert.That(newAnnotationRevision.RawCoordinates, Has.Length.EqualTo(6));
        Assert.That(newAnnotationRevision.RawCoordinates[0], Is.EqualTo(60).Within(0.001));
        Assert.That(newAnnotationRevision.RawCoordinates[1], Is.EqualTo(120).Within(0.001));
        Assert.That(newAnnotationRevision.RawCoordinates[2], Is.EqualTo(85).Within(0.001));
        Assert.That(newAnnotationRevision.RawCoordinates[3], Is.EqualTo(145).Within(0.001));
        Assert.That(newAnnotationRevision.RawCoordinates[4], Is.EqualTo(110).Within(0.001));
        Assert.That(newAnnotationRevision.RawCoordinates[5], Is.EqualTo(120).Within(0.001));
    }

    [Test]
    public async Task CreateAsync_PreservesDeletedAnnotations()
    {
        var projectId = Guid.NewGuid();
        var existingFloorplanRevision = new FloorplanRevision
        {
            Id = Guid.NewGuid(),
            RevisionNumber = 1,
            FileContent = [1, 2, 3],
            FileType = "png",
            FileName = "existing.png",
            CoordinateSystem = CoordinateSystemType.Pixels,
            Origin = CoordinateOriginType.TopLeft,
            Width = 1000,
            Height = 800,
            ScaleDenominator = 1.0,
            OffsetX = 0,
            OffsetY = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        var annotation = new Annotation
        {
            Id = Guid.NewGuid(),
            Revisions = 
            [
                new AnnotationRevision
                {
                    RevisionNumber = 1,
                    Type = AnnotationType.Point,
                    Label = "Deleted Point",
                    Description = "Test Description",
                    Color = "#FF0000",
                    RawCoordinates = [100, 200],
                    IsDeleted = true,
                    Source = AnnotationRevisionSourceType.ManualAdded,
                    CreatedAtUtc = DateTime.UtcNow
                }
            ]
        };

        var project = new Project
        {
            Id = projectId,
            Name = "Test Project",
            FloorplanRevisions = [existingFloorplanRevision],
            Annotations = [annotation]
        };

        _mockRepository.GetProjectByIdAsync(projectId).Returns(project);
        _mockRepository.UpdateProjectAsync(Arg.Any<Project>()).Returns(callInfo => callInfo.Arg<Project>());

        var request = new CreateRevisionRequest
        {
            ProjectId = projectId,
            FloorplanPath = _testFloorplanPath,
            Width = 1000,
            Height = 800,
            ScaleDenominator = 1.0,
            OffsetX = 0,
            OffsetY = 0
        };

        var result = await _service.CreateAsync(request);

        var newAnnotationRevision = result.Annotations.First().Revisions.Last();
        Assert.That(newAnnotationRevision.RevisionNumber, Is.EqualTo(2));
        Assert.That(newAnnotationRevision.IsDeleted, Is.True);
        Assert.That(newAnnotationRevision.Source, Is.EqualTo(AnnotationRevisionSourceType.AutoTransformed));
    }

    [Test]
    public async Task CreateAsync_ReadsFloorplanFileContent()
    {
        var projectId = Guid.NewGuid();
        var existingFloorplanRevision = new FloorplanRevision
        {
            Id = Guid.NewGuid(),
            RevisionNumber = 1,
            FileContent = [1, 2, 3],
            FileType = "png",
            FileName = "existing.png",
            CoordinateSystem = CoordinateSystemType.Pixels,
            Origin = CoordinateOriginType.TopLeft,
            Width = 1000,
            Height = 800,
            ScaleDenominator = 1.0,
            OffsetX = 0,
            OffsetY = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        var project = new Project
        {
            Id = projectId,
            Name = "Test Project",
            FloorplanRevisions = [existingFloorplanRevision],
            Annotations = []
        };

        _mockRepository.GetProjectByIdAsync(projectId).Returns(project);
        _mockRepository.UpdateProjectAsync(Arg.Any<Project>()).Returns(callInfo => callInfo.Arg<Project>());

        var request = new CreateRevisionRequest
        {
            ProjectId = projectId,
            FloorplanPath = _testFloorplanPath,
            Width = 1000,
            Height = 800
        };

        var result = await _service.CreateAsync(request);

        var newFloorplanRevision = result.FloorplanRevisions.Last();
        Assert.That(newFloorplanRevision.FileContent, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.That(newFloorplanRevision.FileName, Is.EqualTo("test_floorplan.png"));
        Assert.That(newFloorplanRevision.FileType, Is.EqualTo("png"));
    }
}
