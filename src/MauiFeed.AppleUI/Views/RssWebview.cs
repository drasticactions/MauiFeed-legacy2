// <copyright file="RssWebview.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

#if !TVOS
using WebKit;
#endif

namespace MauiFeed.UI.Views;

#if !TVOS
/// <summary>
/// Rss Webview.
/// </summary>
public class RssWebview : WKWebView
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RssWebview"/> class.
    /// </summary>
    /// <param name="frame">CGRect Frame.</param>
    /// <param name="configuration">Webview Configuration.</param>
    public RssWebview(CGRect frame, WKWebViewConfiguration configuration)
        : base(frame, configuration)
    {
    }

    /// <summary>
    /// Set source on webview.
    /// </summary>
    /// <param name="html">Html.</param>
    public void SetSource(string html)
    {
        this.InvokeOnMainThread(() => this.LoadHtmlString(new NSString(html), null));
    }
}
#elif TVOS
public class RssWebview : UIView
{
    public RssWebview(CGRect frame)
        : base(frame)
    {
    }

    public void SetSource(string html)
    {
    }
}
#endif