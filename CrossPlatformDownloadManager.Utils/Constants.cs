using CrossPlatformDownloadManager.Utils.Enums;

namespace CrossPlatformDownloadManager.Utils;

public static class Constants
{
    #region Private fields

    // Duplicate download link actions
    private const string LetUserChooseAction = "Show the dialog and let me choose";
    private const string DuplicateWithNumberAction = "Add the duplicate with a number after its file name";
    private const string OverwriteExistingAction = "Add the duplicate and overwrite existing file";
    private const string ShowCompleteDialogOrResumeAction = "if download file complete, show download complete dialog. Otherwise, resume it";

    #endregion

    #region Public fields

    /// <summary>
    /// The amount of bytes in a kilobyte.
    /// </summary>
    public const long KiloByte = 1024;

    /// <summary>
    /// The amount of bytes in a megabyte.
    /// </summary>
    public const long MegaByte = KiloByte * 1024;

    /// <summary>
    /// The amount of bytes in a gigabyte.
    /// </summary>
    public const long GigaByte = MegaByte * 1024;

    /// <summary>
    /// The amount of bytes in a terabyte.
    /// </summary>
    public const long TeraByte = GigaByte * 1024;

    /// <summary>
    /// General category title.
    /// </summary>
    public const string GeneralCategoryTitle = "General";

    /// <summary>
    /// Unknown file type.
    /// </summary>
    public const string UnknownFileType = "Unknown";

    /// <summary>
    /// Default icon for new categories.
    /// </summary>
    public const string NewCategoryIcon =
        "M500.3 7.3C507.7 13.3 512 22.4 512 32l0 144c0 26.5-28.7 48-64 48s-64-21.5-64-48s28.7-48 64-48l0-57L352 90.2 352 208c0 26.5-28.7 48-64 48s-64-21.5-64-48s28.7-48 64-48l0-96c0-15.3 10.8-28.4 25.7-31.4l160-32c9.4-1.9 19.1 .6 26.6 6.6zM74.7 304l11.8-17.8c5.9-8.9 15.9-14.2 26.6-14.2l61.7 0c10.7 0 20.7 5.3 26.6 14.2L213.3 304l26.7 0c26.5 0 48 21.5 48 48l0 112c0 26.5-21.5 48-48 48L48 512c-26.5 0-48-21.5-48-48L0 352c0-26.5 21.5-48 48-48l26.7 0zM192 408a48 48 0 1 0 -96 0 48 48 0 1 0 96 0zM478.7 278.3L440.3 368l55.7 0c6.7 0 12.6 4.1 15 10.4s.6 13.3-4.4 17.7l-128 112c-5.6 4.9-13.9 5.3-19.9 .9s-8.2-12.4-5.3-19.2L391.7 400 336 400c-6.7 0-12.6-4.1-15-10.4s-.6-13.3 4.4-17.7l128-112c5.6-4.9 13.9-5.3 19.9-.9s8.2 12.4 5.3 19.2zm-339-59.2c-6.5 6.5-17 6.5-23 0L19.9 119.2c-28-29-26.5-76.9 5-103.9c27-23.5 68.4-19 93.4 6.5l10 10.5 9.5-10.5c25-25.5 65.9-30 93.9-6.5c31 27 32.5 74.9 4.5 103.9l-96.4 99.9z";

    /// <summary>
    /// Default title for unfinished category header.
    /// </summary>
    public const string UnfinishedCategoryHeaderTitle = "Unfinished";

    /// <summary>
    /// Default title for finished category header.
    /// </summary>
    public const string FinishedCategoryHeaderTitle = "Finished";

    /// <summary>
    /// Default download queue title.
    /// </summary>
    public const string DefaultDownloadQueueTitle = "Main Queue";

    /// <summary>
    /// The application listening to this url for requests that coming from the browser extension and returns file types as response.
    /// </summary>
    public const string GetFileTypesUrl = "http://localhost:5000/cdm/download/filetypes/";

    /// <summary>
    /// The application listening to this url for requests that coming from the browser extension and add received url to the database.
    /// </summary>
    public const string AddDownloadFileUrl = "http://localhost:5000/cdm/download/add/";

    /// <summary>
    /// GitHub project url.
    /// </summary>
    public const string GithubProjectUrl = "https://github.com/adel-bakhshi/CrossPlatformDownloadManager";

    /// <summary>
    /// Telegram url.
    /// </summary>
    public const string TelegramUrl = "https://t.me/ADdy2142";

    /// <summary>
    /// My email.
    /// </summary>
    public const string Email = "adelbakhshi78@yahoo.com";

    /// <summary>
    /// The URL of the CDM website.
    /// </summary>
    public const string CdmWebsiteUrl = "https://cdmapp.netlify.app";

    /// <summary>
    /// The API URL of the CDM website.
    /// </summary>
    public const string CdmApiUrl = $"{CdmWebsiteUrl}/api";

    /// <summary>
    /// The URL of the CDM browser extension page.
    /// </summary>
    public const string CdmBrowserExtensionUrl = $"{CdmWebsiteUrl}/browser-extension";

    /// <summary>
    /// The file path of the light theme data.
    /// </summary>
    public const string LightThemeFilePath = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/Themes/light-theme.json";

    /// <summary>
    /// The file path of the dark theme data.
    /// </summary>
    public const string DarkThemeFilePath = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/Themes/dark-theme.json";

    /// <summary>
    /// The lastest download url of the browser extension.
    /// </summary>
    public const string LastestDownloadUrlOfBrowserExtension = "https://github.com/adel-bakhshi/cdm-browser-extension/releases/latest/download/chromium-extension.zip";

    /// <summary>
    /// The link to the guide for creating themes.
    /// </summary>
    public const string CreateThemeGuideLink = "https://github.com/adel-bakhshi/CrossPlatformDownloadManager/blob/master/Assets/MarkDown/THEME_GUIDE.md";

    /// <summary>
    /// The default days to keep logs.
    /// </summary>
    public const long DefaultLogHistory = 7;

    #endregion

    #region Properties

    /// <summary>
    /// Gets a value that indicates the turn off computer modes.
    /// </summary>
    public static List<string> TurnOffComputerModes
    {
        get
        {
            return Enum
                .GetNames<TurnOffComputerMode>()
                .Select(n =>
                {
                    if (n.Equals(Enum.GetName(TurnOffComputerMode.Shutdown)))
                        n = "Shut down";

                    return n;
                })
                .ToList();
        }
    }

    /// <summary>
    /// Gets a value that indicates the speed limiter units.
    /// </summary>
    public static List<string> SpeedLimiterUnits => ["KB", "MB"];

    /// <summary>
    /// Gets a value that indicates the times of day.
    /// </summary>
    public static List<string> TimesOfDay => ["AM", "PM"];

    /// <summary>
    /// Gets a value that indicates the maximum connections count
    /// </summary>
    public static List<int> MaximumConnectionsCountList =>
    [
        1,
        2,
        4,
        8,
        16,
        32,
    ];

    /// <summary>
    /// Gets a value that indicates the proxy types.
    /// </summary>
    public static List<string> ProxyTypes
    {
        get
        {
            return Enum
                .GetNames<ProxyType>()
                .Select(pt =>
                {
                    if (pt.Equals(Enum.GetName(ProxyType.Socks5)))
                        pt = "Socks 5";

                    return pt;
                })
                .ToList();
        }
    }

    /// <summary>
    /// Gets a value that indicates the main directory of the application.
    /// This directory is the directory that the application is running in.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">If directory not found.</exception>
    public static string MainDirectory
    {
        get
        {
            var processPath = Environment.ProcessPath;
            var directory = Path.GetDirectoryName(processPath);
            if (directory.IsStringNullOrEmpty() || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("Main directory not found.");

            return directory;
        }
    }

    /// <summary>
    /// Gets a value that indicates the application data directory.
    /// This directory is the directory that the application data stored in it.
    /// </summary>
    public static string ApplicationDataDirectory
    {
        get
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CDM");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return directory;
        }
    }

    /// <summary>
    /// Gets a value that indicates the temporary download directory.
    /// This directory is the directory that the temp download files stored in it.
    /// </summary>
    public static string TempDownloadDirectory
    {
        get
        {
            var directory = Path.Combine(ApplicationDataDirectory, "Downloads");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return directory;
        }
    }

    /// <summary>
    /// Gets a value that indicates the directory that themes stored in it.
    /// </summary>
    public static string ThemesDirectory
    {
        get
        {
            var directory = Path.Combine(ApplicationDataDirectory, "Themes");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return directory;
        }
    }

    /// <summary>
    /// Gets a value that indicates the directory that logs stored in it.
    /// </summary>
    public static string LogsDirectory
    {
        get
        {
            var directory = Path.Combine(ApplicationDataDirectory, "Logs");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return directory;
        }
    }

    /// <summary>
    /// Gets  a value that indicates the available fonts in the program.
    /// </summary>
    public static List<string> AvailableFonts =>
    [
        "Asap",
        "Ubuntu"
    ];

    #endregion

    /// <summary>
    /// Creates and returns a list of string representing the messages of the DuplicateDownloadLinkAction enum values.
    /// </summary>
    /// <returns>Returns a list of messages for DuplicateDownloadLinkAction enum.</returns>
    public static List<string> GetDuplicateActionsMessages()
    {
        var names = Enum
            .GetValues<DuplicateDownloadLinkAction>()
            .Select(name =>
            {
                return name switch
                {
                    DuplicateDownloadLinkAction.LetUserChoose => LetUserChooseAction,
                    DuplicateDownloadLinkAction.DuplicateWithNumber => DuplicateWithNumberAction,
                    DuplicateDownloadLinkAction.OverwriteExisting => OverwriteExistingAction,
                    DuplicateDownloadLinkAction.ShowCompleteDialogOrResume => ShowCompleteDialogOrResumeAction,
                    _ => string.Empty
                };
            })
            .ToList();

        return names;
    }

    /// <summary>
    /// Returns the message of the DuplicateDownloadLinkAction enum value.
    /// </summary>
    /// <param name="action">A value of DuplicateDownloadLinkAction enum</param>
    /// <returns>Returns the message related to the value of the enum.</returns>
    public static string? GetDuplicateActionMessage(DuplicateDownloadLinkAction action)
    {
        return action switch
        {
            DuplicateDownloadLinkAction.LetUserChoose => LetUserChooseAction,
            DuplicateDownloadLinkAction.DuplicateWithNumber => DuplicateWithNumberAction,
            DuplicateDownloadLinkAction.OverwriteExisting => OverwriteExistingAction,
            DuplicateDownloadLinkAction.ShowCompleteDialogOrResume => ShowCompleteDialogOrResumeAction,
            _ => null
        };
    }

    /// <summary>
    /// Returns a value of DuplicateDownloadLinkAction enum from the message.
    /// </summary>
    /// <param name="message">The message you want to get DuplicateDownloadLinkAction enum value.</param>
    /// <returns>Returns the value related to the message.</returns>
    /// <exception cref="ArgumentException">If message is not related to any values of DuplicateDownloadLinkAction enum.</exception>
    public static DuplicateDownloadLinkAction GetDuplicateActionFromMessage(string message)
    {
        var action = message switch
        {
            LetUserChooseAction => DuplicateDownloadLinkAction.LetUserChoose,
            DuplicateWithNumberAction => DuplicateDownloadLinkAction.DuplicateWithNumber,
            OverwriteExistingAction => DuplicateDownloadLinkAction.OverwriteExisting,
            ShowCompleteDialogOrResumeAction => DuplicateDownloadLinkAction.ShowCompleteDialogOrResume,
            _ => throw new ArgumentException("Can't get action from message.")
        };

        return action;
    }
}