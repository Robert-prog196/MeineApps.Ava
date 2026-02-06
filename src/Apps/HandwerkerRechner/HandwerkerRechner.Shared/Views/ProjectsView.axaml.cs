using Avalonia.Controls;
using HandwerkerRechner.ViewModels;

namespace HandwerkerRechner.Views;

public partial class ProjectsView : UserControl
{
    public ProjectsView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (DataContext is ProjectsViewModel vm)
        {
            vm.LoadProjectsCommand.Execute(null);
        }
    }
}
