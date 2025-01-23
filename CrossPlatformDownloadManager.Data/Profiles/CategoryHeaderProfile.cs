using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.Data.Profiles;

public class CategoryHeaderProfile : Profile
{
    public CategoryHeaderProfile()
    {
        CreateMap<CategoryHeader, CategoryHeaderViewModel>()
            .ForMember(dest => dest.Categories, opt => opt.MapFrom(src => src.Categories.ToObservableCollection()));

        CreateMap<CategoryHeaderViewModel, CategoryHeader>();
    }
}