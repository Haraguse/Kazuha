using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// <para>Contains various list pools that are commonly used during text layout.</para>
/// <para>
/// This class provides an instance per thread.
/// In most applications, there'll be only one instance: on the UI thread, which is responsible for layout.
/// </para>
/// </summary>
/// <seealso cref="T:Avalonia.Media.TextFormatting.FormattingObjectPool.RentedList`1" />
internal sealed class FormattingObjectPool
{
	internal sealed class ListPool<T>
	{
		private const int MaxSize = 16;

		private readonly RentedList<T>[] _lists = new RentedList<T>[16];

		private int _size;

		private int _pendingReturnCount;

		/// <summary>
		/// Rents a list.
		/// See <see cref="T:Avalonia.Media.TextFormatting.FormattingObjectPool.RentedList`1" /> for the intended usages.
		/// </summary>
		/// <returns>A rented list instance that must be returned to the pool.</returns>
		/// <seealso cref="T:Avalonia.Media.TextFormatting.FormattingObjectPool.RentedList`1" />
		public RentedList<T> Rent()
		{
			RentedList<T> result = ((_size > 0) ? _lists[--_size] : new RentedList<T>());
			_pendingReturnCount++;
			return result;
		}

		/// <summary>
		/// Returns a rented list to the pool.
		/// </summary>
		/// <param name="rentedList">
		/// On input, the list to return.
		/// On output, the reference is set to null to avoid misuse.
		/// </param>
		public void Return(ref RentedList<T>? rentedList)
		{
			if (rentedList != null)
			{
				_pendingReturnCount--;
				FormattingBufferHelper.ClearThenResetIfTooLarge(rentedList);
				if (_size < 16)
				{
					_lists[_size++] = rentedList;
				}
				rentedList = null;
			}
		}

		[Conditional("DEBUG")]
		public void VerifyAllReturned()
		{
			int pendingReturnCount = _pendingReturnCount;
			_pendingReturnCount = 0;
			if (pendingReturnCount > 0)
			{
				throw new InvalidOperationException($"{pendingReturnCount} RentedList<{typeof(T).Name}> haven't been returned to the pool!");
			}
			if (pendingReturnCount < 0)
			{
				throw new InvalidOperationException($"{-pendingReturnCount} RentedList<{typeof(T).Name}> extra lists have been returned to the pool!");
			}
		}
	}

	/// <summary>
	/// <para>Represents a list that has been rented through <see cref="T:Avalonia.Media.TextFormatting.FormattingObjectPool" />.</para>
	/// <para>
	/// This class can be used when a temporary list is needed to store some items during text layout.
	/// It can also be used as a reusable array builder by calling <see cref="M:System.Collections.Generic.List`1.ToArray" /> when done.
	/// </para>
	/// <list type="bullet">
	///   <item>NEVER use an instance of this type after it's been returned to the pool.</item>
	///   <item>AVOID storing an instance of this type into a field or property.</item>
	///   <item>AVOID casting an instance of this type to another type.</item>
	///   <item>
	///     AVOID passing an instance of this type as an argument to a method expecting a standard list,
	///     unless you're absolutely sure it won't store it.
	///   </item>
	///   <item>
	///     If you call a method returning an instance of this type,
	///     you're now responsible for returning it to the pool.
	///   </item>
	/// </list>
	/// </summary>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	internal sealed class RentedList<T> : List<T>
	{
	}

	[ThreadStatic]
	private static FormattingObjectPool? t_instance;

	/// <summary>
	/// Gets an instance of this class for the current thread.
	/// </summary>
	/// <remarks>
	/// Since this is backed by a thread static field which is slower than a normal static field,
	/// prefer passing the instance around when possible instead of calling this property each time.
	/// </remarks>
	public static FormattingObjectPool Instance => t_instance ?? (t_instance = new FormattingObjectPool());

	public ListPool<TextRun> TextRunLists { get; } = new ListPool<TextRun>();

	public ListPool<UnshapedTextRun> UnshapedTextRunLists { get; } = new ListPool<UnshapedTextRun>();

	public ListPool<TextLine> TextLines { get; } = new ListPool<TextLine>();

	[Conditional("DEBUG")]
	public void VerifyAllReturned()
	{
	}
}
