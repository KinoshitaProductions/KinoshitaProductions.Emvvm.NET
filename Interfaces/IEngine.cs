// ReSharper disable TypeParameterCanBeVariant
namespace KinoshitaProductions.Emvvm.Interfaces
{
    /// <summary>
    /// Serves as interface for asynchronous engines.
    /// </summary>
    /// <remarks>
    /// Serves as interface for asynchronous engines such as NavigationEngine, SlideshowEngine and BatchDownloadEngine.
    /// </remarks>
    /// <typeparam name="TStatus">The status code type.</typeparam>
    public interface IEngine<TStatus>
    {
        /// <summary>
        /// Fired when execution fails. IEngine is the engine who sends the error. TStatus is the state before entering error.
        /// </summary>
        event Action<IEngine<TStatus>, TStatus>? OnExecutionFailed;

        /// <summary>
        /// Gets the engine's execution task.
        /// </summary>
        Thread? ExecutionThread { get; }
        
        /// <summary>
        /// Gets the status of the engine.
        /// </summary>
        TStatus Status { get; }

        /// <summary>
        /// Restarts the engine task.
        /// </summary>
        /// <returns>A task that ends when it finishes restarting. Returns true if succeed.</returns>
        Task<bool> RestartAsync();

        /// <summary>
        /// Starts the engine task.
        /// </summary>
        /// <returns>Returns true if succeed.</returns>
        bool Start(bool force = false);

        /// <summary>
        /// Stops the engine task.
        /// </summary>
        /// <returns>A task that ends when it finishes stopping. Returns true if succeed.</returns>
        Task<bool> StopAsync();
    }
}