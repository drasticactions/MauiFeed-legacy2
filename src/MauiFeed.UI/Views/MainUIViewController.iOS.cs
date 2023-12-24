using MauiFeed.Translations;
using MauiFeed.UI.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;

namespace MauiFeed.UI.Views;

/// <summary>
/// Main UI View Controller.
/// </summary>
public sealed class MainUIViewController : UISplitViewController
{
    private readonly MauiContext? context;
    private readonly IServiceProvider provider;
    private readonly SidebarViewController sidebarViewController;
    private readonly UIViewController debugViewController;
    private readonly UIViewController debug2ViewController;
    private List<SidebarItem> menuButtons;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainUIViewController"/> class.
    /// </summary>
    /// <param name="context">MAUI Context.</param>
    public MainUIViewController(IServiceProvider provider, MauiContext? context)
        : base(UISplitViewControllerStyle.TripleColumn)
    {
        this.provider = provider;
        this.context = context;

        this.menuButtons = this.GenerateMenuButtons();

        this.sidebarViewController = new SidebarViewController(provider, this.OnSidebarItemSelected);
        this.debugViewController = context.Services.GetRequiredService<IDebugPage>().GenerateNativeView(this.context);
        this.debug2ViewController = new BasicViewController();
        this.SetViewController(this.sidebarViewController, UISplitViewControllerColumn.Primary);
        this.SetViewController(this.debugViewController, UISplitViewControllerColumn.Secondary);
        this.SetViewController(this.debug2ViewController, UISplitViewControllerColumn.Supplementary);
        this.PreferredDisplayMode = UISplitViewControllerDisplayMode.TwoBesideSecondary;
        this.PreferredPrimaryColumnWidth = 245f;
#if !TVOS
        this.PrimaryBackgroundStyle = UISplitViewControllerBackgroundStyle.Sidebar;
#endif
    }
    
    public SidebarViewController SidebarViewController => this.sidebarViewController;

    public MauiContext Context => this.context;

    private List<SidebarItem> GenerateMenuButtons()
    {
        return
        [
            new SidebarItem(
                Common.RefreshButton,
                UIImage.GetSystemImage("arrow.clockwise.circle")!,
                this.OnRefreshSelected),
            new SidebarItem(Common.AddLabel, UIImage.GetSystemImage("plus.circle")!, this.OnAddSelected)
        ];
    }

    private void OnSidebarItemSelected(SidebarItem? item)
    {
    }

    private async void OnRefreshSelected()
    {
    }

    private async void OnAddSelected()
    {
    }
}

public class BasicViewController : UIViewController
{
    public BasicViewController()
    {
        this.View!.AddSubview(new UILabel(View!.Frame)
        {
#if !TVOS
            BackgroundColor = UIColor.SystemBackground,
#endif
            TextAlignment = UITextAlignment.Center,
            Text = "Hello, Apple!",
            AutoresizingMask = UIViewAutoresizing.All,
        });
    }
}