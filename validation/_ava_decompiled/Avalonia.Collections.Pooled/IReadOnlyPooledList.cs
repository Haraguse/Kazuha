using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Collections.Pooled;

/// <summary>
/// Represents a read-only collection of pooled elements that can be accessed by index
/// </summary>
/// <typeparam name="T">The type of elements in the read-only pooled list.</typeparam>
internal interface IReadOnlyPooledList<T> : IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
{
	/// <summary>
	/// Gets a <see cref="T:System.ReadOnlySpan`1" /> for the items currently in the collection.
	/// </summary>
	ReadOnlySpan<T> Span { get; }
}
