using System.Collections.Concurrent;
using Serilog;
using Newtonsoft.Json.Linq;

namespace KinoshitaProductions.Emvvm.Services;

public static class OperationsManager
{
    private sealed class Operation : IDisposable
    {
        internal object? Sender { get; private set; }
        internal object? Parameter { get; private set; } /* required lately to properly identify cancellations by sender, since we used to send parameter as sender which was very expensive */
        internal Func<object?, object?, Task>? Action { get; private set; }
        internal DateTime? CanStartAt { get; set; }
        internal DateTime TimeoutAt { get; set; }
        internal OperationStatus OperationStatus { get; set; } = OperationStatus.Waiting;
        internal bool IsDisposed => Action == null;
        internal bool HasDelay { get; private set;  }
        internal bool IsCancelled { get; set; }
        internal bool IsFaultedOrCompleted => OperationStatus == OperationStatus.Faulted || OperationStatus == OperationStatus.Completed;

        public void Dispose()
        {
            Sender = null;
            Parameter = null;
            Action = null;
            CanStartAt = null;
            GC.SuppressFinalize(this);
        }

        internal Operation(object? sender, object? parameter, Func<object?, object?, Task> action, TimeSpan delayFor)
        {
            Sender = sender;
            Parameter = parameter;
            Action = action;
            if (delayFor > TimeSpan.Zero)
                CanStartAt = DateTime.Now + delayFor;
            HasDelay = delayFor.TotalMilliseconds > 0;
        }
        ~Operation()
        {
            Dispose();
        }
    }
    
    private sealed class ChainedTask
    {
        internal TimeSpan Delay;
        internal object? Sender;
        internal object? Parameter;
        internal Func<object?, object?, Task>? Action;
    }
    
    public static void Configure(int maxThreads, int maxConcurrentOperations)
    {
        _maxThreads = maxThreads;
        _maxConcurrentOperations = maxConcurrentOperations;
        if (_threads.Length >= _maxThreads) return; // already on limit, do nothing
        ExpandSemaphores();
        ExpandThreads();
    }
    
    private static int _maxThreads = 2;
    private static int _maxConcurrentOperations = 2;
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<Operation>> OperationsQueues = new ();
    private static readonly List<Operation> PendingOperations = new ();
    private static readonly List<Operation> ExecutingOperations = new ();
    private static readonly List<Operation> ImmediateOperations = new ();
    private static readonly ConcurrentQueue<Operation> QueuedToBePending = new ();
    private static readonly List<ConcurrentBag<Func<Task>>> PendingTasks = new (); // this is the legacy option
    private static readonly SemaphoreSlim PendingOperationsSemaphore = new (1, 1);
    private static Thread[] _threads = Array.Empty<Thread>();
    private static SemaphoreSlim[] _threadsSemaphores = Array.Empty<SemaphoreSlim>();

    private static void ExpandSemaphores()
    {
        var creatingSemaphores = _threadsSemaphores.ToList();
        while (creatingSemaphores.Count < _maxThreads)
        {
            creatingSemaphores.Add(new SemaphoreSlim(0));
        }
        _threadsSemaphores = creatingSemaphores.ToArray();
    }

    private static async Task RunThread(ConcurrentBag<Func<Task>> pendingTasks, SemaphoreSlim threadSemaphore)
    {
        try
        {
            while (true)
            {
                await threadSemaphore.WaitAsync();
                if (pendingTasks.TryTake(out var operation))
                    await operation().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to grow background threads");
        }
    }
    
    private static void ExpandThreads()
    {
        var creatingThreads = _threads.ToList();
        while (creatingThreads.Count < _maxThreads)
        {
            var number = creatingThreads.Count;
            // it might be best to just use Task.Run, but needs testing
            // ReSharper disable once AsyncVoidLambda
            var creatingThread = new Thread(async () =>
            {
                while (true)
                {
                   await RunThread(PendingTasks[number], _threadsSemaphores[number]);
                }
                // ReSharper disable once FunctionNeverReturns
            });
            PendingTasks.Add(new ConcurrentBag<Func<Task>>());
            creatingThreads.Add(creatingThread);
            creatingThread.IsBackground = true;
            creatingThread.Start();
        }
        _threads = creatingThreads.ToArray();
    }

    private static bool CheckIfQueueBlocked(ConcurrentQueue<Operation> queue)
    {
        bool queueBlocked = false;
        if (queue.TryPeek(out var peekedOperation))
        {
            switch (peekedOperation.OperationStatus)
            {
                // If operation complete, unlock queue
                case OperationStatus.Completed:
                case OperationStatus.Faulted:
                    if (queue.TryDequeue(out peekedOperation))
                        peekedOperation.Dispose();

                        // waiting to begin execution
                    queueBlocked = false;
                    break;
                // If operation taking too long, unlock queue
                case OperationStatus.Running:
                    // Timeout: check if disposed or running for more than 15 seconds
                    if (peekedOperation.IsDisposed || (DateTime.Now - peekedOperation.CanStartAt) >= TimeSpan.FromSeconds(15))
                    {
                        if (queue.TryDequeue(out peekedOperation))
                            peekedOperation.Dispose();
                        // task has timed out, so we unblock it and run next one
                        queueBlocked = false;
                    }
                    else
                    {
                        // tasks still running on this queue, so it's still blocked
                        queueBlocked = true;
                    }

                    break;
                case OperationStatus.Waiting:
                    // waiting to begin execution
                    queueBlocked = false;
                    break;
            }
        }
        return queueBlocked;
    }

    private static int roundRobinCounter = 0;
    private static int GetAvailableThreadNumber()
    {
        var targetThread = 0;
        var targetLoad = int.MaxValue;
        for (var checkingThread = 0; checkingThread < _threads.Length; ++checkingThread)
        {
            var checkingLoad = _threadsSemaphores[checkingThread].CurrentCount;
            if (checkingLoad < targetLoad)
            {
                targetThread = checkingThread; // find optimal
                targetLoad = checkingLoad;
            }

            if (checkingLoad == 0)
            {
                // try to round robin if all are 0
                var roundRobinThread = (checkingThread + roundRobinCounter) % _threads.Length;
                var roundRobinLoad = _threadsSemaphores[roundRobinThread].CurrentCount;
                if (roundRobinThread != checkingThread && roundRobinLoad <= checkingLoad)
                    targetThread = roundRobinThread;
                ++roundRobinCounter;
                // no need to look up further, we found an empty thread
                break;
            }
        }
        return targetThread;
    }

    private static void IngestPendingQueuedOperation(Operation nextOperation)
    {
        if (nextOperation.IsCancelled) return;
        //nextOperation.ExecuteUntil ??= DateTime.Now; // set start time if missing
        // Check if it's time for operation to execute
        if (nextOperation.CanStartAt == null || nextOperation.CanStartAt <= DateTime.Now)
        {
            // Update status as "running" and transfer it for execution
            nextOperation.OperationStatus = OperationStatus.Running;

            // Select thread for execution
            var targetThread = GetAvailableThreadNumber();

            // Assign to thread for execution
            PendingTasks[targetThread].Add(async () =>
            {
                if (nextOperation.IsCancelled) return;
                // Try running it
                try
                {
                    if (nextOperation.Action != null)
                        await nextOperation.Action.Invoke(nextOperation.Sender, nextOperation.Parameter);
                    nextOperation.OperationStatus = OperationStatus.Completed;
                }
                catch (Exception ex)
                {
                    // If faulted, mark as faulted
                    nextOperation.OperationStatus = OperationStatus.Faulted;
                    Log.Error(ex, "Failed to execute queued pending operation");
                }
            });
            _threadsSemaphores[targetThread].Release();
        }
    }
    
    /// <summary>
    /// Executes pending, non thread-safe critical operations, e.g. Saving settings (writing file)
    /// </summary>
    /// <returns>Count of operations pending in queue.</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public static int ExecutePendingQueuedOperations()
    {
        int workRemaining = 0;
        try
        {
            // Loop queues by name (e.g. Default (""), Settings, Cache, etc.)
            foreach (var queue in OperationsQueues.Values)
            {
                workRemaining += queue.Count;
                // Check for "completed" operations, to unblock queues
                if (CheckIfQueueBlocked(queue)) continue;

                // Check if there are operations pending for this queue
                if (queue.TryPeek(out var nextOperation))
                {
                    // If there is an operation pending for this queue, check the status
                    switch (nextOperation.OperationStatus)
                    {
                        // If operation is waiting to start, start it and update status
                        case OperationStatus.Waiting:
                            IngestPendingQueuedOperation(nextOperation);
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Starting queued operation failed ");
        }
        return workRemaining;
    }

    private static void RemoveTimedOutOperations()
    {
        lock (ExecutingOperations)
        {
            for (int checking = 0; checking < ExecutingOperations.Count; ++checking)
            {
                if (/*ExecutingOperations[checking].TimeoutAt != null &&*/
                    ExecutingOperations[checking].TimeoutAt < DateTime.Now)
                {
                    ExecutingOperations.RemoveAt(checking); // timed out
                    --checking;
                }
            }
        }
    }

    private static bool CheckIfAlreadyExecutingOperationForSender(object? sender)
    {
        if (sender == null) return false;
        lock (ExecutingOperations)
            if (ExecutingOperations.Exists(operation => sender.Equals(operation.Sender)))
                return true; //denied, already trying something

        lock (ImmediateOperations)
            if (ImmediateOperations.Exists(operation => sender.Equals(operation.Sender)))
                return true; //denied, already trying something

        lock (PendingOperations)
            if (PendingOperations.Exists(operation => sender.Equals(operation.Sender)))
                return true; //denied, already trying something
        return false;
    }
    
    private static void IngestToBePendingOperations()
    {
        while (QueuedToBePending.TryDequeue(out var nextOperation))
        {
            if (!nextOperation.HasDelay && CheckIfAlreadyExecutingOperationForSender(nextOperation.Sender)) /* if null, we don't need to validate duplicity*/ // If we will retry, it is possible that is denied since we'd be requesting retry from executingOperation, and thus, //denied, already trying something
                continue;

            lock (PendingOperations)
                PendingOperations.Add(nextOperation);
        }
    }

    private static void CleanupCompletedOperations()
    {
        lock (ExecutingOperations)
        {
            for (int i = 0; i < ExecutingOperations.Count; ++i)
            {
                if (ExecutingOperations[i].IsFaultedOrCompleted)
                    ExecutingOperations.RemoveAt(i--);
            }
        }
    }
    
    /// <summary>
    /// Executes pending, thread-safe operations, e.g. loading or decoding an image.
    /// </summary>
    /// <returns>Count of operations pending.</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public static async Task<int> ExecutePendingOperations()
    {
        await PendingOperationsSemaphore.WaitAsync();
        try
        {
            // Check for new operations to enqueue

            // remove pending on timeout
            RemoveTimedOutOperations();

            // check if there are new operations to add to queue
            IngestToBePendingOperations();

            // Check for completed operations, to remove from list
            CleanupCompletedOperations();

            // Check for pending operations, to execute (as long the limit hasn't been reached)
            if (ShouldRunMorePendingOperations)
            {
                // if should run more operations, take one from the queue
                lock (PendingOperations)
                {
                    while (ShouldRunMorePendingOperations && PendingOperations.Any())
                    {
                        var operation = PendingOperations.ElementAtOrDefault(0);
                        PendingOperations.RemoveAt(0);
                        // Check for cancellations
                        if (operation == null || operation.IsCancelled)
                            continue; // do nothing, this task was cancelled

                        // Set timeout
                        operation.TimeoutAt = DateTime.Now.AddSeconds(30); // 30 seconds timeout

                        // Register as executing
                        lock (ExecutingOperations)
                            ExecutingOperations.Add(operation);

                        // ingest
                        IngestPendingQueuedOperation(operation);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to run background tasks");
        }
        finally
        {
            PendingOperationsSemaphore.Release();
        }

        lock (PendingOperations)
        {
            return PendingOperations.Count;
        }
    }

    private static bool ShouldRunMorePendingOperations 
    {
        get 
        {
            lock (PendingOperations)
                if (!PendingOperations.Any())
                    return false;
            lock (ExecutingOperations)
                return ExecutingOperations.Count < _maxConcurrentOperations;
        } 
    }
    
    // ReSharper disable once UnusedMember.Global
    public static async Task<int> ExecuteIfPendingAndNotBusy()
    {
        int workRemaining = 0;
        workRemaining += ExecutePendingQueuedOperations();
        workRemaining += await ExecutePendingOperations();

        return workRemaining;
    }

    private static async Task AssignThreadForImmediateOperation(Operation nextOperation, bool executeEvenIfAlreadyRunningOne = true)
    {
        if (!executeEvenIfAlreadyRunningOne)
            await PendingOperationsSemaphore.WaitAsync();

        try
        {
            if (!executeEvenIfAlreadyRunningOne && nextOperation.Sender != null)
            {
                /* if null, we don't need to validate duplicity*/
                lock (PendingOperations)
                    if (PendingOperations.Exists(operation => nextOperation.Sender.Equals(operation.Sender)))
                        return; //denied, already trying something
                lock (ExecutingOperations)
                    if (ExecutingOperations.Exists(operation => nextOperation.Sender.Equals(operation.Sender)))
                        return; //denied, already trying something
                lock (ImmediateOperations)
                    if (ImmediateOperations.Exists(operation => nextOperation.Sender.Equals(operation.Sender)))
                        return; //denied, already trying something
            }
            lock (ImmediateOperations)
                ImmediateOperations.Add(nextOperation);
            if (!executeEvenIfAlreadyRunningOne)
                PendingOperationsSemaphore.Release(); // release semaphore, since we'd be holding the lock too long for task execution

            try
            {
                if (nextOperation.Action != null)
                    await nextOperation.Action.Invoke(nextOperation.Sender, nextOperation.Parameter);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to assign thread for immediate operation");
            }

            if (!executeEvenIfAlreadyRunningOne)
                await PendingOperationsSemaphore.WaitAsync(); // get semaphore again before removing the item from the immediateOperations list
            lock (ImmediateOperations)
            {
                ImmediateOperations.Remove(nextOperation);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to run background task");
        }
        finally
        {
            if (!executeEvenIfAlreadyRunningOne)
                PendingOperationsSemaphore.Release();
        }
    }

    private static async Task RunDelayedOperation(object? sender, object? parameter)
    {
        var chainedTask = parameter as ChainedTask;
        if (chainedTask?.Action != null)
        {
            await Task.Delay(chainedTask.Delay);
            await chainedTask.Action.Invoke(chainedTask.Sender, chainedTask.Parameter);
        }
    }

    public static void AddImmediateOperation(TimeSpan delay, object sender, object parameter, Func<object?, object?, Task> action, bool executeEvenIfAlreadyRunningOne = true)
    {
        AddImmediateOperation(sender, new ChainedTask { Delay = delay, Sender = sender, Parameter = parameter, Action = action }, RunDelayedOperation, executeEvenIfAlreadyRunningOne);
    }
    /// <summary>
    /// Adds and executes without waiting in queue. This will block other requests from coming meanwhile it's executed.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="action"></param>
    // <returns>Returns true if the operation is executed.</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public static void AddImmediateOperation(object sender, object parameter, Func<object?, object?, Task> action, bool executeEvenIfAlreadyRunningOne = true)
    {
        try
        {
            Operation nextOperation = new Operation(sender, parameter, action, TimeSpan.Zero);
            // Select thread for execution
            var targetThread = GetAvailableThreadNumber();
            // Assign to thread for execution
            PendingTasks[targetThread].Add(async () =>
            {
                await AssignThreadForImmediateOperation(nextOperation, executeEvenIfAlreadyRunningOne).ConfigureAwait(false);
            });
            _threadsSemaphores[targetThread].Release();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to run immediate operation");
        }
    }
    
    // ReSharper disable once MemberCanBePrivate.Global
    public static void AddQueuedPendingOperation(string queueName, object sender, object parameter, Func<object?, object?, Task> action, TimeSpan? delayFor = null)
    {
        if (!OperationsQueues.TryGetValue(queueName, out var queue))
        {
            queue = new ConcurrentQueue<Operation>();
            OperationsQueues.TryAdd(queueName, queue);
        }
        queue.Enqueue(new Operation(sender, parameter, action, delayFor ?? TimeSpan.Zero));
    }

    public static void AddQueuedPendingOperation(object sender, object parameter, Func<object?, object?, Task> action,
        TimeSpan? delayFor = null) => AddQueuedPendingOperation(string.Empty, sender, parameter, action, delayFor);
    
    // ReSharper disable once UnusedMember.Global
    public static void AddPendingOperation(object sender, object parameter, Func<object?, object?, Task> action, TimeSpan? delayFor = null)
    {
        Operation nextOperation = new Operation(sender, parameter, action, delayFor ?? TimeSpan.Zero);

        QueuedToBePending.Enqueue(nextOperation);
    }

    // ReSharper disable once UnusedMember.Global
    public static void CancelIfHasPendingOperation(object? sender)
    {
        if (sender == null)
            return; // can't match sender, or no operations cancellable
        lock (QueuedToBePending)
            foreach (var operation in QueuedToBePending.Where(operation => !operation.IsCancelled && sender.Equals(operation.Sender)))
                operation.IsCancelled = true;
        lock (PendingOperations) 
            foreach (var operation in PendingOperations.Where(operation => !operation.IsCancelled && sender.Equals(operation.Sender)))
                operation.IsCancelled = true;
    }
}