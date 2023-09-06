// ReSharper disable TypeParameterCanBeVariant
#pragma warning disable S3246
namespace KinoshitaProductions.Emvvm.Interfaces
{
    // ReSharper disable twice TypeParameterCanBeVariant
    public interface IBinder<TItem> : IBindable where TItem : class
    {
        void BindTo(TItem item);
        void Unbind();
    }
    public interface IBinder<TItem, TListener> : IBindable where TItem : class
    {
        TListener BindTo(TItem item);
        void Unbind();
    }
}
