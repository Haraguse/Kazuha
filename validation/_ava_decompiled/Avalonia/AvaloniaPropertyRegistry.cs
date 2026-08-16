using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Avalonia;

/// <summary>
/// Tracks registered <see cref="T:Avalonia.AvaloniaProperty" /> instances.
/// </summary>
public class AvaloniaPropertyRegistry
{
	private readonly Dictionary<int, AvaloniaProperty> _properties = new Dictionary<int, AvaloniaProperty>();

	private readonly Dictionary<Type, Dictionary<int, AvaloniaProperty>> _registered = new Dictionary<Type, Dictionary<int, AvaloniaProperty>>();

	private readonly Dictionary<Type, Dictionary<int, AvaloniaProperty>> _attached = new Dictionary<Type, Dictionary<int, AvaloniaProperty>>();

	private readonly Dictionary<Type, Dictionary<int, AvaloniaProperty>> _direct = new Dictionary<Type, Dictionary<int, AvaloniaProperty>>();

	private readonly Dictionary<Type, List<AvaloniaProperty>> _registeredCache = new Dictionary<Type, List<AvaloniaProperty>>();

	private readonly Dictionary<Type, List<AvaloniaProperty>> _attachedCache = new Dictionary<Type, List<AvaloniaProperty>>();

	private readonly Dictionary<Type, List<AvaloniaProperty>> _directCache = new Dictionary<Type, List<AvaloniaProperty>>();

	private readonly Dictionary<Type, List<AvaloniaProperty>> _inheritedCache = new Dictionary<Type, List<AvaloniaProperty>>();

	private object _unregisteringLocker = new object();

	/// <summary>
	/// Gets the <see cref="T:Avalonia.AvaloniaPropertyRegistry" /> instance
	/// </summary>
	public static AvaloniaPropertyRegistry Instance { get; } = new AvaloniaPropertyRegistry();

	/// <summary>
	/// Gets a list of all registered properties.
	/// </summary>
	internal IReadOnlyCollection<AvaloniaProperty> Properties => _properties.Values;

	/// <summary>
	/// Unregister all<see cref="T:Avalonia.AvaloniaProperty" />s registered on types
	/// </summary>
	/// <param name="types"></param>
	/// <exception cref="T:System.ArgumentNullException"></exception>
	public bool UnregisterByModule(IEnumerable<Type> types)
	{
		if (types == null)
		{
			throw new ArgumentNullException("types");
		}
		lock (_unregisteringLocker)
		{
			try
			{
				foreach (Type type in types)
				{
					Unregister(_registered, type);
					Unregister(_attached, type);
					Unregister(_direct, type);
					Unregister(_registeredCache, type);
					Unregister(_attachedCache, type);
					Unregister(_directCache, type);
					Unregister(_inheritedCache, type);
				}
			}
			catch (Exception)
			{
				return false;
			}
		}
		return true;
	}

	private void Unregister(Dictionary<Type, List<AvaloniaProperty>> dictionary, Type type)
	{
		dictionary.Remove(type);
	}

	private void Unregister(Dictionary<Type, Dictionary<int, AvaloniaProperty>> dictionary, Type type)
	{
		foreach (KeyValuePair<Type, Dictionary<int, AvaloniaProperty>> item in dictionary)
		{
			foreach (KeyValuePair<int, AvaloniaProperty> item2 in item.Value)
			{
				item2.Value.Unregister(type);
			}
		}
	}

	/// <summary>
	/// Gets all non-attached <see cref="T:Avalonia.AvaloniaProperty" />s registered on a type.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>A collection of <see cref="T:Avalonia.AvaloniaProperty" /> definitions.</returns>
	[UnconditionalSuppressMessage("Trimming", "IL2059", Justification = "If type was trimmed out, no properties were referenced")]
	public IReadOnlyList<AvaloniaProperty> GetRegistered(Type type)
	{
		if ((object)type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (_registeredCache.TryGetValue(type, out List<AvaloniaProperty> value))
		{
			return value;
		}
		Type type2 = type;
		value = new List<AvaloniaProperty>();
		lock (_unregisteringLocker)
		{
			while (type2 != null)
			{
				RuntimeHelpers.RunClassConstructor(type2.TypeHandle);
				if (_registered.TryGetValue(type2, out Dictionary<int, AvaloniaProperty> value2))
				{
					value.AddRange(value2.Values);
				}
				type2 = type2.BaseType;
			}
			_registeredCache.Add(type, value);
			return value;
		}
	}

	/// <summary>
	/// Gets all attached <see cref="T:Avalonia.AvaloniaProperty" />s registered on a type.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>A collection of <see cref="T:Avalonia.AvaloniaProperty" /> definitions.</returns>
	public IReadOnlyList<AvaloniaProperty> GetRegisteredAttached(Type type)
	{
		if ((object)type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (_attachedCache.TryGetValue(type, out List<AvaloniaProperty> value))
		{
			return value;
		}
		Type type2 = type;
		value = new List<AvaloniaProperty>();
		lock (_unregisteringLocker)
		{
			while (type2 != null)
			{
				if (_attached.TryGetValue(type2, out Dictionary<int, AvaloniaProperty> value2))
				{
					value.AddRange(value2.Values);
				}
				type2 = type2.BaseType;
			}
			_attachedCache.Add(type, value);
			return value;
		}
	}

	/// <summary>
	/// Gets all direct <see cref="T:Avalonia.AvaloniaProperty" />s registered on a type.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>A collection of <see cref="T:Avalonia.AvaloniaProperty" /> definitions.</returns>
	public IReadOnlyList<AvaloniaProperty> GetRegisteredDirect(Type type)
	{
		if ((object)type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (_directCache.TryGetValue(type, out List<AvaloniaProperty> value))
		{
			return value;
		}
		Type type2 = type;
		value = new List<AvaloniaProperty>();
		lock (_unregisteringLocker)
		{
			while (type2 != null)
			{
				if (_direct.TryGetValue(type2, out Dictionary<int, AvaloniaProperty> value2))
				{
					value.AddRange(value2.Values);
				}
				type2 = type2.BaseType;
			}
			_directCache.Add(type, value);
			return value;
		}
	}

	/// <summary>
	/// Gets all inherited <see cref="T:Avalonia.AvaloniaProperty" />s registered on a type.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>A collection of <see cref="T:Avalonia.AvaloniaProperty" /> definitions.</returns>
	public IReadOnlyList<AvaloniaProperty> GetRegisteredInherited(Type type)
	{
		if ((object)type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (_inheritedCache.TryGetValue(type, out List<AvaloniaProperty> value))
		{
			return value;
		}
		value = new List<AvaloniaProperty>();
		HashSet<AvaloniaProperty> hashSet = new HashSet<AvaloniaProperty>();
		IReadOnlyList<AvaloniaProperty> registered = GetRegistered(type);
		int count = registered.Count;
		for (int i = 0; i < count; i++)
		{
			AvaloniaProperty avaloniaProperty = registered[i];
			if (avaloniaProperty.Inherits)
			{
				value.Add(avaloniaProperty);
				hashSet.Add(avaloniaProperty);
			}
		}
		IReadOnlyList<AvaloniaProperty> registeredAttached = GetRegisteredAttached(type);
		int count2 = registeredAttached.Count;
		for (int j = 0; j < count2; j++)
		{
			AvaloniaProperty avaloniaProperty2 = registeredAttached[j];
			if (avaloniaProperty2.Inherits && !hashSet.Contains(avaloniaProperty2))
			{
				value.Add(avaloniaProperty2);
			}
		}
		lock (_unregisteringLocker)
		{
			_inheritedCache.Add(type, value);
			return value;
		}
	}

	/// <summary>
	/// Gets all <see cref="T:Avalonia.AvaloniaProperty" />s registered on a object.
	/// </summary>
	/// <param name="o">The object.</param>
	/// <returns>A collection of <see cref="T:Avalonia.AvaloniaProperty" /> definitions.</returns>
	public IReadOnlyList<AvaloniaProperty> GetRegistered(AvaloniaObject o)
	{
		if (o == null)
		{
			throw new ArgumentNullException("o");
		}
		return GetRegistered(o.GetType());
	}

	/// <summary>
	/// Gets a direct property as registered on an object.
	/// </summary>
	/// <param name="o">The object.</param>
	/// <param name="property">The direct property.</param>
	/// <returns>
	/// The registered.
	/// </returns>
	public DirectPropertyBase<T> GetRegisteredDirect<T>(AvaloniaObject o, DirectPropertyBase<T> property)
	{
		return (DirectPropertyBase<T>)GetRegisteredDirectUntyped(o, property);
	}

	internal AvaloniaProperty GetRegisteredDirectUntyped(AvaloniaObject o, AvaloniaProperty property)
	{
		return FindRegisteredDirectUntyped(o, property) ?? throw new ArgumentException($"Property '{property.Name} not registered on '{o.GetType()}");
	}

	/// <summary>
	/// Finds a registered property on a type by name.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <param name="name">The property name.</param>
	/// <returns>
	/// The registered property or null if no matching property found.
	/// </returns>
	/// <exception cref="T:System.InvalidOperationException">
	/// The property name contains a '.'.
	/// </exception>
	public AvaloniaProperty? FindRegistered(Type type, string name)
	{
		if ((object)type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Contains('.'))
		{
			throw new InvalidOperationException("Attached properties not supported.");
		}
		IReadOnlyList<AvaloniaProperty> registered = GetRegistered(type);
		int count = registered.Count;
		for (int i = 0; i < count; i++)
		{
			AvaloniaProperty avaloniaProperty = registered[i];
			if (avaloniaProperty.Name == name)
			{
				return avaloniaProperty;
			}
		}
		return null;
	}

	/// <summary>
	/// Finds a registered property on an object by name.
	/// </summary>
	/// <param name="o">The object.</param>
	/// <param name="name">The property name.</param>
	/// <returns>
	/// The registered property or null if no matching property found.
	/// </returns>
	/// <exception cref="T:System.InvalidOperationException">
	/// The property name contains a '.'.
	/// </exception>
	public AvaloniaProperty? FindRegistered(AvaloniaObject o, string name)
	{
		if (o == null)
		{
			throw new ArgumentNullException("o");
		}
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		return FindRegistered(o.GetType(), name);
	}

	/// <summary>
	/// Finds a direct property as registered on an object.
	/// </summary>
	/// <param name="o">The object.</param>
	/// <param name="property">The direct property.</param>
	/// <returns>
	/// The registered property or null if no matching property found.
	/// </returns>
	public DirectPropertyBase<T>? FindRegisteredDirect<T>(AvaloniaObject o, DirectPropertyBase<T> property)
	{
		return (DirectPropertyBase<T>)FindRegisteredDirectUntyped(o, property);
	}

	private AvaloniaProperty? FindRegisteredDirectUntyped(AvaloniaObject o, AvaloniaProperty property)
	{
		if (property.OwnerType == o.GetType())
		{
			return property;
		}
		IReadOnlyList<AvaloniaProperty> registeredDirect = GetRegisteredDirect(o.GetType());
		int count = registeredDirect.Count;
		for (int i = 0; i < count; i++)
		{
			AvaloniaProperty avaloniaProperty = registeredDirect[i];
			if (avaloniaProperty == property)
			{
				return avaloniaProperty;
			}
		}
		return null;
	}

	/// <summary>
	/// Finds a registered property by Id.
	/// </summary>
	/// <param name="id">The property Id.</param>
	/// <returns>The registered property or null if no matching property found.</returns>
	internal AvaloniaProperty? FindRegistered(int id)
	{
		if (id >= _properties.Count)
		{
			return null;
		}
		return _properties[id];
	}

	/// <summary>
	/// Checks whether a <see cref="T:Avalonia.AvaloniaProperty" /> is registered on a type.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <param name="property">The property.</param>
	/// <returns>True if the property is registered, otherwise false.</returns>
	public bool IsRegistered(Type type, AvaloniaProperty property)
	{
		if ((object)type == null)
		{
			throw new ArgumentNullException("type");
		}
		if ((object)property == null)
		{
			throw new ArgumentNullException("property");
		}
		if (!ContainsProperty(Instance.GetRegistered(type), property))
		{
			return ContainsProperty(Instance.GetRegisteredAttached(type), property);
		}
		return true;
		static bool ContainsProperty(IReadOnlyList<AvaloniaProperty> properties, AvaloniaProperty avaloniaProperty)
		{
			int count = properties.Count;
			for (int i = 0; i < count; i++)
			{
				if (properties[i] == avaloniaProperty)
				{
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>
	/// Checks whether a <see cref="T:Avalonia.AvaloniaProperty" /> is registered on a object.
	/// </summary>
	/// <param name="o">The object.</param>
	/// <param name="property">The property.</param>
	/// <returns>True if the property is registered, otherwise false.</returns>
	public bool IsRegistered(object o, AvaloniaProperty property)
	{
		if (o == null)
		{
			throw new ArgumentNullException("o");
		}
		if ((object)property == null)
		{
			throw new ArgumentNullException("property");
		}
		return IsRegistered(o.GetType(), property);
	}

	/// <summary>
	/// Registers a <see cref="T:Avalonia.AvaloniaProperty" /> on a type.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <param name="property">The property.</param>
	/// <remarks>
	/// You won't usually want to call this method directly, instead use the
	/// <see cref="M:Avalonia.AvaloniaProperty.Register``2(System.String,``1,System.Boolean,Avalonia.Data.BindingMode,System.Func{``1,System.Boolean},System.Func{Avalonia.AvaloniaObject,``1,``1},System.Boolean)" />
	/// method.
	/// </remarks>
	public void Register(Type type, AvaloniaProperty property)
	{
		if ((object)type == null)
		{
			throw new ArgumentNullException("type");
		}
		if ((object)property == null)
		{
			throw new ArgumentNullException("property");
		}
		lock (_unregisteringLocker)
		{
			if (!_registered.TryGetValue(type, out Dictionary<int, AvaloniaProperty> value))
			{
				value = new Dictionary<int, AvaloniaProperty>();
				value.Add(property.Id, property);
				_registered.Add(type, value);
			}
			else if (!value.ContainsKey(property.Id))
			{
				value.Add(property.Id, property);
			}
			if (property.IsDirect)
			{
				if (!_direct.TryGetValue(type, out value))
				{
					value = new Dictionary<int, AvaloniaProperty>();
					value.Add(property.Id, property);
					_direct.Add(type, value);
				}
				else if (!value.ContainsKey(property.Id))
				{
					value.Add(property.Id, property);
				}
				_directCache.Clear();
			}
			if (!_properties.ContainsKey(property.Id))
			{
				_properties.Add(property.Id, property);
			}
			_registeredCache.Clear();
			_inheritedCache.Clear();
		}
	}

	/// <summary>
	/// Registers an attached <see cref="T:Avalonia.AvaloniaProperty" /> on a type.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <param name="property">The property.</param>
	/// <remarks>
	/// You won't usually want to call this method directly, instead use the
	/// <see cref="M:Avalonia.AvaloniaProperty.RegisterAttached``2(System.String,System.Type,``1,System.Boolean,Avalonia.Data.BindingMode,System.Func{``1,System.Boolean},System.Func{Avalonia.AvaloniaObject,``1,``1})" />
	/// method.
	/// </remarks>
	public void RegisterAttached(Type type, AvaloniaProperty property)
	{
		if ((object)type == null)
		{
			throw new ArgumentNullException("type");
		}
		if ((object)property == null)
		{
			throw new ArgumentNullException("property");
		}
		if (!property.IsAttached)
		{
			throw new InvalidOperationException("Cannot register a non-attached property as attached.");
		}
		lock (_unregisteringLocker)
		{
			if (!_attached.TryGetValue(type, out Dictionary<int, AvaloniaProperty> value))
			{
				value = new Dictionary<int, AvaloniaProperty>();
				value.Add(property.Id, property);
				_attached.Add(type, value);
			}
			else
			{
				value.Add(property.Id, property);
			}
			_attachedCache.Clear();
			_inheritedCache.Clear();
		}
	}
}
