using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension.Models;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension;

public class BrowserExtension : IBrowserExtension
{
    #region Private Fields

    /// <summary>
    /// The app service instance.
    /// </summary>
    private readonly IAppService _appService;

    /// <summary>
    /// The HTTP listener for listening to the request that the browser extension sends.
    /// </summary>
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
        catch (HttpListenerException ex) when (ex.ErrorCode != 995)
        {
            // The I/O operation has been aborted because of either a thread exit or an application request.
            // This error occurs when the HttpListener is stopped and as a result the GetContextAsync method fails because it cannot find any Listener.
            // This error does not need to be written to the application logs.
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

    /// <summary>
    /// Processes the request and sends the response back to the browser extension.
    /// </summary>
    /// <param name="context">The HTTP listener context.</param>
    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        // Create a response object
        var response = new ExtensionResponse
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

            // Get the requested URL and send the correct response
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

    /// <summary>
    /// Gets all file types from the database and sends them back to the browser extension.
    /// </summary>
    /// <param name="context">The HTTP listener context.</param>
    /// <returns>Returns the response object containing the file types.</returns>
    private ExtensionResponse<List<string>> GetFileTypes(HttpListenerContext context)
    {
        // Create a response object
        var response = new ExtensionResponse<List<string>>
        {
            IsSuccessful = false,
            Message = string.Empty,
            Data = []
        };

        // Make sure this is a GET request
        var validateResult = ValidateRequestMethod(context, "GET");
        // Check if the request is valid
        if (!validateResult.IsSuccessful)
        {
            response.IsSuccessful = validateResult.IsSuccessful;
            response.Message = validateResult.Message;
            return response;
        }

        // Get all file types from the database
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

    /// <summary>
    /// Adds the URL that received from the browser extension to database.
    /// </summary>
    /// <param name="context">The HTTP listener context.</param>
    /// <returns>Returns the response object containing the result of the operation.</returns>
    private async Task<ExtensionResponse> AddDownloadFileUrlAsync(HttpListenerContext context)
    {
        // Create a response object
        var response = new ExtensionResponse
        {
            IsSuccessful = false,
            Message = string.Empty
        };

        // Make sure this is a POST request
        var validateResult = ValidateRequestMethod(context, "POST");
        // Check if the request is valid
        if (!validateResult.IsSuccessful)
        {
            response.IsSuccessful = validateResult.IsSuccessful;
            response.Message = validateResult.Message;
            return response;
        }

        // Read the input stream of the request
        using var reader = new StreamReader(context.Request.InputStream);
        // Read the JSON data from the request
        var json = await reader.ReadToEndAsync();
        // Make sure the JSON data is not null or empty
        if (json.IsStringNullOrEmpty())
        {
            response.Message = "Invalid data. Please retry. If the problem remains, report it for investigation.";
            return response;
        }

        // Convert the JSON data to a ExtensionRequest
        var extensionRequest = json.ConvertFromJson<ExtensionRequest?>();
        if (extensionRequest == null)
        {
            response.Message = "Invalid data. Please retry. If the problem remains, report it for investigation.";
            return response;
        }

        // Check if URL is valid
        var urlIsValid = extensionRequest.Url.CheckUrlValidation();
        if (!urlIsValid)
        {
            response.Message = "CDM can't accept this URL.";
            return response;
        }

        // Change URL to correct format
        extensionRequest.Url = extensionRequest.Url!.Replace('\\', '/').Trim();
        // Check for user option for showing start download dialog
        var showStartDownloadDialog = _appService.SettingsService.Settings.ShowStartDownloadDialog;
        // Go to AddDownloadLinkWindow (Start download dialog) and let user choose what he/she want
        if (showStartDownloadDialog)
        {
            ShowStartDownloadDialog(extensionRequest.Url);
        }
        // Otherwise, add link to database and start it
        else
        {
            await AddNewDownloadFileAndStartItAsync(extensionRequest.Url);
        }

        // Log captured URL
        Log.Information($"Captured URL: {extensionRequest.Url}");
        // Return the response
        response.IsSuccessful = true;
        response.Message = "Link added to CDM.";
        return response;
    }

    /// <summary>
    /// Validates the request method.
    /// </summary>
    /// <param name="context">The context of the HttpListener of the request.</param>
    /// <param name="httpMethod">The HTTP method to validate.</param>
    /// <returns>Returns the response object containing the result of the operation.</returns>
    private static ExtensionResponse ValidateRequestMethod(HttpListenerContext context, string? httpMethod)
    {
        var response = new ExtensionResponse
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

    /// <summary>
    /// Sends the response to the browser.
    /// </summary>
    /// <param name="context">The context of the HttpListener of the request.</param>
    /// <param name="extensionResponse">The response to send.</param>
    private static async Task SendResponseAsync(HttpListenerContext context, ExtensionResponse extensionResponse)
    {
        var json = extensionResponse.ConvertToJson();

        var buffer = Encoding.UTF8.GetBytes(json);
        context.Response.ContentLength64 = buffer.Length;

        await context.Response.OutputStream.WriteAsync(buffer);
        context.Response.OutputStream.Close();
    }

    /// <summary>
    /// Shows the start download dialog.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
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

    /// <summary>
    /// Adds a new download file to database and starts it.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
    private async Task AddNewDownloadFileAndStartItAsync(string url)
    {
        await _appService.DownloadFileService.AddDownloadFileAsync(url, startDownloading: true);
    }

    #endregion
}