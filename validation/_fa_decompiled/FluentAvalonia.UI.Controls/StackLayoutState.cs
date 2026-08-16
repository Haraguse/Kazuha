using System;

namespace FluentAvalonia.UI.Controls;

internal class StackLayoutState
{
	private double[] _estimationBuffer;

	private const int BufferSize = 100;

	public FlowLayoutAlgorithm FlowAlgorithm { get; private set; }

	public double TotalElementSize { get; private set; }

	public double MaxArrangeBounds { get; private set; }

	public int TotalElementsMeasured { get; private set; }

	public void InitializeForContext(FAVirtualizingLayoutContext context, IFlowLayoutAlgorithmDelegates callbacks)
	{
		if (FlowAlgorithm == null)
		{
			FlowLayoutAlgorithm flowLayoutAlgorithm = (FlowAlgorithm = new FlowLayoutAlgorithm());
		}
		FlowAlgorithm.InitializeForContext(context, callbacks);
		if (_estimationBuffer == null)
		{
			_estimationBuffer = new double[100];
		}
		context.LayoutStateCore = this;
	}

	public void UninitializeForContext(FAVirtualizingLayoutContext context)
	{
		FlowAlgorithm.UninitializeForContext(context);
	}

	public void OnElementMeasured(int elementIndex, double majorSize, double minorSize)
	{
		int num = ((elementIndex < 100) ? elementIndex : (elementIndex % 100));
		if (_estimationBuffer[num] == 0.0)
		{
			TotalElementsMeasured++;
		}
		TotalElementSize -= _estimationBuffer[num];
		TotalElementSize += majorSize;
		_estimationBuffer[num] = majorSize;
		MaxArrangeBounds = Math.Max(MaxArrangeBounds, minorSize);
	}

	public void OnMeasureStart()
	{
		MaxArrangeBounds = 0.0;
	}
}
