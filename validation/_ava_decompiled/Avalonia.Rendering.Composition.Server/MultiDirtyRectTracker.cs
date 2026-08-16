using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Reactive;

namespace Avalonia.Rendering.Composition.Server;

internal class MultiDirtyRectTracker : IDirtyRectTracker, IDirtyRectCollector
{
	/// <summary>
	/// This is a port of CDirtyRegion2 from WPF
	/// </summary>
	private class CDirtyRegion2(int MaxDirtyRegionCount)
	{
		private readonly struct UnionResult(double overhead, double area, LtrbRect union)
		{
			public readonly double Overhead = overhead;

			public readonly double Area = area;

			public readonly LtrbRect Union = union;
		}

		private readonly LtrbRect[] _dirtyRegions = new LtrbRect[MaxDirtyRegionCount];

		private readonly LtrbRect[] _resolvedRegions = new LtrbRect[MaxDirtyRegionCount];

		private readonly double[,] _overhead = new double[MaxDirtyRegionCount + 1, MaxDirtyRegionCount];

		private LtrbRect _surfaceBounds;

		private double _allowedDirtyRegionOverhead;

		private int _regionCount;

		private bool _optimized;

		private bool _maxSurfaceFallback;

		/// <summary>
		/// Checks if the dirty region is empty.
		/// </summary>
		public bool IsEmpty
		{
			get
			{
				for (int i = 0; i < MaxDirtyRegionCount; i++)
				{
					if (!_dirtyRegions[i].IsEmpty)
					{
						return false;
					}
				}
				return true;
			}
		}

		/// <summary>
		/// Returns the dirty region count.
		/// NOTE: The region count is NOT VALID until GetUninflatedDirtyRegions is called.
		/// </summary>
		public int RegionCount => _regionCount;

		private static double RectArea(LtrbRect r)
		{
			return (r.Right - r.Left) * (r.Bottom - r.Top);
		}

		private static LtrbRect RectUnion(LtrbRect left, LtrbRect right)
		{
			if (left.IsZeroSize)
			{
				return right;
			}
			if (right.IsZeroSize)
			{
				return left;
			}
			return left.Union(right);
		}

		private static UnionResult ComputeUnion(LtrbRect r0, LtrbRect r1)
		{
			LtrbRect ltrbRect = RectUnion(r0, r1);
			LtrbRect r2 = r0.IntersectOrEmpty(r1);
			double num = RectArea(ltrbRect);
			double num2 = num - (RectArea(r0) + RectArea(r1) - RectArea(r2));
			if (!(num2 > 0.0))
			{
				num2 = 0.0;
			}
			return new UnionResult(num2, num, ltrbRect);
		}

		private void SetOverhead(int i, int j, double value)
		{
			if (i > j)
			{
				_overhead[i, j] = value;
			}
			else if (i < j)
			{
				_overhead[j, i] = value;
			}
		}

		private double GetOverhead(int i, int j)
		{
			if (i > j)
			{
				return _overhead[i, j];
			}
			if (i < j)
			{
				return _overhead[j, i];
			}
			return double.MaxValue;
		}

		private void UpdateOverhead(int regionIndex)
		{
			ref LtrbRect reference = ref _dirtyRegions[regionIndex];
			for (int i = 0; i < MaxDirtyRegionCount; i++)
			{
				if (regionIndex != i)
				{
					SetOverhead(i, regionIndex, ComputeUnion(_dirtyRegions[i], reference).Overhead);
				}
			}
		}

		/// <summary>
		/// Initialize must be called before adding dirty rects. Initialize can also be called to
		/// reset the dirty region.
		/// </summary>
		public void Initialize(LtrbRect surfaceBounds, double allowedDirtyRegionOverhead)
		{
			_allowedDirtyRegionOverhead = allowedDirtyRegionOverhead;
			Array.Clear(_dirtyRegions);
			Array.Clear(_overhead);
			_optimized = false;
			_maxSurfaceFallback = false;
			_regionCount = 0;
			_surfaceBounds = surfaceBounds;
		}

		/// <summary>
		/// Adds a new dirty rectangle to the dirty region.
		/// </summary>
		public void Add(LtrbRect newRegion)
		{
			if (_maxSurfaceFallback)
			{
				return;
			}
			if (!newRegion.IsWellOrdered)
			{
				Initialize(_surfaceBounds, _allowedDirtyRegionOverhead);
				_maxSurfaceFallback = true;
				_regionCount = 1;
				return;
			}
			LtrbRect ltrbRect = newRegion.IntersectOrEmpty(_surfaceBounds);
			if (ltrbRect.IsEmpty)
			{
				return;
			}
			ltrbRect = new LtrbRect(Math.Floor(ltrbRect.Left), Math.Floor(ltrbRect.Top), Math.Ceiling(ltrbRect.Right), Math.Ceiling(ltrbRect.Bottom));
			for (int i = 0; i < MaxDirtyRegionCount; i++)
			{
				UnionResult unionResult = ComputeUnion(_dirtyRegions[i], ltrbRect);
				SetOverhead(MaxDirtyRegionCount, i, unionResult.Overhead);
			}
			double num = double.MaxValue;
			int num2 = 0;
			int num3 = 0;
			bool flag = false;
			int num4 = MaxDirtyRegionCount;
			while (true)
			{
				if (num4 > 0)
				{
					for (int j = 0; j < num4; j++)
					{
						double overhead = GetOverhead(num4, j);
						if (num >= overhead)
						{
							num = overhead;
							num2 = num4;
							num3 = j;
							flag = true;
							if (overhead < _allowedDirtyRegionOverhead)
							{
								goto end_IL_0116;
							}
						}
					}
					num4--;
					continue;
				}
				if (flag)
				{
					break;
				}
				return;
				continue;
				end_IL_0116:
				break;
			}
			if (num2 == MaxDirtyRegionCount)
			{
				LtrbRect union = ComputeUnion(ltrbRect, _dirtyRegions[num3]).Union;
				if (!_dirtyRegions[num3].Contains(union))
				{
					_dirtyRegions[num3] = union;
					UpdateOverhead(num3);
				}
			}
			else
			{
				UnionResult unionResult2 = ComputeUnion(_dirtyRegions[num2], _dirtyRegions[num3]);
				_dirtyRegions[num2] = unionResult2.Union;
				_dirtyRegions[num3] = ltrbRect;
				UpdateOverhead(num2);
				UpdateOverhead(num3);
			}
		}

		/// <summary>
		/// Returns an array of dirty rectangles describing the dirty region.
		/// </summary>
		public ReadOnlySpan<LtrbRect> GetUninflatedDirtyRegions()
		{
			if (_maxSurfaceFallback)
			{
				return new ReadOnlySpan<LtrbRect>(in _surfaceBounds);
			}
			if (!_optimized)
			{
				Array.Clear(_resolvedRegions);
				int num = 0;
				for (int i = 0; i < MaxDirtyRegionCount; i++)
				{
					if (!_dirtyRegions[i].IsEmpty)
					{
						if (i != num)
						{
							_dirtyRegions[num] = _dirtyRegions[i];
							UpdateOverhead(num);
						}
						num++;
					}
				}
				bool flag = true;
				while (flag)
				{
					flag = false;
					for (int j = 0; j < num; j++)
					{
						for (int k = j + 1; k < num; k++)
						{
							if (!_dirtyRegions[j].IsEmpty && !_dirtyRegions[k].IsEmpty && GetOverhead(j, k) < _allowedDirtyRegionOverhead)
							{
								UnionResult unionResult = ComputeUnion(_dirtyRegions[j], _dirtyRegions[k]);
								_dirtyRegions[j] = unionResult.Union;
								_dirtyRegions[k] = default(LtrbRect);
								UpdateOverhead(j);
								flag = true;
							}
						}
					}
				}
				int num2 = 0;
				for (int l = 0; l < num; l++)
				{
					if (!_dirtyRegions[l].IsEmpty)
					{
						_resolvedRegions[num2] = _dirtyRegions[l];
						num2++;
					}
				}
				_regionCount = num2;
				_optimized = true;
			}
			return _resolvedRegions.AsSpan(0, _regionCount);
		}
	}

	private readonly double _maxOverhead;

	private readonly CDirtyRegion2 _regions;

	private readonly IPlatformRenderInterfaceRegion _clipRegion;

	private readonly List<LtrbRect> _inflatedRects = new List<LtrbRect>();

	private Random _random = new Random();

	public bool IsEmpty => _regions.IsEmpty;

	public LtrbRect CombinedRect { get; private set; }

	public IReadOnlyList<LtrbRect> InflatedRects => _inflatedRects;

	public MultiDirtyRectTracker(IPlatformRenderInterface platformRender, int maxDirtyRects, double maxOverhead)
	{
		_maxOverhead = maxOverhead;
		_regions = new CDirtyRegion2(maxDirtyRects);
		_clipRegion = platformRender.CreateRegion();
	}

	public void AddRect(LtrbRect rect)
	{
		_regions.Add(rect);
	}

	public void FinalizeFrame(LtrbRect bounds)
	{
		_inflatedRects.Clear();
		_clipRegion.Reset();
		ReadOnlySpan<LtrbRect> uninflatedDirtyRegions = _regions.GetUninflatedDirtyRegions();
		LtrbRect? left = null;
		ReadOnlySpan<LtrbRect> readOnlySpan = uninflatedDirtyRegions;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			LtrbRect ltrbRect = readOnlySpan[i].Inflate(new Thickness(1.0)).IntersectOrEmpty(bounds);
			_inflatedRects.Add(ltrbRect);
			_clipRegion.AddRect(LtrbPixelRect.FromRectUnscaled(ltrbRect));
			left = LtrbRect.FullUnion(left, ltrbRect);
		}
		CombinedRect = left.GetValueOrDefault();
	}

	public IDisposable BeginDraw(IDrawingContextImpl ctx)
	{
		ctx.PushClip(_clipRegion);
		return Disposable.Create(ctx.PopClip);
	}

	public bool Intersects(LtrbRect rect)
	{
		foreach (LtrbRect inflatedRect in _inflatedRects)
		{
			if (inflatedRect.Intersects(rect))
			{
				return true;
			}
		}
		return false;
	}

	public void Initialize(LtrbRect bounds)
	{
		_regions.Initialize(bounds, _maxOverhead);
		_inflatedRects.Clear();
		_clipRegion.Reset();
		CombinedRect = default(LtrbRect);
	}

	public void Visualize(IDrawingContextImpl context)
	{
		context.DrawRegion(new ImmutableSolidColorBrush(new Color(150, (byte)_random.Next(255), (byte)_random.Next(255), (byte)_random.Next(255))), null, _clipRegion);
	}
}
