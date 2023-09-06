#if __ANDROID__
namespace KinoshitaProductions.Emvvm.Binders
{
    // T is bound item type, U is binder real class
    public abstract class JavaSimpleBinder<TItem, TListener> : Java.Lang.Object, IBinder<TItem, TListener> where TItem : class where TListener : JavaSimpleBinder<TItem, TListener>
    {
        protected JavaSimpleBinder()
        {
        }

        protected JavaSimpleBinder(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        // ReSharper disable once MemberCanBePrivate.Global
        protected TItem? BoundItem;

        public bool IsBound => BoundItem != null;

        public virtual TListener BindTo(TItem item)
        {
            this.BoundItem = item;
            return (TListener)this;
        }

        public void Unbind()
        {
            this.BoundItem = null;
        }
    }
}
#endif