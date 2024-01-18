// <copyright file="SidebarViewController.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using Drastic.Services;
using Drastic.Tools;
using MauiFeed.Models;
using MauiFeed.Services;
using MauiFeed.Translations;
using MauiFeed.UI.Models;
using Microsoft.Extensions.DependencyInjection;
using Realms;

namespace MauiFeed.UI.Views;

/// <summary>
/// Sidebar View Controller.
/// </summary>
public sealed partial class SidebarViewController : UIViewController, IUICollectionViewDelegate, IDisposable
{
    private readonly Action<SidebarItem?> onSidebarItemSelected;
    private readonly NSString smartFiltersSectionIdentifier = new NSString(Guid.NewGuid().ToString());
    private readonly NSString unorganizedItemsSectionIdentifier = new NSString(Guid.NewGuid().ToString());
    private readonly NSString foldersSectionIdentifier = new NSString(Guid.NewGuid().ToString());
    private readonly FeedService cache;
    private readonly Realm context;
    private readonly FeedFolderService folderService;
    private UICollectionView collectionView;
    private UICollectionViewDiffableDataSource<NSString, SidebarItem>? dataSource;
    private OptionsMenu optionsMenu;

    private List<SidebarItem> filterList = new List<SidebarItem>();
    private IErrorHandlerService errorHandler;
    private IDisposable? feedFolderSubscription;
    private IDisposable? feedListItemSubscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="SidebarViewController"/> class.
    /// </summary>
    /// <param name="provider">Provider.</param>
    /// <param name="onSidebarItemSelected">Fired when sidebar item is selected.</param>
    public SidebarViewController(IServiceProvider provider, Action<SidebarItem?> onSidebarItemSelected)
    {
        this.optionsMenu = new OptionsMenu(this, provider);
        this.onSidebarItemSelected = onSidebarItemSelected;
        this.context = Realm.GetInstance(provider.GetRequiredService<RealmConfigurationBase>());
        this.cache = provider.GetRequiredService<FeedService>();
        this.folderService = provider.GetRequiredService<FeedFolderService>();
        this.errorHandler = provider.GetRequiredService<IErrorHandlerService>();
        this.feedFolderSubscription = this.context.All<FeedFolder>().SubscribeForNotifications(this.OnFeedFolderUpdate);
        this.feedListItemSubscription = this.context.All<FeedListItem>().SubscribeForNotifications(this.OnFeedListItemUpdate);
        // this.context.OnFeedFolderRemove += this.OnFeedFolderRemove;
        // this.context.OnFeedFolderUpdate += this.OnFeedFolderUpdate;
        // this.context.OnFeedListItemRemove += this.OnFeedListItemRemove;
        // this.context.OnFeedListItemUpdate += this.OnFeedListItemUpdate;
        // this.context.OnRefreshFeeds += this.OnRefreshFeeds;
        this.collectionView = new UICollectionView(this.View!.Bounds, this.CreateLayout());
        this.collectionView.Delegate = this;
#if !TVOS
        this.collectionView.DragInteractionEnabled = true;
        this.collectionView.DragDelegate = this;
        this.collectionView.DropDelegate = this;
#endif

        this.View.AddSubview(this.collectionView);
        this.collectionView.TranslatesAutoresizingMaskIntoConstraints = false;

        NSLayoutConstraint.ActivateConstraints(new[]
        {
            this.collectionView.TopAnchor.ConstraintEqualTo(this.View.TopAnchor),
            this.collectionView.BottomAnchor.ConstraintEqualTo(this.View.BottomAnchor),
            this.collectionView.LeadingAnchor.ConstraintEqualTo(this.View.LeadingAnchor),
            this.collectionView.TrailingAnchor.ConstraintEqualTo(this.View.TrailingAnchor),
        });

        this.filterList = new List<SidebarItem>
        {
            new SidebarItem(Common.AllLabel, UIImage.GetSystemImage("list.bullet.rectangle")!,
                this.context.All<FeedItem>(), this.OnFeedSelected,
                SidebarItemType.SmartFilter),
            new SidebarItem(Common.TodayLabel, UIImage.GetSystemImage("sun.max")!,
                this.context.All<FeedItem>().Where(n =>
                    n.PublishingDate == DateTimeOffset.UtcNow),
                this.OnFeedSelected,
                SidebarItemType.SmartFilter),
            new SidebarItem(Common.AllUnreadLabel, UIImage.GetSystemImage("eye")!,
                this.context.All<FeedItem>().Where(n => !n.IsRead), this.OnFeedSelected,
                SidebarItemType.SmartFilter),
            new SidebarItem(Common.StarredLabel, UIImage.GetSystemImage("star.square.fill")!,
                this.context.All<FeedItem>().Where(n => n.IsFavorite), this.OnFeedSelected,
                SidebarItemType.SmartFilter),
        };

        this.ConfigureDataSource();
        this.GenerateSmartFilters();
        //this.GenerateItems();

#if IOS
        this.NavigationItem.SetRightBarButtonItem(new UIBarButtonItem(UIBarButtonSystemItem.Add, this.optionsMenu.AddMenu), false);
#endif
    }

    private void OnRefreshFeeds(object? sender, EventArgs e)
    {
        // this.GenerateItems(true);
    }

    /// <summary>
    /// Fired when Item is Selected.
    /// </summary>
    /// <param name="collectionView">CollectionView.</param>
    /// <param name="indexPath">Index Path.</param>
    [Export("collectionView:didSelectItemAtIndexPath:")]
    protected void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
    {
        var item = this.dataSource?.GetItemIdentifier(indexPath);
        this.onSidebarItemSelected(item);
    }

    private void GenerateSmartFilters(bool animated = false)
    {
        var snapshot = new NSDiffableDataSourceSectionSnapshot<SidebarItem>();
        var filters = new SidebarItem(Common.SmartFeedsLabel, UIImage.GetSystemImage("gearshape")!) { };
        snapshot.AppendItems(new[] { filters });
        snapshot.ExpandItems(new[] { filters });
        snapshot.AppendItems(filterList.ToArray(), filters);
        this.dataSource!.ApplySnapshot(snapshot, this.smartFiltersSectionIdentifier, animated);
    }

    private void GenerateFeedFolders(IRealmCollection<FeedFolder> folders, bool animated = false)
    {
        var snapshot = new NSDiffableDataSourceSectionSnapshot<SidebarItem>();
        foreach (var item in folders)
        {
            var sidebarItem = new SidebarItem(item, this.context.All<FeedItem>().Filter("Feed.Folder.Id == $0", item.Id), this.OnFeedSelected);
            snapshot.AppendItems(new[] { sidebarItem });
            snapshot.ExpandItems(new[] { sidebarItem });
            snapshot.AppendItems(
                (from feedItem in item.Items ?? new List<FeedListItem>()
                 select new SidebarItem(
                     feedItem,
                     feedItem.Items,
                     this.OnFeedSelected)).ToArray(),
                sidebarItem);
        }

        this.dataSource!.ApplySnapshot(snapshot, this.foldersSectionIdentifier, animated);
    }

    private void GenerateUnorganizedItems(IRealmCollection<FeedListItem> items, bool animated = false)
    {
        var snapshot = new NSDiffableDataSourceSectionSnapshot<SidebarItem>();
        foreach (var item in items
                     .Where(n => n.Folder == null))
        {
            var sidebarItem = new SidebarItem(
                item,
                this.context.All<FeedItem>().Where(n => n.Feed == item),
                this.OnFeedSelected);
            snapshot.AppendItems(new[] { sidebarItem });
        }

        this.dataSource!.ApplySnapshot(snapshot, this.unorganizedItemsSectionIdentifier, animated);
    }

    private void OnFeedSelected()
    {
        var indexPath = this.collectionView.GetIndexPathsForSelectedItems()?.FirstOrDefault();
        if (indexPath is null)
        {
            // Log Error.
            return;
        }

        var item = this.dataSource?.GetItemIdentifier(indexPath);
        if (item is null)
        {
            // Log Error.
            return;
        }

        this.onSidebarItemSelected(item);
    }

    private void ConfigureDataSource()
    {
        var headerRegistration = UICollectionViewCellRegistration.GetRegistration(
            typeof(UICollectionViewListCell),
            new UICollectionViewCellRegistrationConfigurationHandler((cell, indexpath, item) =>
            {
                var sidebarItem = (SidebarItem)item;
#if !TVOS
                var contentConfiguration = UIListContentConfiguration.SidebarHeaderConfiguration;
                ((UICollectionViewListCell)cell).Accessories = new UICellAccessory[]
                {
                    sidebarItem.UnreadCountView,
                    new UICellAccessoryOutlineDisclosure(),
                };
#else
                var contentConfiguration = UIListContentConfiguration.GroupedHeaderConfiguration;
                ((UICollectionViewListCell)cell).Accessories = new UICellAccessory[]
                {
                    sidebarItem.UnreadCountView,
                    new UICellAccessoryDisclosureIndicator(),
                };
#endif
                contentConfiguration.Text = sidebarItem.Title;
                contentConfiguration.TextProperties.Font = UIFont.PreferredSubheadline;
                contentConfiguration.TextProperties.Color = UIColor.SecondaryLabel;
                cell.ContentConfiguration = contentConfiguration;
                cell.AddInteraction(new MenuInteraction(sidebarItem, this.RemoveSidebarItem,
                    this.RemoveSidebarItemFromFolder));
            }));

        var rowRegistration = UICollectionViewCellRegistration.GetRegistration(
            typeof(UICollectionViewListCell),
            new UICollectionViewCellRegistrationConfigurationHandler((cell, indexpath, item) =>
            {
                var sidebarItem = item as SidebarItem;
                if (sidebarItem is null)
                {
                    return;
                }

#if TVOS
                var cfg = UIListContentConfiguration.CellConfiguration;
#else
                var cfg = UIListContentConfiguration.SidebarCellConfiguration;
#endif
                cfg.Text = sidebarItem.Title;
                switch (sidebarItem.SidebarItemType)
                {
                    case SidebarItemType.FeedListItem:
                        {
                            cfg.Image = (UIImage?)sidebarItem.Image;
                            if (cfg.Image is not null)
                            {
                                cfg.ImageProperties.CornerRadius = 3;
                                cfg.ImageToTextPadding = 5;
                                cfg.ImageProperties.ReservedLayoutSize = new CGSize(30, 30);
                            }

                            break;
                        }

                    case SidebarItemType.SmartFilter:
                        cfg.Image = (UIImage?)sidebarItem.Image;
                        break;
                }

                cell.ContentConfiguration = cfg;
                cell.AddInteraction(new MenuInteraction(sidebarItem, this.RemoveSidebarItem,
                    this.RemoveSidebarItemFromFolder));
                ((UICollectionViewListCell)cell).Accessories = new UICellAccessory[] { sidebarItem.UnreadCountView };
            }));

        if (this.collectionView is null)
        {
            throw new NullReferenceException(nameof(this.collectionView));
        }

        this.dataSource = new UICollectionViewDiffableDataSource<NSString, SidebarItem>(
            this.collectionView,
            new UICollectionViewDiffableDataSourceCellProvider((collectionView, indexPath, item) =>
            {
                var sidebarItem = item as SidebarItem;
                if (sidebarItem is null || collectionView is null)
                {
                    throw new Exception();
                }

                return sidebarItem.SidebarItemType switch
                {
                    SidebarItemType.Other => collectionView.DequeueConfiguredReusableCell(
                        headerRegistration,
                        indexPath,
                        item),
                    SidebarItemType.Folder => collectionView.DequeueConfiguredReusableCell(
                        headerRegistration,
                        indexPath,
                        item),
                    _ =>
                        collectionView.DequeueConfiguredReusableCell(rowRegistration, indexPath, item),
                };
            })
        );
    }

    private void UpdateDataSource()
    {
        Task.Run(() =>
        {
            if (this.dataSource?.Snapshot is null)
            {
                return;
            }

            foreach (var item in this.dataSource.Snapshot.ItemIdentifiers)
            {
                item.Update();
            }
        });
    }

    private UICollectionViewLayout CreateLayout()
    {
        return new UICollectionViewCompositionalLayout((sectionIndex, layoutEnvironment) =>
        {
#if TVOS
            var configuration = new UICollectionLayoutListConfiguration(UICollectionLayoutListAppearance.Grouped);
            configuration.HeaderMode = UICollectionLayoutListHeaderMode.None;
            return NSCollectionLayoutSection.GetSection(configuration, layoutEnvironment);
#else
            var configuration = new UICollectionLayoutListConfiguration(UICollectionLayoutListAppearance.Sidebar);
            configuration.ShowsSeparators = false;
            configuration.HeaderMode = UICollectionLayoutListHeaderMode.None;
            return NSCollectionLayoutSection.GetSection(configuration, layoutEnvironment);
#endif
        });
    }

    /// <summary>
    /// Menu Interaction.
    /// </summary>
    public class MenuInteraction(
        SidebarItem item,
        Action<SidebarItem> onRemoved,
        Action<SidebarItem> onRemovedFromFolder)
        : UIContextMenuInteraction(CreateDelegate(out _))
    {
        private static IUIContextMenuInteractionDelegate CreateDelegate(out IUIContextMenuInteractionDelegate del) =>
            del = new FlyoutUIContextMenuInteractionDelegate();

        private UIContextMenuConfiguration? GetConfigurationForMenu()
        {
            if (item.SidebarItemType == SidebarItemType.Folder)
            {
                return UIContextMenuConfiguration.Create(
                    identifier: null,
                    previewProvider: null,
                    actionProvider: _ => UIMenu.Create(new UIMenuElement[]
                    {
                        UIAction.Create(
                            Common.RemoveFolderLabel,
                            UIImage.GetSystemImage("delete.left"),
                            null,
                            action => { onRemoved.Invoke(item); }),
                    }));
            }

            if (item.SidebarItemType == SidebarItemType.FeedListItem)
            {
                var removeFeed = UIAction.Create(
                    Common.RemoveFeedLabel,
                    UIImage.GetSystemImage("delete.left"),
                    null,
                    action => { onRemoved.Invoke(item); });
                var removeFromFolder = UIAction.Create(
                    Common.RemoveFromFolderLabel,
                    UIImage.GetSystemImage("delete.left"),
                    null,
                    action => { onRemovedFromFolder.Invoke(item); });
                var menu = item.FeedListItem?.Folder is not null
                    ? new UIMenuElement[] { removeFeed, removeFromFolder }
                    : new UIMenuElement[] { removeFeed };
                return UIContextMenuConfiguration.Create(
                    identifier: null,
                    previewProvider: null,
                    actionProvider: _ => UIMenu.Create(menu));
            }

            return null;
        }

        private sealed class FlyoutUIContextMenuInteractionDelegate : NSObject, IUIContextMenuInteractionDelegate
        {
            public FlyoutUIContextMenuInteractionDelegate()
            {
            }

            public UIContextMenuConfiguration? GetConfigurationForMenu(
                UIContextMenuInteraction interaction,
                CGPoint location)
            {
                if (interaction is MenuInteraction contextMenu)
                {
                    return contextMenu.GetConfigurationForMenu();
                }

                return null;
            }
        }
    }

    public async Task AddOrUpdateFeed(string uri)
    {
        await this.cache.ReadFeedAsync(uri);
    }

    public async Task RefreshFeed()
    {
        this.UpdateDataSource();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        this.feedFolderSubscription?.Dispose();
        this.feedListItemSubscription?.Dispose();
    }

    private async void RemoveSidebarItemFromFolder(SidebarItem item)
    {
        if (item.SidebarItemType != SidebarItemType.FeedListItem)
        {
            return;
        }

        if (item.FeedListItem is null)
        {
            return;
        }

        if (item.FeedFolder is null)
        {
            return;
        }

        this.folderService.RemoveFeedFromFolderAsync(item.FeedFolder, item.FeedListItem).FireAndForgetSafeAsync(this.errorHandler);
    }

    private async void RemoveSidebarItem(SidebarItem item)
    {
        switch (item.SidebarItemType)
        {
            case SidebarItemType.FeedListItem:
                await this.folderService.RemoveFeedAsync(item.FeedListItem!);
                break;
            case SidebarItemType.Folder:
                await this.folderService.RemoveFeedFolderAsync(item.FeedFolder!);
                break;
        }
    }

    private void OnFeedListItemUpdate(IRealmCollection<FeedListItem> sender, ChangeSet? changes)
    {
        if (changes is null)
        {
            return;
        }

        this.GenerateUnorganizedItems(sender, true);
        this.filterList.ForEach(n => n.Update());
    }

    private void OnFeedFolderUpdate(IRealmCollection<FeedFolder> sender, ChangeSet? changes)
    {
        this.GenerateFeedFolders(sender, true);
        this.filterList.ForEach(n => n.Update());
    }
}
