// ReSharper disable UnusedMember.Global
using System.Collections.ObjectModel;

namespace KinoshitaProductions.Emvvm.Base
{
    public class AdvancedObservableCollection<T> : ObservableCollection<T> where T : IEquatable<T>
    {
        /**
         * This event allows Android to receive and handle updates properly, by calling NotifyDataSetChanged
         */
        bool _thereWereChanges;

        public AdvancedObservableCollection() {
            base.CollectionChanged += (_, _) => { _thereWereChanges = true; };
        }

        public event Action? Updated;

        public void NotifyUpdated()
        {
            if (_thereWereChanges)
            {
                Updated?.Invoke();
                _thereWereChanges = false;
            }
        }

        public void SmartReplace(IEnumerable<T> newItems)
        {
            int currentIndex = 0;
            bool foundAllMatchingItems = false;
            bool swappedAllPossibleItems = false;
            int itemsFound = 0;
            foreach (var item in newItems)
            {
                ++itemsFound;
                if (!foundAllMatchingItems)
                {
                    if (currentIndex < Items.Count && Items[currentIndex].Equals(item))
                    {
                        ++currentIndex;
                        continue; // DO NOTHING, WE ALREADY HAVE THE SAME ITEM!!
                    }
                    foundAllMatchingItems = true;
                }
                //let's replace items in any slot available
                if (!swappedAllPossibleItems)
                {
                    if (currentIndex < Items.Count)
                    {
                        SetItem(currentIndex, item);
                        ++currentIndex;
                        continue;
                    }
                    else
                        swappedAllPossibleItems = true;
                }
                //and, if there are items left to add, let's add them
                Add(item);
            }

            if (itemsFound == 0)
                Clear();
            while (Items.Count > itemsFound)
                RemoveAt(currentIndex);
        }
    }
}
