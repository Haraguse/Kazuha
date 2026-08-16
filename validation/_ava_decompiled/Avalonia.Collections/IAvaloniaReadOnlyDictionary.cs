using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Avalonia.Collections;

public interface IAvaloniaReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, INotifyCollectionChanged, INotifyPropertyChanged where TKey : notnull
{
}
