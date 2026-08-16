using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Input;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Data.Converters;

[RequiresUnreferencedCode("BindingExpression and ReflectionBinding heavily use reflection. Consider using CompiledBindings instead.")]
internal class MethodToCommandConverter : ICommand
{
	internal class WeakPropertyChangedProxy
	{
		private readonly WeakReference<PropertyChangedEventHandler?> _listener = new WeakReference<PropertyChangedEventHandler>(null);

		private readonly PropertyChangedEventHandler _handler;

		internal WeakReference<INotifyPropertyChanged?> Source { get; } = new WeakReference<INotifyPropertyChanged>(null);

		public WeakPropertyChangedProxy()
		{
			_handler = OnPropertyChanged;
		}

		public WeakPropertyChangedProxy(INotifyPropertyChanged source, PropertyChangedEventHandler listener)
			: this()
		{
			SubscribeTo(source, listener);
		}

		public void SubscribeTo(INotifyPropertyChanged source, PropertyChangedEventHandler listener)
		{
			source.PropertyChanged += _handler;
			Source.SetTarget(source);
			_listener.SetTarget(listener);
		}

		public void Unsubscribe()
		{
			if (Source.TryGetTarget(out INotifyPropertyChanged target) && target != null)
			{
				target.PropertyChanged -= _handler;
			}
			Source.SetTarget(null);
			_listener.SetTarget(null);
		}

		private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (_listener.TryGetTarget(out PropertyChangedEventHandler target) && target != null)
			{
				target(sender, e);
			}
			else
			{
				Unsubscribe();
			}
		}
	}

	private static readonly Func<object?, bool> AlwaysEnabled = (object? _) => true;

	private static readonly MethodInfo tryConvert = typeof(TypeUtilities).GetMethod("TryConvert", BindingFlags.Static | BindingFlags.Public);

	private static readonly PropertyInfo currentCulture = typeof(CultureInfo).GetProperty("CurrentCulture", BindingFlags.Static | BindingFlags.Public);

	private readonly Func<object?, bool> canExecute;

	private readonly Action<object?> execute;

	private readonly WeakPropertyChangedProxy? weakPropertyChanged;

	private readonly PropertyChangedEventHandler? propertyChangedEventHandler;

	private readonly string[]? dependencyProperties;

	public event EventHandler? CanExecuteChanged;

	public MethodToCommandConverter(Delegate action)
	{
		object target = action.Target;
		string canExecuteMethodName = "Can" + action.Method.Name;
		ParameterInfo[] parameters = action.Method.GetParameters();
		Type type = ((parameters.Length == 0) ? null : parameters[0].ParameterType);
		if (type == null)
		{
			execute = CreateExecute(target, action.Method);
		}
		else
		{
			execute = CreateExecute(target, action.Method, type);
		}
		MethodInfo methodInfo = action.Method.DeclaringType?.GetRuntimeMethods().FirstOrDefault((MethodInfo m) => m.Name == canExecuteMethodName && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(object));
		if (methodInfo == null)
		{
			canExecute = AlwaysEnabled;
			return;
		}
		canExecute = CreateCanExecute(target, methodInfo);
		dependencyProperties = (from x in methodInfo.GetCustomAttributes(typeof(DependsOnAttribute), inherit: true).OfType<DependsOnAttribute>()
			select x.Name).ToArray();
		if (dependencyProperties.Any() && target is INotifyPropertyChanged source)
		{
			propertyChangedEventHandler = OnPropertyChanged;
			weakPropertyChanged = new WeakPropertyChangedProxy(source, propertyChangedEventHandler);
		}
	}

	private void OnPropertyChanged(object? sender, PropertyChangedEventArgs args)
	{
		if (!string.IsNullOrWhiteSpace(args.PropertyName))
		{
			string[]? array = dependencyProperties;
			if (array == null || !array.Contains<string>(args.PropertyName))
			{
				return;
			}
		}
		Dispatcher.UIThread.Post(delegate
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}, DispatcherPriority.Input);
	}

	public bool CanExecute(object? parameter)
	{
		return canExecute(parameter);
	}

	public void Execute(object? parameter)
	{
		execute(parameter);
	}

	private static Action<object?> CreateExecute(object? target, MethodInfo method)
	{
		ParameterExpression parameterExpression = Expression.Parameter(typeof(object), "parameter");
		return Expression.Lambda<Action<object>>(Expression.Call(ConvertTarget(target, method), method), new ParameterExpression[1] { parameterExpression }).Compile();
	}

	private static Action<object?> CreateExecute(object? target, MethodInfo method, Type parameterType)
	{
		ParameterExpression parameterExpression = Expression.Parameter(typeof(object), "parameter");
		Expression instance = ConvertTarget(target, method);
		Expression body;
		if (parameterType == typeof(object))
		{
			body = Expression.Call(instance, method, parameterExpression);
		}
		else
		{
			ParameterExpression parameterExpression2 = Expression.Variable(typeof(object), "argX");
			MethodCallExpression methodCallExpression = Expression.Call(tryConvert, Expression.Constant(parameterType), parameterExpression, Expression.Property(null, currentCulture), parameterExpression2);
			MethodCallExpression methodCallExpression2 = Expression.Call(instance, method, Expression.Convert(parameterExpression2, parameterType));
			body = Expression.Block(new ParameterExpression[1] { parameterExpression2 }, methodCallExpression, methodCallExpression2);
		}
		return Expression.Lambda<Action<object>>(body, new ParameterExpression[1] { parameterExpression }).Compile();
	}

	private static Func<object?, bool> CreateCanExecute(object? target, MethodInfo method)
	{
		ParameterExpression parameterExpression = Expression.Parameter(typeof(object), "parameter");
		return Expression.Lambda<Func<object, bool>>(Expression.Call(ConvertTarget(target, method), method, parameterExpression), new ParameterExpression[1] { parameterExpression }).Compile();
	}

	private static Expression? ConvertTarget(object? target, MethodInfo method)
	{
		if (target != null)
		{
			return Expression.Convert(Expression.Constant(target), method.DeclaringType);
		}
		return null;
	}
}
