using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Avalonia.Threading;

/// <summary>
///     A simple awaitable type that will return a DispatcherPriorityAwaiter.
/// </summary>
[UnconditionalSuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "This struct is not supposed to be used directly and should not be compared.")]
public struct DispatcherPriorityAwaitable
{
	private readonly Dispatcher _dispatcher;

	private readonly Task? _task;

	private readonly DispatcherPriority _priority;

	internal DispatcherPriorityAwaitable(Dispatcher dispatcher, Task? task, DispatcherPriority priority)
	{
		_dispatcher = dispatcher;
		_task = task;
		_priority = priority;
	}

	public DispatcherPriorityAwaiter GetAwaiter()
	{
		return new DispatcherPriorityAwaiter(_dispatcher, _task, _priority);
	}
}
/// <summary>
///     A simple awaitable type that will return a DispatcherPriorityAwaiter&lt;T&gt;.
/// </summary>
[UnconditionalSuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "This struct is not supposed to be used directly and should not be compared.")]
public struct DispatcherPriorityAwaitable<T>
{
	private readonly Dispatcher _dispatcher;

	private readonly Task<T> _task;

	private readonly DispatcherPriority _priority;

	internal DispatcherPriorityAwaitable(Dispatcher dispatcher, Task<T> task, DispatcherPriority priority)
	{
		_dispatcher = dispatcher;
		_task = task;
		_priority = priority;
	}

	public DispatcherPriorityAwaiter<T> GetAwaiter()
	{
		return new DispatcherPriorityAwaiter<T>(_dispatcher, _task, _priority);
	}
}
