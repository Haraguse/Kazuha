using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Controls;

public class Grid : Panel
{
	private class ExtendedData
	{
		internal ColumnDefinitions? ColumnDefinitions;

		internal RowDefinitions? RowDefinitions;

		internal IReadOnlyList<DefinitionBase>? DefinitionsU;

		internal IReadOnlyList<DefinitionBase>? DefinitionsV;

		internal CellCache[]? CellCachesCollection;

		internal int CellGroup1;

		internal int CellGroup2;

		internal int CellGroup3;

		internal int CellGroup4;

		internal DefinitionBase?[]? TempDefinitions;
	}

	[Flags]
	private enum Flags
	{
		ValidDefinitionsUStructure = 1,
		ValidDefinitionsVStructure = 2,
		ValidCellsStructure = 4,
		ShowGridLinesPropertyValue = 0x100,
		ListenToNotifications = 0x1000,
		SizeToContentU = 0x2000,
		SizeToContentV = 0x4000,
		HasStarCellsU = 0x8000,
		HasStarCellsV = 0x10000,
		HasGroup3CellsInAutoRows = 0x20000,
		MeasureOverrideInProgress = 0x40000,
		ArrangeOverrideInProgress = 0x80000
	}

	[Flags]
	internal enum LayoutTimeSizeType : byte
	{
		None = 0,
		Pixel = 1,
		Auto = 2,
		Star = 4
	}

	private struct CellCache
	{
		internal int ColumnIndex;

		internal int RowIndex;

		internal int ColumnSpan;

		internal int RowSpan;

		internal LayoutTimeSizeType SizeTypeU;

		internal LayoutTimeSizeType SizeTypeV;

		internal int Next;

		internal bool IsStarU => SizeTypeU.HasAllFlags(LayoutTimeSizeType.Star);

		internal bool IsAutoU => SizeTypeU.HasAllFlags(LayoutTimeSizeType.Auto);

		internal bool IsStarV => SizeTypeV.HasAllFlags(LayoutTimeSizeType.Star);

		internal bool IsAutoV => SizeTypeV.HasAllFlags(LayoutTimeSizeType.Auto);
	}

	private class SpanKey
	{
		private int _start;

		private int _count;

		private bool _u;

		internal int Start => _start;

		internal int Count => _count;

		internal bool U => _u;

		internal SpanKey(int start, int count, bool u)
		{
			_start = start;
			_count = count;
			_u = u;
		}

		public override int GetHashCode()
		{
			int num = _start ^ (_count << 2);
			if (_u)
			{
				return num & 0x7FFFFFF;
			}
			return num | 0x8000000;
		}

		public override bool Equals(object? obj)
		{
			if (obj is SpanKey spanKey && spanKey._start == _start && spanKey._count == _count)
			{
				return spanKey._u == _u;
			}
			return false;
		}
	}

	private class SpanPreferredDistributionOrderComparer : IComparer
	{
		public int Compare(object? x, object? y)
		{
			DefinitionBase definitionBase = x as DefinitionBase;
			DefinitionBase definitionBase2 = y as DefinitionBase;
			if (!CompareNullRefs(definitionBase, definitionBase2, out var result))
			{
				if (definitionBase.UserSize.IsAuto)
				{
					if (definitionBase2.UserSize.IsAuto)
					{
						return definitionBase.MinSize.CompareTo(definitionBase2.MinSize);
					}
					return -1;
				}
				if (definitionBase2.UserSize.IsAuto)
				{
					return 1;
				}
				return definitionBase.PreferredSize.CompareTo(definitionBase2.PreferredSize);
			}
			return result;
		}
	}

	private class SpanMaxDistributionOrderComparer : IComparer
	{
		public int Compare(object? x, object? y)
		{
			DefinitionBase definitionBase = x as DefinitionBase;
			DefinitionBase definitionBase2 = y as DefinitionBase;
			if (!CompareNullRefs(definitionBase, definitionBase2, out var result))
			{
				if (definitionBase.UserSize.IsAuto)
				{
					if (definitionBase2.UserSize.IsAuto)
					{
						return definitionBase.SizeCache.CompareTo(definitionBase2.SizeCache);
					}
					return 1;
				}
				if (definitionBase2.UserSize.IsAuto)
				{
					return -1;
				}
				return definitionBase.SizeCache.CompareTo(definitionBase2.SizeCache);
			}
			return result;
		}
	}

	private class RoundingErrorIndexComparer : IComparer
	{
		private readonly double[] errors;

		internal RoundingErrorIndexComparer(double[] errors)
		{
			this.errors = errors ?? throw new ArgumentNullException("errors");
		}

		public int Compare(object? x, object? y)
		{
			int? num = x as int?;
			int? num2 = y as int?;
			if (!CompareNullRefs(num, num2, out var result))
			{
				double num3 = errors[num.Value];
				double value = errors[num2.Value];
				return num3.CompareTo(value);
			}
			return result;
		}
	}

	private class MinRatioComparer : IComparer
	{
		public int Compare(object? x, object? y)
		{
			DefinitionBase definitionBase = x as DefinitionBase;
			DefinitionBase definitionBase2 = y as DefinitionBase;
			if (!CompareNullRefs(definitionBase2, definitionBase, out var result))
			{
				return definitionBase2.MeasureSize.CompareTo(definitionBase.MeasureSize);
			}
			return result;
		}
	}

	private class MaxRatioComparer : IComparer
	{
		public int Compare(object? x, object? y)
		{
			DefinitionBase definitionBase = x as DefinitionBase;
			DefinitionBase definitionBase2 = y as DefinitionBase;
			if (!CompareNullRefs(definitionBase, definitionBase2, out var result))
			{
				return definitionBase.SizeCache.CompareTo(definitionBase2.SizeCache);
			}
			return result;
		}
	}

	private class StarWeightComparer : IComparer
	{
		public int Compare(object? x, object? y)
		{
			DefinitionBase definitionBase = x as DefinitionBase;
			DefinitionBase definitionBase2 = y as DefinitionBase;
			if (!CompareNullRefs(definitionBase, definitionBase2, out var result))
			{
				return definitionBase.MeasureSize.CompareTo(definitionBase2.MeasureSize);
			}
			return result;
		}
	}

	private class MinRatioIndexComparer : IComparer
	{
		private readonly IReadOnlyList<DefinitionBase> definitions;

		internal MinRatioIndexComparer(IReadOnlyList<DefinitionBase> definitions)
		{
			this.definitions = definitions ?? throw new ArgumentNullException("definitions");
		}

		public int Compare(object? x, object? y)
		{
			int? num = x as int?;
			int? num2 = y as int?;
			DefinitionBase definitionBase = null;
			DefinitionBase definitionBase2 = null;
			if (num.HasValue)
			{
				definitionBase = definitions[num.Value];
			}
			if (num2.HasValue)
			{
				definitionBase2 = definitions[num2.Value];
			}
			if (!CompareNullRefs(definitionBase2, definitionBase, out var result))
			{
				return definitionBase2.MeasureSize.CompareTo(definitionBase.MeasureSize);
			}
			return result;
		}
	}

	private class MaxRatioIndexComparer : IComparer
	{
		private readonly IReadOnlyList<DefinitionBase> definitions;

		internal MaxRatioIndexComparer(IReadOnlyList<DefinitionBase> definitions)
		{
			this.definitions = definitions ?? throw new ArgumentNullException("definitions");
		}

		public int Compare(object? x, object? y)
		{
			int? num = x as int?;
			int? num2 = y as int?;
			DefinitionBase definitionBase = null;
			DefinitionBase definitionBase2 = null;
			if (num.HasValue)
			{
				definitionBase = definitions[num.Value];
			}
			if (num2.HasValue)
			{
				definitionBase2 = definitions[num2.Value];
			}
			if (!CompareNullRefs(definitionBase, definitionBase2, out var result))
			{
				return definitionBase.SizeCache.CompareTo(definitionBase2.SizeCache);
			}
			return result;
		}
	}

	private class StarWeightIndexComparer : IComparer
	{
		private readonly IReadOnlyList<DefinitionBase> definitions;

		internal StarWeightIndexComparer(IReadOnlyList<DefinitionBase> definitions)
		{
			this.definitions = definitions ?? throw new ArgumentNullException("definitions");
		}

		public int Compare(object? x, object? y)
		{
			int? num = x as int?;
			int? num2 = y as int?;
			DefinitionBase definitionBase = null;
			DefinitionBase definitionBase2 = null;
			if (num.HasValue)
			{
				definitionBase = definitions[num.Value];
			}
			if (num2.HasValue)
			{
				definitionBase2 = definitions[num2.Value];
			}
			if (!CompareNullRefs(definitionBase, definitionBase2, out var result))
			{
				return definitionBase.MeasureSize.CompareTo(definitionBase2.MeasureSize);
			}
			return result;
		}
	}

	internal class GridLinesRenderer : Control
	{
		private static Size _lastArrangeSize;

		private const double _dashLength = 4.0;

		private const double _penWidth = 1.0;

		private static readonly Pen _oddDashPen;

		private static readonly Pen _evenDashPen;

		static GridLinesRenderer()
		{
			List<double> dashes = new List<double> { 4.0, 4.0 };
			_oddDashPen = new Pen(dashStyle: new DashStyle(dashes, 0.0), brush: Brushes.Blue);
			DashStyle dashStyle = new DashStyle(dashes, 4.0);
			_evenDashPen = new Pen(Brushes.Yellow, 1.0, dashStyle);
		}

		public sealed override void Render(DrawingContext drawingContext)
		{
			Grid visualParent = this.GetVisualParent<Grid>();
			if (visualParent == null || !visualParent.ShowGridLines)
			{
				return;
			}
			for (int i = 1; i < visualParent.ColumnDefinitions.Count; i++)
			{
				DrawGridLine(drawingContext, visualParent.ColumnDefinitions[i].FinalOffset, 0.0, visualParent.ColumnDefinitions[i].FinalOffset, _lastArrangeSize.Height);
				if (visualParent.ColumnSpacing != 0.0)
				{
					DrawGridLine(drawingContext, visualParent.ColumnDefinitions[i].FinalOffset - visualParent.ColumnSpacing, 0.0, visualParent.ColumnDefinitions[i].FinalOffset - visualParent.ColumnSpacing, _lastArrangeSize.Height);
				}
			}
			for (int j = 1; j < visualParent.RowDefinitions.Count; j++)
			{
				DrawGridLine(drawingContext, 0.0, visualParent.RowDefinitions[j].FinalOffset, _lastArrangeSize.Width, visualParent.RowDefinitions[j].FinalOffset);
				if (visualParent.RowSpacing != 0.0)
				{
					DrawGridLine(drawingContext, 0.0, visualParent.RowDefinitions[j].FinalOffset - visualParent.RowSpacing, _lastArrangeSize.Width, visualParent.RowDefinitions[j].FinalOffset - visualParent.RowSpacing);
				}
			}
		}

		private static void DrawGridLine(DrawingContext drawingContext, double startX, double startY, double endX, double endY)
		{
			Point p = new Point(startX, startY);
			Point p2 = new Point(endX, endY);
			drawingContext.DrawLine(_oddDashPen, p, p2);
			drawingContext.DrawLine(_evenDashPen, p, p2);
		}

		internal void UpdateRenderBounds(Size arrangeSize)
		{
			_lastArrangeSize = arrangeSize;
			InvalidateVisual();
		}
	}

	private ExtendedData? _extData;

	private Flags _flags;

	private GridLinesRenderer? _gridLinesRenderer;

	private int[]? _definitionIndices;

	private double[]? _roundingErrors;

	private const int c_layoutLoopMaxCount = 5;

	private static readonly LocalDataStoreSlot s_tempDefinitionsDataSlot;

	private static readonly IComparer s_spanPreferredDistributionOrderComparer;

	private static readonly IComparer s_spanMaxDistributionOrderComparer;

	private static readonly IComparer s_minRatioComparer;

	private static readonly IComparer s_maxRatioComparer;

	private static readonly IComparer s_starWeightComparer;

	public static readonly StyledProperty<bool> ShowGridLinesProperty;

	public static readonly StyledProperty<double> RowSpacingProperty;

	public static readonly StyledProperty<double> ColumnSpacingProperty;

	public static readonly AttachedProperty<int> ColumnProperty;

	public static readonly AttachedProperty<int> RowProperty;

	public static readonly AttachedProperty<int> ColumnSpanProperty;

	public static readonly AttachedProperty<int> RowSpanProperty;

	public static readonly AttachedProperty<bool> IsSharedSizeScopeProperty;

	public bool ShowGridLines
	{
		get
		{
			return GetValue(ShowGridLinesProperty);
		}
		set
		{
			SetValue(ShowGridLinesProperty, value);
		}
	}

	public double RowSpacing
	{
		get
		{
			return GetValue(RowSpacingProperty);
		}
		set
		{
			SetValue(RowSpacingProperty, value);
		}
	}

	public double ColumnSpacing
	{
		get
		{
			return GetValue(ColumnSpacingProperty);
		}
		set
		{
			SetValue(ColumnSpacingProperty, value);
		}
	}

	[MemberNotNull("_extData")]
	public ColumnDefinitions ColumnDefinitions
	{
		[MemberNotNull("_extData")]
		get
		{
			if (_extData == null)
			{
				_extData = new ExtendedData();
			}
			if (_extData.ColumnDefinitions == null)
			{
				_extData.ColumnDefinitions = new ColumnDefinitions
				{
					Parent = this
				};
			}
			return _extData.ColumnDefinitions;
		}
		[MemberNotNull("_extData")]
		set
		{
			if (_extData == null)
			{
				_extData = new ExtendedData();
			}
			ColumnDefinitions columnDefinitions = _extData.ColumnDefinitions;
			if (columnDefinitions != null && columnDefinitions != value)
			{
				columnDefinitions.Parent = null;
			}
			_extData.ColumnDefinitions = value;
			_extData.ColumnDefinitions.Parent = this;
			InvalidateMeasure();
		}
	}

	[MemberNotNull("_extData")]
	public RowDefinitions RowDefinitions
	{
		[MemberNotNull("_extData")]
		get
		{
			if (_extData == null)
			{
				_extData = new ExtendedData();
			}
			if (_extData.RowDefinitions == null)
			{
				_extData.RowDefinitions = new RowDefinitions
				{
					Parent = this
				};
			}
			return _extData.RowDefinitions;
		}
		[MemberNotNull("_extData")]
		set
		{
			if (_extData == null)
			{
				_extData = new ExtendedData();
			}
			RowDefinitions rowDefinitions = _extData.RowDefinitions;
			if (rowDefinitions != null && rowDefinitions != value)
			{
				rowDefinitions.Parent = null;
			}
			_extData.RowDefinitions = value;
			_extData.RowDefinitions.Parent = this;
			InvalidateMeasure();
		}
	}

	internal bool MeasureOverrideInProgress
	{
		get
		{
			return CheckFlags(Flags.MeasureOverrideInProgress);
		}
		set
		{
			SetFlags(value, Flags.MeasureOverrideInProgress);
		}
	}

	internal bool ArrangeOverrideInProgress
	{
		get
		{
			return CheckFlags(Flags.ArrangeOverrideInProgress);
		}
		set
		{
			SetFlags(value, Flags.ArrangeOverrideInProgress);
		}
	}

	[MemberNotNull("_extData")]
	internal bool ColumnDefinitionsDirty
	{
		[MemberNotNull("_extData")]
		get
		{
			return ColumnDefinitions.IsDirty;
		}
		[MemberNotNull("_extData")]
		set
		{
			ColumnDefinitions.IsDirty = value;
		}
	}

	[MemberNotNull("_extData")]
	internal bool RowDefinitionsDirty
	{
		[MemberNotNull("_extData")]
		get
		{
			return RowDefinitions.IsDirty;
		}
		[MemberNotNull("_extData")]
		set
		{
			RowDefinitions.IsDirty = value;
		}
	}

	private IReadOnlyList<DefinitionBase> DefinitionsU => _extData.DefinitionsU;

	private IReadOnlyList<DefinitionBase> DefinitionsV => _extData.DefinitionsV;

	private DefinitionBase?[] TempDefinitions
	{
		get
		{
			ExtendedData extData = _extData;
			int num = Math.Max(DefinitionsU.Count, DefinitionsV.Count) * 2;
			if (extData.TempDefinitions == null || extData.TempDefinitions.Length < num)
			{
				WeakReference weakReference = (WeakReference)Thread.GetData(s_tempDefinitionsDataSlot);
				if (weakReference == null)
				{
					extData.TempDefinitions = new DefinitionBase[num];
					Thread.SetData(s_tempDefinitionsDataSlot, new WeakReference(extData.TempDefinitions));
				}
				else
				{
					extData.TempDefinitions = (DefinitionBase[])weakReference.Target;
					if (extData.TempDefinitions == null || extData.TempDefinitions.Length < num)
					{
						extData.TempDefinitions = new DefinitionBase[num];
						weakReference.Target = extData.TempDefinitions;
					}
				}
			}
			return extData.TempDefinitions;
		}
	}

	private int[] DefinitionIndices
	{
		get
		{
			int num = Math.Max(Math.Max(DefinitionsU.Count, DefinitionsV.Count), 1) * 2;
			if (_definitionIndices == null || _definitionIndices.Length < num)
			{
				_definitionIndices = new int[num];
			}
			return _definitionIndices;
		}
	}

	private double[] RoundingErrors
	{
		get
		{
			int num = Math.Max(DefinitionsU.Count, DefinitionsV.Count);
			if (_roundingErrors == null && num == 0)
			{
				_roundingErrors = new double[1];
			}
			else if (_roundingErrors == null || _roundingErrors.Length < num)
			{
				_roundingErrors = new double[num];
			}
			return _roundingErrors;
		}
	}

	private CellCache[] PrivateCells => _extData.CellCachesCollection;

	private bool CellsStructureDirty
	{
		get
		{
			return !CheckFlags(Flags.ValidCellsStructure);
		}
		set
		{
			SetFlags(!value, Flags.ValidCellsStructure);
		}
	}

	private bool ListenToNotifications
	{
		get
		{
			return CheckFlags(Flags.ListenToNotifications);
		}
		set
		{
			SetFlags(value, Flags.ListenToNotifications);
		}
	}

	private bool SizeToContentU
	{
		get
		{
			return CheckFlags(Flags.SizeToContentU);
		}
		set
		{
			SetFlags(value, Flags.SizeToContentU);
		}
	}

	private bool SizeToContentV
	{
		get
		{
			return CheckFlags(Flags.SizeToContentV);
		}
		set
		{
			SetFlags(value, Flags.SizeToContentV);
		}
	}

	private bool HasStarCellsU
	{
		get
		{
			return CheckFlags(Flags.HasStarCellsU);
		}
		set
		{
			SetFlags(value, Flags.HasStarCellsU);
		}
	}

	private bool HasStarCellsV
	{
		get
		{
			return CheckFlags(Flags.HasStarCellsV);
		}
		set
		{
			SetFlags(value, Flags.HasStarCellsV);
		}
	}

	private bool HasGroup3CellsInAutoRows
	{
		get
		{
			return CheckFlags(Flags.HasGroup3CellsInAutoRows);
		}
		set
		{
			SetFlags(value, Flags.HasGroup3CellsInAutoRows);
		}
	}

	static Grid()
	{
		s_tempDefinitionsDataSlot = Thread.AllocateDataSlot();
		s_spanPreferredDistributionOrderComparer = new SpanPreferredDistributionOrderComparer();
		s_spanMaxDistributionOrderComparer = new SpanMaxDistributionOrderComparer();
		s_minRatioComparer = new MinRatioComparer();
		s_maxRatioComparer = new MaxRatioComparer();
		s_starWeightComparer = new StarWeightComparer();
		ShowGridLinesProperty = AvaloniaProperty.Register<Grid, bool>("ShowGridLines", defaultValue: false);
		RowSpacingProperty = AvaloniaProperty.Register<Grid, double>("RowSpacing", 0.0);
		ColumnSpacingProperty = AvaloniaProperty.Register<Grid, double>("ColumnSpacing", 0.0);
		ColumnProperty = AvaloniaProperty.RegisterAttached<Grid, Control, int>("Column", 0, inherits: false, BindingMode.OneWay, (int v) => v >= 0);
		RowProperty = AvaloniaProperty.RegisterAttached<Grid, Control, int>("Row", 0, inherits: false, BindingMode.OneWay, (int v) => v >= 0);
		ColumnSpanProperty = AvaloniaProperty.RegisterAttached<Grid, Control, int>("ColumnSpan", 1, inherits: false, BindingMode.OneWay, (int v) => v > 0);
		RowSpanProperty = AvaloniaProperty.RegisterAttached<Grid, Control, int>("RowSpan", 1, inherits: false, BindingMode.OneWay, (int v) => v > 0);
		IsSharedSizeScopeProperty = AvaloniaProperty.RegisterAttached<Grid, Control, bool>("IsSharedSizeScope", defaultValue: false);
		ShowGridLinesProperty.Changed.AddClassHandler<Grid>(OnShowGridLinesPropertyChanged);
		ColumnSpacingProperty.Changed.AddClassHandler<Control>(OnSpacingPropertyChanged);
		RowSpacingProperty.Changed.AddClassHandler<Control>(OnSpacingPropertyChanged);
		IsSharedSizeScopeProperty.Changed.AddClassHandler<Control>(DefinitionBase.OnIsSharedSizeScopePropertyChanged);
		ColumnProperty.Changed.AddClassHandler<Control>(OnCellAttachedPropertyChanged);
		ColumnSpanProperty.Changed.AddClassHandler<Control>(OnCellAttachedPropertyChanged);
		RowProperty.Changed.AddClassHandler<Control>(OnCellAttachedPropertyChanged);
		RowSpanProperty.Changed.AddClassHandler<Control>(OnCellAttachedPropertyChanged);
		Layoutable.AffectsMeasure<Grid>(new AvaloniaProperty[2] { ColumnSpacingProperty, RowSpacingProperty });
		Panel.AffectsParentMeasure<Grid>(new AvaloniaProperty[4] { ColumnProperty, ColumnSpanProperty, RowProperty, RowSpanProperty });
	}

	public static void SetColumn(Control element, int value)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		element.SetValue(ColumnProperty, value);
	}

	public static int GetColumn(Control element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return element.GetValue(ColumnProperty);
	}

	public static void SetRow(Control element, int value)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		element.SetValue(RowProperty, value);
	}

	public static int GetRow(Control element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return element.GetValue(RowProperty);
	}

	public static void SetColumnSpan(Control element, int value)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		element.SetValue(ColumnSpanProperty, value);
	}

	public static int GetColumnSpan(Control element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return element.GetValue(ColumnSpanProperty);
	}

	public static void SetRowSpan(Control element, int value)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		element.SetValue(RowSpanProperty, value);
	}

	public static int GetRowSpan(Control element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return element.GetValue(RowSpanProperty);
	}

	public static void SetIsSharedSizeScope(Control element, bool value)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		element.SetValue(IsSharedSizeScopeProperty, value);
	}

	public static bool GetIsSharedSizeScope(Control element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return element.GetValue(IsSharedSizeScopeProperty);
	}

	protected override Size MeasureOverride(Size constraint)
	{
		ExtendedData extData = _extData;
		Size result;
		try
		{
			ListenToNotifications = true;
			MeasureOverrideInProgress = true;
			if (extData == null)
			{
				result = default(Size);
				Controls children = base.Children;
				int i = 0;
				for (int count = children.Count; i < count; i++)
				{
					Control control = children[i];
					control.Measure(constraint);
					result = new Size(Math.Max(result.Width, control.DesiredSize.Width), Math.Max(result.Height, control.DesiredSize.Height));
				}
			}
			else
			{
				bool flag = double.IsPositiveInfinity(constraint.Width);
				bool flag2 = double.IsPositiveInfinity(constraint.Height);
				if (RowDefinitionsDirty || ColumnDefinitionsDirty)
				{
					if (_definitionIndices != null)
					{
						Array.Clear(_definitionIndices, 0, _definitionIndices.Length);
						_definitionIndices = null;
					}
					if (base.UseLayoutRounding && _roundingErrors != null)
					{
						Array.Clear(_roundingErrors, 0, _roundingErrors.Length);
						_roundingErrors = null;
					}
				}
				ValidateDefinitionsUStructure();
				ValidateDefinitionsLayout(DefinitionsU, flag);
				ValidateDefinitionsVStructure();
				ValidateDefinitionsLayout(DefinitionsV, flag2);
				CellsStructureDirty |= SizeToContentU != flag || SizeToContentV != flag2;
				SizeToContentU = flag;
				SizeToContentV = flag2;
				ValidateCells();
				MeasureCellsGroup(extData.CellGroup1, ignoreDesiredSizeU: false, forceInfinityV: false);
				_ = RowSpacing;
				_ = ColumnSpacing;
				double num = RowSpacing * (double)(DefinitionsV.Count - 1);
				double num2 = ColumnSpacing * (double)(DefinitionsU.Count - 1);
				Size size = new Size(constraint.Width - num2, constraint.Height - num);
				if (!HasGroup3CellsInAutoRows)
				{
					if (HasStarCellsV)
					{
						ResolveStar(DefinitionsV, size.Height);
					}
					MeasureCellsGroup(extData.CellGroup2, ignoreDesiredSizeU: false, forceInfinityV: false);
					if (HasStarCellsU)
					{
						ResolveStar(DefinitionsU, size.Width);
					}
					MeasureCellsGroup(extData.CellGroup3, ignoreDesiredSizeU: false, forceInfinityV: false);
				}
				else if (extData.CellGroup2 > PrivateCells.Length)
				{
					if (HasStarCellsU)
					{
						ResolveStar(DefinitionsU, size.Width);
					}
					MeasureCellsGroup(extData.CellGroup3, ignoreDesiredSizeU: false, forceInfinityV: false);
					if (HasStarCellsV)
					{
						ResolveStar(DefinitionsV, size.Height);
					}
				}
				else
				{
					bool hasDesiredSizeUChanged = false;
					int num3 = 0;
					double[] minSizes = CacheMinSizes(extData.CellGroup2, isRows: false);
					double[] minSizes2 = CacheMinSizes(extData.CellGroup3, isRows: true);
					MeasureCellsGroup(extData.CellGroup2, ignoreDesiredSizeU: false, forceInfinityV: true);
					do
					{
						if (hasDesiredSizeUChanged)
						{
							ApplyCachedMinSizes(minSizes2, isRows: true);
						}
						if (HasStarCellsU)
						{
							ResolveStar(DefinitionsU, size.Width);
						}
						MeasureCellsGroup(extData.CellGroup3, ignoreDesiredSizeU: false, forceInfinityV: false);
						ApplyCachedMinSizes(minSizes, isRows: false);
						if (HasStarCellsV)
						{
							ResolveStar(DefinitionsV, size.Height);
						}
						MeasureCellsGroup(extData.CellGroup2, num3 == 5, forceInfinityV: false, out hasDesiredSizeUChanged);
					}
					while (hasDesiredSizeUChanged && ++num3 <= 5);
				}
				MeasureCellsGroup(extData.CellGroup4, ignoreDesiredSizeU: false, forceInfinityV: false);
				result = new Size(CalculateDesiredSize(DefinitionsU) + ColumnSpacing * (double)(DefinitionsU.Count - 1), CalculateDesiredSize(DefinitionsV) + RowSpacing * (double)(DefinitionsV.Count - 1));
			}
		}
		finally
		{
			MeasureOverrideInProgress = false;
		}
		return result;
	}

	protected override Size ArrangeOverride(Size arrangeSize)
	{
		try
		{
			ArrangeOverrideInProgress = true;
			if (_extData == null)
			{
				Controls children = base.Children;
				int i = 0;
				for (int count = children.Count; i < count; i++)
				{
					children[i].Arrange(new Rect(arrangeSize));
				}
			}
			else
			{
				double rowSpacing = RowSpacing;
				double columnSpacing = ColumnSpacing;
				double num = rowSpacing * (double)(DefinitionsV.Count - 1);
				double num2 = columnSpacing * (double)(DefinitionsU.Count - 1);
				SetFinalSize(DefinitionsU, arrangeSize.Width - num2, columns: true);
				SetFinalSize(DefinitionsV, arrangeSize.Height - num, columns: false);
				Controls children2 = base.Children;
				for (int j = 0; j < PrivateCells.Length; j++)
				{
					Control control = children2[j];
					int columnIndex = PrivateCells[j].ColumnIndex;
					int rowIndex = PrivateCells[j].RowIndex;
					int columnSpan = PrivateCells[j].ColumnSpan;
					int rowSpan = PrivateCells[j].RowSpan;
					Rect rect = new Rect((columnIndex == 0) ? 0.0 : DefinitionsU[columnIndex].FinalOffset, (rowIndex == 0) ? 0.0 : DefinitionsV[rowIndex].FinalOffset, GetFinalSizeForRange(DefinitionsU, columnIndex, columnSpan, columnSpacing), GetFinalSizeForRange(DefinitionsV, rowIndex, rowSpan, rowSpacing));
					control.Arrange(rect);
				}
				EnsureGridLinesRenderer()?.UpdateRenderBounds(arrangeSize);
			}
		}
		finally
		{
			SetValid();
			ArrangeOverrideInProgress = false;
		}
		return arrangeSize;
	}

	protected override void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		CellsStructureDirty = true;
		base.ChildrenChanged(sender, e);
	}

	internal void Invalidate()
	{
		CellsStructureDirty = true;
		InvalidateMeasure();
	}

	internal double GetFinalColumnDefinitionWidth(int columnIndex)
	{
		double num = 0.0;
		if (!ColumnDefinitionsDirty)
		{
			IReadOnlyList<DefinitionBase> definitionsU = DefinitionsU;
			num = definitionsU[(columnIndex + 1) % definitionsU.Count].FinalOffset;
			if (columnIndex != 0)
			{
				num -= definitionsU[columnIndex].FinalOffset;
			}
		}
		return num;
	}

	internal double GetFinalRowDefinitionHeight(int rowIndex)
	{
		double num = 0.0;
		if (!RowDefinitionsDirty)
		{
			IReadOnlyList<DefinitionBase> definitionsV = DefinitionsV;
			num = definitionsV[(rowIndex + 1) % definitionsV.Count].FinalOffset;
			if (rowIndex != 0)
			{
				num -= definitionsV[rowIndex].FinalOffset;
			}
		}
		return num;
	}

	private void ValidateCells()
	{
		if (CellsStructureDirty)
		{
			ValidateCellsCore();
			CellsStructureDirty = false;
		}
	}

	private void ValidateCellsCore()
	{
		Controls children = base.Children;
		ExtendedData extData = _extData;
		extData.CellCachesCollection = new CellCache[children.Count];
		extData.CellGroup1 = int.MaxValue;
		extData.CellGroup2 = int.MaxValue;
		extData.CellGroup3 = int.MaxValue;
		extData.CellGroup4 = int.MaxValue;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		for (int num = PrivateCells.Length - 1; num >= 0; num--)
		{
			Control element = children[num];
			CellCache cellCache = default(CellCache);
			cellCache.ColumnIndex = Math.Min(GetColumn(element), DefinitionsU.Count - 1);
			cellCache.RowIndex = Math.Min(GetRow(element), DefinitionsV.Count - 1);
			cellCache.ColumnSpan = Math.Min(GetColumnSpan(element), DefinitionsU.Count - cellCache.ColumnIndex);
			cellCache.RowSpan = Math.Min(GetRowSpan(element), DefinitionsV.Count - cellCache.RowIndex);
			cellCache.SizeTypeU = GetLengthTypeForRange(DefinitionsU, cellCache.ColumnIndex, cellCache.ColumnSpan);
			cellCache.SizeTypeV = GetLengthTypeForRange(DefinitionsV, cellCache.RowIndex, cellCache.RowSpan);
			flag |= cellCache.IsStarU;
			flag2 |= cellCache.IsStarV;
			if (!cellCache.IsStarV)
			{
				if (!cellCache.IsStarU)
				{
					cellCache.Next = extData.CellGroup1;
					extData.CellGroup1 = num;
				}
				else
				{
					cellCache.Next = extData.CellGroup3;
					extData.CellGroup3 = num;
					flag3 |= cellCache.IsAutoV;
				}
			}
			else if (cellCache.IsAutoU && !cellCache.IsStarU)
			{
				cellCache.Next = extData.CellGroup2;
				extData.CellGroup2 = num;
			}
			else
			{
				cellCache.Next = extData.CellGroup4;
				extData.CellGroup4 = num;
			}
			PrivateCells[num] = cellCache;
		}
		HasStarCellsU = flag;
		HasStarCellsV = flag2;
		HasGroup3CellsInAutoRows = flag3;
	}

	private void ValidateDefinitionsUStructure()
	{
		if (!ColumnDefinitionsDirty)
		{
			return;
		}
		ExtendedData extData = _extData;
		if (extData.ColumnDefinitions == null)
		{
			if (extData.DefinitionsU == null)
			{
				extData.DefinitionsU = new DefinitionBase[1]
				{
					new ColumnDefinition
					{
						Parent = this
					}
				};
			}
		}
		else if (extData.ColumnDefinitions.Count == 0)
		{
			extData.DefinitionsU = new DefinitionBase[1]
			{
				new ColumnDefinition
				{
					Parent = this
				}
			};
		}
		else
		{
			extData.DefinitionsU = extData.ColumnDefinitions;
		}
		ColumnDefinitionsDirty = false;
	}

	private void ValidateDefinitionsVStructure()
	{
		if (!RowDefinitionsDirty)
		{
			return;
		}
		ExtendedData extData = _extData;
		if (extData.RowDefinitions == null)
		{
			if (extData.DefinitionsV == null)
			{
				extData.DefinitionsV = new DefinitionBase[1]
				{
					new RowDefinition
					{
						Parent = this
					}
				};
			}
		}
		else if (extData.RowDefinitions.Count == 0)
		{
			extData.DefinitionsV = new DefinitionBase[1]
			{
				new RowDefinition
				{
					Parent = this
				}
			};
		}
		else
		{
			extData.DefinitionsV = extData.RowDefinitions;
		}
		RowDefinitionsDirty = false;
	}

	private void ValidateDefinitionsLayout(IReadOnlyList<DefinitionBase> definitions, bool treatStarAsAuto)
	{
		for (int i = 0; i < definitions.Count; i++)
		{
			definitions[i].OnBeforeLayout(this);
			double num = definitions[i].UserMinSize;
			double userMaxSize = definitions[i].UserMaxSize;
			double val = 0.0;
			switch (definitions[i].UserSize.GridUnitType)
			{
			case GridUnitType.Pixel:
				definitions[i].SizeType = LayoutTimeSizeType.Pixel;
				val = definitions[i].UserSize.Value;
				num = Math.Max(num, Math.Min(val, userMaxSize));
				break;
			case GridUnitType.Auto:
				definitions[i].SizeType = LayoutTimeSizeType.Auto;
				val = double.PositiveInfinity;
				break;
			case GridUnitType.Star:
				if (treatStarAsAuto)
				{
					definitions[i].SizeType = LayoutTimeSizeType.Auto;
					val = double.PositiveInfinity;
				}
				else
				{
					definitions[i].SizeType = LayoutTimeSizeType.Star;
					val = double.PositiveInfinity;
				}
				break;
			}
			definitions[i].UpdateMinSize(num);
			definitions[i].MeasureSize = Math.Max(num, Math.Min(val, userMaxSize));
		}
	}

	private double[] CacheMinSizes(int cellsHead, bool isRows)
	{
		double[] array = (isRows ? new double[DefinitionsV.Count] : new double[DefinitionsU.Count]);
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = -1.0;
		}
		int num = cellsHead;
		do
		{
			if (isRows)
			{
				array[PrivateCells[num].RowIndex] = DefinitionsV[PrivateCells[num].RowIndex].RawMinSize;
			}
			else
			{
				array[PrivateCells[num].ColumnIndex] = DefinitionsU[PrivateCells[num].ColumnIndex].RawMinSize;
			}
			num = PrivateCells[num].Next;
		}
		while (num < PrivateCells.Length);
		return array;
	}

	private void ApplyCachedMinSizes(double[] minSizes, bool isRows)
	{
		for (int i = 0; i < minSizes.Length; i++)
		{
			if (MathUtilities.GreaterThanOrClose(minSizes[i], 0.0))
			{
				if (isRows)
				{
					DefinitionsV[i].SetMinSize(minSizes[i]);
				}
				else
				{
					DefinitionsU[i].SetMinSize(minSizes[i]);
				}
			}
		}
	}

	private void MeasureCellsGroup(int cellsHead, bool ignoreDesiredSizeU, bool forceInfinityV)
	{
		MeasureCellsGroup(cellsHead, ignoreDesiredSizeU, forceInfinityV, out var _);
	}

	private void MeasureCellsGroup(int cellsHead, bool ignoreDesiredSizeU, bool forceInfinityV, out bool hasDesiredSizeUChanged)
	{
		hasDesiredSizeUChanged = false;
		if (cellsHead >= PrivateCells.Length)
		{
			return;
		}
		Controls children = base.Children;
		Hashtable store = null;
		bool flag = forceInfinityV;
		int num = cellsHead;
		do
		{
			double width = children[num].DesiredSize.Width;
			MeasureCell(num, forceInfinityV);
			hasDesiredSizeUChanged |= !MathUtilities.AreClose(width, children[num].DesiredSize.Width);
			if (!ignoreDesiredSizeU)
			{
				if (PrivateCells[num].ColumnSpan == 1)
				{
					DefinitionsU[PrivateCells[num].ColumnIndex].UpdateMinSize(Math.Min(children[num].DesiredSize.Width, DefinitionsU[PrivateCells[num].ColumnIndex].UserMaxSize));
				}
				else
				{
					RegisterSpan(ref store, PrivateCells[num].ColumnIndex, PrivateCells[num].ColumnSpan, u: true, children[num].DesiredSize.Width);
				}
			}
			if (!flag)
			{
				if (PrivateCells[num].RowSpan == 1)
				{
					DefinitionsV[PrivateCells[num].RowIndex].UpdateMinSize(Math.Min(children[num].DesiredSize.Height, DefinitionsV[PrivateCells[num].RowIndex].UserMaxSize));
				}
				else
				{
					RegisterSpan(ref store, PrivateCells[num].RowIndex, PrivateCells[num].RowSpan, u: false, children[num].DesiredSize.Height);
				}
			}
			num = PrivateCells[num].Next;
		}
		while (num < PrivateCells.Length);
		if (store == null)
		{
			return;
		}
		foreach (DictionaryEntry item in store)
		{
			SpanKey spanKey = (SpanKey)item.Key;
			double desiredSize = (double)item.Value;
			EnsureMinSizeInDefinitionRange(spanKey.U ? DefinitionsU : DefinitionsV, spanKey.Start, spanKey.Count, spanKey.U ? ColumnSpacing : RowSpacing, desiredSize);
		}
	}

	private static void RegisterSpan(ref Hashtable? store, int start, int count, bool u, double value)
	{
		if (store == null)
		{
			store = new Hashtable();
		}
		SpanKey key = new SpanKey(start, count, u);
		object obj = store[key];
		if (obj == null || value > (double)obj)
		{
			store[key] = value;
		}
	}

	private void MeasureCell(int cell, bool forceInfinityV)
	{
		double width = ((!PrivateCells[cell].IsAutoU || PrivateCells[cell].IsStarU) ? GetMeasureSizeForRange(DefinitionsU, PrivateCells[cell].ColumnIndex, PrivateCells[cell].ColumnSpan, ColumnSpacing) : double.PositiveInfinity);
		double height = (forceInfinityV ? double.PositiveInfinity : ((!PrivateCells[cell].IsAutoV || PrivateCells[cell].IsStarV) ? GetMeasureSizeForRange(DefinitionsV, PrivateCells[cell].RowIndex, PrivateCells[cell].RowSpan, RowSpacing) : double.PositiveInfinity));
		Control control = base.Children[cell];
		Size availableSize = new Size(width, height);
		control.Measure(availableSize);
	}

	private static double GetMeasureSizeForRange(IReadOnlyList<DefinitionBase> definitions, int start, int count, double spacing)
	{
		double num = 0.0 - spacing;
		int num2 = start + count - 1;
		do
		{
			num += spacing + ((definitions[num2].SizeType == LayoutTimeSizeType.Auto) ? definitions[num2].MinSize : definitions[num2].MeasureSize);
		}
		while (--num2 >= start);
		return num;
	}

	private static LayoutTimeSizeType GetLengthTypeForRange(IReadOnlyList<DefinitionBase> definitions, int start, int count)
	{
		LayoutTimeSizeType layoutTimeSizeType = LayoutTimeSizeType.None;
		int num = start + count - 1;
		do
		{
			layoutTimeSizeType |= definitions[num].SizeType;
		}
		while (--num >= start);
		return layoutTimeSizeType;
	}

	private void EnsureMinSizeInDefinitionRange(IReadOnlyList<DefinitionBase> definitions, int start, int count, double spacing, double desiredSize)
	{
		double num = Math.Max(desiredSize - spacing * (double)(count - 1), 0.0);
		if (MathUtilities.IsZero(num))
		{
			return;
		}
		DefinitionBase[] tempDefinitions = TempDefinitions;
		int num2 = start + count;
		int num3 = 0;
		double num4 = 0.0;
		double num5 = 0.0;
		double num6 = 0.0;
		double num7 = 0.0;
		for (int i = start; i < num2; i++)
		{
			double minSize = definitions[i].MinSize;
			double preferredSize = definitions[i].PreferredSize;
			double num8 = Math.Max(definitions[i].UserMaxSize, minSize);
			num4 += minSize;
			num5 += preferredSize;
			num6 += num8;
			definitions[i].SizeCache = num8;
			if (num7 < num8)
			{
				num7 = num8;
			}
			if (definitions[i].UserSize.IsAuto)
			{
				num3++;
			}
			tempDefinitions[i - start] = definitions[i];
		}
		if (!(num > num4))
		{
			return;
		}
		if (num <= num5)
		{
			Array.Sort(tempDefinitions, 0, count, s_spanPreferredDistributionOrderComparer);
			int j = 0;
			double num9 = num;
			for (; j < num3; j++)
			{
				DefinitionBase definitionBase = tempDefinitions[j];
				num9 -= definitionBase.MinSize;
			}
			for (; j < count; j++)
			{
				DefinitionBase definitionBase2 = tempDefinitions[j];
				double num10 = Math.Min(num9 / (double)(count - j), definitionBase2.PreferredSize);
				if (num10 > definitionBase2.MinSize)
				{
					definitionBase2.UpdateMinSize(num10);
				}
				num9 -= num10;
			}
			return;
		}
		if (num <= num6)
		{
			Array.Sort(tempDefinitions, 0, count, s_spanMaxDistributionOrderComparer);
			int k = 0;
			double num11 = num - num5;
			for (; k < count - num3; k++)
			{
				DefinitionBase definitionBase3 = tempDefinitions[k];
				double preferredSize2 = definitionBase3.PreferredSize;
				double val = preferredSize2 + num11 / (double)(count - num3 - k);
				definitionBase3.UpdateMinSize(Math.Min(val, definitionBase3.SizeCache));
				num11 -= definitionBase3.MinSize - preferredSize2;
			}
			for (; k < count; k++)
			{
				DefinitionBase definitionBase4 = tempDefinitions[k];
				double minSize2 = definitionBase4.MinSize;
				double val2 = minSize2 + num11 / (double)(count - k);
				definitionBase4.UpdateMinSize(Math.Min(val2, definitionBase4.SizeCache));
				num11 -= definitionBase4.MinSize - minSize2;
			}
			return;
		}
		double num12 = num / (double)count;
		if (num12 < num7 && !MathUtilities.AreClose(num12, num7))
		{
			double num13 = num7 * (double)count - num6;
			double num14 = num - num6;
			for (int l = 0; l < count; l++)
			{
				DefinitionBase definitionBase5 = tempDefinitions[l];
				double num15 = (num7 - definitionBase5.SizeCache) * num14 / num13;
				definitionBase5.UpdateMinSize(definitionBase5.SizeCache + num15);
			}
		}
		else
		{
			for (int m = 0; m < count; m++)
			{
				tempDefinitions[m].UpdateMinSize(num12);
			}
		}
	}

	private void ResolveStar(IReadOnlyList<DefinitionBase> definitions, double availableSize)
	{
		ResolveStarMaxDiscrepancy(definitions, availableSize);
	}

	private void ResolveStarMaxDiscrepancy(IReadOnlyList<DefinitionBase> definitions, double availableSize)
	{
		int count = definitions.Count;
		DefinitionBase[] tempDefinitions = TempDefinitions;
		int num = 0;
		int num2 = 0;
		double num3 = 0.0;
		double num4 = 0.0;
		int num5 = 0;
		double scale = 1.0;
		double num6 = 0.0;
		for (int i = 0; i < count; i++)
		{
			DefinitionBase definitionBase = definitions[i];
			if (definitionBase.SizeType == LayoutTimeSizeType.Star)
			{
				num5++;
				definitionBase.MeasureSize = 1.0;
				if (definitionBase.UserSize.Value > num6)
				{
					num6 = definitionBase.UserSize.Value;
				}
			}
		}
		if (double.IsPositiveInfinity(num6))
		{
			scale = -1.0;
		}
		else if (num5 > 0)
		{
			double num7 = Math.Floor(Math.Log(double.MaxValue / num6 / (double)num5, 2.0));
			if (num7 < 0.0)
			{
				scale = Math.Pow(2.0, num7 - 4.0);
			}
		}
		bool flag = true;
		while (flag)
		{
			num4 = 0.0;
			num3 = 0.0;
			num = (num2 = 0);
			for (int j = 0; j < count; j++)
			{
				DefinitionBase definitionBase2 = definitions[j];
				switch (definitionBase2.SizeType)
				{
				case LayoutTimeSizeType.Auto:
					num3 += definitions[j].MinSize;
					break;
				case LayoutTimeSizeType.Pixel:
					num3 += definitionBase2.MeasureSize;
					break;
				case LayoutTimeSizeType.Star:
				{
					if (definitionBase2.MeasureSize < 0.0)
					{
						num3 += 0.0 - definitionBase2.MeasureSize;
						break;
					}
					double num8 = StarWeight(definitionBase2, scale);
					num4 += num8;
					if (definitionBase2.MinSize > 0.0)
					{
						tempDefinitions[num++] = definitionBase2;
						definitionBase2.MeasureSize = num8 / definitionBase2.MinSize;
					}
					double num9 = Math.Max(definitionBase2.MinSize, definitionBase2.UserMaxSize);
					if (!double.IsPositiveInfinity(num9))
					{
						tempDefinitions[count + num2++] = definitionBase2;
						definitionBase2.SizeCache = num8 / num9;
					}
					break;
				}
				}
			}
			int num10 = num;
			int num11 = num2;
			double num12 = 0.0;
			double num13 = availableSize - num3;
			double num14 = num4 - num12;
			Array.Sort(tempDefinitions, 0, num, s_minRatioComparer);
			Array.Sort(tempDefinitions, count, num2, s_maxRatioComparer);
			while (num + num2 > 0 && num13 > 0.0)
			{
				if (num14 < num4 * (1.0 / 256.0))
				{
					num12 = 0.0;
					num4 = 0.0;
					for (int k = 0; k < count; k++)
					{
						DefinitionBase definitionBase3 = definitions[k];
						if (definitionBase3.SizeType == LayoutTimeSizeType.Star && definitionBase3.MeasureSize > 0.0)
						{
							num4 += StarWeight(definitionBase3, scale);
						}
					}
					num14 = num4 - num12;
				}
				double minRatio = ((num > 0) ? tempDefinitions[num - 1].MeasureSize : double.PositiveInfinity);
				double maxRatio = ((num2 > 0) ? tempDefinitions[count + num2 - 1].SizeCache : (-1.0));
				double proportion = num14 / num13;
				bool? flag2 = Choose(minRatio, maxRatio, proportion);
				if (!flag2.HasValue)
				{
					break;
				}
				DefinitionBase definitionBase4;
				double num15;
				if (flag2 == true)
				{
					definitionBase4 = tempDefinitions[num - 1];
					num15 = definitionBase4.MinSize;
					num--;
				}
				else
				{
					definitionBase4 = tempDefinitions[count + num2 - 1];
					num15 = Math.Max(definitionBase4.MinSize, definitionBase4.UserMaxSize);
					num2--;
				}
				num3 += num15;
				definitionBase4.MeasureSize = 0.0 - num15;
				num12 += StarWeight(definitionBase4, scale);
				num5--;
				num13 = availableSize - num3;
				num14 = num4 - num12;
				while (num > 0 && tempDefinitions[num - 1].MeasureSize < 0.0)
				{
					num--;
					tempDefinitions[num] = null;
				}
				while (num2 > 0 && tempDefinitions[count + num2 - 1].MeasureSize < 0.0)
				{
					num2--;
					tempDefinitions[count + num2] = null;
				}
			}
			flag = false;
			if (num5 == 0 && num3 < availableSize)
			{
				for (int l = num; l < num10; l++)
				{
					DefinitionBase definitionBase5 = tempDefinitions[l];
					if (definitionBase5 != null)
					{
						definitionBase5.MeasureSize = 1.0;
						num5++;
						flag = true;
					}
				}
			}
			if (!(num3 > availableSize))
			{
				continue;
			}
			for (int m = num2; m < num11; m++)
			{
				DefinitionBase definitionBase6 = tempDefinitions[count + m];
				if (definitionBase6 != null)
				{
					definitionBase6.MeasureSize = 1.0;
					num5++;
					flag = true;
				}
			}
		}
		num5 = 0;
		for (int n = 0; n < count; n++)
		{
			DefinitionBase definitionBase7 = definitions[n];
			if (definitionBase7.SizeType == LayoutTimeSizeType.Star)
			{
				if (definitionBase7.MeasureSize < 0.0)
				{
					definitionBase7.MeasureSize = 0.0 - definitionBase7.MeasureSize;
					continue;
				}
				tempDefinitions[num5++] = definitionBase7;
				definitionBase7.MeasureSize = StarWeight(definitionBase7, scale);
			}
		}
		if (num5 > 0)
		{
			Array.Sort(tempDefinitions, 0, num5, s_starWeightComparer);
			num4 = 0.0;
			for (int num16 = 0; num16 < num5; num16++)
			{
				DefinitionBase definitionBase8 = tempDefinitions[num16];
				num4 = (definitionBase8.SizeCache = num4 + definitionBase8.MeasureSize);
			}
			for (int num18 = num5 - 1; num18 >= 0; num18--)
			{
				DefinitionBase definitionBase9 = tempDefinitions[num18];
				double val = ((definitionBase9.MeasureSize > 0.0) ? (Math.Max(availableSize - num3, 0.0) * (definitionBase9.MeasureSize / definitionBase9.SizeCache)) : 0.0);
				val = Math.Min(val, definitionBase9.UserMaxSize);
				val = (definitionBase9.MeasureSize = Math.Max(definitionBase9.MinSize, val));
				num3 += val;
			}
		}
	}

	private static double CalculateDesiredSize(IReadOnlyList<DefinitionBase> definitions)
	{
		double num = 0.0;
		for (int i = 0; i < definitions.Count; i++)
		{
			num += definitions[i].MinSize;
		}
		return num;
	}

	private void SetFinalSize(IReadOnlyList<DefinitionBase> definitions, double finalSize, bool columns)
	{
		SetFinalSizeMaxDiscrepancy(definitions, finalSize, columns);
	}

	private void SetFinalSizeMaxDiscrepancy(IReadOnlyList<DefinitionBase> definitions, double finalSize, bool columns)
	{
		int count = definitions.Count;
		int[] definitionIndices = DefinitionIndices;
		int num = 0;
		int num2 = 0;
		double num3 = 0.0;
		double num4 = 0.0;
		int num5 = 0;
		double scale = 1.0;
		double num6 = 0.0;
		for (int i = 0; i < count; i++)
		{
			DefinitionBase definitionBase = definitions[i];
			if (definitionBase.UserSize.IsStar)
			{
				num5++;
				definitionBase.MeasureSize = 1.0;
				if (definitionBase.UserSize.Value > num6)
				{
					num6 = definitionBase.UserSize.Value;
				}
			}
		}
		if (double.IsPositiveInfinity(num6))
		{
			scale = -1.0;
		}
		else if (num5 > 0)
		{
			double num7 = Math.Floor(Math.Log(double.MaxValue / num6 / (double)num5, 2.0));
			if (num7 < 0.0)
			{
				scale = Math.Pow(2.0, num7 - 4.0);
			}
		}
		bool flag = true;
		while (flag)
		{
			num4 = 0.0;
			num3 = 0.0;
			num = (num2 = 0);
			for (int j = 0; j < count; j++)
			{
				DefinitionBase definitionBase2 = definitions[j];
				if (definitionBase2.UserSize.IsStar)
				{
					if (definitionBase2.MeasureSize < 0.0)
					{
						num3 += 0.0 - definitionBase2.MeasureSize;
						continue;
					}
					double num8 = StarWeight(definitionBase2, scale);
					num4 += num8;
					if (definitionBase2.MinSizeForArrange > 0.0)
					{
						definitionIndices[num++] = j;
						definitionBase2.MeasureSize = num8 / definitionBase2.MinSizeForArrange;
					}
					double num9 = Math.Max(definitionBase2.MinSizeForArrange, definitionBase2.UserMaxSize);
					if (!double.IsPositiveInfinity(num9))
					{
						definitionIndices[count + num2++] = j;
						definitionBase2.SizeCache = num8 / num9;
					}
				}
				else
				{
					double num10 = 0.0;
					switch (definitionBase2.UserSize.GridUnitType)
					{
					case GridUnitType.Pixel:
						num10 = definitionBase2.UserSize.Value;
						break;
					case GridUnitType.Auto:
						num10 = definitionBase2.MinSizeForArrange;
						break;
					}
					definitionBase2.SizeCache = Math.Max(val2: Math.Min(num10, (!definitionBase2.IsShared) ? definitionBase2.UserMaxSize : num10), val1: definitionBase2.MinSizeForArrange);
					num3 += definitionBase2.SizeCache;
				}
			}
			int num11 = num;
			int num12 = num2;
			double num13 = 0.0;
			double num14 = finalSize - num3;
			double num15 = num4 - num13;
			MinRatioIndexComparer comparer = new MinRatioIndexComparer(definitions);
			Array.Sort(definitionIndices, 0, num, comparer);
			MaxRatioIndexComparer comparer2 = new MaxRatioIndexComparer(definitions);
			Array.Sort(definitionIndices, count, num2, comparer2);
			while (num + num2 > 0 && num14 > 0.0)
			{
				if (num15 < num4 * (1.0 / 256.0))
				{
					num13 = 0.0;
					num4 = 0.0;
					for (int k = 0; k < count; k++)
					{
						DefinitionBase definitionBase3 = definitions[k];
						if (definitionBase3.UserSize.IsStar && definitionBase3.MeasureSize > 0.0)
						{
							num4 += StarWeight(definitionBase3, scale);
						}
					}
					num15 = num4 - num13;
				}
				double minRatio = ((num > 0) ? definitions[definitionIndices[num - 1]].MeasureSize : double.PositiveInfinity);
				double maxRatio = ((num2 > 0) ? definitions[definitionIndices[count + num2 - 1]].SizeCache : (-1.0));
				double proportion = num15 / num14;
				bool? flag2 = Choose(minRatio, maxRatio, proportion);
				if (!flag2.HasValue)
				{
					break;
				}
				DefinitionBase definitionBase4;
				double num16;
				if (flag2 == true)
				{
					int index = definitionIndices[num - 1];
					definitionBase4 = definitions[index];
					num16 = definitionBase4.MinSizeForArrange;
					num--;
				}
				else
				{
					int index = definitionIndices[count + num2 - 1];
					definitionBase4 = definitions[index];
					num16 = Math.Max(definitionBase4.MinSizeForArrange, definitionBase4.UserMaxSize);
					num2--;
				}
				num3 += num16;
				definitionBase4.MeasureSize = 0.0 - num16;
				num13 += StarWeight(definitionBase4, scale);
				num5--;
				num14 = finalSize - num3;
				num15 = num4 - num13;
				while (num > 0 && definitions[definitionIndices[num - 1]].MeasureSize < 0.0)
				{
					num--;
					definitionIndices[num] = -1;
				}
				while (num2 > 0 && definitions[definitionIndices[count + num2 - 1]].MeasureSize < 0.0)
				{
					num2--;
					definitionIndices[count + num2] = -1;
				}
			}
			flag = false;
			if (num5 == 0 && num3 < finalSize)
			{
				for (int l = num; l < num11; l++)
				{
					if (definitionIndices[l] >= 0)
					{
						definitions[definitionIndices[l]].MeasureSize = 1.0;
						num5++;
						flag = true;
					}
				}
			}
			if (!(num3 > finalSize))
			{
				continue;
			}
			for (int m = num2; m < num12; m++)
			{
				if (definitionIndices[count + m] >= 0)
				{
					definitions[definitionIndices[count + m]].MeasureSize = 1.0;
					num5++;
					flag = true;
				}
			}
		}
		num5 = 0;
		for (int n = 0; n < count; n++)
		{
			DefinitionBase definitionBase5 = definitions[n];
			if (definitionBase5.UserSize.IsStar)
			{
				if (definitionBase5.MeasureSize < 0.0)
				{
					definitionBase5.SizeCache = 0.0 - definitionBase5.MeasureSize;
					continue;
				}
				definitionIndices[num5++] = n;
				definitionBase5.MeasureSize = StarWeight(definitionBase5, scale);
			}
		}
		if (num5 > 0)
		{
			StarWeightIndexComparer comparer3 = new StarWeightIndexComparer(definitions);
			Array.Sort(definitionIndices, 0, num5, comparer3);
			num4 = 0.0;
			for (int num17 = 0; num17 < num5; num17++)
			{
				DefinitionBase definitionBase6 = definitions[definitionIndices[num17]];
				num4 = (definitionBase6.SizeCache = num4 + definitionBase6.MeasureSize);
			}
			for (int num19 = num5 - 1; num19 >= 0; num19--)
			{
				DefinitionBase definitionBase7 = definitions[definitionIndices[num19]];
				double val = ((definitionBase7.MeasureSize > 0.0) ? (Math.Max(finalSize - num3, 0.0) * (definitionBase7.MeasureSize / definitionBase7.SizeCache)) : 0.0);
				val = Math.Min(val, definitionBase7.UserMaxSize);
				val = Math.Max(definitionBase7.MinSizeForArrange, val);
				num3 += val;
				definitionBase7.SizeCache = val;
			}
		}
		if (base.UseLayoutRounding)
		{
			double num20 = this.GetLayoutRoot()?.LayoutScaling ?? 1.0;
			double[] roundingErrors = RoundingErrors;
			double num21 = 0.0;
			for (int num22 = 0; num22 < definitions.Count; num22++)
			{
				DefinitionBase definitionBase8 = definitions[num22];
				double num23 = LayoutHelper.RoundLayoutValue(definitionBase8.SizeCache, num20);
				roundingErrors[num22] = num23 - definitionBase8.SizeCache;
				definitionBase8.SizeCache = num23;
				num21 += num23;
			}
			if (!MathUtilities.AreClose(num21, finalSize))
			{
				for (int num24 = 0; num24 < definitions.Count; num24++)
				{
					definitionIndices[num24] = num24;
				}
				RoundingErrorIndexComparer comparer4 = new RoundingErrorIndexComparer(roundingErrors);
				Array.Sort(definitionIndices, 0, definitions.Count, comparer4);
				double num25 = num21;
				double num26 = 1.0 / num20;
				if (num21 > finalSize)
				{
					int num27 = definitions.Count - 1;
					while (num25 > finalSize && !MathUtilities.AreClose(num25, finalSize) && num27 >= 0)
					{
						DefinitionBase definitionBase9 = definitions[definitionIndices[num27]];
						double val2 = definitionBase9.SizeCache - num26;
						val2 = Math.Max(val2, definitionBase9.MinSizeForArrange);
						if (val2 < definitionBase9.SizeCache)
						{
							num25 -= num26;
						}
						definitionBase9.SizeCache = val2;
						num27--;
					}
				}
				else if (num21 < finalSize)
				{
					int num28 = 0;
					while (num25 < finalSize && !MathUtilities.AreClose(num25, finalSize) && num28 < definitions.Count)
					{
						DefinitionBase definitionBase10 = definitions[definitionIndices[num28]];
						double val3 = definitionBase10.SizeCache + num26;
						val3 = Math.Max(val3, definitionBase10.MinSizeForArrange);
						if (val3 > definitionBase10.SizeCache)
						{
							num25 += num26;
						}
						definitionBase10.SizeCache = val3;
						num28++;
					}
				}
			}
		}
		double num29 = (columns ? ColumnSpacing : RowSpacing);
		definitions[0].FinalOffset = 0.0;
		for (int num30 = 0; num30 < definitions.Count; num30++)
		{
			definitions[(num30 + 1) % definitions.Count].FinalOffset = definitions[num30].FinalOffset + definitions[num30].SizeCache + num29;
		}
	}

	private static bool? Choose(double minRatio, double maxRatio, double proportion)
	{
		if (minRatio < proportion)
		{
			if (maxRatio > proportion)
			{
				double num = Math.Floor(Math.Log(minRatio, 2.0));
				double num2 = Math.Floor(Math.Log(maxRatio, 2.0));
				double num3 = Math.Pow(2.0, Math.Floor((num + num2) / 2.0));
				if (proportion / num3 * (proportion / num3) > minRatio / num3 * (maxRatio / num3))
				{
					return true;
				}
				return false;
			}
			return true;
		}
		if (maxRatio > proportion)
		{
			return false;
		}
		return null;
	}

	private static double GetFinalSizeForRange(IReadOnlyList<DefinitionBase> definitions, int start, int count, double spacing)
	{
		double num = 0.0 - spacing;
		int num2 = start + count - 1;
		do
		{
			num += spacing + definitions[num2].SizeCache;
		}
		while (--num2 >= start);
		return num;
	}

	private void SetValid()
	{
		ExtendedData extData = _extData;
		if (extData != null && extData.TempDefinitions != null)
		{
			Array.Clear(extData.TempDefinitions, 0, Math.Max(DefinitionsU.Count, DefinitionsV.Count));
			extData.TempDefinitions = null;
		}
	}

	private GridLinesRenderer? EnsureGridLinesRenderer()
	{
		if (ShowGridLines && _gridLinesRenderer == null)
		{
			_gridLinesRenderer = new GridLinesRenderer();
			base.VisualChildren.Add(_gridLinesRenderer);
		}
		if (!ShowGridLines && _gridLinesRenderer != null)
		{
			base.VisualChildren.Remove(_gridLinesRenderer);
			_gridLinesRenderer = null;
		}
		return _gridLinesRenderer;
	}

	private void SetFlags(bool value, Flags flags)
	{
		_flags = (value ? (_flags | flags) : (_flags & ~flags));
	}

	private bool CheckFlags(Flags flags)
	{
		return _flags.HasAllFlags(flags);
	}

	private static void OnShowGridLinesPropertyChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
	{
		Grid grid = (Grid)d;
		if (grid._extData != null && grid.ListenToNotifications)
		{
			grid.InvalidateArrange();
		}
		grid.SetFlags((bool)e.NewValue, Flags.ShowGridLinesPropertyValue);
	}

	private static void OnSpacingPropertyChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
	{
		Grid grid = (Grid)d;
		if (grid._extData != null && grid.ListenToNotifications)
		{
			grid.CellsStructureDirty = true;
		}
	}

	private static void OnCellAttachedPropertyChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
	{
		if (d is Visual visual)
		{
			Grid visualParent = visual.GetVisualParent<Grid>();
			if (visualParent != null && visualParent._extData != null && visualParent.ListenToNotifications)
			{
				visualParent.CellsStructureDirty = true;
			}
		}
	}

	private static bool CompareNullRefs([NotNullWhen(false)] object? x, [NotNullWhen(false)] object? y, out int result)
	{
		result = 2;
		if (x == null)
		{
			if (y == null)
			{
				result = 0;
			}
			else
			{
				result = -1;
			}
		}
		else if (y == null)
		{
			result = 1;
		}
		return result != 2;
	}

	private static double StarWeight(DefinitionBase def, double scale)
	{
		if (scale < 0.0)
		{
			if (!double.IsPositiveInfinity(def.UserSize.Value))
			{
				return 0.0;
			}
			return 1.0;
		}
		return def.UserSize.Value * scale;
	}
}
