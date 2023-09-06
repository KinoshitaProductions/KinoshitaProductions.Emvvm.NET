#pragma warning disable S2326,S3246
namespace KinoshitaProductions.Emvvm.Interfaces
{
    // ReSharper disable once TypeParameterCanBeVariant
    public interface IViewModelBinder<TItem> where TItem : ObservableViewModel
    {
        bool IsBound { get; }

#if __ANDROID__
        void
        BindTo(

                Activity activity,
                Android.Content.Context context,
                TItem viewModel

        );

        void Unbind();
#else
        void BindTo(TItem viewModel);
#endif
    }
}
