using System.Collections.Specialized;
using System.ComponentModel;
using AiForms.Renderers;
using AiForms.Renderers.iOS;
using AiForms.Renderers.iOS.Cells;
using CoreGraphics;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(HAiCollectionView), typeof(HCollectionViewRenderer))]
namespace AiForms.Renderers.iOS
{
    [Foundation.Preserve(AllMembers = true)]
    public class HCollectionViewRenderer : CollectionViewRenderer
    {
        UICollectionView _collectionView;
        CGRect _previousFrame = CGRect.Empty;
        bool _disposed;
        HAiCollectionView HAiCollectionView => Element as HAiCollectionView;
        float _firstSpacing => (float)HAiCollectionView.GroupFirstSpacing;
        float _lastSpacing => (float)HAiCollectionView.GroupLastSpacing;

        protected override void OnElementChanged(ElementChangedEventArgs<AiCollectionView> e)
        {
            if (e.NewElement != null)
            {
                ViewLayout = new UICollectionViewFlowLayout();
                ViewLayout.ScrollDirection = UICollectionViewScrollDirection.Horizontal;
                ViewLayout.SectionInset = new UIEdgeInsets(0, 0, 0, 0);
                ViewLayout.MinimumLineSpacing = 0.0f;
                ViewLayout.MinimumInteritemSpacing = 0.0f;

                _collectionView = new UICollectionView(CGRect.Empty, ViewLayout);
                _collectionView.RegisterClassForCell(typeof(ContentCellContainer), typeof(ContentCell).FullName);
                _collectionView.RegisterClassForSupplementaryView(typeof(ContentCellContainer), UICollectionElementKindSection.Header, SectionHeaderId);

                _collectionView.ShowsHorizontalScrollIndicator = false;
                _collectionView.AllowsSelection = true;

                SetNativeControl(_collectionView);


                DataSource = new HCollectionViewSource(e.NewElement, _collectionView);
                _collectionView.Source = DataSource;

                UpdateSpacing();
            }

            base.OnElementChanged(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                DataSource?.Dispose();
                DataSource = null;

                _collectionView?.Dispose();
                _collectionView = null;
            }
            _disposed = true;
            base.Dispose(disposing);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            if (_previousFrame != Frame)
            {
                UpdateCellSize();
                UpdateGroupHeaderWidth();
                ViewLayout.InvalidateLayout();
            }
            _previousFrame = Frame;
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == HAiCollectionView.ColumnWidthProperty.PropertyName ||
               e.PropertyName == VisualElement.HeightRequestProperty.PropertyName)
            {
                UpdateCellSize();
                ViewLayout.InvalidateLayout();
            }
            else if (e.PropertyName == HAiCollectionView.GroupHeaderWidthProperty.PropertyName)
            {
                UpdateGroupHeaderWidth();
                ViewLayout.InvalidateLayout();
            }
            else if (e.PropertyName == HAiCollectionView.SpacingProperty.PropertyName ||
                     e.PropertyName == AiCollectionView.GroupFirstSpacingProperty.PropertyName ||
                     e.PropertyName == AiCollectionView.GroupLastSpacingProperty.PropertyName)
            {
                UpdateSpacing();
                ViewLayout.InvalidateLayout();
            }
            else if (e.PropertyName == HAiCollectionView.IsInfiniteProperty.PropertyName)
            {
                _collectionView.ReloadData();
                ViewLayout.InvalidateLayout();
            }
        }

        protected override UICollectionViewScrollPosition GetScrollPosition(ScrollToPosition position)
        {
            switch (position)
            {
                case ScrollToPosition.Center:
                    return UICollectionViewScrollPosition.CenteredHorizontally;
                case ScrollToPosition.End:
                    return UICollectionViewScrollPosition.Right;
                case ScrollToPosition.Start:
                    return UICollectionViewScrollPosition.Left;
                case ScrollToPosition.MakeVisible:
                default:
                    return UICollectionViewScrollPosition.None;
            }
        }

        protected override void OnGroupedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (HAiCollectionView.IsInfinite)
            {
                // If infinite, any events are dealt with as a reset event.
                UpdateItems(e, 0, true);
                return;
            }

            base.OnGroupedCollectionChanged(sender, e);
        }

        protected override void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (HAiCollectionView.IsInfinite)
            {
                UpdateItems(e, 0, true, true);
                return;
            }
            base.OnCollectionChanged(sender, e);
        }

        protected virtual void UpdateCellSize()
        {
            if (Element.Height < 0)
            {
                return;
            }

            var height = Element.HeightRequest >= 0 ? Element.HeightRequest : Bounds.Height;
            DataSource.CellSize = new CGSize((float)HAiCollectionView.ColumnWidth, (float)height);
            DataSource.LoadMoreMargin = Element.LoadMoreMargin * (float)DataSource.CellSize.Width;
        }

        protected virtual void UpdateSpacing()
        {
            ViewLayout.MinimumLineSpacing = (System.nfloat)HAiCollectionView.Spacing;

            if(HAiCollectionView.IsInfinite && !HAiCollectionView.IsGroupingEnabled)
            {
                return;
            }

            ViewLayout.SectionInset = new UIEdgeInsets(0, _firstSpacing, 0, _lastSpacing);
        }

        protected virtual void UpdateGroupHeaderWidth()
        {
            if (HAiCollectionView.IsGroupingEnabled)
            {
                ViewLayout.HeaderReferenceSize = new CGSize(HAiCollectionView.GroupHeaderWidth, Bounds.Height);
            }
        }

    }
}
