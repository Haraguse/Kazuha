using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionSimpleGradientBrush : ServerCompositionSimpleBrush, IGradientBrush, IBrush
{
	private readonly List<IGradientStop> _gradientStops = new List<IGradientStop>();

	public IReadOnlyList<IGradientStop> GradientStops => _gradientStops;

	public GradientSpreadMethod SpreadMethod { get; private set; }

	internal ServerCompositionSimpleGradientBrush(ServerCompositor compositor)
		: base(compositor)
	{
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		base.DeserializeChangesCore(reader, committedAt);
		SpreadMethod = reader.Read<GradientSpreadMethod>();
		_gradientStops.Clear();
		int num = reader.Read<int>();
		for (int i = 0; i < num; i++)
		{
			_gradientStops.Add(reader.ReadObject<ImmutableGradientStop>());
		}
	}
}
