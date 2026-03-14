using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WoCo.Core.DataAccess;
using WoCo.Core.DependencyInjection;

namespace WoCo.Cli;

internal class Program
{
    static void Main(string[] args)
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

        DumpDatabase(db);
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
                Console.WriteLine($"    Size: {fp.SourceWidth} x {fp.SourceHeight}");
                Console.WriteLine($"    File: {fp.FloorplanPath}");
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
