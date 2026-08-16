using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// <see cref="T:Avalonia.Rendering.Composition.CompositionPropertySet" />s are <see cref="T:Avalonia.Rendering.Composition.CompositionObject" />s that allow storage of key values pairs
/// that can be shared across the application and are not tied to the lifetime of another composition object.
/// <see cref="T:Avalonia.Rendering.Composition.CompositionPropertySet" />s are most commonly used with animations, where they maintain key-value pairs
/// that are referenced to drive portions of composition animations. <see cref="T:Avalonia.Rendering.Composition.CompositionPropertySet" />s
/// provide the ability to insert key-value pairs or retrieve a value for a given key.
/// <see cref="T:Avalonia.Rendering.Composition.CompositionPropertySet" /> does not support a delete function – ensure you use <see cref="T:Avalonia.Rendering.Composition.CompositionPropertySet" />
/// to store values that will be shared across the application.
/// </summary>
public sealed class CompositionPropertySet : CompositionObject
{
	private readonly Dictionary<string, ExpressionVariant> _variants = new Dictionary<string, ExpressionVariant>();

	private readonly Dictionary<string, CompositionObject> _objects = new Dictionary<string, CompositionObject>();

	internal CompositionPropertySet(Compositor compositor)
		: base(compositor, null)
	{
	}

	internal void Set(string key, ExpressionVariant value)
	{
		_objects.Remove(key);
		_variants[key] = value;
	}

	internal void Set(string key, CompositionObject obj)
	{
		_objects[key] = obj ?? throw new ArgumentNullException("obj");
		_variants.Remove(key);
	}

	public void InsertColor(string propertyName, Color value)
	{
		Set(propertyName, value);
	}

	public void InsertMatrix3x2(string propertyName, Matrix3x2 value)
	{
		Set(propertyName, value);
	}

	public void InsertMatrix4x4(string propertyName, Matrix4x4 value)
	{
		Set(propertyName, value);
	}

	public void InsertQuaternion(string propertyName, Quaternion value)
	{
		Set(propertyName, value);
	}

	public void InsertScalar(string propertyName, float value)
	{
		Set(propertyName, value);
	}

	public void InsertVector2(string propertyName, Vector2 value)
	{
		Set(propertyName, value);
	}

	public void InsertVector3(string propertyName, Vector3 value)
	{
		Set(propertyName, value);
	}

	public void InsertVector4(string propertyName, Vector4 value)
	{
		Set(propertyName, value);
	}

	private CompositionGetValueStatus TryGetVariant<T>(string key, out T value) where T : struct
	{
		value = default(T);
		if (!_variants.TryGetValue(key, out var value2))
		{
			if (!_objects.ContainsKey(key))
			{
				return CompositionGetValueStatus.NotFound;
			}
			return CompositionGetValueStatus.TypeMismatch;
		}
		if (!value2.TryCast<T>(out value))
		{
			return CompositionGetValueStatus.TypeMismatch;
		}
		return CompositionGetValueStatus.Succeeded;
	}

	public CompositionGetValueStatus TryGetColor(string propertyName, out Color value)
	{
		return TryGetVariant<Color>(propertyName, out value);
	}

	public CompositionGetValueStatus TryGetMatrix3x2(string propertyName, out Matrix3x2 value)
	{
		return TryGetVariant<Matrix3x2>(propertyName, out value);
	}

	public CompositionGetValueStatus TryGetMatrix4x4(string propertyName, out Matrix4x4 value)
	{
		return TryGetVariant<Matrix4x4>(propertyName, out value);
	}

	public CompositionGetValueStatus TryGetQuaternion(string propertyName, out Quaternion value)
	{
		return TryGetVariant<Quaternion>(propertyName, out value);
	}

	public CompositionGetValueStatus TryGetScalar(string propertyName, out float value)
	{
		return TryGetVariant<float>(propertyName, out value);
	}

	public CompositionGetValueStatus TryGetVector2(string propertyName, out Vector2 value)
	{
		return TryGetVariant<Vector2>(propertyName, out value);
	}

	public CompositionGetValueStatus TryGetVector3(string propertyName, out Vector3 value)
	{
		return TryGetVariant<Vector3>(propertyName, out value);
	}

	public CompositionGetValueStatus TryGetVector4(string propertyName, out Vector4 value)
	{
		return TryGetVariant<Vector4>(propertyName, out value);
	}

	public void InsertBoolean(string propertyName, bool value)
	{
		Set(propertyName, value);
	}

	public CompositionGetValueStatus TryGetBoolean(string propertyName, out bool value)
	{
		return TryGetVariant<bool>(propertyName, out value);
	}

	internal void ClearAll()
	{
		_objects.Clear();
		_variants.Clear();
	}

	internal void Clear(string key)
	{
		_objects.Remove(key);
		_variants.Remove(key);
	}

	internal PropertySetSnapshot Snapshot()
	{
		return SnapshotCore(1);
	}

	private PropertySetSnapshot SnapshotCore(int allowedNestingLevel)
	{
		Dictionary<string, PropertySetSnapshot.Value> dictionary = new Dictionary<string, PropertySetSnapshot.Value>(_objects.Count + _variants.Count);
		foreach (KeyValuePair<string, CompositionObject> @object in _objects)
		{
			if (@object.Value is CompositionPropertySet compositionPropertySet)
			{
				if (allowedNestingLevel <= 0)
				{
					throw new InvalidOperationException("PropertySet depth limit reached");
				}
				dictionary[@object.Key] = new PropertySetSnapshot.Value(compositionPropertySet.SnapshotCore(allowedNestingLevel - 1));
				continue;
			}
			if (@object.Value.Server == null)
			{
				throw new InvalidOperationException($"Object of type {@object.Value.GetType()} is not allowed");
			}
			dictionary[@object.Key] = new PropertySetSnapshot.Value((ServerObject)@object.Value.Server);
		}
		foreach (KeyValuePair<string, ExpressionVariant> variant in _variants)
		{
			dictionary[variant.Key] = variant.Value;
		}
		return new PropertySetSnapshot(dictionary);
	}
}
