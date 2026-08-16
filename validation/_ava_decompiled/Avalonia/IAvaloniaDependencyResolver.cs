using System;
using Avalonia.Metadata;

namespace Avalonia;

[PrivateApi]
public interface IAvaloniaDependencyResolver
{
	object? GetService(Type t);
}
