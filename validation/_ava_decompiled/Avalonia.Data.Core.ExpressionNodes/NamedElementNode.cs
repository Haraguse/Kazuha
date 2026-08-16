using System;
using System.Text;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

namespace Avalonia.Data.Core.ExpressionNodes;

internal sealed class NamedElementNode : SourceNode
{
	private readonly WeakReference<INameScope?> _nameScope;

	private readonly string _name;

	private IDisposable? _subscription;

	public NamedElementNode(INameScope? nameScope, string name)
	{
		_nameScope = new WeakReference<INameScope>(nameScope);
		_name = name;
	}

	public override void BuildString(StringBuilder builder)
	{
		builder.Append('#');
		builder.Append(_name);
	}

	public override bool ShouldLogErrors(object target)
	{
		if (target is ILogical logical)
		{
			return logical.IsAttachedToLogicalTree;
		}
		return true;
	}

	protected override void OnSourceChanged(object? source, Exception? dataValidationError)
	{
		if (ValidateNonNullSource(source))
		{
			if (_nameScope.TryGetTarget(out INameScope target))
			{
				_subscription = NameScopeLocator.Track(target, _name).Subscribe(base.SetValue);
			}
			else
			{
				SetError("NameScope not found.");
			}
		}
	}

	protected override void Unsubscribe(object oldSource)
	{
		_subscription?.Dispose();
		_subscription = null;
	}
}
