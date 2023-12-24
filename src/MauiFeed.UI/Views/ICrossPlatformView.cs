using Microsoft.Maui;

namespace MauiFeed.UI.Views;

public interface ICrossPlatformView<T>
{
    T GenerateNativeView(MauiContext context);
}