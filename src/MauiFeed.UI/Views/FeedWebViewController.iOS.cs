using Drastic.PureLayout;
using Drastic.Services;
using Drastic.Tools;
using MauiFeed.Models;
using MauiFeed.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MauiFeed.UI.Views;

/// <summary>
/// Feed Web View Controller.
/// </summary>
public class FeedWebViewController : UIViewController
{
    private ITemplateService templateService;
    private IErrorHandlerService errorHandler;
    private RssWebview webview;

    private MainUIViewController rootSplitViewController;

    private FeedItem? item;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="FeedWebViewController"/> class.
    /// </summary>
    /// <param name="controller">Root Split View Controller.</param>
    public FeedWebViewController(MainUIViewController controller, IServiceProvider provider)
    {
        this.rootSplitViewController = controller;
        this.templateService = provider.GetRequiredService<ITemplateService>()!;
        this.errorHandler = provider.GetRequiredService<IErrorHandlerService>()!;
#if !TVOS
        this.webview = new RssWebview(this.View?.Frame ?? CGRect.Empty, new WebKit.WKWebViewConfiguration());
#else
        this.webview = new RssWebview(this.View?.Frame ?? CGRect.Empty);
#endif
        this.View?.AddSubview(this.webview);
        this.webview.AutoPinEdgesToSuperviewSafeArea();
        this.SetFeedItem(null);
    }

    /// <summary>
    /// Set the feed item on the webview.
    /// </summary>
    /// <param name="item">Item.</param>
    public void SetFeedItem(FeedItem? item)
    {
        this.InvokeOnMainThread(() =>
        {
            this.item = item;
            var isDarkMode = this.TraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark;
            Task.Run(async () =>
            {
                var resultString = string.Empty;
                if (item is null)
                {
                    resultString = await this.templateService.RenderBlankAsync(isDarkMode);
                }
                else
                {
                    resultString = await this.templateService.RenderFeedItemAsync(item, isDarkMode);
                }

                this.webview.SetSource(resultString);
            }).FireAndForgetSafeAsync(this.errorHandler);
        });
    }

    public override void TraitCollectionDidChange(UITraitCollection? previousTraitCollection)
    {
        base.TraitCollectionDidChange(previousTraitCollection);

        if (this.TraitCollection.UserInterfaceStyle != previousTraitCollection?.UserInterfaceStyle)
        {
            this.SetFeedItem(this.item);
        }
    }
}