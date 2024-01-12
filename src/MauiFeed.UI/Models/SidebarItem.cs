using System.ComponentModel;
using System.Runtime.CompilerServices;
using MauiFeed.Models;

namespace MauiFeed.UI.Models;

public partial class SidebarItem : INotifyPropertyChanged
{
    private string title;

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

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

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
    public MauiFeed.UI.Models.SidebarItemType SidebarItemType { get; }

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

    /// <summary>
    /// Gets or sets a value indicating whether to hide unread items.
    /// </summary>
    public bool AlwaysHideUnread { get; set; }

    /// <summary>
    /// On Property Changed.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        var changed = this.PropertyChanged;
        if (changed == null)
        {
            return;
        }

        changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

#pragma warning disable SA1600 // Elements should be documented
    private bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "",
        Action? onChanged = null)
#pragma warning restore SA1600 // Elements should be documented
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
        {
            return false;
        }

        backingStore = value;
        onChanged?.Invoke();
        this.OnPropertyChanged(propertyName);
        return true;
    }
}