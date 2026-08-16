using System;
using System.Buffers;
using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Caches shaped text runs and bidi processing results to avoid redundant shaping
/// when only the paragraph width constraint changes (e.g., between Measure and Arrange).
/// </summary>
/// <remarks>
/// Uses an inline single-entry store for the common case of a single paragraph,
/// and only promotes to a dictionary when multiple entries are added.
/// </remarks>
[Unstable("This API is in preview and subject to change without deprecation.")]
public class TextRunCache : IDisposable
{
	private bool _hasSingleEntry;

	private int _singleKey;

	private CachedShapingResult _singleValue;

	private Dictionary<int, CachedShapingResult>? _entries;

	/// <summary>
	/// Invalidates all cached entries and disposes their shaped buffers.
	/// </summary>
	public void Invalidate()
	{
		if (_hasSingleEntry)
		{
			DisposeCachedRuns(_singleValue);
			_hasSingleEntry = false;
			_singleValue = default(CachedShapingResult);
		}
		else
		{
			if (_entries == null)
			{
				return;
			}
			foreach (CachedShapingResult value in _entries.Values)
			{
				DisposeCachedRuns(value);
			}
			_entries.Clear();
		}
	}

	/// <summary>
	/// Invalidates all cached entries at or after the specified text source index.
	/// </summary>
	/// <param name="textSourceIndex">The text source index from which to invalidate.</param>
	public void InvalidateFrom(int textSourceIndex)
	{
		if (_hasSingleEntry)
		{
			if (_singleKey >= textSourceIndex)
			{
				DisposeCachedRuns(_singleValue);
				_hasSingleEntry = false;
				_singleValue = default(CachedShapingResult);
			}
		}
		else
		{
			if (_entries == null || _entries.Count == 0)
			{
				return;
			}
			int count = _entries.Count;
			int[] array = null;
			Span<int> span = ((count > 16) ? ((Span<int>)(array = ArrayPool<int>.Shared.Rent(count))) : stackalloc int[16]);
			Span<int> span2 = span;
			int num = 0;
			foreach (int key in _entries.Keys)
			{
				if (key >= textSourceIndex)
				{
					span2[num++] = key;
				}
			}
			for (int i = 0; i < num; i++)
			{
				if (_entries.Remove(span2[i], out var value))
				{
					DisposeCachedRuns(value);
				}
			}
			if (array != null)
			{
				ArrayPool<int>.Shared.Return(array);
			}
		}
	}

	/// <summary>
	/// Tries to retrieve cached shaped runs for the given text source index.
	/// </summary>
	internal bool TryGetShapedRuns(int firstTextSourceIndex, out CachedShapingResult result)
	{
		if (_hasSingleEntry && _singleKey == firstTextSourceIndex)
		{
			result = _singleValue;
			return true;
		}
		if (_entries != null && _entries.TryGetValue(firstTextSourceIndex, out result))
		{
			return true;
		}
		result = default(CachedShapingResult);
		return false;
	}

	/// <summary>
	/// Adds shaped runs to the cache for the given text source index. The cache takes
	/// its own reference to each <see cref="T:Avalonia.Media.TextFormatting.ShapedTextRun" />; the caller retains its
	/// original references unchanged.
	/// </summary>
	internal void Add(int firstTextSourceIndex, CachedShapingResult result)
	{
		AddRefShapedRuns(result.ShapedRuns);
		if (_entries != null)
		{
			if (_entries.TryGetValue(firstTextSourceIndex, out var value))
			{
				DisposeCachedRuns(value);
			}
			_entries[firstTextSourceIndex] = result;
		}
		else if (!_hasSingleEntry)
		{
			_singleKey = firstTextSourceIndex;
			_singleValue = result;
			_hasSingleEntry = true;
		}
		else if (_singleKey == firstTextSourceIndex)
		{
			DisposeCachedRuns(_singleValue);
			_singleValue = result;
		}
		else
		{
			_entries = new Dictionary<int, CachedShapingResult>
			{
				{ _singleKey, _singleValue },
				{ firstTextSourceIndex, result }
			};
			_hasSingleEntry = false;
			_singleValue = default(CachedShapingResult);
		}
	}

	private static void AddRefShapedRuns(TextRun[] runs)
	{
		for (int i = 0; i < runs.Length; i++)
		{
			if (runs[i] is ShapedTextRun shapedTextRun)
			{
				shapedTextRun.AddRef();
			}
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		Invalidate();
		_entries = null;
	}

	private static void DisposeCachedRuns(CachedShapingResult result)
	{
		TextRun[] shapedRuns = result.ShapedRuns;
		for (int i = 0; i < shapedRuns.Length; i++)
		{
			if (shapedRuns[i] is ShapedTextRun shapedTextRun)
			{
				shapedTextRun.Dispose();
			}
		}
	}
}
