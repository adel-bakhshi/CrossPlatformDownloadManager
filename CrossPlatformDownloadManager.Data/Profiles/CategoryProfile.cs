using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;

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