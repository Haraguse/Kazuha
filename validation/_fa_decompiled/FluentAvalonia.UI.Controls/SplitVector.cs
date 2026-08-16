using System;
using System.Collections.Generic;
using Avalonia.Collections;

namespace FluentAvalonia.UI.Controls;

internal class SplitVector<T, TVectorId>
{
	private TVectorId _vectorID;

	private IList<T> vector;

	private List<int> indicesInOriginalVector = new List<int>();

	private Func<T, int> indexFunctionFromDataSource;

	public IList<T> Vector => vector;

	public int Size => indicesInOriginalVector.Count;

	public SplitVector(TVectorId id, Func<T, int> indexOfFunction)
	{
		_vectorID = id;
		indexFunctionFromDataSource = indexOfFunction;
		vector = (IList<T>)new AvaloniaList<T>();
	}

	public TVectorId GetVectorIDForItem()
	{
		return _vectorID;
	}

	public void OnRawDataRemove(int indexInOriginalVector, TVectorId vectorID)
	{
		if (_vectorID.Equals(vectorID))
		{
			RemoveAt(indexInOriginalVector);
		}
		for (int i = 0; i < indicesInOriginalVector.Count; i++)
		{
			if (indicesInOriginalVector[i] > indexInOriginalVector)
			{
				indicesInOriginalVector[i]--;
			}
		}
	}

	public void OnRawDataInsert(int preferIndex, int indexInOriginalVector, T value, TVectorId vectorID)
	{
		for (int i = 0; i < indicesInOriginalVector.Count; i++)
		{
			if (indicesInOriginalVector[i] >= indexInOriginalVector)
			{
				indicesInOriginalVector[i]++;
			}
		}
		if (_vectorID.Equals(vectorID))
		{
			InsertAt(preferIndex, indexInOriginalVector, value);
		}
	}

	public void InsertAt(int preferIndex, int indexInOriginalVector, T value)
	{
		vector.Insert(preferIndex, value);
		indicesInOriginalVector.Insert(preferIndex, indexInOriginalVector);
	}

	public void Replace(int indexInOriginalVector, T value)
	{
		int index = IndexFromIndexInOriginalVector(indexInOriginalVector);
		IList<T> list = vector;
		list.RemoveAt(index);
		list.Insert(index, value);
	}

	public void Clear()
	{
		vector.Clear();
		indicesInOriginalVector.Clear();
	}

	public void RemoveAt(int indexInOriginalVector)
	{
		int index = IndexFromIndexInOriginalVector(indexInOriginalVector);
		vector.RemoveAt(index);
		indicesInOriginalVector.RemoveAt(index);
	}

	public int IndexOf(T value)
	{
		int indexInOriginalVector = indexFunctionFromDataSource(value);
		return IndexFromIndexInOriginalVector(indexInOriginalVector);
	}

	public int IndexToIndexInOriginalVector(int index)
	{
		return indicesInOriginalVector[index];
	}

	public int IndexFromIndexInOriginalVector(int indexInOriginalVector)
	{
		int result = indicesInOriginalVector.IndexOf(indexInOriginalVector);
		_ = -1;
		return result;
	}
}
