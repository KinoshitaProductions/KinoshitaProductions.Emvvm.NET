namespace KinoshitaProductions.Emvvm.Interfaces
{
    // ReSharper disable once InconsistentNaming
    public interface IUIBinding : IBinding
    {
        bool CanBeUsed();
        bool NeedsView();
    }

    // ReSharper disable once UnusedType.Global
    // ReSharper disable once InconsistentNaming
    public interface IUIBinding<TItem> : IBinding<TItem>
    {
        bool CanBeUsed();
        bool NeedsView();
    }
}
