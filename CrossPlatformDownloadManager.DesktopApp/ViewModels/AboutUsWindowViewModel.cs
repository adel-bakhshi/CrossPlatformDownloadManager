using System;
using System.Reflection;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class AboutUsWindowViewModel : ViewModelBase
{
    #region Private Fields

    private string _appVersion = string.Empty;
    private string _currentYear = string.Empty;
    private string _githubUrl = string.Empty;
    private string _telegramUrl = string.Empty;
    private string _emailAddress = string.Empty;

    #endregion

    #region Properties

    public string AppVersion
    {
        get => _appVersion;
        set => this.RaiseAndSetIfChanged(ref _appVersion, value);
    }

    public string CurrentYear
    {
        get => _currentYear;
        set => this.RaiseAndSetIfChanged(ref _currentYear, value);
    }
    
    public string GithubUrl
    {
        get => _githubUrl;
        set => this.RaiseAndSetIfChanged(ref _githubUrl, value);
    }

    public string TelegramUrl
    {
        get => _telegramUrl;
        set => this.RaiseAndSetIfChanged(ref _telegramUrl, value);
    }
    
    public string EmailAddress
    {
        get => _emailAddress;
        set => this.RaiseAndSetIfChanged(ref _emailAddress, value);
    }

    #endregion
    
    public AboutUsWindowViewModel(IAppService appService) : base(appService)
    {
        FillData();
    }

    private void FillData()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        if (!version.IsNullOrEmpty())
            AppVersion = version!;

        CurrentYear = DateTime.Now.Year.ToString();
        GithubUrl = Constants.GithubProjectUrl;
        TelegramUrl = Constants.TelegramUrl;
        EmailAddress = $"mailto:{Constants.Email}";
    }
}