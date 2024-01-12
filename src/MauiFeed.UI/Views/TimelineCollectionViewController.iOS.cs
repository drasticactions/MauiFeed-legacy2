using Drastic.Services;
using Drastic.PureLayout;
using MauiFeed.Models;
using MauiFeed.UI.Models;
using MauiFeed.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Realms;
using System.Text.RegularExpressions;
using MauiFeed.UI;

public class TimelineCollectionViewController : UIViewController, IUICollectionViewDelegate
{
    private readonly Realm context;
    private SidebarItem? sidebarItem;
    private FeedItem? selectedItem;

    private UICollectionView collectionView;
    private UICollectionViewDiffableDataSource<NSString, FeedItemWrapper>? dataSource;
    private NSString feedSelector = new NSString("Feed");
    private Action<FeedItem?> onFeedItemSelected;
    private IErrorHandlerService errorHandler;

    public TimelineCollectionViewController(MainUIViewController controller,
        IServiceProvider provider,
        Action<FeedItem?> onFeedItemSelected)
        : base()
    {
        this.errorHandler = provider.GetRequiredService<IErrorHandlerService>();
        this.onFeedItemSelected = onFeedItemSelected;
        this.context = Realm.GetInstance(provider.GetRequiredService<RealmConfigurationBase>());
        var layout = new UICollectionViewCompositionalLayout((sectionIndex, layoutEnvironment) =>
        {
            var configuration = new UICollectionLayoutListConfiguration(UICollectionLayoutListAppearance.Plain);
            configuration.HeaderMode = UICollectionLayoutListHeaderMode.None;
#if !TVOS
            configuration.ShowsSeparators = true;
#endif
            return NSCollectionLayoutSection.GetSection(configuration, layoutEnvironment);
        });

        this.collectionView = new UICollectionView(this.View!.Bounds, layout);
        this.collectionView.Delegate = this;

        this.View.AddSubview(this.collectionView);
        this.collectionView.TranslatesAutoresizingMaskIntoConstraints = false;
        this.collectionView.PrefetchingEnabled = true;
        this.collectionView.AutoPinEdgesToSuperviewSafeArea();

        this.ConfigureDataSource();
        this.UpdateFeed();
    }

    public bool ShowIcon { get; set; }

    /// <summary>
    /// Gets or sets the sidebar item.
    /// </summary>
    public SidebarItem? SidebarItem
    {
        get { return this.sidebarItem; }

        set
        {
            this.sidebarItem = value;
            this.UpdateFeed();
        }
    }

        /// <summary>
    /// Gets or sets the selected item.
    /// </summary>
    public FeedItem? SelectedItem
    {
        get { return this.selectedItem; }

        set
        {
            this.selectedItem = value;
            this.onFeedItemSelected(this.SelectedItem);
        }
    }

    /// <summary>
    /// Fired when Item is Selected.
    /// </summary>
    /// <param name="collectionView">CollectionView.</param>
    /// <param name="indexPath">Index Path.</param>
    [Export("collectionView:didSelectItemAtIndexPath:")]
    protected void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
    {
        this.context.Write(() =>
        {
            var cell = (TimelineCollectionCell?)this.collectionView.CellForItem(indexPath);
            if (cell is null)
            {
                return;
            }

            var item = cell.Item;
            item.IsRead = true;
            this.SelectedItem = cell.Item;
            cell.UpdateIsRead();
        });
    }

    private void UpdateFeed(bool animated = true)
    {
        var itemList = this.sidebarItem?.Query?.ToList() ?? new List<FeedItem>();
        var items = new List<FeedItemWrapper>();
        var snapshot = new NSDiffableDataSourceSectionSnapshot<FeedItemWrapper>();
        if (this.sidebarItem is not null)
        {
            items.AddRange(itemList.Select(item => new FeedItemWrapper(item)));
        }

        snapshot.AppendItems(items.ToArray());
        this.dataSource!.ApplySnapshot(snapshot, this.feedSelector, animated);
    }

    public class FeedItemWrapper : NSObject
    {
        public FeedItemWrapper(FeedItem item)
        {
            this.Item = item;
        }

        public FeedItem Item { get; set; }
    }

    public class TimelineCollectionCell : UICollectionViewCell
    {
        private FeedItem item;

        private UIView hasSeenHolder = new UIView() { TranslatesAutoresizingMaskIntoConstraints = false };
        private UIView iconHolder = new UIView() { TranslatesAutoresizingMaskIntoConstraints = false };
        private UIView feedHolder = new UIView() { TranslatesAutoresizingMaskIntoConstraints = false };

        private UIImageView hasSeenIcon = new UIImageView() { TranslatesAutoresizingMaskIntoConstraints = false };
        private UIImageView icon = new UIImageView() { TranslatesAutoresizingMaskIntoConstraints = false };
        private UIView content = new UIView() { TranslatesAutoresizingMaskIntoConstraints = false };

        private NSLayoutConstraint withIcon;
        private NSLayoutConstraint withoutIcon;

        private UILabel title = new UILabel()
        {
            Lines = 5,
            LineBreakMode = UILineBreakMode.WordWrap,
            Font = UIFont.PreferredHeadline,
            TextAlignment = UITextAlignment.Left,
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        private UILabel description = new UILabel()
        {
            Lines = 3,
            LineBreakMode = UILineBreakMode.WordWrap,
            Font = UIFont.PreferredSubheadline,
            TextAlignment = UITextAlignment.Left,
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        private UILabel releaseDate = new UILabel()
        {
            Lines = 1,
            Font = UIFont.PreferredCaption1,
            TextAlignment = UITextAlignment.Right,
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        private UILabel author = new UILabel()
        {
            Lines = 1,
            Font = UIFont.PreferredCaption1,
            TextAlignment = UITextAlignment.Left,
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        public TimelineCollectionCell(IntPtr handle)
            : base(handle)
        {
#if !TVOS
            this.icon.Layer.BackgroundColor = UIColor.White.CGColor;
#endif
            this.icon.Layer.CornerRadius = 5;
            this.icon.Layer.MasksToBounds = true;
            this.withIcon = this.iconHolder.WidthAnchor.ConstraintEqualTo(25f);
            this.withoutIcon = this.iconHolder.WidthAnchor.ConstraintEqualTo(0f);
            this.SetupUI();
            this.SetupLayout();
            this.SelectedBackgroundView = new UIView(this.Frame) { BackgroundColor = UIColor.Blue };
            // this.SetupCell(info, showIcon);
        }

        public FeedItem Item => this.item;

        public override UICollectionViewLayoutAttributes PreferredLayoutAttributesFittingAttributes(
            UICollectionViewLayoutAttributes layoutAttributes)
        {
            return base.PreferredLayoutAttributesFittingAttributes(layoutAttributes);
        }

        public void SetupUI()
        {
            this.ContentView.AddSubview(this.content);

            this.content.AddSubview(this.hasSeenHolder);
            this.content.AddSubview(this.iconHolder);
            this.content.AddSubview(this.feedHolder);

            this.hasSeenHolder.AddSubview(this.hasSeenIcon);

            this.iconHolder.AddSubview(this.icon);

            this.feedHolder.AddSubview(this.title);
            this.feedHolder.AddSubview(this.description);
            this.feedHolder.AddSubview(this.author);
            this.feedHolder.AddSubview(this.releaseDate);

            this.hasSeenIcon.Image = UIImage.GetSystemImage("circle.fill");
        }

        public void SetupLayout()
        {
            this.content.AutoPinEdgesToSuperviewEdges();
            this.hasSeenHolder.AutoPinEdgesToSuperviewEdgesExcludingEdge(UIEdgeInsets.Zero, ALEdge.Right);
            this.hasSeenHolder.AutoPinEdge(ALEdge.Right, ALEdge.Left, this.iconHolder);
            this.hasSeenHolder.AutoSetDimension(ALDimension.Width, 25f);

            this.iconHolder.AutoPinEdge(ALEdge.Left, ALEdge.Right, this.hasSeenHolder);
            this.iconHolder.AutoPinEdge(ALEdge.Right, ALEdge.Left, this.feedHolder);
            // this.iconHolder.AutoPinEdge(ALEdge.Top, ALEdge.Top, this.content);
            // this.iconHolder.AutoPinEdge(ALEdge.Bottom, ALEdge.Bottom, this.content);
            this.iconHolder.AutoAlignAxis(ALAxis.Horizontal, this.feedHolder);
            this.iconHolder.AutoSetDimension(ALDimension.Width, 25f);

            this.hasSeenIcon.AutoCenterInSuperview();
            this.hasSeenIcon.AutoSetDimensionsToSize(new CGSize(12, 12));

            this.icon.AutoCenterInSuperview();
            this.icon.AutoSetDimensionsToSize(new CGSize(25f, 25f));

            this.feedHolder.AutoPinEdgesToSuperviewEdgesExcludingEdge(
                new UIEdgeInsets(top: 0f, left: 0f, bottom: 0f, right: 0f), ALEdge.Left);
            this.feedHolder.AutoPinEdge(ALEdge.Left, ALEdge.Right, this.iconHolder);

            this.title.AutoPinEdge(ALEdge.Top, ALEdge.Top, this.feedHolder, 5f);
            this.title.AutoPinEdge(ALEdge.Right, ALEdge.Right, this.feedHolder, -15f);
            this.title.AutoPinEdge(ALEdge.Left, ALEdge.Left, this.feedHolder, 10f);

            this.description.AutoPinEdge(ALEdge.Top, ALEdge.Bottom, this.title, 0);
            this.description.AutoPinEdge(ALEdge.Right, ALEdge.Right, this.title);
            this.description.AutoPinEdge(ALEdge.Left, ALEdge.Left, this.title);
            this.description.AutoPinEdge(ALEdge.Bottom, ALEdge.Bottom, this.releaseDate, -15f);

            this.author.AutoPinEdge(ALEdge.Bottom, ALEdge.Bottom, this.content, -5);
            this.author.AutoPinEdge(ALEdge.Left, ALEdge.Left, this.title);
            this.author.AutoPinEdge(ALEdge.Right, ALEdge.Left, this.releaseDate);

            this.releaseDate.AutoPinEdge(ALEdge.Bottom, ALEdge.Bottom, this.content, -5f);
            this.releaseDate.AutoPinEdge(ALEdge.Right, ALEdge.Right, this.title);
            this.releaseDate.AutoPinEdge(ALEdge.Left, ALEdge.Right, this.author);

        }

        public void UpdateConstraints()
        {
            this.ContentView.UpdateConstraints();
        }

        public void SetupCell(FeedItem item, bool showIcon)
        {
            this.item = item;

            this.icon.Image = UIImage.LoadFromData(NSData.FromArray(item.Feed!.ImageCache!))!.WithRoundedCorners(5f);
            this.title.Text = item.Title;
            this.author.Text = item.Author;

            var htmlString = !string.IsNullOrEmpty(item.Description) ? item.Description : item.Content;

            // We don't want to render the HTML, we just want to get the raw text out.
            this.description.Text = Regex.Replace(htmlString ?? string.Empty, "<[^>]*>", string.Empty)!.Trim();

            this.releaseDate.Text = item.PublishingDate.DateTime.ToShortDateString();

            this.withIcon.Active = showIcon;
            this.withoutIcon.Active = !showIcon;

            this.icon.Hidden = !showIcon;

            this.UpdateIsRead();

            this.LayoutIfNeeded();
        }

        public void UpdateIsRead()
        {
            if (this.item?.IsFavorite ?? false)
            {
                this.InvokeOnMainThread(() =>
                    this.hasSeenIcon.Image = UIImage.GetSystemImage("circle.fill")!.ApplyTintColor(UIColor.Yellow));
            }
            else
            {
                this.InvokeOnMainThread(() => this.hasSeenIcon.Image = this.item?.IsRead ?? false
                    ? UIImage.GetSystemImage("circle")
                    : UIImage.GetSystemImage("circle.fill"));
            }
        }
    }

    private void ConfigureDataSource()
    {
        var rowRegistration = UICollectionViewCellRegistration.GetRegistration(
            typeof(TimelineCollectionCell),
            new UICollectionViewCellRegistrationConfigurationHandler((cell, indexpath, item) =>
                {
                    var sidebarItem = item as FeedItemWrapper;
                    if (sidebarItem is null || collectionView is null)
                    {
                        throw new Exception();
                    }

                    if (cell is not TimelineCollectionCell timelineCell)
                    {
                        throw new Exception();
                    }

                    timelineCell.SetupCell(sidebarItem.Item, this.ShowIcon);
                }
            ));

        this.dataSource = new UICollectionViewDiffableDataSource<NSString, FeedItemWrapper>(
            this.collectionView,
            new UICollectionViewDiffableDataSourceCellProvider((collectionView, indexPath, item) =>
            {
                var view = collectionView.DequeueConfiguredReusableCell(rowRegistration, indexPath, item);
                return view;
            })
        );
    }

    private UICollectionViewLayout CreateLayout()
    {
        return new UICollectionViewCompositionalLayout((sectionIndex, layoutEnvironment) =>
        {
#if TVOS
            var app = UICollectionLayoutListAppearance.Plain;
#else
            var app = UICollectionLayoutListAppearance.Sidebar;
#endif
            var configuration = new UICollectionLayoutListConfiguration(app);
#if !TVOS
            configuration.ShowsSeparators = true;
#endif
            configuration.HeaderMode = UICollectionLayoutListHeaderMode.None;
            return NSCollectionLayoutSection.GetSection(configuration, layoutEnvironment);
        });
    }
}