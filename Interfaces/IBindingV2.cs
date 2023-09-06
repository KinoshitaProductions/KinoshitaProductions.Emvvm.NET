#pragma warning disable S3246
namespace KinoshitaProductions.Emvvm.Interfaces
{
    // ReSharper disable once TypeParameterCanBeVariant
    public interface IBindingV2<TItem> : IDisposable
    {
        bool IsBound { get; }
        TItem? BoundItem { get; }
    }
}
