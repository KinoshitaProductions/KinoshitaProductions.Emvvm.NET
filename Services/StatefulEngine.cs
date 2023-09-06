using KinoshitaProductions.Common.Interfaces;
using Newtonsoft.Json;

namespace KinoshitaProductions.Emvvm.Services
{
    /// <summary>
    /// Basic execution engine. Does only handle basic actions (start, stop, pause, resume, fail).
    /// </summary>
    public abstract class StatefulEngine: Engine, IStatefulAsJson
    {
        private int _referencedByCount;
        public bool IsReferenced => _referencedByCount > 0;
        public void NotifyAsReferenced() => Interlocked.Increment(ref _referencedByCount);
        public void NotifyAsDereferenced() => Interlocked.Decrement(ref _referencedByCount);
        private DateTime _lastMaterializedAt = DateTime.Now.AddSeconds(4); // we block materialization for some extra seconds
        [JsonIgnore]
        public string? StateJson { get; set; }
        // ReSharper disable once MemberCanBePrivate.Global
        public bool IsMaterialized { get; private set; }
        /// <summary>
        /// If the current state has been saved, this will avoid to save it again.
        /// </summary>
        public void NotifyMaterialized()
        {
            _lastMaterializedAt = DateTime.Now;
            IsMaterialized = true;
        }

        /// <summary>
        /// If the current state has changed, this will request it to be saved again.
        /// </summary>
        public void InvalidateMaterialized() => IsMaterialized = false;
        public bool ShouldMaterialize => !IsMaterialized && _lastMaterializedAt.AddSeconds(3) < DateTime.Now; // do not materialize more than once every three seconds 
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        protected bool IsRestoring { get; private set; }
        /// <summary>
        /// Constructor intended for JSON deserialization.
        /// </summary>
        protected StatefulEngine()
        {
            this.IsRestoring = true;
        }
        public virtual void NotifyRestored()
        {
            this.IsRestoring = false;
        }
    }
}