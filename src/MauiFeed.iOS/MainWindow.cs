using MauiFeed.UI.Views;

namespace MauiFeed.iOS;

public sealed class MainWindow : UIWindow
{
    public MainWindow(IServiceProvider provider, CGRect frame)
        : base(frame)
    {
        this.RootViewController = new MainUIViewController(provider);
        var windowScene = this.WindowScene;
    }
}