namespace KinoshitaProductions.Emvvm.Enums
{
    /// <summary>
    /// Describes briefly the current status of the engine.
    /// </summary>
    public enum EngineStatusCode
    {
        /// <summary>
        /// The engine has NEVER started.
        /// </summary>
        NotStarted,

        /// <summary>
        /// The engine is idle (either low load or waiting for load).
        /// </summary>
        Idle,

        /// <summary>
        /// The engine is running and processing data.
        /// </summary>
        Running,

        /// <summary>
        /// The engine has been stopped.
        /// </summary>
        Stopped,

        /// <summary>
        /// The engine encountered and error and stopped.
        /// </summary>
        Faulted
    }
}
