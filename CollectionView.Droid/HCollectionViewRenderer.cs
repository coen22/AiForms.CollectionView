using System.ComponentModel;
using AiForms.Renderers;
using AiForms.Renderers.Droid;
using Android.Content;
using Android.Graphics;
using AndroidX.AppCompat.View;
using AndroidX.RecyclerView.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(HAiCollectionView), typeof(HCollectionViewRenderer))]
namespace AiForms.Renderers.Droid
{
    [Android.Runtime.Preserve(AllMembers = true)]
    public class HCollectionViewRenderer : CollectionViewRenderer, ICollectionViewRenderer
    {
        HSpacingDecoration _itemDecoration;
        HAiCollectionView HAiCollectionView => Element as HAiCollectionView;
        HCollectionViewAdapter _hAdapter => Adapter as HCollectionViewAdapter;
        int _spacing;
        bool _disposed;
        int _firstSpacing;
        int _lastSpacing;

        public HCollectionViewRenderer(Context context) : base(context)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                RecyclerView?.StopScroll();
                RecyclerView?.SetAdapter(null);
                RecyclerView?.RemoveItemDecoration(_itemDecoration);

                _itemDecoration?.Dispose();
                _itemDecoration = null;
            }
            _disposed = true;
            base.Dispose(disposing);
        }

        protected override void OnElementChanged(ElementChangedEventArgs<AiCollectionView> e)
        {
            if (e.NewElement != null)
            {
                if (Control == null)
                {
                    RecyclerView = new RecyclerView(new ContextThemeWrapper(Context, Resource.Style.scrollViewScrollBars), null, Resource.Attribute.collectionViewStyle);
                    LayoutManager = new LinearLayoutManager(Context);
                    LayoutManager.Orientation = LinearLayoutManager.Horizontal;


                    SetNativeControl(RecyclerView);

                    RecyclerView.Focusable = false;
                    RecyclerView.DescendantFocusability = Android.Views.DescendantFocusability.AfterDescendants;
                    RecyclerView.SetClipToPadding(false);
                    RecyclerView.HorizontalScrollBarEnabled = false;

                    _itemDecoration = new HSpacingDecoration(this);
                    RecyclerView.AddItemDecoration(_itemDecoration);

                    Adapter = new HCollectionViewAdapter(Context, e.NewElement, RecyclerView, this);
                    RecyclerView.SetAdapter(Adapter);

                    RecyclerView.SetLayoutManager(LayoutManager);

                    UpdateIsInfinite();
                    UpdateSpacing();
                }
            }

            base.OnElementChanged(e);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (e.PropertyName == HAiCollectionView.ColumnWidthProperty.PropertyName ||
               e.PropertyName == VisualElement.HeightRequestProperty.PropertyName)
            {
                UpdateCellSize();
                RefreshAll();
            }
            else if (e.PropertyName == HAiCollectionView.GroupHeaderWidthProperty.PropertyName)
            {
                UpdateGroupHeaderWidth();
                RefreshAll();
            }
            else if (e.PropertyName == HAiCollectionView.SpacingProperty.PropertyName ||
                     e.PropertyName == AiCollectionView.GroupFirstSpacingProperty.PropertyName ||
                     e.PropertyName == AiCollectionView.GroupLastSpacingProperty.PropertyName)
            {
                UpdateSpacing();
                RefreshAll();
            }
            else if (e.PropertyName == HAiCollectionView.IsInfiniteProperty.PropertyName)
            {
                RefreshAll();
                UpdateIsInfinite();
            }
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            if (changed)
            {
                UpdateCellSize();
                UpdateGroupHeaderWidth();
                RefreshAll();
            }

            base.OnLayout(changed, l, t, r, b);
        }

        protected virtual void RefreshAll()
        {
            RecyclerView.RemoveItemDecoration(_itemDecoration);

            Adapter.OnDataChanged();
            RecyclerView.AddItemDecoration(_itemDecoration);
            RequestLayout();
            Invalidate();
        }

        protected override void ExecuteScroll(int targetPosition, ScrollToRequestedEventArgs eventArgs)
        {
            if (HAiCollectionView.IsInfinite)
            {
                int fixPosition = _hAdapter.GetInitialPosition();
                if (targetPosition == Adapter.ItemCount - 1)
                {
                    fixPosition += Adapter.RealItemCount - 1;
                }
                else
                {
                    fixPosition += targetPosition;
                }
                base.ExecuteScroll(fixPosition, eventArgs);
                return;
            }
            base.ExecuteScroll(targetPosition, eventArgs);
        }

        protected virtual void UpdateIsInfinite()
        {
            if (HAiCollectionView.IsInfinite)
            {
                LayoutManager.ScrollToPositionWithOffset(_hAdapter.GetInitialPosition(), 0);
            }
        }

        protected virtual void UpdateCellSize()
        {
            if (Element.Height < 0)
            {
                return;
            }
            var height = Element.HeightRequest >= 0 ? Element.HeightRequest : Element.Height;
            CellWidth = (int)Context.ToPixels(HAiCollectionView.ColumnWidth);
            CellHeight = (int)Context.ToPixels(height);
        }

        protected virtual void UpdateSpacing()
        {
            _spacing = (int)Context.ToPixels(HAiCollectionView.Spacing);
            _firstSpacing = (int)Context.ToPixels(HAiCollectionView.GroupFirstSpacing);
            _lastSpacing = (int)Context.ToPixels(HAiCollectionView.GroupLastSpacing);
        }

        protected virtual void UpdateGroupHeaderWidth()
        {
            if (HAiCollectionView.IsGroupingEnabled)
            {
                GroupHeaderWidth = (int)Context.ToPixels(HAiCollectionView.GroupHeaderWidth);
                GroupHeaderHeight = (int)Context.ToPixels(Element.Height);
            }
        }

        public class HSpacingDecoration : RecyclerView.ItemDecoration
        {
            HCollectionViewRenderer _renderer;

            public HSpacingDecoration(HCollectionViewRenderer renderer)
            {
                _renderer = renderer;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _renderer = null;
                }
                base.Dispose(disposing);
            }

            public override void GetItemOffsets(Android.Graphics.Rect outRect, Android.Views.View view, RecyclerView parent, RecyclerView.State state)
            {
                var holder = parent.GetChildViewHolder(view) as ContentViewHolder;
                var position = parent.GetChildAdapterPosition(view);
                var realPosition = position;
                if (_renderer.HAiCollectionView.IsInfinite)
                {
                    realPosition = _renderer.Adapter.GetRealPosition(position);
                }

                if(holder.IsHeader)
                {
                    outRect.Right = _renderer._firstSpacing;
                    if (position != 0)
                    {
                        outRect.Left = _renderer._lastSpacing;
                    }

                    return;
                }

                // Disabled grouping first spacing is applied at the first cell.
                if (!_renderer.HAiCollectionView.IsGroupingEnabled && position == 0)                  
                {
                    outRect.Left = _renderer._firstSpacing;            
                }

                // Group last or single last spacing is applied at the last cell.
                if (position == _renderer.Adapter.ItemCount - 1)
                {
                    outRect.Right = _renderer._lastSpacing;
                }

                if (position == 0 || _renderer.Adapter.FirstSectionItems.Contains(realPosition))
                {
                    return;
                }

                outRect.Left = _renderer._spacing;
            }
        }

    }
}
