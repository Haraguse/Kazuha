using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Expressions;

internal class ExpressionTrackedObjects : IEnumerable<IExpressionObject>, IEnumerable
{
	public struct Pool
	{
		private readonly Stack<ExpressionTrackedObjects> _stack = new Stack<ExpressionTrackedObjects>();

		public Pool()
		{
		}

		public ExpressionTrackedObjects Get()
		{
			if (_stack.Count > 0)
			{
				return _stack.Pop();
			}
			return new ExpressionTrackedObjects();
		}

		public void Return(ExpressionTrackedObjects obj)
		{
			_stack.Clear();
			_stack.Push(obj);
		}
	}

	private readonly List<IExpressionObject> _list = new List<IExpressionObject>();

	private readonly HashSet<IExpressionObject> _hashSet = new HashSet<IExpressionObject>();

	public void Add(IExpressionObject obj, string member)
	{
		if (_hashSet.Add(obj))
		{
			_list.Add(obj);
		}
	}

	public void Clear()
	{
		_list.Clear();
		_hashSet.Clear();
	}

	IEnumerator<IExpressionObject> IEnumerable<IExpressionObject>.GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)_list).GetEnumerator();
	}

	public List<IExpressionObject>.Enumerator GetEnumerator()
	{
		return _list.GetEnumerator();
	}
}
