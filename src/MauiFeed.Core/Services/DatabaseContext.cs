// <copyright file="DatabaseContext.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using MauiFeed.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MauiFeed.Services
{
    /// <summary>
    /// Database Context.
    /// </summary>
    public class DatabaseContext : DbContext
    {
        private string databasePath = "database.db";

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseContext"/> class.
        /// </summary>
        /// <param name="databasePath">Path to database.</param>
        public DatabaseContext(string databasePath = "")
        {
            if (!string.IsNullOrEmpty(databasePath))
            {
                this.databasePath = databasePath;
            }

            this.Database.EnsureCreated();
        }

        public event EventHandler<FeedListItemContentEventArgs>? OnFeedListItemUpdate;

        public event EventHandler<FeedListItemContentEventArgs>? OnFeedListItemRemove;

        public event EventHandler<FeedFolderContentEventArgs>? OnFeedFolderUpdate;

        public event EventHandler<FeedFolderContentEventArgs>? OnFeedFolderRemove;

        public event EventHandler? OnRefreshFeeds;

        /// <summary>
        /// Gets the database Path.
        /// </summary>
        public string Location => this.databasePath;

        /// <summary>
        /// Gets or sets the list of feed list items.
        /// </summary>
        public DbSet<FeedListItem>? FeedListItems { get; set; }

        /// <summary>
        /// Gets or sets the list of feed items.
        /// </summary>
        public DbSet<FeedItem>? FeedItems { get; set; }

        /// <summary>
        /// Gets or sets the list of feed folders.
        /// </summary>
        public DbSet<FeedFolder>? FeedFolder { get; set; }

        /// <summary>
        /// Gets or sets the App Settings.
        /// </summary>
        public DbSet<AppSettings>? AppSettings { get; set; }

        public async Task<int> AddOrUpdateFeedFolderAsync(FeedFolder folder)
        {
            if (folder.Id == 0)
            {
                await this.FeedFolder!.AddAsync(folder);
            }
            else
            {
                this.FeedFolder!.Update(folder);
            }

            var result = await this.SaveChangesAsync();
            this.OnFeedFolderUpdate?.Invoke(this, new FeedFolderContentEventArgs(folder));
            return result;
        }

        public async Task<int> AddOrUpdateFeedListItem(FeedListItem item)
        {
            if (item.Id == 0)
            {
                await this.FeedListItems!.AddAsync(item);
            }
            else
            {
                this.FeedListItems!.Update(item);
            }

            var result = await this.SaveChangesAsync();
            this.OnFeedListItemUpdate?.Invoke(this, new FeedListItemContentEventArgs(item));
            return result;
        }

        public async Task<int> RemoveFeedListItemAsync(FeedListItem item)
        {
            this.FeedListItems!.Remove(item);
            var result = await this.SaveChangesAsync();
            this.OnFeedListItemRemove?.Invoke(this, new FeedListItemContentEventArgs(item));
            return result;
        }

        public async Task<int> RemoveFeedFolderAsync(FeedFolder folder)
        {
            foreach (var item in folder.Items ?? new List<FeedListItem>())
            {
                item.FolderId = null;
            }

            this.FeedFolder!.Remove(folder);
            var result = await this.SaveChangesAsync();
            this.OnFeedFolderRemove?.Invoke(this, new FeedFolderContentEventArgs(folder));
            return result;
        }

        public async Task<int> RefreshFeedsAsync(
            IEnumerable<FeedListItem> feedResults,
            IEnumerable<FeedItem> feedItemResults)
        {
            this.FeedListItems!.UpdateRange(feedResults);
            foreach (var feedItem in feedItemResults)
            {
                var val = await this.FeedItems!.AnyAsync(n =>
                    n.RssId == feedItem.RssId && n.FeedListItemId == n.FeedListItemId);
                if (!val)
                {
                    await this.FeedItems!.AddAsync(feedItem);
                }
            }

            var result = await this.SaveChangesAsync();
            this.OnRefreshFeeds?.Invoke(this, EventArgs.Empty);
            return result;
        }

        public async Task<int> AddOrUpdateFeedListItemAsync(FeedListItem feed, IList<FeedItem> feedListItems)
        {
            if (feed?.Id <= 0)
            {
                await this.FeedListItems!.AddAsync(feed!);
            }
            else
            {
                this.FeedListItems!.Update(feed!);
            }

            foreach (var feedItem in feedListItems!)
            {
                // ... that we will then set for the individual items to link back to this one.
                var oldItem = await this.FeedItems!.FirstOrDefaultAsync(n => n.RssId == feedItem.RssId);
                if (oldItem is not null)
                {
                    continue;
                }

                feedItem.FeedListItemId = feed!.Id;
                feedItem.Feed = feed;
                await this.FeedItems!.AddAsync(feedItem);
            }

            var result = await this.SaveChangesAsync();
            this.OnFeedListItemUpdate?.Invoke(this, new FeedListItemContentEventArgs(feed!));
            return result;
        }

        /// <summary>
        /// Run when configuring the database.
        /// </summary>
        /// <param name="optionsBuilder"><see cref="DbContextOptionsBuilder"/>.</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Filename={this.databasePath}");
            optionsBuilder.EnableSensitiveDataLogging();
        }

        /// <summary>
        /// Run when building the model.
        /// </summary>
        /// <param name="modelBuilder"><see cref="ModelBuilder"/>.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
            {
                throw new ArgumentNullException(nameof(modelBuilder));
            }

            modelBuilder.Entity<AppSettings>().HasKey(n => n.Id);
            modelBuilder.Entity<FeedFolder>().HasKey(n => n.Id);
            modelBuilder.Entity<FeedListItem>().HasKey(n => n.Id);
            modelBuilder.Entity<FeedListItem>().HasIndex(n => n.Uri).IsUnique();
            modelBuilder.Entity<FeedItem>().HasKey(n => n.Id);
            modelBuilder.Entity<FeedItem>().HasIndex(n => n.RssId).IsUnique(false);
        }
    }
}

public class FeedListItemContentEventArgs : EventArgs
{
    public FeedListItemContentEventArgs(FeedListItem feed)
    {
        this.Feed = feed;
    }

    public FeedListItem Feed { get; }
}

public class FeedFolderContentEventArgs : EventArgs
{
    public FeedFolderContentEventArgs(FeedFolder feed)
    {
        this.Folder = feed;
    }

    public FeedFolder Folder { get; }
}