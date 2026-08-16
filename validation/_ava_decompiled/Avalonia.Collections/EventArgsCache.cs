using System.Collections.Specialized;
using System.ComponentModel;

namespace Avalonia.Collections;

internal static class EventArgsCache
{
	internal static readonly PropertyChangedEventArgs CountPropertyChanged = new PropertyChangedEventArgs("Count");

	internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
}
