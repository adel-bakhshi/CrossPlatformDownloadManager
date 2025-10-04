using MultipartDownloader.Core.Extensions.Helpers;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text.RegularExpressions;

namespace MultipartDownloader.Core;

/// <summary>
/// Represents a client for making HTTP requests.
/// </summary>
public partial class SocketClient : IDisposable
{
    private const string HeaderContentLengthKey = "Content-Length";
    private const string HeaderContentDispositionKey = "Content-Disposition";
    private const string HeaderContentRangeKey = "Content-Range";
    private const string HeaderAcceptRangesKey = "Accept-Ranges";
    private const string FilenameStartPointKey = "filename=";

    private readonly Regex _contentRangePattern = RangePatternRegex();
    private readonly ConcurrentDictionary<string, string> _responseHeaders = [];
    private readonly HttpClient _client;
    private bool _isDisposed;
    private bool? _isSupportDownloadInRange;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketClient"/> class with the specified configuration.
    /// </summary>
    public SocketClient(RequestConfiguration config)
    {
        _client = GetHttpClientWithSocketHandler(config);
    }

    private static SocketsHttpHandler GetSocketsHttpHandler(RequestConfiguration config)
    {
        var handler = new SocketsHttpHandler()
        {
            AllowAutoRedirect = config.AllowAutoRedirect,
            MaxAutomaticRedirections = config.MaximumAutomaticRedirections,
            AutomaticDecompression = config.AutomaticDecompression,
            PreAuthenticate = config.PreAuthenticate,
            UseCookies = config.CookieContainer != null,
            UseProxy = config.Proxy != null,
            MaxConnectionsPerServer = 1000,
            PooledConnectionIdleTimeout = config.KeepAliveTimeout,
            PooledConnectionLifetime = Timeout.InfiniteTimeSpan,
            EnableMultipleHttp2Connections = true,
            ConnectTimeout = TimeSpan.FromMilliseconds(config.Timeout)
        };

        // Set up the SslClientAuthenticationOptions for custom certificate validation
        if (config.ClientCertificates.Count > 0)
            handler.SslOptions.ClientCertificates = config.ClientCertificates;

        handler.SslOptions.EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12;
        handler.SslOptions.RemoteCertificateValidationCallback = ExceptionHelper.CertificateValidationCallBack;

        // Configure keep-alive
        if (config.KeepAlive)
        {
            handler.KeepAlivePingTimeout = config.KeepAliveTimeout;
            handler.KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests;
        }

        // Configure credentials
        if (config.Credentials != null)
        {
            handler.Credentials = config.Credentials;
            handler.PreAuthenticate = config.PreAuthenticate;
        }

        // Configure cookies
        if (handler.UseCookies && config.CookieContainer != null)
            handler.CookieContainer = config.CookieContainer;

        // Configure proxy
        if (handler.UseProxy && config.Proxy != null)
            handler.Proxy = config.Proxy;

        // Add expect header
        if (!string.IsNullOrWhiteSpace(config.Expect))
            handler.Expect100ContinueTimeout = TimeSpan.FromSeconds(1);

        return handler;
    }

    private HttpClient GetHttpClientWithSocketHandler(RequestConfiguration config)
    {
        var handler = GetSocketsHttpHandler(config);
        HttpClient client = new(handler);
        client.DefaultRequestHeaders.Clear();

        // Add standard headers
        AddHeaderIfNotEmpty(client.DefaultRequestHeaders, "Accept", config.Accept);
        AddHeaderIfNotEmpty(client.DefaultRequestHeaders, "User-Agent", config.UserAgent);

        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        client.DefaultRequestHeaders.Add("Connection", config.KeepAlive ? "keep-alive" : "close");
        client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };

        // Add custom headers
        if (config.Headers.Count > 0)
        {
            foreach (var key in config.Headers.AllKeys.Where(k => !string.IsNullOrWhiteSpace(k.Trim())).ToList())
                AddHeaderIfNotEmpty(client.DefaultRequestHeaders, key, config.Headers[key]);
        }

        // Add optional headers
        if (!string.IsNullOrWhiteSpace(config.Referer))
            client.DefaultRequestHeaders.Referrer = new Uri(config.Referer);

        if (!string.IsNullOrWhiteSpace(config.ContentType))
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(config.ContentType));

        if (!string.IsNullOrWhiteSpace(config.TransferEncoding))
        {
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue(config.TransferEncoding));
            client.DefaultRequestHeaders.TransferEncoding.Add(new TransferCodingHeaderValue(config.TransferEncoding));
        }

        AddHeaderIfNotEmpty(client.DefaultRequestHeaders, "Expect", config.Expect);
        return client;
    }

    /// <summary>
    /// Adds the header to the request headers if it is not empty.
    /// </summary>
    /// <param name="headers">The request headers.</param>
    /// <param name="key">The key of the header.</param>
    /// <param name="value">The value of the header.</param>
    private static void AddHeaderIfNotEmpty(HttpRequestHeaders headers, string key, string? value)
    {
        value = value?.Trim();
        if (!string.IsNullOrWhiteSpace(value))
            headers.Add(key, value);
    }

    /// <summary>
    /// Fetches the response headers asynchronously.
    /// </summary>
    /// <param name="request">The request of client</param>
    /// <param name="addRange">Indicates whether to add a range header to the request.</param>
    /// <param name="cancelToken">Cancel request token</param>
    private async Task FetchResponseHeaders(Request request, bool addRange, CancellationToken cancelToken = default)
    {
        try
        {
            if (!_responseHeaders.IsEmpty)
                return;

            var requestMsg = request.GetRequest();
            if (addRange)
                requestMsg.Headers.Range = new RangeHeaderValue(0, 0);

            using var response = await SendRequestAsync(requestMsg, cancelToken).ConfigureAwait(false);
            if (response.StatusCode.IsRedirectStatus() && request.Configuration.AllowAutoRedirect)
                return;

            var redirectUrl = response.Headers.Location?.ToString();
            if (!string.IsNullOrWhiteSpace(redirectUrl) && !request.Address.ToString().Equals(redirectUrl, StringComparison.OrdinalIgnoreCase))
            {
                request.Address = new Uri(redirectUrl);
                await FetchResponseHeaders(request, true, cancelToken).ConfigureAwait(false);
                return;
            }

            EnsureResponseAddressIsSameWithOrigin(request, response);
        }
        catch (HttpRequestException exp) when (addRange && exp.IsRequestedRangeNotSatisfiable())
        {
            await FetchResponseHeaders(request, false, cancelToken).ConfigureAwait(false);
        }
        catch (HttpRequestException exp) when (request.Configuration.AllowAutoRedirect
                                               && exp.IsRedirectError()
                                               && _responseHeaders.TryGetValue("location", out var redirectedUrl)
                                               && !string.IsNullOrWhiteSpace(redirectedUrl)
                                               && !request.Address.ToString().Equals(redirectedUrl, StringComparison.OrdinalIgnoreCase))
        {
            request.Address = new Uri(redirectedUrl);
            await FetchResponseHeaders(request, true, cancelToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Ensures that the response address is the same as the original address.
    /// </summary>
    /// <param name="request">The request of client</param>
    /// <param name="response">The web response to check.</param>
    /// <returns>True if the response address is the same as the original address; otherwise, false.</returns>
    private static void EnsureResponseAddressIsSameWithOrigin(Request request, HttpResponseMessage response)
    {
        var redirectUri = GetRedirectUrl(response);
        if (redirectUri != null)
            request.Address = redirectUri;
    }

    /// <summary>
    /// Gets the redirect URL from the web response.
    /// </summary>
    /// <param name="response">The web response to get the redirect URL from.</param>
    /// <returns>The redirect URL.</returns>
    private static Uri? GetRedirectUrl(HttpResponseMessage response)
    {
        // https://github.com/dotnet/runtime/issues/23264
        var redirectLocation = response.Headers.Location;
        return redirectLocation ?? response.RequestMessage?.RequestUri;
    }

    /// <summary>
    /// Gets the file size asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the file size.</returns>
    public async ValueTask<long> GetFileSizeAsync(Request request)
    {
        if (await IsSupportDownloadInRangeAsync(request).ConfigureAwait(false))
            return GetTotalSizeFromContentRange(_responseHeaders.ToDictionary());

        return GetTotalSizeFromContentLength(_responseHeaders.ToDictionary());
    }

    private static long GetTotalSizeFromContentLength(Dictionary<string, string> headers)
    {
        // gets the total size from the content length headers.
        if (headers.TryGetValue(HeaderContentLengthKey, out var contentLengthText) && long.TryParse(contentLengthText, out var contentLength))
            return contentLength;

        return -1L;
    }

    /// <summary>
    /// Throws an exception if the download in range is not supported.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask ThrowIfIsNotSupportDownloadInRange(Request request)
    {
        var isSupport = await IsSupportDownloadInRangeAsync(request).ConfigureAwait(false);
        if (!isSupport)
            throw new NotSupportedException("The downloader cannot continue downloading because the network or server failed to download in range.");
    }

    /// <summary>
    /// Checks if the download in range is supported.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the download in range is supported.</returns>
    public async ValueTask<bool> IsSupportDownloadInRangeAsync(Request request)
    {
        if (_isSupportDownloadInRange.HasValue)
            return _isSupportDownloadInRange.Value;

        await FetchResponseHeaders(request, addRange: true).ConfigureAwait(false);

        // https://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.5
        if (_responseHeaders.TryGetValue(HeaderAcceptRangesKey, out var acceptRanges) && acceptRanges.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            _isSupportDownloadInRange = false;
            return false;
        }

        // https://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.16
        if (_responseHeaders.TryGetValue(HeaderContentRangeKey, out var contentRange) && !string.IsNullOrWhiteSpace(contentRange))
        {
            _isSupportDownloadInRange = true;
            return true;
        }

        _isSupportDownloadInRange = false;
        return false;
    }

    /// <summary>
    /// Gets the total size from the content range headers.
    /// </summary>
    /// <param name="headers">The headers to get the total size from.</param>
    /// <returns>The total size of the content.</returns>
    private long GetTotalSizeFromContentRange(Dictionary<string, string> headers)
    {
        if (headers.TryGetValue(HeaderContentRangeKey, out var contentRange) && !string.IsNullOrWhiteSpace(contentRange) && _contentRangePattern.IsMatch(contentRange))
        {
            var match = _contentRangePattern.Match(contentRange);
            var size = match.Groups["size"].Value;

            return long.TryParse(size, out var totalSize) ? totalSize : -1L;
        }

        return -1L;
    }

    /// <summary>
    /// Gets the file name asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the file name.</returns>
    public async Task<string> SetRequestFileNameAsync(Request request)
    {
        if (!string.IsNullOrWhiteSpace(request.FileName))
            return request.FileName;

        var filename = await GetUrlDispositionFilenameAsync(request).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(filename))
        {
            filename = request.GetFileNameFromUrl();
            if (string.IsNullOrWhiteSpace(filename))
                filename = Guid.NewGuid().ToString("N");
        }

        request.FileName = filename;
        return filename;
    }

    /// <summary>
    /// Gets the file name from the URL disposition header asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the file name.</returns>
    private async Task<string?> GetUrlDispositionFilenameAsync(Request request)
    {
        try
        {
            // Validate URL format
            if (request.Address.IsAbsoluteUri != true
                || !(request.Address.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) || request.Address.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                || string.IsNullOrWhiteSpace(request.Address.Host)
                || string.IsNullOrWhiteSpace(request.Address.AbsolutePath)
                || request.Address.AbsolutePath.Equals("/", StringComparison.OrdinalIgnoreCase)
                || request.Address.Segments.Length <= 1)
            {
                return null;
            }

            // Fetch headers if validations pass
            await FetchResponseHeaders(request, true).ConfigureAwait(false);

            if (_responseHeaders.TryGetValue(HeaderContentDispositionKey, out var disposition))
            {
                var filename = request
                    .ToUnicode(disposition)
                    .Split(';')
                    .FirstOrDefault(part => part.Trim().StartsWith(FilenameStartPointKey, StringComparison.OrdinalIgnoreCase))?
                    .Replace(FilenameStartPointKey, "")
                    .Replace("\"", "")
                    .Trim();

                return string.IsNullOrWhiteSpace(filename) ? null : filename;
            }
        }
        catch
        {
            // Ignore exceptions
        }

        return null;
    }

    /// <summary>
    /// Gets the response stream asynchronously.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancelToken"></param>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, CancellationToken cancelToken = default)
    {
        var response = await _client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancelToken)
            .ConfigureAwait(false);

        // Copy all response headers to our dictionary
        _responseHeaders.Clear();
        foreach (var header in response.Headers)
            _responseHeaders.TryAdd(header.Key, header.Value.FirstOrDefault() ?? string.Empty);

        foreach (var header in response.Content.Headers)
            _responseHeaders.TryAdd(header.Key, header.Value.FirstOrDefault() ?? string.Empty);

        // throws an HttpRequestException error if the response status code isn't within the 200-299 range.
        response.EnsureSuccessStatusCode();

        return response;
    }

    /// <summary>
    /// Disposes of the resources (if any) used by the <see cref="SocketClient"/>.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Generated regex

    [GeneratedRegex(@"bytes\s*((?<from>\d*)\s*-\s*(?<to>\d*)|\*)\s*\/\s*(?<size>\d+|\*)", RegexOptions.Compiled)]
    private static partial Regex RangePatternRegex();

    #endregion Generated regex
}