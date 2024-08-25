using CrossPlatformDownloadManager.Utils.Enums;

namespace CrossPlatformDownloadManager.Utils;

public static class Constants
{
    // File Size
    public const double KB = 1024;
    public const double MB = KB * 1024;
    public const double GB = MB * 1024;
    public const double TB = GB * 1024;

    // Turn off computer modes
    public static readonly List<string> TurnOffComputerModes = Enum.GetNames(typeof(TurnOffComputerMode))
        .Select(n =>
        {
            if (n.Equals(Enum.GetName(TurnOffComputerMode.Shutdown)))
                n = "Shut down";

            return n;
        })
        .ToList();

    // Speed limiter units
    public static readonly List<string> SpeedLimiterUnits = ["KB", "MB"];
    
    // Times of day
    public static readonly List<string> TimesOfDay = ["AM", "PM"];
    
    // General category title
    public const string GeneralCategoryTitle = "General";
}