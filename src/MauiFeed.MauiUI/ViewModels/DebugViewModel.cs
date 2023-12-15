using Drastic.Tools;
using Drastic.ViewModels;
using MauiFeed.Models;
using MauiFeed.Services;
using Microsoft.Maui.Adapters;

namespace MauiFeed.MauiUI.ViewModels;

public class DebugViewModel : BaseViewModel
{
    private List<FeedItem> feedItems = new List<FeedItem>();
    private AsyncCommand debugCommand;
    private RssFeedCacheService rssFeedCacheService;
    
    public DebugViewModel(IServiceProvider services)
        : base(services)
    {
        this.FeedItems = new VirtualListViewAdapter<FeedItem>(this.feedItems);
        this.rssFeedCacheService = services.GetRequiredService<RssFeedCacheService>();
    }

    public VirtualListViewAdapter<FeedItem> FeedItems { get; }
 
    public AsyncCommand DebugCommand => this.debugCommand ??= new AsyncCommand(this.DebugAsync, null, this.Dispatcher, this.ErrorHandler);

    public async Task DebugAsync()
    {
        var feed = await this.rssFeedCacheService.RetrieveFeedAsync("https://ascii.jp/rss.xml");
        this.feedItems.Clear();
        this.feedItems.AddRange(feed.Items);
        this.FeedItems.InvalidateData();
    }
}