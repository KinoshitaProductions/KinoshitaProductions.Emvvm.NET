namespace KinoshitaProductions.Emvvm.Services;
using KinoshitaProductions.Common.Enums;
using KinoshitaProductions.Common.Services;
using Serilog;

/// <summary>
/// The application engine controls and watches over the other sub engines.
/// </summary>

public abstract class StateEngine<TStateMetadata> : Engine where TStateMetadata : StateMetadataDefinition, new()
{
    #region STATE_SAVING
    private bool ForceTimestampUpdate => _stateMetadata.Timestamp.AddMinutes(2) < DateTime.Now || _stateMetadata.LastViewModelGeneration != State.LastViewModelGeneration;

    private bool _isRestoring;
    /// <summary>
    /// Used to notify NavigationEngines to not write states until this is done.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public bool IsClearingState { get; protected set; }

    private TStateMetadata _stateMetadata  = new ();

    /// <summary>
    /// Stores whether if the app already attempted to restore or not.
    /// If the app has been restored, it can write new state.
    /// If it hasn't been restored, it must never try to write a new state (or would delete the possibly restoring one).
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public bool AttemptedRestore { get; private set; }
    /// <summary>
    /// Stores whether if the app thinks it can store state.
    /// If it fails multiple times to store state, this will change to false and stop trying to save state, should attempt erasing it first
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public bool CanSaveState => true; /* Since clearing the state doesn't work without it enabled, we'll set it always on for now */
    private bool _canRestoreState = true;
    private int _failedStateSavingAttempts;
    /// <summary>
    /// Helper function to track issues saving state and disable it if happens too often.
    /// </summary>
    // ReSharper disable once MemberCanBeProtected.Global
    public virtual void NotifyFailedStateSavingAttempt()
    {
        // Counts for 3 points per failure, since we have multiple saving points, this should "alleviate" issues due to variety
        _failedStateSavingAttempts += 3;
        // can disable saving if _failedStateSavingAttempts > 20
    }
    /// <summary>
    /// Allows disabling from other parts of the code the state saving/restoration.
    /// </summary>
    public virtual void NotifyStateSavingDisabled()
    {
        AttemptedRestore = true;
        _failedStateSavingAttempts = 100;
    }
    /// <summary>
    /// Helper function to disable restore (on user prompt)
    /// </summary>
    /// <returns></returns>
    public virtual void NotifyInvalidState()
    {
        AttemptedRestore = true;
        _canRestoreState = false;
        _lastTimeSavedState = DateTime.Now + _intervalBetweenStateResets;
    }
    /// <summary>
    /// Allows enabling from other parts of the code the state saving/restoration.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public virtual void NotifyStateSavingEnabled()
    {
        AttemptedRestore = true;
        _failedStateSavingAttempts = 0;
    }
    /// <summary>
    /// Helper function to track issues saving state and disable it if happens too often.
    /// </summary>
    // ReSharper disable once MemberCanBeProtected.Global
    public virtual void NotifySuccessfulStateSavingAttempt()
    {
        if (_failedStateSavingAttempts > 0)
            _failedStateSavingAttempts = 0;
    }
    /// <summary>
    /// Helper function to notify state was restored.
    /// </summary>
    /// <returns></returns>
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once MemberCanBeProtected.Global
    public virtual void NotifyStateRestored()
    {
        AttemptedRestore = true;
        _lastTimeSavedState = DateTime.Now + _intervalBetweenStateResets;
        _savedStateOnce = true; // else, it would delete old state data
    }
    // ReSharper disable once MemberCanBeProtected.Global
    public virtual void NotifyChangesDetected()
    {
        _stateMetadata.StateJson = null;
    }
    private async Task<bool> MarkRestoreAttempt()
    {
        if (!await FileManager.ExistsAsync(AppFolder.State, "ra1").ConfigureAwait(false))
        {
            await FileManager.TryTouchFileAsync(AppFolder.State, "ra1").ConfigureAwait(false);
        }
        else if (!await FileManager.ExistsAsync(AppFolder.State, "ra2").ConfigureAwait(false))
        {
            await FileManager.TryTouchFileAsync(AppFolder.State, "ra2").ConfigureAwait(false);
        }
        else if (!await FileManager.ExistsAsync(AppFolder.State, "ra3").ConfigureAwait(false))
        {
            await FileManager.TryTouchFileAsync(AppFolder.State, "ra3").ConfigureAwait(false);
        }
        else
        {
            // already tried to restore three times, mark that it cannot be restored and clean up the state
            _canRestoreState = false;
            return false;
        }

        if (!CanSaveState || !_canRestoreState)
        {
            await ClearState().ConfigureAwait(false);
            return false;
        }

        return true;
    }
    private async Task ClearRestoreAttempts()
    {
        try
        {
            // delete restore attempt markers
            if (await FileManager.ExistsAsync(AppFolder.State, "ra1").ConfigureAwait(false))
            {
                await FileManager.DeleteAsync(AppFolder.State, "ra1").ConfigureAwait(false);
                if (await FileManager.ExistsAsync(AppFolder.State, "ra2").ConfigureAwait(false))
                {
                    await FileManager.DeleteAsync(AppFolder.State, "ra2").ConfigureAwait(false);
                    if (await FileManager.ExistsAsync(AppFolder.State, "ra3").ConfigureAwait(false))
                        await FileManager.DeleteAsync(AppFolder.State, "ra3").ConfigureAwait(false);

                    // delete state cause dang
                    if (await FileManager.ExistsAsync(AppFolder.State, "s").ConfigureAwait(false))
                        await FileManager.DeleteAsync(AppFolder.State, "s").ConfigureAwait(false);
                }
            }
            NotifyChangesDetected();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to clear restore attempts");
        }
    }
    /// <summary>
    /// During first state save, it saves THE ENTIRE STATE, while in the other saves it only saves the active pieces of it instead.
    /// </summary>
    private bool _savedStateOnce;
    private DateTime _lastTimeSavedState = DateTime.Now - TimeSpan.FromHours(1);
    private readonly TimeSpan _intervalBetweenStateSaves = TimeSpan.FromSeconds(3);
    private readonly TimeSpan _intervalBetweenStateResets = TimeSpan.FromSeconds(7);

    public async Task<StateRestoreStatus> CheckIfThereIsAnStateToRestore()
    {
        // if can't save state, there is no state to restore
        if (!CanSaveState || !_canRestoreState)
            return StateRestoreStatus.NoStateSaved;

        // if there is an saved state
        if (await FileManager.ExistsAsync(AppFolder.State, "s").ConfigureAwait(false))
        {
            // and haven't run over the 3 attempts
            if (!await FileManager.ExistsAsync(AppFolder.State, "ra1").ConfigureAwait(false)) // Changed from ra3 to ra1, so at first failure it'll always try a new state
            {
                // there is an state to restore, let's validate the timestamp too
                var stateMetadata = (await SettingsManager.TryLoading<StateMetadataDefinition>(AppFolder.State, "s", CompressionAlgorithm.GZipFast).ConfigureAwait(false));
                if (stateMetadata == null)
                    return StateRestoreStatus.NoStateSaved; // corrupted data
                // less than 12 minutes passed, perform auto-restore
#if __ANDROID__
                if (DateTime.Now < stateMetadata.Timestamp.AddMinutes(24))
#else
                if (DateTime.Now < stateMetadata.Timestamp.AddMinutes(12))
#endif
                    return StateRestoreStatus.AutomaticRestore;
                // less than a day passed, ask if should restore
                if (DateTime.Now < stateMetadata.Timestamp.AddHours(20))
                    return StateRestoreStatus.PromptForRestore;
                // disable restoration after a day
                return StateRestoreStatus.NoStateSaved;
            }
            else
            {
                // state failed to restore too many times, discard it
                await ClearState().ConfigureAwait(false);
            }
        }
        // else, there isn't
        return StateRestoreStatus.NoStateSaved;
    }

    // ReSharper disable once MemberCanBeProtected.Global
    public virtual async Task ClearState()
    {
        IsClearingState = true;
        try
        {
            // delete restore attempts
            await ClearRestoreAttempts().ConfigureAwait(false);

            // delete old states, create new folder
            if (await FileManager.ExistsAsync(AppFolder.State, "ss").ConfigureAwait(false))
                await FileManager.DeleteAsync(AppFolder.State, "ss").ConfigureAwait(false);
            await FileManager.CreateOrOpenFolderAsync(AppFolder.State, "ss").ConfigureAwait(false);
            // delete old summary
            if (await FileManager.ExistsAsync(AppFolder.State, "s").ConfigureAwait(false))
                await FileManager.DeleteAsync(AppFolder.State, "s").ConfigureAwait(false);
            
            // delete old navigation engines, create new folder
            if (await FileManager.ExistsAsync(AppFolder.State, "n").ConfigureAwait(false))
                await FileManager.DeleteAsync(AppFolder.State, "n").ConfigureAwait(false);
            await FileManager.CreateOrOpenFolderAsync(AppFolder.State, "n").ConfigureAwait(false);
            // delete old navigation engines batches, create new folder
            if (await FileManager.ExistsAsync(AppFolder.State, "nb").ConfigureAwait(false))
                await FileManager.DeleteAsync(AppFolder.State, "nb").ConfigureAwait(false);
            await FileManager.CreateOrOpenFolderAsync(AppFolder.State, "nb").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to clear state");
        }
        IsClearingState = false;
    }

    private bool MarkRestoreCompletedAndRun(Func<bool> after)
    {
        this._isRestoring = false;
        return after();
    }
    protected virtual Task<bool> SaveData(TStateMetadata stateMetadata) => Task.FromResult(true);
    protected virtual Task<bool> RestoreData(TStateMetadata stateMetadata) => Task.FromResult(true);
    protected virtual bool LinkData(ObservableViewModel viewModel, int activationDepth) => true;
    protected virtual void CleanupAfterRestoration() { }
    private static async Task<List<ObservableViewModel>?> RestoreViewModels(TStateMetadata stateMetadata)
    {
        // 4) We must load the ViewModels
        var viewModels = new List<ObservableViewModel>();
        var previousViewModelKind = ""; // in certain disastrous situation, unknown how it was achieved, 4 MainViewModels had been stored nothing else
        for (int i = 1; i <= stateMetadata.DeepestActivationDepth; ++i)
        {
            FilePresence filePresence;
            if ((filePresence = await SettingsManager.ExistsAsync(AppFolder.State, "ss" + Path.DirectorySeparatorChar + i).ConfigureAwait(false)) == FilePresence.NotFound)
                return null; // viewModel not found
            var viewModelEntry = (await SettingsManager.TryLoadingStatefulAsJson<ViewModelEntry>(AppFolder.State, "ss" + Path.DirectorySeparatorChar + i, filePresence, CompressionAlgorithm.GZipFast).ConfigureAwait(false));
            if (viewModelEntry?.Value == null)
                return null; // corrupted data

            if (!ViewModelManager.ExistsMappingFor(viewModelEntry.Kind))
                return null; // no mapping for viewModel

            if (i == 1 && viewModelEntry.Kind != "m")
                return null; // expected a MainViewModel as root

            if (previousViewModelKind != viewModelEntry.Kind)
            {
                previousViewModelKind = viewModelEntry.Kind;
                viewModelEntry.Value.NotifyMaterialized();
                viewModels.Add(viewModelEntry.Value);
            }
        }

        if (viewModels.Count == 0)
            return null; // missing data
        return viewModels;
    }
    public async Task<bool> RestoreState(
#if WINDOWS_UWP
#if NET7_0_OR_GREATER
        Microsoft.UI.Xaml.Controls.Frame frame,
#else
        Windows.UI.Xaml.Controls.Frame frame,
#endif
#elif __ANDROID__
        Activity activity,
#endif
        Func<bool> onSuccess, Func<bool> onFailure)
    {
        this._isRestoring = true;
        try
        {
            if (!CanSaveState || !_canRestoreState)
                return MarkRestoreCompletedAndRun(onFailure); // either shouldn't have reached here, or the user refused state restore

            // 1) Set up a marker to count restoration attempts (so we don't attempt too many times to restore a corrupt state)
            if (!await MarkRestoreAttempt())
                return MarkRestoreCompletedAndRun(onFailure); // either shouldn't have reached here, or too many failed restore attempts

            // set up file presence var
            FilePresence filePresence;

            // 2) Read the summary
            if ((filePresence = await SettingsManager.ExistsAsync(AppFolder.State, "s").ConfigureAwait(false)) == FilePresence.NotFound)
                return MarkRestoreCompletedAndRun(onFailure); // stateMetadata not found
            var stateMetadata = (await SettingsManager.TryLoadingStatefulAsJson<TStateMetadata>(AppFolder.State, "s", filePresence, CompressionAlgorithm.GZipFast).ConfigureAwait(false));
            if (stateMetadata?.IsValid != true)
                return MarkRestoreCompletedAndRun(onFailure); // corrupted data

            if (!await RestoreData(stateMetadata))
                return MarkRestoreCompletedAndRun(onFailure); // failed to restore data

            var viewModels = await RestoreViewModels(stateMetadata);
            if (viewModels == null)
                return MarkRestoreCompletedAndRun(onFailure); // failed to restore viewModels
            
            // clear navigation stack here, or we might get duplicates
            State.ClearStack();

            // 5) Restore and link Engines with ViewModels
            for (int i = 0; i < viewModels.Count; ++i)
            {
                if (i == 0)
                {
                    await Task.Delay(300); // grant some extra delay to avoid some weird startup issues
                }

                await Task.Delay(50);  // in Android, resources are loaded so quick that it ends up corrupting resources (or end up out of order)

                var viewModel = viewModels[i];

                if (!LinkData(viewModel, i)) 
                    return MarkRestoreCompletedAndRun(onFailure); // failed to link data

                bool isLastView = i == viewModels.Count - 1;

                viewModel.IsLazyInitialization = !isLastView;

                // activate and navigate to it
                var viewModelMapping = ViewModelManager.GetMappingFor(viewModel.Kind);

                // marshall since we're on background (NOTE: We must await, since the Navigation Contexts and Engines will be created during inflation
                var waitForNavigation = State.WaitForNavigationToComplete(); // must be done separately, or won't go through if it's super fast
#if WINDOWS_UWP
                await Marshaller.MarshalTask(() =>
#elif __ANDROID__
                await Marshaller.MarshalTaskAndWait(() =>
#else
                Marshaller.MarshalTask(() =>
#endif
                    {
                        viewModelMapping.NavigateToView(
#if WINDOWS_UWP
                            frame,
#elif __ANDROID__
                            activity,
#endif
                            viewModel, isLastView);
                    }
#if WINDOWS_UWP || __ANDROID__
                ).ConfigureAwait(false);
#else
                );
#endif
                await waitForNavigation;

                if (isLastView)
                {
                    await Task.Delay(70);  // in Android, resources are loaded so quick that it ends up corrupting resources (or end up out of order)
                    // Notify we're done, since next activation will have a normal behaviour
                    State.NotifyRestorationCompleted(true, true);
                    NotifyStateRestored();
                }
            }

            // apply restored state
            _stateMetadata = stateMetadata;

            // cleanup
            CleanupAfterRestoration();
            viewModels.Clear();

            await ClearRestoreAttempts().ConfigureAwait(false);

            return MarkRestoreCompletedAndRun(onSuccess); // success
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to restore state");
        }
        finally
        {
            CleanupAfterRestoration();
            NotifyStateRestored();
        }
        return MarkRestoreCompletedAndRun(onFailure);
    }
    private async Task SaveViewModelState(ObservableViewModel viewModel, int activationDepth)
    {
        if (!CanSaveState || !AttemptedRestore)
            return;
        // if it's null or whitespace, it should not be materialized since it cannot be restored
        if (viewModel.IsActivated && !string.IsNullOrWhiteSpace(viewModel.Kind))
            await SettingsManager.TrySavingStatefulAsJson(new ViewModelEntry { Kind = viewModel.Kind, Value = viewModel }, AppFolder.State, "ss" + Path.DirectorySeparatorChar + activationDepth, CompressionAlgorithm.GZipFast).ConfigureAwait(false);
    }

    private async Task SaveCurrentState(bool entireStack)
    {
        if (_isRestoring || !AttemptedRestore || !CanSaveState || (!entireStack && !_savedStateOnce) || !(DateTime.Now >= _lastTimeSavedState + _intervalBetweenStateSaves || ForceTimestampUpdate))
            return; // cannot save state, it would be incomplete, and folders might not exist

        // 1) set LastTimeSavedState to now to avoid running it too frequently
        _lastTimeSavedState = DateTime.Now;

        // 2) if we're going to save the entire stack, clean the previous State
        if (entireStack)
        {
            await ClearState().ConfigureAwait(false);
        }

        // 3) save the remaining missing stack IF NOT MATERIALIZED (to save missing state entries)
        // we need a copy, since we can't lock onto the original one and run async saving
        int maxNavigatableDepth = 0;
        ObservableViewModel[] loopableNavigationStack;
        lock (State.NavigationStack)
            loopableNavigationStack = State.NavigationStack.ToArray();
        
        foreach (var viewModel in loopableNavigationStack)
        {
            // ensure it's a valid path
            if (string.IsNullOrEmpty(viewModel.Kind) || !ViewModelManager.ExistsMappingFor(viewModel.Kind))
                break;
            ++maxNavigatableDepth;

            if (!viewModel.IsMaterialized)
            {
                await SaveViewModelState(viewModel, viewModel.ActivationDepth).ConfigureAwait(false);
                viewModel.NotifyMaterialized(); // notify materialized here to lock it
            }
        }

        _stateMetadata.MaxNavigatableDepthPreSave = maxNavigatableDepth;

        // 4) save the latest view ALWAYS (to keep state updated)
        var currentViewModel = State.Current;
        if (currentViewModel?.IsActivated == true && currentViewModel.ActivationDepth <= maxNavigatableDepth)
        {
            await SaveViewModelState(currentViewModel, currentViewModel.ActivationDepth).ConfigureAwait(false);
            currentViewModel.NotifyMaterialized(); // notify materialized here to lock it
        }

        await SaveData(_stateMetadata);

        // Save once the timestamp at least every 2 minutes OR viewModelGeneration changed, or it will have difficulties on deciding whether restore or not, or show popup or not
        bool forceTimestampUpdate = ForceTimestampUpdate; // required to copy since it will be invalidated on state timestamp update
        // Perform a silly changes in state check before making a new file (SINCE Timestamp check will make it fail, as it's ALWAYS DIFFERENT)
        if (
            forceTimestampUpdate ||
            // Or save in case the stack has been modified
            _stateMetadata.HasChanges())
        {
            _stateMetadata.UpdateMetadataForSaving();

            // 5) Store the metadata for quick restoration.
            await SettingsManager.TrySavingStatefulAsJsonWithTimestamp(_stateMetadata, AppFolder.State, "s", CompressionAlgorithm.GZipFast, forceTimestampUpdate: forceTimestampUpdate).ConfigureAwait(false);
        }
        _savedStateOnce = true;
    }

    #endregion STATE_SAVING

    protected override async Task<int> OnLoopTick(CancellationToken cancellationToken)
    {
        // check if the user decided to discard previous state\
        if (!AttemptedRestore && !_canRestoreState)
        {
            _lastTimeSavedState = DateTime.Now + _intervalBetweenStateSaves;
            AttemptedRestore = true;
        }

        // run state saving
        #region STATE_SAVING

        if (CanSaveState && !IsClearingState)
        {
            try
            {
                if (DateTime.Now >= _lastTimeSavedState + _intervalBetweenStateSaves || ForceTimestampUpdate)
                {
                    await SaveCurrentState(!_savedStateOnce).ConfigureAwait(false);
                    NotifySuccessfulStateSavingAttempt();
                }
            }
            catch (Exception ex)
            {
                NotifyFailedStateSavingAttempt();
                Log.Error(ex, "Failed to save current state");
            }
        }

        #endregion STATE_SAVING

        return 0; // no need to run actively
    }
}