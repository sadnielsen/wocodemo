using WoCo.Core.Models;

namespace WoCo.Core.Services;

public interface ICreateRevisionService
{
    Task<Project> CreateAsync(CreateRevisionRequest request);
}
