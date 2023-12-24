using MauiFeed.Translations;
using MauiFeed.UI.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MauiFeed.UI.Views;

/// <summary>
/// Main UI View Controller.
/// </summary>
public sealed class MainUIViewController : UISplitViewController
{
    private readonly IServiceProvider provider;
    private readonly SidebarViewController sidebarViewController;
    private readonly TimelineTableViewController timelineTableViewController;
    private readonly UIViewController debug2ViewController;
    private List<SidebarItem> menuButtons;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainUIViewController"/> class.
    /// </summary>
    /// <param name="context">MAUI Context.</param>
    public MainUIViewController(IServiceProvider provider)
        : base(UISplitViewControllerStyle.TripleColumn)
    {
        this.provider = provider;

        this.menuButtons = this.GenerateMenuButtons();
        this.timelineTableViewController = new TimelineTableViewController(this, provider);
        this.sidebarViewController = new SidebarViewController(provider, this.OnSidebarItemSelected);
        this.debug2ViewController = new BasicViewController();
        this.SetViewController(this.sidebarViewController, UISplitViewControllerColumn.Primary);
        this.SetViewController(this.debug2ViewController, UISplitViewControllerColumn.Secondary);
        this.SetViewController(this.timelineTableViewController, UISplitViewControllerColumn.Supplementary);
        this.PreferredDisplayMode = UISplitViewControllerDisplayMode.TwoBesideSecondary;
        this.PreferredPrimaryColumnWidth = 245f;
#if !TVOS
        this.PrimaryBackgroundStyle = UISplitViewControllerBackgroundStyle.Sidebar;
#endif
    }

    public override void ViewDidAppear(bool animated)
    {
        base.ViewDidAppear(animated);
        this.ShowColumn(UISplitViewControllerColumn.Primary);
    }

    public SidebarViewController SidebarViewController => this.sidebarViewController;

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
        this.timelineTableViewController.SidebarItem = item;
        this.ShowColumn(UISplitViewControllerColumn.Supplementary);
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