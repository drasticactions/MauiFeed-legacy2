// <copyright file="FeedFolder.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations.Schema;

namespace MauiFeed.Models
{
    /// <summary>
    /// Feed Folder.
    /// </summary>
    public partial class FeedFolder : IRealmObject
    {
        [PrimaryKey]
        [MapTo("_id")]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

        /// <summary>
        /// Gets the list of Feed List Items.
        /// </summary>
#nullable disable
        public IList<FeedListItem> Items { get; }
#nullable enable

        /// <summary>
        /// Gets or sets the name of the folder.
        /// </summary>
        public string? Name { get; set; }
    }
}
