using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;
using Avalonia.Utilities;

namespace Avalonia.PropertyStore;

internal abstract class ValueFrame
{
	private List<IValueEntry>? _entries;

	private AvaloniaPropertyDictionary<IValueEntry> _index;

	private ValueStore? _owner;

	private bool _isShared;

	public int EntryCount => _index.Count;

	public ValueStore? Owner
	{
		get
		{
			if (_isShared)
			{
				throw new AvaloniaInternalException("Cannot get owner for shared ValueFrame");
			}
			return _owner;
		}
	}

	public BindingPriority Priority { get; }

	public FramePriority FramePriority { get; }

	protected ValueFrame(BindingPriority priority, FrameType type)
	{
		Priority = priority;
		FramePriority = priority.ToFramePriority(type);
	}

	public bool IsActive()
	{
		bool hasChanged;
		return GetIsActive(out hasChanged);
	}

	public bool Contains(AvaloniaProperty property)
	{
		return _index.ContainsKey(property);
	}

	public IValueEntry GetEntry(int index)
	{
		return _entries?[index] ?? _index[0];
	}

	public void SetOwner(ValueStore? owner)
	{
		if (_owner != null && owner != null)
		{
			throw new AvaloniaInternalException("ValueFrame already has an owner.");
		}
		if (!_isShared)
		{
			_owner = owner;
		}
	}

	public bool TryGetEntryIfActive(AvaloniaProperty property, [NotNullWhen(true)] out IValueEntry? entry, out bool activeChanged)
	{
		if (_index.TryGetValue(property, out entry))
		{
			return GetIsActive(out activeChanged);
		}
		activeChanged = false;
		return false;
	}

	public void OnBindingCompleted(IValueEntry binding)
	{
		AvaloniaProperty property = binding.Property;
		Remove(property);
		Owner?.OnValueEntryRemoved(this, property);
	}

	public virtual void Dispose()
	{
		for (int i = 0; i < _index.Count; i++)
		{
			_index[i].Unsubscribe();
		}
	}

	protected abstract bool GetIsActive(out bool hasChanged);

	protected void MakeShared()
	{
		_isShared = true;
		_owner = null;
	}

	protected void Add(IValueEntry value)
	{
		if (_entries == null && _index.Count == 1)
		{
			_entries = new List<IValueEntry>();
			_entries.Add(_index[0]);
		}
		_index.Add(value.Property, value);
		_entries?.Add(value);
	}

	protected void Remove(AvaloniaProperty property)
	{
		if (_entries != null)
		{
			int count = _entries.Count;
			for (int i = 0; i < count; i++)
			{
				if (_entries[i].Property == property)
				{
					_entries.RemoveAt(i);
					break;
				}
			}
		}
		_index.Remove(property);
	}
}
