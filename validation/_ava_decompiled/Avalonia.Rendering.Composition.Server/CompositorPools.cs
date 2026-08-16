using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal class CompositorPools
{
	public class StackPool<T> : Stack<Stack<T>>
	{
		public Stack<T> Rent()
		{
			if (base.Count > 0)
			{
				return Pop();
			}
			return new Stack<T>();
		}

		public void Return(ref Stack<T> stack)
		{
			Return(stack);
			stack = null;
		}

		public void Return(Stack<T>? stack)
		{
			if (stack != null)
			{
				stack.Clear();
				Push(stack);
			}
		}
	}

	public StackPool<ServerCompositionVisual.TreeWalkerFrame> TreeWalkerFrameStackPool { get; } = new StackPool<ServerCompositionVisual.TreeWalkerFrame>();

	public StackPool<Matrix> MatrixStackPool { get; } = new StackPool<Matrix>();

	public StackPool<LtrbRect> LtrbRectStackPool { get; } = new StackPool<LtrbRect>();

	public StackPool<double> DoubleStackPool { get; } = new StackPool<double>();

	public StackPool<int> IntStackPool { get; } = new StackPool<int>();

	public StackPool<IDirtyRectCollector> DirtyRectCollectorStackPool { get; } = new StackPool<IDirtyRectCollector>();
}
