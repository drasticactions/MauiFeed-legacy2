using MauiFeed.Models;
using ObjCRuntime;
using UIKit;

namespace MauiFeed.Apple.Views;

public sealed class SidebarViewController : UIViewController, IUICollectionViewDelegate
{
    private UICollectionView collectionView;

    private UICollectionViewDiffableDataSource<NSString, SidebarItem>? dataSource;

    public SidebarViewController()
    {
        this.collectionView = new UICollectionView(this.View!.Bounds, this.CreateLayout());
        this.collectionView.Delegate = this;

        this.View.AddSubview(this.collectionView);

        // Anchor collectionView
        this.collectionView.TranslatesAutoresizingMaskIntoConstraints = false;

        // Create constraints to pin the edges of myView to its superview's edges
        NSLayoutConstraint.ActivateConstraints(new[]
        {
            this.collectionView.TopAnchor.ConstraintEqualTo(this.View.TopAnchor),
            this.collectionView.BottomAnchor.ConstraintEqualTo(this.View.BottomAnchor),
            this.collectionView.LeadingAnchor.ConstraintEqualTo(this.View.LeadingAnchor),
            this.collectionView.TrailingAnchor.ConstraintEqualTo(this.View.TrailingAnchor),
        });
    }

    private void ConfigureDataSource()
    {
        var headerRegistration = UICollectionViewCellRegistration.GetRegistration(
            typeof(UICollectionViewListCell),
            new UICollectionViewCellRegistrationConfigurationHandler((cell, indexpath, item) =>
            {
                var sidebarItem = (SidebarItem)item;
                var contentConfiguration = UIListContentConfiguration.SidebarHeaderConfiguration;
                contentConfiguration.Text = sidebarItem.Title;
                contentConfiguration.TextProperties.Font = UIFont.PreferredSubheadline;
                contentConfiguration.TextProperties.Color = UIColor.SecondaryLabel;
                cell.ContentConfiguration = contentConfiguration;
                ((UICollectionViewListCell)cell).Accessories = new[] { new UICellAccessoryOutlineDisclosure() };
            }));

        var rowRegistration = UICollectionViewCellRegistration.GetRegistration(typeof(CustomCell),
            new UICollectionViewCellRegistrationConfigurationHandler((cell, indexpath, item) =>
            {
                var sidebarItem = item as SidebarItem;
                if (sidebarItem is null)
                {
                    return;
                }

#if TVOS
                    var cfg = UIListContentConfiguration.CellConfiguration;
#else
                var cfg = UIListContentConfiguration.SidebarCellConfiguration;
#endif
                cfg.Text = sidebarItem.Title;
                // if (sidebarItem.SystemIcon is not null)
                // {
                //     cfg.Image = UIImage.GetSystemImage(sidebarItem.SystemIcon);
                // }

                cell.ContentConfiguration = cfg;
            })
        );

        if (this.collectionView is null)
        {
            throw new NullReferenceException(nameof(this.collectionView));
        }

        this.dataSource = new UICollectionViewDiffableDataSource<NSString, SidebarItem>(this.collectionView,
            new UICollectionViewDiffableDataSourceCellProvider((collectionView, indexPath, item) =>
            {
                var sidebarItem = item as SidebarItem;
                if (sidebarItem is null || collectionView is null)
                {
                    throw new Exception();
                }

                if (sidebarItem.SidebarItemType == SidebarItemType.Folder)
                {
                    return collectionView.DequeueConfiguredReusableCell(headerRegistration, indexPath, item);
                }
                else
                {
                    return collectionView.DequeueConfiguredReusableCell(rowRegistration, indexPath, item);
                }
            })
        );
    }

    private UICollectionViewLayout CreateLayout()
    {
        return new UICollectionViewCompositionalLayout((sectionIndex, layoutEnvironment) =>
        {
            var configuration = new UICollectionLayoutListConfiguration(UICollectionLayoutListAppearance.Sidebar);
            configuration.ShowsSeparators = false;
            configuration.HeaderMode = UICollectionLayoutListHeaderMode.None;
            return NSCollectionLayoutSection.GetSection(configuration, layoutEnvironment);
        });
    }

    private class CustomCell : UICollectionViewListCell
    {
        public CustomCell()
        {
        }

        public CustomCell(NSCoder coder)
            : base(coder)
        {
        }

        public CustomCell(CGRect frame)
            : base(frame)
        {
        }

        protected CustomCell(NSObjectFlag t)
            : base(t)
        {
        }

        protected internal CustomCell(NativeHandle handle)
            : base(handle)
        {
        }
    }
}