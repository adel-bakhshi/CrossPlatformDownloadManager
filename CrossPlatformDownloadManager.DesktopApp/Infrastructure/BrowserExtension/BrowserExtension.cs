using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension.Models;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.Models;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension;

/// <summary>
/// Represents the browser extension.
/// </summary>
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
        // Initialize the browser extension with the provided app service
        Log.Debug("Initializing browser extension...");

        // Store the app service instance
        _appService = appService;

        // Initialize HttpListener for handling browser extension requests
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add(Constants.GetFileTypesUrl);
        _httpListener.Prefixes.Add(Constants.AddDownloadFileUrl);
    }

    public async Task StartListeningAsync()
    {
        try
        {
            // Start listening for incoming requests
            Log.Information("Start listening for requests...");
            _httpListener.Start();

            // Continuously listen for requests
            while (true)
            {
                // Process each request asynchronously
                var context = await _httpListener.GetContextAsync();
                Log.Debug("Received request from browser extension. Request URL: {RequestUrl}", context.Request.Url);

                _ = ProcessRequestAsync(context);
            }
        }
        catch (HttpListenerException ex) when (ex.ErrorCode != 995)
        {
            // Handle listener exceptions (excluding normal shutdown errors)
            // The I/O operation has been aborted because of either a thread exit or an application request.
            // This error occurs when the HttpListener is stopped and as a result the GetContextAsync method fails because it cannot find any Listener.
            // This error does not need to be written to the application logs.
            Log.Error(ex, "An error occurred while listening for requests.");
        }
        catch (Exception ex)
        {
            // Handle any other exceptions that might occur during listening
            Log.Error(ex, "Failed to start listening for requests.");
        }
    }

    public void StopListening()
    {
        try
        {
            // Stop the HTTP listener
            _httpListener.Stop();
            Log.Information("Stop listening for requests...");
        }
        catch (HttpListenerException)
        {
            // Ignore exceptions that occur when the listener is already stopped
        }
        catch (Exception ex)
        {
            // Handle any exceptions that might occur during listener shutdown
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
            Log.Debug("Processing request from browser extension...");

            // Check if browser extension is enabled
            var useBrowserExtension = _appService.SettingsService.Settings.UseBrowserExtension;
            if (!useBrowserExtension)
            {
                response.Message = "Browser extension is disabled. Please enable it and try again.";
                Log.Debug("{Message}", response.Message);

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
            // Handle any exceptions that occur during request processing
            response.Message = $"An error occurred while trying to add link to CDM. Error message: {ex.Message}";
            Log.Error(ex, "{Message}", response.Message);
        }

        // Send the response back to the client
        await SendResponseAsync(context, response);
    }

    /// <summary>
    /// Gets all file types from the database and sends them back to the browser extension.
    /// </summary>
    /// <param name="context">The HTTP listener context.</param>
    /// <returns>Returns the response object containing the file types.</returns>
    private ExtensionResponse<List<string>> GetFileTypes(HttpListenerContext context)
    {
        Log.Debug("Getting file types from the database...");

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

        Log.Debug("File types retrieved successfully. Count: {Count}", fileExtensions.Count);

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
        Log.Debug("Adding download file url to the application that received from browser extension...");

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
            const string message = "Invalid data. Please retry. If the problem remains, report it for investigation.";
            Log.Debug("{Message}", message);

            response.Message = message;
            return response;
        }

        // Convert the JSON data to a ExtensionRequest
        var extensionRequests = json.ConvertFromJson<List<ExtensionRequest>?>();
        if (extensionRequests == null || extensionRequests.Count == 0)
        {
            const string message = "Invalid data. Please retry. If the problem remains, report it for investigation.";
            Log.Debug("{Message}", message);

            response.Message = message;
            return response;
        }

        Log.Debug("Received {Count} URLs from browser extension.", extensionRequests.Count);

        // Define a list of tasks to get the details of the requests
        var updateTasks = new List<Task>();

        // Prepare all requests
        foreach (var extensionRequest in extensionRequests)
        {
            // Check format
            extensionRequest.CheckFormat();

            // Get update task and add it to the list
            var updateTask = extensionRequest.UpdateRequestUrlAsync(_appService);
            updateTasks.Add(updateTask);
        }

        // Wait for all update tasks to complete
        Log.Debug("Awaiting all update tasks to complete...");
        await Task.WhenAll(updateTasks);
        Log.Debug("All update tasks completed.");

        // Check the count of the requests.
        // If the count is equal to 1, open start download dialog or add the URL to the database and start it.
        if (extensionRequests.Count == 1)
        {
            var data = extensionRequests[0];

            // Change URL to correct format
            data.Url = data.Url!.Replace('\\', '/').Trim();
            Log.Information("The URL that received from browser extension is '{Url}'.", data.Url);

            // Check if URL is valid
            var urlIsValid = data.Url.CheckUrlValidation();
            if (!urlIsValid)
            {
                const string message = "CDM can't accept this URL.";
                Log.Debug("{Message}", message);

                response.Message = message;
                return response;
            }

            // Check for user option for showing start download dialog
            var showStartDownloadDialog = _appService.SettingsService.Settings.ShowStartDownloadDialog;
            // Go to AddDownloadLinkWindow (Start download dialog) and let user choose what he/she want
            if (showStartDownloadDialog)
            {
                Log.Debug("Showing start download dialog...");
                ShowStartDownloadDialog(data.Url!, data.Referer, data.PageAddress, data.Description);
            }
            // Otherwise, add link to database and start it
            else
            {
                Log.Debug("Adding download file url to database and starting it...");
                await AddNewDownloadFileAndStartItAsync(data.Url!, data.Referer, data.PageAddress, data.Description);
            }
        }
        // Otherwise, show manage links window
        else
        {
            Log.Debug("Trying to add multiple URLs to database...");

            // Convert received URLs to DownloadFileViewModel
            var downloadFiles = extensionRequests
                .Where(er => er.Url.CheckUrlValidation())
                .Select(er =>
                {
                    var result = new DownloadFileViewModel
                    {
                        Url = er.Url!.Replace('\\', '/').Trim(),
                        Referer = er.Referer,
                        PageAddress = er.PageAddress,
                        Description = er.Description
                    };

                    Log.Information("The URL that received from browser extension is '{Url}'.", result.Url);
                    return result;
                })
                .ToList();

            // Show manage links window
            ShowManageLinksWindow(downloadFiles);
        }

        // Return the response
        response.IsSuccessful = true;
        response.Message = $"{(extensionRequests.Count > 1 ? $"{extensionRequests.Count} Links" : "Link")} added to CDM.";
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
        Log.Debug("Validating request method...");

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
            const string message = "We encountered an issue while processing your request. Please try again. If the problem persists, report it to be investigated.";
            Log.Debug("{Message}", message);

            response.Message = message;
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
        Log.Debug("Sending response to browser...");

        // Create buffer from response object and send it to browser
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
    /// <param name="referer">The referer of the file to download.</param>
    /// <param name="pageAddress">The web page address of the file to download.</param>
    /// <param name="description">The description of the file to download.</param>
    private void ShowStartDownloadDialog(string url, string? referer, string? pageAddress, string? description)
    {
        Log.Debug("Opening add download link window for further steps...");

        var vm = new AddDownloadLinkWindowViewModel(_appService)
        {
            IsLoadingUrl = true,
            DownloadFile =
            {
                Url = url,
                Referer = referer,
                PageAddress = pageAddress,
                Description = description
            }
        };

        var window = new AddDownloadLinkWindow { DataContext = vm };
        window.Show();
    }

    /// <summary>
    /// Adds a new download file to database and starts it.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
    /// <param name="referer">The referer of the file to download.</param>
    /// <param name="pageAddress">The web page address of the file to download.</param>
    /// <param name="description">The description of the file to download.</param>
    private async Task AddNewDownloadFileAndStartItAsync(string url, string? referer, string? pageAddress, string? description)
    {
        Log.Debug("Adding new download file and starting it...");

        var options = new DownloadFileOptions
        {
            Referer = referer,
            PageAddress = pageAddress,
            Description = description,
            StartDownloading = true
        };

        await _appService.DownloadFileService.AddDownloadFileAsync(url, options);
    }

    /// <summary>
    /// Shows manage links window and let the user select the URLs that he/she wants.
    /// </summary>
    /// <param name="downloadFiles">The download files that received from the browser extensions.</param>
    private void ShowManageLinksWindow(List<DownloadFileViewModel> downloadFiles)
    {
        if (downloadFiles.Count == 0)
            return;

        Log.Debug("Opening manage links window...");

        var viewModel = new ManageLinksWindowViewModel(_appService, downloadFiles);
        var window = new ManageLinksWindow { DataContext = viewModel };
        window.Show();
    }

    #endregion
}