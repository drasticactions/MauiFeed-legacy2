// <copyright file="OpmlFeedListItemFactory.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using MauiFeed.Models;
using MauiFeed.Models.OPML;
using MauiFeed.Tools;

namespace MauiFeed.Services;

/// <summary>
/// Opml Feed List Item Factory.
/// </summary>
public class OpmlFeedListItemFactory
{
    private HttpClient client;
    private Realm context;
    private byte[] placeholderImage;
    private HtmlParser parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpmlFeedListItemFactory"/> class.
    /// </summary>
    /// <param name="context">Context.</param>
    /// <param name="client">Http Client.</param>
    public OpmlFeedListItemFactory(RealmConfigurationBase context, HttpClient? client = default)
    {
        this.context = Realm.GetInstance(context);
        this.client = client ?? new HttpClient();
        this.parser = new HtmlParser();
        this.placeholderImage = Utilities.GetPlaceholderIcon();
    }

    /// <summary>
    /// Generate an Opml feed from given database.
    /// </summary>
    /// <returns>Opml.</returns>
    public Opml GenerateOpmlFeed()
    {
        var opml = new Opml
        {
            Head = new Head()
            {
                Title = "MauiFeed",
                DateCreated = DateTime.UtcNow,
            },
            Body = new Body(),
        };

        foreach (var folder in this.context.All<FeedFolder>())
        {
            var folderOutline = new Outline() { Text = folder.Name, Title = folder.Name };
            foreach (var feedItem in folder.Items ?? new List<FeedListItem>())
            {
                var feedOutline = new Outline()
                {
                    Text = feedItem.Name,
                    Title = feedItem.Name,
                    Description = feedItem.Description,
                    Type = "rss",
                    Version = "RSS",
                    HTMLUrl = feedItem.Link,
                    XMLUrl = feedItem.Uri?.ToString(),
                };
                folderOutline.Outlines.Add(feedOutline);
            }

            opml.Body.Outlines.Add(folderOutline);
        }

        foreach (var feedItem in this.context.All<FeedListItem>()!.Where(n => n.Folder == null))
        {
            var feedOutline = new Outline()
            {
                Text = feedItem.Name,
                Title = feedItem.Name,
                Description = feedItem.Description,
                Type = "rss",
                Version = "RSS",
                HTMLUrl = feedItem.Link,
                XMLUrl = feedItem.Uri?.ToString(),
            };
            opml.Body.Outlines.Add(feedOutline);
        }

        return opml;
    }

    /// <summary>
    /// Generate a list of Feed List Items from Opml documents.
    /// </summary>
    /// <param name="opml">Opml.</param>
    /// <returns>Task.</returns>
    public Task GenerateFeedListItemsFromOpmlAsync(Opml opml)
    {
        return this.context.WriteAsync(() =>
        {
            var opmlGroup = opml.Body.Outlines.SelectMany(n => this.Flatten(n)).Where(n => n.IsFeed).Select(n => n.ToFeedListItem()).ToList();
            var cachedFolders = new List<FeedFolder>();

            foreach (var item in opmlGroup.Where(item => !this.context.All<FeedListItem>()!.Any(n => n.Uri == item.Uri)))
            {
                if (item.Folder is not null)
                {
                    var cachedFolder = cachedFolders.FirstOrDefault(n => n.Name == item.Folder.Name);
                    if (cachedFolder is not null)
                    {
                        item.Folder = cachedFolder;
                    }
                    else
                    {
                        var existingFolder =
                            this.context.All<FeedFolder>()!.FirstOrDefault(n => n.Name == item.Folder.Name);
                        if (existingFolder is not null)
                        {
                            item.Folder = existingFolder;
                        }
                        else
                        {
                            this.context.Add(item.Folder, true);
                            cachedFolders.Add(item.Folder);
                        }
                    }
                }

                if (item.Folder!.Items?.Contains(item) ?? false)
                {
                    // How did we get here?
                }
                else
                {
                    item.Folder!.Items?.Add(item);
                }
            }
        });
    }

    private IEnumerable<Outline> Flatten(Outline forum)
    {
        yield return forum;
        var forums = forum.Outlines;
        foreach (var descendant in forum.Outlines.SelectMany(this.Flatten))
        {
            yield return descendant;
        }
    }
}