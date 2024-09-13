using PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

[AddINotifyPropertyChangedInterface]
public class CategoryFileExtensionViewModel
{
    public int Id { get; set; }
    public string? Extension { get; set; }
    public string? Alias { get; set; }
    public string? CategoryTitle { get; set; }
}