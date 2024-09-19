using System;
using Avalonia.Controls;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure;

/// <summary>
/// Base class of UserControls
/// </summary>
/// <typeparam name="T">The ViewModel type of this UserControl</typeparam>
public class MyUserControlBase<T> : UserControl where T : ViewModelBase
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