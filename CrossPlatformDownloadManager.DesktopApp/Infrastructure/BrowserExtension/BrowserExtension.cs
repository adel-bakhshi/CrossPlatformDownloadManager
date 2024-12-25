using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels.BrowserExtensions;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
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
        _httpListener.Prefixes.Add(Constants.CheckFileTypeSupportUrl);
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
        // Initialize variables
        var request = context.Request;
        var response = context.Response;
        string? json;
        byte[] buffer;
        var result = new ResponseViewModel();

        // Check if browser extension is enabled
        var useBrowserExtension = _appService.SettingsService.Settings.UseBrowserExtension;
        if (!useBrowserExtension)
        {
            result.IsSuccessful = false;
            result.Message = "Browser extension is disabled. Please enable it and try again.";

            json = result.ConvertToJson();
            buffer = Encoding.UTF8.GetBytes(json!);
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer);
            response.OutputStream.Close();

            return;
        }

        // Make sure this is a POST request
        if (!request.HttpMethod.Equals("POST"))
            return;

        using var reader = new StreamReader(request.InputStream);
        json = await reader.ReadToEndAsync();

        var requestViewModel = json.ConvertFromJson<RequestViewModel>();
        if (requestViewModel == null)
            return;

        switch (request.Url?.OriginalString.ToLower())
        {
            case Constants.CheckFileTypeSupportUrl:
            {
                if (requestViewModel.Url.IsNullOrEmpty())
                {
                    result.IsSuccessful = false;
                    result.Message = "Url is empty.";
                }
                else
                {
                    var fileName = requestViewModel.Url!.GetFileName();
                    var fileExtension = Path.GetExtension(fileName);
                    if (fileExtension.IsNullOrEmpty())
                    {
                        result.IsSuccessful = false;
                        result.Message = "Can't get file extension.";
                        break;
                    }

                    var fileExtensions = await _appService
                        .UnitOfWork
                        .CategoryFileExtensionRepository
                        .GetAllAsync(select: fe => fe.Extension, distinct: true);

                    var isExtensionExists = !fileExtension.IsNullOrEmpty() && fileExtensions.Contains(fileExtension!);
                    result.IsSuccessful = isExtensionExists;
                    result.Message = isExtensionExists ? $"CDM supports {fileExtension} file types." : $"CDM doesn't support {fileExtension} file types.";

                    var supportMessage = isExtensionExists ? "supporting" : "not supporting";
                    var message = $"CDM is {supportMessage} '{fileExtension}' file types.";
                    Log.Information(message);
                }

                break;
            }

            case Constants.AddDownloadFileUrl:
            {
                var urlIsValid = requestViewModel.Url.CheckUrlValidation();
                if (!urlIsValid)
                {
                    result.IsSuccessful = false;
                    result.Message = "CDM can't accept this URL.";
                    break;
                }

                // Check for user option for showing start download dialog
                var showStartDownloadDialog = _appService.SettingsService.Settings.ShowStartDownloadDialog;
                if (showStartDownloadDialog)
                {
                    var vm = new AddDownloadLinkWindowViewModel(_appService)
                    {
                        IsLoadingUrl = true,
                        DownloadFile =
                        {
                            Url = requestViewModel.Url
                        }
                    };

                    var window = new AddDownloadLinkWindow { DataContext = vm };
                    window.Show();
                }
                else
                {
                    var details = await _appService.DownloadFileService.GetUrlDetailsAsync(requestViewModel.Url!);
                    if (!details.IsSuccess)
                    {
                        switch (details.Result)
                        {
                            case { IsFile: false }:
                            {
                                const string message = "The link you selected does not return a downloadable file. This could be due to an incorrect link, restricted access, " +
                                                       "or unsupported content.";

                                result.IsSuccessful = false;
                                result.Message = message;
                                
                                Log.Information(message);
                                break;
                            }
                        }
                        
                        break;
                    }

                    if (details.Result!.IsUrlDuplicate)
                    {
                        
                    }
                }

                result.IsSuccessful = true;
                result.Message = "Link added to CDM.";

                Log.Information($"Captured URL: {requestViewModel.Url}");
                break;
            }

            default:
            {
                result.IsSuccessful = false;
                result.Message = "Your request url is not supported.";
                break;
            }
        }

        json = result.ConvertToJson();

        // Send a response back to the browser extension if needed
        buffer = Encoding.UTF8.GetBytes(json!);
        response.ContentLength64 = buffer.Length;

        await response.OutputStream.WriteAsync(buffer);
        response.OutputStream.Close();
    }

    #endregion
}