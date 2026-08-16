using System;

namespace Avalonia.Reactive;

/// <summary>
/// Contains fields for <see cref="T:Avalonia.Reactive.AnonymousObserver`1" /> that aren't using generic arguments.
/// Separated to avoid unnecessary generic instantiations.
/// </summary>
internal static class AnonymousObserverNonGenericHelper
{
	public static readonly Action<Exception> ThrowsOnError = delegate(Exception ex)
	{
		throw ex;
	};

	public static readonly Action NoOpCompleted = delegate
	{
	};
}
