// <copyright file="ISidebarItem.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

namespace MauiFeed.Models;

/// <summary>
/// Sidebar Item.
/// </summary>
public interface ISidebarItem
{
    /// <summary>
    /// Gets the id.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the type of sidebar item.
    /// </summary>
    public SidebarItemType SidebarItemType { get; }

    /// <summary>
    /// Gets an action which fires when the item is selected.
    /// </summary>
    public Action? OnSelected { get; }

    /// <summary>
    /// Gets a value indicating whether the item is enabled for selection.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether the item allows dropping other items on top of it.
    /// </summary>
    public bool AllowDrop { get; }

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
    public int UnreadCount { get; }

    /// <summary>
    /// Update the item.
    /// </summary>
    public void Update();
}