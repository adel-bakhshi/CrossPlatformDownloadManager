using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
        try
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
                buffer = Encoding.UTF8.GetBytes(json);
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
                        var urlDetails = await _appService.DownloadFileService.GetUrlDetailsAsync(requestViewModel.Url!);
                        var validateUrlDetails = _appService.DownloadFileService.ValidateUrlDetails(urlDetails);
                        if (!validateUrlDetails.IsValid)
                        {
                            result.IsSuccessful = validateUrlDetails.IsValid;
                            result.Message = validateUrlDetails.Message;
                            break;
                        }

                        var fileExtension = Path.GetExtension(urlDetails.FileName);
                        if (fileExtension.IsNullOrEmpty())
                        {
                            result.IsSuccessful = false;
                            result.Message = "Can't get file extension.";
                            break;
                        }

                        var fileExtensions = _appService
                            .CategoryService
                            .Categories
                            .SelectMany(c => c.FileExtensions)
                            .Select(fe => fe.Extension)
                            .Distinct()
                            .ToList();

                        var isExtensionExists = !fileExtension.IsNullOrEmpty() && fileExtensions.Contains(fileExtension);
                        var message = $"CDM is {(isExtensionExists ? "supporting" : "not supporting")} '{fileExtension}' file types.";

                        result.IsSuccessful = isExtensionExists;
                        result.Message = message;

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

                    // Make url for correct
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
                            result.IsSuccessful = addResult.IsSuccessful;
                            result.Message = addResult.Message;
                            break;
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
            buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer);
            response.OutputStream.Close();
        }
        catch (Exception ex)
        {
            var result = new ResponseViewModel
            {
                IsSuccessful = false,
                Message = ex.Message
            };

            var json = result.ConvertToJson();

            // Send a response back to the browser extension if needed
            var buffer = Encoding.UTF8.GetBytes(json);
            context.Response.ContentLength64 = buffer.Length;

            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.OutputStream.Close();
        }
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
        var urlDetails = await _appService.DownloadFileService.GetUrlDetailsAsync(url);
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