using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace KinoshitaProductions.Emvvm.Helpers;

public static class BindingHelper
{    private static void RemoveItems<TItem, TBinding>(List<TBinding> bindings, NotifyCollectionChangedEventArgs e,
        Func<TItem,
                IEnumerable<(
                    ObservableObject item,
                    PropertyChangedEventHandler item_PropertyChanged
                    )
                >
            >
            itemAndHandlerSelectorFunction,
        List<TBinding> removedBindingsTracker)
        where TItem : ObservableObject
        where TBinding : IBinding, new()
    {
        if (e.OldItems == null) return;
        foreach (TItem item in e.OldItems)
        {
            var selectedItemsAndHandlers =
                itemAndHandlerSelectorFunction(item);
            foreach (var
                         selectedItemAndHandler
                     in
                     selectedItemsAndHandlers
                    )
                selectedItemAndHandler
                        .item
                        .PropertyChanged -=
                    selectedItemAndHandler
                        .item_PropertyChanged;

            var binding = bindings.Find(x => x.BoundItem == item);
            if (binding != null)
            {
                // track binding
                removedBindingsTracker.Add(binding);

                //remove binding
                bindings.Remove(binding);
            }
        }
    }
    private static void AddItems<TItem, TBinding>(List<TBinding> bindings, NotifyCollectionChangedEventArgs e,
        Func<TItem,
                IEnumerable<(
                    ObservableObject item,
                    PropertyChangedEventHandler item_PropertyChanged
                    )
                >
            >
            itemAndHandlerSelectorFunction,
        Func<TItem, TBinding>? bindingClassSelector,
        Func<TBinding, TBinding>? onCreate = null)
        where TItem : ObservableObject
        where TBinding : IBinding, new()
    {
        if (e.NewItems == null) return;
        foreach (TItem item in e.NewItems)
        {
            var selectedItemsAndHandlers =
                itemAndHandlerSelectorFunction(item);
            foreach (var
                         selectedItemAndHandler
                     in
                     selectedItemsAndHandlers
                    )
                selectedItemAndHandler
                        .item
                        .PropertyChanged +=
                    selectedItemAndHandler
                        .item_PropertyChanged;

            TBinding binding;
            if (bindingClassSelector != null) binding = bindingClassSelector.Invoke(item);
            else if (onCreate != null) binding = onCreate(new TBinding());
            else binding = new TBinding();
            
            binding.BoundItem = item;

            //register binding
            bindings.Add(binding);
        }
    }

    /**
         * The following function is used to auto-bind items. An selector may be specified to add a list of listeners. 
         */
    // ReSharper disable once MemberCanBePrivate.Global
    public static void UpdateBindingsFor<TItem, TBinding>(
        ObservableCollection<TItem> items,
        NotifyCollectionChangedEventArgs e,
        List<TBinding> bindings,
        Func<TItem,
                IEnumerable<(
                    ObservableObject item,
                    PropertyChangedEventHandler item_PropertyChanged
                    )
                >
            >
            itemAndHandlerSelectorFunction,
        Func<TItem, TBinding>? bindingClassSelector = null,
        Func<TBinding, TBinding>? onCreate = null
    )
        where TItem : ObservableObject
        where TBinding : IBinding, new()
    {
        var removedBindings = new List<TBinding>();

        switch (e.Action)
        {
            case NotifyCollectionChangedAction
                .Add:
                AddItems(bindings, e, itemAndHandlerSelectorFunction, bindingClassSelector, onCreate);

                break;
            case NotifyCollectionChangedAction
                .Remove:
                RemoveItems(bindings, e, itemAndHandlerSelectorFunction, removedBindings);

                break;
            case NotifyCollectionChangedAction
                .Replace:
                RemoveItems(bindings, e, itemAndHandlerSelectorFunction, removedBindings);
                AddItems(bindings, e, itemAndHandlerSelectorFunction, bindingClassSelector, onCreate);

                break;
            case NotifyCollectionChangedAction
                .Move:
                break;
            case NotifyCollectionChangedAction
                .Reset:
                foreach (var binding in bindings)
                {
                    var item = (TItem)binding.BoundItem; // recover from here
                    var selectedItemsAndHandlers =
                        itemAndHandlerSelectorFunction(item);
                    foreach (var
                                 selectedItemAndHandler
                             in
                             selectedItemsAndHandlers
                            )
                        selectedItemAndHandler
                                .item
                                .PropertyChanged -=
                            selectedItemAndHandler
                                .item_PropertyChanged;
                }

                // track bindings
                removedBindings = bindings.Select(x => x).ToList();

                // clear bindings
                bindings.Clear();
                break;
        }

        if (
            e.Action ==
            NotifyCollectionChangedAction
                .Remove ||
            e.Action == NotifyCollectionChangedAction.Reset
        )
            foreach (var binding in removedBindings)
                binding.Dispose();
    }

    // ReSharper disable once UnusedMember.Global
    public static void UpdateBindingsFor<TItem, TBinding>(
        ObservableCollection<TItem> items,
        NotifyCollectionChangedEventArgs e,
        List<TBinding> bindings,
        PropertyChangedEventHandler itemPropertyChanged,
        Func<TItem, TBinding>? bindingClassSelector = null
    )
        where TItem : ObservableObject
        where TBinding : IBinding, new()
    {
        UpdateBindingsFor(items,
            e,
            bindings,
            item => new[] { ((ObservableObject)item, item_PropertyChanged: itemPropertyChanged) });
    }


     private static void AddPropertyChanged<TBinding, TItem>(NotifyCollectionChangedEventArgs e,
        Func<TBinding,
                IEnumerable<(
                    TItem item,
                    PropertyChangedEventHandler item_PropertyChanged
                    )
                >
            >
            itemAndHandlerSelectorFunction)
        where TItem : ObservableObject
    {
        if (e.NewItems == null) return;
        foreach (TBinding item in e.NewItems)
        {
            var selectedItemsAndHandlers =
                itemAndHandlerSelectorFunction(item);
            foreach (var
                         selectedItemAndHandler
                     in
                     selectedItemsAndHandlers
                    )
                selectedItemAndHandler.item.PropertyChanged +=
                    selectedItemAndHandler.item_PropertyChanged;
        }
    }

    private static void RemovePropertyChanged<TBinding, TItem>(NotifyCollectionChangedEventArgs e,
        Func<TBinding,
                IEnumerable<(
                    TItem item,
                    PropertyChangedEventHandler item_PropertyChanged
                    )
                >
            >
            itemAndHandlerSelectorFunction)
        where TItem : ObservableObject
    {
        if (e.OldItems == null) return;
        foreach (TBinding item in e.OldItems)
        {
            var selectedItemsAndHandlers =
                itemAndHandlerSelectorFunction(item);
            foreach (var
                         selectedItemAndHandler
                     in
                     selectedItemsAndHandlers
                    )
                selectedItemAndHandler.item.PropertyChanged -=
                    selectedItemAndHandler.item_PropertyChanged;
        }
    }

    // ReSharper disable once UnusedMember.Global
    public static void AddPropertyChangedEventHandlersFor<TItem>(
        NotifyCollectionChangedEventArgs e,
        PropertyChangedEventHandler itemPropertyChanged
    )
        where TItem : ObservableObject
    {
        AddPropertyChangedEventHandlersFor<TItem>(e,
            item => new (ObservableObject, PropertyChangedEventHandler)[] { (item, itemPropertyChanged) });
    }

    /**
         * The following function is used to auto-bind items. An selector may be specified to add a list of listeners. 
         */
    // ReSharper disable once MemberCanBePrivate.Global
    public static void AddPropertyChangedEventHandlersFor<TItem>(
        NotifyCollectionChangedEventArgs e,
        Func<TItem,
                IEnumerable<(
                    ObservableObject item,
                    PropertyChangedEventHandler item_PropertyChanged
                    )
                >
            >
            itemAndHandlerSelectorFunction
    )
        where TItem : ObservableObject
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction
                .Add:
                AddPropertyChanged(e, itemAndHandlerSelectorFunction);

                break;
            case NotifyCollectionChangedAction
                .Remove:
                RemovePropertyChanged(e, itemAndHandlerSelectorFunction);

                break;
            case NotifyCollectionChangedAction
                .Replace:
                RemovePropertyChanged(e, itemAndHandlerSelectorFunction);
                AddPropertyChanged(e, itemAndHandlerSelectorFunction);

                break;
            case NotifyCollectionChangedAction
                .Move:
                break;
            case NotifyCollectionChangedAction
                .Reset:
                break;
        }
    }
}