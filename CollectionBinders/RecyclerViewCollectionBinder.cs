#if __ANDROID__
#pragma warning disable S2436
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Android.Content;
using AndroidX.RecyclerView.Widget;
using Serilog;

namespace KinoshitaProductions.Emvvm.CollectionBinders
{
    // it is different since we may need to extend an Adapter in Android
    public abstract class RecyclerViewCollectionBinder<TViewModel, TViewModelBinder, TItem, TBinding> : ICollectionBinder<TViewModel, TViewModelBinder, RecyclerView.Adapter, TItem>, IDisposable 
        where TViewModel : ObservableViewModel 
        where TViewModelBinder : class, IViewModelBinder<TViewModel>
        where TItem : ObservableObject
        where TBinding : class, IUIBindingV2<TItem>
    {

        // ReSharper disable once MemberCanBePrivate.Global
        public Activity? Activity { get; set; }
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public Context? Context { get; set; }
        // ReSharper disable once MemberCanBePrivate.Global
        public TViewModel? ViewModel { get; set; }
        // ReSharper disable once MemberCanBePrivate.Global
        public TViewModelBinder? ViewModelBinder { get; set; }
        // ReSharper disable once MemberCanBePrivate.Global
        public RecyclerView.Adapter? Adapter { get; set; }
        // ReSharper disable once MemberCanBePrivate.Global
        public ObservableCollection<TItem>? Items { get; set; }
        // ReSharper disable once MemberCanBePrivate.Global
        public List<TBinding> Bindings { get; } = new ();

        // ReSharper disable once MemberCanBePrivate.Global
        public bool IsBound { get; private set; }
        // ReSharper disable once MemberCanBePrivate.Global
        public bool IsDisposing { get; private set; }

        ~RecyclerViewCollectionBinder()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            this.IsDisposing = true;
            Unbind();
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void BindTo(Activity activity, Context context, TViewModel viewModel, TViewModelBinder viewModelBinder, RecyclerView.Adapter adapter, ObservableCollection<TItem> items)
        {
            if (IsBound)
                Unbind(); // Should never happen, literally, why would we rebind something already bound?
            
            this.Activity = activity;
            this.Context = context;
            this.ViewModel = viewModel;
            this.ViewModelBinder = viewModelBinder;
            this.Adapter = adapter;
            this.Items = items;

            IsBound = true;

            // load items
            if (this.Items != null)
            {
                this.Items_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items));
                this.Items.CollectionChanged += this.MarshallItems_CollectionChanged;
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public void Unbind()
        {
            if (!IsBound)
                return; // huh? was not bound???
            
            // remove items
            if (this.Items != null)
            {
                this.Items.CollectionChanged -= MarshallItems_CollectionChanged;

                // this will dispose bindings
                this.Items_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            this.Activity = null;
            this.Context = null;
            this.ViewModel = null;
            this.ViewModelBinder = null;
            this.Adapter = null;
            this.Items = null;

            IsBound = false;
        }

        private void MarshallItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            CheckIsBoundAndMarshall(() =>
            {
                Items_CollectionChanged(sender, e);
            });
        }


        // ReSharper disable twice UnusedParameter.Local
        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!IsBound || IsDisposing)
                return; // don't do anything more since it is not bound

            //IMPORTANT: Add try catch here, since it will crash the app if had already been disposed when this was called!! And it happens, sometimes
            try
            {
                // should implement lifecycle notifying changes and such in the future, but this is a simple solution for now
                Adapter?.NotifyDataSetChanged();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to notify data set changed for Adapter");
            }
        }
        
        // ReSharper disable once MemberCanBePrivate.Global
        public void CheckIsBoundAndMarshall(Action action)
        {
            if (!this.IsBound || Activity == null)
                return; // disposed somewhere on time
            
            if (Android.OS.Looper.MainLooper?.Thread == Java.Lang.Thread.CurrentThread())
            {
                action.Invoke(); // we are in main thread, invoke it
            }
            else
            {
                var initiallyBoundViewModel = this.ViewModel;
                var initiallyBoundViewModelBinder = this.ViewModelBinder;
                var initiallyBoundItems = this.Items;

                // we're not in main thread, marshall it
                Marshaller.MarshalTask(() =>
                {
                    if (!this.IsBound || this.ViewModel != initiallyBoundViewModel ||
                        this.ViewModelBinder != initiallyBoundViewModelBinder || this.Items != initiallyBoundItems)
                        return; // disposed/unbound somewhere on time
                    action.Invoke();
                }, Activity);
            }
        }
    }
}
#endif
