using System.Windows;
using System.Windows.Controls;

namespace WoCo.Wpf.Controls;

/// <summary>
/// Interaction logic for ProjectTreeView.xaml
/// </summary>
public partial class ProjectTreeView : UserControl
{
    public ProjectTreeView()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(ProjectTreeView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    private void ProjectsTreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        SelectedItem = e.NewValue;
    }
}
