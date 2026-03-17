using WoCo.Core.Models;

namespace WoCo.Core.DataAccess;

public interface IProjectRepository
{
    Task<List<Project>> GetAllProjectsAsync();
    Task<Project?> GetProjectByIdAsync(Guid id);
    Task<Project> AddProjectAsync(Project project);
    Task DeleteProjectAsync(Guid id);
}