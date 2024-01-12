// <copyright file="FeedItem.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace MauiFeed.Models
{
    /// <summary>
    /// Feed Item.
    /// </summary>
    public partial class FeedItem : IRealmObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeedItem"/> class.
        /// </summary>
        public FeedItem()
        {
        }

        [PrimaryKey]
        [MapTo("_id")]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

        /// <summary>
        /// Gets or sets the id from the rss feed.
        /// </summary>
        public string? RssId { get; set; }

        /// <summary>
        /// Gets or sets the Feed List Item.
        /// </summary>
        public FeedListItem? Feed { get; set; }

        /// <summary>
        /// Gets or sets the title of the feed item.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets link (url) to the feed item.
        /// </summary>
        public string? Link { get; set; }

        /// <summary>
        /// Gets or sets link (url) to the feed item.
        /// </summary>
        public string? ExternalLink { get; set; }

        /// <summary>
        /// Gets or sets description of the feed item.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets The publishing date as string.
        /// </summary>
        public string? PublishingDateString { get; set; }

        /// <summary>
        /// Gets or sets The published date as datetime.
        /// </summary>
        public DateTimeOffset PublishingDate { get; set; }

        /// <summary>
        /// Gets or sets The author of the feed item.
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// Gets or sets The content of the feed item.
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Gets or sets The html of the feed item.
        /// </summary>
        public string? Html { get; set; }

        /// <summary>
        /// Gets or sets The image url of the feed item.
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the feed is favorited.
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the feed item has been read.
        /// </summary>
        public bool IsRead { get; set; }
    }
}
