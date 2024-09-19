using PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

[AddINotifyPropertyChangedInterface]
public class CategoryViewModel
{
    public int? Id { get; set; }
    public string? Title { get; set; }
    public string? CategorySaveDirectory { get; set; }
}