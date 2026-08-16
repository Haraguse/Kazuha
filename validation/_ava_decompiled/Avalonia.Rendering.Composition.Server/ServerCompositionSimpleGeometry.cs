using System;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionSimpleGeometry : SimpleServerRenderResource, IRenderDataGeometry
{
	private IGeometryImpl? _geometryImpl;

	internal static readonly CompositionProperty<IGeometryImpl?> s_IdOfGeometryImplProperty = CompositionProperty.Register<ServerCompositionSimpleGeometry, IGeometryImpl>("GeometryImpl", (SimpleServerObject obj) => ((ServerCompositionSimpleGeometry)obj)._geometryImpl, delegate(SimpleServerObject obj, IGeometryImpl? v)
	{
		((ServerCompositionSimpleGeometry)obj)._geometryImpl = v;
	}, null);

	public IGeometryImpl? GeometryImpl
	{
		get
		{
			return _geometryImpl;
		}
		set
		{
			bool flag = false;
			if (_geometryImpl != value)
			{
				flag = true;
			}
			SetValue(s_IdOfGeometryImplProperty, ref _geometryImpl, value);
		}
	}

	internal ServerCompositionSimpleGeometry(ServerCompositor compositor)
		: base(compositor)
	{
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		base.DeserializeChangesCore(reader, committedAt);
		if ((reader.Read<CompositionSimpleGeometryChangedFields>() & CompositionSimpleGeometryChangedFields.GeometryImpl) == CompositionSimpleGeometryChangedFields.GeometryImpl)
		{
			GeometryImpl = reader.ReadObject<IGeometryImpl>();
		}
	}

	internal static void SerializeAllChanges(BatchStreamWriter writer, IGeometryImpl? geometryImpl)
	{
		writer.Write(CompositionSimpleGeometryChangedFields.GeometryImpl);
		writer.WriteObject(geometryImpl);
	}
}
