// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace KinoshitaProductions.Emvvm.Models
{
    public class AnimationState
    {
        private int _generation;
        private int _currentStep;
        /// <summary>
        /// Allows knowing an old animation that it has been cancelled.
        /// </summary>
        public int Generation => _generation;
        /// <summary>
        /// Allows knowing which step is currently executing.
        /// </summary>
        public int CurrentStep => _currentStep;
        /// <summary>
        /// Allows knowing how many steps there are.
        /// </summary>
        public int StepsCount { get; set; }
        /// <summary>
        /// Allows calculating next execution time.
        /// </summary>
        public TimeSpan StepDuration { get; set; }
        /// <summary>
        /// Allows waiting for next frame.
        /// </summary>
        public DateTime NextFrameAt { get; set; }

        public bool IsCompleted => CurrentStep < StepsCount;

        public async Task<bool> NextStepAsync(int generation)
        {
            if (_generation != generation || _currentStep >= StepsCount)
                return false;
            var upcomingFrameAt = NextFrameAt + StepDuration;
            var timeUntilNextStep = upcomingFrameAt - DateTime.Now;
            NextFrameAt = upcomingFrameAt;
            if (timeUntilNextStep > TimeSpan.Zero)
                await Task.Delay(timeUntilNextStep);
            if (_generation == generation)
                Interlocked.Increment(ref _currentStep);
            return _generation == generation && _currentStep < StepsCount;
        }

        public Task LastStepAsync(int generation)
        {
            if (_generation != generation || _currentStep >= StepsCount)
                return Task.CompletedTask;
            var upcomingFrameAt = NextFrameAt + StepDuration * (StepsCount - _currentStep - 1);
            var timeUntilNextStep = upcomingFrameAt - DateTime.Now;
            NextFrameAt = upcomingFrameAt;
            if (timeUntilNextStep > TimeSpan.Zero)
                return Task.Delay(timeUntilNextStep);
            _currentStep = StepsCount;
            return Task.CompletedTask;
        }

        public DateTime EndTime
        {
            get
            {
                var upcomingFrameAt = NextFrameAt + StepDuration * (StepsCount - _currentStep - 1);
                return upcomingFrameAt;
            }
        }

        public int Start()
        {
            var nextGen = Interlocked.Increment(ref _generation);
            Interlocked.Add(ref _currentStep, -_currentStep);
            NextFrameAt = DateTime.Now;
            return nextGen;
        }

        public void Cancel()
        {
            Interlocked.Increment(ref _generation);
        }
    }
}
