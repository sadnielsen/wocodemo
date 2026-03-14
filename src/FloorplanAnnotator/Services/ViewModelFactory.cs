using FloorplanAnnotator.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FloorplanAnnotator.Services;

public interface IViewModelFactory
{
    CreateProjectViewModel CreateCreateProjectViewModel();
}

public class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ViewModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public CreateProjectViewModel CreateCreateProjectViewModel()
    {
        return _serviceProvider.GetRequiredService<CreateProjectViewModel>();
    }
}