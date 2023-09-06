#if __ANDROID__
using System.ComponentModel;
using Android.Content;

namespace KinoshitaProductions.Emvvm.Binders
{
    // ReSharper disable once UnusedType.Global
    public abstract class SimpleViewModelBinder<TViewModel> : IViewModelBinder<TViewModel>, IDisposable where TViewModel : ObservableViewModel
    {
        // ReSharper disable once MemberCanBePrivate.Global
        protected Activity? Activity;
        // ReSharper disable once MemberCanBePrivate.Global
        protected Context? Context;
        // ReSharper disable once MemberCanBePrivate.Global
        protected TViewModel? ViewModel;
        // ReSharper disable once MemberCanBePrivate.Global
        public TViewModel? BoundItem => ViewModel;
        public bool IsBound { get; private set; }
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool IsDisposing { get; private set; }

        ~SimpleViewModelBinder()
        {
            Dispose(false); // calls Unbind
        }

        public void BindTo(Activity activity, Context context, TViewModel viewModel)
        {
            if (IsBound)
                Unbind(); // Should never happen, literally, why would we rebind something already bound?

            this.Activity = activity;
            this.Context = context;
            this.ViewModel = viewModel;
            if (activity.Resources?.Configuration != null)
                this.ViewModel.LastKnownOrientation =
                    ScreenHelper.ConvertToUniversalOrientation(activity.Resources.Configuration
                        .Orientation); // inform orientation

            DoBind();

            IsBound = true;

            EnforceUpdate();
        }

        public void Unbind()
        {
            if (!IsBound)
                return; // huh? it was already disposed

            DoUnbind();

            this.Activity = null;
            this.Context = null;
            this.ViewModel = null;

            IsBound = false;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public void EnforceUpdate()
        {
            if (!IsBound)
                return;

            DoEnforceUpdate();
        }

        protected virtual void DoBind()
        {
        }
        protected virtual void DoEnforceUpdate()
        {
        }
        protected virtual void DoUnbind()
        {
        }
        protected virtual void Dispose(bool isDisposing)
        {
            this.IsDisposing = true;

            if (IsBound)
                Unbind();
        }
        protected virtual void DoViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
        }

        public void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            CheckIsBoundAndMarshall(() =>
            {
                DoViewModel_PropertyChanged(sender, e);
            });
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        public void InvokeUpdate(Action<object?, PropertyChangedEventArgs> propertyChangedEventHandler, params string[] propertiesNames)
        {
            foreach (var propertyName in propertiesNames)
                propertyChangedEventHandler.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void MarshallUpdate(Action<object?, PropertyChangedEventArgs> propertyChangedEventHandler, params string[] propertiesNames)
        {
            CheckIsBoundAndMarshall(() =>
            {
                foreach (var propertyName in propertiesNames)
                    propertyChangedEventHandler.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
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
                // safety check, since many things can suddenly happen
                var initiallyBoundItem = this.BoundItem;

                // we're not in main thread, marshall it
                Marshaller.MarshalTask(() =>
                {
                    if (!this.IsBound || this.BoundItem != initiallyBoundItem)
                        return; // disposed/unbound somewhere on time
                    action.Invoke();
                }, Activity);
            }
        }
    }
}
#endif
