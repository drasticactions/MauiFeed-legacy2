using Foundation;
using MauiFeed.Models;
using MauiFeed.UI.Views;
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

    public UIImage? Image { get; private set; }

    public UnreadCountView UnreadCountView { get; } = new UnreadCountView(new PaddingLabel());

    public void Update()
    {
        this.UnreadCountView.SetUnreadCount(this.UnreadCount);
    }

    private void InitalizeImage()
    {
        switch (this.SidebarItemType)
        {
            case SidebarItemType.FeedListItem:
                var cache = this.FeedListItem?.ImageCache ??
                            throw new NullReferenceException(nameof(this.FeedListItem.ImageCache));
                this.Image = UIImage.LoadFromData(NSData.FromArray(cache));
                break;
        }
    }
}