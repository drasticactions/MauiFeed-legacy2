using System.Diagnostics.CodeAnalysis;
using MauiFeed.Models;
using MauiFeed.Services;
using MauiFeed.Translations;
using MauiFeed.UI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ObjCRuntime;

namespace MauiFeed.UI.Views;

/// <summary>
/// Sidebar View Controller.
/// </summary>
public sealed partial class SidebarViewController : UIViewController, IUICollectionViewDelegate
{
    private readonly Action<SidebarItem> onSidebarItemSelected;
    private readonly NSString smartFiltersSectionIdentifier = new NSString(Guid.NewGuid().ToString());
    private readonly NSString unorganizedItemsSectionIdentifier = new NSString(Guid.NewGuid().ToString());
    private readonly NSString foldersSectionIdentifier = new NSString(Guid.NewGuid().ToString());
    private readonly RssFeedCacheService cache;
    private readonly DatabaseContext context;
    private UICollectionView collectionView;
    private UICollectionViewDiffableDataSource<NSString, SidebarItem>? dataSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="SidebarViewController"/> class.
    /// </summary>
    /// <param name="provider">Provider.</param>
    /// <param name="onSidebarItemSelected">Fired when sidebar item is selected.</param>
    public SidebarViewController(IServiceProvider provider, Action<SidebarItem?> onSidebarItemSelected)
    {
        this.onSidebarItemSelected = onSidebarItemSelected;
        this.cache = provider.GetRequiredService<RssFeedCacheService>();
        this.context = provider.GetRequiredService<DatabaseContext>();
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

        this.ConfigureDataSource();
        this.GenerateItems();
    }

    private void GenerateSmartFilters(bool animated = false)
    {
        var snapshot = new NSDiffableDataSourceSectionSnapshot<SidebarItem>();
        var filters = new SidebarItem(Common.SmartFeedsLabel, UIImage.GetSystemImage("gearshape")!) { };
        snapshot.AppendItems(new[] { filters });
        snapshot.ExpandItems(new[] { filters });
        var filterList = new List<SidebarItem>
        {
            new SidebarItem(Common.AllLabel, UIImage.GetSystemImage("list.bullet.rectangle")!, this.OnFeedSelected,
                SidebarItemType.SmartFilter),
            new SidebarItem(Common.TodayLabel, UIImage.GetSystemImage("sun.max")!, this.OnFeedSelected,
                SidebarItemType.SmartFilter),
            new SidebarItem(Common.AllUnreadLabel, UIImage.GetSystemImage("eye")!, this.OnFeedSelected,
                SidebarItemType.SmartFilter),
            new SidebarItem(Common.StarredLabel, UIImage.GetSystemImage("star.square.fill")!, this.OnFeedSelected,
                SidebarItemType.SmartFilter),
        };
        snapshot.AppendItems(filterList.ToArray(), filters);
        this.dataSource!.ApplySnapshot(snapshot, this.smartFiltersSectionIdentifier, animated);
    }

    private void GenerateFeedFolders(bool animated = false)
    {
        var snapshot = new NSDiffableDataSourceSectionSnapshot<SidebarItem>();
        foreach (var item in this.context.FeedFolder!.Include(n => n.Items))
        {
            var query = this.context.FeedItems!.Include(n => n.Feed).Where(n => (n.Feed!.FolderId ?? 0) == item.Id);
            var sidebarItem = new SidebarItem(item, query, this.OnFeedSelected);
            snapshot.AppendItems(new[] { sidebarItem });
            snapshot.ExpandItems(new[] { sidebarItem });
            snapshot.AppendItems(
                (from feedItem in item.Items ?? new List<FeedListItem>()
                    select new SidebarItem(
                        feedItem,
                        this.context.FeedItems!.Include(n => n.Feed).Where(n => n.FeedListItemId == item.Id),
                        this.OnFeedSelected)).ToArray(),
                sidebarItem);
        }

        this.dataSource!.ApplySnapshot(snapshot, this.foldersSectionIdentifier, animated);
    }

    private void GenerateUnorganizedItems(bool animated = false)
    {
        var snapshot = new NSDiffableDataSourceSectionSnapshot<SidebarItem>();
        foreach (var item in this.context.FeedListItems!.Include(n => n.Items)
                     .Where(n => n.FolderId == null || n.FolderId <= 0))
        {
            var sidebarItem = new SidebarItem(
                item,
                this.context.FeedItems!.Include(n => n.Feed).Where(n => n.FeedListItemId == item.Id),
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
                cell.AddInteraction(new MenuInteraction(sidebarItem, this.RemoveSidebarItem));
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
                        cfg.Image = sidebarItem.Image?.AddBackgroundWithInsetAndResize(UIColor.White, 10,
                            new CGSize(25, 25));
                        if (cfg.Image is not null)
                        {
                            cfg.ImageProperties.CornerRadius = 3;
                            cfg.ImageToTextPadding = 5;
                            cfg.ImageProperties.ReservedLayoutSize = new CGSize(30, 30);
                        }

                        break;
                    }

                    case SidebarItemType.SmartFilter:
                        cfg.Image = sidebarItem.Image;
                        break;
                }

                cell.ContentConfiguration = cfg;
                cell.AddInteraction(new MenuInteraction(sidebarItem, this.RemoveSidebarItem));
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
    public class MenuInteraction(SidebarItem item, Action<SidebarItem> onRemoved)
        : UIContextMenuInteraction(CreateDelegate(out _))
    {
        private static IUIContextMenuInteractionDelegate CreateDelegate(out IUIContextMenuInteractionDelegate del) =>
            del = new FlyoutUIContextMenuInteractionDelegate();

        private UIContextMenuConfiguration? GetConfigurationForMenu()
        {
            var remove = item.SidebarItemType switch
            {
                SidebarItemType.FeedListItem => UIAction.Create(
                    Common.RemoveFeedLabel,
                    UIImage.GetSystemImage("delete.left"),
                    null,
                    action => { onRemoved.Invoke(item); }),
                SidebarItemType.Folder => UIAction.Create(
                    Common.RemoveFolderLabel,
                    UIImage.GetSystemImage("delete.left"),
                    null,
                    action => { onRemoved.Invoke(item); }),
                _ => throw new Exception("Unsupported."),
            };
            var uiMenu = UIMenu.Create(new UIMenuElement[]
            {
                remove!,
            });
            return UIContextMenuConfiguration.Create(
                identifier: null,
                previewProvider: null,
                actionProvider: _ => uiMenu);
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

    private void GenerateItems(bool animated = false)
    {
        this.GenerateSmartFilters(animated);
        this.GenerateUnorganizedItems(animated);
        this.GenerateFeedFolders(animated);
    }


    public async Task AddOrUpdateFeed(string uri)
    {
        var test = await this.cache.RetrieveFeedAsync(uri);
        this.GenerateItems(true);
    }

    public async Task RefreshFeed()
    {
        await this.cache.RefreshFeedsAsync();
        this.UpdateDataSource();
    }

    private void RemoveSidebarItem(SidebarItem item)
    {
        switch (item.SidebarItemType)
        {
            case SidebarItemType.FeedListItem:
                this.context.RemoveFeedListItem(item.FeedListItem!);
                break;
            case SidebarItemType.Folder:
                this.context.RemoveFeedFolder(item.FeedFolder!);
                break;
        }

        this.GenerateItems(true);
    }
}