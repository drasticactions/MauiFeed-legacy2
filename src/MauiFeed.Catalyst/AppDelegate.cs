// <copyright file="AppDelegate.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using Drastic.Services;
using Foundation;
using MauiFeed.MauiUI;
using MauiFeed.Models;
using MauiFeed.Services;
using MauiFeed.UI.Services;
using MauiFeed.UI.Views;
using Microsoft.Maui.Embedding;
using UIKit;

namespace MauiFeed.Catalyst;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    private MauiContext mauiContext;

    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MauiFeed");
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        var databasePath = Path.Combine(appDataPath, "MauiFeed.db");
        var database = new DatabaseContext(databasePath);

        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder.UseMauiEmbedding<Microsoft.Maui.Controls.Application>();

        builder.Services
            .AddSingleton<IErrorHandlerService, DefaultErrorHandlerService>()
            .AddSingleton<IAppDispatcher, DefaultAppDispatcher>()
            .AddSingleton(database)
            .AddSingleton<ITemplateService, HandlebarsTemplateService>()
            .AddSingleton<FeedService>()
            .AddSingleton<RssFeedCacheService>()
            .AddSingleton(new Progress<RssCacheFeedUpdate>())
            .AddTransient<IDebugPage, DebugPage>()
            .AddSingleton<OpmlFeedListItemFactory>();
        MauiApp mauiApp = builder.Build();
        this.mauiContext = new MauiContext(mauiApp.Services);

        this.Window = new MainWindow(this.mauiContext, UIScreen.MainScreen.Bounds);

        this.Window.MakeKeyAndVisible();

        return true;
    }
}