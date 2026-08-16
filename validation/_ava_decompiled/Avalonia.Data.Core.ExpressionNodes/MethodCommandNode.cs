using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in an <see cref="T:Avalonia.Data.Core.BindingExpression" /> which converts methods to an
/// <see cref="T:System.Windows.Input.ICommand" />.
/// </summary>
internal sealed class MethodCommandNode : ExpressionNode, IWeakEventSubscriber<PropertyChangedEventArgs>
{
	private sealed class Command : ICommand
	{
		private readonly WeakReference<object?> _target;

		private readonly Action<object, object?> _execute;

		private readonly Func<object, object?, bool>? _canExecute;

		public event EventHandler? CanExecuteChanged;

		public Command(object? target, Action<object, object?> execute, Func<object, object?, bool>? canExecute)
		{
			_target = new WeakReference<object>(target);
			_execute = execute;
			_canExecute = canExecute;
		}

		public void RaiseCanExecuteChanged()
		{
			Dispatcher.UIThread.Post(delegate
			{
				CanExecuteChanged?.Invoke(this, EventArgs.Empty);
			}, DispatcherPriority.Input);
		}

		public bool CanExecute(object? parameter)
		{
			if (_target.TryGetTarget(out object target))
			{
				if (_canExecute == null)
				{
					return true;
				}
				return _canExecute(target, parameter);
			}
			return false;
		}

		public void Execute(object? parameter)
		{
			if (_target.TryGetTarget(out object target))
			{
				_execute(target, parameter);
			}
		}
	}

	private readonly string _methodName;

	private readonly Action<object, object?> _execute;

	private readonly Func<object, object?, bool>? _canExecute;

	private readonly ISet<string> _dependsOnProperties;

	private Command? _command;

	public MethodCommandNode(string methodName, Action<object, object?> execute, Func<object, object?, bool>? canExecute, ISet<string> dependsOnProperties)
	{
		_methodName = methodName;
		_execute = execute;
		_canExecute = canExecute;
		_dependsOnProperties = dependsOnProperties;
	}

	public override void BuildString(StringBuilder builder)
	{
		if (builder.Length > 0 && builder[builder.Length - 1] != '!')
		{
			builder.Append('.');
		}
		builder.Append(_methodName);
		builder.Append("()");
	}

	protected override void OnSourceChanged(object? source, Exception? dataValidationError)
	{
		if (ValidateNonNullSource(source))
		{
			if (source is INotifyPropertyChanged target)
			{
				WeakEvents.ThreadSafePropertyChanged.Subscribe(target, this);
			}
			_command = new Command(source, _execute, _canExecute);
			SetValue(_command);
		}
	}

	protected override void Unsubscribe(object oldSource)
	{
		if (oldSource is INotifyPropertyChanged target)
		{
			WeakEvents.ThreadSafePropertyChanged.Unsubscribe(target, this);
		}
	}

	public void OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs e)
	{
		if (string.IsNullOrEmpty(e.PropertyName) || _dependsOnProperties.Contains(e.PropertyName))
		{
			_command?.RaiseCanExecuteChanged();
		}
	}
}
