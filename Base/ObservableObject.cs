using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KinoshitaProductions.Emvvm.Base
{
    /// <summary>
    /// Observable object with INotifyPropertyChanged implemented
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        /// <summary>
        /// Sets the property.
        /// </summary>
        /// <returns><c>true</c>, if property was set, <c>false</c> otherwise.</returns>
        /// <param name="backingStore">Backing store.</param>
        /// <param name="value">Value.</param>
        /// <param name="propertyName">Property name.</param>
        /// <typeparam name="T">The first type parameter.</typeparam>
        protected bool
        SetProperty<T>(
            ref T backingStore,
            T value,
            [CallerMemberName] string propertyName = ""
        )
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged (propertyName);
            return true;
        }

        /// <summary>
        /// Occurs when property changed.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the property changed event.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        // ReSharper disable once MemberCanBePrivate.Global
        protected void OnPropertyChanged(
            [CallerMemberName] string propertyName = ""
        )
        {
            var changed = PropertyChanged;
            if (changed == null) return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
