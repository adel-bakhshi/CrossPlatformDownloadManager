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
        var request = context.Request;

        // Make sure this is a POST request
        if (!request.HttpMethod.Equals("POST"))
            return;

        using var reader = new StreamReader(request.InputStream);
        var json = await reader.ReadToEndAsync();

        var requestViewModel = json.ConvertFromJson<RequestViewModel>();
        if (requestViewModel == null)
            return;

        var responseViewModel = new ResponseViewModel();
        switch (request.Url?.OriginalString.ToLower())
        {
            case Constants.CheckFileTypeSupportUrl:
            {
                if (requestViewModel.Url.IsNullOrEmpty())
                {
                    responseViewModel.IsSuccessful = false;
                    responseViewModel.Message = "Url is empty.";
                }
                else
                {
                    var fileName = requestViewModel.Url!.GetFileName();
                    var fileExtension = Path.GetExtension(fileName);
                    if (fileExtension.IsNullOrEmpty())
                    {
                        responseViewModel.IsSuccessful = false;
                        responseViewModel.Message = "Can't get file extension.";
                        break;
                    }

                    var fileExtensions = await _appService
                        .UnitOfWork
                        .CategoryFileExtensionRepository
                        .GetAllAsync(select: fe => fe.Extension, distinct: true);

                    var result = !fileExtension.IsNullOrEmpty() && fileExtensions.Contains(fileExtension!);
                    responseViewModel.IsSuccessful = result;
                    responseViewModel.Message = result
                        ? $"CDM supports {fileExtension} file types."
                        : $"CDM doesn't support {fileExtension} file types.";

                    var supportMessage = result ? "supporting" : "not supporting";
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
                    responseViewModel.IsSuccessful = false;
                    responseViewModel.Message = "CDM can't accept this URL.";
                    break;
                }

                var vm = new AddDownloadLinkWindowViewModel(_appService)
                {
                    IsLoadingUrl = true,
                    Url = requestViewModel.Url
                };

                var window = new AddDownloadLinkWindow { DataContext = vm };
                window.Show();

                responseViewModel.IsSuccessful = true;
                responseViewModel.Message = "Link added to CDM.";

                Log.Information($"Captured URL: {requestViewModel.Url}");
                break;
            }

            default:
            {
                responseViewModel.IsSuccessful = false;
                responseViewModel.Message = "Your request url is not supported.";
                break;
            }
        }

        json = responseViewModel.ConvertToJson();

        // Send a response back to the browser extension if needed
        var response = context.Response;
        var buffer = Encoding.UTF8.GetBytes(json!);
        response.ContentLength64 = buffer.Length;

        await response.OutputStream.WriteAsync(buffer);
        response.OutputStream.Close();
    }

    #endregion
}