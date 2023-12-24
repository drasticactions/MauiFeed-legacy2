using Drastic.Services;
using MauiFeed.Models;
using MauiFeed.Services;
using MauiFeed.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MauiFeed.iOS;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public IServiceProvider? provider;
    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MauiFeed");
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        var databasePath = Path.Combine(appDataPath, "MauiFeedDB.db");
        var database = new DatabaseContext(databasePath);

        var services = new ServiceCollection();
        services
            .AddSingleton<IErrorHandlerService, DefaultErrorHandlerService>()
            .AddSingleton<IAppDispatcher, DefaultAppDispatcher>()
            .AddSingleton(database)
            .AddSingleton<ITemplateService, HandlebarsTemplateService>()
            .AddSingleton<FeedService>()
            .AddSingleton<RssFeedCacheService>()
            .AddSingleton(new Progress<RssCacheFeedUpdate>())
            .AddSingleton<OpmlFeedListItemFactory>();
        this.provider = services.BuildServiceProvider();

        this.Window = new MainWindow(this.provider, UIScreen.MainScreen.Bounds);

        this.Window.MakeKeyAndVisible();
        return true;
    }
}