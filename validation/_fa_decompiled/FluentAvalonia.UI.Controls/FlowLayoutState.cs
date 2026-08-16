using System.Runtime.CompilerServices;
using Avalonia;

namespace FluentAvalonia.UI.Controls;

internal class FlowLayoutState
{
	[CompilerGenerated]
	private Size _003CSpecialElementDesiredSize_003Ek__BackingField;

	private readonly FlowLayoutAlgorithm _flowAlgorithm = new FlowLayoutAlgorithm();

	private double[] _lineSizeEstimationBuffer;

	private double[] _itemsPerLineEstimationBuffer;

	private double _totalLineSize;

	private int _totalLinesMeasured;

	private double _totalItemsPerLine;

	private const int BufferSize = 100;

	internal FlowLayoutAlgorithm FlowAlgorithm => _flowAlgorithm;

	internal Size SpecialElementDesiredSize
	{
		[CompilerGenerated]
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return _003CSpecialElementDesiredSize_003Ek__BackingField;
		}
		[CompilerGenerated]
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			_003CSpecialElementDesiredSize_003Ek__BackingField = value;
		}
	}

	internal double TotalLineSize => _totalLineSize;

	internal int TotalLinesMeasured => _totalLinesMeasured;

	internal double TotalItemsPerLine => _totalItemsPerLine;

	public void InitializeForContext(FAVirtualizingLayoutContext context, IFlowLayoutAlgorithmDelegates callbacks)
	{
		_flowAlgorithm.InitializeForContext(context, callbacks);
		if (_lineSizeEstimationBuffer == null)
		{
			_lineSizeEstimationBuffer = new double[100];
			_itemsPerLineEstimationBuffer = new double[100];
		}
		context.LayoutStateCore = this;
	}

	public void UninitializeForContext(FAVirtualizingLayoutContext context)
	{
		_flowAlgorithm.UninitializeForContext(context);
	}

	public void OnLineArranged(int startIndex, int countInLine, double lineSize, FAVirtualizingLayoutContext context)
	{
		if (_totalLinesMeasured == 0 || startIndex + countInLine != context.ItemCount)
		{
			int num = startIndex % _lineSizeEstimationBuffer.Length;
			if (_lineSizeEstimationBuffer[num] == 0.0)
			{
				_totalLinesMeasured++;
			}
			_totalLineSize -= _lineSizeEstimationBuffer[num];
			_totalLineSize += lineSize;
			_lineSizeEstimationBuffer[num] = lineSize;
			_totalItemsPerLine -= _itemsPerLineEstimationBuffer[num];
			_totalItemsPerLine += countInLine;
			_itemsPerLineEstimationBuffer[num] = countInLine;
		}
	}
}
