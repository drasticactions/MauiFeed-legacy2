using Drastic.ViewModels;
using MauiFeed.Models;

namespace MauiFeed.ViewModels;

public abstract class SidebarViewModel : BaseViewModel
{
    public SidebarViewModel(IServiceProvider services)
        : base(services)
    {
    }

    internal abstract IList<ISidebarItem> GenerateFilterItems();
}