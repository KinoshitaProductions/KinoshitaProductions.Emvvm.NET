namespace KinoshitaProductions.Emvvm.Interfaces
{
    // ReSharper disable once InconsistentNaming
    public interface IUIBindingV2<TItem> : IBindingV2<TItem>
    {
        bool CanBeUsed { get; }
        bool NeedsView { get; }
    }
}
