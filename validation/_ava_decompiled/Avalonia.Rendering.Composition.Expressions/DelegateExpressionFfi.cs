using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia.Media;

namespace Avalonia.Rendering.Composition.Expressions;

/// <summary>
/// Foreign function interface for composition animations based on calling delegates
/// </summary>
internal class DelegateExpressionFfi : IExpressionForeignFunctionInterface, IEnumerable
{
	private struct FfiRecord
	{
		public VariantType[] Types;

		public Func<IReadOnlyList<ExpressionVariant>, ExpressionVariant> Delegate;
	}

	private readonly Dictionary<string, Dictionary<int, List<FfiRecord>>> _registry = new Dictionary<string, Dictionary<int, List<FfiRecord>>>();

	private static readonly Dictionary<Type, VariantType> TypeMap = new Dictionary<Type, VariantType>
	{
		[typeof(bool)] = VariantType.Boolean,
		[typeof(float)] = VariantType.Double,
		[typeof(double)] = VariantType.Double,
		[typeof(Vector2)] = VariantType.Vector2,
		[typeof(Vector)] = VariantType.Vector,
		[typeof(Vector3)] = VariantType.Vector3,
		[typeof(Vector3D)] = VariantType.Vector3D,
		[typeof(Vector4)] = VariantType.Vector4,
		[typeof(Matrix3x2)] = VariantType.Matrix3x2,
		[typeof(Matrix4x4)] = VariantType.Matrix4x4,
		[typeof(Quaternion)] = VariantType.Quaternion,
		[typeof(Color)] = VariantType.Color
	};

	public bool Call(string name, IReadOnlyList<ExpressionVariant> arguments, out ExpressionVariant result)
	{
		result = default(ExpressionVariant);
		if (!_registry.TryGetValue(name, out Dictionary<int, List<FfiRecord>> value))
		{
			return false;
		}
		if (!value.TryGetValue(arguments.Count, out var value2))
		{
			return false;
		}
		foreach (FfiRecord item in value2)
		{
			bool flag = true;
			for (int i = 0; i < arguments.Count; i++)
			{
				if (item.Types[i] != arguments[i].Type)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				result = item.Delegate(arguments);
				return true;
			}
		}
		return CallWithCast(value2, arguments, out result, anyCast: false);
	}

	private bool CallWithCast(List<FfiRecord> countGroup, IReadOnlyList<ExpressionVariant> arguments, out ExpressionVariant result, bool anyCast)
	{
		result = default(ExpressionVariant);
		foreach (FfiRecord item in countGroup)
		{
			bool flag = true;
			for (int i = 0; i < arguments.Count; i++)
			{
				VariantType variantType = item.Types[i];
				VariantType type = arguments[i].Type;
				if (variantType != type && (variantType != VariantType.Vector3D || type != VariantType.Vector3) && (variantType != VariantType.Vector || type != VariantType.Vector2) && (!anyCast || ((type != VariantType.Vector3D || variantType != VariantType.Vector3) && (type != VariantType.Vector || variantType != VariantType.Vector2))))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				result = item.Delegate(arguments);
				return true;
			}
		}
		if (!anyCast)
		{
			return CallWithCast(countGroup, arguments, out result, anyCast: true);
		}
		return false;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return Array.Empty<object>().GetEnumerator();
	}

	private void Add(string name, Func<IReadOnlyList<ExpressionVariant>, ExpressionVariant> cb, params Type[] types)
	{
		if (!_registry.TryGetValue(name, out Dictionary<int, List<FfiRecord>> value))
		{
			value = (_registry[name] = new Dictionary<int, List<FfiRecord>>());
		}
		if (!value.TryGetValue(types.Length, out var value2))
		{
			value2 = (value[types.Length] = new List<FfiRecord>());
		}
		value2.Add(new FfiRecord
		{
			Types = types.Select((Type t) => TypeMap[t]).ToArray(),
			Delegate = cb
		});
	}

	public void Add<T1>(string name, Func<T1, ExpressionVariant> cb) where T1 : struct
	{
		Add(name, (IReadOnlyList<ExpressionVariant> args) => cb(args[0].CastOrDefault<T1>()), typeof(T1));
	}

	public void Add<T1, T2>(string name, Func<T1, T2, ExpressionVariant> cb) where T1 : struct where T2 : struct
	{
		Add(name, (IReadOnlyList<ExpressionVariant> args) => cb(args[0].CastOrDefault<T1>(), args[1].CastOrDefault<T2>()), typeof(T1), typeof(T2));
	}

	public void Add<T1, T2, T3>(string name, Func<T1, T2, T3, ExpressionVariant> cb) where T1 : struct where T2 : struct where T3 : struct
	{
		Add(name, (IReadOnlyList<ExpressionVariant> args) => cb(args[0].CastOrDefault<T1>(), args[1].CastOrDefault<T2>(), args[2].CastOrDefault<T3>()), typeof(T1), typeof(T2), typeof(T3));
	}

	public void Add<T1, T2, T3, T4>(string name, Func<T1, T2, T3, T4, ExpressionVariant> cb) where T1 : struct where T2 : struct where T3 : struct where T4 : struct
	{
		Add(name, (IReadOnlyList<ExpressionVariant> args) => cb(args[0].CastOrDefault<T1>(), args[1].CastOrDefault<T2>(), args[2].CastOrDefault<T3>(), args[3].CastOrDefault<T4>()), typeof(T1), typeof(T2), typeof(T3), typeof(T4));
	}

	public void Add<T1, T2, T3, T4, T5>(string name, Func<T1, T2, T3, T4, T5, ExpressionVariant> cb) where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
	{
		Add(name, (IReadOnlyList<ExpressionVariant> args) => cb(args[0].CastOrDefault<T1>(), args[1].CastOrDefault<T2>(), args[2].CastOrDefault<T3>(), args[3].CastOrDefault<T4>(), args[4].CastOrDefault<T5>()), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));
	}

	public void Add<T1, T2, T3, T4, T5, T6>(string name, Func<T1, T2, T3, T4, T5, T6, ExpressionVariant> cb) where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
	{
		Add(name, (IReadOnlyList<ExpressionVariant> args) => cb(args[0].CastOrDefault<T1>(), args[1].CastOrDefault<T2>(), args[2].CastOrDefault<T3>(), args[3].CastOrDefault<T4>(), args[4].CastOrDefault<T5>(), args[4].CastOrDefault<T6>()), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6));
	}

	public void Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, ExpressionVariant> cb) where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct
	{
		Add(name, (IReadOnlyList<ExpressionVariant> args) => cb(args[0].CastOrDefault<T1>(), args[1].CastOrDefault<T2>(), args[2].CastOrDefault<T3>(), args[3].CastOrDefault<T4>(), args[4].CastOrDefault<T5>(), args[4].CastOrDefault<T6>(), args[4].CastOrDefault<T7>(), args[4].CastOrDefault<T8>(), args[4].CastOrDefault<T9>(), args[4].CastOrDefault<T10>(), args[4].CastOrDefault<T11>(), args[4].CastOrDefault<T12>(), args[4].CastOrDefault<T13>(), args[4].CastOrDefault<T14>(), args[4].CastOrDefault<T15>(), args[4].CastOrDefault<T16>()), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16));
	}
}
