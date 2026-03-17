using System.Windows;
using System.Windows.Controls;

namespace WoCo.Wpf.Controls;

/// <summary>
/// Interaction logic for MetaInfoPanel.xaml
/// </summary>
public partial class MetaInfoPanel : UserControl
{
    public MetaInfoPanel()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(MetaInfoPanel),
            new PropertyMetadata(null, OnSelectedItemChanged));

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MetaInfoPanel panel)
        {
            panel.DataContext = e.NewValue;
        }
    }
}
