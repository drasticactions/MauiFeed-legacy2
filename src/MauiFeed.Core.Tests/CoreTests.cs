// <copyright file="CoreTests.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using MauiFeed.Models;
using MauiFeed.Models.OPML;
using MauiFeed.Services;
using MongoDB.Bson;
using Realms;

namespace MauiFeed.Core.Tests;

public class CoreTests
{
    [Fact]
    public async void JsonFeedTest()
    {
        Realm? realm = Realm.GetInstance(new InMemoryConfiguration("JsonFeedTest"));
        var feedService = new FeedService(new TestErrorHandlerService(true), new InMemoryConfiguration("JsonFeedTest"));
        await feedService.ReadFeedAsync("https://daringfireball.net/feeds/json");
        var feedList = realm.All<FeedListItem>().FirstOrDefault(n => n.Uri == "https://daringfireball.net/feeds/json");
        Assert.NotNull(feedList);
        Assert.Equal("Daring Fireball", feedList.Name);
        Assert.Equal("https://daringfireball.net/feeds/json", feedList.Uri);
        Assert.True(feedList.Items.Count > 0);
    }

    [Fact]
    public async void OpmlFeedListItemFactoryTest()
    {
        var opmlFactory = new OpmlFeedListItemFactory(new InMemoryConfiguration("OpmlFeedListItemFactoryTest"));
        var opml = new Opml("Subscriptions-OnMyiPhone.opml");
        await opmlFactory.GenerateFeedListItemsFromOpmlAsync(opml);
    }

    [Fact]
    public async void AddAndRemoveFeedFolderTest()
    {
        Realm? realm = Realm.GetInstance(new InMemoryConfiguration("AddAndRemoveFolderTest"));
        var feedFolderService = new FeedFolderService(new InMemoryConfiguration("AddAndRemoveFolderTest"));
        var rssService = new FeedService(new TestErrorHandlerService(), new InMemoryConfiguration("AddAndRemoveFolderTest"));
        await this.AddTempItems(rssService);

        var folder = new FeedFolder() { Name = "Test" };
        await feedFolderService.AddFeedFolderAsync(folder);
        Assert.NotNull(folder);
        Assert.Equal("Test", folder.Name);
        Assert.True(folder.Id != ObjectId.Empty);

        var folder2 = new FeedFolder() { Name = "Test2" };
        await feedFolderService.AddFeedFolderAsync(folder2);
        Assert.NotNull(folder2);
        Assert.Equal("Test2", folder2.Name);
        Assert.True(folder2.Id != ObjectId.Empty);

        var feedList = realm.All<FeedListItem>().FirstOrDefault(n => n.Uri == "https://daringfireball.net/feeds/json");
        Assert.NotNull(feedList);
        Assert.Equal("Daring Fireball", feedList.Name);

        await feedFolderService.AddFeedToFolderAsync(folder, feedList);
        var feedList2 = realm.All<FeedListItem>().FirstOrDefault(n => n.Uri == "https://feeds.macrumors.com/MacRumors-All");
        Assert.NotNull(feedList2);
        Assert.Equal("MacRumors: Mac News and Rumors - All Stories", feedList2.Name);
        await feedFolderService.AddFeedToFolderAsync(folder, feedList2);
        Assert.True(folder.Items.Count == 2);
        await feedFolderService.RemoveFeedFromFolderAsync(folder, feedList);
        Assert.True(folder.Items.Count == 1);

        await feedFolderService.AddFeedToFolderAsync(folder, feedList);
        Assert.True(folder.Items.Count == 2);
        Assert.True(folder2.Items.Count == 0);
        await feedFolderService.AddFeedToFolderAsync(folder2, feedList);
        Assert.True(folder2.Items.Count == 1);
        Assert.True(folder.Items.Count == 1);

        await feedFolderService.RemoveFeedFolderAsync(folder2);
        Assert.True(folder2.Items.Count == 0);
        Assert.True(feedList.Folder is null);
    }

    private async Task AddTempItems(FeedService rssService)
    {
        await rssService.ReadFeedAsync("https://daringfireball.net/feeds/json");
        await rssService.ReadFeedAsync("https://feeds.macrumors.com/MacRumors-All");
        await rssService.ReadFeedAsync("https://ascii.jp/rss.xml");
    }
}