using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.DesktopApp.CustomControls;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class ChangeFileNameWindow : MyWindowBase<ChangeFileNameWindowViewModel>
{
    public ChangeFileNameWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        // TODO: Show message box
        try
        {
            base.OnLoaded(e);

            ViewModel?.SetFileNames();

            var textBox = this.FindControl<CustomTextBox>("NewFileNameTextBox");
            textBox?.Focus();
            textBox?.SelectAll();
        }
        catch (Exception ex)
        {
            Close();
        }
    }
}