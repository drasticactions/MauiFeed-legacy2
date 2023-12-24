// <copyright file="AppDelegate.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using Drastic.Services;
using Foundation;
using MauiFeed.Models;
using MauiFeed.Services;
using MauiFeed.UI.Services;
using MauiFeed.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using UIKit;

namespace MauiFeed.Catalyst;

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