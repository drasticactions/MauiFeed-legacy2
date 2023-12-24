// <copyright file="DatabaseContext.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using MauiFeed.Models;
using Microsoft.EntityFrameworkCore;

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

        public int AddOrUpdateFeedFolder(FeedFolder folder)
        {
            if (folder.Id == 0)
            {
                this.FeedFolder!.Add(folder);
            }
            else
            {
                this.FeedFolder!.Update(folder);
            }

            var result = this.SaveChanges();
            return result;
        }

        public int AddOrUpdateFeedListItem(FeedListItem item)
        {
            if (item.Id == 0)
            {
                this.FeedListItems!.Add(item);
            }
            else
            {
                this.FeedListItems!.Update(item);
            }

            var result = this.SaveChanges();
            return result;
        }

        public int RemoveFeedListItem(FeedListItem item)
        {
            this.FeedListItems!.Remove(item);
            var result = this.SaveChanges();
            return result;
        }

        public int RemoveFeedFolder(FeedFolder folder)
        {
            foreach (var item in folder.Items ?? new List<FeedListItem>())
            {
                item.FolderId = null;
            }

            this.FeedFolder!.Remove(folder);
            var result = this.SaveChanges();
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
