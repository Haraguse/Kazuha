using System;

namespace Avalonia.Platform;

public interface IScopedResource<T> : IDisposable
{
	T Value { get; }
}
