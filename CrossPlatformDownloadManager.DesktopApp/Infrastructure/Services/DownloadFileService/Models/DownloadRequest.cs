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
using CrossPlatformDownloadManager.Utils.PropertyChanged;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.Models;

/// <summary>
/// Configure a request for downloading a file.
/// </summary>
public class DownloadRequest : PropertyChangedBase
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
    
    /// <summary>
    /// The url of the request.
    /// </summary>
    private Uri? _url;

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
    public Uri? Url
    {
        get => _url;
        private set => SetField(ref _url, value);
    }

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
    }

    /// <summary>
    /// Initialize a new instance of <see cref="DownloadRequest"/>.
    /// </summary>
    /// <param name="url">The url of the request.</param>
    /// <param name="options">The options of the request.</param>
    public DownloadRequest(string url, DownloadRequestOptions options)
    {
        _settingsService = GetSettingsService();
        
        Url = url.CheckUrlValidation() ? new Uri(url) : new Uri(new Uri("http://localhost"), url);
        Options = options;
    }

    /// <summary>
    /// Fetches the response headers from the server.
    /// </summary>
    /// <returns>A dictionary contains response headers.</returns>
    public async Task<HttpStatusCode?> FetchResponseHeadersAsync(CancellationToken cancelToken = default)
    {
        HttpStatusCode? statusCode;
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
            // Ensure that the request was successful
            response.EnsureSuccessStatusCode();
            // Ensure that the redirect URI is the same as the origin
            EnsureRedirectUriIsTheSameAsTheOrigin(response);

            // Store the response headers in the ResponseHeaders property
            ResponseHeaders = response.Content.Headers.ToDictionary(x => x.Key, x => x.Value.First());
        }
        catch
        {
            // If an exception is thrown, clear the ResponseHeaders property
            ResponseHeaders.Clear();
            // Throw the exception
            throw;
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
        RequestClient.DefaultRequestHeaders.Range = new RangeHeaderValue(start, end);
    }

    /// <summary>
    /// Removes range header from the request.
    /// </summary>
    public void RemoveRange()
    {
        RequestClient.DefaultRequestHeaders.Range = null;
    }

    /// <summary>
    /// Checks that the URL supports downloading in range.
    /// </summary>
    /// <returns></returns>
    public async Task<bool> CheckSupportsDownloadInRangeAsync(CancellationToken cancelToken = default)
    {
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
                    return true;
            }
            
            // Some servers don't include Accept-Ranges but still support partial content.
            // If Range request succeeds with Partial Content status:
            if (statusCode == HttpStatusCode.PartialContent)
                return true;
        }
        catch (Exception ex)
        {
            // Log error
            Log.Error("An error occurred while trying to check download in range capability. Error message: {ErrorMessage},", ex.Message);
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
        
        // Create a SocketsHttpHandler for the request
        var handler = GetHandler();
        // Create a HttpClient with the SocketsHttpHandler
        _httpClient = new HttpClient(handler);

        return _httpClient;
    }

    /// <summary>
    /// Find the settings service in the application's service provider.
    /// </summary>
    /// <returns>Returns the settings service if it is found.</returns>
    /// <exception cref="InvalidOperationException">Throw an InvalidOperationException if the service is not found.</exception>
    private static ISettingsService GetSettingsService()
    {
        // Get service provider
        var serviceProvider = Application.Current?.TryGetServiceProvider();
        // Get settings service
        var settingsService = serviceProvider?.GetService<ISettingsService>();
        // Make sure settings service has value
        if (settingsService == null)
            throw new InvalidOperationException("Settings service is null.");

        return settingsService;
    }

    /// <summary>
    /// Creates a SocketsHttpHandler for the request.
    /// This handler is same for all request and ensure that all requests uses the same configuration.
    /// </summary>
    /// <returns>Returns a SocketsHttpHandler.</returns>
    private SocketsHttpHandler GetHandler()
    {
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

        return handler;
    }

    /// <summary>
    /// Gets the proxy settings from the settings service.
    /// </summary>
    /// <returns>Returns an IWebProxy.</returns>
    /// <exception cref="InvalidOperationException">Throw an InvalidOperationException if the proxy mode is invalid.</exception>
    private IWebProxy? GetProxy()
    {
        IWebProxy? proxy = null;
        switch (_settingsService.Settings.ProxyMode)
        {
            // Don't use proxy
            case ProxyMode.DisableProxy:
            {
                proxy = null;
                break;
            }

            // Use system proxy settings
            case ProxyMode.UseSystemProxySettings:
            {
                var systemProxy = WebRequest.DefaultWebProxy;
                if (systemProxy == null)
                    break;

                proxy = systemProxy;
                break;
            }

            // Use custom proxy
            case ProxyMode.UseCustomProxy:
            {
                // Find active proxy
                var activeProxy = _settingsService.Settings.Proxies.FirstOrDefault(p => p.IsActive);
                // Make sure active proxy is not null
                if (activeProxy == null)
                    break;

                // Create a proxy with the active proxy settings
                proxy = new WebProxy
                {
                    Address = new Uri(activeProxy.GetProxyUri()),
                    Credentials = new NetworkCredential(activeProxy.Username, activeProxy.Password)
                };

                break;
            }

            default:
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
            Url = new Uri(finalUrl);
    }

    #endregion
}