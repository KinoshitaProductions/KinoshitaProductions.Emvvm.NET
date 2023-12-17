using Serilog;

namespace KinoshitaProductions.Emvvm.Services
{
    using Base;
    using Enums;
    using Interfaces;

    /// <summary>
    /// Basic execution engine. Does only handle basic actions (start, stop, pause, resume, fail).
    /// </summary>

    public abstract class Engine : ObservableObject, IEngine<EngineStatusCode>
    {
        protected Engine()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        ///  These reset flags have been exposed for engine management
        /// </summary>
        public bool IsResetting { get; set; }
        protected bool ResetRequested = false;
        public bool IsRunning => (Status == EngineStatusCode.Idle || Status == EngineStatusCode.Running) && ExecutionThread?.IsAlive == true;

        /// <summary>
        /// Fired when execution fails. IEngine is the engine who sends the error. TStatus is the state before entering error.
        /// </summary>
        public event Action<IEngine<EngineStatusCode>, EngineStatusCode>? OnExecutionFailed;

        /// <summary>
        /// Gets the engine's execution task.
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public Thread? ExecutionThread { get; protected set; }
        
        private EngineStatusCode _status = EngineStatusCode.NotStarted;

        /// <summary>
        /// Gets the engine's execution status code.
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public EngineStatusCode Status { get => _status; protected set => SetProperty(ref _status, value); }

        /// <summary>
        /// Restarts the engine task.
        /// </summary>
        /// <returns>A task that ends when it finishes restarting.</returns>
        public virtual async Task<bool> RestartAsync()
        {
            if (!await StopAsync())
                return false;
            return Start();
        }

        /// <summary>
        /// Starts the engine task.
        /// </summary>
        public virtual bool Start(bool force = false)
        {
            if (!force && (Status == EngineStatusCode.Idle && ExecutionThread != null))
            {
                // the task does nothing, but it exists, let's update it's state.
                Status = EngineStatusCode.Running;
                return true;
            }
            else if (force || (Status <= EngineStatusCode.Idle || Status >= EngineStatusCode.Stopped || ExecutionThread?.IsAlive != true))
            {
                // set the cancellation token source
                this.Abort(EngineStatusCode.Running);
                var cancellationToken = _cancellationTokenSource.Token;

                // if not: the task has not been set or it has been stopped, let's start a new task.
                Status = EngineStatusCode.Running;
                ExecutionThread = new Thread(() =>
                {
                    try
                    {
                        LoopAsync(cancellationToken).Wait(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Engine thread stopped");
                    }
                })
                {
                    IsBackground = true
                };
                ExecutionThread.Start();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Stops the engine task.
        /// </summary>
        /// <returns>A task that ends when it finishes stopping.</returns>
        public virtual async Task<bool> StopAsync()
        {
            if (Status is > EngineStatusCode.Idle and < EngineStatusCode.Stopped)
            {
                // if not: the task has not been set or it has been stopped, let's start a new task.
                this.Abort(EngineStatusCode.Stopped);

                while (ExecutionThread?.IsAlive == true)
                    await Task.Delay(1).ConfigureAwait(false);

                return true;
            }
            else
            {
                // else, throw error, this should have not been called now
                return false;
            }
        }

        /// <summary>
        /// Gets or sets the delay time in milliseconds of the engine until next tick.
        /// </summary>
#if WINDOWS_UWP
        // ReSharper disable once MemberCanBePrivate.Global
        protected int ActiveDelayTime { get; set; } = 7;
        // ReSharper disable once MemberCanBePrivate.Global
        protected int IdleDelayTime { get; set; } = 70;
#else
        // ReSharper disable once MemberCanBePrivate.Global
        protected int ActiveDelayTime { get; set; } = 10;
        // ReSharper disable once MemberCanBePrivate.Global
        protected int IdleDelayTime { get; set; } = 120;
#endif
        // ReSharper disable once MemberCanBePrivate.Global
        protected int RecoveryDelayTime { get; set; } = 3000; // 3 seconds

        /// <summary>
        /// Cancellation token source used to track and control cancellation of the task during execution
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        private int _runningThreadsCount;

        private async Task TickAsync(CancellationToken cancellationToken)
        {
            try
            {
                var remainingWorkload = await OnLoopTick(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    return; // cancelled halfway!
                if (remainingWorkload == 0)
                {
                    this.Status = EngineStatusCode.Idle;
                    await Task.Delay(IdleDelayTime, cancellationToken); // activeDelayTime is now unused
                }
                else
                {
                    this.Status = EngineStatusCode.Running;
                    await Task.Delay(ActiveDelayTime, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    return; // must continue, since we must check if there aren't threads waiting for this one to freeze
                Log.Error(ex, "Engine execution failed, sleeping before running again");
                OnExecutionFail();
                await Task.Delay(RecoveryDelayTime, cancellationToken); // freeze
                Log.Information("Engine execution failed, running again");
                Status = EngineStatusCode.Running; // restore
            }
        }

        /// <summary>
        /// Action executed by the engine's Task.
        /// </summary>
        /// <returns>A task that ends when the ExecutionTask stops.</returns>
        private async Task LoopAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _runningThreadsCount);
            // This task may be cancelled, so, it should have a way out of execution
            
            while (true)
            {
                // this must happen first, in order to release threads awaiting for freeze
                if (this._freezeRequested)
                    await this.WaitForUnfreezeAsync();
                bool stopped = false;
                switch (Status)
                {
                    case EngineStatusCode.Idle:
                    case EngineStatusCode.Running:
                        await TickAsync(cancellationToken);
                        break;
                    case EngineStatusCode.Stopped:
                    case EngineStatusCode.Faulted:
                    case EngineStatusCode.NotStarted:
                        stopped = true;
                        break;
                    default:
                        stopped = true;
                        break;
                }
                if (cancellationToken.IsCancellationRequested || stopped)
                    break;
            }
            Interlocked.Decrement(ref _runningThreadsCount);
        }

        /// <summary>
        /// Action executed by the engine's Task on execution error.
        /// </summary>
        private void OnExecutionFail()
        {
            var previousStatus = Status;
            Status = EngineStatusCode.Faulted;
            OnExecutionFailed?.Invoke(this, previousStatus);
        }

        /// <summary>
        /// Performs engine operations.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to stop the engine.</param>
        /// <returns>Amount of work performed (0 if idle, 1+ if a lot of things in queue).</returns>
        protected virtual Task<int> OnLoopTick(CancellationToken cancellationToken)
        {
            //does nothing
            return Task.FromResult(0);
        }

        /// <summary>
        /// This will notify the old task to cancel, and forcefully clear it's status
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        protected void Abort(EngineStatusCode nextStatusCode = EngineStatusCode.NotStarted)
        {
            try
            {
                _cancellationTokenSource.Cancel(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when aborting engine execution");
            }
            _cancellationTokenSource = new CancellationTokenSource();

            this.Status = nextStatusCode;
        }
        
        private readonly SemaphoreSlim _freezeSemaphore = new SemaphoreSlim(0);
        private bool _freezeRequested;
        protected bool IsFrozen => this._frozenThreadsCount > 0 && this._frozenThreadsCount == this._runningThreadsCount;
        protected bool ThreadConflict => this._runningThreadsCount > 1;
        private int _frozenThreadsCount;
        public async Task RequestFreezeAsync()
        {
            this._freezeRequested = true;
            // if the thread is dead/not running, don't wait
            if (this.ExecutionThread?.IsAlive != true)
                return; // nothing to do, literally

            int maxWaitTime = 300; // 3 seconds should be plenty of time for it
            while (this._runningThreadsCount != this._frozenThreadsCount && --maxWaitTime >= 0)
                await Task.Delay(10);
        }
        private Task WaitForUnfreezeAsync()
        {
            Interlocked.Increment(ref _frozenThreadsCount);
            // we must confirm here, to ensure that this can get released
            if (_freezeRequested)
                return Task.WhenAny(_freezeSemaphore.WaitAsync(), Task.Delay(5000));

            return Task.CompletedTask;
        }

        public void Unfreeze()
        {
            this._freezeRequested = false; // avoids other threads from freezing in the meantime
            while (this._frozenThreadsCount > 0)
            {
                _freezeSemaphore.Release();
                Interlocked.Decrement(ref _frozenThreadsCount);
            }
        }
    }
}