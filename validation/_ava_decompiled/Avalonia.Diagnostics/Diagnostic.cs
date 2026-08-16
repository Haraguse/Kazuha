using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Diagnostics;

internal static class Diagnostic
{
	public static class Meters
	{
		public const string SecondsUnit = "s";

		public const string MillisecondsUnit = "ms";

		public const string CompositorRenderPassName = "avalonia.comp.render.time";

		public const string CompositorRenderPassDescription = "Duration of the compositor render pass on render thread";

		public const string CompositorUpdatePassName = "avalonia.comp.update.time";

		public const string CompositorUpdatePassDescription = "Duration of the compositor update pass on render thread";

		public const string LayoutMeasurePassName = "avalonia.ui.measure.time";

		public const string LayoutMeasurePassDescription = "Duration of layout measurement pass on UI thread";

		public const string LayoutArrangePassName = "avalonia.ui.arrange.time";

		public const string LayoutArrangePassDescription = "Duration of layout arrangement pass on UI thread";

		public const string LayoutRenderPassName = "avalonia.ui.render.time";

		public const string LayoutRenderPassDescription = "Duration of render recording pass on UI thread";

		public const string LayoutInputPassName = "avalonia.ui.input.time";

		public const string LayoutInputPassDescription = "Duration of input processing on UI thread";

		public const string TotalEventHandleCountName = "avalonia.ui.event.handler.count";

		public const string TotalEventHandleCountDescription = "Number of event handlers currently registered in the application";

		public const string TotalEventHandleCountUnit = "{handler}";

		public const string TotalVisualCountName = "avalonia.ui.visual.count";

		public const string TotalVisualCountDescription = "Number of visual elements currently present in the visual tree";

		public const string TotalVisualCountUnit = "{visual}";

		public const string TotalDispatcherTimerCountName = "avalonia.ui.dispatcher.timer.count";

		public const string TotalDispatcherTimerCountDescription = "Number of active dispatcher timers in the application";

		public const string TotalDispatcherTimerCountUnit = "{timer}";
	}

	public static class Tags
	{
		public const string Style = "Style";

		public const string SelectorResult = "SelectorResult";

		public const string Key = "Key";

		public const string ThemeVariant = "ThemeVariant";

		public const string Result = "Result";

		public const string Activator = "Activator";

		public const string IsActive = "IsActive";

		public const string Selector = "Selector";

		public const string Control = "Control";

		public const string RoutedEvent = "RoutedEvent";
	}

	internal readonly ref struct HistogramReportDisposable
	{
		private readonly Histogram<double> _histogram;

		private readonly long _timestamp;

		public HistogramReportDisposable(Histogram<double> histogram)
		{
			_timestamp = 0L;
			_histogram = histogram;
			if (histogram.Enabled)
			{
				_timestamp = Stopwatch.GetTimestamp();
			}
		}

		public void Dispose()
		{
			if (_timestamp > 0)
			{
				_histogram.Record(StopwatchHelper.GetElapsedTimeMs(_timestamp));
			}
		}
	}

	private static ActivitySource? s_activitySource;

	private static Histogram<double>? s_compositorRender;

	private static Histogram<double>? s_compositorUpdate;

	private static Histogram<double>? s_layoutMeasure;

	private static Histogram<double>? s_layoutArrange;

	private static Histogram<double>? s_layoutRender;

	private static Histogram<double>? s_layoutInput;

	public static bool IsEnabled { get; }

	public static void InitActivitySource()
	{
		s_activitySource = new ActivitySource("Avalonia.Diagnostic.Source");
	}

	private static Activity? StartActivity(string name)
	{
		return s_activitySource?.StartActivity(name);
	}

	public static Activity? AttachingStyle()
	{
		return StartActivity("Avalonia.AttachingStyle");
	}

	public static Activity? FindingResource()
	{
		return StartActivity("Avalonia.FindingResource");
	}

	public static Activity? EvaluatingStyle()
	{
		return StartActivity("Avalonia.EvaluatingStyle");
	}

	public static Activity? MeasuringLayoutable()
	{
		return StartActivity("Avalonia.MeasuringLayoutable");
	}

	public static Activity? ArrangingLayoutable()
	{
		return StartActivity("Avalonia.ArrangingLayoutable");
	}

	public static Activity? PerformingHitTest()
	{
		return StartActivity("Avalonia.PerformingHitTest");
	}

	public static Activity? RaisingRoutedEvent()
	{
		return StartActivity("Avalonia.RaisingRoutedEvent");
	}

	private static bool InitializeIsEnabled()
	{
		bool isEnabled;
		return AppContext.TryGetSwitch("Avalonia.Diagnostics.Diagnostic.IsEnabled", out isEnabled) & isEnabled;
	}

	static Diagnostic()
	{
		IsEnabled = InitializeIsEnabled();
		if (IsEnabled)
		{
			InitActivitySource();
			InitMetrics();
		}
	}

	public static void InitMetrics()
	{
		Meter meter = new Meter("Avalonia.Diagnostic.Meter");
		s_compositorRender = meter.CreateHistogram<double>("avalonia.comp.render.time", "ms", "Duration of the compositor render pass on render thread");
		s_compositorUpdate = meter.CreateHistogram<double>("avalonia.comp.update.time", "ms", "Duration of the compositor update pass on render thread");
		s_layoutMeasure = meter.CreateHistogram<double>("avalonia.ui.measure.time", "ms", "Duration of layout measurement pass on UI thread");
		s_layoutArrange = meter.CreateHistogram<double>("avalonia.ui.arrange.time", "ms", "Duration of layout arrangement pass on UI thread");
		s_layoutRender = meter.CreateHistogram<double>("avalonia.ui.render.time", "ms", "Duration of render recording pass on UI thread");
		s_layoutInput = meter.CreateHistogram<double>("avalonia.ui.input.time", "ms", "Duration of input processing on UI thread");
		meter.CreateObservableUpDownCounter("avalonia.ui.event.handler.count", () => Interactive.TotalHandlersCount, "{handler}", "Number of event handlers currently registered in the application");
		meter.CreateObservableUpDownCounter("avalonia.ui.visual.count", () => Visual.RootedVisualChildrenCount, "{visual}", "Number of visual elements currently present in the visual tree");
		meter.CreateObservableUpDownCounter("avalonia.ui.dispatcher.timer.count", () => DispatcherTimer.ActiveTimersCount, "{timer}", "Number of active dispatcher timers in the application");
	}

	public static HistogramReportDisposable BeginCompositorRenderPass()
	{
		return Begin(s_compositorRender);
	}

	public static HistogramReportDisposable BeginCompositorUpdatePass()
	{
		return Begin(s_compositorUpdate);
	}

	public static HistogramReportDisposable BeginLayoutMeasurePass()
	{
		return Begin(s_layoutMeasure);
	}

	public static HistogramReportDisposable BeginLayoutArrangePass()
	{
		return Begin(s_layoutArrange);
	}

	public static HistogramReportDisposable BeginLayoutInputPass()
	{
		return Begin(s_layoutInput);
	}

	public static HistogramReportDisposable BeginLayoutRenderPass()
	{
		return Begin(s_layoutRender);
	}

	private static HistogramReportDisposable Begin(Histogram<double>? histogram)
	{
		if (histogram == null)
		{
			return default(HistogramReportDisposable);
		}
		return new HistogramReportDisposable(histogram);
	}
}
