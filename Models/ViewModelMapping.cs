// ReSharper disable UnassignedField.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8618

namespace KinoshitaProductions.Emvvm.Models
{
    /// <summary>
    /// Helper class for state restoring.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ViewModelMapping
    {
        /// <summary>
        /// Data to load into the ViewModel.
        /// </summary>
        public Type ViewModelType { get; set; }
#if WINDOWS_UWP
#if NET7_0_OR_GREATER
        public Action<Microsoft.UI.Xaml.Controls.Frame, ObservableViewModel, bool> NavigateToView { get; set; }
#else
        public Action<Windows.UI.Xaml.Controls.Frame, ObservableViewModel, bool> NavigateToView { get; set; }
#endif

#elif __ANDROID__
        public Action<Activity, ObservableViewModel, bool> NavigateToView { get; set; }
#else
        public Action<ObservableViewModel, bool> NavigateToView { get; set; }
#endif
    }
}
