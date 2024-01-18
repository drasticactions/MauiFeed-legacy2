// <copyright file="FeedSidebarItem.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using MauiFeed.Events;
using MauiFeed.Tools;
using MauiFeed.WinUI.Tools;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using WinUICommunity;

namespace MauiFeed.Models;

public class FeedSidebarItem : ISidebarItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSidebarItem"/> class.
    /// </summary>
    /// <param name="feedListItem">Feed List Item.</param>
    /// <param name="query">Optional query parameter.</param>
    public FeedSidebarItem(FeedListItem feedListItem, IQueryable<Models.FeedItem>? query = default)
    {
        this.Id = Guid.NewGuid();
        this.FeedListItem = feedListItem;

        byte[] cache;

        if (!this.FeedListItem.HasValidImage())
        {
            cache = Utilities.GetPlaceholderIcon();
        }
        else
        {
            cache = this.FeedListItem.ImageCache!;
        }

        var icon = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
        icon.SetSource(cache.ToRandomAccessStream());

        this.Query = query;
        this.NavItem = new NavigationViewItem() { Content = feedListItem.Name, Icon = new ImageIcon() { Source = icon, Width = 30, Height = 30, }, Tag = this.Id };
        this.NavItem.CanDrag = true;
        this.NavItem.Loaded += this.NavItemLoaded;
        this.SidebarItemType = SidebarItemType.FeedListItem;
        this.Update();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSidebarItem"/> class.
    /// </summary>
    /// <param name="folder">Feed Folder.</param>
    /// <param name="query">Optional query parameter.</param>
    public FeedSidebarItem(FeedFolder folder, IQueryable<FeedItem>? query = default)
    {
        this.Id = Guid.NewGuid();
        this.Query = query;
        this.FeedFolder = folder;
        this.SidebarItemType = SidebarItemType.Folder;
        this.NavItem = new NavigationViewItem() { Content = folder.Name, Icon = new SymbolIcon(Symbol.Folder), Tag = this.Id };
        this.NavItem.Loaded += this.NavItemLoaded;
        this.NavItem.AllowDrop = true;
        this.Update();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSidebarItem"/> class.
    /// </summary>
    /// <param name="title">The Title.</param>
    /// <param name="icon">The Icon.</param>
    /// <param name="query">Optional query parameter.</param>
    public FeedSidebarItem(string title, IconElement icon, IQueryable<FeedItem>? query = default)
    {
        this.Id = Guid.NewGuid();
        this.Query = query;
        this.NavItem = new NavigationViewItem() { Content = title, Icon = icon, Tag = this.Id };
        this.SidebarItemType = SidebarItemType.SmartFilter;
        this.Update();
    }

    /// <summary>
    /// Event fired when folder gets item dropped.
    /// </summary>
    public event EventHandler<FeedFolderDropEventArgs>? OnFolderDropped;

    /// <summary>
    /// Event fired when nav item is right tapped.
    /// </summary>
    public event EventHandler<NavItemRightTappedEventArgs>? RightTapped;

    /// <summary>
    /// Gets the id.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the navigation view item.
    /// </summary>
    public NavigationViewItem NavItem { get; }

    /// <summary>
    /// Gets the title.
    /// </summary>
    /// <inheritdoc/>
    public string Title
    {
        get
        {
            if (this.NavItem.Content is string result)
            {
                return result;
            }

            if (this.FeedListItem is not null)
            {
                return this.FeedListItem.Name ?? string.Empty;
            }

            if (this.FeedFolder is not null)
            {
                return this.FeedFolder.Name ?? string.Empty;
            }

            return string.Empty;
        }
    }

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
        var count = this.UnreadCount;
        if (count > 0)
        {
            this.NavItem.InfoBadge = new InfoBadge() { Value = count };
        }
        else
        {
            this.NavItem.InfoBadge = null;
        }
    }

    private void NavItemLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var nav = (NavigationViewItem)sender!;
        nav.Loaded -= this.NavItemLoaded;
        var presenter = nav.FindDescendant<NavigationViewItemPresenter>();
        if (presenter is not null)
        {
            presenter.CanDrag = nav.CanDrag;
            presenter.AllowDrop = nav.AllowDrop;
            presenter.RightTapped += this.PresenterRightTapped;
            if (presenter.CanDrag)
            {
                presenter.DragStarting += this.PresenterDragStarting;
            }

            if (presenter.AllowDrop)
            {
                presenter.DragOver += this.PresenterDragOver;
                presenter.Drop += this.PresenterDrop;
            }
        }
    }

    private void PresenterRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        this.RightTapped?.Invoke(this, new NavItemRightTappedEventArgs(this));
    }

    private async void PresenterDrop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        var feedIdObject = await e.DataView.GetDataAsync(nameof(this.Id));
        if (feedIdObject is not Guid feedId)
        {
            return;
        }

        this.OnFolderDropped?.Invoke(this, new FeedFolderDropEventArgs(feedId, this));
    }

    private void PresenterDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
    }

    private void PresenterDragStarting(Microsoft.UI.Xaml.UIElement sender, Microsoft.UI.Xaml.DragStartingEventArgs args)
    {
        args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        args.Data.SetData(nameof(this.Id), this?.Id);
    }
}
