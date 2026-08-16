using System;
using System.Runtime.CompilerServices;
using Avalonia.Collections;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Visuals.Platform;

namespace Avalonia.Media;

public class PathGeometry : StreamGeometry
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.PathGeometry.Figures" /> property.
	/// </summary>
	public static readonly DirectProperty<PathGeometry, PathFigures?> FiguresProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.PathGeometry.FillRule" /> property.
	/// </summary>
	public static readonly StyledProperty<FillRule> FillRuleProperty;

	private PathFigures? _figures;

	private IDisposable? _figuresObserver;

	private IDisposable? _figuresPropertiesObserver;

	/// <summary>
	/// Gets or sets the figures.
	/// </summary>
	/// <value>
	/// The figures.
	/// </value>
	[Content]
	public PathFigures? Figures
	{
		get
		{
			return _figures;
		}
		set
		{
			SetAndRaise(FiguresProperty, ref _figures, value);
		}
	}

	/// <summary>
	/// Gets or sets the fill rule.
	/// </summary>
	/// <value>
	/// The fill rule.
	/// </value>
	public FillRule FillRule
	{
		get
		{
			return GetValue(FillRuleProperty);
		}
		set
		{
			SetValue(FillRuleProperty, value);
		}
	}

	static PathGeometry()
	{
		FiguresProperty = AvaloniaProperty.RegisterDirect("Figures", (PathGeometry g) => g.Figures, delegate(PathGeometry g, PathFigures? f)
		{
			g.Figures = f;
		});
		FillRuleProperty = AvaloniaProperty.Register<PathGeometry, FillRule>("FillRule", FillRule.EvenOdd);
		FiguresProperty.Changed.AddClassHandler(delegate(PathGeometry s, AvaloniaPropertyChangedEventArgs e)
		{
			s.OnFiguresChanged(e.NewValue as PathFigures);
		});
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.PathGeometry" /> class.
	/// </summary>
	public PathGeometry()
	{
		Figures = new PathFigures();
	}

	/// <summary>
	/// Parses the specified path data to a <see cref="T:Avalonia.Media.PathGeometry" />.
	/// </summary>
	/// <param name="pathData">The s.</param>
	/// <returns></returns>
	public new static PathGeometry Parse(string pathData)
	{
		PathGeometry pathGeometry = new PathGeometry();
		using PathGeometryContext geometryContext = new PathGeometryContext(pathGeometry);
		using PathMarkupParser pathMarkupParser = new PathMarkupParser(geometryContext);
		pathMarkupParser.Parse(pathData);
		return pathGeometry;
	}

	private protected sealed override IGeometryImpl? CreateDefiningGeometry()
	{
		PathFigures figures = Figures;
		if (figures == null)
		{
			return null;
		}
		IStreamGeometryImpl streamGeometryImpl = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>().CreateStreamGeometry();
		using StreamGeometryContext streamGeometryContext = new StreamGeometryContext(streamGeometryImpl.Open());
		streamGeometryContext.SetFillRule(FillRule);
		foreach (PathFigure item in figures)
		{
			item.ApplyTo(streamGeometryContext);
		}
		return streamGeometryImpl;
	}

	private void OnFiguresChanged(PathFigures? figures)
	{
		_figuresObserver?.Dispose();
		_figuresPropertiesObserver?.Dispose();
		_figuresObserver = figures?.ForEachItem(delegate(PathFigure s)
		{
			s.SegmentsInvalidated += InvalidateGeometryFromSegments;
			InvalidateGeometry();
		}, delegate(PathFigure s)
		{
			s.SegmentsInvalidated -= InvalidateGeometryFromSegments;
			InvalidateGeometry();
		}, base.InvalidateGeometry);
		_figuresPropertiesObserver = figures?.TrackItemPropertyChanged(delegate
		{
			InvalidateGeometry();
		});
	}

	private void InvalidateGeometryFromSegments(object? _, EventArgs __)
	{
		InvalidateGeometry();
	}

	public override string ToString()
	{
		string text = ((_figures != null) ? string.Join(" ", _figures) : string.Empty);
		return FormattableString.Invariant(FormattableStringFactory.Create("{0}{1}", (FillRule != FillRule.EvenOdd) ? "F1 " : "", text));
	}
}
