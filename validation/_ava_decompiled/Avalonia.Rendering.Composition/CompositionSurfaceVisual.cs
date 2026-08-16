using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition;

public class CompositionSurfaceVisual : CompositionContainerVisual
{
	private CompositionSurfaceVisualChangedFields _changedFieldsOfCompositionSurfaceVisual;

	private CompositionSurface? _surface;

	internal new ServerCompositionSurfaceVisual Server { get; }

	public CompositionSurface? Surface
	{
		get
		{
			return _surface;
		}
		set
		{
			bool flag = false;
			if (_surface != value)
			{
				flag = true;
				_surface = value;
				_changedFieldsOfCompositionSurfaceVisual |= CompositionSurfaceVisualChangedFields.Surface;
				RegisterForSerialization();
			}
			_surface = value;
		}
	}

	internal CompositionSurfaceVisual(Compositor compositor, ServerCompositionSurfaceVisual server)
		: base(compositor, server)
	{
		Server = server;
		InitializeDefaults();
	}

	private void InitializeDefaults()
	{
	}

	private protected override void SerializeChangesCore(BatchStreamWriter writer)
	{
		base.SerializeChangesCore(writer);
		writer.Write(_changedFieldsOfCompositionSurfaceVisual);
		if ((_changedFieldsOfCompositionSurfaceVisual & CompositionSurfaceVisualChangedFields.Surface) == CompositionSurfaceVisualChangedFields.Surface)
		{
			writer.WriteObject(_surface?.Server);
		}
		_changedFieldsOfCompositionSurfaceVisual = (CompositionSurfaceVisualChangedFields)0;
	}
}
