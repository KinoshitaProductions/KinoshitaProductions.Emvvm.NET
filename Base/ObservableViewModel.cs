using Newtonsoft.Json;
using KinoshitaProductions.Common.Interfaces;

namespace KinoshitaProductions.Emvvm.Base
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public abstract class ObservableViewModel : ObservableObject, IDisposable, IStatefulAsJson
    {
        #region STATE_SAVING
        /// <summary>
        /// Allows tracking if the viewModel state has already been materialized into a file or not.
        /// </summary>
        public bool IsMaterialized { get; private set; }
        /// <summary>
        /// Acknowledges the state as saved.
        /// </summary>
        public void NotifyMaterialized()
        {
            IsMaterialized = true;
        }
        /// <summary>
        /// Allows identifying the ViewModel Type from a serialized state.
        /// </summary>
        [JsonIgnore]
        public string Kind { get; set; } = string.Empty;
        #endregion
        [JsonIgnore]
        public string? StateJson { get; set; }

        private int _activationDepth;
        [JsonProperty("_ad")]
        public int ActivationDepth
        {
            get => _activationDepth;
            set
            {
                bool newActivationDetected = _activationDepth == 0;
                if (_activationDepth != value) // if activationDepth changed
                    IsMaterialized = false; // request materialization again (a.k.a. saving it to a file)
                _activationDepth = value;
                if (newActivationDetected && value != 0)
                {
                    RecentlyActivated = true;
                }
            }
        }

        /// <summary>
        /// Allows to init late the view if possible (used on Android).
        /// </summary>
        public bool IsLazyInitialization { get; set; }
        // In Android, we need two checks (one for OnCreate, and one for OnResume)
        private int _lazyInitializationChecksLeft = 2;
        public void NotifyLazyInitializationTriggered()
        {
           --_lazyInitializationChecksLeft;
            if (_lazyInitializationChecksLeft == 0)
                IsLazyInitialization = false; // next is the real initialization
        }

        /// <summary>
        /// Required to materialize previous view after navigating (since it would not be scanned for changes)
        /// </summary>
        public void NotifyStateChanged()
        {
            IsMaterialized = false;
        }

        /// <summary>
        /// If it was recently activated, full initialization is required.
        /// Else, an update could be more appropriate.
        /// </summary>
        protected bool RecentlyActivated { get; private set; }

        /// <summary>
        /// The ViewModel is considered activated if the user navigated to it, and hasn't been removed from the navigation stack.
        /// </summary>
        public bool IsActivated => _activationDepth != 0;

        private bool _isBusy;

        /// <summary>
        /// The ViewModel is considered busy if there is an action being executed (to avoid invoking a command twice, for example)
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// Private backing field to hold the title
        /// </summary>
        private string _title = string.Empty;

        /// <summary>
        /// Public property to set and get the title of the item
        /// </summary>
        [JsonProperty("_t")]
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private bool _navigationButtonsEnabled = true;

        public bool NavigationButtonsEnabled
        {
            get => _navigationButtonsEnabled;
            set => SetProperty(ref _navigationButtonsEnabled, value);
        }

        private ScreenOrientation _lastKnownOrientation = ScreenOrientation.Unknown;
        /// <summary>
        /// Stores the last orientation this view was in, in order to realign in case we went to another view.
        /// </summary>
        [JsonProperty("_so")]
        public virtual ScreenOrientation LastKnownOrientation
        {
            get => _lastKnownOrientation;
            set => SetProperty(ref _lastKnownOrientation, value);
        }

        /// <summary>
        /// If navigation succeeds and completes, this event will be called. 
        /// For example, if the user went from a video to comments, then back to the video, it'll resume the playback.
        /// On overriding, base.OnNavigatedTo() must be called.
        /// </summary>
        public virtual void OnNavigatedTo()
        {
            RecentlyActivated = false; // avoid over initializing
        }

        /// <summary>
        /// If the navigation exists the view successfully, trigger this event. It may deactivate events or background tasks (like video playback).
        /// </summary>
        public virtual void OnNavigatedFrom()
        {
            State.NotifyNavigatedFrom(this, ActivationDepth);
        }

        /// <summary>
        /// If the view is destroyed, or the user navigates back from it, or it's cleared from the navigation stack, this method must be invoked as it's not "active" anymore neither ever will be.
        /// By marking it as deactivated, the state will never be persisted, and may be fully recycled.
        /// </summary>
        public virtual void Deactivate()
        {
            _activationDepth = 0;

            Dispose();
        }
        
        /// <summary>
        /// Attempt to navigate to the specified viewModel. 
        /// If it succeeds, execute navigation action and return true. 
        /// If it fails, does nothing and returns false.
        /// </summary>
        /// <param name="activationSuccessful"></param>
        /// <param name="activationFailed"></param>
        /// <returns></returns>
        public virtual bool Activate<T>(Func<T, bool> activationSuccessful, Action? activationFailed = null) where T : ObservableViewModel
        {
            // it should be impossible to activate a view if navigation buttons are disabled
            if (!NavigationButtonsEnabled) return false;

            // disable navigation buttons to lock on for an activation
            NavigationButtonsEnabled = false;

            bool success = activationSuccessful.Invoke((T)this);

            // if successful, finish activation
            if (success)
                State.Activate(this);
            else
                activationFailed?.Invoke();

            NavigationButtonsEnabled = true; // user may retry navigating

            // ELSE, WILL REACTIVATE ON NAVIGATION COMPLETED
            return success;
        }

        protected bool IsDisposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;
            
            // get rid of managed resources
                
            // get rid of unmanaged resources
            
            IsDisposed = true;
        }

        ~ObservableViewModel()
        {
            Dispose(false);
        }
    }
}
