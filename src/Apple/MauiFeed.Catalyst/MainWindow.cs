using AppKit;
using MauiFeed.UI.Views;

namespace MauiFeed.Catalyst;

public sealed class MainWindow : UIWindow
{
    public MainWindow(IServiceProvider provider, CGRect frame)
        : base(frame)
    {
        this.RootViewController = new MainUIViewController(provider);
        var windowScene = this.WindowScene;

        if (windowScene is not null)
        {
#pragma warning disable CA1416 // プラットフォームの互換性を検証
            windowScene.Titlebar!.TitleVisibility = UITitlebarTitleVisibility.Visible;

            var toolbar = new NSToolbar();
            var controller = (MainUIViewController)this.RootViewController;
            toolbar.Delegate = new MainToolbarDelegate(this.RootViewController, provider);
            toolbar.DisplayMode = NSToolbarDisplayMode.Icon;

            windowScene.Title = MauiFeed.Translations.Common.AppTitle;
            windowScene.Titlebar.Toolbar = toolbar;
            windowScene.Titlebar.ToolbarStyle = UITitlebarToolbarStyle.Automatic;
            windowScene.Titlebar.Toolbar.Visible = true;
#pragma warning restore CA1416 // プラットフォームの互換性を検証
        }
    }
}