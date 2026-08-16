using System;

namespace Avalonia.Utilities;

/// <summary>
/// A ref-counted wrapper for a disposable object.
/// </summary>
/// <typeparam name="T"></typeparam>
internal interface IRef<out T> : IDisposable where T : class
{
	/// <summary>
	/// The item that is being ref-counted.
	/// </summary>
	T Item { get; }

	/// <summary>
	/// Gets whether the reference still tracks a valid item.
	/// </summary>
	bool IsAlive { get; }

	/// <summary>
	/// The current refcount of the object tracked in this reference. For debugging/unit test use only.
	/// </summary>
	int RefCount { get; }

	/// <summary>
	/// Create another reference to this object and increment the refcount.
	/// </summary>
	/// <returns>A new reference to this object.</returns>
	IRef<T> Clone();

	/// <summary>
	/// Create another reference to the same object, but cast the object to a different type.
	/// </summary>
	/// <typeparam name="TResult">The type of the new reference.</typeparam>
	/// <returns>A reference to the value as the new type but sharing the refcount.</returns>
	IRef<TResult> CloneAs<TResult>() where TResult : class;
}
