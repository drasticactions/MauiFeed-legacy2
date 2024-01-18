// <copyright file="MainWindow.xaml.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Drastic.Services;
using Drastic.Tools;
using MauiFeed.Models;
using MauiFeed.Pages;
using MauiFeed.Services;
using MauiFeed.Translations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;

namespace MauiFeed
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx, IErrorHandlerService
    {
        private SettingsPage settingsPage;
        private ApplicationSettingsService appSettings;
        private IAppDispatcher dispatcher;
        private IErrorHandlerService errorHandlerService;

        public MainWindow()
        {
            this.InitializeComponent();
            this.MainWindowGrid.DataContext = this;

            // Setup
            this.appSettings = Ioc.Default.GetRequiredService<ApplicationSettingsService>()!;
            this.dispatcher = Ioc.Default.GetRequiredService<IAppDispatcher>()!;
            this.errorHandlerService = Ioc.Default.GetRequiredService<IErrorHandlerService>()!;
            this.Activated += this.MainWindowActivated;

            // TitleBar
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(this.AppTitleBar);
            this.AppWindow.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            this.AppWindow.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);

            // Window
            this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();

            this.settingsPage = new SettingsPage(this);

            this.NavView.Loaded += this.NavigationFrameLoaded;

            ((FrameworkElement)this.Content).Loaded += this.MainWindowLoaded;
        }

        /// <summary>
        /// Gets the app logo path.
        /// </summary>
        public string AppLogo => "Icon.logo_header.png";

        /// <inheritdoc/>
        public void HandleError(Exception ex)
        {
            this.FeedRefreshView.IsRefreshing = false;
            this.errorHandlerService.HandleError(ex);
        }

        private void FeedSearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.QueryText))
            {
                return;
            }

            //this.feedSplitPage.SelectedSidebarItem = new FeedSidebarItem(Common.SearchLabel, new SymbolIcon(Symbol.Find), this.context.FeedItems!.Where(n => (n.Title ?? string.Empty).Contains(args.QueryText)));
            //System.Diagnostics.Debug.Assert(this.feedSplitPage.SelectedSidebarItem is not null, "Why is this null?");
            //this.NavigationFrame.Content = this.feedSplitPage;
        }

        private void NavViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem nav)
            {
                var value = nav.Tag?.ToString() ?? string.Empty;
                switch (value)
                {
                    case "Settings":
                        this.NavigationFrame.Content = this.settingsPage;
                        break;
                    default:
                        //this.feedSplitPage.SelectedSidebarItem = this.SidebarItems.FirstOrDefault(n => n.Id.ToString() == value);
                        //System.Diagnostics.Debug.Assert(this.feedSplitPage.SelectedSidebarItem is not null, "Why is this null?");
                        //this.NavigationFrame.Content = this.feedSplitPage;
                        break;
                }

                return;
            }
        }

        private void MainWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            this.Activated -= this.MainWindowActivated;
            this.dispatcher.Dispatch(() => this.appSettings.UpdateTheme());
        }

        private async Task LastUpdateCheckAsync()
        {
            var lastUpdated = this.appSettings.LastUpdated;
            if (lastUpdated == null)
            {
                this.appSettings.LastUpdated = DateTime.UtcNow;
                return;
            }

            var totalHours = (DateTime.UtcNow - lastUpdated.Value).TotalHours;
            if (totalHours > 1)
            {
                // await this.RefreshAllFeedsAsync();
            }
        }

        private void NavigationFrameLoaded(object sender, RoutedEventArgs e)
        {
            this.NavigationFrame.Loaded -= this.NavigationFrameLoaded;
            var settings = (NavigationViewItem)this.NavView.SettingsItem;
            settings.Content = Translations.Common.SettingsLabel;
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).Loaded -= this.MainWindowLoaded;
            this.LastUpdateCheckAsync().FireAndForgetSafeAsync(this);
        }

        private class FolderMenuFlyoutItem : MenuFlyoutItem
        {
            public FolderMenuFlyoutItem(FeedSidebarItem folder, FeedSidebarItem feedItem)
            {
                this.Folder = folder;
                this.Text = folder.FeedFolder!.Name;
                this.FeedItem = feedItem;
            }

            /// <summary>
            /// Gets the Folder.
            /// </summary>
            public FeedSidebarItem Folder { get; }

            /// <summary>
            /// Gets the feed item.
            /// </summary>
            public FeedSidebarItem FeedItem { get; }
        }
    }
}
