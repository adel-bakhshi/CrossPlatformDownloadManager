using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.Data.Profiles;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryViewModel>()
            .ForMember(dest => dest.FileExtensions, opt => opt.MapFrom(src => src.FileExtensions.ToObservableCollection()));

        CreateMap<CategoryViewModel, Category>()
            .ForMember(dest => dest.FileExtensions, opt => opt.Ignore())
            .ForMember(dest => dest.CategorySaveDirectory, opt => opt.Ignore());
    }
}