﻿using System.Collections.Specialized;
using Android.Content;
using AndroidX.RecyclerView.Widget;

namespace AiForms.Renderers.Droid
{
    [Android.Runtime.Preserve(AllMembers = true)]
    public class HCollectionViewAdapter : CollectionViewAdapter
    {
        public readonly int InfiniteCount = 50000;
        HAiCollectionView HAiCollectionView => AiCollectionView as HAiCollectionView;

        public HCollectionViewAdapter(Context context, AiCollectionView aiCollectionView, RecyclerView recyclerView, ICollectionViewRenderer renderer)
            : base(context, aiCollectionView, recyclerView, renderer)
        {
        }

        public override int ItemCount
        {
            get
            {
                if (HAiCollectionView.IsInfinite)
                {
                    if (_listCount == -1)
                    {
                        InvalidateCount();
                    }
                    return InfiniteCount;
                }
                return base.ItemCount;
            }
        }

        protected override void OnGroupedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (HAiCollectionView.IsInfinite)
            {
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


        public override int GetRealPosition(int position)
        {
            if (_listCount == 0)
            {
                return position;
            }
            return HAiCollectionView.IsInfinite ? position % _listCount : position;
        }

        public virtual int GetInitialPosition()
        {
            if (_listCount == -1)
            {
                InvalidateCount();
            }
            return InfiniteCount / 2 / _listCount * _listCount;
        }
    }
}
