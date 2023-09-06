// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace KinoshitaProductions.Emvvm.Models;

public class MarshallerOptions
{
#if ANDROID
    public Func<Activity?> GetCurrentActivityFn { get; set; } = () => null;
#endif
}