namespace KinoshitaProductions.Emvvm
{
    public static class State
    {
#if WINDOWS_UWP
#if NET7_0_OR_GREATER
        public static Microsoft.UI.Dispatching.DispatcherQueue? DispatcherQueue { get; private set; }
        public static void Initialize(Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue)
        {
            DispatcherQueue = dispatcherQueue;
        }
#endif
#endif
        // ReSharper disable once MemberCanBePrivate.Global
        public static int LastViewModelGeneration { get; private set; } = -1;
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool IsStartInProgress { get; private set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool IsRestartInProgress { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private static bool IsFaultedRestore { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private static bool IsRestored { get; set; }
        private static bool IsRestoreInProcess { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private static bool IsLastViewRestoreInProcess { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private static int LastViewDepth { get; set; } = -1;
        public static void NotifyStartInProgress()
        {
            IsStartInProgress = true;
        }
        public static void NotifyStartComplete()
        {
            IsStartInProgress = false;
        }
        public static void NotifyRestartInProgress()
        {
            IsRestartInProgress = true;
        }
        public static void NotifyRestartCompleted()
        {
            IsRestartInProgress = false;
        }
        public static void NotifyRestorationStart()
        {
            IsRestoreInProcess = true;
        }
        public static void NotifyRestorationCompleted(bool restorationSuccessful, bool lastViewIsPending = false)
        {
            IsRestoreInProcess = false;
            IsFaultedRestore = !restorationSuccessful;
            IsLastViewRestoreInProcess = lastViewIsPending;

            // we need these two to identify the last view
            IsRestored = true;
            if (lastViewIsPending)
                LastViewDepth = NavigationStackDepth + 1;

            UpdateDepths();
        }

        /// <summary>
        /// This value is increased +1 before being assigned (so the first one will be 1)
        /// </summary>
        private static int _currentActivationDepth;

        /// <summary>
        /// Keeps track of the view models stack.
        /// It's being kept separated to avoid being intrusive in app code.
        /// </summary>
        private static readonly List<ObservableViewModel> NavigationStackPrivate = new ();
        private static readonly Stack<(DateTime DisposableAt, ObservableViewModel ViewModel)> DisposingStack = new ();
  
        public static IList<ObservableViewModel> NavigationStack => NavigationStackPrivate;

        private static void AddToStack(ObservableViewModel viewModel)
        {
            lock (NavigationStackPrivate)
            {
                LocklessCurrent?.NotifyStateChanged(); // allows change detection on navigation (since we only scan the active one, and the non-materialized ones)
                NavigationStackPrivate.Add(viewModel);
            }
            ++LastViewModelGeneration;
        }
        /// <summary>
        /// Create a new instance of ViewModel <typeparamref name="T"/>, try to activate and add to stack if successful.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="onSuccess"></param>
        /// <param name="onFailure"></param>
        /// <param name="activationAction"></param>
        /// <returns></returns>
        public static bool TryActivateAndAddToStack<T>(Func<T, bool> onSuccess, Action? onFailure = null, Func<T, Func<T, bool>, Action?, bool>? activationAction = null) where T : ObservableViewModel, new()
        {
            var newViewModel = new T();

            return TryActivateAndAddToStack(newViewModel, onSuccess, onFailure, activationAction);
        }
        /// <summary>
        /// Take an instance of ViewModel <typeparamref name="T"/>, try to activate and add to stack if successful.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="existingViewModel"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFailure"></param>
        /// <param name="activationAction"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool TryActivateAndAddToStack<T>(T existingViewModel, Func<T, bool> onSuccess, Action? onFailure = null, Func<T, Func<T, bool>, Action?, bool>? activationAction = null) where T : ObservableViewModel, new()
        {
            bool success = activationAction?.Invoke(existingViewModel, onSuccess, onFailure) ?? existingViewModel.Activate(onSuccess, onFailure);

            if (success)
                AddToStack(existingViewModel);
            else
                NotifyNavigationCompleted(); // failed to navigate

            return success;
        }

        public static void RemoveFromStack(ObservableViewModel? viewModel)
        {
            if (viewModel == null) return;
            if (viewModel.IsActivated)
                EnqueueForGarbageCollection(viewModel); // schedule for deactivate

            int position;
            lock (NavigationStackPrivate)
            {
                position = NavigationStackPrivate.IndexOf(viewModel);
                if (position != -1)
                {
                    NavigationStackPrivate.Remove(viewModel);
                }
            }

            // resort the stack assigned depths
            UpdateDepths(position);

            ++LastViewModelGeneration;
        }

        private static void UpdateDepths(int fromPosition = 0)
        {
            if (fromPosition < 0 || IsRestoreInProcess)
                return; // do not run if invalid position, or still in restoring process

            lock (NavigationStackPrivate)
            {
                for (int i = fromPosition; i < NavigationStackPrivate.Count; ++i)
                {
                    NavigationStackPrivate[i].ActivationDepth = i + 1;
                }
                _currentActivationDepth = NavigationStackPrivate.Count;
            }
        }
        /// <summary>
        /// Activates the element properly.
        /// </summary>
        public static void Activate(ObservableViewModel viewModel)
        {
            if (!viewModel.IsActivated)
            {
                viewModel.ActivationDepth = ++_currentActivationDepth;
                NotifyActivated(viewModel, viewModel.ActivationDepth);
            }
        }

        /// <summary>
        /// Clears ALL views from stack.
        /// May be desired if restarting the app.
        /// </summary>
        public static void ClearStack()
        {
            lock (NavigationStackPrivate)
            {
                while (NavigationStackPrivate.Count > 1)
                {
                    if (NavigationStackPrivate[NavigationStackPrivate.Count - 1].IsActivated)
                        EnqueueForGarbageCollection(NavigationStackPrivate[NavigationStackPrivate.Count - 1]); // schedule for deactivate
                    NavigationStackPrivate.RemoveAt(NavigationStackPrivate.Count - 1);
                }

                _currentActivationDepth = NavigationStackPrivate.Count;
            }
        }

        /// <summary>
        /// Clears ALMOST ALL views from the stack, until specified activationDepth (deepest is 1).
        /// Root view remains active
        /// </summary>
        public static void ResetToRoot(int activationDepth = 1)
        {
            lock (NavigationStackPrivate)
            {
                while (NavigationStackPrivate.Count > 1 && NavigationStackPrivate[NavigationStackPrivate.Count - 1].ActivationDepth > activationDepth)
                {
                    if (NavigationStackPrivate[NavigationStackPrivate.Count - 1].IsActivated)
                        EnqueueForGarbageCollection(NavigationStackPrivate[NavigationStackPrivate.Count - 1]); // schedule for deactivate
                    NavigationStackPrivate.RemoveAt(NavigationStackPrivate.Count - 1);
                }

                _currentActivationDepth = NavigationStackPrivate.Count;
            }
        }

        /// <summary>
        /// Adds the viewModel to a disposal queue (to avoid flickering on back button, since it'd dispose and update the UI during the disposal)
        /// </summary>
        /// <param name="viewModel"></param>
        private static void EnqueueForGarbageCollection(ObservableViewModel viewModel)
        {
            lock (DisposingStack)
            {
                int secondsUntilDeactivation = 5;
                DisposingStack.Push((DateTime.Now + TimeSpan.FromSeconds(secondsUntilDeactivation), viewModel));
            }
        }
        /// <summary>
        /// Performs the garbage collection on the viewModels which can now be disposed.
        /// </summary>
        public static
#if WINDOWS_UWP
            async Task
#else 
            void
#endif
            PerformGarbageCollection()
        {
            lock (DisposingStack)
            {
                if (DisposingStack.Count == 0)
                    return; // nothing to do
            }

            // in some cases, this might be relayed to a background thread which would fail on disposing
            List<int> deactivatedDepths = new List<int>();

            void CollectGarbage()
            {
                lock (DisposingStack)
                {
                    while (DisposingStack.TryPeek(out var checking) && checking.DisposableAt < DateTime.Now)
                    {
                        if (DisposingStack.TryPop(out var disposingViewModel))
                        {
                            deactivatedDepths.Add((disposingViewModel.ViewModel.ActivationDepth));
                            if (disposingViewModel.ViewModel.IsActivated) disposingViewModel.ViewModel.Deactivate();
                        }
                    }
                }
            }
#if WINDOWS_UWP
            await
#endif
            Marshaller.MarshalTask(CollectGarbage)
#if WINDOWS_UWP
            .ConfigureAwait(false)
#endif
            ;

            foreach (var deactivatedDepth in deactivatedDepths)
            {
                // we must check if the number exceeds the latest valid state
                // if not, we would be deleting a valid state!
                if (deactivatedDepth > NavigationStackDepth)
                    NotifyDeactivated(deactivatedDepth);
            }
        }

        public static ObservableViewModel? Current { get { lock (NavigationStackPrivate) if (NavigationStackPrivate.Count > 0) return NavigationStackPrivate[NavigationStackPrivate.Count - 1]; else return null; } }
        private static ObservableViewModel? LocklessCurrent { get { if (NavigationStackPrivate.Count > 0) return NavigationStackPrivate[NavigationStackPrivate.Count - 1]; else return null; } }
        public static int NavigationStackDepth { get { lock (NavigationStackPrivate) return NavigationStackPrivate.Count; } }

#if __ANDROID__
        private static int _nextExtra = (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
        private static readonly Dictionary<int, ObservableViewModel> ExtraReferences = new ();
        public static int MakeExtra(ObservableViewModel viewModel)
        {
            var extra = Interlocked.Increment(ref _nextExtra);
            lock (ExtraReferences)
                ExtraReferences.Add(extra, viewModel);
            return extra;
        }
        public static bool HasExtra(int extra)
        {
            lock (ExtraReferences)
            {
                return ExtraReferences.ContainsKey(extra);
            }
        }
        public static bool HasExtraAndIsLastView(int extra)
        {
            lock (ExtraReferences)
            {
                return LastViewDepth == extra && ExtraReferences.ContainsKey(extra);
            }
        }

        public static bool TryPeekExtra(int extra, out ObservableViewModel? viewModel)
        {
            lock (ExtraReferences)
            {
                return ExtraReferences.TryGetValue(extra, out viewModel);
            }
        }

        public static T GetExtra<T>(int extra) where T : ObservableViewModel
        {
            lock (ExtraReferences)
            {
                if (ExtraReferences.TryGetValue(extra, out var viewModel))
                {
                    ExtraReferences.Remove(extra);
                    return (T)viewModel;
                }
                throw new ArgumentException($"Requested extra {extra} not found");
            }
        }
#endif

        private static int _waitingForNavigationCompletedSemaphore;
        private static readonly SemaphoreSlim NavigationCompletedSemaphore = new SemaphoreSlim(0);
        public static Task WaitForNavigationToComplete()
        {
            Interlocked.Increment(ref _waitingForNavigationCompletedSemaphore);
            return Task.WhenAny(NavigationCompletedSemaphore.WaitAsync(), Task.Delay(4000));
        }
        // ReSharper disable once MemberCanBePrivate.Global
        public static void NotifyNavigationCompleted()
        {
            while (_waitingForNavigationCompletedSemaphore > 0)
            {
                NavigationCompletedSemaphore.Release();
                Interlocked.Decrement(ref _waitingForNavigationCompletedSemaphore);
            }
        }

        // THe following listeners are normally used to save states of ViewModels
        // 1) On activation (saves initial state),
        // 2) On navigated from (saves last state),
        // 3) On deactivation (deletes state),
        // The latest state is saved through a passive scan every X seconds.
#region STATE_SAVING
        private static Func<ObservableViewModel, int, Task>? _onActivatedListener;
        public static void SetOnActivatedListener(Func<ObservableViewModel, int, Task> setListener)
        {
            _onActivatedListener = setListener;
        }
        private static void NotifyActivated(ObservableViewModel viewModel, int activationDepth)
        {
            _onActivatedListener?.Invoke(viewModel, activationDepth);
        }
        private static Func<ObservableViewModel, int, Task>? _onNavigatedFromListener;
        public static void SetOnNavigatedFromListener(Func<ObservableViewModel, int, Task> setListener)
        {
            _onNavigatedFromListener = setListener;
        }
        public static void NotifyNavigatedFrom(ObservableViewModel viewModel, int activationDepth)
        {
            _onNavigatedFromListener?.Invoke(viewModel, activationDepth);
        }
        private static Func<int, Task>? _onDeactivatedListener;
        public static void SetOnDeactivatedListener(Func<int, Task> setListener)
        {
            _onDeactivatedListener = setListener;
        }
        private static void NotifyDeactivated(int activationDepth)
        {
            _onDeactivatedListener?.Invoke(activationDepth);
        }
#endregion
    }
}
