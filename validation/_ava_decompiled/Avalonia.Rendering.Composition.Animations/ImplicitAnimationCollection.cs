using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Rendering.Composition.Animations;

/// <summary>
/// A collection of animations triggered when a condition is met.
/// </summary>
/// <remarks>
/// Implicit animations let you drive animations by specifying trigger conditions rather than requiring the manual definition of animation behavior.
/// They help decouple animation start logic from core app logic. You define animations and the events that should trigger these animations.
/// Currently the only available trigger is animated property change.
///
/// When expression is used in ImplicitAnimationCollection a special keyword `this.FinalValue` will represent
/// the final value of the animated property that was changed 
/// </remarks>
public sealed class ImplicitAnimationCollection : CompositionObject, IDictionary<string, ICompositionAnimationBase>, ICollection<KeyValuePair<string, ICompositionAnimationBase>>, IEnumerable<KeyValuePair<string, ICompositionAnimationBase>>, IEnumerable
{
	private readonly Dictionary<string, ICompositionAnimationBase> _inner = new Dictionary<string, ICompositionAnimationBase>();

	private readonly IDictionary<string, ICompositionAnimationBase> _innerface;

	public int Count => _inner.Count;

	bool ICollection<KeyValuePair<string, ICompositionAnimationBase>>.IsReadOnly => _innerface.IsReadOnly;

	public ICompositionAnimationBase this[string key]
	{
		get
		{
			return _inner[key];
		}
		set
		{
			_inner[key] = value;
		}
	}

	ICollection<string> IDictionary<string, ICompositionAnimationBase>.Keys => _innerface.Keys;

	ICollection<ICompositionAnimationBase> IDictionary<string, ICompositionAnimationBase>.Values => _innerface.Values;

	public uint Size => (uint)Count;

	internal ImplicitAnimationCollection(Compositor compositor)
		: base(compositor, null)
	{
		_innerface = _inner;
	}

	public IEnumerator<KeyValuePair<string, ICompositionAnimationBase>> GetEnumerator()
	{
		return _inner.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)_inner).GetEnumerator();
	}

	void ICollection<KeyValuePair<string, ICompositionAnimationBase>>.Add(KeyValuePair<string, ICompositionAnimationBase> item)
	{
		_innerface.Add(item);
	}

	public void Clear()
	{
		_inner.Clear();
	}

	bool ICollection<KeyValuePair<string, ICompositionAnimationBase>>.Contains(KeyValuePair<string, ICompositionAnimationBase> item)
	{
		return _innerface.Contains(item);
	}

	void ICollection<KeyValuePair<string, ICompositionAnimationBase>>.CopyTo(KeyValuePair<string, ICompositionAnimationBase>[] array, int arrayIndex)
	{
		_innerface.CopyTo(array, arrayIndex);
	}

	bool ICollection<KeyValuePair<string, ICompositionAnimationBase>>.Remove(KeyValuePair<string, ICompositionAnimationBase> item)
	{
		return _innerface.Remove(item);
	}

	public void Add(string key, ICompositionAnimationBase value)
	{
		_inner.Add(key, value);
	}

	public bool ContainsKey(string key)
	{
		return _inner.ContainsKey(key);
	}

	public bool Remove(string key)
	{
		return _inner.Remove(key);
	}

	public bool TryGetValue(string key, [MaybeNullWhen(false)] out ICompositionAnimationBase value)
	{
		return _inner.TryGetValue(key, out value);
	}

	public IReadOnlyDictionary<string, ICompositionAnimationBase> GetView()
	{
		return new Dictionary<string, ICompositionAnimationBase>(this);
	}

	public bool HasKey(string key)
	{
		return ContainsKey(key);
	}

	public void Insert(string key, ICompositionAnimationBase animation)
	{
		Add(key, animation);
	}

	public ICompositionAnimationBase? Lookup(string key)
	{
		_inner.TryGetValue(key, out ICompositionAnimationBase value);
		return value;
	}
}
