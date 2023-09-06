#if __ANDROID__
#pragma warning disable S2326,S3246
namespace KinoshitaProductions.Emvvm.Interfaces
{
    // ReSharper disable once TypeParameterCanBeVariant
    // ReSharper disable once InconsistentNaming
    public interface IUIBinder<TItem> where TItem : class
    {
        void BindTo(Activity activity, Android.Content.Context context, TItem item);
        void Unbind();
        bool IsBound { get; }
    }
}
#endif
