using System;
using System.Collections;

namespace FluentAvalonia.UI.Controls;

internal class BreadcrumbIterable : IEnumerable
{
	public class BreadcrumbIterator : IEnumerator
	{
		private readonly FAItemsSourceView _itemsSource;

		private int _currentIndex = -1;

		private readonly int _size;

		public object Current
		{
			get
			{
				if (_currentIndex == 0)
				{
					return null;
				}
				if (HasCurrent())
				{
					return _itemsSource.GetAt(_currentIndex - 1);
				}
				throw new IndexOutOfRangeException();
			}
		}

		public BreadcrumbIterator(IEnumerable itemsSource)
		{
			if (itemsSource != null)
			{
				_itemsSource = new FAItemsSourceView(itemsSource);
				_size = _itemsSource.Count + 1;
			}
			else
			{
				_size = 1;
			}
		}

		public bool MoveNext()
		{
			if (HasCurrent())
			{
				_currentIndex++;
				return HasCurrent();
			}
			throw new IndexOutOfRangeException();
		}

		public void Reset()
		{
		}

		private bool HasCurrent()
		{
			return _currentIndex < _size;
		}
	}

	public IEnumerable ItemsSource { get; set; }

	public BreadcrumbIterable(IEnumerable src)
	{
		ItemsSource = src;
	}

	public IEnumerator GetEnumerator()
	{
		return new BreadcrumbIterator(ItemsSource);
	}
}
