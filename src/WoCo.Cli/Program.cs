using System.Drawing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WoCo.Core.DataAccess;
using WoCo.Core.DependencyInjection;
using WoCo.Core.Services;
using WoCo.Core.Types;

namespace WoCo.Cli;

internal class Program
{
    static async Task Main(string[] args)
    {
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.ConfigureWoCoServices();
            })
            .Build();

        using var scope = host.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = factory.CreateDbContext();

        db.Database.Migrate();

        if (ShouldImportSample(args))
        {
            var project = await CreateSampleProjectAsync(scope.ServiceProvider);
            Console.WriteLine($"Imported project: {project.Name} ({project.Id})");
        }
        else
        {
            Console.WriteLine("Sample import skipped. Pass 'import-sample' to create a project from the data folder.");
        }

        DumpDatabase(db);
    }

    private static bool ShouldImportSample(string[] args)
        => args.Any(arg => string.Equals(arg, "import-sample", StringComparison.OrdinalIgnoreCase));

    private static async Task<WoCo.Core.Models.Project> CreateSampleProjectAsync(IServiceProvider serviceProvider)
    {
        var dataDirectory = FindDataDirectory();
        var floorplanPath = Path.Combine(dataDirectory, "floorplan-1.0-1200x600.png");
        var annotationsPath = Path.Combine(dataDirectory, "floorplan-1.0.json");
        var (width, height) = ParseDimensions(floorplanPath);

        var createProjectService = serviceProvider.GetRequiredService<CreateProjectService>();

        var request = new CreateProjectRequest
        {
            ProjectName = $"Sample Project {DateTime.UtcNow:yyyyMMddHHmmss}",
            FloorplanPath = floorplanPath,
            AnnotationsPath = annotationsPath,
            Width = width,
            Height = height,
            SourceCoordinateSystem = CoordinateSystemType.Pixels,
            SourceOrigin = CoordinateOriginType.TopLeft
        };

        return await createProjectService.CreateAsync(request);
    }

    private static string FindDataDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "data");
            var floorplanPath = Path.Combine(candidate, "floorplan-1.0-1200x600.png");
            var annotationsPath = Path.Combine(candidate, "floorplan-1.0.json");

            if (Directory.Exists(candidate) &&
                File.Exists(floorplanPath) &&
                File.Exists(annotationsPath))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the workspace data directory.");
    }

    private static (double Width, double Height) ParseDimensions(string floorplanPath)
    {
        using var bitmap = new Bitmap(floorplanPath);
        return (bitmap.Width, bitmap.Height);
    }

    private static void DumpDatabase(AppDbContext db)
    {
        Console.WriteLine($"Using DB: {db.Database.GetDbConnection().DataSource}");

        var projects = db.Projects
            .ToList();

        Console.WriteLine("=== DATABASE DUMP ===");
        Console.WriteLine();

        foreach (var project in projects)
        {
            Console.WriteLine($"Project: {project.Name} ({project.Id})");

            var floorplans = db.FloorplanRevisions
                .Where(x => x.ProjectId == project.Id)
                .OrderBy(x => x.RevisionNumber)
                .ToList();

            foreach (var fp in floorplans)
            {
                Console.WriteLine($"  FloorPlanRevision v{fp.RevisionNumber}");
                Console.WriteLine($"    Size: {fp.Width} x {fp.Height}");
                Console.WriteLine($"    FileName: {fp.FileName}");
                Console.WriteLine($"    FileType: {fp.FileType}");
                Console.WriteLine($"    FileBytes: {fp.FileContent.Length}");
            }

            var annotations = db.Annotations
                .Where(x => x.ProjectId == project.Id)
                .ToList();

            foreach (var annotation in annotations)
            {
                Console.WriteLine($"  Annotation: {annotation.Id}");

                var revisions = db.AnnotationRevisions
                    .Where(x => x.AnnotationId == annotation.Id)
                    .OrderBy(x => x.RevisionNumber)
                    .ToList();

                foreach (var rev in revisions)
                {
                    Console.WriteLine(
                        $"    Rev {rev.RevisionNumber} | {rev.Type} | ({rev.RawCoordinates}) | Deleted: {rev.IsDeleted} | CreatedAt: {rev.CreatedAtUtc}");
                }
            }

            Console.WriteLine();
        }

        Console.WriteLine("=== END ===");
    }
}
