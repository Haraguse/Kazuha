using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Reactive.Operators;

internal sealed class CombineLatest<TFirst, TSecond, TResult> : IObservable<TResult>
{
	internal sealed class @_ : IdentitySink<TResult>
	{
		private sealed class FirstObserver : IObserver<TFirst>
		{
			private readonly @_ _parent;

			private SecondObserver _other;

			public bool HasValue { get; private set; }

			public TFirst? Value { get; private set; }

			public bool Done { get; private set; }

			public FirstObserver(@_ parent)
			{
				_parent = parent;
				_other = null;
			}

			public void SetOther(SecondObserver other)
			{
				_other = other;
			}

			public void OnNext(TFirst value)
			{
				lock (_parent._gate)
				{
					HasValue = true;
					Value = value;
					if (_other.HasValue)
					{
						TResult value2;
						try
						{
							value2 = _parent._resultSelector(value, _other.Value);
						}
						catch (Exception error)
						{
							_parent.ForwardOnError(error);
							return;
						}
						_parent.ForwardOnNext(value2);
					}
					else if (_other.Done)
					{
						_parent.ForwardOnCompleted();
					}
				}
			}

			public void OnError(Exception error)
			{
				lock (_parent._gate)
				{
					_parent.ForwardOnError(error);
				}
			}

			public void OnCompleted()
			{
				lock (_parent._gate)
				{
					Done = true;
					if (_other.Done)
					{
						_parent.ForwardOnCompleted();
					}
					else
					{
						_parent._firstDisposable.Dispose();
					}
				}
			}
		}

		private sealed class SecondObserver : IObserver<TSecond>
		{
			private readonly @_ _parent;

			private FirstObserver _other;

			public bool HasValue { get; private set; }

			public TSecond? Value { get; private set; }

			public bool Done { get; private set; }

			public SecondObserver(@_ parent)
			{
				_parent = parent;
				_other = null;
			}

			public void SetOther(FirstObserver other)
			{
				_other = other;
			}

			public void OnNext(TSecond value)
			{
				lock (_parent._gate)
				{
					HasValue = true;
					Value = value;
					if (_other.HasValue)
					{
						TResult value2;
						try
						{
							value2 = _parent._resultSelector(_other.Value, value);
						}
						catch (Exception error)
						{
							_parent.ForwardOnError(error);
							return;
						}
						_parent.ForwardOnNext(value2);
					}
					else if (_other.Done)
					{
						_parent.ForwardOnCompleted();
					}
				}
			}

			public void OnError(Exception error)
			{
				lock (_parent._gate)
				{
					_parent.ForwardOnError(error);
				}
			}

			public void OnCompleted()
			{
				lock (_parent._gate)
				{
					Done = true;
					if (_other.Done)
					{
						_parent.ForwardOnCompleted();
					}
					else
					{
						_parent._secondDisposable.Dispose();
					}
				}
			}
		}

		private readonly Func<TFirst, TSecond, TResult> _resultSelector;

		private readonly object _gate = new object();

		private IDisposable _firstDisposable;

		private IDisposable _secondDisposable;

		public _(Func<TFirst, TSecond, TResult> resultSelector, IObserver<TResult> observer)
			: base(observer)
		{
			_resultSelector = resultSelector;
			_firstDisposable = null;
			_secondDisposable = null;
		}

		public void Run(IObservable<TFirst> first, IObservable<TSecond> second)
		{
			FirstObserver firstObserver = new FirstObserver(this);
			SecondObserver secondObserver = new SecondObserver(this);
			firstObserver.SetOther(secondObserver);
			secondObserver.SetOther(firstObserver);
			_firstDisposable = first.Subscribe(firstObserver);
			_secondDisposable = second.Subscribe(secondObserver);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_firstDisposable.Dispose();
				_secondDisposable.Dispose();
			}
			base.Dispose(disposing);
		}
	}

	private readonly IObservable<TFirst> _first;

	private readonly IObservable<TSecond> _second;

	private readonly Func<TFirst, TSecond, TResult> _resultSelector;

	public CombineLatest(IObservable<TFirst> first, IObservable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
	{
		_first = first;
		_second = second;
		_resultSelector = resultSelector;
	}

	public IDisposable Subscribe(IObserver<TResult> observer)
	{
		@_ obj = new @_(_resultSelector, observer);
		obj.Run(_first, _second);
		return obj;
	}
}
internal sealed class CombineLatest<TSource, TResult> : IObservable<TResult>
{
	internal sealed class @_ : IdentitySink<TResult>
	{
		private sealed class SourceObserver : IObserver<TSource>, IDisposable
		{
			private readonly @_ _parent;

			private readonly int _index;

			public IDisposable? Disposable { get; set; }

			public SourceObserver(@_ parent, int index)
			{
				_parent = parent;
				_index = index;
			}

			public void OnNext(TSource value)
			{
				_parent.OnNext(_index, value);
			}

			public void OnError(Exception error)
			{
				_parent.OnError(error);
			}

			public void OnCompleted()
			{
				_parent.OnCompleted(_index);
			}

			public void Dispose()
			{
				Disposable?.Dispose();
			}
		}

		private readonly object _gate = new object();

		private readonly Func<TSource[], TResult> _resultSelector;

		private bool[] _hasValue;

		private bool _hasValueAll;

		private TSource[] _values;

		private bool[] _isDone;

		private IDisposable[] _subscriptions;

		public _(Func<TSource[], TResult> resultSelector, IObserver<TResult> observer)
			: base(observer)
		{
			_resultSelector = resultSelector;
			_hasValue = null;
			_values = null;
			_isDone = null;
			_subscriptions = null;
		}

		public void Run(IEnumerable<IObservable<TSource>> sources)
		{
			IObservable<TSource>[] array = sources.ToArray();
			int num = array.Length;
			_hasValue = new bool[num];
			_hasValueAll = false;
			_values = new TSource[num];
			_isDone = new bool[num];
			_subscriptions = new IDisposable[num];
			for (int i = 0; i < num; i++)
			{
				int num2 = i;
				SourceObserver sourceObserver = new SourceObserver(this, num2);
				_subscriptions[num2] = sourceObserver;
				sourceObserver.Disposable = array[num2].Subscribe(sourceObserver);
			}
			SetUpstream(new CompositeDisposable(_subscriptions));
		}

		private void OnNext(int index, TSource value)
		{
			lock (_gate)
			{
				_values[index] = value;
				_hasValue[index] = true;
				if (_hasValueAll || (_hasValueAll = _hasValue.All((bool v) => v)))
				{
					TResult value2;
					try
					{
						value2 = _resultSelector(_values);
					}
					catch (Exception error)
					{
						ForwardOnError(error);
						return;
					}
					ForwardOnNext(value2);
				}
				else if (_isDone.Where((bool flag, int i) => i != index).All((bool d) => d))
				{
					ForwardOnCompleted();
				}
			}
		}

		private new void OnError(Exception error)
		{
			lock (_gate)
			{
				ForwardOnError(error);
			}
		}

		private void OnCompleted(int index)
		{
			lock (_gate)
			{
				_isDone[index] = true;
				if (_isDone.All((bool d) => d))
				{
					ForwardOnCompleted();
				}
				else
				{
					_subscriptions[index].Dispose();
				}
			}
		}
	}

	private readonly IEnumerable<IObservable<TSource>> _sources;

	private readonly Func<TSource[], TResult> _resultSelector;

	public CombineLatest(IEnumerable<IObservable<TSource>> sources, Func<TSource[], TResult> resultSelector)
	{
		_sources = sources;
		_resultSelector = resultSelector;
	}

	public IDisposable Subscribe(IObserver<TResult> observer)
	{
		@_ obj = new @_(_resultSelector, observer);
		obj.Run(_sources);
		return obj;
	}
}
