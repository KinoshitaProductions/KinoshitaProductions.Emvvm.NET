#if __ANDROID__
#pragma warning disable S2436
using Android.Content;
using Android.Views;
using System.ComponentModel;
using AndroidX.RecyclerView.Widget;

namespace KinoshitaProductions.Emvvm.Bindings
{
    // ReSharper disable once UnusedType.Global
    public abstract class RecyclerViewItemBinding<TViewModel, TViewModelBinder, TCollectionBinder, TItem, TRootView> : RecyclerView.ViewHolder, IUIBindingV2<TItem>
        where TViewModel : ObservableViewModel
        where TViewModelBinder : class, IViewModelBinder<TViewModel>
        where TCollectionBinder :  class, ICollectionBinder<TViewModel, TViewModelBinder, RecyclerView.Adapter, TItem>
        where TItem : ObservableObject
        where TRootView : View
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
        // ReSharper disable once MemberCanBePrivate.Global
        protected TRootView? RootLayout;

        protected RecyclerViewItemBinding(Activity activity, Context context, TViewModel viewModel, TViewModelBinder viewModelBinder, TCollectionBinder collectionBinder, TRootView rootLayout) : base(rootLayout)
        {
            this.Activity = activity;
            this.Context = context;
            this.ViewModel = viewModel;
            this.RootLayout = rootLayout;
            this.ViewModelBinder = viewModelBinder;
            this.CollectionBinder = collectionBinder;
        }

        protected RecyclerViewItemBinding(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }
        ~RecyclerViewItemBinding()
        {
            Dispose(false);
        }

        public TItem? BoundItem { get; private set; }
        public bool IsBound => BoundItem != null;
        private bool _awaitingForRebind;
        public void BindTo(TItem bindItem)
        {
            // check if was already bound
            bool wasAlreadyBound = false;
            // if there was already an item, unbind events on old item
            if (BoundItem != null)
            {
                wasAlreadyBound = true;
                BoundItem.PropertyChanged -= MarshallItem_PropertyChanged;
            }

            // bind item
            BoundItem = bindItem;
            // bind resources
            DoBind(wasAlreadyBound || _awaitingForRebind);
            // bind events
            BoundItem.PropertyChanged += MarshallItem_PropertyChanged;
            // notify already rebound (to allow rebinding UI events)
            _awaitingForRebind = false;
        }
        // ReSharper disable once MemberCanBePrivate.Global
        public void Unbind(bool willRebind)
        {
            if (!IsBound)
                return;
            // unbind events
            if (BoundItem != null)
                BoundItem.PropertyChanged -= MarshallItem_PropertyChanged;
            // unbind resources
            DoUnbind(willRebind);
            // unbind item
            BoundItem = null;
            // store if will rebind (to avoid rebinding UI events)
            _awaitingForRebind = willRebind;
        }
        protected abstract void DoBind(bool wasAlreadyBound);
        protected abstract void DoUnbind(bool willRebind);
        public bool CanBeUsed => BoundItem != null && RootLayout != null;
        public bool NeedsView => (BoundItem != null) && (RootLayout == null);
        
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Unbind(willRebind: false);

            this.Activity = null;
            this.Context = null;
            this.ViewModel = null;
            this.ViewModelBinder = null;
            this.CollectionBinder = null;
            this.BoundItem = null;
            this.RootLayout = null;
        }
        
        // ReSharper disable once MemberCanBePrivate.Global
        protected bool IsInvalidBinding(object? sender) => !IsBound || !CanBeUsed || sender != BoundItem;
        public void ForcePropertyChanged(PropertyChangedEventArgs[] propertiesChanged, bool marshall = false) => Item_PropertyChanged(this.BoundItem, propertiesChanged, marshall);
        public void ForcePropertyChanged(PropertyChangedEventArgs propertyChanged, bool marshall = false) => Item_PropertyChanged(this.BoundItem, propertyChanged, marshall);
        // ReSharper disable once MemberCanBePrivate.Global
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
                        DoItem_PropertyChanged(sender, e);
                }, Activity);
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
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
        // ReSharper disable once MemberCanBePrivate.Global
        protected void MarshallItem_PropertyChanged(object? sender, PropertyChangedEventArgs propertyChanged) => Item_PropertyChanged(sender, propertyChanged, marshall: true);
    }
}
#endif