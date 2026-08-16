using System;
using System.Text;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Data.Core.ExpressionNodes;

internal sealed class VisualAncestorElementNode : SourceNode
{
	private readonly Type? _ancestorType;

	private readonly int _ancestorLevel;

	private IDisposable? _subscription;

	public VisualAncestorElementNode(Type? ancestorType, int ancestorLevel)
	{
		_ancestorType = ancestorType;
		_ancestorLevel = ancestorLevel;
	}

	public override void BuildString(StringBuilder builder)
	{
		builder.Append("$visualParent");
		if (_ancestorLevel <= 0 && (object)_ancestorType == null)
		{
			return;
		}
		builder.Append('[');
		if ((object)_ancestorType != null)
		{
			builder.Append(_ancestorType.Name);
			if (_ancestorLevel > 0)
			{
				builder.Append(',');
			}
		}
		if (_ancestorLevel > 0)
		{
			builder.Append(_ancestorLevel);
		}
		builder.Append(']');
	}

	public override object? SelectSource(object? source, object target, object? anchor)
	{
		if (source != AvaloniaProperty.UnsetValue)
		{
			throw new NotSupportedException("VisualAncestorNode is invalid in conjunction with a binding source.");
		}
		if (target is Visual)
		{
			return target;
		}
		if (anchor is Visual)
		{
			return anchor;
		}
		throw new InvalidOperationException("Cannot find an ILogical to get a visual ancestor.");
	}

	public override bool ShouldLogErrors(object target)
	{
		if (target is Visual visual)
		{
			return visual.IsAttachedToVisualTree;
		}
		return false;
	}

	protected override void OnSourceChanged(object? source, Exception? dataValidationError)
	{
		if (ValidateNonNullSource(source) && source is Visual relativeTo)
		{
			IObservable<Visual> source2 = VisualLocator.Track(relativeTo, _ancestorLevel, _ancestorType);
			_subscription = source2.Subscribe(TrackedControlChanged);
		}
	}

	protected override void Unsubscribe(object oldSource)
	{
		_subscription?.Dispose();
		_subscription = null;
	}

	private void TrackedControlChanged(Visual? control)
	{
		if (control != null)
		{
			SetValue(control);
		}
		else
		{
			SetError("Ancestor not found.");
		}
	}
}
