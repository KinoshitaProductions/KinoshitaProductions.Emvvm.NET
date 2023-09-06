#if __ANDROID__
// ReSharper disable TypeParameterCanBeVariant
#pragma warning disable S2326,S2436
using System.Collections.ObjectModel;

namespace KinoshitaProductions.Emvvm.Interfaces
{
    public interface ICollectionBindable<TViewModel, TViewModelBinder, TCollectionBinder, TItem>
        where TViewModel : ObservableViewModel
        where TViewModelBinder : class, IViewModelBinder<TViewModel>
        where TCollectionBinder : class, ICollectionBinder<TViewModel, TViewModelBinder, TItem>
        where TItem : ObservableObject
    {
        bool IsBound { get; }
        void BindTo(Activity activity, Android.Content.Context context, TViewModel viewModel, TViewModelBinder viewModelBinder, TCollectionBinder collectionBinder, ObservableCollection<TItem> items);
        void Unbind();
    }
}
#endif
