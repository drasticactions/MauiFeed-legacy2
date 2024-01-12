// <copyright file="FeedListItem.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MauiFeed.Models
{
    /// <summary>
    /// Feed List Item.
    /// </summary>
    public partial class FeedListItem : IRealmObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeedListItem"/> class.
        /// </summary>
        public FeedListItem()
        {
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [PrimaryKey]
        [MapTo("_id")]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

        /// <summary>
        /// Gets or sets the folder.
        /// </summary>
        public FeedFolder? Folder { get; set; }

        /// <summary>
        /// Gets or sets the feed name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the feed description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the feed Language.
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Gets or sets the last updated date.
        /// </summary>
        public DateTimeOffset? LastUpdatedDate { get; set; }

        /// <summary>
        /// Gets or sets the last updated date string.
        /// </summary>
        public string? LastUpdatedDateString { get; set; }

        /// <summary>
        /// Gets or sets the image uri.
        /// </summary>
        public string? ImageUri { get; set; }

        /// <summary>
        /// Gets or sets the Feed Uri.
        /// </summary>
        public string? Uri { get; set; }

        /// <summary>
        /// Gets or sets the image cache.
        /// </summary>
        public byte[]? ImageCache { get; set; }

        /// <summary>
        /// Gets or sets the Feed Link.
        /// </summary>
        public string? Link { get; set; }

        /// <summary>
        /// Gets or sets the Feed Type.
        /// </summary>
        public int FeedTypeInt { get; set; }

        /// <summary>
        /// Gets or sets the Feed Type.
        /// </summary>
        public FeedType FeedType
        {
            get => (FeedType)this.FeedTypeInt;

            set => this.FeedTypeInt = (int)value;
        }

        /// <summary>
        /// Gets the list of feed items.
        /// </summary>
        public IList<FeedItem> Items { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the feed is favorited.
        /// </summary>
        public bool IsFavorite { get; set; }
    }
}
