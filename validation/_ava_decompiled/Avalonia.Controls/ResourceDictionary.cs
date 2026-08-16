using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Templates;
using Avalonia.Styling;

namespace Avalonia.Controls;

/// <summary>
/// An indexed dictionary of resources.
/// </summary>
public class ResourceDictionary : ResourceProvider, IResourceDictionary, IResourceProvider, IResourceNode, IDictionary<object, object?>, ICollection<KeyValuePair<object, object?>>, IEnumerable<KeyValuePair<object, object?>>, IEnumerable, IThemeVariantProvider
{
	private sealed class DeferredItem : IDeferredContent
	{
		private readonly Func<IServiceProvider?, object?> _factory;

		public DeferredItem(Func<IServiceProvider?, object?> factory)
		{
			_factory = factory;
		}

		public object? Build(IServiceProvider? serviceProvider)
		{
			return _factory(serviceProvider);
		}
	}

	private sealed class NotSharedDeferredItem(IDeferredContent deferredContent) : IDeferredContent
	{
		private readonly IDeferredContent _deferredContent = deferredContent;

		public object? Build(IServiceProvider? serviceProvider)
		{
			return _deferredContent.Build(serviceProvider);
		}
	}

	private object? _lastDeferredItemKey;

	private Dictionary<object, object?>? _inner;

	private AvaloniaList<IResourceProvider>? _mergedDictionaries;

	private AvaloniaDictionary<ThemeVariant, IThemeVariantProvider>? _themeDictionary;

	public int Count => _inner?.Count ?? 0;

	public object? this[object key]
	{
		get
		{
			TryGetValue(key, out object value);
			return value;
		}
		set
		{
			Inner[key] = value;
			RaiseResourcesChanged();
		}
	}

	public ICollection<object> Keys
	{
		get
		{
			ICollection<object> collection = _inner?.Keys;
			return collection ?? Array.Empty<object>();
		}
	}

	public ICollection<object?> Values
	{
		get
		{
			ICollection<object> collection = _inner?.Values;
			return collection ?? Array.Empty<object>();
		}
	}

	public IList<IResourceProvider> MergedDictionaries
	{
		get
		{
			if (_mergedDictionaries == null)
			{
				_mergedDictionaries = new AvaloniaList<IResourceProvider>();
				_mergedDictionaries.ResetBehavior = ResetBehavior.Remove;
				_mergedDictionaries.ForEachItem(delegate(IResourceProvider x)
				{
					if (base.Owner != null)
					{
						x.AddOwner(base.Owner);
					}
				}, delegate(IResourceProvider x)
				{
					if (base.Owner != null)
					{
						x.RemoveOwner(base.Owner);
					}
				}, delegate
				{
					throw new NotSupportedException("Dictionary reset not supported");
				});
			}
			return _mergedDictionaries;
		}
	}

	public IDictionary<ThemeVariant, IThemeVariantProvider> ThemeDictionaries
	{
		get
		{
			if (_themeDictionary == null)
			{
				_themeDictionary = new AvaloniaDictionary<ThemeVariant, IThemeVariantProvider>(2);
				_themeDictionary.ForEachItem(delegate(ThemeVariant _, IThemeVariantProvider x)
				{
					if (base.Owner != null)
					{
						x.AddOwner(base.Owner);
					}
				}, delegate(ThemeVariant _, IThemeVariantProvider x)
				{
					if (base.Owner != null)
					{
						x.RemoveOwner(base.Owner);
					}
				}, delegate
				{
					throw new NotSupportedException("Dictionary reset not supported");
				});
			}
			return _themeDictionary;
		}
	}

	ThemeVariant? IThemeVariantProvider.Key { get; set; }

	public sealed override bool HasResources
	{
		get
		{
			Dictionary<object, object?>? inner = _inner;
			if (inner != null && inner.Count > 0)
			{
				return true;
			}
			AvaloniaList<IResourceProvider>? mergedDictionaries = _mergedDictionaries;
			if (mergedDictionaries != null && mergedDictionaries.Count > 0)
			{
				foreach (IResourceProvider mergedDictionary in _mergedDictionaries)
				{
					if (mergedDictionary.HasResources)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	bool ICollection<KeyValuePair<object, object?>>.IsReadOnly => false;

	private Dictionary<object, object?> Inner => _inner ?? (_inner = new Dictionary<object, object>());

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Controls.ResourceDictionary" /> class.
	/// </summary>
	public ResourceDictionary()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Controls.ResourceDictionary" /> class.
	/// </summary>
	public ResourceDictionary(IResourceHost owner)
		: base(owner)
	{
	}

	public void Add(object key, object? value)
	{
		Inner.Add(key, value);
		RaiseResourcesChanged();
	}

	public void AddDeferred(object key, Func<IServiceProvider?, object?> factory)
	{
		Add(key, new DeferredItem(factory));
	}

	public void AddDeferred(object key, IDeferredContent deferredContent)
	{
		Add(key, deferredContent);
	}

	public void AddNotSharedDeferred(object key, IDeferredContent deferredContent)
	{
		Add(key, new NotSharedDeferredItem(deferredContent));
	}

	public void SetItems(IEnumerable<KeyValuePair<object, object?>> values)
	{
		try
		{
			foreach (KeyValuePair<object, object> value in values)
			{
				Inner[value.Key] = value.Value;
			}
		}
		finally
		{
			RaiseResourcesChanged();
		}
	}

	public void Clear()
	{
		Dictionary<object, object?>? inner = _inner;
		if (inner != null && inner.Count > 0)
		{
			_inner.Clear();
			RaiseResourcesChanged();
		}
	}

	public bool ContainsKey(object key)
	{
		return _inner?.ContainsKey(key) ?? false;
	}

	public bool Remove(object key)
	{
		Dictionary<object, object?>? inner = _inner;
		if (inner != null && inner.Remove(key))
		{
			RaiseResourcesChanged();
			return true;
		}
		return false;
	}

	public sealed override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
	{
		if (TryGetValue(key, out value))
		{
			return true;
		}
		if (_themeDictionary != null)
		{
			IThemeVariantProvider value2;
			if ((object)theme != null && theme != ThemeVariant.Default)
			{
				if (_themeDictionary.TryGetValue(theme, out value2) && value2.TryGetResource(key, theme, out value))
				{
					return true;
				}
				ThemeVariant inheritVariant = theme.InheritVariant;
				while ((object)inheritVariant != null)
				{
					if (_themeDictionary.TryGetValue(inheritVariant, out value2) && value2.TryGetResource(key, theme, out value))
					{
						return true;
					}
					inheritVariant = inheritVariant.InheritVariant;
				}
			}
			if (_themeDictionary.TryGetValue(ThemeVariant.Default, out value2) && value2.TryGetResource(key, theme, out value))
			{
				return true;
			}
		}
		if (_mergedDictionaries != null)
		{
			for (int num = _mergedDictionaries.Count - 1; num >= 0; num--)
			{
				if (_mergedDictionaries[num].TryGetResource(key, theme, out value))
				{
					return true;
				}
			}
		}
		value = null;
		return false;
	}

	public bool TryGetValue(object key, out object? value)
	{
		if (_inner != null && _inner.TryGetValue(key, out value))
		{
			if (value is IDeferredContent deferredContent)
			{
				if (_lastDeferredItemKey == key)
				{
					value = null;
					return false;
				}
				try
				{
					_lastDeferredItemKey = key;
					object obj = deferredContent.Build(null);
					object obj2 = ((obj is ITemplateResult templateResult) ? templateResult.Result : ((obj == null) ? null : obj));
					value = obj2;
					if (!(deferredContent is NotSharedDeferredItem))
					{
						_inner[key] = value;
					}
				}
				finally
				{
					_lastDeferredItemKey = null;
				}
			}
			return true;
		}
		value = null;
		return false;
	}

	/// <summary>
	/// Ensures that the resource dictionary can hold up to <paramref name="capacity" /> entries without
	/// any further expansion of its backing storage.
	/// </summary>
	/// <remarks>This method may have no effect when targeting .NET Standard 2.0.</remarks>
	public void EnsureCapacity(int capacity)
	{
		if (_inner == null)
		{
			_inner = new Dictionary<object, object>(capacity);
		}
		else
		{
			Inner.EnsureCapacity(capacity);
		}
	}

	public IEnumerator<KeyValuePair<object, object?>> GetEnumerator()
	{
		Dictionary<object, object>.Enumerator? enumerator = _inner?.GetEnumerator();
		if (!enumerator.HasValue)
		{
			return Enumerable.Empty<KeyValuePair<object, object>>().GetEnumerator();
		}
		return enumerator.GetValueOrDefault();
	}

	void ICollection<KeyValuePair<object, object?>>.Add(KeyValuePair<object, object?> item)
	{
		Add(item.Key, item.Value);
	}

	bool ICollection<KeyValuePair<object, object?>>.Contains(KeyValuePair<object, object?> item)
	{
		return ((ICollection<KeyValuePair<object, object>>)_inner)?.Contains(item) ?? false;
	}

	void ICollection<KeyValuePair<object, object?>>.CopyTo(KeyValuePair<object, object?>[] array, int arrayIndex)
	{
		((ICollection<KeyValuePair<object, object>>)_inner)?.CopyTo(array, arrayIndex);
	}

	bool ICollection<KeyValuePair<object, object?>>.Remove(KeyValuePair<object, object?> item)
	{
		Dictionary<object, object?>? inner = _inner;
		if (inner != null && ((ICollection<KeyValuePair<object, object>>)inner).Remove(item))
		{
			RaiseResourcesChanged();
			return true;
		}
		return false;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	internal bool ContainsDeferredKey(object key)
	{
		if (_inner != null && _inner.TryGetValue(key, out object value))
		{
			return value is IDeferredContent;
		}
		return false;
	}

	protected sealed override void OnAddOwner(IResourceHost owner)
	{
		Dictionary<object, object?>? inner = _inner;
		bool flag = inner != null && inner.Count > 0;
		if (_mergedDictionaries != null)
		{
			foreach (IResourceProvider mergedDictionary in _mergedDictionaries)
			{
				mergedDictionary.AddOwner(owner);
				flag |= mergedDictionary.HasResources;
			}
		}
		if (_themeDictionary != null)
		{
			foreach (IThemeVariantProvider value in _themeDictionary.Values)
			{
				value.AddOwner(owner);
				flag |= value.HasResources;
			}
		}
		if (flag)
		{
			owner.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Create());
		}
	}

	protected sealed override void OnRemoveOwner(IResourceHost owner)
	{
		Dictionary<object, object?>? inner = _inner;
		bool flag = inner != null && inner.Count > 0;
		if (_mergedDictionaries != null)
		{
			foreach (IResourceProvider mergedDictionary in _mergedDictionaries)
			{
				mergedDictionary.RemoveOwner(owner);
				flag |= mergedDictionary.HasResources;
			}
		}
		if (_themeDictionary != null)
		{
			foreach (IThemeVariantProvider value in _themeDictionary.Values)
			{
				value.RemoveOwner(owner);
				flag |= value.HasResources;
			}
		}
		if (flag)
		{
			owner.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Create());
		}
	}
}
