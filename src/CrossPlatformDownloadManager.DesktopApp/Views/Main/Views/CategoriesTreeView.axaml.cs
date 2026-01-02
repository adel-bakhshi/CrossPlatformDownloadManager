using Avalonia.Interactivity;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.Main.Views;

namespace CrossPlatformDownloadManager.DesktopApp.Views.Main.Views;

public partial class CategoriesTreeView : MyUserControlBase<CategoriesTreeViewModel>
{
    public CategoriesTreeView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Select first item in the list
        if (CategoriesTreeItemViewsLibBox.Items.Count > 0)
            CategoriesTreeItemViewsLibBox.SelectedIndex = 0;
    }
}