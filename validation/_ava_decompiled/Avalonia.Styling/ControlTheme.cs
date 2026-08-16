using System;
using System.Diagnostics;
using Avalonia.Diagnostics;
using Avalonia.PropertyStore;

namespace Avalonia.Styling;

/// <summary>
/// Defines a switchable theme for a control.
/// </summary>
public class ControlTheme : StyleBase
{
	/// <summary>
	/// Gets or sets the type for which this control theme is intended.
	/// </summary>
	public Type? TargetType { get; set; }

	/// <summary>
	/// Gets or sets a control theme that is the basis of the current theme.
	/// </summary>
	public ControlTheme? BasedOn { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Styling.ControlTheme" /> class.
	/// </summary>
	public ControlTheme()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Styling.ControlTheme" /> class.
	/// </summary>
	/// <param name="targetType">The value for <see cref="P:Avalonia.Styling.ControlTheme.TargetType" />.</param>
	public ControlTheme(Type targetType)
	{
		TargetType = targetType;
	}

	public override string ToString()
	{
		return TargetType?.Name ?? "ControlTheme";
	}

	internal override void SetParent(StyleBase? parent)
	{
		throw new InvalidOperationException("ControlThemes cannot be added as a nested style.");
	}

	internal SelectorMatchResult TryAttach(StyledElement target, FrameType type)
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		if ((object)TargetType == null)
		{
			throw new InvalidOperationException("ControlTheme has no TargetType.");
		}
		using Activity activity = Diagnostic.AttachingStyle()?.AddTag("Style", this);
		if (base.HasSettersOrAnimations && TargetType.IsAssignableFrom(target.StyleKey))
		{
			Attach(target, null, type, canShareInstance: true);
			activity?.AddTag("SelectorResult", SelectorMatchResult.AlwaysThisType);
			return SelectorMatchResult.AlwaysThisType;
		}
		activity?.AddTag("SelectorResult", SelectorMatchResult.NeverThisType);
		return SelectorMatchResult.NeverThisType;
	}
}
