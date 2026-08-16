using System.Numerics;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Animations;

/// <summary>
/// This is the base class for ExpressionAnimation and KeyFrameAnimation.
/// </summary>
/// <remarks>
/// Use the <see cref="M:Avalonia.Rendering.Composition.CompositionObject.StartAnimation(System.String,Avalonia.Rendering.Composition.Animations.CompositionAnimation)" /> method to start the animation.
/// Value parameters (as opposed to reference parameters which are set using <see cref="M:Avalonia.Rendering.Composition.Animations.CompositionAnimation.SetReferenceParameter(System.String,Avalonia.Rendering.Composition.CompositionObject)" />)
/// are copied and "embedded" into an expression at the time CompositionObject.StartAnimation is called.
/// Changing the value of the variable after <see cref="M:Avalonia.Rendering.Composition.CompositionObject.StartAnimation(System.String,Avalonia.Rendering.Composition.Animations.CompositionAnimation)" /> is called will not affect
/// the value of the ExpressionAnimation.
/// See the remarks section of ExpressionAnimation for additional information.
/// </remarks>
public abstract class CompositionAnimation : CompositionObject, ICompositionAnimationBase
{
	private readonly CompositionPropertySet _propertySet;

	public string? Target { get; set; }

	internal CompositionAnimation(Compositor compositor)
		: base(compositor, null)
	{
		_propertySet = new CompositionPropertySet(compositor);
	}

	/// <summary>
	/// Clears all of the parameters of the animation.
	/// </summary>
	public void ClearAllParameters()
	{
		_propertySet.ClearAll();
	}

	/// <summary>
	/// Clears a parameter from the animation.
	/// </summary>
	public void ClearParameter(string key)
	{
		_propertySet.Clear(key);
	}

	private void SetVariant(string key, ExpressionVariant value)
	{
		_propertySet.Set(key, value);
	}

	public void SetColorParameter(string key, Color value)
	{
		SetVariant(key, value);
	}

	public void SetMatrix3x2Parameter(string key, Matrix3x2 value)
	{
		SetVariant(key, value);
	}

	public void SetMatrix4x4Parameter(string key, Matrix4x4 value)
	{
		SetVariant(key, value);
	}

	public void SetQuaternionParameter(string key, Quaternion value)
	{
		SetVariant(key, value);
	}

	public void SetReferenceParameter(string key, CompositionObject compositionObject)
	{
		_propertySet.Set(key, compositionObject);
	}

	public void SetScalarParameter(string key, float value)
	{
		SetVariant(key, value);
	}

	public void SetVector2Parameter(string key, Vector2 value)
	{
		SetVariant(key, value);
	}

	public void SetVector3Parameter(string key, Vector3 value)
	{
		SetVariant(key, value);
	}

	public void SetVector4Parameter(string key, Vector4 value)
	{
		SetVariant(key, value);
	}

	internal abstract IAnimationInstance CreateInstance(ServerObject targetObject, ExpressionVariant? finalValue);

	internal PropertySetSnapshot CreateSnapshot()
	{
		return _propertySet.Snapshot();
	}

	void ICompositionAnimationBase.InternalOnly()
	{
	}
}
