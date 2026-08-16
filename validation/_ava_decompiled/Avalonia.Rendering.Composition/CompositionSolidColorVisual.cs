using Avalonia.Media;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition;

public class CompositionSolidColorVisual : CompositionContainerVisual
{
	private CompositionSolidColorVisualChangedFields _changedFieldsOfCompositionSolidColorVisual;

	private Color _color;

	internal new ServerCompositionSolidColorVisual Server { get; }

	public Color Color
	{
		get
		{
			return _color;
		}
		set
		{
			bool flag = false;
			if (_color != value)
			{
				flag = true;
				_color = value;
				_changedFieldsOfCompositionSolidColorVisual |= CompositionSolidColorVisualChangedFields.Color;
				RegisterForSerialization();
				PendingAnimations.Remove(ServerCompositionSolidColorVisual.s_IdOfColorProperty);
				_changedFieldsOfCompositionSolidColorVisual &= ~CompositionSolidColorVisualChangedFields.ColorAnimated;
				if (base.ImplicitAnimations != null && base.ImplicitAnimations.TryGetValue("Color", out ICompositionAnimationBase value2))
				{
					if (value2 is CompositionAnimation compositionAnimation)
					{
						_changedFieldsOfCompositionSolidColorVisual |= CompositionSolidColorVisualChangedFields.ColorAnimated;
						PendingAnimations[ServerCompositionSolidColorVisual.s_IdOfColorProperty] = compositionAnimation.CreateInstance(Server, value);
					}
					StartAnimationGroup(value2, "Color", value);
				}
			}
			_color = value;
		}
	}

	internal CompositionSolidColorVisual(Compositor compositor, ServerCompositionSolidColorVisual server)
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
		writer.Write(_changedFieldsOfCompositionSolidColorVisual);
		if ((_changedFieldsOfCompositionSolidColorVisual & CompositionSolidColorVisualChangedFields.ColorAnimated) == CompositionSolidColorVisualChangedFields.ColorAnimated)
		{
			writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionSolidColorVisual.s_IdOfColorProperty));
		}
		else if ((_changedFieldsOfCompositionSolidColorVisual & CompositionSolidColorVisualChangedFields.Color) == CompositionSolidColorVisualChangedFields.Color)
		{
			writer.Write(_color);
		}
		_changedFieldsOfCompositionSolidColorVisual = (CompositionSolidColorVisualChangedFields)0;
	}

	internal override void StartAnimation(string propertyName, CompositionAnimation animation, ExpressionVariant? finalValue)
	{
		if (propertyName == "Color")
		{
			_ = _color;
			IAnimationInstance value = animation.CreateInstance(Server, finalValue);
			PendingAnimations[ServerCompositionSolidColorVisual.s_IdOfColorProperty] = value;
			_changedFieldsOfCompositionSolidColorVisual |= CompositionSolidColorVisualChangedFields.ColorAnimated;
			RegisterForSerialization();
		}
		else
		{
			base.StartAnimation(propertyName, animation, finalValue);
		}
	}
}
