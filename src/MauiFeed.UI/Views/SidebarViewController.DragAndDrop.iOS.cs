using MauiFeed.UI.Models;
using MobileCoreServices;

namespace MauiFeed.UI.Views;

/// <summary>
/// Sidebar View Controller.
/// </summary>
#if !TVOS
public sealed partial class SidebarViewController : IUICollectionViewDragDelegate, IUICollectionViewDropDelegate
{
    /// <summary>
    /// Can Handle Drop Session.
    /// </summary>
    /// <param name="collectionView">Collection View.</param>
    /// <param name="session">Session.</param>
    /// <returns>Bool.</returns>
    [Export("collectionView:canHandleDropSession:")]
    public bool CanHandleDropSession(UICollectionView collectionView, IUIDropSession session)
    {
        return true;
    }

    /// <summary>
    /// Drop Session Did Update.
    /// </summary>
    /// <param name="collectionView">Collection View.</param>
    /// <param name="session">UI Drop Session.</param>
    /// <param name="destinationIndexPath">Destination Index Path.</param>
    /// <returns><see cref="UICollectionViewDropProposal"/>.</returns>
    [Export("collectionView:dropSessionDidUpdate:withDestinationIndexPath:")]
    public UICollectionViewDropProposal DropSessionDidUpdate(
        UICollectionView collectionView,
        IUIDropSession session,
        NSIndexPath? destinationIndexPath)
    {
        if (destinationIndexPath is null)
        {
            return new UICollectionViewDropProposal(
                UIDropOperation.Forbidden,
                UICollectionViewDropIntent.InsertIntoDestinationIndexPath);
        }

        var operation = UIDropOperation.Forbidden;

        if (session.LocalDragSession is null)
        {
            operation = UIDropOperation.Forbidden;
        }

        var item = this.dataSource?.GetItemIdentifier(destinationIndexPath);
        if (item is null)
        {
            operation = UIDropOperation.Forbidden;
        }

        if (item?.SidebarItemType != SidebarItemType.Folder)
        {
            operation = UIDropOperation.Forbidden;
        }

        operation = UIDropOperation.Move;

        return new UICollectionViewDropProposal(
            operation,
            UICollectionViewDropIntent.InsertIntoDestinationIndexPath);
    }

    /// <summary>
    /// Get Items For Beginning Drag Session.
    /// </summary>
    /// <param name="collectionView">Collection View.</param>
    /// <param name="session">Session.</param>
    /// <param name="indexPath">Index Path.</param>
    /// <returns>Array of <see cref="UIDragItem"/>.</returns>
    public UIDragItem[] GetItemsForBeginningDragSession(
        UICollectionView collectionView,
        IUIDragSession session,
        NSIndexPath indexPath)
    {
        var item = this.dataSource?.GetItemIdentifier(indexPath);
        if (item is null)
        {
            return Array.Empty<UIDragItem>();
        }

        var data = NSData.FromString($"{indexPath.Section},{indexPath.Row}");
        var itemProvider = new NSItemProvider();
#pragma warning disable CA1416 // プラットフォームの互換性を検証
#pragma warning disable CA1422 // プラットフォームの互換性を検証
        itemProvider.RegisterDataRepresentation(UTType.PlainText, NSItemProviderRepresentationVisibility.OwnProcess, (completionHandler) =>
            {
#nullable disable
                completionHandler(data, null);
                return null;
#nullable enable
            });

#pragma warning restore CA1422 // プラットフォームの互換性を検証
#pragma warning restore CA1416 // プラットフォームの互換性を検証

        return new UIDragItem[] { new UIDragItem(itemProvider) { LocalObject = item } };
    }

    /// <summary>
    /// Perform Drop Operation.
    /// </summary>
    /// <param name="collectionView">Collection View.</param>
    /// <param name="coordinator">Coordinator.</param>
    public void PerformDrop(UICollectionView collectionView, IUICollectionViewDropCoordinator coordinator)
    {
        if (coordinator.DestinationIndexPath is null)
        {
            return;
        }

        var destinationIndexPath = coordinator.DestinationIndexPath;
        var destinationIndex = destinationIndexPath.Item;
        var folder = this.dataSource?.GetItemIdentifier(destinationIndexPath);
        var sidebarItem = coordinator.Items[0].DragItem.LocalObject as SidebarItem;
        if (sidebarItem?.FeedListItem is null)
        {
            return;
        }

        if (folder?.FeedFolder is null || folder.SidebarItemType is not SidebarItemType.Folder)
        {
            return;
        }
        else
        {
            sidebarItem.FeedListItem.FolderId = folder.FeedFolder.Id;
        }

        this.context.AddOrUpdateFeedListItem(sidebarItem.FeedListItem);

        this.GenerateItems(true);
    }
}
#endif