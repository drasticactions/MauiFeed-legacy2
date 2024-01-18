using MauiFeed.Models;

namespace MauiFeed.Adapters;

public class SidebarViewAdapter : IVirtualListViewAdapter<ISidebarItem, ISidebarHeader>
{
    public int GetNumberOfSections()
    {
        throw new NotImplementedException();
    }

    public ISidebarHeader GetSection(int sectionIndex)
    {
        throw new NotImplementedException();
    }

    public int GetNumberOfItemsInSection(int sectionIndex)
    {
        throw new NotImplementedException();
    }

    public ISidebarItem GetItem(int sectionIndex, int itemIndex)
    {
        throw new NotImplementedException();
    }

    public event EventHandler? OnDataInvalidated;

    public virtual void InvalidateData()
    {
        throw new NotImplementedException();
    }
}