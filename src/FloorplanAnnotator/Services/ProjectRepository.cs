using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FloorplanAnnotator.Models;
using Microsoft.EntityFrameworkCore;

namespace FloorplanAnnotator.Services
{
    public class ProjectRepository : IProjectRepository, IDisposable
    {
        private readonly AppDbContext _context;

        public ProjectRepository(string dbPath)
        {
            _context = new AppDbContext(dbPath);
            _context.Database.EnsureCreated();
        }

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            return await _context.Projects
                .Include(p => p.Annotations)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Project?> GetProjectByIdAsync(int id)
        {
            return await _context.Projects
                .Include(p => p.Annotations)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Project> AddProjectAsync(Project project)
        {
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return project;
        }

        public async Task DeleteProjectAsync(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
