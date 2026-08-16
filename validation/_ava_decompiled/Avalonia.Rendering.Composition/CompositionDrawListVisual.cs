using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// A composition visual that holds a list of drawing commands issued by <see cref="T:Avalonia.Visual" />
/// </summary>
internal class CompositionDrawListVisual : CompositionContainerVisual
{
	private bool _drawListChanged;

	private CompositionRenderData? _drawList;

	/// <summary>
	/// The associated <see cref="T:Avalonia.Visual" />
	/// </summary>
	public Visual Visual { get; }

	/// <summary>
	/// The list of drawing commands
	/// </summary>
	public CompositionRenderData? DrawList
	{
		get
		{
			return _drawList;
		}
		set
		{
			if (value != null || _drawList != null)
			{
				_drawList?.Dispose();
				_drawList = value;
				_drawListChanged = true;
				RegisterForSerialization();
			}
		}
	}

	private protected override void SerializeChangesCore(BatchStreamWriter writer)
	{
		writer.Write(_drawListChanged ? ((byte)1) : ((byte)0));
		if (_drawListChanged)
		{
			writer.WriteObject(DrawList?.Server);
			_drawListChanged = false;
		}
		base.SerializeChangesCore(writer);
	}

	internal CompositionDrawListVisual(Compositor compositor, ServerCompositionDrawListVisual server, Visual visual)
		: base(compositor, server)
	{
		Visual = visual;
		CustomHitTestCountInSubTree = ((visual is ICustomHitTest) ? 1 : 0);
	}

	internal override bool HitTest(Point pt)
	{
		ICustomHitTest customHitTest = Visual as ICustomHitTest;
		if (DrawList == null && customHitTest == null)
		{
			return false;
		}
		return customHitTest?.HitTest(pt) ?? DrawList?.HitTest(pt) ?? false;
	}
}
