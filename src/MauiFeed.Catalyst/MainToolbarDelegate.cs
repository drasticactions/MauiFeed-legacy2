using AppKit;
using Drastic.Services;
using Drastic.Tools;
using Foundation;
using MauiFeed.Models;
using MauiFeed.Models.OPML;
using MauiFeed.Services;
using MauiFeed.Translations;
using MauiFeed.UI.Views;
using MobileCoreServices;
using UIKit;

namespace MauiFeed.Catalyst;

/// <summary>
/// Main Toolbar Delegate.
/// </summary>
public class MainToolbarDelegate : AppKit.NSToolbarDelegate
{
    private const string Refresh = "Refresh";
    private const string Plus = "Plus";
    private const string MarkAllAsRead = "MarkAllAsRead";
    private const string MarkAsRead = "MarkAsRead";
    private const string HideRead = "HideRead";
    private const string Star = "Star";
    private const string NextUnread = "NextUnread";
    private const string ReaderView = "ReaderView";
    private const string Share = "Share";
    private const string OpenInBrowser = "OpenInBrowser";

    private RssFeedCacheService cache;
    private DatabaseContext db;

    private UIAlertController enterFeedUrlAlert;
    private UIAlertController enterFolderNameAlert;
    private UIViewController rootViewController;
    private UIDocumentPickerViewController documentPicker;
    private IErrorHandlerService errorHandler;
    private OpmlFeedListItemFactory opmlFeedListItemFactory;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="MainToolbarDelegate"/> class.
    /// </summary>
    /// <param name="controller">Root View Controller.</param>
    public MainToolbarDelegate(UIViewController controller, IServiceProvider services)
    {
        this.rootViewController = controller;
        this.errorHandler = services.GetRequiredService<IErrorHandlerService>();
        this.cache = services.GetRequiredService<RssFeedCacheService>();
        this.opmlFeedListItemFactory = services.GetRequiredService<OpmlFeedListItemFactory>();
        this.db = services.GetRequiredService<DatabaseContext>();

        this.enterFeedUrlAlert =
            UIAlertController.Create(Common.AddFeedButton, string.Empty, UIAlertControllerStyle.Alert);
        this.enterFeedUrlAlert.AddAction(UIAlertAction.Create(Common.CancelText, UIAlertActionStyle.Cancel, null));
        this.enterFeedUrlAlert.AddTextField((textField) => { });
        var okAction = UIAlertAction.Create(Common.OKText, UIAlertActionStyle.Default, (action) =>
        {
            var enteredString = this.enterFeedUrlAlert.TextFields[0].Text;
            Uri.TryCreate(enteredString, UriKind.Absolute, out Uri? uri);
            if (uri is null)
            {
                return;
            }

            this.cache.RetrieveFeedAsync(uri).FireAndForgetSafeAsync(this.errorHandler);
            this.enterFeedUrlAlert.TextFields[0].Text = string.Empty;
        });
        this.enterFeedUrlAlert.AddAction(okAction);

        this.enterFolderNameAlert =
            UIAlertController.Create(Common.AddFolderLabel, string.Empty, UIAlertControllerStyle.Alert);
        this.enterFolderNameAlert.AddAction(UIAlertAction.Create(Common.CancelText, UIAlertActionStyle.Cancel, null));
        this.enterFolderNameAlert.AddTextField((textField) => { });
        var okAction2 = UIAlertAction.Create(Common.OKText, UIAlertActionStyle.Default, (action) =>
        {
            var enteredString = this.enterFolderNameAlert.TextFields[0].Text;
            if (string.IsNullOrEmpty(enteredString))
            {
                return;
            }

            this.db.AddOrUpdateFeedFolderAsync(new FeedFolder() { Name = enteredString })
                .FireAndForgetSafeAsync(this.errorHandler);
            this.enterFolderNameAlert.TextFields[0].Text = string.Empty;
        });
        this.enterFolderNameAlert.AddAction(okAction2);

        var allowedUTIs = new string[] { UTType.XML };
        this.documentPicker = new UIDocumentPickerViewController(allowedUTIs, UIDocumentPickerMode.Import);
        this.documentPicker.ModalPresentationStyle = UIModalPresentationStyle.FullScreen;
        this.documentPicker.DidPickDocumentAtUrls += (sender, e) => {
            var url = e.Urls[0];
            // Handle the URL of the picked file
            var opml = new Opml(url.Path!);
            Task.Run(async () =>
            {
                var result = await this.opmlFeedListItemFactory.GenerateFeedListItemsFromOpmlAsync(opml);
                if (result > 0)
                {
                    this.cache.RefreshFeedsAsync().FireAndForgetSafeAsync(this.errorHandler);
                }
            }).FireAndForgetSafeAsync(this.errorHandler);
        };
    }

    /// <inheritdoc/>
    public override string[] SelectableItemIdentifiers(NSToolbar toolbar)
    {
        return new string[] { HideRead, };
    }

    /// <inheritdoc/>
    public override string[] AllowedItemIdentifiers(NSToolbar toolbar)
    {
        return new string[0];
    }

    /// <inheritdoc/>
    public override string[] DefaultItemIdentifiers(NSToolbar toolbar)
    {
        // https://github.com/xamarin/xamarin-macios/issues/12871
        // I only figured this out by going into a Catalyst Swift app,
        // and checking the raw value for NSToolbarItem.Identifier.primarySidebarTrackingSeparatorItemIdentifier
        // This value is not bound yet in dotnet maccatalyst.
        return new string[]
        {
            NSToolbar.NSToolbarFlexibleSpaceItemIdentifier,
            Refresh,
            Plus,
            "NSToolbarPrimarySidebarTrackingSeparatorItem",
            NSToolbar.NSToolbarFlexibleSpaceItemIdentifier,
            MarkAllAsRead,
            HideRead,
            "NSToolbarSupplementarySidebarTrackingSeparatorItem",
            MarkAsRead,
            Star,
            NextUnread,
            ReaderView,
            Share,
            OpenInBrowser,
        };
    }

    /// <inheritdoc/>
    public override NSToolbarItem? WillInsertItem(NSToolbar toolbar, string itemIdentifier, bool willBeInserted)
    {
        if (itemIdentifier == Plus)
        {
            NSMenuToolbarItem addButton = new NSMenuToolbarItem(itemIdentifier);
            addButton.Title = Common.AddLabel;
            addButton.UIImage = UIImage.GetSystemImage("plus");
            addButton.ItemMenu = UIMenu.Create(
                new[]
                {
                    UIAction.Create(Common.FeedLabel, UIImage.GetSystemImage("newspaper"), null,
                        action =>
                        {
                            this.rootViewController.PresentViewController(this.enterFeedUrlAlert, true, null);
                        }),
                    UIAction.Create(Common.FolderLabel, UIImage.GetSystemImage("folder.badge.plus"), null,
                        action =>
                        {
                            this.rootViewController.PresentViewController(this.enterFolderNameAlert, true, null);
                        }),
                    UIAction.Create(Common.OPMLFeedLabel, UIImage.GetSystemImage("list.bullet.rectangle"), null,
                        action =>
                        {
                            
                            this.rootViewController.PresentViewController(this.documentPicker, true, null);
                        }),
                });
            return addButton;
        }

        NSToolbarItem toolbarItem = new NSToolbarItem(itemIdentifier);

        if (itemIdentifier == Refresh)
        {
            toolbarItem.Title = Common.RefreshButton;
            toolbarItem.UIImage = UIImage.GetSystemImage("arrow.clockwise");
            toolbarItem.Action = new ObjCRuntime.Selector("refreshClickAction:");
        }
        else if (itemIdentifier == MarkAllAsRead)
        {
            toolbarItem.UIImage = UIImage.GetSystemImage("arrow.up.arrow.down.circle");
        }
        else if (itemIdentifier == HideRead)
        {
            toolbarItem.UIImage = UIImage.GetSystemImage("circle");
        }
        else if (itemIdentifier == MarkAsRead)
        {
            toolbarItem.UIImage = UIImage.GetSystemImage("book.circle");
        }
        else if (itemIdentifier == Star)
        {
            toolbarItem.UIImage = UIImage.GetSystemImage("star");
        }
        else if (itemIdentifier == NextUnread)
        {
            toolbarItem.UIImage = UIImage.GetSystemImage("arrowtriangle.down.circle");
        }
        else if (itemIdentifier == OpenInBrowser)
        {
            toolbarItem.UIImage = UIImage.GetSystemImage("safari");
        }
        else if (itemIdentifier == ReaderView)
        {
            toolbarItem.UIImage = UIImage.GetSystemImage("note.text");
        }
        else if (itemIdentifier == Share)
        {
            toolbarItem.UIImage = UIImage.GetSystemImage("square.and.arrow.up");
        }

        toolbarItem.Enabled = true;
        toolbarItem.Target = this;
        return toolbarItem;
    }

    [Export("refreshClickAction:")]
    public async void RefreshClickAction(NSObject sender)
    {
        await this.cache.RefreshFeedsAsync();
    }
}