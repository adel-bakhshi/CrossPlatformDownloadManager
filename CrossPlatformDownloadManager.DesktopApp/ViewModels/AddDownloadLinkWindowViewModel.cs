using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class AddDownloadLinkWindowViewModel : ViewModelBase
{
    #region Properties

    private string? _url;

    public string? Url
    {
        get => _url;
        set => this.RaiseAndSetIfChanged(ref _url, value?.Trim());
    }

    private ObservableCollection<CategoryItem> _categories;

    public ObservableCollection<CategoryItem> Categories
    {
        get => _categories;
        set => this.RaiseAndSetIfChanged(ref _categories, value);
    }

    private CategoryItem? _selectedCategory;

    public CategoryItem? SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    private string? _fileName;

    public string? FileName
    {
        get => _fileName;
        set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }

    private string? _description;

    public string? Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    private string? _fileTypeIcon;

    public string? FileTypeIcon
    {
        get => _fileTypeIcon;
        set => this.RaiseAndSetIfChanged(ref _fileTypeIcon, value);
    }

    private string? _fileSize;

    public string? FileSize
    {
        get => _fileSize;
        set => this.RaiseAndSetIfChanged(ref _fileSize, value);
    }

    private bool _isLoadingUrl = false;

    public bool IsLoadingUrl
    {
        get => _isLoadingUrl;
        set => this.RaiseAndSetIfChanged(ref _isLoadingUrl, value);
    }

    private ObservableCollection<Queue> _queues;

    public ObservableCollection<Queue> Queues
    {
        get => _queues;
        set => this.RaiseAndSetIfChanged(ref _queues, value);
    }

    private Queue? _selectedQueue;

    public Queue? SelectedQueue
    {
        get => _selectedQueue;
        set => this.RaiseAndSetIfChanged(ref _selectedQueue, value);
    }

    #endregion

    #region Commands

    public ICommand AddNewCategoryCommand { get; }

    public ICommand AddNewQueueCommand { get; }

    #endregion

    public AddDownloadLinkWindowViewModel(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        Categories = GetCategories();
        Queues = GetQueues();

        AddNewCategoryCommand = ReactiveCommand.Create<Window?>(AddNewCategory);
        AddNewQueueCommand = ReactiveCommand.Create<Window?>(AddNewQueue);
    }

    private async void AddNewCategory(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            var vm = new AddNewCategoryWindowViewModel(UnitOfWork);
            var window = new AddNewCategoryWindow { DataContext = vm };
            var result = await window.ShowDialog<bool>(owner);
            if (!result)
                return;
            
            Categories = GetCategories();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void AddNewQueue(Window? owner)
    {
        // TODO: Show AddNewQueueWindow
        throw new NotImplementedException();
    }

    private ObservableCollection<Queue> GetQueues()
    {
        try
        {
            var queues = UnitOfWork.QueueRepository.GetAll();
            return queues.ToObservableCollection();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new ObservableCollection<Queue>();
        }
    }

    private ObservableCollection<CategoryItem> GetCategories()
    {
        try
        {
            var categories = UnitOfWork.CategoryItemRepository.GetAll();
            categories.Insert(0, new CategoryItem { Title = "General" });
            return categories.ToObservableCollection();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new ObservableCollection<CategoryItem>();
        }
    }

    public async Task GetUrlInfoAsync()
    {
        IsLoadingUrl = true;

        try
        {
            if (!Url.CheckUrlValidation())
            {
                IsLoadingUrl = false;
                return;
            }

            var httpClient = new HttpClient();
            string fileName = string.Empty;
            double fileSize = 0;

            // Send a HEAD request to get the headers only
            using var request = new HttpRequestMessage(HttpMethod.Head, Url);
            using var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve URL: {response.StatusCode}");

            // Check if the Content-Type indicates a file
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType != null && (contentType.StartsWith("application/") || contentType.StartsWith("image/") ||
                                        contentType.StartsWith("video/") || contentType.StartsWith("audio/") ||
                                        contentType == "text/plain"))
            {
                if (response.Content.Headers.ContentDisposition != null)
                    fileName = response.Content.Headers.ContentDisposition.FileName?.Trim('\"') ?? string.Empty;

                // Fallback to using the URL to guess the file name if Content-Disposition is not present
                if (string.IsNullOrEmpty(fileName))
                {
                    var uri = new Uri(Url!);
                    fileName = Path.GetFileName(uri.LocalPath);
                }

                // Get the content length
                fileSize = response.Content.Headers.ContentLength ?? 0;
            }

            // Set file name, file size, file icon and category
            FileName = fileName;
            FileSize = fileSize.ToFileSize();

            // find category item by file extension
            var ext = Path.GetExtension(FileName);
            var fileExtension = UnitOfWork.CategoryItemFileExtensionRepository
                .Get(where: fe => fe.Extension.ToLower() == ext.ToLower());

            if (fileExtension != null)
            {
                var categoryItem = UnitOfWork.CategoryItemRepository
                    .Get(where: ci => ci.Id == fileExtension.CategoryItemId);

                FileTypeIcon = categoryItem?.Icon;
                SelectedCategory = categoryItem;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        IsLoadingUrl = false;
    }
}