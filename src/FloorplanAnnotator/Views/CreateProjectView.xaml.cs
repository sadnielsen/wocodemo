using FloorplanAnnotator.ViewModels;
using System.Windows;

namespace FloorplanAnnotator.Views
{
    public partial class CreateProjectView : Window
    {
        public CreateProjectView(CreateProjectViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
