
using System.Text.RegularExpressions;
using Drastic.PureLayout;
using MauiFeed.Models;
using MauiFeed.UI.Models;

namespace MauiFeed.UI.Views;

public class TimelineCollectionViewController : UIViewController, IUICollectionViewDelegate
{
    private SidebarItem? sidebarItem;
    private UICollectionView collectionView;
    private UICollectionViewDiffableDataSource<NSString, FeedItemWrapper>? dataSource;
    private NSString feedSelector = new NSString("Feed");
    
    public TimelineCollectionViewController(MainUIViewController controller,
        IServiceProvider provider,
        Action<FeedItem> onFeedItemSelected)
        : base()
    {
        var layout = new UICollectionViewFlowLayout {
            ScrollDirection = UICollectionViewScrollDirection.Vertical,
            EstimatedItemSize = UICollectionViewFlowLayout.AutomaticSize
        };
        this.collectionView = new UICollectionView(this.View!.Bounds, layout);
        this.collectionView.Delegate = this;

        this.View.AddSubview(this.collectionView);
        this.collectionView.TranslatesAutoresizingMaskIntoConstraints = false;
        this.collectionView.PrefetchingEnabled = true;
        this.collectionView.AutoPinEdgesToSuperviewSafeArea();

        this.ConfigureDataSource();
        this.UpdateFeed();
    }

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

    public IList<FeedItem> Items => this.sidebarItem?.Query?.ToList() ?? new List<FeedItem>();

    private void UpdateFeed(bool animated = true)
    {
        var items = new List<FeedItemWrapper>();
        var snapshot = new NSDiffableDataSourceSectionSnapshot<FeedItemWrapper>();
        if (this.sidebarItem is not null)
        {
            items.AddRange(this.Items.Select(item => new FeedItemWrapper(item)));
        }

        snapshot.AppendItems(items.ToArray());
        this.dataSource!.ApplySnapshot(snapshot, this.feedSelector, animated);
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
                    
                    timelineCell.SetupCell(sidebarItem.Item, true);
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
            var configuration = new UICollectionLayoutListConfiguration(UICollectionLayoutListAppearance.Plain);
#if !TVOS
            configuration.ShowsSeparators = true;
#endif
            configuration.HeaderMode = UICollectionLayoutListHeaderMode.None;
            return NSCollectionLayoutSection.GetSection(configuration, layoutEnvironment);
        });
    }
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

    private UILabel title = new UILabel()
        { Lines = 3, Font = UIFont.PreferredHeadline, TextAlignment = UITextAlignment.Left, TranslatesAutoresizingMaskIntoConstraints = false };

    private UILabel description = new UILabel()
        { Lines = 2, Font = UIFont.PreferredSubheadline, TextAlignment = UITextAlignment.Left, TranslatesAutoresizingMaskIntoConstraints = false };

    private UILabel releaseDate = new UILabel()
        { Lines = 1, Font = UIFont.PreferredCaption1, TextAlignment = UITextAlignment.Right, TranslatesAutoresizingMaskIntoConstraints = false };

    private UILabel author = new UILabel()
        { Lines = 1, Font = UIFont.PreferredCaption1, TextAlignment = UITextAlignment.Left, TranslatesAutoresizingMaskIntoConstraints = false };

    public TimelineCollectionCell(IntPtr handle)
        : base(handle)
    {
        this.icon.Layer.CornerRadius = 5;
        this.icon.Layer.MasksToBounds = true;
#if IOS
                this.title.Lines = 2;
                this.description.Lines = 1;
#endif

        this.SetupUI();
        this.SetupLayout();
        // this.SetupCell(info, showIcon);
    }

    public override UICollectionViewLayoutAttributes PreferredLayoutAttributesFittingAttributes(
        UICollectionViewLayoutAttributes layoutAttributes)
    {
        return base.PreferredLayoutAttributesFittingAttributes(layoutAttributes);
    }

    public void SetupUI()
    {
        this.ContentView.AddSubview(this.hasSeenHolder);
        this.ContentView.AddSubview(this.iconHolder);
        this.ContentView.AddSubview(this.feedHolder);

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
        this.ContentView.AutoSetContentHuggingPriorityForAxis(ALAxis.Horizontal);
        this.hasSeenHolder.AutoPinEdgesToSuperviewEdgesExcludingEdge(UIEdgeInsets.Zero, ALEdge.Right);
        this.hasSeenHolder.AutoPinEdge(ALEdge.Right, ALEdge.Left, this.iconHolder);
        this.hasSeenHolder.AutoSetDimension(ALDimension.Width, 25f);

        this.iconHolder.AutoPinEdge(ALEdge.Left, ALEdge.Right, this.hasSeenHolder);
        this.iconHolder.AutoPinEdge(ALEdge.Right, ALEdge.Left, this.feedHolder);
        this.iconHolder.AutoPinEdge(ALEdge.Top, ALEdge.Top, this.ContentView);
        this.iconHolder.AutoPinEdge(ALEdge.Bottom, ALEdge.Bottom, this.ContentView);
        this.iconHolder.AutoSetDimension(ALDimension.Width, 60f);

        this.hasSeenIcon.AutoCenterInSuperview();
        this.hasSeenIcon.AutoSetDimensionsToSize(new CGSize(12, 12));

        this.icon.AutoCenterInSuperview();
        this.icon.AutoSetDimensionsToSize(new CGSize(50f, 50f));

        this.feedHolder.AutoPinEdgesToSuperviewEdgesExcludingEdge(
            new UIEdgeInsets(top: 0f, left: 0f, bottom: 0f, right: 0f), ALEdge.Left);
        this.feedHolder.AutoPinEdge(ALEdge.Left, ALEdge.Right, this.iconHolder);

        this.title.AutoPinEdge(ALEdge.Top, ALEdge.Top, this.feedHolder, 5f);
        this.title.AutoPinEdge(ALEdge.Right, ALEdge.Right, this.feedHolder, -15f);
        this.title.AutoPinEdge(ALEdge.Left, ALEdge.Left, this.feedHolder, 10f);

        this.description.AutoPinEdge(ALEdge.Top, ALEdge.Bottom, this.title, 0);
        this.description.AutoPinEdge(ALEdge.Right, ALEdge.Right, this.title);
        this.description.AutoPinEdge(ALEdge.Left, ALEdge.Left, this.title);

        this.author.AutoPinEdge(ALEdge.Bottom, ALEdge.Bottom, this.ContentView, -5);
        this.author.AutoPinEdge(ALEdge.Left, ALEdge.Left, this.title);
        this.author.AutoPinEdge(ALEdge.Right, ALEdge.Left, this.releaseDate);

        this.releaseDate.AutoPinEdge(ALEdge.Bottom, ALEdge.Bottom, this.ContentView,　-5f);
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

        this.releaseDate.Text = item.PublishingDate?.ToShortDateString();

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