using System;
using Avalonia.Controls;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure;

/// <summary>
/// Base class of Windows
/// </summary>
/// <typeparam name="T">The ViewModel type of this Window</typeparam>
public class MyWindowBase<T> : Window where T : ViewModelBase
{
    protected T ViewModel
    {
        get
        {
            if (DataContext is T vm)
                return vm;

            vm = Activator.CreateInstance<T>();
            DataContext = vm;
            return vm;
        }
    }
}