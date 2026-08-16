namespace Avalonia.Utilities;

internal abstract class FrugalListBase<T>
{
	internal class Compacter
	{
		protected readonly FrugalListBase<T> _store;

		private readonly int _newCount;

		protected int _validItemCount;

		protected int _previousEnd;

		public Compacter(FrugalListBase<T> store, int newCount)
		{
			_store = store;
			_newCount = newCount;
		}

		public void Include(int start, int end)
		{
			IncludeOverride(start, end);
			_previousEnd = end;
		}

		protected virtual void IncludeOverride(int start, int end)
		{
			for (int i = start; i < end; i++)
			{
				_store.SetAt(_validItemCount++, _store.EntryAt(i));
			}
		}

		public virtual FrugalListBase<T> Finish()
		{
			T value = default(T);
			int i = _validItemCount;
			for (int count = _store._count; i < count; i++)
			{
				_store.SetAt(i, value);
			}
			_store._count = _validItemCount;
			return _store;
		}
	}

	protected int _count;

	/// <summary>
	/// Number of entries in this store
	/// </summary>
	public int Count => _count;

	/// <summary>
	/// Capacity of this store
	/// </summary>
	public abstract int Capacity { get; }

	internal void TrustedSetCount(int newCount)
	{
		_count = newCount;
	}

	public abstract FrugalListStoreState Add(T value);

	/// <summary>
	/// Removes all values from the store
	/// </summary>
	public abstract void Clear();

	/// <summary>
	/// Returns true if the store contains the entry.
	/// </summary>
	public abstract bool Contains(T value);

	/// <summary>
	/// Returns the index into the store that contains the item.
	/// -1 is returned if the item is not in the store.
	/// </summary>
	public abstract int IndexOf(T value);

	/// <summary>
	/// Insert item into the store at index, grows if needed
	/// </summary>
	public abstract void Insert(int index, T value);

	public abstract void SetAt(int index, T value);

	/// <summary>
	/// Removes the item from the store. If the item was not
	/// in the store false is returned.
	/// </summary>
	public abstract bool Remove(T value);

	/// <summary>
	/// Removes the item from the store
	/// </summary>
	public abstract void RemoveAt(int index);

	/// <summary>
	/// Return the item at index in the store
	/// </summary>
	public abstract T EntryAt(int index);

	/// <summary>
	/// Promotes the values in the current store to the next larger
	/// and more complex storage model.
	/// </summary>
	public abstract void Promote(FrugalListBase<T> newList);

	/// <summary>
	/// Returns the entries as an array
	/// </summary>
	public abstract T[] ToArray();

	/// <summary>
	/// Copies the entries to the given array starting at the
	/// specified index
	/// </summary>
	public abstract void CopyTo(T[] array, int index);

	/// <summary>
	/// Creates a shallow copy of the  list
	/// </summary>
	public abstract object Clone();

	public virtual Compacter NewCompacter(int newCount)
	{
		return new Compacter(this, newCount);
	}
}
