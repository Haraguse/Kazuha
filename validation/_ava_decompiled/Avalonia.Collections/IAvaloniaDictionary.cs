using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Avalonia.Collections;

public interface IAvaloniaDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IAvaloniaReadOnlyDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, INotifyCollectionChanged, INotifyPropertyChanged, IDictionary, ICollection where TKey : notnull
{
}
