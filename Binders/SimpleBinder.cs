namespace KinoshitaProductions.Emvvm.Binders
{
    public abstract class SimpleBinder<TItem> : IBinder<TItem> where TItem : class
    {
        // ReSharper disable once MemberCanBePrivate.Global
        protected TItem? BoundItem;

        public bool IsBound => BoundItem != null;

        public void BindTo(TItem item)
        {
            BoundItem = item;
        }

        public void Unbind()
        {
            BoundItem = null;
        }
    }
}
