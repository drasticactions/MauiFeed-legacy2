using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MauiFeed.UI.Views;
using Microsoft.Maui.Platform;
using UIKit;

namespace MauiFeed.MauiUI;

public partial class DebugPage : ContentPage, IDebugPage
{
    public DebugPage(IServiceProvider provider)
    {
        InitializeComponent();
    }

#if IOS || MACCATALYST
    public UIViewController GenerateNativeView(MauiContext context)
    {
        return this.ToUIViewController(context);
    }
#endif
}