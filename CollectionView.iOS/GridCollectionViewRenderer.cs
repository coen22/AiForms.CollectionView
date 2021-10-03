using System;
using System.ComponentModel;
using AiForms.Renderers;
using AiForms.Renderers.iOS;
using AiForms.Renderers.iOS.Cells;
using CoreGraphics;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using RectangleF = CoreGraphics.CGRect;
using Foundation;
using System.Collections.Generic;
using CoreFoundation;
using System.Collections.Specialized;
using Xamarin.Forms.Internals;

[assembly: ExportRenderer(typeof(GridAiCollectionView), typeof(GridCollectionViewRenderer))]
namespace AiForms.Renderers.iOS
{
    [Foundation.Preserve(AllMembers = true)]
    public class GridCollectionViewRenderer : CollectionViewRenderer
    {
        UICollectionView _collectionView;
        UIRefreshControl _refreshControl;
        KeyboardInsetTracker _insetTracker;
        RectangleF _previousFrame;
        bool _disposed;
        GridAiCollectionView GridAiCollectionView => (GridAiCollectionView)Element;
        GridCollectionViewSource _gridSource => DataSource as GridCollectionViewSource;
        float _firstSpacing => (float)GridAiCollectionView.GroupFirstSpacing;
        float _lastSpacing => (float)GridAiCollectionView.GroupLastSpacing;
        bool _isRatioHeight => GridAiCollectionView.ColumnHeight <= 5.0;

        protected override void OnElementChanged(ElementChangedEventArgs<AiCollectionView> e)
        {
            if (e.NewElement != null)
            {
                ViewLayout = new GridViewLayout();
                ViewLayout.ScrollDirection = UICollectionViewScrollDirection.Vertical;
                ViewLayout.SectionInset = new UIEdgeInsets(0, 0, 0, 0);
                ViewLayout.MinimumLineSpacing = 0.0f;
                ViewLayout.MinimumInteritemSpacing = 0.0f;
                ViewLayout.EstimatedItemSize = UICollectionViewFlowLayout.AutomaticSize;

                _refreshControl = new UIRefreshControl();
                _refreshControl.ValueChanged += RefreshControl_ValueChanged;

                _collectionView = new UICollectionView(CGRect.Empty, ViewLayout);
                _collectionView.RegisterClassForCell(typeof(ContentCellContainer), typeof(ContentCell).FullName);
                _collectionView.RegisterClassForSupplementaryView(typeof(ContentCellContainer), UICollectionElementKindSection.Header, SectionHeaderId);

                _collectionView.AllowsSelection = true;

                SetNativeControl(_collectionView);

                _insetTracker = new KeyboardInsetTracker(_collectionView, () => Control.Window, insets => Control.ContentInset = Control.ScrollIndicatorInsets = insets, point =>
                {
                    var offset = Control.ContentOffset;
                    offset.Y += point.Y;
                    Control.SetContentOffset(offset, true);
                });


                DataSource = new GridCollectionViewSource(e.NewElement, _collectionView);
                _collectionView.Source = DataSource;

                UpdateIsSticky();
                UpdateRowSpacing();
                UpdatePullToRefreshEnabled();
                UpdatePullToRefreshColor();
            }

            base.OnElementChanged(e);
        }

        public override void LayoutSubviews()
        {
            if (_previousFrame != Frame)
            {
                _previousFrame = Frame;
                UpdateGridType();
                UpdateGroupHeaderHeight();
                _insetTracker?.UpdateInsets();
            }
            base.LayoutSubviews();
        }

        public override SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            return Control.GetSizeRequest(widthConstraint, heightConstraint, 50, 50);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _insetTracker?.Dispose();
                _insetTracker = null;

                DataSource?.Dispose();
                DataSource = null;

                if (_refreshControl != null)
                {
                    _refreshControl.ValueChanged -= RefreshControl_ValueChanged;
                    _refreshControl.Dispose();
                    _refreshControl = null;
                }

                _collectionView?.Dispose();
                _collectionView = null;
            }

            _disposed = true;

            base.Dispose(disposing);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (e.PropertyName == GridAiCollectionView.GroupHeaderHeightProperty.PropertyName)
            {
                UpdateGroupHeaderHeight();
                ViewLayout.InvalidateLayout();
            }
            else if (e.PropertyName == GridAiCollectionView.GridTypeProperty.PropertyName ||
                     e.PropertyName == GridAiCollectionView.PortraitColumnsProperty.PropertyName ||
                     e.PropertyName == GridAiCollectionView.LandscapeColumnsProperty.PropertyName ||
                     e.PropertyName == GridAiCollectionView.ColumnSpacingProperty.PropertyName ||
                     e.PropertyName == GridAiCollectionView.ColumnHeightProperty.PropertyName ||
                     e.PropertyName == GridAiCollectionView.SpacingTypeProperty.PropertyName ||
                     e.PropertyName == GridAiCollectionView.AdditionalHeightProperty.PropertyName ||
                     e.PropertyName == AiCollectionView.GroupFirstSpacingProperty.PropertyName ||
                     e.PropertyName == AiCollectionView.GroupLastSpacingProperty.PropertyName ||
                     e.PropertyName == GridAiCollectionView.BothSidesMarginProperty.PropertyName)
            {
                UpdateGridType();
                InvalidateLayout();
            }
            else if (e.PropertyName == GridAiCollectionView.RowSpacingProperty.PropertyName)
            {
                UpdateRowSpacing();
                ViewLayout.InvalidateLayout();
            }
            else if (e.PropertyName == GridAiCollectionView.ColumnWidthProperty.PropertyName)
            {
                if (GridAiCollectionView.GridType != GridType.UniformGrid)
                {
                    UpdateGridType();
                    InvalidateLayout();
                }
            }
            else if (e.PropertyName == ListView.IsPullToRefreshEnabledProperty.PropertyName)
            {
                UpdatePullToRefreshEnabled();
            }
            else if (e.PropertyName == GridAiCollectionView.PullToRefreshColorProperty.PropertyName)
            {
                UpdatePullToRefreshColor();
            }
            else if (e.PropertyName == Xamarin.Forms.ListView.IsRefreshingProperty.PropertyName)
            {
                UpdateIsRefreshing();
            }
            else if (e.PropertyName == GridAiCollectionView.IsGroupHeaderStickyProperty.PropertyName)
            {
                UpdateIsSticky();
                InvalidateLayout();
            }
        }

        protected override UICollectionViewScrollPosition GetScrollPosition(ScrollToPosition position)
        {
            switch (position)
            {
                case ScrollToPosition.Center:
                    return UICollectionViewScrollPosition.CenteredVertically;
                case ScrollToPosition.End:
                    return UICollectionViewScrollPosition.Bottom;
                case ScrollToPosition.Start:
                    return UICollectionViewScrollPosition.Top;
                case ScrollToPosition.MakeVisible:
                default:
                    return UICollectionViewScrollPosition.None;
            }
        }

        protected virtual void RefreshControl_ValueChanged(object sender, EventArgs e)
        {
            if (_refreshControl.Refreshing)
            {
                GridAiCollectionView.SendRefreshing();
            }
            GridAiCollectionView.IsRefreshing = _refreshControl.Refreshing;
        }

        protected virtual void UpdateIsRefreshing()
        {
            var refreshing = Element.IsRefreshing;
            if (GridAiCollectionView == null)
            {
                return;
            }
            if (refreshing)
            {
                if (!_refreshControl.Refreshing)
                {
                    _refreshControl.BeginRefreshing();
                }
            }
            else
            {
                _refreshControl.EndRefreshing();
            }

        }

        protected virtual void UpdatePullToRefreshColor()
        {
            if (!GridAiCollectionView.PullToRefreshColor.IsDefault)
            {
                _refreshControl.TintColor = GridAiCollectionView.PullToRefreshColor.ToUIColor();
            }
        }

        protected virtual void UpdatePullToRefreshEnabled()
        {
            _refreshControl.Enabled = Element.IsPullToRefreshEnabled && (Element as IListViewController).RefreshAllowed;
            if (_refreshControl.Enabled)
            {
                _collectionView.RefreshControl = _refreshControl;
            }
            else
            {
                _collectionView.RefreshControl = null;
            }
        }

        protected virtual void UpdateRowSpacing()
        {
            ViewLayout.MinimumLineSpacing = (System.nfloat)GridAiCollectionView.RowSpacing;
        }

        protected virtual void UpdateGroupHeaderHeight()
        {
            if (GridAiCollectionView.IsGroupingEnabled)
            {
                ViewLayout.HeaderReferenceSize = new CGSize(Bounds.Width, GridAiCollectionView.GroupHeaderHeight);
            }
        }

        protected virtual void UpdateIsSticky()
        {
            ViewLayout.SectionHeadersPinToVisibleBounds = GridAiCollectionView.IsGroupHeaderSticky;
        }

        protected virtual void UpdateGridType()
        {
            // Reset insets
            ViewLayout.SectionInset = new UIEdgeInsets(_firstSpacing, 0,_lastSpacing, 0); 
            ViewLayout.MinimumInteritemSpacing = 0;
            CGSize itemSize = CGSize.Empty;

            if (GridAiCollectionView.GridType == GridType.UniformGrid)
            {
                switch (UIApplication.SharedApplication.StatusBarOrientation)
                {
                    case UIInterfaceOrientation.Portrait:
                    case UIInterfaceOrientation.PortraitUpsideDown:
                    case UIInterfaceOrientation.Unknown:
                        itemSize = GetUniformItemSize(GridAiCollectionView.PortraitColumns);
                        DataSource.LoadMoreMargin = Element.LoadMoreMargin / GridAiCollectionView.PortraitColumns * (float)itemSize.Height;
                        break;
                    case UIInterfaceOrientation.LandscapeLeft:
                    case UIInterfaceOrientation.LandscapeRight:
                        itemSize = GetUniformItemSize(GridAiCollectionView.LandscapeColumns);
                        DataSource.LoadMoreMargin = Element.LoadMoreMargin / GridAiCollectionView.LandscapeColumns * (float)itemSize.Height;
                        break;
                }
                ViewLayout.MinimumInteritemSpacing = (System.nfloat)GridAiCollectionView.ColumnSpacing;
            }
            else
            {
                itemSize = GetAutoSpacingItemSize();
            }

            GridAiCollectionView.SetValue(GridAiCollectionView.ComputedWidthProperty, itemSize.Width);
            GridAiCollectionView.SetValue(GridAiCollectionView.ComputedHeightProperty, itemSize.Height);

            DataSource.CellSize = itemSize;
            ViewLayout.EstimatedItemSize = itemSize;
        }

        protected virtual double CalcurateColumnHeight(double itemWidth)
        {
            if (_isRatioHeight)
            {
                return itemWidth * GridAiCollectionView.ColumnHeight + GridAiCollectionView.AdditionalHeight;
            }

            return GridAiCollectionView.ColumnHeight + GridAiCollectionView.AdditionalHeight;
        }

        protected virtual CGSize GetUniformItemSize(int columns)
        {
            var margin = (float)GridAiCollectionView.BothSidesMargin;
            ViewLayout.SectionInset = new UIEdgeInsets(_firstSpacing,margin, _lastSpacing, margin);

            float width = (float)Frame.Width - margin * 2f - (float)GridAiCollectionView.ColumnSpacing * (float)(columns - 1.0f);

            _gridSource.SurplusPixel = (int)width % columns;

            var itemWidth = Math.Floor((float)(width / (float)columns));
            var itemHeight = CalcurateColumnHeight(itemWidth);

            return new CGSize(itemWidth, itemHeight);
        }

        protected virtual CGSize GetAutoSpacingItemSize()
        {
            var itemWidth = (float)Math.Min(Frame.Width, GridAiCollectionView.ColumnWidth);
            var itemHeight = CalcurateColumnHeight(itemWidth);
            if (GridAiCollectionView.SpacingType == SpacingType.Between)
            {
                DataSource.LoadMoreMargin = Element.LoadMoreMargin / ((float)Frame.Width / itemWidth) * (float)itemHeight;
                return new CGSize(itemWidth, itemHeight);
            }


            var leftSize = (float)Frame.Width;
            var spacing = (float)GridAiCollectionView.ColumnSpacing;
            int columnCount = 0;
            do
            {
                leftSize -= itemWidth;
                if (leftSize < 0)
                {
                    break;
                }
                columnCount++;
                if (leftSize - spacing < 0)
                {
                    break;
                }
                leftSize -= spacing;
            } while (true);

            DataSource.LoadMoreMargin = Element.LoadMoreMargin / (float)columnCount * (float)itemHeight;

            var contentWidth = itemWidth * columnCount + spacing * (columnCount - 1f);

            var insetSum = Frame.Width - contentWidth;
            var insetSurplus = (int)insetSum % 2;
            var inset = (float)Math.Floor(insetSum / 2.0f);

            ViewLayout.SectionInset = new UIEdgeInsets(_firstSpacing, inset + (float)insetSurplus, _lastSpacing, inset);

            return new CGSize(itemWidth, itemHeight);
        }

        void InvalidateLayout()
        {
            if (!GridAiCollectionView.IsGroupingEnabled)
            {
                ViewLayout.InvalidateLayout();
                return;
            }

            // HACK: When IsGroupingEnabled is true and changing such layout size as the item size and the spacing size,
            //       a header cell content is sometimes not reflected or broken layout.
            //       By reloading with a bit delay after scrolling to Top, this issue can be avoided.
            Control.SetContentOffset(CGPoint.Empty, false);
            DispatchQueue.MainQueue.DispatchAfter(new DispatchTime(DispatchTime.Now, TimeSpan.FromMilliseconds(150)), () =>
            {
                Control.ReloadData();
                ViewLayout.InvalidateLayout();
            });
        }
    }
}
