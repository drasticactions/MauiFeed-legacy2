// <copyright file="AppDelegate.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using Drastic.Services;
using MauiFeed.Models;
using MauiFeed.Services;
using MauiFeed.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Realms;

namespace MauiFeed;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public IServiceProvider? provider;

    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var config = new InMemoryConfiguration("AppDelegateTest");
        var services = new ServiceCollection();
        services
            .AddSingleton<IErrorHandlerService, DefaultErrorHandlerService>()
            .AddSingleton<IAppDispatcher, DefaultAppDispatcher>()
            .AddSingleton<RealmConfigurationBase>(config)
            .AddSingleton<ITemplateService, HandlebarsTemplateService>()
            .AddSingleton<FeedService>()
            .AddSingleton<FeedFolderService>()
            .AddSingleton(new Progress<RssCacheFeedUpdate>())
            .AddSingleton<OpmlFeedListItemFactory>();
        this.provider = services.BuildServiceProvider();

        // create a new window instance based on the screen size
        this.Window = new MainWindow(this.provider, UIScreen.MainScreen.Bounds);

        // make the window visible
        this.Window.MakeKeyAndVisible();

        return true;
    }
}
