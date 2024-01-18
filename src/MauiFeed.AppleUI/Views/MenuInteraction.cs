// <copyright file="MenuInteraction.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using MauiFeed.Models;
using MauiFeed.Translations;
using MauiFeed.UI.Models;

namespace MauiFeed.UI.Views;

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
