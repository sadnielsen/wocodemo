using System.Collections.Generic;
using System.Threading.Tasks;
using FloorplanAnnotator.Models;

namespace FloorplanAnnotator.Services
{
    public interface IProjectRepository
    {
        Task<List<Project>> GetAllProjectsAsync();
        Task<Project?> GetProjectByIdAsync(int id);
        Task<Project> AddProjectAsync(Project project);
        Task DeleteProjectAsync(int id);
    }
}
