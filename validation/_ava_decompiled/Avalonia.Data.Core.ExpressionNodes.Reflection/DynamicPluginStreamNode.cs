using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Avalonia.Data.Core.Plugins;
using Avalonia.Reactive;

namespace Avalonia.Data.Core.ExpressionNodes.Reflection;

[RequiresUnreferencedCode("ExpressionNode might require unreferenced code.")]
[RequiresDynamicCode("ExpressionNode requires dynamic code.")]
internal sealed class DynamicPluginStreamNode : ExpressionNode
{
	private IDisposable? _subscription;

	public override void BuildString(StringBuilder builder)
	{
		builder.Append('^');
	}

	protected override void OnSourceChanged(object? source, Exception? dataValidationError)
	{
		if (!ValidateNonNullSource(source))
		{
			return;
		}
		WeakReference<object> weakReference = new WeakReference<object>(source);
		IStreamPlugin plugin = GetPlugin(weakReference);
		if (plugin != null)
		{
			IObservable<object> observable = plugin.Start(weakReference);
			if (observable != null)
			{
				_subscription = observable.Subscribe(base.SetValue);
				return;
			}
		}
		SetValue(null);
	}

	protected override void Unsubscribe(object oldSource)
	{
		_subscription?.Dispose();
		_subscription = null;
	}

	private static IStreamPlugin? GetPlugin(WeakReference<object?> source)
	{
		if (source == null)
		{
			return null;
		}
		foreach (IStreamPlugin s_streamHandler in BindingPlugins.s_streamHandlers)
		{
			if (s_streamHandler.Match(source))
			{
				return s_streamHandler;
			}
		}
		return null;
	}
}
