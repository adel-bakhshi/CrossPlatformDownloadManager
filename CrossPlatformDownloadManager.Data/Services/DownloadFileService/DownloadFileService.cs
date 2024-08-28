using System.Collections.ObjectModel;
using System.Globalization;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.Data.Services.DownloadFileService;

public class DownloadFileService : NotifyProperty, IDownloadFileService
{
    #region Private Fields

    private readonly IUnitOfWork _unitOfWork;

    #endregion

    #region Events

    public event EventHandler<List<DownloadFileViewModel>>? DataChanged;

    #endregion

    #region Properties

    private ObservableCollection<DownloadFileViewModel> _downloadFiles = [];

    public ObservableCollection<DownloadFileViewModel> DownloadFiles
    {
        get => _downloadFiles;
        set => SetField(ref _downloadFiles, value);
    }

    #endregion

    public DownloadFileService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;

        LoadFilesAsync().GetAwaiter().GetResult();
    }

    public async Task LoadFilesAsync()
    {
        try
        {
            var downloadFiles = await _unitOfWork.DownloadFileRepository
                .GetAllAsync(includeProperties: ["Category.FileExtensions", "DownloadQueue"]);

            var result = new List<DownloadFileViewModel>();
            foreach (var downloadFile in downloadFiles)
            {
                var vm = ConvertToDownloadFileViewModel(downloadFile);
                result.Add(vm);
            }

            DownloadFiles = result.ToObservableCollection();
            DataChanged?.Invoke(this, result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            DownloadFiles = new ObservableCollection<DownloadFileViewModel>();
        }
    }

    public async Task AddFileAsync(DownloadFile downloadFile)
    {
        await _unitOfWork.DownloadFileRepository.AddAsync(downloadFile);
        await _unitOfWork.SaveAsync();
        await LoadFilesAsync();
    }

    public async Task UpdateFileAsync(DownloadFile downloadFile)
    {
        await _unitOfWork.DownloadFileRepository.UpdateAsync(downloadFile);
        await _unitOfWork.SaveAsync();
        await LoadFilesAsync();
    }

    public async Task UpdateFilesAsync(List<DownloadFile> downloadFiles)
    {
        await _unitOfWork.DownloadFileRepository.UpdateAllAsync(downloadFiles);
        await _unitOfWork.SaveAsync();
        await LoadFilesAsync();
    }

    #region Helpers

    private DownloadFileViewModel ConvertToDownloadFileViewModel(DownloadFile downloadFile)
    {
        var ext = Path.GetExtension(downloadFile.FileName);
        var fileType =
            downloadFile.Category?.FileExtensions
                ?.FirstOrDefault(fe => fe.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase))?.Alias ??
            Constants.UnknownFileType;

        var vm = new DownloadFileViewModel
        {
            Id = downloadFile.Id,
            FileName = downloadFile.FileName,
            FileType = fileType,
            QueueId = downloadFile.DownloadQueue?.Id,
            QueueName = downloadFile.DownloadQueue?.Title ?? string.Empty,
            Size = downloadFile.Size == 0 ? null : downloadFile.Size,
            IsCompleted = Math.Abs(Math.Floor(downloadFile.DownloadProgress) - 100) == 0,
            IsDownloading = false,
            IsPaused = downloadFile.IsPaused,
            IsError = downloadFile.IsError,
            DownloadProgress = downloadFile.DownloadProgress == 0 ? null : downloadFile.DownloadProgress,
            TimeLeft = downloadFile.TimeLeft.GetShortTime(),
            TransferRate = downloadFile.TransferRate?.ToFileSize() ?? string.Empty,
            LastTryDate = downloadFile.LastTryDate?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            DateAdded = downloadFile.DateAdded.ToString(CultureInfo.InvariantCulture),
        };

        return vm;
    }

    #endregion
}