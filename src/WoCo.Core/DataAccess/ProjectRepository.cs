using Microsoft.EntityFrameworkCore;
using WoCo.Core.Models;

namespace WoCo.Core.DataAccess;

internal class ProjectRepository : IProjectRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ProjectRepository(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<Project>> GetAllProjectsAsync()
    {
        using var _context = CreateContext;

        return await _context.Projects
            .Include(p => p.FloorplanRevisions)
                .ThenInclude(fr => fr.AnnotationRevisions)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<Project?> GetProjectByIdAsync(Guid id)
    {
        var _context = CreateContext;

        return await _context.Projects
            .Include(p => p.FloorplanRevisions)
                .ThenInclude(fr => fr.AnnotationRevisions)
            .Include(p => p.Annotations)
                .ThenInclude(a => a.Revisions)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project> AddProjectAsync(Project project)
    {
        var _context = CreateContext;

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async Task<Project> UpdateProjectAsync(Project project)
    {
        var _context = CreateContext;

        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async Task DeleteProjectAsync(Guid id)
    {
        var _context = CreateContext;
        var project = await _context.Projects.FindAsync(id);
        if (project != null)
        {
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }
    }

    private AppDbContext CreateContext => _factory.CreateDbContext();
    
}