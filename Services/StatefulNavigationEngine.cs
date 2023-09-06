using Newtonsoft.Json;

namespace KinoshitaProductions.Emvvm.Services
{
    /// <summary>
    /// Basic execution engine. Does only handle basic actions (start, stop, pause, resume, fail).
    /// </summary>
    public abstract class StatefulNavigationEngine: StatefulEngine
    {
        #region STATEFUL_NAVIGATION_ENGINE
        /// <summary>
        /// Activation depth from whenever it was created.
        /// </summary>
        [JsonProperty("ad")]
        public int ActivationDepth { get; private set; }
        /// <summary>
        /// Constructor intended for engine restoring (not creation)
        /// </summary>
        protected StatefulNavigationEngine()
        {
        }
        /// <summary>
        /// Constructor intended for new engine creation (not restoring).
        /// </summary>
        protected StatefulNavigationEngine(bool isRoot)
        {
            // we set it here, since it's 100% likely that it hasn't been assigned yet
            // since the splash is likely present, we avoid adding + 1, as we're replacing the root view model
            this.ActivationDepth = isRoot ? 1 : State.NavigationStackDepth + 1; 
            #pragma warning disable S1699
            // ReSharper disable once VirtualMemberCallInConstructor
            this.OnReadyToStart();
            #pragma warning restore S1699
        }
        /// <summary>
        /// Once the Engine has been created/restored completely, this function will be invoked (in order to notify other threads that's ready or let it be used)
        /// </summary>
        protected virtual void OnReadyToStart()
        {
        }
        public override void NotifyRestored()
        {
            throw new ArgumentException(
                "This method shouldn't be used for this engine type, please use NotifyRestored(int activationDepth) instead!");
        }
        public void NotifyRestored(int activationDepth)
        {
            this.ActivationDepth = activationDepth;
            this.OnReadyToStart();
            base.NotifyRestored();
        }
        #endregion
    }
}