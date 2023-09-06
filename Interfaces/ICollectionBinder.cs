#if __ANDROID__
// ReSharper disable TypeParameterCanBeVariant
#pragma warning disable S2326,S2436
using System.Collections.ObjectModel;

namespace KinoshitaProductions.Emvvm.Interfaces
{
    public interface ICollectionBinder<TViewModel, TViewModelBinder, TItem>
        where TViewModel : ObservableViewModel
        where TViewModelBinder : class, IViewModelBinder<TViewModel>
        where TItem : ObservableObject
    {
        bool IsBound { get; }
        void BindTo(Activity activity, Android.Content.Context context, TViewModel viewModel, TViewModelBinder viewModelBinder, ObservableCollection<TItem> items);
        void Unbind();
    }
    
    public interface ICollectionBinder<TViewModel, TViewModelBinder, TAdapter, TItem>
        where TViewModel : ObservableViewModel
        where TViewModelBinder : class, IViewModelBinder<TViewModel>
        where TItem : ObservableObject
        where TAdapter : class
    {
        bool IsBound { get; }
        void BindTo(Activity activity, Android.Content.Context context, TViewModel viewModel, TViewModelBinder viewModelBinder, TAdapter adapter, ObservableCollection<TItem> items);
        void Unbind();
    }
}
#endif
