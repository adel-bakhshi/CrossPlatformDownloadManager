using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        _httpListener.Prefixes.Add(Constants.GetFileTypesUrl);
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

            var requestedUrl = context.Request.Url?.AbsoluteUri;
            response = requestedUrl switch
            {
                Constants.GetFileTypesUrl => GetFileTypes(context),
                Constants.AddDownloadFileUrl => await AddDownloadFileUrlAsync(context),
                _ => response
            };
        }
        catch (Exception ex)
        {
            response.Message = $"An error occurred while trying to add link to CDM. Error message: {ex.Message}";
            Log.Error(ex, response.Message);
        }

        await SendResponseAsync(context, response);
    }

    private ResponseViewModel<List<string>> GetFileTypes(HttpListenerContext context)
    {
        var response = new ResponseViewModel<List<string>>
        {
            IsSuccessful = false,
            Message = string.Empty,
            Data = []
        };

        // Make sure this is a GET request
        var validateResult = ValidateRequestMethod(context, "GET");
        if (!validateResult.IsSuccessful)
        {
            response.IsSuccessful = validateResult.IsSuccessful;
            response.Message = validateResult.Message;
            return response;
        }

        var fileExtensions = _appService
            .CategoryService
            .Categories
            .SelectMany(c => c.FileExtensions)
            .Select(fe => fe.Extension)
            .ToList();

        response.IsSuccessful = true;
        response.Data = fileExtensions;
        return response;
    }

    private async Task<ResponseViewModel> AddDownloadFileUrlAsync(HttpListenerContext context)
    {
        var response = new ResponseViewModel
        {
            IsSuccessful = false,
            Message = string.Empty
        };

        // Make sure this is a POST request
        var validateResult = ValidateRequestMethod(context, "POST");
        if (!validateResult.IsSuccessful)
        {
            response.IsSuccessful = validateResult.IsSuccessful;
            response.Message = validateResult.Message;
            return response;
        }

        using var reader = new StreamReader(context.Request.InputStream);
        var json = await reader.ReadToEndAsync();
        if (json.IsNullOrEmpty())
        {
            response.Message = "Invalid data. Please retry. If the problem remains, report it for investigation.";
            return response;
        }

        var requestViewModel = json.ConvertFromJson<RequestViewModel?>();
        if (requestViewModel == null)
        {
            response.Message = "Invalid data. Please retry. If the problem remains, report it for investigation.";
            return response;
        }

        var urlIsValid = requestViewModel.Url.CheckUrlValidation();
        if (!urlIsValid)
        {
            response.Message = "CDM can't accept this URL.";
            return response;
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
                return response;
            }
        }

        // Log captured URL
        Log.Information($"Captured URL: {requestViewModel.Url}");

        response.IsSuccessful = true;
        response.Message = "Link added to CDM.";
        return response;
    }

    private static ResponseViewModel ValidateRequestMethod(HttpListenerContext context, string? httpMethod)
    {
        var response = new ResponseViewModel
        {
            IsSuccessful = false,
            Message = string.Empty
        };

        // Trim method
        httpMethod = httpMethod?.Trim();
        // Validate request method
        if (!context.Request.HttpMethod.Equals(httpMethod, StringComparison.OrdinalIgnoreCase))
        {
            response.Message = "We encountered an issue while processing your request. Please try again. If the problem persists, report it to be investigated.";
        }
        else
        {
            response.IsSuccessful = true;
        }

        return response;
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

        // Check for duplicate download file
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

        // Create a new download file
        var downloadFile = new DownloadFileViewModel
        {
            Url = urlDetails.Url,
            FileName = urlDetails.FileName,
            CategoryId = urlDetails.Category?.Id,
            Size = urlDetails.FileSize,
            IsSizeUnknown = urlDetails.IsFileSizeUnknown
        };

        // Add download file
        var addResult = await _appService
            .DownloadFileService
            .AddDownloadFileAsync(downloadFile,
                isUrlDuplicate: urlDetails.IsUrlDuplicate,
                duplicateAction: duplicateAction,
                isFileNameDuplicate: urlDetails.IsFileNameDuplicate,
                startDownloading: true);

        // Check download file added or not
        result.IsSuccessful = addResult != null;
        return result;
    }

    #endregion
}