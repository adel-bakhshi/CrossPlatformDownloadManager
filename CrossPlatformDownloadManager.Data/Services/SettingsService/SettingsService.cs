using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.Services.SettingsService;

public class SettingsService : PropertyChangedBase, ISettingsService
{
    #region Private Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    private SettingsViewModel? _settings;

    #endregion

    #region Properties

    public SettingsViewModel? Settings
    {
        get => _settings;
        private set => SetField(ref _settings, value);
    }

    #endregion

    public SettingsService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task LoadSettingsAsync()
    {
        // TODO: Show message box
        try
        {
            if (Settings != null)
                return;
            
            var settingsList = await _unitOfWork.SettingsRepository.GetAllAsync();
            Settings? settings;
            if (settingsList.Count == 0)
            {
                const string assetName = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/settings.json";
                var assetsUri = new Uri(assetName);
                settings = assetsUri.OpenJsonAsset<Settings>();
                if (settings == null)
                    throw new InvalidOperationException("An error occurred while loading settings.");

                await _unitOfWork.SettingsRepository.AddAsync(settings);
                await _unitOfWork.SaveAsync();
            }
            else
            {
                settings = settingsList.First();
            }

            Settings = _mapper.Map<SettingsViewModel>(settings);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public async Task SaveSettingsAsync(SettingsViewModel settings)
    {
        // TODO: Show message box
        try
        {
            await LoadSettingsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}