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
        _httpListener.Prefixes.Add("http://localhost:5000/download/check/");
        _httpListener.Prefixes.Add("http://localhost:5000/download/add/");
    }

    public async Task StartListeningAsync()
    {
        // TODO: Show message box
        try
        {
            _httpListener.Start();
            Console.WriteLine("Listening for URLs...");

            while (true)
            {
                var context = await _httpListener.GetContextAsync();
                _ = ProcessRequestAsync(context);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public void StopListening()
    {
        // TODO: Show message box
        try
        {
            _httpListener.Stop();
            Console.WriteLine("Stopped listening for URLs...");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
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
            case "http://localhost:5000/download/check/":
            {
                if (requestViewModel.Url.IsNullOrEmpty())
                {
                    responseViewModel.IsSuccessful = false;
                    responseViewModel.Message = "Url is empty.";
                }
                else
                {
                    var fileExtension = requestViewModel.Url!.GetFileName();

                    var fileExtensions = await _appService
                        .UnitOfWork
                        .CategoryFileExtensionRepository
                        .GetAllAsync(select: fe => fe.Extension, distinct: true);

                    var result = !fileExtension.IsNullOrEmpty() && fileExtensions.Contains(fileExtension!);
                    responseViewModel.IsSuccessful = result;
                    responseViewModel.Message = result
                        ? $"CDM supports {fileExtension} file types."
                        : $"CDM doesn't support {fileExtension} file types.";

                    var dateTimeAsString = DateTime.Now.ToShortTimeString();
                    var supportMessage = result ? "supporting" : "not supporting";
                    var message = $"{dateTimeAsString} - CDM is {supportMessage} '{fileExtension}' file types.";
                    Console.WriteLine(message);
                }

                break;
            }

            case "http://localhost:5000/download/add/":
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

                Console.WriteLine($"{DateTime.Now.ToShortTimeString()} - Captured URL: {requestViewModel.Url}");
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