#if __ANDROID__
#pragma warning disable S2436
using System.Collections.ObjectModel;
using System.ComponentModel;

using Android.Content;

namespace KinoshitaProductions.Emvvm.CollectionBinders
{
    // it is different since we may need to extend an Adapter in Android
    // ReSharper disable once UnusedType.Global
    public abstract class SimpleCollectionBinder<TViewModel, TViewModelBinder, TItem, TBinding> : ICollectionBinder<TViewModel, TViewModelBinder, TItem>, IDisposable
        where TViewModel : ObservableViewModel 
        where TViewModelBinder : class, IViewModelBinder<TViewModel>  
        where TItem : ObservableObject
        where TBinding : class, IUIBinding
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
        public ObservableCollection<TItem>? Items { get; set; }
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once CollectionNeverUpdated.Global
        public List<TBinding> Bindings { get; } = new ();

        public bool IsBound { get; private set; }
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool IsDisposing { get; private set; }

        ~SimpleCollectionBinder()
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

        public void BindTo(Activity activity, Context context, TViewModel viewModel, TViewModelBinder viewModelBinder, ObservableCollection<TItem> items)
        {
            if (IsBound)
                Unbind(); // Should never happen, literally, why would we rebind something already bound?
            
            this.Activity = activity;
            this.Context = context;
            this.ViewModel = viewModel;
            this.ViewModelBinder = viewModelBinder;
            this.Items = items;
            
            IsBound = true;
            
            // load items
            if (this.Items != null)
            {
                this.MarshallItems_CollectionChanged(this, new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Add, items));
                this.Items.CollectionChanged += this.MarshallItems_CollectionChanged;
            }
        }

        public void Unbind()
        {
            if (!IsBound)
                return; // huh? was not bound???
            
            // remove items
            if (this.Items != null)
            {
                this.Items.CollectionChanged -= MarshallItems_CollectionChanged;

                // this will dispose bindings
                this.MarshallItems_CollectionChanged(this, new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
            }
            
            IsBound = false;

            this.Activity = null;
            this.Context = null;
            this.ViewModel = null;
            this.ViewModelBinder = null;
            this.Items = null;
            }

        public void EnforceUpdateForItem(TBinding binding)
        {
            CheckIsBoundAndMarshall(() =>
            {
                DoEnforceUpdateForItem(binding);
            });
        }

        
        protected virtual void DoEnforceUpdateForItem(TBinding binding)
        {
        }
        
        protected virtual void DoBind()
        {
        }
        protected virtual void DoUnbind()
        {
        }

        private void MarshallItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            CheckIsBoundAndMarshall(() =>
            {
                Items_CollectionChanged(sender, e);
            });
        }


        protected virtual void Items_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public void CheckIsBoundAndMarshall(Action action)
        {
            if (!IsBound || Activity == null)
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

        private void ExecuteForItem(object sender, TBinding binding, Action<TBinding, TItem> action)
        {
            if (!this.IsBound || !binding.IsBound || !binding.OwnsObject(sender) || !binding.CanBeUsed()) return;
            action.Invoke(binding, (TItem)binding.BoundItem); // we are in main thread, invoke it
        }
        
        private void ExecuteForItem(object sender, TBinding binding, PropertyChangedEventArgs e, Action<TBinding, TItem, PropertyChangedEventArgs> action)
        {
            if (!this.IsBound || !binding.IsBound || !binding.OwnsObject(sender) || !binding.CanBeUsed()) return;
            action.Invoke(binding, (TItem)binding.BoundItem, e); // we are in main thread, invoke it
        }
        
        // ReSharper disable once UnusedMember.Global
        public void CheckIsBoundAndMarshallForItem(object sender, Action<TBinding, TItem> action)
        {
            if (!this.IsBound)
                return; // disposed somewhere on time

            var binding = Bindings.Find(x => x.OwnsObject(sender));
            if (binding == null) return;
            if (Android.OS.Looper.MainLooper?.Thread == Java.Lang.Thread.CurrentThread())
            {
                ExecuteForItem(sender, binding, action);
            }
            else
            {
                var initiallyBoundItem = binding.BoundItem;
                // we're not in main thread, marshall it
                Marshaller.MarshalTask(() =>
                {
                    if (initiallyBoundItem != binding.BoundItem) return; // disposed somewhere on time
                    ExecuteForItem(sender, binding, action);
                }, Activity);
            }
        }

        // ReSharper disable once UnusedMember.Global
        public void CheckIsBoundAndMarshallForItem(object sender, PropertyChangedEventArgs e, Action<TBinding, TItem, PropertyChangedEventArgs> action)
        {
            if (!this.IsBound)
                return; // disposed somewhere on time
            
            var binding = Bindings.Find(x => x.OwnsObject(sender));
            if (binding == null) return;
            if (Android.OS.Looper.MainLooper?.Thread == Java.Lang.Thread.CurrentThread())
            {
                ExecuteForItem(sender, binding, e, action); // we are in main thread, invoke it
            }
            else
            {
                var initiallyBoundItem = binding.BoundItem;
                // we're not in main thread, marshall it
                Marshaller.MarshalTask(() =>
                {
                    if (initiallyBoundItem != binding.BoundItem) return; // disposed somewhere on time
                    ExecuteForItem(sender, binding, e, action); // we are in main thread, invoke it
                }, Activity);
            }
        }
    }
}
#endif
