using System;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Reactive;

namespace Avalonia;

/// <summary>
/// Provides extension methods for <see cref="T:Avalonia.AvaloniaObject" /> and related classes.
/// </summary>
public static class AvaloniaObjectExtensions
{
	private class BindingAdaptor : BindingBase
	{
		private readonly IObservable<object?> _source;

		public BindingAdaptor(IObservable<object?> source)
		{
			_source = source;
		}

		internal override BindingExpressionBase CreateInstance(AvaloniaObject target, AvaloniaProperty? property, object? anchor)
		{
			return new UntypedObservableBindingExpression(_source, BindingPriority.LocalValue);
		}
	}

	private class ClassHandlerObserver<TTarget, TValue> : IObserver<AvaloniaPropertyChangedEventArgs<TValue>>
	{
		private readonly Action<TTarget, AvaloniaPropertyChangedEventArgs<TValue>> _action;

		public ClassHandlerObserver(Action<TTarget, AvaloniaPropertyChangedEventArgs<TValue>> action)
		{
			_action = action;
		}

		public void OnCompleted()
		{
		}

		public void OnError(Exception error)
		{
		}

		public void OnNext(AvaloniaPropertyChangedEventArgs<TValue> value)
		{
			if (value.Sender is TTarget arg)
			{
				_action(arg, value);
			}
		}
	}

	private class ClassHandlerObserver<TTarget> : IObserver<AvaloniaPropertyChangedEventArgs>
	{
		private readonly Action<TTarget, AvaloniaPropertyChangedEventArgs> _action;

		public ClassHandlerObserver(Action<TTarget, AvaloniaPropertyChangedEventArgs> action)
		{
			_action = action;
		}

		public void OnCompleted()
		{
		}

		public void OnError(Exception error)
		{
		}

		public void OnNext(AvaloniaPropertyChangedEventArgs value)
		{
			if (value.Sender is TTarget arg)
			{
				_action(arg, value);
			}
		}
	}

	/// <summary>
	/// Converts an <see cref="T:System.IObservable`1" /> to an <see cref="T:Avalonia.Data.BindingBase" />.
	/// </summary>
	/// <typeparam name="T">The type produced by the observable.</typeparam>
	/// <param name="source">The observable</param>
	/// <returns>An <see cref="T:Avalonia.Data.BindingBase" />.</returns>
	public static BindingBase ToBinding<T>(this IObservable<T> source)
	{
		object source2;
		if (!typeof(T).IsValueType)
		{
			source2 = (IObservable<object>)source;
		}
		else
		{
			IObservable<object> observable = source.Select((Func<T, object>)((T x) => x));
			source2 = observable;
		}
		return new BindingAdaptor((IObservable<object?>)source2);
	}

	/// <summary>
	/// Gets an observable for an <see cref="T:Avalonia.AvaloniaProperty" />.
	/// </summary>
	/// <param name="o">The object.</param>
	/// <param name="property">The property.</param>
	/// <returns>
	/// An observable which fires immediately with the current value of the property on the
	/// object and subsequently each time the property value changes.
	/// </returns>
	/// <remarks>
	/// The subscription to <paramref name="o" /> is created using a weak reference.
	/// </remarks>
	public static IObservable<object?> GetObservable(this AvaloniaObject o, AvaloniaProperty property)
	{
		return new AvaloniaPropertyObservable<object, object>(o ?? throw new ArgumentNullException("o"), property ?? throw new ArgumentNullException("property"));
	}

	/// <summary>
	/// Gets an observable for an <see cref="T:Avalonia.AvaloniaProperty" />.
	/// </summary>
	/// <param name="o">The object.</param>
	/// <typeparam name="T">The property type.</typeparam>
	/// <param name="property">The property.</param>
	/// <returns>
	/// An observable which fires immediately with the current value of the property on the
	/// object and subsequently each time the property value changes.
	/// </returns>
	/// <remarks>
	/// The subscription to <paramref name="o" /> is created using a weak reference.
	/// </remarks>
	public static IObservable<T> GetObservable<T>(this AvaloniaObject o, AvaloniaProperty<T> property)
	{
		return new AvaloniaPropertyObservable<T, T>(o ?? throw new ArgumentNullException("o"), property ?? throw new ArgumentNullException("property"));
	}

	/// <inheritdoc cref="M:Avalonia.AvaloniaObjectExtensions.GetObservable``1(Avalonia.AvaloniaObject,Avalonia.AvaloniaProperty{``0})" />
	/// <typeparam name="TSource">The type of the values held by the <paramref name="property" />.</typeparam>
	/// <typeparam name="TResult">The type of the value returned by the <paramref name="converter" />.</typeparam>
	/// <param name="o" />
	/// <param name="property" />
	/// <param name="converter">A method which is executed to convert each property value to <typeparamref name="TResult" />.</param>
	public static IObservable<TResult> GetObservable<TSource, TResult>(this AvaloniaObject o, AvaloniaProperty<TSource> property, Func<TSource, TResult> converter)
	{
		return new AvaloniaPropertyObservable<TSource, TResult>(o ?? throw new ArgumentNullException("o"), property ?? throw new ArgumentNullException("property"), converter ?? throw new ArgumentNullException("converter"));
	}

	/// <inheritdoc cref="M:Avalonia.AvaloniaObjectExtensions.GetObservable``2(Avalonia.AvaloniaObject,Avalonia.AvaloniaProperty{``0},System.Func{``0,``1})" />
	public static IObservable<TResult> GetObservable<TResult>(this AvaloniaObject o, AvaloniaProperty property, Func<object?, TResult> converter)
	{
		return new AvaloniaPropertyObservable<object, TResult>(o ?? throw new ArgumentNullException("o"), property ?? throw new ArgumentNullException("property"), converter ?? throw new ArgumentNullException("converter"));
	}

	/// <summary>
	/// Gets an observable for an <see cref="T:Avalonia.AvaloniaProperty" />.
	/// </summary>
	/// <param name="o">The object.</param>
	/// <param name="property">The property.</param>
	/// <returns>
	/// An observable which fires immediately with the current value of the property on the
	/// object and subsequently each time the property value changes.
	/// </returns>
	/// <remarks>
	/// The subscription to <paramref name="o" /> is created using a weak reference.
	/// </remarks>
	public static IObservable<BindingValue<object?>> GetBindingObservable(this AvaloniaObject o, AvaloniaProperty property)
	{
		return new AvaloniaPropertyBindingObservable<object, object>(o ?? throw new ArgumentNullException("o"), property ?? throw new ArgumentNullException("property"));
	}

	/// <inheritdoc cref="M:Avalonia.AvaloniaObjectExtensions.GetObservable``2(Avalonia.AvaloniaObject,Avalonia.AvaloniaProperty{``0},System.Func{``0,``1})" />
	public static IObservable<BindingValue<TResult>> GetBindingObservable<TResult>(this AvaloniaObject o, AvaloniaProperty property, Func<object?, TResult> converter)
	{
		return new AvaloniaPropertyBindingObservable<object, TResult>(o ?? throw new ArgumentNullException("o"), property ?? throw new ArgumentNullException("property"), converter ?? throw new ArgumentNullException("converter"));
	}

	/// <summary>
	/// Gets an observable for an <see cref="T:Avalonia.AvaloniaProperty" />.
	/// </summary>
	/// <param name="o">The object.</param>
	/// <typeparam name="T">The property type.</typeparam>
	/// <param name="property">The property.</param>
	/// <returns>
	/// An observable which fires immediately with the current value of the property on the
	/// object and subsequently each time the property value changes.
	/// </returns>
	/// <remarks>
	/// The subscription to <paramref name="o" /> is created using a weak reference.
	/// </remarks>
	public static IObservable<BindingValue<T>> GetBindingObservable<T>(this AvaloniaObject o, AvaloniaProperty<T> property)
	{
		return new AvaloniaPropertyBindingObservable<T, T>(o ?? throw new ArgumentNullException("o"), property ?? throw new ArgumentNullException("property"));
	}

	/// <inheritdoc cref="M:Avalonia.AvaloniaObjectExtensions.GetBindingObservable``1(Avalonia.AvaloniaObject,Avalonia.AvaloniaProperty{``0})" />
	/// <param name="o" />
	/// <param name="property" />
	/// <param name="converter">A method which is executed to convert each property value to <typeparamref name="TResult" />.</param>
	public static IObservable<BindingValue<TResult>> GetBindingObservable<TSource, TResult>(this AvaloniaObject o, AvaloniaProperty<TSource> property, Func<TSource, TResult> converter)
	{
		return new AvaloniaPropertyBindingObservable<TSource, TResult>(o ?? throw new ArgumentNullException("o"), property ?? throw new ArgumentNullException("property"), converter ?? throw new ArgumentNullException("converter"));
	}

	/// <summary>
	/// Gets an observable that listens for property changed events for an
	/// <see cref="T:Avalonia.AvaloniaProperty" />.
	/// </summary>
	/// <param name="o">The object.</param>
	/// <param name="property">The property.</param>
	/// <returns>
	/// An observable which when subscribed pushes the property changed event args
	/// each time a <see cref="E:Avalonia.AvaloniaObject.PropertyChanged" /> event is raised
	/// for the specified property.
	/// </returns>
	public static IObservable<AvaloniaPropertyChangedEventArgs> GetPropertyChangedObservable(this AvaloniaObject o, AvaloniaProperty property)
	{
		return new AvaloniaPropertyChangedObservable(o ?? throw new ArgumentNullException("o"), property ?? throw new ArgumentNullException("property"));
	}

	/// <summary>
	/// Binds an <see cref="T:Avalonia.AvaloniaProperty" /> to an observable.
	/// </summary>
	/// <typeparam name="T">The type of the property.</typeparam>
	/// <param name="target">The object.</param>
	/// <param name="property">The property.</param>
	/// <param name="source">The observable.</param>
	/// <param name="priority">The priority of the binding.</param>
	/// <returns>
	/// A disposable which can be used to terminate the binding.
	/// </returns>
	public static IDisposable Bind<T>(this AvaloniaObject target, AvaloniaProperty<T> property, IObservable<BindingValue<T>> source, BindingPriority priority = BindingPriority.LocalValue)
	{
		target = target ?? throw new ArgumentNullException("target");
		property = property ?? throw new ArgumentNullException("property");
		source = source ?? throw new ArgumentNullException("source");
		if (!(property is StyledProperty<T> property2))
		{
			if (property is DirectPropertyBase<T> property3)
			{
				return target.Bind(property3, source);
			}
			throw new NotSupportedException("Unsupported AvaloniaProperty type.");
		}
		return target.Bind(property2, source, priority);
	}

	/// <summary>
	/// Binds an <see cref="T:Avalonia.AvaloniaProperty" /> to an observable.
	/// </summary>
	/// <param name="target">The object.</param>
	/// <param name="property">The property.</param>
	/// <param name="source">The observable.</param>
	/// <param name="priority">The priority of the binding.</param>
	/// <returns>
	/// A disposable which can be used to terminate the binding.
	/// </returns>
	public static IDisposable Bind<T>(this AvaloniaObject target, AvaloniaProperty<T> property, IObservable<T> source, BindingPriority priority = BindingPriority.LocalValue)
	{
		if (!(property is StyledProperty<T> property2))
		{
			if (property is DirectPropertyBase<T> property3)
			{
				return target.Bind(property3, source);
			}
			throw new NotSupportedException("Unsupported AvaloniaProperty type.");
		}
		return target.Bind(property2, source, priority);
	}

	/// <summary>
	/// Gets a <see cref="T:Avalonia.AvaloniaProperty" /> value.
	/// </summary>
	/// <typeparam name="T">The type of the property.</typeparam>
	/// <param name="target">The object.</param>
	/// <param name="property">The property.</param>
	/// <returns>The value.</returns>
	public static T GetValue<T>(this AvaloniaObject target, AvaloniaProperty<T> property)
	{
		target = target ?? throw new ArgumentNullException("target");
		property = property ?? throw new ArgumentNullException("property");
		if (!(property is StyledProperty<T> property2))
		{
			if (property is DirectPropertyBase<T> property3)
			{
				return target.GetValue(property3);
			}
			throw new NotSupportedException("Unsupported AvaloniaProperty type.");
		}
		return target.GetValue(property2);
	}

	/// <summary>
	/// Gets an <see cref="T:Avalonia.AvaloniaProperty" /> base value.
	/// </summary>
	/// <param name="target">The object.</param>
	/// <param name="property">The property.</param>
	/// <remarks>
	/// For styled properties, gets the value of the property excluding animated values, otherwise
	/// <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" />. Note that this method does not return
	/// property values that come from inherited or default values.
	///
	/// For direct properties returns the current value of the property.
	/// </remarks>
	public static object? GetBaseValue(this AvaloniaObject target, AvaloniaProperty property)
	{
		target = target ?? throw new ArgumentNullException("target");
		property = property ?? throw new ArgumentNullException("property");
		return property.RouteGetBaseValue(target);
	}

	/// <summary>
	/// Gets an <see cref="T:Avalonia.AvaloniaProperty" /> base value.
	/// </summary>
	/// <param name="target">The object.</param>
	/// <param name="property">The property.</param>
	/// <remarks>
	/// For styled properties, gets the value of the property excluding animated values, otherwise
	/// <see cref="P:Avalonia.Data.Optional`1.Empty" />. Note that this method does not return property values
	/// that come from inherited or default values.
	///
	/// For direct properties returns the current value of the property.
	/// </remarks>
	public static Optional<T> GetBaseValue<T>(this AvaloniaObject target, AvaloniaProperty<T> property)
	{
		target = target ?? throw new ArgumentNullException("target");
		property = property ?? throw new ArgumentNullException("property");
		if (!(property is StyledProperty<T> property2))
		{
			if (property is DirectPropertyBase<T> property3)
			{
				return target.GetValue(property3);
			}
			throw new NotSupportedException("Unsupported AvaloniaProperty type.");
		}
		return target.GetBaseValue(property2);
	}

	/// <summary>
	/// Subscribes to a property changed notifications for changes that originate from a
	/// <typeparamref name="TTarget" />.
	/// </summary>
	/// <typeparam name="TTarget">The type of the property change sender.</typeparam>
	/// <param name="observable">The property changed observable.</param>
	/// <param name="action">
	/// The method to call. The parameters are the sender and the event args.
	/// </param>
	/// <returns>A disposable that can be used to terminate the subscription.</returns>
	public static IDisposable AddClassHandler<TTarget>(this IObservable<AvaloniaPropertyChangedEventArgs> observable, Action<TTarget, AvaloniaPropertyChangedEventArgs> action) where TTarget : AvaloniaObject
	{
		return observable.Subscribe(new ClassHandlerObserver<TTarget>(action));
	}

	/// <summary>
	/// Subscribes to a property changed notifications for changes that originate from a
	/// <typeparamref name="TTarget" />.
	/// </summary>
	/// <typeparam name="TTarget">The type of the property change sender.</typeparam>
	/// <typeparam name="TValue">The type of the property.</typeparam>
	/// <param name="observable">The property changed observable.</param>
	/// <param name="action">
	/// The method to call. The parameters are the sender and the event args.
	/// </param>
	/// <returns>A disposable that can be used to terminate the subscription.</returns>
	public static IDisposable AddClassHandler<TTarget, TValue>(this IObservable<AvaloniaPropertyChangedEventArgs<TValue>> observable, Action<TTarget, AvaloniaPropertyChangedEventArgs<TValue>> action) where TTarget : AvaloniaObject
	{
		return observable.Subscribe(new ClassHandlerObserver<TTarget, TValue>(action));
	}
}
