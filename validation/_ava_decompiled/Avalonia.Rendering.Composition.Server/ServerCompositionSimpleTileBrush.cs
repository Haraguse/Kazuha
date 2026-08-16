using System;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionSimpleTileBrush : ServerCompositionSimpleBrush
{
	private AlignmentX _alignmentX;

	internal static readonly CompositionProperty<AlignmentX> s_IdOfAlignmentXProperty = CompositionProperty.Register<ServerCompositionSimpleTileBrush, AlignmentX>("AlignmentX", (SimpleServerObject obj) => ((ServerCompositionSimpleTileBrush)obj)._alignmentX, delegate(SimpleServerObject obj, AlignmentX v)
	{
		((ServerCompositionSimpleTileBrush)obj)._alignmentX = v;
	}, null);

	private AlignmentY _alignmentY;

	internal static readonly CompositionProperty<AlignmentY> s_IdOfAlignmentYProperty = CompositionProperty.Register<ServerCompositionSimpleTileBrush, AlignmentY>("AlignmentY", (SimpleServerObject obj) => ((ServerCompositionSimpleTileBrush)obj)._alignmentY, delegate(SimpleServerObject obj, AlignmentY v)
	{
		((ServerCompositionSimpleTileBrush)obj)._alignmentY = v;
	}, null);

	private RelativeRect _destinationRect;

	internal static readonly CompositionProperty<RelativeRect> s_IdOfDestinationRectProperty = CompositionProperty.Register<ServerCompositionSimpleTileBrush, RelativeRect>("DestinationRect", (SimpleServerObject obj) => ((ServerCompositionSimpleTileBrush)obj)._destinationRect, delegate(SimpleServerObject obj, RelativeRect v)
	{
		((ServerCompositionSimpleTileBrush)obj)._destinationRect = v;
	}, null);

	private RelativeRect _sourceRect;

	internal static readonly CompositionProperty<RelativeRect> s_IdOfSourceRectProperty = CompositionProperty.Register<ServerCompositionSimpleTileBrush, RelativeRect>("SourceRect", (SimpleServerObject obj) => ((ServerCompositionSimpleTileBrush)obj)._sourceRect, delegate(SimpleServerObject obj, RelativeRect v)
	{
		((ServerCompositionSimpleTileBrush)obj)._sourceRect = v;
	}, null);

	private Stretch _stretch;

	internal static readonly CompositionProperty<Stretch> s_IdOfStretchProperty = CompositionProperty.Register<ServerCompositionSimpleTileBrush, Stretch>("Stretch", (SimpleServerObject obj) => ((ServerCompositionSimpleTileBrush)obj)._stretch, delegate(SimpleServerObject obj, Stretch v)
	{
		((ServerCompositionSimpleTileBrush)obj)._stretch = v;
	}, null);

	private TileMode _tileMode;

	internal static readonly CompositionProperty<TileMode> s_IdOfTileModeProperty = CompositionProperty.Register<ServerCompositionSimpleTileBrush, TileMode>("TileMode", (SimpleServerObject obj) => ((ServerCompositionSimpleTileBrush)obj)._tileMode, delegate(SimpleServerObject obj, TileMode v)
	{
		((ServerCompositionSimpleTileBrush)obj)._tileMode = v;
	}, null);

	public AlignmentX AlignmentX
	{
		get
		{
			return _alignmentX;
		}
		set
		{
			bool flag = false;
			if (_alignmentX != value)
			{
				flag = true;
			}
			SetValue(s_IdOfAlignmentXProperty, ref _alignmentX, value);
		}
	}

	public AlignmentY AlignmentY
	{
		get
		{
			return _alignmentY;
		}
		set
		{
			bool flag = false;
			if (_alignmentY != value)
			{
				flag = true;
			}
			SetValue(s_IdOfAlignmentYProperty, ref _alignmentY, value);
		}
	}

	public RelativeRect DestinationRect
	{
		get
		{
			return _destinationRect;
		}
		set
		{
			bool flag = false;
			if (_destinationRect != value)
			{
				flag = true;
			}
			SetValue(s_IdOfDestinationRectProperty, ref _destinationRect, value);
		}
	}

	public RelativeRect SourceRect
	{
		get
		{
			return _sourceRect;
		}
		set
		{
			bool flag = false;
			if (_sourceRect != value)
			{
				flag = true;
			}
			SetValue(s_IdOfSourceRectProperty, ref _sourceRect, value);
		}
	}

	public Stretch Stretch
	{
		get
		{
			return _stretch;
		}
		set
		{
			bool flag = false;
			if (_stretch != value)
			{
				flag = true;
			}
			SetValue(s_IdOfStretchProperty, ref _stretch, value);
		}
	}

	public TileMode TileMode
	{
		get
		{
			return _tileMode;
		}
		set
		{
			bool flag = false;
			if (_tileMode != value)
			{
				flag = true;
			}
			SetValue(s_IdOfTileModeProperty, ref _tileMode, value);
		}
	}

	internal ServerCompositionSimpleTileBrush(ServerCompositor compositor)
		: base(compositor)
	{
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		base.DeserializeChangesCore(reader, committedAt);
		CompositionSimpleTileBrushChangedFields num = reader.Read<CompositionSimpleTileBrushChangedFields>();
		if ((num & CompositionSimpleTileBrushChangedFields.AlignmentX) == CompositionSimpleTileBrushChangedFields.AlignmentX)
		{
			AlignmentX = reader.Read<AlignmentX>();
		}
		if ((num & CompositionSimpleTileBrushChangedFields.AlignmentY) == CompositionSimpleTileBrushChangedFields.AlignmentY)
		{
			AlignmentY = reader.Read<AlignmentY>();
		}
		if ((num & CompositionSimpleTileBrushChangedFields.DestinationRect) == CompositionSimpleTileBrushChangedFields.DestinationRect)
		{
			DestinationRect = reader.Read<RelativeRect>();
		}
		if ((num & CompositionSimpleTileBrushChangedFields.SourceRect) == CompositionSimpleTileBrushChangedFields.SourceRect)
		{
			SourceRect = reader.Read<RelativeRect>();
		}
		if ((num & CompositionSimpleTileBrushChangedFields.Stretch) == CompositionSimpleTileBrushChangedFields.Stretch)
		{
			Stretch = reader.Read<Stretch>();
		}
		if ((num & CompositionSimpleTileBrushChangedFields.TileMode) == CompositionSimpleTileBrushChangedFields.TileMode)
		{
			TileMode = reader.Read<TileMode>();
		}
	}

	internal static void SerializeAllChanges(BatchStreamWriter writer, AlignmentX alignmentX, AlignmentY alignmentY, RelativeRect destinationRect, RelativeRect sourceRect, Stretch stretch, TileMode tileMode)
	{
		writer.Write(CompositionSimpleTileBrushChangedFields.AlignmentX | CompositionSimpleTileBrushChangedFields.AlignmentY | CompositionSimpleTileBrushChangedFields.DestinationRect | CompositionSimpleTileBrushChangedFields.SourceRect | CompositionSimpleTileBrushChangedFields.Stretch | CompositionSimpleTileBrushChangedFields.TileMode);
		writer.Write(alignmentX);
		writer.Write(alignmentY);
		writer.Write(destinationRect);
		writer.Write(sourceRect);
		writer.Write(stretch);
		writer.Write(tileMode);
	}
}
