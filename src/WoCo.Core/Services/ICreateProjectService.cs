using WoCo.Core.Models;

namespace WoCo.Core.Services;

public interface ICreateProjectService
{
    Task<Project> CreateAsync(CreateProjectRequest request);
}
