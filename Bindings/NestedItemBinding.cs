#if __ANDROID__
#pragma warning disable S2326,S2436
using Android.Content;
using System.ComponentModel;
// ReSharper disable MemberCanBePrivate.Global

namespace KinoshitaProductions.Emvvm.Bindings
{
    // T is bound item type, U is binder real class
    // ReSharper disable once UnusedType.Global
    public abstract class NestedItemBinding<TViewModel, TViewModelBinder, TCollectionBinder, TItem> : IBindingV2<TItem>
        where TViewModel : ObservableViewModel
        where TViewModelBinder : class, IViewModelBinder<TViewModel>
        where TCollectionBinder : class
        where TItem : ObservableObject
    {
        // ReSharper disable once MemberCanBePrivate.Global
        protected Activity? Activity;
        // ReSharper disable once MemberCanBePrivate.Global
        protected Context? Context;
        // ReSharper disable once MemberCanBePrivate.Global
        protected TViewModel? ViewModel;
        // ReSharper disable once MemberCanBePrivate.Global
        protected TViewModelBinder? ViewModelBinder;
        // ReSharper disable once MemberCanBePrivate.Global
        protected TCollectionBinder? CollectionBinder;
        protected NestedItemBinding(Activity activity, Context context, TViewModel viewModel, TViewModelBinder viewModelBinder, TCollectionBinder collectionBinder)
        {
            this.Activity = activity;
            this.Context = context;
            this.ViewModel = viewModel;
            this.ViewModelBinder = viewModelBinder;
            this.CollectionBinder = collectionBinder;
        }
        ~NestedItemBinding()
        {
            Dispose(false);
        }

        public TItem? BoundItem { get; private set; }
        public bool IsBound => BoundItem != null;
        public void BindTo(TItem bindItem)
        {
            // if there was already an item, unbind events on old item
            if (BoundItem != null)
                BoundItem.PropertyChanged -= MarshallItem_PropertyChanged;
            // bind item
            BoundItem = bindItem;
            // bind resources
            DoBind();
            // bind events
            BoundItem.PropertyChanged += MarshallItem_PropertyChanged;
        }
        public void Unbind()
        {
            // unbind events
            if (BoundItem != null)
                BoundItem.PropertyChanged -= MarshallItem_PropertyChanged;
            // unbind resources
            DoUnbind();
            // unbind item
            BoundItem = null;
        }
        protected abstract void DoBind();
        protected abstract void DoUnbind();

        protected virtual void Dispose(bool disposing)
        {
            Unbind();

            this.Activity = null;
            this.Context = null;
            this.ViewModel = null;
            this.ViewModelBinder = null;
            this.CollectionBinder = null;
            this.BoundItem = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private bool IsInvalidBinding(object? sender) => !IsBound || sender != BoundItem;
        public void ForcePropertyChanged(PropertyChangedEventArgs[] propertiesChanged, bool marshall = false) => Item_PropertyChanged(this.BoundItem, propertiesChanged, marshall);
        public void ForcePropertyChanged(PropertyChangedEventArgs propertyChanged, bool marshall = false) => Item_PropertyChanged(this.BoundItem, propertyChanged, marshall);
        protected void MarshallItem_PropertyChanged(object? sender, PropertyChangedEventArgs propertyChanged) => Item_PropertyChanged(sender, propertyChanged, marshall: true);
        protected void Item_PropertyChanged(object? sender, PropertyChangedEventArgs[] propertiesChanged, bool marshall = true)
        {
            if (IsInvalidBinding(sender) || Activity == null)
                return;

            //update view
            if (!marshall || Android.OS.Looper.MainLooper?.Thread == Java.Lang.Thread.CurrentThread())
            {
                foreach (var e in propertiesChanged)
                {
                    DoItem_PropertyChanged(sender, e);
                }
            }
            else
            {
                Marshaller.MarshalTask(() =>
                {
                    if (IsInvalidBinding(sender))
                        return;
                    foreach (var e in propertiesChanged)
                    {
                        DoItem_PropertyChanged(sender, e);
                    }

                }, Activity);
            }
        }
        protected void Item_PropertyChanged(object? sender, PropertyChangedEventArgs propertyChanged, bool marshall = true)
        {
            if (IsInvalidBinding(sender) || Activity == null)
                return;

            //update view
            if (!marshall || Android.OS.Looper.MainLooper?.Thread == Java.Lang.Thread.CurrentThread())
            {
                DoItem_PropertyChanged(sender, propertyChanged);
            }
            else
            {
                Marshaller.MarshalTask(() =>
                {
                    if (IsInvalidBinding(sender))
                        return;
                    DoItem_PropertyChanged(sender, propertyChanged);
                }, Activity);
            }
        }
        protected abstract void DoItem_PropertyChanged(object? sender, PropertyChangedEventArgs e);
    }
}
#endif