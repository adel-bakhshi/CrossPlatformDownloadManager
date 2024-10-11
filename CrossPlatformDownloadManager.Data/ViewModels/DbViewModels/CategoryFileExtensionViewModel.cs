namespace CrossPlatformDownloadManager.Data.ViewModels.DbViewModels;

public class CategoryFileExtensionViewModel : DbViewModelBase
{
    #region Private Fields

    private int _id;
    private string? _extension;
    private string? _alias;
    private string? _categoryTitle;

    #endregion

    #region Properties

    public int Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string? Extension
    {
        get => _extension;
        set => SetField(ref _extension, value);
    }

    public string? Alias
    {
        get => _alias;
        set => SetField(ref _alias, value);
    }

    public string? CategoryTitle
    {
        get => _categoryTitle;
        set => SetField(ref _categoryTitle, value);
    }

    #endregion

    public override void UpdateViewModel(DbViewModelBase? viewModel)
    {
        if (viewModel is not CategoryFileExtensionViewModel categoryFileExtensionViewModel ||
            Id != categoryFileExtensionViewModel.Id)
        {
            return;
        }

        Extension = categoryFileExtensionViewModel.Extension;
        Alias = categoryFileExtensionViewModel.Alias;
        CategoryTitle = categoryFileExtensionViewModel.CategoryTitle;
    }
}