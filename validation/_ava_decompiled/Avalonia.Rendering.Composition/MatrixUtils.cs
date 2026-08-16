using System.Numerics;

namespace Avalonia.Rendering.Composition;

internal static class MatrixUtils
{
	public static Matrix? ComputeTransform(Vector size, Vector anchorPoint, Vector3D centerPoint, Matrix transformMatrix, Vector3D scale, float rotationAngle, Quaternion orientation, Vector3D offset)
	{
		Vector vector = Vector.Multiply(size, anchorPoint);
		Matrix matrix = Matrix.CreateTranslation(0.0 - vector.X, 0.0 - vector.Y);
		Vector3D vector3D = new Vector3D(centerPoint.X, centerPoint.Y, centerPoint.Z);
		if (!transformMatrix.IsIdentity)
		{
			matrix = transformMatrix * matrix;
		}
		if (scale != new Vector3D(1.0, 1.0, 1.0))
		{
			matrix *= ToMatrix(Matrix4x4.CreateScale(scale.ToVector3(), vector3D.ToVector3()));
		}
		if (rotationAngle != 0f)
		{
			matrix *= ToMatrix(Matrix4x4.CreateRotationZ(rotationAngle, vector3D.ToVector3()));
		}
		if (orientation != Quaternion.Identity)
		{
			if (centerPoint != default(Vector3D))
			{
				matrix *= ToMatrix(Matrix4x4.CreateTranslation(-vector3D.ToVector3()) * Matrix4x4.CreateFromQuaternion(orientation) * Matrix4x4.CreateTranslation(vector3D.ToVector3()));
			}
			else
			{
				matrix *= ToMatrix(Matrix4x4.CreateFromQuaternion(orientation));
			}
		}
		if (offset != default(Vector3D))
		{
			if (offset.Z == 0.0)
			{
				matrix *= Matrix.CreateTranslation(offset.X, offset.Y);
			}
			else
			{
				matrix *= ToMatrix(Matrix4x4.CreateTranslation(offset.ToVector3()));
			}
		}
		if (matrix.IsIdentity)
		{
			return null;
		}
		return matrix;
	}

	public static Matrix4x4 ToMatrix4x4(Matrix matrix)
	{
		return new Matrix4x4((float)matrix.M11, (float)matrix.M12, 0f, (float)matrix.M13, (float)matrix.M21, (float)matrix.M22, 0f, (float)matrix.M23, 0f, 0f, 1f, 0f, (float)matrix.M31, (float)matrix.M32, 0f, (float)matrix.M33);
	}

	public static Matrix ToMatrix(Matrix4x4 matrix44)
	{
		return new Matrix(matrix44.M11, matrix44.M12, matrix44.M14, matrix44.M21, matrix44.M22, matrix44.M24, matrix44.M41, matrix44.M42, matrix44.M44);
	}
}
