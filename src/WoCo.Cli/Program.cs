using System.Drawing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WoCo.Core.DataAccess;
using WoCo.Core.DependencyInjection;
using WoCo.Core.Services;
using WoCo.Core.Types;

namespace WoCo.Cli;

internal class Program
{
    private record FloorplanMetaData
    {
        public double ScaleDenominator { get; init; } = 1.0;
        public double OffsetX { get; init; } = 0.0;
        public double OffsetY { get; init; } = 0.0;

        public static FloorplanMetaData FromJson(string jsonPath)
        {
            var jsonText = File.ReadAllText(jsonPath);
            var floorplanJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(jsonText);
            double scaleDenominator = 1.0;
            double offsetX = 0.0;
            double offsetY = 0.0;
            if (floorplanJson.TryGetProperty("scaleDenominator", out var scaleElement))
            {
                scaleDenominator = scaleElement.GetDouble();
            }
            if (floorplanJson.TryGetProperty("offsetX", out var offsetXElement))
            {
                offsetX = offsetXElement.GetDouble();
            }
            if (floorplanJson.TryGetProperty("offsetY", out var offsetYElement))
            {
                offsetY = offsetYElement.GetDouble();
            }
            return new FloorplanMetaData
            {
                ScaleDenominator = scaleDenominator,
                OffsetX = offsetX,
                OffsetY = offsetY
            };
        }
    }

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

        bool shouldPurge = ShouldPurge(args);
        var loadPrefix = GetLoadPrefix(args);

        if (shouldPurge)
        {
            await PurgeDatabaseAsync(db);
            Console.WriteLine("Database purged successfully.");
        }

        if (loadPrefix is not null)
        {
            var configuration = host.Services.GetRequiredService<IConfiguration>();
            var project = await CreateSampleProjectAsync(scope.ServiceProvider, configuration, loadPrefix);
            Console.WriteLine($"Imported project: {project.Name} ({project.Id})");

            // Load additional revisions
            await LoadAdditionalRevisionsAsync(scope.ServiceProvider, configuration, loadPrefix, project);
        }

        DumpDatabase(db);
    }

    private static bool ShouldPurge(string[] args)
        => args.Any(arg => string.Equals(arg, "purge", StringComparison.OrdinalIgnoreCase));

    private static string? GetLoadPrefix(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith("load=", StringComparison.OrdinalIgnoreCase))
            {
                return arg.Substring(5);
            }
        }
        return null;
    }

    private static async Task PurgeDatabaseAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("DELETE FROM AnnotationRevisions");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Annotations");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM FloorplanRevisions");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Projects");
    }

    private static async Task<WoCo.Core.Models.Project> CreateSampleProjectAsync(IServiceProvider serviceProvider, IConfiguration configuration, string prefix)
    {
        var dataDirectory = GetDataDirectory(configuration);
        var floorplanPath = Path.Combine(dataDirectory, $"{prefix}.0-floorplan.png");
        var annotationsPath = Path.Combine(dataDirectory, $"{prefix}.0-annotations.json");
        var floorplanJsonPath = Path.Combine(dataDirectory, $"{prefix}.0-floorplan.json");

        if (!File.Exists(floorplanPath))
        {
            throw new FileNotFoundException($"Floorplan file not found: {floorplanPath}");
        }

        if (!File.Exists(annotationsPath))
        {
            throw new FileNotFoundException($"Annotations file not found: {annotationsPath}");
        }

        if (!File.Exists(floorplanJsonPath))
        {
            throw new FileNotFoundException($"Floorplan metadata file not found: {floorplanJsonPath}");
        }

        var floorplanMetaData = FloorplanMetaData.FromJson(floorplanJsonPath);

        var (width, height) = ParseDimensions(floorplanPath);
       
        var createProjectService = serviceProvider.GetRequiredService<ICreateProjectService>();

        var request = new CreateProjectRequest
        {
            ProjectName = $"Sample Project {DateTime.UtcNow:yyyyMMddHHmmss}",
            FloorplanPath = floorplanPath,
            AnnotationsPath = annotationsPath,
            Width = width,
            Height = height,
            ScaleDenominator = floorplanMetaData.ScaleDenominator,
            OffsetX = floorplanMetaData.OffsetX,
            OffsetY = floorplanMetaData.OffsetY,
            SourceCoordinateSystem = CoordinateSystemType.Pixels,
            SourceOrigin = CoordinateOriginType.TopLeft
        };

        return await createProjectService.CreateAsync(request);
    }

    private static async Task LoadAdditionalRevisionsAsync(IServiceProvider serviceProvider, IConfiguration configuration, string prefix, WoCo.Core.Models.Project project)
    {
        var dataDirectory = GetDataDirectory(configuration);
        var createRevisionService = serviceProvider.GetRequiredService<ICreateRevisionService>();

        int revisionNumber = 1;

        while (true)
        {
            var floorplanPath = Path.Combine(dataDirectory, $"{prefix}.{revisionNumber}-floorplan.png");

            if (!File.Exists(floorplanPath))
            {
                break;
            }

            var floorplanJsonPath = Path.Combine(dataDirectory, $"{prefix}.{revisionNumber}-floorplan.json");
            
            if (!File.Exists(floorplanJsonPath))
            {
                throw new FileNotFoundException($"Floorplan metadata file not found: {floorplanJsonPath}");
            }

            Console.WriteLine($"Creating revision {revisionNumber} from {Path.GetFileName(floorplanPath)}");

            var (width, height) = ParseDimensions(floorplanPath);
            var floorplanMetaData = FloorplanMetaData.FromJson(floorplanJsonPath);

            var request = new CreateRevisionRequest
            {
                ProjectId = project.Id,
                FloorplanPath = floorplanPath,
                Width = width,
                Height = height,
                ScaleDenominator = floorplanMetaData.ScaleDenominator,
                OffsetX = floorplanMetaData.OffsetX,
                OffsetY = floorplanMetaData.OffsetY,
                SourceCoordinateSystem = CoordinateSystemType.Pixels,
                SourceOrigin = CoordinateOriginType.TopLeft
            };

            project = await createRevisionService.CreateAsync(request);
            Console.WriteLine($"Created revision {revisionNumber} for project {project.Name}");

            revisionNumber++;
        }

        if (revisionNumber > 1)
        {
            Console.WriteLine($"Successfully loaded {revisionNumber - 1} additional revision(s).");
        }
    }

    private static string GetDataDirectory(IConfiguration configuration)
    {
        var dataFolder = configuration["DataFolder"] ?? "Data";

        // Try relative path first
        if (Directory.Exists(dataFolder))
        {
            return Path.GetFullPath(dataFolder);
        }

        // Try relative to base directory
        var relativeToBase = Path.Combine(AppContext.BaseDirectory, dataFolder);
        if (Directory.Exists(relativeToBase))
        {
            return relativeToBase;
        }

        // Search upwards from base directory
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, dataFolder);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate the data directory '{dataFolder}'. Searched relative paths and upwards from {AppContext.BaseDirectory}");
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
                Console.WriteLine($"    ScaleDenominator: {fp.ScaleDenominator}");
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
                    var coords = string.Join(",", rev.RawCoordinates.Select(d => d.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)));
                    Console.WriteLine(
                        $"    Rev {rev.RevisionNumber} | {rev.Type} | ({coords}) | Deleted: {rev.IsDeleted} | CreatedAt: {rev.CreatedAtUtc}");
                }
            }

            Console.WriteLine();
        }

        Console.WriteLine("=== END ===");
    }
}
