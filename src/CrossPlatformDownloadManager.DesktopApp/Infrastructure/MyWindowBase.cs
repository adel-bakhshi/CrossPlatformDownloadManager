using Avalonia.Controls;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure;

/// <summary>
/// Base class of Windows
/// </summary>
/// <typeparam name="T">The ViewModel type of this Window</typeparam>
public class MyWindowBase<T> : Window where T : ViewModelBase
{
    protected T? ViewModel => DataContext as T;
}