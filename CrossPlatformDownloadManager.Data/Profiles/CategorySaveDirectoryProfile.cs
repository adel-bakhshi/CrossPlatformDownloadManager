using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.Data.Profiles;

public class CategorySaveDirectoryProfile : Profile
{
    public CategorySaveDirectoryProfile()
    {
        CreateMap<CategorySaveDirectory, CategorySaveDirectoryViewModel>();
        
        CreateMap<CategorySaveDirectoryViewModel, CategorySaveDirectory>()
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => GetCategoryId(src)))
            .ForMember(dest => dest.Category, opt => opt.Ignore());
    }

    #region Helpers

    private static int? GetCategoryId(CategorySaveDirectoryViewModel viewModel)
    {
        return viewModel.Category?.Id;
    }

    #endregion
}