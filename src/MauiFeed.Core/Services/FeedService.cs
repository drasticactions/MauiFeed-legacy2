// <copyright file="FeedService.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.Data.SqlTypes;
using System.Text.RegularExpressions;
using System.Xml;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CodeHollow.FeedReader;
using Drastic.Services;
using Drastic.Tools;
using JsonFeedNet;
using MauiFeed.Models;
using MauiFeed.Tools;
using Newtonsoft.Json.Linq;
using FeedType = MauiFeed.Models.FeedType;

namespace MauiFeed.Services
{
    /// <summary>
    /// Feed Service.
    /// </summary>
    public class FeedService
    {
        private HttpClient client;
        private HtmlParser parser;
        private byte[] placeholderImage;
        private IErrorHandlerService errorHandler;
        private Realm realm;

        private IDisposable? feedListItemSubscription;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedService"/> class.
        /// </summary>
        /// <param name="errorHandler">Error handler.</param>
        /// <param name="client">Optional HttpClient.</param>
        public FeedService(IErrorHandlerService errorHandler, RealmConfigurationBase config,
            HttpClient? client = default)
        {
            this.errorHandler = errorHandler;
            this.client = client ?? new HttpClient();
            this.parser = new HtmlParser();
            this.placeholderImage = Utilities.GetPlaceholderIcon();
            this.realm = Realm.GetInstance(config);
            this.feedListItemSubscription = this.realm.All<FeedListItem>().SubscribeForNotifications(this.OnFeedListItemUpdate);
        }

        /// <summary>
        /// Read Feed Async.
        /// </summary>
        /// <param name="feedUri">Feed Item.</param>
        /// <param name="token">Token.</param>
        /// <returns>FeedList and Item.</returns>
        public async Task ReadFeedAsync(string feedUri, CancellationToken? token = default)
            => await this.ReadFeedAsync(new Uri(feedUri), token);

        /// <summary>
        /// Read Feed Async.
        /// </summary>
        /// <param name="feedUri">Feed Item.</param>
        /// <param name="token">Token.</param>
        /// <returns>FeedList and Item.</returns>
        public async Task ReadFeedAsync(Uri feedUri, CancellationToken? token = default)
        {
            var feedItem = this.realm.All<FeedListItem>().FirstOrDefault(n => n.Uri == feedUri.ToString()) ??
                           new FeedListItem() { Uri = feedUri.ToString() };
            await this.ReadFeedAsync(feedItem, token);
            await this.realm.WriteAsync(() =>
            {
                this.realm.Add(feedItem, true);
            });
        }

        /// <summary>
        /// Refresh Feeds Async.
        /// </summary>
        public async Task RefreshFeedsAsync()
        {
            var feeds = this.realm.All<FeedListItem>();
            using var transaction = await this.realm.BeginWriteAsync();
            foreach (var feed in feeds)
            {
                await this.ReadFeedAsync(feed);
            }

            await transaction.CommitAsync();
        }

        /// <summary>
        /// Read Feed Async.
        /// </summary>
        /// <param name="feedItem">Feed Item.</param>
        /// <param name="token">Token.</param>
        /// <returns>FeedList and Item.</returns>
        /// <exception cref="ArgumentNullException">Thrown if URI not set on feed item.</exception>
        /// <exception cref="NotImplementedException">Thrown if unknown feed type detected.</exception>
        public async Task ReadFeedAsync(FeedListItem feedItem, CancellationToken? token = default)
        {
            if (feedItem.Uri is null)
            {
                throw new ArgumentNullException(nameof(feedItem));
            }

            var cancelationToken = token ?? CancellationToken.None;
            string stringResponse = string.Empty;
            try
            {
                using var response = await this.client.GetAsync(feedItem.Uri, cancelationToken);
                stringResponse = (await response.Content.ReadAsStringAsync()).Trim();
            }
            catch (Exception ex)
            {
                this.errorHandler.HandleError(ex);
            }

            // We have a response, time to figure out what to do with it.
            try
            {
                if (!string.IsNullOrEmpty(stringResponse))
                {
                    var type = feedItem.FeedType;

                    if (type == Models.FeedType.Unknown)
                    {
                        type = this.ValidateString(stringResponse);
                    }

                    System.Diagnostics.Debug.Assert(
                        type != Models.FeedType.Unknown,
                        "Should not have unknown at this point");

                    switch (type)
                    {
                        case Models.FeedType.Unknown:
                            throw new NotImplementedException();
                        case FeedType.Rss:
                            await this.ParseWithFeedReaderAsync(stringResponse, feedItem, cancelationToken);
                            break;
                        case FeedType.Json:
                            await this.ParseWithJsonFeedAsync(stringResponse, feedItem, cancelationToken);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            catch (Exception ex)
            {
                this.errorHandler.HandleError(ex);
            }
        }

        private Task ParseWithFeedReaderAsync(
            string stringResponse,
            FeedListItem feedItem,
            CancellationToken? token = default)
        {
            var feed = FeedReader.ReadFromString(stringResponse);

            return this.realm.WriteAsync(() =>
            {
                feedItem.Update(feed);
                this.realm.Add(feedItem, true);
            });
        }

        private Task ParseWithJsonFeedAsync(
            string stringResponse,
            FeedListItem feedItem,
            CancellationToken? token = default)
        {
            var feed = JsonFeed.Parse(stringResponse);
            return this.realm.WriteAsync(() =>
                {
                    feedItem.Update(feed);
                    this.realm.Add(feedItem, true);
                });
        }

        private MauiFeed.Models.FeedType ValidateString(string stringResponse)
        {
            if (this.IsXml(stringResponse))
            {
                return MauiFeed.Models.FeedType.Rss;
            }
            else if (this.IsJson(stringResponse))
            {
                return MauiFeed.Models.FeedType.Json;
            }

            return Models.FeedType.Unknown;
        }

        private bool IsXml(string stringResponse)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(stringResponse);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool IsJson(string stringResponse)
        {
            try
            {
                return stringResponse.Trim().StartsWith("{") && stringResponse.Trim().EndsWith("}");
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<byte[]?> GetImageForItem(FeedListItem item)
        {
            var image = item.ImageCache;

            if (image?.IsValidImage() ?? false)
            {
                return image;
            }

            if (item.ImageUri is not null)
            {
                try
                {
                    image = await this.GetByteArrayAsync(item.ImageUri);
                }
                catch (Exception)
                {
                    // If it fails to work for whatever reason, ignore it for now, use the placeholder.
                }
            }

            if (!image.IsValidImage() && item.Uri is not null)
            {
                try
                {
                    // If ImageUri is null, try to get the favicon from the site itself.
                    image = await this.GetFaviconFromUriAsync(new Uri(item.Uri));
                }
                catch (Exception)
                {
                    // If it fails to work for whatever reason, ignore it for now, use the placeholder.
                }
            }

            if (!image.IsValidImage() && item.Items.Any() && item.Items.First().Link is { } link)
            {
                try
                {
                    // If ImageUri is null, try to get the favicon from the site itself.
                    image = await this.GetFaviconFromUriAsync(new Uri(link));
                }
                catch (Exception)
                {
                    // If it fails to work for whatever reason, ignore it for now, use the placeholder.
                }
            }

            if (!image.IsValidImage())
            {
                try
                {
                    // If all else fails, get the icon from the webpage itself by parsing it.
                    image = await this.ParseRootWebpageForIcon(new Uri(item.Uri!));
                }
                catch (Exception)
                {
                    // If it fails to work for whatever reason, ignore it for now, use the placeholder.
                }
            }

            return image.IsValidImage() ? image : this.placeholderImage;
        }

        private async Task<byte[]?> GetFaviconFromUriAsync(Uri uri)
            => await this.GetByteArrayAsync($"{uri.Scheme}://{uri.Host}/favicon.ico");

        private async Task<byte[]?> ParseRootWebpageForIcon(Uri uri)
        {
            var htmlString = await this.client.GetStringAsync(new Uri($"{uri.Scheme}://{uri.Host}/"));
            var html = await this.parser.ParseDocumentAsync(htmlString);
            var favIcon = html.QuerySelector("link[rel~='icon']");
            if (favIcon is not IHtmlLinkElement anchor)
            {
                return null;
            }

            return anchor.Href is not null ? await this.GetByteArrayAsync(anchor.Href) : null;
        }

        private async Task<byte[]?> GetByteArrayAsync(string uri)
        {
            using HttpResponseMessage response = await this.client.GetAsync(uri);

            if (!response.IsSuccessStatusCode)
            {
                throw new ArgumentException("Could not get image");
            }

            return await response.Content.ReadAsByteArrayAsync();
        }

        private async void OnFeedListItemUpdate(IRealmCollection<FeedListItem> sender, ChangeSet? changes)
        {
            if (changes is null)
            {
                return;
            }

            foreach (var change in changes.InsertedIndices)
            {
                this.GetImageForItemAsync(sender[change]).FireAndForgetSafeAsync(this.errorHandler);
                this.ReadFeedAsync(sender[change]).FireAndForgetSafeAsync(this.errorHandler);
            }
        }

        private async Task GetImageForItemAsync(FeedListItem item)
        {
            if (item.ImageCache is null || item.ImageCache.Length == 0)
            {
                var image = await this.GetImageForItem(item) ?? this.placeholderImage;
                await this.realm.WriteAsync(() =>
                {
                    item.ImageCache = image;
                    this.realm.Add(item, true);
                });
            }
        }
    }
}
