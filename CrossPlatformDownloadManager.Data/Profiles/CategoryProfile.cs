using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.DbViewModels;

namespace CrossPlatformDownloadManager.Data.Profiles;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryViewModel>()
            .ForMember(dest => dest.CategorySaveDirectory,
                opt => opt.MapFrom(src => src.CategorySaveDirectory.SaveDirectory))
            .ReverseMap();
    }
}