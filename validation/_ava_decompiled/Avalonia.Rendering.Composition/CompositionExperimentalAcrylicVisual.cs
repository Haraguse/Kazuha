using Avalonia.Media;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition;

internal class CompositionExperimentalAcrylicVisual : CompositionDrawListVisual
{
	private CompositionExperimentalAcrylicVisualChangedFields _changedFieldsOfCompositionExperimentalAcrylicVisual;

	private ImmutableExperimentalAcrylicMaterial _material;

	private CornerRadius _cornerRadius;

	public ImmutableExperimentalAcrylicMaterial Material
	{
		get
		{
			return _material;
		}
		set
		{
			bool flag = false;
			if (_material != value)
			{
				flag = true;
				_material = value;
				_changedFieldsOfCompositionExperimentalAcrylicVisual |= CompositionExperimentalAcrylicVisualChangedFields.Material;
				RegisterForSerialization();
			}
			_material = value;
		}
	}

	public CornerRadius CornerRadius
	{
		get
		{
			return _cornerRadius;
		}
		set
		{
			bool flag = false;
			if (_cornerRadius != value)
			{
				flag = true;
				_cornerRadius = value;
				_changedFieldsOfCompositionExperimentalAcrylicVisual |= CompositionExperimentalAcrylicVisualChangedFields.CornerRadius;
				RegisterForSerialization();
			}
			_cornerRadius = value;
		}
	}

	internal CompositionExperimentalAcrylicVisual(Compositor compositor, Visual visual)
		: base(compositor, new ServerCompositionExperimentalAcrylicVisual(compositor.Server, visual), visual)
	{
	}

	private void InitializeDefaults()
	{
	}

	private protected override void SerializeChangesCore(BatchStreamWriter writer)
	{
		base.SerializeChangesCore(writer);
		writer.Write(_changedFieldsOfCompositionExperimentalAcrylicVisual);
		if ((_changedFieldsOfCompositionExperimentalAcrylicVisual & CompositionExperimentalAcrylicVisualChangedFields.Material) == CompositionExperimentalAcrylicVisualChangedFields.Material)
		{
			writer.Write(_material);
		}
		if ((_changedFieldsOfCompositionExperimentalAcrylicVisual & CompositionExperimentalAcrylicVisualChangedFields.CornerRadius) == CompositionExperimentalAcrylicVisualChangedFields.CornerRadius)
		{
			writer.Write(_cornerRadius);
		}
		_changedFieldsOfCompositionExperimentalAcrylicVisual = (CompositionExperimentalAcrylicVisualChangedFields)0;
	}
}
