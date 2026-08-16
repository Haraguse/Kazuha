namespace Avalonia.Threading;

internal class PriorityChain
{
	public DispatcherPriority Priority { get; set; }

	public int Count { get; set; }

	public DispatcherOperation? Head { get; set; }

	public DispatcherOperation? Tail { get; set; }

	public PriorityChain(DispatcherPriority priority)
	{
		Priority = priority;
	}
}
