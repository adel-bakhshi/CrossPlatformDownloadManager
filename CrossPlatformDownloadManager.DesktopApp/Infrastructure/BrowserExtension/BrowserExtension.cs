using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension;

public class BrowserExtension : IBrowserExtension
{
    #region Private Fields

    private readonly IAppService _appService;
    private readonly HttpListener _httpListener;

    #endregion

    public BrowserExtension(IAppService appService)
    {
        _appService = appService;

        // Initialize HttpListener
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add(Constants.AddDownloadFileUrl);
    }

    public async Task StartListeningAsync()
    {
        try
        {
            _httpListener.Start();
            Log.Information("Start listening for requests...");

            while (true)
            {
                var context = await _httpListener.GetContextAsync();
                _ = ProcessRequestAsync(context);
            }
        }
        catch (HttpListenerException ex)
        {
            // The I/O operation has been aborted because of either a thread exit or an application request.
            // This error occurs when the HttpListener is stopped and as a result the GetContextAsync method fails because it cannot find any Listener. This error does not need to be written to the application logs.
            if (ex.ErrorCode != 995)
                Log.Error(ex, "An error occurred while listening for requests.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start listening for requests.");
        }
    }

    public void StopListening()
    {
        try
        {
            _httpListener.Stop();
            Log.Information("Stop listening for requests...");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to stop listening for requests.");
        }
    }

    #region Helpers

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        var response = new ResponseViewModel
        {
            IsSuccessful = false,
            Message = string.Empty
        };

        try
        {
            // Check if browser extension is enabled
            var useBrowserExtension = _appService.SettingsService.Settings.UseBrowserExtension;
            if (!useBrowserExtension)
            {
                response.Message = "Browser extension is disabled. Please enable it and try again.";
                await SendResponseAsync(context, response);
                return;
            }

            // Make sure this is a POST request
            if (!context.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                response.Message = "We encountered an issue while processing your request. Please try again. If the problem persists, report it to be investigated.";
                await SendResponseAsync(context, response);
                return;
            }

            using var reader = new StreamReader(context.Request.InputStream);
            var json = await reader.ReadToEndAsync();
            if (json.IsNullOrEmpty())
            {
                response.Message = "Invalid data. Please retry. If the problem remains, report it for investigation.";
                await SendResponseAsync(context, response);
                return;
            }

            var requestViewModel = json.ConvertFromJson<RequestViewModel?>();
            if (requestViewModel == null)
            {
                response.Message = "Invalid data. Please retry. If the problem remains, report it for investigation.";
                await SendResponseAsync(context, response);
                return;
            }

            var urlIsValid = requestViewModel.Url.CheckUrlValidation();
            if (!urlIsValid)
            {
                response.Message = "CDM can't accept this URL.";
                await SendResponseAsync(context, response);
                return;
            }

            // Change URL to correct format
            requestViewModel.Url = requestViewModel.Url!.Replace('\\', '/').Trim();

            // Check for user option for showing start download dialog
            var showStartDownloadDialog = _appService.SettingsService.Settings.ShowStartDownloadDialog;
            // Go to AddDownloadLinkWindow (Start download dialog) and let user choose what he/she want
            if (showStartDownloadDialog)
            {
                ShowStartDownloadDialog(requestViewModel.Url);
            }
            // Otherwise, add link to database and start it
            else
            {
                var addResult = await AddNewDownloadFileAndStartItAsync(requestViewModel.Url);
                if (!addResult.IsSuccessful)
                {
                    response.Message = addResult.Message;
                    await SendResponseAsync(context, response);
                    return;
                }
            }

            response.IsSuccessful = true;
            response.Message = "Link added to CDM.";

            Log.Information($"Captured URL: {requestViewModel.Url}");
        }
        catch (Exception ex)
        {
            response.Message = $"An error occurred while trying to add link to CDM. Error message: {ex.Message}";
            Log.Error(ex, response.Message);
        }

        await SendResponseAsync(context, response);
    }

    private static async Task SendResponseAsync(HttpListenerContext context, ResponseViewModel response)
    {
        var json = response.ConvertToJson();

        var buffer = Encoding.UTF8.GetBytes(json);
        context.Response.ContentLength64 = buffer.Length;

        await context.Response.OutputStream.WriteAsync(buffer);
        context.Response.OutputStream.Close();
    }

    private void ShowStartDownloadDialog(string url)
    {
        var vm = new AddDownloadLinkWindowViewModel(_appService)
        {
            IsLoadingUrl = true,
            DownloadFile =
            {
                Url = url
            }
        };

        var window = new AddDownloadLinkWindow { DataContext = vm };
        window.Show();
    }

    private async Task<ResponseViewModel> AddNewDownloadFileAndStartItAsync(string url)
    {
        var result = new ResponseViewModel();

        // Get url details
        var urlDetails = await _appService.DownloadFileService.GetUrlDetailsAsync(url, CancellationToken.None);
        // Validate url details
        var validateUrlDetails = _appService.DownloadFileService.ValidateUrlDetails(urlDetails);
        if (!validateUrlDetails.IsValid)
        {
            result.IsSuccessful = validateUrlDetails.IsValid;
            result.Message = validateUrlDetails.Message;
            return result;
        }

        DuplicateDownloadLinkAction? duplicateAction = null;
        if (urlDetails.IsUrlDuplicate)
        {
            var savedDuplicateAction = _appService.SettingsService.Settings.DuplicateDownloadLinkAction;
            if (savedDuplicateAction == DuplicateDownloadLinkAction.LetUserChoose)
            {
                duplicateAction = await _appService
                    .DownloadFileService
                    .GetUserDuplicateActionAsync(urlDetails.Url, urlDetails.FileName, urlDetails.Category!.CategorySaveDirectory!.SaveDirectory);
            }
            else
            {
                duplicateAction = savedDuplicateAction;
            }
        }

        var downloadFile = new DownloadFileViewModel
        {
            Url = urlDetails.Url,
            FileName = urlDetails.FileName,
            CategoryId = urlDetails.Category?.Id,
            Size = urlDetails.FileSize
        };

        await _appService
            .DownloadFileService
            .AddDownloadFileAsync(downloadFile,
                isUrlDuplicate: urlDetails.IsUrlDuplicate,
                duplicateAction: duplicateAction,
                isFileNameDuplicate: urlDetails.IsFileNameDuplicate,
                startDownloading: true);

        result.IsSuccessful = true;
        return result;
    }

    #endregion
}