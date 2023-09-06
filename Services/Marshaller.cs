namespace KinoshitaProductions.Emvvm.Services
{
    using Serilog;
    
    
#if WINDOWS_UWP
#if NET7_0_OR_GREATER
    using Microsoft.UI.Dispatching;
#endif
    using Windows.UI.Core;
#endif
#if __ANDROID__
    using Android.App;
#endif
    /// <summary>
    /// Exposes functions to marshal tasks to UI thread.
    /// </summary>
    public static class Marshaller
    {
#if ANDROID
        private static Func<Activity?> _getCurrentActivityFn = () => null;
#endif
        public static void Preinitialize(MarshallerOptions options)
        {
    #if ANDROID
            Marshaller._getCurrentActivityFn = options.GetCurrentActivityFn;
#endif
        }
#if __ANDROID__
        private static void ReleaseSemaphoreOnComplete(Action action, SemaphoreSlim semaphore)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to run task on UI thread (waiting)");
            }
            finally
            {
                semaphore.Release();
            }
        }
        public static void MarshalTask(Action action, Activity? activity, Action? onFail = null)
        {
            try
            {
                activity?.RunOnUiThread(action);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to run task on UI thread");
                onFail?.Invoke();
            }
        }
        public static void MarshalTask(Action action, Action? onFail = null) => MarshalTask(action, _getCurrentActivityFn(), onFail);
        public static async Task MarshalTaskAndWait(Action action)
        {
            using var semaphore = new SemaphoreSlim(0, 1);
            try
            {
                // ReSharper disable once AccessToDisposedClosure
                _getCurrentActivityFn()?.RunOnUiThread(() => ReleaseSemaphoreOnComplete(action, semaphore));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to run task on UI thread and wait");
            }
            await semaphore.WaitAsync();
        }

#elif WINDOWS_UWP
        /// <summary>
        /// Sends an Action to be executed by the UI thread.
        /// </summary>
        /// <param name="action">The Action to execute using the UI thread.</param>
        /// <returns>The Task generated for the Action.</returns>
#if NET7_0_OR_GREATER
        public static async Task MarshalTask(Action action)
        {
            DispatcherQueue? dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? State.DispatcherQueue;
            if (dispatcherQueue == null) return;
            
            using (var semaphore = new SemaphoreSlim(0, 1))
            {
                try
                {
                    dispatcherQueue.TryEnqueue(() => {
                        try
                        {
                            action();
                        }
                        finally
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            semaphore.Release();
                        }
                    });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to run task on UI thread");
                }
                await semaphore.WaitAsync();
                
            }
        }
#else
        public static Task MarshalTask(Action action)
        {
            return Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(action)).AsTask();
        }
#endif

        public static void MarshalTaskAndPersistWhile(Action action, Func<bool> condition)
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                   await MarshalTask(() =>
                   {
                       try
                       {
                           action.Invoke();
                           if(condition.Invoke())
                           {
                               MarshalTaskAndPersistWhile(action, condition);
                           }
                       }
                       catch (Exception ex)
                       {
                           Log.Error(ex, "Failed to run task on UI thread");
                       }
                   });

               });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to persist running task on UI thread");
            }
        }


        /// <summary>
        /// Sends an Action to be executed by the UI thread and waits until it has been finished.
        /// </summary>
        /// <param name="action">The Action to execute using the UI thread.</param>
        // ReSharper disable once MemberCanBePrivate.Global
        public static void MarshalTaskAndWait(Action action)
        {
            MarshalTask(action).Wait();
        }

        /// <summary>
        /// Sends an Action to be executed by the UI thread and waits until it has been finished. Catches if there is an exception and returns a bool if there was one.
        /// </summary>
        /// <param name="action">The Action to execute using the UI thread.</param>
        /// <returns>Returns true if the task has been finished successfully. Returns false if an exception has been thrown.</returns>
        public static bool TryMarshalTaskAndWait(Action action)
        {
            try
            {
                MarshalTaskAndWait(action);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
#else
        // ReSharper disable once MemberCanBePrivate.Global
        public static void MarshalTask(Action action)
        {
            action();
        }

        /// <summary>
        /// Sends an Action to be executed by the UI thread and waits until it has been finished.
        /// </summary>
        /// <param name="action">The Action to execute using the UI thread.</param>
        // ReSharper disable once MemberCanBePrivate.Global
        public static void MarshalTaskAndWait(Action action)
        {
            action();
        }

        /// <summary>
        /// Sends an Action to be executed by the UI thread and waits until it has been finished. Catches if there is an exception and returns a bool if there was one.
        /// </summary>
        /// <param name="action">The Action to execute using the UI thread.</param>
        /// <returns>Returns true if the task has been finished successfully. Returns false if an exception has been thrown.</returns>
        public static bool TryMarshalTaskAndWait(Action action)
        {
            try
            {
                MarshalTaskAndWait(action);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
#endif
    }
}