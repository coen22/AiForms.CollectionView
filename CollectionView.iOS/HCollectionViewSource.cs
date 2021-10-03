using System;
using UIKit;
using CoreGraphics;
using Foundation;
using Xamarin.Forms.Internals;
using Xamarin.Forms;
using AiForms.Renderers.iOS.Cells;
using System.Collections;

namespace AiForms.Renderers.iOS
{
    [Foundation.Preserve(AllMembers = true)]
    public class HCollectionViewSource : CollectionViewSource
    {
        HAiCollectionView HAiCollectionView => AiCollectionView as HAiCollectionView;
        int _infiniteMultiple = 3;
        nfloat _visibleContentWidth = 0f;

        public HCollectionViewSource(AiCollectionView aiCollectionView, UICollectionView uICollectionView) : base(aiCollectionView, uICollectionView)
        {
        }

        public override nint NumberOfSections(UICollectionView collectionView)
        {
            if (!HAiCollectionView.IsInfinite)
            {
                return base.NumberOfSections(collectionView);
            }

            if (AiCollectionView.IsGroupingEnabled)
            {
                return TemplatedItemsView.TemplatedItems.Count * _infiniteMultiple;
            }
            return 1;
        }

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
        {
            if (!HAiCollectionView.IsInfinite)
            {
                return base.GetItemsCount(collectionView, section);
            }

            if (!AiCollectionView.IsGroupingEnabled)
            {
                return TemplatedItemsView.TemplatedItems.Count * _infiniteMultiple;
            }

            var realSec = section % TemplatedItemsView.TemplatedItems.Count;
            return base.GetItemsCount(collectionView, realSec);
        }

        public override void Scrolled(UIScrollView scrollView)
        {
            base.Scrolled(scrollView);

            if (HAiCollectionView.IsInfinite)
            {
                _visibleContentWidth = scrollView.ContentSize.Width / _infiniteMultiple;

                if (scrollView.ContentOffset.X <= 0f || scrollView.ContentOffset.X > _visibleContentWidth * 2f)
                {
                    scrollView.ContentOffset = new CGPoint(_visibleContentWidth, scrollView.ContentOffset.Y);
                }
                return;
            }

            if (IsReachedBottom || AiCollectionView.LoadMoreCommand == null)
            {
                return;
            }

            if (scrollView.ContentSize.Width <= scrollView.ContentOffset.X + scrollView.Bounds.Width + LoadMoreMargin)
            {
                RaiseReachedBottom();
            }
        }

        public override CGSize GetSizeForItem(UICollectionView collectionView, UICollectionViewLayout layout, NSIndexPath indexPath)
        {
            return base.GetSizeForItem(collectionView, layout, GetRealIndexPath(indexPath));
        }


        protected override NSIndexPath GetRealIndexPath(NSIndexPath indexPath)
        {
            if (!HAiCollectionView.IsInfinite)
            {
                return indexPath;
            }

            int sec = 0;
            int row = indexPath.Row;

            if (HAiCollectionView.IsGroupingEnabled)
            {
                sec = indexPath.Section % TemplatedItemsView.TemplatedItems.Count;
            }
            else
            {
                row = indexPath.Row % TemplatedItemsView.TemplatedItems.Count;
            }

            return NSIndexPath.Create(sec, row);
        }
    }
}
