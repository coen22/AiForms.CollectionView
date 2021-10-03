using System;
using AiForms.Renderers;
using AndroidX.RecyclerView.Widget;

namespace AiForms.Renderers.Droid
{
    public class CollectionViewScrollListener:RecyclerView.OnScrollListener
    {
        public bool IsReachedBottom { get; set; }

        AiCollectionView _aiCollectionView;


        public CollectionViewScrollListener(AiCollectionView aiCollectionView)
        {
            _aiCollectionView = aiCollectionView;
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                _aiCollectionView = null;
            }
            base.Dispose(disposing);
        }

        public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
        {
            base.OnScrolled(recyclerView, dx, dy);

            if(dx < 0 || dy < 0 || IsReachedBottom || _aiCollectionView.LoadMoreCommand == null)
            {
                return;
            }

            var layoutManager = recyclerView.GetLayoutManager() as LinearLayoutManager;

            var visibleItemCount = recyclerView.ChildCount;
            var totalItemCount = layoutManager.ItemCount;
            var firstVisibleItem = layoutManager.FindFirstVisibleItemPosition();

            if(totalItemCount - visibleItemCount - _aiCollectionView.LoadMoreMargin <= firstVisibleItem)
            {
                IsReachedBottom = true;
                _aiCollectionView.LoadMoreCommand?.Execute(null);
            }
        }
    }
}
