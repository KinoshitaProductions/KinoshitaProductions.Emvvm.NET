namespace KinoshitaProductions.Emvvm.Interfaces
{
    public interface IBinding : IDisposable
    {
        bool IsBound { get; }
        ObservableObject BoundItem { get; set; }
        bool OwnsObject(object obj);
    }

    public interface IBinding<TItem> : IDisposable
    {
        bool IsBound { get; }
        TItem BoundItem { get; set; }
        bool OwnsObject(object obj);
    }
}
