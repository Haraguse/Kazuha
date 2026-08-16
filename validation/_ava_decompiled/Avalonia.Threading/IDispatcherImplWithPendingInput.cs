using Avalonia.Metadata;

namespace Avalonia.Threading;

[PrivateApi]
public interface IDispatcherImplWithPendingInput : IDispatcherImpl
{
	bool CanQueryPendingInput { get; }

	bool HasPendingInput { get; }
}
