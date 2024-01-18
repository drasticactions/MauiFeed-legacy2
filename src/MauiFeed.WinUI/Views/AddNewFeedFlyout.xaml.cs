using CommunityToolkit.Mvvm.DependencyInjection;
using Drastic.Services;
using Drastic.Tools;
using MauiFeed.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MauiFeed.Views;

/// <summary>
/// Add New Feed Flyout.
/// </summary>
public sealed partial class AddNewFeedFlyout : UserControl
{
    private FeedService cache;
    private MainWindow sidebar;
    private IErrorHandlerService errorHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddNewFeedFlyout"/> class.
    /// </summary>
    /// <param name="sidebar">Sidebar.</param>
    public AddNewFeedFlyout(MainWindow sidebar)
    {
        this.InitializeComponent();
        this.cache = (FeedService)Ioc.Default.GetService(typeof(FeedService))!;
        this.errorHandler = (IErrorHandlerService)Ioc.Default.GetService(typeof(IErrorHandlerService))!;
        this.sidebar = sidebar;
        this.AddNewFeedCommand = new AsyncCommand<string>(this.AddNewFeed, (x) => true, this.errorHandler);
    }

    /// <summary>
    /// Gets the Add New Feed Command.
    /// </summary>
    public AsyncCommand<string> AddNewFeedCommand { get; private set; }

    private async Task AddNewFeed(string feedUri)
    {
        Uri.TryCreate(feedUri, UriKind.Absolute, out Uri? uri);
        if (uri is null)
        {
            return;
        }

        var popup = ((Popup)((FrameworkElement)this.Parent).Parent)!;
        popup.IsOpen = false;

        this.cache.ReadFeedAsync(uri).FireAndForgetSafeAsync();

        this.FeedUrlField.Text = string.Empty;
    }

    private void FeedUrlField_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            if (string.IsNullOrEmpty(this.FeedUrlField.Text))
            {
                return;
            }

            this.AddNewFeedCommand.ExecuteAsync(this.FeedUrlField.Text).FireAndForgetSafeAsync();
        }
    }
}
