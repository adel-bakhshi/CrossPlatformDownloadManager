using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class ChangeFileNameWindowViewModel : ViewModelBase
{
    #region Private Fields

    private readonly DownloadFileViewModel? _downloadFile;

    private string _currentFileName = string.Empty;
    private string _newFileName = string.Empty;

    #endregion

    #region Properties

    public string CurrentFileName
    {
        get => _currentFileName;
        set => this.RaiseAndSetIfChanged(ref _currentFileName, value);
    }

    public string NewFileName
    {
        get => _newFileName;
        set => this.RaiseAndSetIfChanged(ref _newFileName, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    #endregion

    public ChangeFileNameWindowViewModel(IAppService appService, DownloadFileViewModel? downloadFile) : base(appService)
    {
        _downloadFile = downloadFile;

        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
        CancelCommand = ReactiveCommand.CreateFromTask<Window?>(CancelAsync);
    }

    private async Task SaveAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            var result = await SaveFileNameAsync();
            if (!result)
                return;

            owner.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to close the window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task CancelAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            if (!CurrentFileName.Equals(NewFileName))
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("Change file name",
                    "Are you sure you want to discard the changes?",
                    DialogButtons.YesNoCancel);

                switch (result)
                {
                    case DialogResult.No:
                    {
                        await SaveAsync(owner);
                        return;
                    }

                    case DialogResult.Cancel:
                    {
                        return;
                    }
                }
            }

            owner.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to close the window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    public void SetFileNames()
    {
        var downloadFile = AppService
            .DownloadFileService
            .DownloadFiles
            .FirstOrDefault(df => df.Id == _downloadFile?.Id);

        if (downloadFile == null ||
            downloadFile.SaveLocation.IsStringNullOrEmpty() ||
            downloadFile.FileName.IsStringNullOrEmpty())
        {
            throw new InvalidOperationException("Download file not found.");
        }

        CurrentFileName = NewFileName = downloadFile.FileName!;
    }

    #region Helpers

    private async Task<bool> SaveFileNameAsync()
    {
        var downloadFile = AppService
            .DownloadFileService
            .DownloadFiles
            .FirstOrDefault(df => df.Id == _downloadFile?.Id);

        if (downloadFile == null ||
            downloadFile.SaveLocation.IsStringNullOrEmpty() ||
            downloadFile.FileName.IsStringNullOrEmpty())
        {
            return false;
        }

        var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
        if (!File.Exists(filePath))
            return false;

        var newFilePath = Path.Combine(downloadFile.SaveLocation!, NewFileName);
        var fileExtension = Path.GetExtension(newFilePath);
        if (fileExtension.IsStringNullOrEmpty())
        {
            fileExtension = Path.GetExtension(filePath);
            if (fileExtension.IsStringNullOrEmpty())
                throw new InvalidOperationException("An error occurred while trying to get file extension.");

            newFilePath += fileExtension;
        }

        var originalFileExtension = Path.GetExtension(filePath);
        if (!fileExtension.Equals(originalFileExtension))
        {
            var result = await DialogBoxManager.ShowWarningDialogAsync("Change file extension",
                "Are you sure you want to change the file extension?",
                DialogButtons.YesNo);

            if (result == DialogResult.No)
            {
                newFilePath = newFilePath.Substring(0, newFilePath.Length - fileExtension.Length) + originalFileExtension;
            }
        }

        await filePath.MoveFileAsync(newFilePath);

        var categoryFileExtension = AppService
            .CategoryService
            .Categories
            .SelectMany(c => c.FileExtensions)
            .FirstOrDefault(fe => fe.Extension.Equals(fileExtension, StringComparison.OrdinalIgnoreCase));

        if (categoryFileExtension?.Category != null && categoryFileExtension.Category.Id != downloadFile.CategoryId)
            downloadFile.CategoryId = categoryFileExtension.Category!.Id;

        downloadFile.FileName = NewFileName;

        await AppService
            .DownloadFileService
            .UpdateDownloadFileAsync(downloadFile);

        return true;
    }

    #endregion
}