using System.Collections.ObjectModel;
using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using PropertyChanged;

namespace CrossPlatformDownloadManager.Data.Services.DownloadQueueService;

[AddINotifyPropertyChangedInterface]
public class DownloadQueueService : IDownloadQueueService
{
    #region Private Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    #endregion

    #region Events

    public event EventHandler? DataChanged;

    #endregion

    #region Properties

    public ObservableCollection<DownloadQueueViewModel> DownloadQueues { get; }

    #endregion

    public DownloadQueueService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;

        DownloadQueues = [];
    }

    public async Task LoadDownloadQueuesAsync(bool addDefaultDownloadQueue = false)
    {
        if (addDefaultDownloadQueue)
            await AddDefaultDownloadQueueAsync();

        var downloadQueues = await _unitOfWork
            .DownloadQueueRepository
            .GetAllAsync();

        var primaryKeys = downloadQueues
            .Select(df => df.Id)
            .ToList();

        var exceptDownloadQueues = DownloadQueues
            .Where(df => !primaryKeys.Contains(df.Id))
            .ToList();

        foreach (var downloadQueue in exceptDownloadQueues)
            await DeleteDownloadQueueAsync(downloadQueue);

        var downloadQueueViewModels = _mapper.Map<List<DownloadQueueViewModel>>(downloadQueues);
        foreach (var downloadQueue in downloadQueueViewModels)
        {
            var oldDownloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadQueue.Id);
            if (oldDownloadQueue != null)
                UpdateDownloadQueueViewModel(oldDownloadQueue, downloadQueue);
            else
                DownloadQueues.Add(downloadQueue);
        }

        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task AddNewDownloadQueueAsync(DownloadQueue downloadQueue, bool reloadData = true)
    {
        await _unitOfWork.DownloadQueueRepository.AddAsync(downloadQueue);
        await _unitOfWork.SaveAsync();

        if (reloadData)
            await LoadDownloadQueuesAsync();
    }

    public async Task DeleteDownloadQueueAsync(DownloadQueueViewModel downloadQueueViewModel)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateDownloadQueueAsync(DownloadQueueViewModel downloadQueueViewModel)
    {
        throw new NotImplementedException();
    }

    public async Task StartDownloadQueueAsync(DownloadQueueViewModel downloadQueueView)
    {
    }

    public async Task StopDownloadQueueAsync(DownloadQueueViewModel downloadQueueViewModel)
    {
        throw new NotImplementedException();
    }

    public async Task AddDownloadFileToDownloadQueueAsync(DownloadQueueViewModel downloadQueueViewModel,
        DownloadFileViewModel downloadFileViewModel)
    {
        throw new NotImplementedException();
    }

    public async Task RemoveDownloadFileFromDownloadQueueAsync(DownloadQueueViewModel downloadQueueViewModel,
        DownloadFileViewModel downloadFileViewModel)
    {
        throw new NotImplementedException();
    }

    #region Helpers

    private void UpdateDownloadQueueViewModel(DownloadQueueViewModel? oldDownloadQueue,
        DownloadQueueViewModel? newDownloadQueue)
    {
        if (oldDownloadQueue == null || newDownloadQueue == null)
            return;

        var properties = newDownloadQueue
            .GetType()
            .GetProperties()
            .Where(pi => !pi.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) && pi.CanWrite)
            .ToList();

        foreach (var property in properties)
        {
            var value = property.GetValue(newDownloadQueue);
            property.SetValue(oldDownloadQueue, value);
        }
    }

    private async Task AddDefaultDownloadQueueAsync()
    {
        var downloadQueueInDb = await _unitOfWork
            .DownloadQueueRepository
            .GetAsync(where: dq => dq.Title.ToLower() == Constants.DefaultDownloadQueueTitle.ToLower());

        if (downloadQueueInDb != null)
            return;

        var downloadQueue = new DownloadQueue
        {
            Title = Constants.DefaultDownloadQueueTitle,
            RetryOnDownloadingFailed = true,
            RetryCount = 3,
            ShowAlarmWhenDone = true,
        };

        await AddNewDownloadQueueAsync(downloadQueue, reloadData: false);
    }

    #endregion
}