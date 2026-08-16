using System;
using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia;

[PrivateApi]
public class AvaloniaLocator : IAvaloniaDependencyResolver
{
	public class RegistrationHelper<TService>
	{
		private readonly AvaloniaLocator _locator;

		public RegistrationHelper(AvaloniaLocator locator)
		{
			_locator = locator;
		}

		public AvaloniaLocator ToConstant<TImpl>(TImpl constant) where TImpl : TService
		{
			_locator._registry[typeof(TService)] = () => constant;
			return _locator;
		}

		public AvaloniaLocator ToFunc<TImlp>(Func<TImlp> func) where TImlp : TService
		{
			_locator._registry[typeof(TService)] = () => func();
			return _locator;
		}

		public AvaloniaLocator ToLazy<TImlp>(Func<TImlp> func) where TImlp : TService
		{
			bool constructed = false;
			TImlp instance = default(TImlp);
			_locator._registry[typeof(TService)] = delegate
			{
				if (!constructed)
				{
					instance = func();
					constructed = true;
				}
				return instance;
			};
			return _locator;
		}

		public AvaloniaLocator ToSingleton<TImpl>() where TImpl : class, TService, new()
		{
			TImpl instance = null;
			return this.ToFunc<TImpl>((Func<TImpl>)(() => instance ?? (instance = new TImpl())));
		}

		public AvaloniaLocator ToTransient<TImpl>() where TImpl : class, TService, new()
		{
			return this.ToFunc<TImpl>((Func<TImpl>)(() => new TImpl()));
		}
	}

	private class ResolverDisposable : IDisposable
	{
		private readonly IAvaloniaDependencyResolver _resolver;

		private readonly AvaloniaLocator _mutable;

		public ResolverDisposable(IAvaloniaDependencyResolver resolver, AvaloniaLocator mutable)
		{
			_resolver = resolver;
			_mutable = mutable;
		}

		public void Dispose()
		{
			Current = _resolver;
			CurrentMutable = _mutable;
		}
	}

	private readonly IAvaloniaDependencyResolver? _parentScope;

	private readonly Dictionary<Type, Func<object?>> _registry = new Dictionary<Type, Func<object>>();

	public static IAvaloniaDependencyResolver Current { get; set; }

	public static AvaloniaLocator CurrentMutable { get; set; }

	static AvaloniaLocator()
	{
		Current = (CurrentMutable = new AvaloniaLocator());
	}

	public AvaloniaLocator()
	{
	}

	public AvaloniaLocator(IAvaloniaDependencyResolver parentScope)
	{
		_parentScope = parentScope;
	}

	public object? GetService(Type t)
	{
		if (!_registry.TryGetValue(t, out Func<object> value))
		{
			return _parentScope?.GetService(t);
		}
		return value();
	}

	public RegistrationHelper<T> Bind<T>()
	{
		return new RegistrationHelper<T>(this);
	}

	public AvaloniaLocator BindToSelf<T>(T constant)
	{
		return Bind<T>().ToConstant(constant);
	}

	public AvaloniaLocator BindToSelfSingleton<T>() where T : class, new()
	{
		return Bind<T>().ToSingleton<T>();
	}

	public static IDisposable EnterScope()
	{
		ResolverDisposable result = new ResolverDisposable(Current, CurrentMutable);
		Current = (CurrentMutable = new AvaloniaLocator(Current));
		return result;
	}
}
