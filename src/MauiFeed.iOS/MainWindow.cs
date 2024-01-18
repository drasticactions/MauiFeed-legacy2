// <copyright file="MainWindow.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using MauiFeed.UI.Views;

namespace MauiFeed;

public sealed class MainWindow : UIWindow
{
    public MainWindow(IServiceProvider provider, CGRect frame)
        : base(frame)
    {
        this.RootViewController = new MainUIViewController(provider);
        var windowScene = this.WindowScene;
    }
}
