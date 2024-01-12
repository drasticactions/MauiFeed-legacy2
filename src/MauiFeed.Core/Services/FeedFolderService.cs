// <copyright file="FeedFolderService.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using MauiFeed.Models;

namespace MauiFeed.Services;

public class FeedFolderService
{
    private Realm databaseContext;

    public FeedFolderService(RealmConfigurationBase databaseContext)
    {
        this.databaseContext = Realm.GetInstance(databaseContext);
    }

    public Task AddFeedFolderAsync(string name)
    {
        var folder = new FeedFolder() { Name = name };
        return this.AddFeedFolderAsync(folder);
    }

    public Task AddFeedFolderAsync(FeedFolder folder)
    {
        return this.databaseContext.WriteAsync(() => this.databaseContext.Add(folder, true));
    }

    public Task RemoveFeedFolderAsync(FeedFolder folder)
    {
        return this.databaseContext.WriteAsync(() =>
        {
            folder!.Items.Clear();
            this.databaseContext.Remove(folder);
        });
    }

    public Task RenameFeedFolderAsync(FeedFolder folder, string name)
    {
        return this.databaseContext.WriteAsync(() => folder.Name = name);
    }

    public Task RemoveFeedFromFolderAsync(FeedFolder folder, FeedListItem feed)
    {
        return this.databaseContext.WriteAsync(() =>
        {
            if (feed.Folder is not null)
            {
                feed.Folder.Items.Remove(feed);
            }

            feed.Folder = null;
        });
    }

    public Task RemoveFeedAsync(FeedListItem feed)
    {
        return this.databaseContext.WriteAsync(() =>
        {
            if (feed.Folder is not null)
            {
                feed.Folder.Items.Remove(feed);
            }

            this.databaseContext.Remove(feed);
        });
    }

    public Task AddFeedToFolderAsync(FeedFolder folder, FeedListItem feed)
    {
        return this.databaseContext.WriteAsync(() =>
        {
            if (feed.Folder is not null)
            {
                feed.Folder.Items.Remove(feed);
            }

            if (!folder.Items.Contains(feed))
            {
                folder.Items.Add(feed);
            }

            feed.Folder = folder;
        });
    }
}