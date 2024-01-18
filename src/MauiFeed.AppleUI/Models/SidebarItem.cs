// <copyright file="SidebarItem.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using MauiFeed.Models;
using MauiFeed.UI.Tools;

namespace MauiFeed.UI.Models;

public class SidebarItem : NSObject, ISidebarItem
{
    private string title;

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

    public SidebarItem(
        FeedListItem item,
        IEnumerable<FeedItem> query,
        Action onSelected)
    {
        this.OnSelected = onSelected;
        this.SidebarItemType = SidebarItemType.FeedListItem;
        this.FeedListItem = item;
        this.Query = query;
        this.title = this.FeedListItem?.Name ?? string.Empty;
        this.Id = Guid.NewGuid();
        this.Update();
        this.InitializeImage();
    }

    public SidebarItem(
        string title,
        IEnumerable<FeedItem> query,
        Action onSelected)
    {
        this.title = title;
        this.Query = query;
        this.OnSelected = onSelected;
        this.SidebarItemType = SidebarItemType.SmartFilter;
        this.Update();
        this.InitializeImage();
    }

    public SidebarItem(
        FeedFolder folder,
        IEnumerable<FeedItem> query,
        Action onSelected)
    {
        this.FeedFolder = folder;
        this.OnSelected = onSelected;
        this.Query = query;
        this.SidebarItemType = SidebarItemType.Folder;
        this.title = this.FeedFolder.Name ?? string.Empty;
        this.Update();
        this.InitializeImage();
    }

    public UIImage? Image { get; private set; }

    public MauiFeed.UI.Views.UnreadCountView UnreadCountView { get; } = new MauiFeed.UI.Views.UnreadCountView(new MauiFeed.UI.Views.PaddingLabel());

    /// <summary>
    /// Gets the id.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the title.
    /// </summary>
    public string Title => this.title;

    /// <summary>
    /// Gets the type of sidebar item.
    /// </summary>
    public SidebarItemType SidebarItemType { get; }

    /// <summary>
    /// Gets or sets an action which fires when the item is selected.
    /// </summary>
    public Action? OnSelected { get; set; }

    /// <summary>
    /// Gets a value indicating whether the item is enabled for selection.
    /// </summary>
    public bool IsEnabled => this.SidebarItemType != SidebarItemType.Other;

    /// <summary>
    /// Gets a value indicating whether the item allows dropping other items on top of it.
    /// </summary>
    public bool AllowDrop => this.SidebarItemType == SidebarItemType.Folder;

    /// <summary>
    /// Gets the Feed List Item, optional.
    /// </summary>
    public IEnumerable<FeedItem>? Query { get; }

    /// <summary>
    /// Gets the queryable for feed items. Optional.
    /// </summary>
    public FeedListItem? FeedListItem { get; }

    /// <summary>
    /// Gets the feed folder, Optional.
    /// </summary>
    public FeedFolder? FeedFolder { get; }

    /// <summary>
    /// Gets the unread count.
    /// </summary>
    public int UnreadCount => this.Query?.Where(n => !n.IsRead).Count() ?? 0;

    /// <inheritdoc/>
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
