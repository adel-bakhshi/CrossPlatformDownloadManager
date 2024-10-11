using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.DbViewModels;

namespace CrossPlatformDownloadManager.Data.Profiles;

public class CategoryFileExtensionProfile : Profile
{
    public CategoryFileExtensionProfile()
    {
        CreateMap<CategoryFileExtension, CategoryFileExtensionViewModel>()
            .ForMember(dest => dest.CategoryTitle, opt => opt.MapFrom(src => src.Category.Title))
            .ReverseMap();
    }
}