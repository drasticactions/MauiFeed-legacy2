using Foundation;
using MauiFeed.Models;
using UIKit;

namespace MauiFeed.UI.Models;

public partial class SidebarItem : NSObject
{
    public SidebarItem(string title, UIImage image, Action? onSelected = default, SidebarItemType type = SidebarItemType.Other)
    {
        this.SidebarItemType = type;
        this.title = title;
        this.Image = image;
        this.OnSelected = onSelected;
        this.Update();
    }

    public SidebarItem(string title, UIImage image, IQueryable<FeedItem> query, Action? onSelected = default, SidebarItemType type = SidebarItemType.Other)
    {
        this.SidebarItemType = type;
        this.Query = query;
        this.title = title;
        this.Image = image;
        this.OnSelected = onSelected;
        this.Update();
    }

    public UIImage? Image { get; private set; }

    public MauiFeed.UI.Views.UnreadCountView UnreadCountView { get; } = new MauiFeed.UI.Views.UnreadCountView(new MauiFeed.UI.Views.PaddingLabel());

    public void Update()
    {
        this.UnreadCountView.SetUnreadCount(this.UnreadCount);
    }

    private void InitializeImage()
    {
        switch (this.SidebarItemType)
        {
            case SidebarItemType.FeedListItem:
                if (this.FeedListItem?.ImageCache is null)
                {
                    this.Image = UIImage.LoadFromData(NSData.FromArray(Utilities.GetPlaceholderIcon()))!.ScalePreservingAspectRatio(new CGSize(25, 25));
                    break;
                }

                var cache = this.FeedListItem.ImageCache;
                var image = UIImage.LoadFromData(NSData.FromArray(cache));
                if (image is null)
                {
                    break;
                }
                this.Image = image.ScalePreservingAspectRatio(new CGSize(25, 25));
                break;
        }
    }
}