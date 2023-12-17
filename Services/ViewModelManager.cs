namespace KinoshitaProductions.Emvvm.Services
{
    public static class ViewModelManager
    {
        private static readonly Dictionary<string, ViewModelMapping> KnownMappings = new ();
        public static ViewModelMapping GetMappingFor(string kind) => KnownMappings[kind];
        public static bool ExistsMappingFor(string kind) => KnownMappings.ContainsKey(kind);
        // ReSharper disable once UnusedMember.Global
        public static void AddMapping<T>(string key, ViewModelMapping.NavigateToViewHandler navigateToViewHandler) where T : ObservableViewModel
        {
            KnownMappings.Add(key, new ViewModelMapping(typeof(T), navigateToViewHandler));
        }
        
        // ReSharper disable once ClassNeverInstantiated.Global
        public class ViewModelMapping
        {
            internal ViewModelMapping(Type viewModelType, NavigateToViewHandler navigateToViewHandler)
            {
                ViewModelType = viewModelType;
                NavigateToView = navigateToViewHandler;
            }
            public Type ViewModelType { get; set; }
            public NavigateToViewHandler NavigateToView { get; set; }
            
#if WINDOWS_UWP
#if NET7_0_OR_GREATER
            public delegate bool NavigateToViewHandler(Microsoft.UI.Xaml.Controls.Frame frame, ObservableViewModel viewModel, bool useAnimations);
#else
            public delegate bool NavigateToViewHandler(Windows.UI.Xaml.Controls.Frame frame, ObservableViewModel viewModel, bool useAnimations);
#endif
#elif __ANDROID__
            public delegate bool NavigateToViewHandler(Activity activity, ObservableViewModel viewModel, bool useAnimations);
#else
            public delegate bool NavigateToViewHandler(ObservableViewModel viewModel, bool useAnimations);
#endif
        }
    }
}
