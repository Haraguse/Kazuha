using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting;

public sealed class ShapedBuffer : IReadOnlyList<GlyphInfo>, IEnumerable<GlyphInfo>, IEnumerable, IReadOnlyCollection<GlyphInfo>, IDisposable
{
	/// <summary>
	/// Disposable wrapper around an <see cref="T:System.Buffers.ArrayPool`1" />-rented array.
	/// Combined with <see cref="T:Avalonia.Utilities.IRef`1" /> this gives <see cref="T:Avalonia.Media.TextFormatting.ShapedBuffer" />
	/// shared, ref-counted ownership of its glyph and cluster-cache storage so
	/// that <see cref="M:Avalonia.Media.TextFormatting.ShapedBuffer.Split(System.Int32)" /> and <see cref="M:Avalonia.Media.TextFormatting.ShapedBuffer.WithBidiLevel(System.SByte)" /> can safely alias
	/// the same backing arrays.
	/// </summary>
	internal sealed class PooledArray<T> : IDisposable
	{
		private T[]? _array;

		private int _generation;

		public T[] Array => _array ?? throw new ObjectDisposedException("PooledArray");

		/// <summary>
		/// Monotonically increasing version stamp. Bumped whenever the underlying
		/// data is mutated so siblings sharing this holder can detect that any
		/// cache derived from the data is stale and must be rebuilt.
		/// </summary>
		public int Generation => Volatile.Read(in _generation);

		public PooledArray(int minLength)
		{
			_array = ArrayPool<T>.Shared.Rent(minLength);
		}

		public void BumpGeneration()
		{
			Interlocked.Increment(ref _generation);
		}

		public void Dispose()
		{
			T[] array = Interlocked.Exchange(ref _array, null);
			if (array != null)
			{
				ArrayPool<T>.Shared.Return(array);
			}
		}
	}

	private IRef<PooledArray<GlyphInfo>>? _glyphRef;

	private ArraySlice<GlyphInfo> _glyphInfos;

	private double[]? _clusterPrefix;

	private int[]? _clusterStartChars;

	private IRef<PooledArray<double>>? _prefixRef;

	private IRef<PooledArray<int>>? _startsRef;

	private int _clusterStartIdx;

	private int _clusterCount;

	private int _cacheGeneration;

	private bool _disposed;

	private static readonly int[] s_emptyStartChars = new int[1];

	private static readonly double[] s_emptyPrefix = new double[1];

	/// <summary>
	/// The buffer's length.
	/// </summary>
	public int Length => _glyphInfos.Length;

	/// <summary>
	/// The buffer's glyph infos.
	/// </summary>
	internal ArraySlice<GlyphInfo> GlyphInfos => _glyphInfos;

	/// <summary>
	/// The buffer's glyph typeface.
	/// </summary>
	public GlyphTypeface GlyphTypeface { get; }

	/// <summary>
	/// The buffers font rendering em size.
	/// </summary>
	public double FontRenderingEmSize { get; }

	/// <summary>
	/// The buffer's bidi level.
	/// </summary>
	public sbyte BidiLevel { get; }

	/// <summary>
	/// The buffer's reading direction.
	/// </summary>
	public bool IsLeftToRight => (BidiLevel & 1) == 0;

	/// <summary>
	/// The text that is represended by this buffer.
	/// </summary>
	public ReadOnlyMemory<char> Text { get; }

	public GlyphInfo this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return _glyphInfos[index];
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set
		{
			_glyphInfos[index] = value;
			_glyphRef?.Item.BumpGeneration();
			InvalidateClusterCache();
		}
	}

	/// <summary>
	/// Test hook: indicates that the cluster cache is using the
	/// one-char-per-cluster fast path (no <c>_clusterStartChars</c> allocation).
	/// Materialises the cache as a side effect, so call after the buffer is
	/// fully populated.
	/// </summary>
	internal bool IsClusterCacheSimple
	{
		get
		{
			EnsureClusterCache();
			return _clusterStartChars == null;
		}
	}

	/// <summary>
	/// Test hook: exposes the backing cluster-prefix array reference so unit
	/// tests can assert that <see cref="M:Avalonia.Media.TextFormatting.ShapedBuffer.Split(System.Int32)" /> / <see cref="M:Avalonia.Media.TextFormatting.ShapedBuffer.WithBidiLevel(System.SByte)" />
	/// aliases share the parent's cache rather than rebuilding their own.
	/// Returns null when no cache has been built yet.
	/// </summary>
	internal double[]? ClusterPrefix => _clusterPrefix;

	/// <summary>
	/// Returns the total advance of all glyphs in the buffer (i.e. the buffer's
	/// rendered width). Cached after first access; the value is summed in
	/// logical cluster order, which can differ by ULPs from a visual-order sum
	/// on RTL buffers — fine for layout but tests should use FP-tolerant equality.
	/// </summary>
	internal double TotalGlyphAdvance
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			double[] array = EnsureClusterCache();
			int clusterStartIdx = _clusterStartIdx;
			return array[clusterStartIdx + _clusterCount] - array[clusterStartIdx];
		}
	}

	/// <summary>
	/// Returns the character length of the first logical cluster in this buffer.
	/// Used by <c>MeasureLength</c> to satisfy the "include at least one cluster"
	/// rule when even the first cluster does not fit the paragraph width.
	/// </summary>
	internal int FirstClusterCharLength
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			EnsureClusterCache();
			if (_clusterCount == 0)
			{
				return 0;
			}
			int[] clusterStartChars = _clusterStartChars;
			if (clusterStartChars == null)
			{
				return 1;
			}
			int clusterStartIdx = _clusterStartIdx;
			return clusterStartChars[clusterStartIdx + 1] - clusterStartChars[clusterStartIdx];
		}
	}

	int IReadOnlyCollection<GlyphInfo>.Count => _glyphInfos.Length;

	public ShapedBuffer(ReadOnlyMemory<char> text, int bufferLength, GlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel)
	{
		Text = text;
		_glyphRef = RefCountable.Create(new PooledArray<GlyphInfo>(bufferLength));
		_glyphInfos = new ArraySlice<GlyphInfo>(_glyphRef.Item.Array, 0, bufferLength);
		GlyphTypeface = glyphTypeface;
		FontRenderingEmSize = fontRenderingEmSize;
		BidiLevel = bidiLevel;
	}

	internal ShapedBuffer(ReadOnlyMemory<char> text, ArraySlice<GlyphInfo> glyphInfos, GlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel)
	{
		Text = text;
		_glyphInfos = glyphInfos;
		GlyphTypeface = glyphTypeface;
		FontRenderingEmSize = fontRenderingEmSize;
		BidiLevel = bidiLevel;
	}

	/// <summary>
	/// Internal constructor used by <see cref="M:Avalonia.Media.TextFormatting.ShapedBuffer.Split(System.Int32)" /> and <see cref="M:Avalonia.Media.TextFormatting.ShapedBuffer.WithBidiLevel(System.SByte)" /> to
	/// alias the source buffer's pooled glyph storage and (optionally) cluster cache.
	/// Each non-null ref is <see cref="M:Avalonia.Utilities.IRef`1.Clone" />d so the underlying arrays survive
	/// until every sibling has been disposed. When <paramref name="sourcePrefixRef" />
	/// is null the alias starts without a cluster cache and will build its own lazily.
	/// </summary>
	private ShapedBuffer(ReadOnlyMemory<char> text, ArraySlice<GlyphInfo> glyphInfos, GlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel, IRef<PooledArray<GlyphInfo>>? sourceGlyphRef, IRef<PooledArray<double>>? sourcePrefixRef, IRef<PooledArray<int>>? sourceStartsRef, int clusterStartIdx, int clusterCount, int sourceCacheGeneration)
	{
		Text = text;
		_glyphInfos = glyphInfos;
		GlyphTypeface = glyphTypeface;
		FontRenderingEmSize = fontRenderingEmSize;
		BidiLevel = bidiLevel;
		_glyphRef = sourceGlyphRef?.Clone();
		if (sourcePrefixRef != null)
		{
			_prefixRef = sourcePrefixRef.Clone();
			_clusterPrefix = _prefixRef.Item.Array;
			if (sourceStartsRef != null)
			{
				_startsRef = sourceStartsRef.Clone();
				_clusterStartChars = _startsRef.Item.Array;
			}
			_clusterStartIdx = clusterStartIdx;
			_clusterCount = clusterCount;
			_cacheGeneration = sourceCacheGeneration;
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_glyphRef?.Dispose();
			_glyphRef = null;
			_glyphInfos = ArraySlice<GlyphInfo>.Empty;
			ReleaseClusterCacheRefs();
			_clusterPrefix = null;
			_clusterStartChars = null;
			_clusterStartIdx = 0;
			_clusterCount = 0;
		}
	}

	/// <summary>
	/// Releases this buffer's ref-counted handles to the cluster-cache arrays
	/// (if any). The underlying arrays are only returned to the pool when the
	/// last sibling releases its handle, so this is safe to call from both
	/// <see cref="M:Avalonia.Media.TextFormatting.ShapedBuffer.Dispose" /> and <see cref="M:Avalonia.Media.TextFormatting.ShapedBuffer.InvalidateClusterCache" />.
	/// </summary>
	private void ReleaseClusterCacheRefs()
	{
		_prefixRef?.Dispose();
		_prefixRef = null;
		_startsRef?.Dispose();
		_startsRef = null;
	}

	/// <summary>
	/// Ensures the cluster cache is built in <em>logical</em> order. LTR buffers store glyphs
	/// in ascending cluster order so logical order is the same as visual order
	/// (walk forward from index 0). RTL buffers store glyphs in descending cluster
	/// order — visual order is reverse-logical — so logical order means walking
	/// the underlying array backwards. The output `prefix` and `startChars` are
	/// always in logical order: `startChars[0] == 0`, `startChars[count] == Text.Length`,
	/// and `prefix[count]` equals the total advance.
	/// </summary>
	private double[] EnsureClusterCache()
	{
		int num = _glyphRef?.Item.Generation ?? 0;
		if (_clusterPrefix != null)
		{
			if (_cacheGeneration == num)
			{
				return _clusterPrefix;
			}
			InvalidateClusterCache();
		}
		Span<GlyphInfo> span = _glyphInfos.Span;
		int length = _glyphInfos.Length;
		if (length == 0)
		{
			_clusterCount = 0;
			_clusterStartChars = s_emptyStartChars;
			_cacheGeneration = num;
			return _clusterPrefix = s_emptyPrefix;
		}
		bool isLeftToRight = IsLeftToRight;
		int num2 = (isLeftToRight ? 1 : (-1));
		int num3 = ((!isLeftToRight) ? (length - 1) : 0);
		int num4 = (isLeftToRight ? length : (-1));
		int glyphCluster = span[num3].GlyphCluster;
		int length2 = Text.Length;
		int num5 = 1;
		bool flag = length == length2;
		int num6 = span[num3].GlyphCluster;
		int num7 = 1;
		int num8 = num3 + num2;
		while (num8 != num4)
		{
			int glyphCluster2 = span[num8].GlyphCluster;
			if (glyphCluster2 != num6)
			{
				num5++;
				num6 = glyphCluster2;
			}
			if (flag && glyphCluster2 - glyphCluster != num7)
			{
				flag = false;
			}
			num8 += num2;
			num7++;
		}
		bool flag2 = flag && num5 == length;
		PooledArray<double> pooledArray = new PooledArray<double>(num5 + 1);
		_prefixRef = RefCountable.Create(pooledArray);
		double[] array = pooledArray.Array;
		int[] array2 = null;
		if (!flag2)
		{
			PooledArray<int> pooledArray2 = new PooledArray<int>(num5 + 1);
			_startsRef = RefCountable.Create(pooledArray2);
			array2 = pooledArray2.Array;
		}
		array[0] = 0.0;
		int num9 = 0;
		int num10 = glyphCluster;
		double num11 = 0.0;
		if (!flag2)
		{
			array2[0] = 0;
		}
		for (int i = num3; i != num4; i += num2)
		{
			GlyphInfo glyphInfo = span[i];
			if (glyphInfo.GlyphCluster != num10)
			{
				array[num9 + 1] = array[num9] + num11;
				if (!flag2)
				{
					array2[num9 + 1] = glyphInfo.GlyphCluster - glyphCluster;
				}
				num9++;
				num10 = glyphInfo.GlyphCluster;
				num11 = glyphInfo.GlyphAdvance;
			}
			else
			{
				num11 += glyphInfo.GlyphAdvance;
			}
		}
		array[num9 + 1] = array[num9] + num11;
		if (!flag2)
		{
			array2[num9 + 1] = length2;
		}
		_clusterCount = num5;
		_clusterStartChars = array2;
		_clusterPrefix = array;
		_cacheGeneration = num;
		return array;
	}

	/// <summary>
	/// Returns the cluster index (relative to this sub-buffer's cache view) at
	/// which a logical text-character offset lands. Split methods snap their
	/// boundaries to whole clusters, so this is an exact match in the cluster
	/// starts table.
	/// </summary>
	private int FindClusterOffsetForSplit(int splitCharCount)
	{
		int[] clusterStartChars = _clusterStartChars;
		if (clusterStartChars == null)
		{
			return splitCharCount;
		}
		int clusterStartIdx = _clusterStartIdx;
		int clusterCount = _clusterCount;
		int num = clusterStartChars[clusterStartIdx] + splitCharCount;
		int num2 = 0;
		int num3 = clusterCount;
		while (num2 < num3)
		{
			int num4 = num2 + num3 >> 1;
			if (clusterStartChars[clusterStartIdx + num4] < num)
			{
				num2 = num4 + 1;
			}
			else
			{
				num3 = num4;
			}
		}
		return num2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void InvalidateClusterCache()
	{
		ReleaseClusterCacheRefs();
		_clusterPrefix = null;
		_clusterStartChars = null;
		_clusterStartIdx = 0;
		_clusterCount = 0;
	}

	public IEnumerator<GlyphInfo> GetEnumerator()
	{
		return _glyphInfos.GetEnumerator();
	}

	/// <summary>
	/// Creates a view of this buffer that reports the given <paramref name="paragraphEmbeddingLevel" />
	/// as its <see cref="P:Avalonia.Media.TextFormatting.ShapedBuffer.BidiLevel" /> while sharing the same underlying glyphs.
	/// Used for trailing-whitespace handling where the visual direction follows the paragraph.
	/// </summary>
	internal ShapedBuffer WithBidiLevel(sbyte paragraphEmbeddingLevel)
	{
		if (BidiLevel == paragraphEmbeddingLevel)
		{
			return this;
		}
		IRef<PooledArray<double>> sourcePrefixRef = null;
		IRef<PooledArray<int>> sourceStartsRef = null;
		int clusterStartIdx = 0;
		int clusterCount = 0;
		if (_prefixRef != null)
		{
			sourcePrefixRef = _prefixRef;
			sourceStartsRef = _startsRef;
			clusterStartIdx = _clusterStartIdx;
			clusterCount = _clusterCount;
		}
		return new ShapedBuffer(Text, _glyphInfos, GlyphTypeface, FontRenderingEmSize, paragraphEmbeddingLevel, _glyphRef, sourcePrefixRef, sourceStartsRef, clusterStartIdx, clusterCount, _cacheGeneration);
	}

	/// <summary>
	/// Creates a deep copy backed by a fresh, non-pooled <see cref="T:Avalonia.Media.TextFormatting.GlyphInfo" /> array so the
	/// copy's advances can be mutated (e.g. by justification) without touching glyph storage
	/// shared with a <see cref="T:Avalonia.Media.TextFormatting.TextRunCache" /> entry, a <see cref="M:Avalonia.Media.TextFormatting.ShapedBuffer.Split(System.Int32)" /> sibling or a
	/// <see cref="M:Avalonia.Media.TextFormatting.ShapedBuffer.WithBidiLevel(System.SByte)" /> alias. The copy owns no ref-counted pooled handles (its
	/// <c>_glyphRef</c> is null) and builds its own cluster cache lazily.
	/// </summary>
	internal ShapedBuffer CloneWritable()
	{
		Span<GlyphInfo> span = _glyphInfos.Span;
		GlyphInfo[] array = new GlyphInfo[span.Length];
		span.CopyTo(array);
		return new ShapedBuffer(Text, new ArraySlice<GlyphInfo>(array), GlyphTypeface, FontRenderingEmSize, BidiLevel);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	/// <summary>
	/// Splits the <see cref="T:Avalonia.Media.TextFormatting.TextRun" /> at specified length.
	/// </summary>
	/// <param name="textLength">The text length.</param>
	/// <returns>The split result.</returns>
	public SplitResult<ShapedBuffer> Split(int textLength)
	{
		textLength = Math.Min(Text.Length, textLength);
		if (textLength <= 0)
		{
			return new SplitResult<ShapedBuffer>(new ShapedBuffer(Text.Slice(0, 0), _glyphInfos.Slice(_glyphInfos.Start, 0), GlyphTypeface, FontRenderingEmSize, BidiLevel), this);
		}
		if (textLength == Text.Length)
		{
			return new SplitResult<ShapedBuffer>(this, null);
		}
		if (!IsLeftToRight)
		{
			return SplitDescending(textLength);
		}
		return SplitAscending(textLength);
	}

	/// <summary>
	/// Split a buffer whose glyphs are ordered by ascending cluster (LTR visual / logical order).
	/// </summary>
	private SplitResult<ShapedBuffer> SplitAscending(int textLength)
	{
		int start = _glyphInfos.Start;
		Span<GlyphInfo> span = _glyphInfos.Span;
		int length = _glyphInfos.Length;
		int glyphCluster = span[0].GlyphCluster;
		int num = glyphCluster + textLength;
		GlyphInfo value = new GlyphInfo(0, num, 0.0);
		int num2 = ((ReadOnlySpan<GlyphInfo>)span).BinarySearch(value, GlyphInfo.ClusterAscendingComparer);
		int num4;
		int num5;
		if (num2 >= 0)
		{
			int num3 = num2;
			while (num3 > 0 && span[num3 - 1].GlyphCluster == num)
			{
				num3--;
			}
			num4 = num3;
			num5 = num - glyphCluster;
		}
		else
		{
			int num6 = ~num2;
			if (num6 >= length)
			{
				num4 = length;
				num5 = Text.Length;
			}
			else
			{
				num4 = num6;
				num5 = span[num6].GlyphCluster - glyphCluster;
			}
		}
		ArraySlice<GlyphInfo> glyphInfos = _glyphInfos.Slice(start, num4);
		ArraySlice<GlyphInfo> glyphInfos2 = _glyphInfos.Slice(start + num4, length - num4);
		ReadOnlyMemory<char> text = Text.Slice(0, num5);
		ReadOnlyMemory<char> text2 = Text.Slice(num5);
		EnsureClusterCache();
		int num7 = FindClusterOffsetForSplit(num5);
		ShapedBuffer first = new ShapedBuffer(text, glyphInfos, GlyphTypeface, FontRenderingEmSize, BidiLevel, _glyphRef, _prefixRef, _startsRef, _clusterStartIdx, num7, _cacheGeneration);
		if (text2.Length == 0)
		{
			return new SplitResult<ShapedBuffer>(first, null);
		}
		ShapedBuffer second = new ShapedBuffer(text2, glyphInfos2, GlyphTypeface, FontRenderingEmSize, BidiLevel, _glyphRef, _prefixRef, _startsRef, _clusterStartIdx + num7, _clusterCount - num7, _cacheGeneration);
		return new SplitResult<ShapedBuffer>(first, second);
	}

	/// <summary>
	/// Split a buffer whose glyphs are ordered by descending cluster (RTL visual order).
	/// The leading <see cref="T:Avalonia.Media.TextFormatting.ShapedBuffer" /> corresponds to text[0..textLength], whose glyphs
	/// live at the <em>tail</em> of the visual array.
	/// </summary>
	private SplitResult<ShapedBuffer> SplitDescending(int textLength)
	{
		int start = _glyphInfos.Start;
		Span<GlyphInfo> span = _glyphInfos.Span;
		int length = _glyphInfos.Length;
		int num = span[length - 1].GlyphCluster + textLength;
		GlyphInfo value = new GlyphInfo(0, num, 0.0);
		int i = ((ReadOnlySpan<GlyphInfo>)span).BinarySearch(value, GlyphInfo.ClusterDescendingComparer);
		int num2;
		if (i >= 0)
		{
			for (; i + 1 < length && span[i + 1].GlyphCluster == num; i++)
			{
			}
			num2 = i + 1;
		}
		else
		{
			num2 = ~i;
		}
		ArraySlice<GlyphInfo> glyphInfos = _glyphInfos.Slice(start, num2);
		ArraySlice<GlyphInfo> glyphInfos2 = _glyphInfos.Slice(start + num2, length - num2);
		ReadOnlyMemory<char> text = Text.Slice(0, textLength);
		ReadOnlyMemory<char> text2 = Text.Slice(textLength);
		EnsureClusterCache();
		int num3 = FindClusterOffsetForSplit(textLength);
		ShapedBuffer first = new ShapedBuffer(text, glyphInfos2, GlyphTypeface, FontRenderingEmSize, BidiLevel, _glyphRef, _prefixRef, _startsRef, _clusterStartIdx, num3, _cacheGeneration);
		if (text2.Length == 0 || glyphInfos.Length == 0)
		{
			return new SplitResult<ShapedBuffer>(first, null);
		}
		ShapedBuffer second = new ShapedBuffer(text2, glyphInfos, GlyphTypeface, FontRenderingEmSize, BidiLevel, _glyphRef, _prefixRef, _startsRef, _clusterStartIdx + num3, _clusterCount - num3, _cacheGeneration);
		return new SplitResult<ShapedBuffer>(first, second);
	}

	/// <summary>
	/// Returns the cumulative glyph advance for the logical character range
	/// <c>[<paramref name="startChar" />, <paramref name="endChar" />)</c>
	/// within this sub-buffer. Uses the cluster cache via binary search, so
	/// each call is O(log clusters) regardless of how big the buffer is or
	/// where the range sits inside it.
	/// </summary>
	/// <remarks>
	/// The cluster cache is built in <i>logical</i> order for both LTR and
	/// RTL buffers (see <see cref="M:Avalonia.Media.TextFormatting.ShapedBuffer.EnsureClusterCache" />), so callers pass
	/// logical char offsets and the same code path serves both directions.
	/// Out-of-range arguments are clamped to <c>[0, Text.Length]</c>.
	/// </remarks>
	internal double GetCharRangeWidth(int startChar, int endChar)
	{
		if (endChar <= startChar)
		{
			return 0.0;
		}
		double[] array = EnsureClusterCache();
		int clusterStartIdx = _clusterStartIdx;
		int clusterCount = _clusterCount;
		int[] clusterStartChars = _clusterStartChars;
		int num;
		int num2;
		if (clusterStartChars == null)
		{
			num = Math.Clamp(startChar, 0, clusterCount);
			num2 = Math.Clamp(endChar, 0, clusterCount);
		}
		else
		{
			int baseChar = clusterStartChars[clusterStartIdx];
			num = FindLargestClusterAtOrBefore(clusterStartChars, clusterStartIdx, clusterCount, baseChar, startChar);
			num2 = FindLargestClusterAtOrBefore(clusterStartChars, clusterStartIdx, clusterCount, baseChar, endChar);
		}
		return array[clusterStartIdx + num2] - array[clusterStartIdx + num];
	}

	/// <summary>
	/// Binary-search the largest cluster boundary index <c>i ∈ [0, count]</c>
	/// such that <c>starts[startIdx + i] - baseChar ≤ charPos</c>. Cluster
	/// starts are non-decreasing within the sub-buffer range, so a standard
	/// upper-bound search works in both LTR and RTL buffers (the cache is
	/// always built in logical order).
	/// </summary>
	private static int FindLargestClusterAtOrBefore(int[] starts, int startIdx, int count, int baseChar, int charPos)
	{
		if (charPos < 0)
		{
			return 0;
		}
		int num = 0;
		int num2 = count;
		while (num < num2)
		{
			int num3 = num + num2 + 1 >> 1;
			if (starts[startIdx + num3] - baseChar <= charPos)
			{
				num = num3;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		return num;
	}

	/// <summary>
	/// Finds the largest <c>N</c> such that the first <c>N</c> logical
	/// characters of this sub-buffer fit within <paramref name="availableWidth" />.
	/// Cluster-atomic: a multi-glyph cluster either fits completely or not at
	/// all. Returns 0 if <paramref name="availableWidth" /> is non-positive
	/// or the first cluster's width already exceeds it.
	/// </summary>
	/// <remarks>
	/// Walks the cluster cache (built in logical order for both LTR and RTL
	/// buffers) via binary search, so each call is O(log clusters) and the
	/// returned count is the correct logical-leading char count regardless
	/// of the buffer's visual direction.
	/// </remarks>
	internal int FindLeadingCharCountWithinWidth(double availableWidth)
	{
		if (availableWidth <= 0.0)
		{
			return 0;
		}
		double[] array = EnsureClusterCache();
		int clusterStartIdx = _clusterStartIdx;
		int clusterCount = _clusterCount;
		double num = array[clusterStartIdx];
		int num2 = 0;
		int num3 = clusterCount;
		while (num2 < num3)
		{
			int num4 = num2 + num3 + 1 >> 1;
			if (MathUtilities.LessThanOrClose(array[clusterStartIdx + num4] - num, availableWidth))
			{
				num2 = num4;
			}
			else
			{
				num3 = num4 - 1;
			}
		}
		int[] clusterStartChars = _clusterStartChars;
		if (clusterStartChars == null)
		{
			return num2;
		}
		return clusterStartChars[clusterStartIdx + num2] - clusterStartChars[clusterStartIdx];
	}

	/// <summary>
	/// Finds the largest <c>N</c> such that the last <c>N</c> logical
	/// characters of this sub-buffer fit within <paramref name="availableWidth" />.
	/// Cluster-atomic; <paramref name="consumedWidth" /> reports the actual
	/// cumulative advance of those <c>N</c> chars.
	/// </summary>
	/// <remarks>
	/// O(log clusters) via the cluster cache; direction-agnostic (cache is
	/// always in logical order). The returned count is the logical-trailing
	/// char count regardless of whether the buffer is LTR or RTL.
	/// </remarks>
	internal int FindTrailingCharCountWithinWidth(double availableWidth, out double consumedWidth)
	{
		consumedWidth = 0.0;
		if (availableWidth <= 0.0)
		{
			return 0;
		}
		double[] array = EnsureClusterCache();
		int clusterStartIdx = _clusterStartIdx;
		int clusterCount = _clusterCount;
		double num = array[clusterStartIdx + clusterCount];
		int num2 = 0;
		int num3 = clusterCount;
		while (num2 < num3)
		{
			int num4 = num2 + num3 >> 1;
			if (MathUtilities.LessThanOrClose(num - array[clusterStartIdx + num4], availableWidth))
			{
				num3 = num4;
			}
			else
			{
				num2 = num4 + 1;
			}
		}
		consumedWidth = num - array[clusterStartIdx + num2];
		int[] clusterStartChars = _clusterStartChars;
		if (clusterStartChars == null)
		{
			return clusterCount - num2;
		}
		return clusterStartChars[clusterStartIdx + clusterCount] - clusterStartChars[clusterStartIdx + num2];
	}
}
