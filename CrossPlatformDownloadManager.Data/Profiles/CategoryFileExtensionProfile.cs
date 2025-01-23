using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.Data.Profiles;

public class CategoryFileExtensionProfile : Profile
{
    public CategoryFileExtensionProfile()
    {
        CreateMap<CategoryFileExtension, CategoryFileExtensionViewModel>();
    }
}