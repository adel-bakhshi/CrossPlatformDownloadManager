using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.Models;

/// <summary>
/// Configure a request for downloading a file.
/// </summary>
public class DownloadRequest
{
    #region Private Fields

    /// <summary>
    /// The settings service.
    /// </summary>
    private readonly ISettingsService _settingsService;

    /// <summary>
    /// The HttpClient for sending the request.
    /// </summary>
    private HttpClient? _httpClient;

    #endregion

    #region Properties

    /// <summary>
    /// Gets a value that indicates the HttpClient for managing the http requests.
    /// </summary>
    public HttpClient RequestClient => GetRequestClient();

    /// <summary>
    /// Gets a dictionary of response headers.
    /// </summary>
    public Dictionary<string, string> ResponseHeaders { get; private set; } = [];

    /// <summary>
    /// Gets the url of the request.
    /// </summary>
    public Uri? Url { get; private set; }

    /// <summary>
    /// Gets the request options.
    /// </summary>
    public DownloadRequestOptions Options { get; }

    /// <summary>
    /// Get the proxy from the settings service.
    /// </summary>
    public IWebProxy? Proxy => GetProxy();

    #endregion

    /// <summary>
    /// Initialize a new instance of <see cref="DownloadRequest"/>.
    /// </summary>
    /// <param name="url">The url of the request.</param>
    public DownloadRequest(string url) : this(url, new DownloadRequestOptions())
    {
        Log.Debug("DownloadRequest initialized with URL: {Url}", url);
    }

    /// <summary>
    /// Initialize a new instance of <see cref="DownloadRequest"/>.
    /// </summary>
    /// <param name="url">The url of the request.</param>
    /// <param name="options">The options of the request.</param>
    public DownloadRequest(string url, DownloadRequestOptions options)
    {
        Log.Debug("Initializing DownloadRequest with URL: {Url} and custom options", url);

        _settingsService = GetSettingsService();

        Url = url.CheckUrlValidation() ? new Uri(url) : new Uri(new Uri("http://localhost"), url);
        Options = options;

        Log.Debug("DownloadRequest initialized successfully. Final URL: {FinalUrl}", Url);
    }

    /// <summary>
    /// Fetches the response headers from the server.
    /// </summary>
    /// <returns>A dictionary contains response headers.</returns>
    public async Task<HttpStatusCode?> FetchResponseHeadersAsync(CancellationToken cancelToken = default)
    {
        Log.Debug("Fetching response headers for URL: {Url}", Url);

        HttpStatusCode? statusCode = null;
        // Try to get the response headers from the URL
        try
        {
            // Create a new HttpClient instance
            using var httpClient = RequestClient;
            // Create a new HttpRequestMessage instance with the specified HttpMethod and URL
            using var request = new HttpRequestMessage(HttpMethod.Get, Url);
            // Send the request and wait for the response
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancelToken);
            // Set the status code
            statusCode = response.StatusCode;

            Log.Debug("Response received with status code: {StatusCode}", statusCode);

            // Ensure that the redirect URI is the same as the origin
            EnsureRedirectUriIsTheSameAsTheOrigin(response);
            // Store the response headers in the ResponseHeaders property
            ResponseHeaders = response.Content.Headers.ToDictionary(x => x.Key, x => x.Value.First());

            Log.Debug("Retrieved {HeaderCount} response headers", ResponseHeaders.Count);
        }
        catch (Exception ex)
        {
            // If an exception is thrown, clear the ResponseHeaders property
            ResponseHeaders.Clear();
            Log.Error(ex, "Failed to fetch response headers for URL: {Url}", Url);
        }

        // Return the ResponseHeaders property
        return statusCode;
    }

    /// <summary>
    /// Adds a range value to the request.
    /// </summary>
    /// <param name="start">The start value of the range.</param>
    /// <param name="end">The end value of the range.</param>
    public void AddRange(long start = 0, long end = 0)
    {
        Log.Debug("Adding range header: {Start}-{End}", start, end);
        RequestClient.DefaultRequestHeaders.Range = new RangeHeaderValue(start, end);
    }

    /// <summary>
    /// Removes range header from the request.
    /// </summary>
    public void RemoveRange()
    {
        Log.Debug("Removing range header");
        RequestClient.DefaultRequestHeaders.Range = null;
    }

    /// <summary>
    /// Checks that the URL supports downloading in range.
    /// </summary>
    /// <returns>True if the URL supports range downloads, otherwise false.</returns>
    public async Task<bool> CheckSupportsDownloadInRangeAsync(CancellationToken cancelToken = default)
    {
        Log.Debug("Checking if URL supports download in range: {Url}", Url);

        try
        {
            // Add range to request
            AddRange();
            // Fetch response headers
            var statusCode = await FetchResponseHeadersAsync(cancelToken);

            // Check for Accept-Ranges header
            if (ResponseHeaders.TryGetValue("Accept-Ranges", out var acceptRanges))
            {
                if (acceptRanges.Contains("bytes"))
                {
                    Log.Debug("URL supports range downloads (Accept-Ranges header found)");
                    return true;
                }
            }

            // Some servers don't include Accept-Ranges but still support partial content.
            // If Range request succeeds with Partial Content status:
            if (statusCode == HttpStatusCode.PartialContent)
            {
                Log.Debug("URL supports range downloads (Partial Content status received)");
                return true;
            }

            Log.Debug("URL does not support range downloads");
        }
        catch (Exception ex)
        {
            // Log error
            Log.Error(ex, "An error occurred while trying to check download in range capability. Error message: {ErrorMessage}", ex.Message);
        }

        return false;
    }

    #region Helpers

    /// <summary>
    /// Creates an instance of HttpClient with specific options.
    /// </summary>
    /// <returns>Returns a new instance of HttpClient.</returns>
    private HttpClient GetRequestClient()
    {
        // Check if http client is null.
        if (_httpClient != null)
            return _httpClient;

        Log.Debug("Creating new HttpClient instance");

        // Create a SocketsHttpHandler for the request
        var handler = GetHandler();
        // Create a HttpClient with the SocketsHttpHandler
        _httpClient = new HttpClient(handler);

        Log.Debug("HttpClient created successfully with proxy: {HasProxy}", handler.Proxy != null);
        return _httpClient;
    }

    /// <summary>
    /// Find the settings service in the application's service provider.
    /// </summary>
    /// <returns>Returns the settings service if it is found.</returns>
    /// <exception cref="InvalidOperationException">Throw an InvalidOperationException if the service is not found.</exception>
    private static ISettingsService GetSettingsService()
    {
        Log.Debug("Retrieving settings service from service provider");

        // Get service provider
        var serviceProvider = Application.Current?.TryGetServiceProvider();
        // Get settings service
        var settingsService = serviceProvider?.GetService<ISettingsService>();
        // Make sure settings service has value
        if (settingsService == null)
        {
            Log.Error("Settings service not found in service provider");
            throw new InvalidOperationException("Settings service is null.");
        }

        Log.Debug("Settings service retrieved successfully");
        return settingsService;
    }

    /// <summary>
    /// Creates a SocketsHttpHandler for the request.
    /// This handler is same for all request and ensure that all requests uses the same configuration.
    /// </summary>
    /// <returns>Returns a SocketsHttpHandler.</returns>
    private SocketsHttpHandler GetHandler()
    {
        Log.Debug("Creating SocketsHttpHandler with proxy and timeout configuration");

        // Create a SocketsHttpHandler for handling HttpClient requests
        var handler = new SocketsHttpHandler();
        // Add proxy to the handler
        handler.Proxy = Proxy;
        handler.UseProxy = Proxy != null;
        // Add timeout to the handler
        handler.ConnectTimeout = TimeSpan.FromSeconds(30);

        // Redirect options
        handler.AllowAutoRedirect = Options.AllowAutoRedirect;
        handler.MaxAutomaticRedirections = Options.MaxAutomaticRedirections;

        Log.Debug("SocketsHttpHandler created with AutoRedirect: {AllowAutoRedirect}, MaxRedirections: {MaxRedirections}",
            handler.AllowAutoRedirect, handler.MaxAutomaticRedirections);

        return handler;
    }

    /// <summary>
    /// Gets the proxy settings from the settings service.
    /// </summary>
    /// <returns>Returns an IWebProxy.</returns>
    /// <exception cref="InvalidOperationException">Throw an InvalidOperationException if the proxy mode is invalid.</exception>
    private IWebProxy? GetProxy()
    {
        Log.Debug("Retrieving proxy configuration. Proxy mode: {ProxyMode}", _settingsService.Settings.ProxyMode);

        IWebProxy? proxy = null;
        switch (_settingsService.Settings.ProxyMode)
        {
            // Don't use proxy
            case ProxyMode.DisableProxy:
            {
                proxy = null;
                Log.Debug("Proxy disabled");
                break;
            }

            // Use system proxy settings
            case ProxyMode.UseSystemProxySettings:
            {
                var systemProxy = WebRequest.DefaultWebProxy;
                if (systemProxy == null)
                {
                    Log.Debug("System proxy is not available");
                    break;
                }

                proxy = systemProxy;
                Log.Debug("Using system proxy settings");
                break;
            }

            // Use custom proxy
            case ProxyMode.UseCustomProxy:
            {
                // Find active proxy
                var activeProxy = _settingsService.Settings.Proxies.FirstOrDefault(p => p.IsActive);
                // Make sure active proxy is not null
                if (activeProxy == null)
                {
                    Log.Debug("No active custom proxy found");
                    break;
                }

                // Create a proxy with the active proxy settings
                proxy = new WebProxy
                {
                    Address = new Uri(activeProxy.GetProxyUri()),
                    Credentials = new NetworkCredential(activeProxy.Username, activeProxy.Password)
                };

                Log.Debug("Using custom proxy: {ProxyAddress}", activeProxy.GetProxyUri());
                break;
            }

            default:
                Log.Error("Invalid proxy mode: {ProxyMode}", _settingsService.Settings.ProxyMode);
                throw new InvalidOperationException("Invalid proxy mode.");
        }

        return proxy;
    }

    /// <summary>
    /// Find and compare the redirect URI with the origin URI.
    /// </summary>
    /// <param name="response">The response received by HttpClient</param>
    private void EnsureRedirectUriIsTheSameAsTheOrigin(HttpResponseMessage? response)
    {
        // Check if the response or request message is null
        if (response?.RequestMessage == null)
            return;

        // Get the redirect URI from the response
        var finalUrl = response.RequestMessage.RequestUri?.AbsoluteUri;
        // If the base address has changed, update the URL property
        if (!finalUrl.IsStringNullOrEmpty() && !finalUrl!.Equals(Url?.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
        {
            Log.Debug("URL redirected from {OriginalUrl} to {FinalUrl}", Url?.AbsoluteUri, finalUrl);
            Url = new Uri(finalUrl);
        }
    }

    #endregion
}