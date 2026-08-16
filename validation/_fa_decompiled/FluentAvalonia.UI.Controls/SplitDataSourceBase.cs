using System.Collections.Generic;

namespace FluentAvalonia.UI.Controls;

internal abstract class SplitDataSourceBase<T, TVectorID, AttachedDataType>
{
	private List<TVectorID> flags = new List<TVectorID>();

	private List<AttachedDataType> _attachedData = new List<AttachedDataType>();

	private SplitVector<T, TVectorID>[] splitVectors;

	public abstract int Size { get; }

	protected abstract TVectorID DefaultVectorIDOnInsert { get; }

	protected abstract AttachedDataType DefaultAttachedData { get; }

	protected int RawDataSize => flags.Count;

	public SplitDataSourceBase(int vectorIdSize)
	{
		splitVectors = new SplitVector<T, TVectorID>[vectorIdSize];
	}

	public TVectorID GetVectorIDForItem(int index)
	{
		return flags[index];
	}

	public AttachedDataType AttachedData(int index)
	{
		return _attachedData[index];
	}

	public void AttachedData(int index, AttachedDataType attachedData)
	{
		_attachedData[index] = attachedData;
	}

	public void ResetAttachedData(AttachedDataType attachedData)
	{
		for (int i = 0; i < RawDataSize; i++)
		{
			_attachedData[i] = attachedData;
		}
	}

	public SplitVector<T, TVectorID> GetVectorForItem(int index)
	{
		if (index >= 0 && index < RawDataSize)
		{
			return splitVectors[(int)(object)flags[index]];
		}
		return null;
	}

	public void MoveItemsToVector(TVectorID newVectorID)
	{
		MoveItemsToVector(0, RawDataSize, newVectorID);
	}

	public void MoveItemsToVector(int start, int end, TVectorID newVectorID)
	{
		for (int i = start; i < end; i++)
		{
			MoveItemToVector(i, newVectorID);
		}
	}

	public void MoveItemToVector(int index, TVectorID newVectorID)
	{
		if (!flags[index].Equals(newVectorID))
		{
			GetVectorForItem(index)?.RemoveAt(index);
			flags[index] = newVectorID;
			SplitVector<T, TVectorID> splitVector = splitVectors[(int)(object)newVectorID];
			if (splitVector != null)
			{
				int preferIndex = GetPreferIndex(index, newVectorID);
				T at = GetAt(index);
				splitVector.InsertAt(preferIndex, index, at);
			}
		}
	}

	public abstract int IndexOf(T value);

	public abstract T GetAt(int index);

	protected int IndexOfImpl(T value, TVectorID vectorID)
	{
		int num = IndexOf(value);
		int result = -1;
		if (num != -1)
		{
			SplitVector<T, TVectorID> vectorForItem = GetVectorForItem(num);
			if (vectorForItem != null && vectorForItem.GetVectorIDForItem().Equals(vectorID))
			{
				result = vectorForItem.IndexFromIndexInOriginalVector(num);
			}
		}
		return result;
	}

	protected void InitializeSplitVectors(params SplitVector<T, TVectorID>[] vectors)
	{
		foreach (SplitVector<T, TVectorID> splitVector in vectors)
		{
			splitVectors[(int)(object)splitVector.GetVectorIDForItem()] = splitVector;
		}
	}

	protected SplitVector<T, TVectorID> GetVector(TVectorID vectorID)
	{
		return splitVectors[(int)(object)vectorID];
	}

	protected void OnClear()
	{
		SplitVector<T, TVectorID>[] array = splitVectors;
		for (int i = 0; i < array.Length; i++)
		{
			array[i]?.Clear();
		}
		flags.Clear();
		_attachedData.Clear();
	}

	protected void OnRemoveAt(int startIndex, int count)
	{
		for (int num = startIndex + count - 1; num >= startIndex; num--)
		{
			OnRemoveAt(num);
		}
	}

	protected void OnInsertAt(int startIndex, int count)
	{
		for (int i = startIndex; i < startIndex + count; i++)
		{
			OnInsertAt(i);
		}
	}

	protected void SyncAndInitVectorFlagsWithID(TVectorID defaultID, AttachedDataType defaultAttachedData)
	{
		for (int i = 0; i < Size; i++)
		{
			flags.Add(defaultID);
			_attachedData.Add(defaultAttachedData);
		}
	}

	protected void Clear()
	{
		OnClear();
	}

	private void OnRemoveAt(int index)
	{
		TVectorID vectorID = flags[index];
		SplitVector<T, TVectorID>[] array = splitVectors;
		for (int i = 0; i < array.Length; i++)
		{
			array[i]?.OnRawDataRemove(index, vectorID);
		}
		flags.RemoveAt(index);
		_attachedData.RemoveAt(index);
	}

	private void OnReplace(int index)
	{
		SplitVector<T, TVectorID> vectorForItem = GetVectorForItem(index);
		if (vectorForItem != null)
		{
			T at = GetAt(index);
			vectorForItem.Replace(index, at);
		}
	}

	private void OnInsertAt(int index)
	{
		TVectorID defaultVectorIDOnInsert = DefaultVectorIDOnInsert;
		AttachedDataType defaultAttachedData = DefaultAttachedData;
		int preferIndex = GetPreferIndex(index, defaultVectorIDOnInsert);
		T at = GetAt(index);
		SplitVector<T, TVectorID>[] array = splitVectors;
		for (int i = 0; i < array.Length; i++)
		{
			array[i]?.OnRawDataInsert(preferIndex, index, at, defaultVectorIDOnInsert);
		}
		flags.Insert(index, defaultVectorIDOnInsert);
		_attachedData.Insert(index, defaultAttachedData);
	}

	private int GetPreferIndex(int index, TVectorID vectorID)
	{
		return RangeCount(0, index, vectorID);
	}

	private int RangeCount(int start, int end, TVectorID vectorID)
	{
		int num = 0;
		for (int i = start; i < end; i++)
		{
			if (flags[i].Equals(vectorID))
			{
				num++;
			}
		}
		return num;
	}
}
