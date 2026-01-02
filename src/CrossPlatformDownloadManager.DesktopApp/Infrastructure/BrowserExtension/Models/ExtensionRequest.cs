using System;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using Newtonsoft.Json;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension.Models;

/// <summary>
/// Represents a request from the browser extension.
/// </summary>
public class ExtensionRequest
{
    #region Properties

    /// <summary>
    /// Gets or sets a value that indicates the URL of the download file.
    /// </summary>
    [JsonProperty("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates the referer of the download file.
    /// </summary>
    [JsonProperty("referer")]
    public string? Referer { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates the page address that download started from it.
    /// </summary>
    [JsonProperty("pageAddress")]
    public string? PageAddress { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates the description of the download file.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    #endregion

    /// <summary>
    /// Checks the format of the request.
    /// </summary>
    public void CheckFormat()
    {
        Log.Debug("Checking the format of the request with URL '{URL}'...", Url);

        Url = Url?.Replace('\\', '/').Trim();
        Referer = Referer?.Replace('\\', '/').Trim();
        PageAddress = PageAddress?.Replace('\\', '/').Trim();
        Description = Description?.Replace('\n', ' ').Trim();
    }

    /// <summary>
    /// Updates the URL of the request by sending a request to the server.
    /// </summary>
    /// <param name="appService">The application service.</param>
    public async Task UpdateRequestUrlAsync(IAppService appService)
    {
        try
        {
            Log.Debug("Updating the URL of the request (if possible) using the original URL '{URL}'...", Url);

            var downloadFile = await appService.DownloadFileService.GetDownloadFileFromUrlAsync(Url);
            Url = downloadFile.Url;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while updating the URL of the request.");
        }
    }
}