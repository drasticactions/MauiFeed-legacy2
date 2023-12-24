namespace MauiFeed.UI.Views;

public interface IDebugPage : 
#if IOS || MACCATALYST || TVOS
    ICrossPlatformView<UIViewController>
#endif
{
}