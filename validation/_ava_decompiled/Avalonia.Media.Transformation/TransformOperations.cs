using System;
using System.Collections.Generic;

namespace Avalonia.Media.Transformation;

/// <summary>
/// Contains a list of <see cref="T:Avalonia.Media.Transformation.TransformOperation" /> that represent primitive transforms that will be
/// applied in declared order.
/// </summary>
public sealed class TransformOperations : ITransform
{
	public readonly record struct Builder
	{
		private readonly List<TransformOperation> _operations;

		public Builder(int capacity)
		{
			_operations = new List<TransformOperation>(capacity);
		}

		public void AppendTranslate(double x, double y)
		{
			TransformOperation item = default(TransformOperation);
			item.Type = TransformOperation.OperationType.Translate;
			item.Data.Translate.X = x;
			item.Data.Translate.Y = y;
			item.Bake();
			_operations.Add(item);
		}

		public void AppendRotate(double angle)
		{
			TransformOperation item = default(TransformOperation);
			item.Type = TransformOperation.OperationType.Rotate;
			item.Data.Rotate.Angle = angle;
			item.Bake();
			_operations.Add(item);
		}

		public void AppendScale(double x, double y)
		{
			TransformOperation item = default(TransformOperation);
			item.Type = TransformOperation.OperationType.Scale;
			item.Data.Scale.X = x;
			item.Data.Scale.Y = y;
			item.Bake();
			_operations.Add(item);
		}

		public void AppendSkew(double x, double y)
		{
			TransformOperation item = default(TransformOperation);
			item.Type = TransformOperation.OperationType.Skew;
			item.Data.Skew.X = x;
			item.Data.Skew.Y = y;
			item.Bake();
			_operations.Add(item);
		}

		public void AppendMatrix(Matrix matrix)
		{
			TransformOperation item = new TransformOperation
			{
				Type = TransformOperation.OperationType.Matrix,
				Matrix = matrix
			};
			_operations.Add(item);
		}

		public void AppendIdentity()
		{
			TransformOperation item = new TransformOperation
			{
				Type = TransformOperation.OperationType.Identity
			};
			_operations.Add(item);
		}

		public void Append(TransformOperation toAdd)
		{
			_operations.Add(toAdd);
		}

		public TransformOperations Build()
		{
			return new TransformOperations(_operations);
		}
	}

	private readonly List<TransformOperation> _operations;

	public static TransformOperations Identity { get; } = new TransformOperations(new List<TransformOperation>());

	/// <summary>
	/// Returns whether all operations combined together produce the identity matrix.
	/// </summary>
	public bool IsIdentity { get; }

	public IReadOnlyList<TransformOperation> Operations => _operations;

	public Matrix Value { get; }

	private TransformOperations(List<TransformOperation> operations)
	{
		_operations = operations ?? throw new ArgumentNullException("operations");
		IsIdentity = CheckIsIdentity();
		Value = ApplyTransforms();
	}

	public static TransformOperations Parse(string s)
	{
		return TransformParser.Parse(s);
	}

	public static Builder CreateBuilder(int capacity)
	{
		return new Builder(capacity);
	}

	public static TransformOperations Interpolate(TransformOperations from, TransformOperations to, double progress)
	{
		TransformOperations result = Identity;
		if (!TryInterpolate(from, to, progress, ref result))
		{
			result = ((progress < 0.5) ? from : to);
		}
		return result;
	}

	private Matrix ApplyTransforms(int startOffset = 0)
	{
		Matrix identity = Matrix.Identity;
		for (int i = startOffset; i < _operations.Count; i++)
		{
			identity *= _operations[i].Matrix;
		}
		return identity;
	}

	private bool CheckIsIdentity()
	{
		foreach (TransformOperation operation in _operations)
		{
			if (!operation.IsIdentity)
			{
				return false;
			}
		}
		return true;
	}

	private static bool TryInterpolate(TransformOperations from, TransformOperations to, double progress, ref TransformOperations result)
	{
		bool isIdentity = from.IsIdentity;
		bool isIdentity2 = to.IsIdentity;
		if (isIdentity & isIdentity2)
		{
			return true;
		}
		int num = ComputeMatchingPrefixLength(from, to);
		int num2 = ((!isIdentity) ? from._operations.Count : 0);
		int num3 = ((!isIdentity2) ? to._operations.Count : 0);
		int num4 = Math.Max(num2, num3);
		Builder builder = new Builder(num);
		for (int i = 0; i < num; i++)
		{
			TransformOperation result2 = new TransformOperation
			{
				Type = TransformOperation.OperationType.Identity
			};
			if (!TransformOperation.TryInterpolate((i >= num2) ? ((TransformOperation?)null) : new TransformOperation?(from._operations[i]), (i >= num3) ? ((TransformOperation?)null) : new TransformOperation?(to._operations[i]), progress, ref result2))
			{
				return false;
			}
			builder.Append(result2);
		}
		if (num < num4)
		{
			if (!ComputeDecomposedTransform(from, num, out var decomposed) || !ComputeDecomposedTransform(to, num, out var to2))
			{
				return false;
			}
			Matrix.Decomposed decomposed2 = InterpolationUtilities.InterpolateDecomposedTransforms(ref decomposed, ref to2, progress);
			builder.AppendMatrix(InterpolationUtilities.ComposeTransform(decomposed2));
		}
		result = builder.Build();
		return true;
	}

	private static bool ComputeDecomposedTransform(TransformOperations operations, int startOffset, out Matrix.Decomposed decomposed)
	{
		if (!Matrix.TryDecomposeTransform(operations.ApplyTransforms(startOffset), out decomposed))
		{
			return false;
		}
		return true;
	}

	private static int ComputeMatchingPrefixLength(TransformOperations from, TransformOperations to)
	{
		int num = Math.Min(from._operations.Count, to._operations.Count);
		for (int i = 0; i < num; i++)
		{
			if (from._operations[i].Type != to._operations[i].Type)
			{
				return i;
			}
		}
		return Math.Max(from._operations.Count, to._operations.Count);
	}
}
