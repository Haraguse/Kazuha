using System.Threading;
using Avalonia.Metadata;

namespace Avalonia.Threading;

[PrivateApi]
public interface IControlledDispatcherImpl : IDispatcherImplWithPendingInput, IDispatcherImpl
{
	void RunLoop(CancellationToken token);
}
