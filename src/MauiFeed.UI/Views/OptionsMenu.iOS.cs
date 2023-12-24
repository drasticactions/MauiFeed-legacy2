using Drastic.Services;
using Drastic.Tools;
using MauiFeed.Models;
using MauiFeed.Models.OPML;
using MauiFeed.Services;
using MauiFeed.Translations;
using Microsoft.Extensions.DependencyInjection;
using MobileCoreServices;

namespace MauiFeed.UI.Views;

public class OptionsMenu
{
    private readonly UIViewController rootViewController;
    private RssFeedCacheService cache;
    private DatabaseContext db;

    private UIAlertController enterFeedUrlAlert;
    private UIAlertController enterFolderNameAlert;
    
    private IErrorHandlerService errorHandler;
    private OpmlFeedListItemFactory opmlFeedListItemFactory;
    
    #if IOS || MACCATALYST
    private UIDocumentPickerViewController documentPicker;
    #endif
    
    public OptionsMenu(UIViewController rootViewController, IServiceProvider services)
    {
        this.rootViewController = rootViewController;
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
        
        #if IOS || MACCATALYST
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
        #endif
        
        this.AddMenu = UIMenu.Create(Common.AddLabel, UIImage.GetSystemImage("plus.circle"), UIMenuIdentifier.Font, UIMenuOptions.DisplayInline, new UIMenuElement[]
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
#if IOS || MACCATALYST
            UIAction.Create(Common.OPMLFeedLabel, UIImage.GetSystemImage("list.bullet.rectangle"), null,
                action =>
                {
                            
                    this.rootViewController.PresentViewController(this.documentPicker, true, null);
                }),
#endif
        });
    }
    
    public UIMenu AddMenu { get; }
}