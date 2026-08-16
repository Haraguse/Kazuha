using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Utilities;

internal static class AsyncEnumerableHelper
{
	private sealed class EnumerableAsyncWrapper<T> : IAsyncEnumerable<T>
	{
		private readonly IEnumerable<T> _enumerable;

		public EnumerableAsyncWrapper(IEnumerable<T> enumerable)
		{
			_enumerable = enumerable;
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default(CancellationToken))
		{
			return new EnumeratorAsyncWrapper<T>(_enumerable.GetEnumerator(), cancellationToken);
		}
	}

	private sealed class EnumeratorAsyncWrapper<T> : IAsyncEnumerator<T>, IAsyncDisposable
	{
		private readonly IEnumerator<T> _enumerator;

		private readonly CancellationToken _cancellationToken;

		public T Current => _enumerator.Current;

		public EnumeratorAsyncWrapper(IEnumerator<T> enumerator, CancellationToken cancellationToken)
		{
			_enumerator = enumerator;
			_cancellationToken = cancellationToken;
		}

		public ValueTask<bool> MoveNextAsync()
		{
			if (!_cancellationToken.IsCancellationRequested)
			{
				return new ValueTask<bool>(_enumerator.MoveNext());
			}
			return new ValueTask<bool>(Task.FromCanceled<bool>(_cancellationToken));
		}

		public ValueTask DisposeAsync()
		{
			_enumerator.Dispose();
			return default(ValueTask);
		}
	}

	public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerable<T> enumerable)
	{
		return new EnumerableAsyncWrapper<T>(enumerable);
	}
}
