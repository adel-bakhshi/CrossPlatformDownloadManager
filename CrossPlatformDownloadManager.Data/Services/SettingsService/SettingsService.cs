using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using PropertyChanged;

namespace CrossPlatformDownloadManager.Data.Services.SettingsService;

[AddINotifyPropertyChangedInterface]
public class SettingsService : ISettingsService
{
    #region Private Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    #endregion

    #region Properties

    public SettingsViewModel Settings { get; private set; }

    #endregion

    public SettingsService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;

        Settings = new SettingsViewModel();
    }

    public async Task LoadSettingsAsync()
    {
        // TODO: Show message box
        try
        {
            var settingsList = await _unitOfWork.SettingsRepository.GetAllAsync();
            Settings? settings = null;
            if (settingsList.Count == 0)
            {
                var assetName = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/settings.json";
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

            var settingsViewModel = _mapper.Map<SettingsViewModel>(settings);
            UpdateSettingsData(settingsViewModel);
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

    #region Helpers

    private void UpdateSettingsData(SettingsViewModel settings)
    {
        var properties = Settings
            .GetType()
            .GetProperties()
            .Where(p => p.CanWrite)
            .ToList();

        foreach (var property in properties)
            property.SetValue(Settings, property.GetValue(settings, null));
    }

    #endregion
}