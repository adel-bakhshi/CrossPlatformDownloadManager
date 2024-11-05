using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

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
        CancelCommand = ReactiveCommand.Create<Window?>(Cancel);
    }

    private async Task SaveAsync(Window? owner)
    {
        // TODO: Show message box
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
            Console.WriteLine(ex);
        }
    }

    private void Cancel(Window? owner)
    {
        // TODO: Show message box
        try
        {
            if (owner == null)
                return;

            if (!CurrentFileName.Equals(NewFileName))
            {
                // TODO: Ask user if he wants to save changes
            }

            owner.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public void SetFileNames()
    {
        var downloadFile = AppService
            .DownloadFileService
            .DownloadFiles
            .FirstOrDefault(df => df.Id == _downloadFile?.Id);

        if (downloadFile == null ||
            downloadFile.SaveLocation.IsNullOrEmpty() ||
            downloadFile.FileName.IsNullOrEmpty())
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
            downloadFile.SaveLocation.IsNullOrEmpty() ||
            downloadFile.FileName.IsNullOrEmpty())
        {
            return false;
        }

        var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
        if (!File.Exists(filePath))
            return false;

        var newFilePath = Path.Combine(downloadFile.SaveLocation!, NewFileName);
        var fileExtension = Path.GetExtension(newFilePath);
        if (fileExtension.IsNullOrEmpty())
        {
            fileExtension = Path.GetExtension(filePath);
            if (fileExtension.IsNullOrEmpty())
                throw new InvalidOperationException("An unknown error occured.");

            newFilePath += fileExtension;
        }

        if (!fileExtension.Equals(Path.GetExtension(filePath)))
        {
            // TODO: Ask user for continue with new fileExtension
        }

        File.Move(filePath, newFilePath);

        var categoryFileExtension = await AppService
            .UnitOfWork
            .CategoryFileExtensionRepository
            .GetAsync(where: fe => fe.Extension.ToLower() == fileExtension.ToLower(), includeProperties: "Category");

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