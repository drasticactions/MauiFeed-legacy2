// <copyright file="MainWindow.xaml.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using CommunityToolkit.Mvvm.DependencyInjection;
using Drastic.Services;
using Drastic.Tools;
using MauiFeed.Events;
using MauiFeed.Models;
using MauiFeed.Pages;
using MauiFeed.Services;
using MauiFeed.Tools;
using MauiFeed.Translations;
using MauiFeed.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Realms;
using Windows.Storage.Pickers;
using WinUIEx;

namespace MauiFeed
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx, IErrorHandlerService
    {
        private bool isRefreshing;
        private SettingsPage settingsPage;
        private FeedTimelineSplitPage feedSplitPage;
        private ApplicationSettingsService appSettings;
        private IAppDispatcher dispatcher;
        private IErrorHandlerService errorHandlerService;
        private Realm context;
        private OpmlFeedListItemFactory opmlFactory;
        private FeedService rssFeedCacheService;
        private NavigationViewItemSeparator folderSeparator;
        private NavigationViewItemSeparator filterSeparator;
        private NavigationViewItem? addFolderButton;
        private Flyout? folderFlyout;
        private Progress<RssCacheFeedUpdate> refreshProgress;
        private IDisposable? feedFolderSubscription;
        private IDisposable? feedListItemSubscription;

        public MainWindow()
        {
            this.InitializeComponent();
            this.MainWindowGrid.DataContext = this;

            // Setup
            this.appSettings = Ioc.Default.GetRequiredService<ApplicationSettingsService>()!;
            this.dispatcher = Ioc.Default.GetRequiredService<IAppDispatcher>()!;
            this.errorHandlerService = Ioc.Default.GetRequiredService<IErrorHandlerService>()!;
            this.context = Realm.GetInstance(Ioc.Default.GetRequiredService<RealmConfigurationBase>()!);
            this.opmlFactory = Ioc.Default.GetService<OpmlFeedListItemFactory>()!;
            this.Activated += this.MainWindowActivated;

            this.feedFolderSubscription = this.context.All<FeedFolder>().SubscribeForNotifications(this.OnFeedFolderUpdate);
            this.feedListItemSubscription = this.context.All<FeedListItem>().SubscribeForNotifications(this.OnFeedListItemUpdate);

            // TitleBar
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(this.AppTitleBar);
            this.AppWindow.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            this.AppWindow.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);

            // Window
            this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();

            this.settingsPage = new SettingsPage(this);
            this.feedSplitPage = new FeedTimelineSplitPage(this);
            this.NavigationFrame.Content = this.feedSplitPage;

            this.folderSeparator = new NavigationViewItemSeparator();
            this.filterSeparator = new NavigationViewItemSeparator();
            // this.RemoveFeedCommand = new AsyncCommand<FeedSidebarItem>(this.RemoveFeed, null, this);
            //this.RemoveFromFolderCommand = new AsyncCommand<FeedSidebarItem>((x) => this.RemoveFromFolderAsync(x, true), null, this);

            this.GenerateSidebarItems();
            this.NavView.Loaded += this.NavigationFrameLoaded;

            ((FrameworkElement)this.Content).Loaded += this.MainWindowLoaded;
        }

        /// <summary>
        /// Gets the app logo path.
        /// </summary>
        public string AppLogo => "Icon.logo_header.png";

        /// <summary>
        /// Gets the list of sidebar items.
        /// </summary>
        public List<FeedSidebarItem> SidebarItems { get; } = new List<FeedSidebarItem>();

        /// <summary>
        /// Gets the remove feed command.
        /// </summary>
        public AsyncCommand<FeedSidebarItem> RemoveFeedCommand { get; }

        /// <summary>
        /// Gets the remove from folder feed command.
        /// </summary>
        public AsyncCommand<FeedSidebarItem> RemoveFromFolderCommand { get; }

        /// <summary>
        /// Gets the list of navigation items.
        /// </summary>
        public ObservableRangeCollection<NavigationViewItemBase> Items { get; } = new ObservableRangeCollection<NavigationViewItemBase>();

        /// <inheritdoc/>
        public void HandleError(Exception ex)
        {
            this.FeedRefreshView.IsRefreshing = false;
            this.errorHandlerService.HandleError(ex);
        }

        private void GenerateSidebarItems()
        {
            this.Items.Clear();
            this.SidebarItems.Clear();
            this.GenerateMenuButtons();
            this.GenerateSmartFilters();
            this.GenerateFolderItems();
            this.GenerateNavigationItems();
        }

        private void GenerateMenuButtons()
        {
            var refreshButton = new NavigationViewItem() { Content = Translations.Common.RefreshButton, Icon = new SymbolIcon(Symbol.Refresh) };
            refreshButton.SelectsOnInvoked = false;
            refreshButton.Tapped += (sender, args) =>
            {
                 // this.RefreshAllFeedsAsync().FireAndForgetSafeAsync(this);
            };

            this.Items.Add(refreshButton);

            var addButton = new NavigationViewItem() { Content = Translations.Common.AddLabel, Icon = new SymbolIcon(Symbol.Add) };
            addButton.SelectsOnInvoked = false;

            var addFeedButton = new NavigationViewItem() { Content = Translations.Common.FeedLabel, Icon = new SymbolIcon(Symbol.Library) };
            addFeedButton.SelectsOnInvoked = false;
            addFeedButton.ContextFlyout = new Flyout() { Content = new AddNewFeedFlyout(this) };
            addFeedButton.Tapped += this.MenuButtonTapped;

            addButton.MenuItems.Add(addFeedButton);

            var addOpmlButton = new NavigationViewItem() { Content = Translations.Common.OPMLFeedLabel, Icon = new SymbolIcon(Symbol.Globe) };
            addOpmlButton.SelectsOnInvoked = false;
            addOpmlButton.Tapped += (sender, args) => this.OpenImportOpmlFeedPickerAsync().FireAndForgetSafeAsync(this);
            addButton.MenuItems.Add(addOpmlButton);

            this.addFolderButton = new NavigationViewItem() { Content = Translations.Common.FolderLabel, Icon = new SymbolIcon(Symbol.Folder) };
            this.addFolderButton.SelectsOnInvoked = false;
            this.addFolderButton.Tapped += this.AddFolderButtonTapped;
            addButton.MenuItems.Add(this.addFolderButton);

            this.Items.Add(addButton);
            this.Items.Add(new NavigationViewItemSeparator());
        }

        private void GenerateSmartFilters()
        {
            var smartFilters = new NavigationViewItem() { Content = Translations.Common.SmartFeedsLabel, Icon = new SymbolIcon(Symbol.Filter) };
            smartFilters.SelectsOnInvoked = false;

            var allButtonItem = new FeedSidebarItem(Translations.Common.AllLabel, new SymbolIcon(Symbol.Bookmarks), this.context.All<FeedItem>());
            smartFilters.MenuItems.Add(allButtonItem.NavItem);
            this.SidebarItems.Add(allButtonItem);

            var today = new FeedSidebarItem(Translations.Common.TodayLabel, new SymbolIcon(Symbol.GoToToday), this.context.All<FeedItem>().Where(n =>
                    n.PublishingDate == DateTimeOffset.UtcNow));
            smartFilters.MenuItems.Add(today.NavItem);
            this.SidebarItems.Add(today);

            var unread = new FeedSidebarItem(Translations.Common.AllUnreadLabel, new SymbolIcon(Symbol.Filter), this.context.All<FeedItem>().Where(n => !n.IsRead));
            smartFilters.MenuItems.Add(unread.NavItem);
            this.SidebarItems.Add(unread);

            var star = new FeedSidebarItem(Translations.Common.StarredLabel, new SymbolIcon(Symbol.Favorite), this.context.All<FeedItem>().Where(n => n.IsFavorite));
            smartFilters.MenuItems.Add(star.NavItem);
            this.SidebarItems.Add(star);

            this.Items.Add(smartFilters);
            this.Items.Add(this.filterSeparator);
        }

        private void GenerateFolderItems()
        {
            foreach (var item in this.context.All<FeedFolder>())
            {
                var (folder, feedSidebarItems) = this.GenerateFeedFolderSidebarItem(item);
                this.SidebarItems.Add(folder);
                this.SidebarItems.AddRange(feedSidebarItems);
                this.Items.Add(folder.NavItem);
            }

            // If we have folders, add the separator.
            if (this.SidebarItems.Any(n => n.SidebarItemType == SidebarItemType.Folder))
            {
                this.Items.Add(this.folderSeparator);
            }
        }

        private void GenerateNavigationItems()
        {
            foreach (var item in this.context.All<FeedListItem>().Where(n => n.Folder == null))
            {
                var sidebarItem = new FeedSidebarItem(item!, item.Items);
                sidebarItem.RightTapped += this.NavItemRightTapped;
                this.Items.Add(sidebarItem.NavItem);
                this.SidebarItems.Add(sidebarItem);
            }
        }


        private void AddFolderButtonTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            ((NavigationViewItem)sender).ContextFlyout = new Flyout() { Content = new FolderOptionsFlyout(this, new FeedSidebarItem(new FeedFolder())) };
            ((FrameworkElement)sender)!.ContextFlyout.ShowAt((FrameworkElement)sender!);
        }

        private void MenuButtonTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            ((FrameworkElement)sender)!.ContextFlyout.ShowAt((FrameworkElement)sender!);
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

        private void NavItemRightTapped(object? sender, NavItemRightTappedEventArgs e)
        {
        }

        private void SidebarItemOnFolderDropped(object? sender, FeedFolderDropEventArgs e)
        {
        }

        private (FeedSidebarItem Folder, List<FeedSidebarItem> FeedItems) GenerateFeedFolderSidebarItem(FeedFolder item)
        {
            var feedSidebarItems = new List<FeedSidebarItem>();
            var folder = new FeedSidebarItem(item, this.context.All<FeedItem>().Filter("Feed.Folder.Id == $0", item.Id));
            folder.RightTapped += this.NavItemRightTapped;
            folder.OnFolderDropped += this.SidebarItemOnFolderDropped;
            foreach (var feed in item.Items!)
            {
                var sidebarItem = new FeedSidebarItem(feed, feed.Items);
                sidebarItem.RightTapped += this.NavItemRightTapped;
                sidebarItem.NavItem.SetValue(Canvas.ZIndexProperty, 99);
                folder.NavItem.MenuItems.Add(sidebarItem.NavItem);
                feedSidebarItems.Add(sidebarItem);
            }

            return (folder, feedSidebarItems);
        }

        private async Task OpenImportOpmlFeedPickerAsync()
        {
            var filePicker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);
            filePicker.FileTypeFilter.Add(".opml");
            var file = await filePicker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }

            var text = await Windows.Storage.FileIO.ReadTextAsync(file);
            var xml = new XmlDocument();
            xml.LoadXml(text);
            await this.opmlFactory.GenerateFeedListItemsFromOpmlAsync(new Models.OPML.Opml(xml));
            this.GenerateSidebarItems();
            //if (result > 0)
            //{
            //    this.GenerateSidebarItems();
            //    this.RefreshAllFeedsAsync().FireAndForgetSafeAsync(this);
            //}
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

        private void OnFeedListItemUpdate(IRealmCollection<FeedListItem> sender, ChangeSet? changes)
        {
            if (changes is null)
            {
                return;
            }

            System.Diagnostics.Debug.WriteLine(changes);
            //this.GenerateUnorganizedItems(sender, true);
            //this.filterList.ForEach(n => n.Update());
        }

        private void OnFeedFolderUpdate(IRealmCollection<FeedFolder> sender, ChangeSet? changes)
        {
            if (changes is null)
            {
                return;
            }
            //this.GenerateFeedFolders(sender, true);
            //this.filterList.ForEach(n => n.Update());
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
