using Avalonia.Controls;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure;

/// <summary>
/// Base class of UserControls
/// </summary>
/// <typeparam name="T">The ViewModel type of this UserControl</typeparam>
public class MyUserControlBase<T> : UserControl where T : ViewModelBase
{
    protected T? ViewModel => DataContext as T;
}