using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

internal class ChildrenInTabFocusOrderIterable : IEnumerable<Control>, IEnumerable
{
	private struct ChildrenInTabFocusOrderIterator : IEnumerator<Control>, IEnumerator, IDisposable
	{
		private List<KeyValuePair<int, Control>> _realizedChildren;

		private int _index = 0;

		public Control Current
		{
			get
			{
				if (_index < _realizedChildren.Count)
				{
					return _realizedChildren[_index].Value;
				}
				return null;
			}
		}

		object IEnumerator.Current => Current;

		public ChildrenInTabFocusOrderIterator(FAItemsRepeater repeater)
		{
			Controls children = ((Panel)repeater).Children;
			_realizedChildren = new List<KeyValuePair<int, Control>>(((AvaloniaList<Control>)(object)children).Count);
			for (int i = 0; i < ((AvaloniaList<Control>)(object)children).Count; i++)
			{
				Control val = ((AvaloniaList<Control>)(object)children)[i];
				VirtualizationInfo virtualizationInfo = FAItemsRepeater.GetVirtualizationInfo(val);
				if (virtualizationInfo.IsRealized)
				{
					_realizedChildren.Add(new KeyValuePair<int, Control>(virtualizationInfo.Index, val));
				}
			}
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			if (_index < _realizedChildren.Count)
			{
				_index++;
				return _index < _realizedChildren.Count;
			}
			return false;
		}

		public void Reset()
		{
		}
	}

	private FAItemsRepeater _repeater;

	public ChildrenInTabFocusOrderIterable(FAItemsRepeater owner)
	{
		_repeater = owner;
	}

	public IEnumerator<Control> GetEnumerator()
	{
		return new ChildrenInTabFocusOrderIterator(_repeater);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
