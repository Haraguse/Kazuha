using System;
using Avalonia.Collections;
using Avalonia.Threading;

namespace Avalonia.Animation;

/// <summary>
/// A collection of <see cref="T:Avalonia.Animation.ITransition" /> definitions.
/// </summary>
public sealed class Transitions : AvaloniaList<ITransition>, IAvaloniaListItemValidator<ITransition>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Animation.Transitions" /> class.
	/// </summary>
	public Transitions()
	{
		base.ResetBehavior = ResetBehavior.Remove;
		base.Validator = this;
	}

	void IAvaloniaListItemValidator<ITransition>.Validate(ITransition item)
	{
		Dispatcher.UIThread.VerifyAccess();
		AvaloniaProperty property = item.Property;
		if (property.IsDirect)
		{
			string value = ((item is TransitionBase transitionBase) ? transitionBase.DebugDisplay : item.ToString());
			throw new InvalidOperationException($"Cannot animate direct property {property} on {value}.");
		}
	}
}
