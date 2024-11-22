using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls.SettingsWindowControls;

public partial class ProxyView : MyUserControlBase<ProxyViewModel>
{
    public ProxyView()
    {
        InitializeComponent();
    }

    protected override async void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        try
        {
            base.OnPropertyChanged(change);

            if (change.Property != IsVisibleProperty || !IsVisible || ViewModel == null)
                return;

            await ViewModel.LoadAvailableProxiesAsync();
        }
        catch (Exception ex)
        {
            if (ViewModel == null)
            {
                Console.WriteLine(ex);
                return;
            }

            await ViewModel.ShowErrorDialogAsync(ex);
        }
    }
}