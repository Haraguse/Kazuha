using System;
using Avalonia.Animation;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Media;

/// <summary>
/// Represents a transform on an <see cref="T:Avalonia.Visual" />.
/// </summary>
public abstract class Transform : Animatable, IMutableTransform, ITransform, ICompositionRenderResource<ITransform>, ICompositionRenderResource, ICompositorSerializable
{
	private CompositorResourceHolder<ServerCompositionSimpleTransform> _resource;

	/// <summary>
	/// Gets the transform's <see cref="T:Avalonia.Matrix" />.
	/// </summary>
	public abstract Matrix Value { get; }

	/// <summary>
	/// Raised when the transform changes.
	/// </summary>
	public event EventHandler? Changed;

	internal Transform()
	{
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.Media.Transform" /> string.
	/// </summary>
	/// <param name="s">Six comma-delimited double values that describe the new <see cref="T:Avalonia.Media.Transform" />. For details check <see cref="M:Avalonia.Matrix.Parse(System.String)" /> </param>
	/// <returns>The <see cref="T:Avalonia.Media.Transform" />.</returns>
	public static Transform Parse(string s)
	{
		return new MatrixTransform(Matrix.Parse(s));
	}

	/// <summary>
	/// Raises the <see cref="E:Avalonia.Media.Transform.Changed" /> event.
	/// </summary>
	protected void RaiseChanged()
	{
		Changed?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Converts a transform to an immutable transform.
	/// </summary>
	/// <returns>The immutable transform</returns>
	public ImmutableTransform ToImmutable()
	{
		return new ImmutableTransform(Value);
	}

	/// <summary>
	/// Returns a String representing this transform matrix instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override string ToString()
	{
		return Value.ToString();
	}

	ITransform ICompositionRenderResource<ITransform>.GetForCompositor(Compositor c)
	{
		return _resource.GetForCompositor(c);
	}

	SimpleServerObject? ICompositorSerializable.TryGetServer(Compositor c)
	{
		return _resource.TryGetForCompositor(c);
	}

	void ICompositionRenderResource.AddRefOnCompositor(Compositor c)
	{
		_resource.CreateOrAddRef(c, this, out ServerCompositionSimpleTransform _, (Compositor cc) => new ServerCompositionSimpleTransform(cc.Server));
	}

	void ICompositionRenderResource.ReleaseOnCompositor(Compositor c)
	{
		_resource.Release(c);
	}

	void ICompositorSerializable.SerializeChanges(Compositor c, BatchStreamWriter writer)
	{
		ServerCompositionSimpleTransform.SerializeAllChanges(writer, Value);
	}
}
