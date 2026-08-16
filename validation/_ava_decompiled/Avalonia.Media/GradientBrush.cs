using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Collections;
using Avalonia.Media.Immutable;
using Avalonia.Metadata;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Media;

/// <summary>
/// Base class for brushes that draw with a gradient.
/// </summary>
public abstract class GradientBrush : Brush, IGradientBrush, IBrush, IMutableBrush
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.GradientBrush.SpreadMethod" /> property.
	/// </summary>
	public static readonly StyledProperty<GradientSpreadMethod> SpreadMethodProperty = AvaloniaProperty.Register<GradientBrush, GradientSpreadMethod>("SpreadMethod", GradientSpreadMethod.Pad);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.GradientBrush.GradientStops" /> property.
	/// </summary>
	public static readonly StyledProperty<GradientStops> GradientStopsProperty = AvaloniaProperty.Register<GradientBrush, GradientStops>("GradientStops");

	private IDisposable? _gradientStopsSubscription;

	/// <inheritdoc />
	public GradientSpreadMethod SpreadMethod
	{
		get
		{
			return GetValue(SpreadMethodProperty);
		}
		set
		{
			SetValue(SpreadMethodProperty, value);
		}
	}

	/// <inheritdoc cref="P:Avalonia.Media.IGradientBrush.GradientStops" />
	[Content]
	public GradientStops GradientStops
	{
		get
		{
			return GetValue(GradientStopsProperty);
		}
		set
		{
			SetValue(GradientStopsProperty, value);
		}
	}

	/// <inheritdoc />
	IReadOnlyList<IGradientStop> IGradientBrush.GradientStops => GradientStops;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.GradientBrush" /> class.
	/// </summary>
	internal GradientBrush()
	{
		GradientStops = new GradientStops();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == GradientStopsProperty)
		{
			var (gradientStops, gradientStops2) = change.GetOldAndNewValue<GradientStops>();
			if (gradientStops != null)
			{
				gradientStops.CollectionChanged -= GradientStopsChanged;
				_gradientStopsSubscription?.Dispose();
			}
			if (gradientStops2 != null)
			{
				gradientStops2.CollectionChanged += GradientStopsChanged;
				_gradientStopsSubscription = gradientStops2.TrackItemPropertyChanged(GradientStopChanged);
			}
		}
		base.OnPropertyChanged(change);
	}

	private void GradientStopsChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		RegisterForSerialization();
	}

	private void GradientStopChanged(Tuple<object?, PropertyChangedEventArgs> e)
	{
		RegisterForSerialization();
	}

	private protected override void SerializeChanges(Compositor c, BatchStreamWriter writer)
	{
		base.SerializeChanges(c, writer);
		writer.Write(SpreadMethod);
		writer.Write(GradientStops.Count);
		foreach (GradientStop gradientStop in GradientStops)
		{
			writer.WriteObject(new ImmutableGradientStop(gradientStop.Offset, gradientStop.Color));
		}
	}

	public abstract IImmutableBrush ToImmutable();
}
