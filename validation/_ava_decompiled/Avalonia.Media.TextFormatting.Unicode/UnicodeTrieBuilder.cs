using System;
using System.Collections.Generic;

namespace Avalonia.Media.TextFormatting.Unicode;

internal class UnicodeTrieBuilder
{
	internal const int SHIFT_1 = 11;

	internal const int SHIFT_2 = 5;

	private const int SHIFT_1_2 = 6;

	internal const int OMITTED_BMP_INDEX_1_LENGTH = 32;

	private const int CP_PER_INDEX_1_ENTRY = 2048;

	private const int INDEX_2_BLOCK_LENGTH = 64;

	internal const int INDEX_2_MASK = 63;

	private const int DATA_BLOCK_LENGTH = 32;

	internal const int DATA_MASK = 31;

	internal const int INDEX_SHIFT = 2;

	internal const int DATA_GRANULARITY = 4;

	private const int INDEX_2_OFFSET = 0;

	internal const int LSCP_INDEX_2_OFFSET = 2048;

	private const int LSCP_INDEX_2_LENGTH = 32;

	private const int INDEX_2_BMP_LENGTH = 2080;

	private const int UTF8_2B_INDEX_2_OFFSET = 2080;

	private const int UTF8_2B_INDEX_2_LENGTH = 32;

	internal const int INDEX_1_OFFSET = 2112;

	private const int MAX_INDEX_1_LENGTH = 512;

	private const int BAD_UTF8_DATA_OFFSET = 128;

	private const int DATA_START_OFFSET = 192;

	private const int DATA_NULL_OFFSET = 192;

	private const int NEW_DATA_START_OFFSET = 256;

	private const int DATA_0800_OFFSET = 2176;

	private const int INITIAL_DATA_LENGTH = 16384;

	private const int MEDIUM_DATA_LENGTH = 131072;

	private const int MAX_DATA_LENGTH_RUNTIME = 262140;

	private const int INDEX_1_LENGTH = 544;

	private const int MAX_DATA_LENGTH_BUILDTIME = 1115264;

	private const int INDEX_GAP_OFFSET = 2080;

	private const int INDEX_GAP_LENGTH = 576;

	private const int MAX_INDEX_2_LENGTH = 35488;

	private const int INDEX_2_NULL_OFFSET = 2656;

	private const int INDEX_2_START_OFFSET = 2720;

	private const int MAX_INDEX_LENGTH = 65535;

	private readonly uint _initialValue;

	private readonly uint _errorValue;

	private readonly int[] _index1;

	private readonly int[] _index2;

	private int _highStart;

	private uint[] _data;

	private int _dataCapacity;

	private int _firstFreeBlock;

	private bool _isCompacted;

	private readonly int[] _map;

	private int _dataNullOffset;

	private int _dataLength;

	private int _index2NullOffset;

	private int _index2Length;

	public UnicodeTrieBuilder(uint initialValue = 0u, uint errorValue = 0u)
	{
		_initialValue = initialValue;
		_errorValue = errorValue;
		_index1 = new int[544];
		_index2 = new int[35488];
		_highStart = 1114112;
		_data = new uint[16384];
		_dataCapacity = 16384;
		_firstFreeBlock = 0;
		_isCompacted = false;
		_map = new int[34852];
		int i;
		for (i = 0; i < 128; i++)
		{
			_data[i] = _initialValue;
		}
		for (; i < 192; i++)
		{
			_data[i] = _errorValue;
		}
		for (i = 192; i < 256; i++)
		{
			_data[i] = _initialValue;
		}
		_dataNullOffset = 192;
		_dataLength = 256;
		i = 0;
		int j;
		for (j = 0; j < 128; j += 32)
		{
			_index2[i] = j;
			_map[i++] = 1;
		}
		for (; j < 192; j += 32)
		{
			_map[i++] = 0;
		}
		_map[i++] = 34845;
		for (j += 32; j < 256; j += 32)
		{
			_map[i++] = 0;
		}
		for (i = 4; i < 2080; i++)
		{
			_index2[i] = 192;
		}
		for (i = 0; i < 576; i++)
		{
			_index2[2080 + i] = -1;
		}
		for (i = 0; i < 64; i++)
		{
			_index2[2656 + i] = 192;
		}
		_index2NullOffset = 2656;
		_index2Length = 2720;
		j = 0;
		for (i = 0; i < 32; i++)
		{
			_index1[i] = j;
			j += 64;
		}
		for (; i < 544; i++)
		{
			_index1[i] = 2656;
		}
		for (i = 128; i < 2048; i += 32)
		{
			Set(i, _initialValue);
		}
	}

	public UnicodeTrieBuilder Set(int codePoint, uint value)
	{
		if (codePoint < 0 || codePoint > 1114111)
		{
			throw new InvalidOperationException("Invalid code point");
		}
		if (_isCompacted)
		{
			throw new InvalidOperationException("Already compacted");
		}
		int dataBlock = GetDataBlock(codePoint, forLSCP: true);
		_data[dataBlock + (codePoint & 0x1F)] = value;
		return this;
	}

	public UnicodeTrieBuilder SetRange(int start, int end, uint value, bool overwrite = true)
	{
		if (start > 1114111 || end > 1114111 || start > end)
		{
			throw new InvalidOperationException("Invalid code point");
		}
		if (_isCompacted)
		{
			throw new InvalidOperationException("Already compacted");
		}
		if (!overwrite && value == _initialValue)
		{
			return this;
		}
		int num = end + 1;
		if ((start & 0x1F) != 0)
		{
			int dataBlock = GetDataBlock(start, forLSCP: true);
			int num2 = (start + 32) & -32;
			if (num2 > num)
			{
				FillBlock(dataBlock, start & 0x1F, num & 0x1F, value, _initialValue, overwrite);
				return this;
			}
			FillBlock(dataBlock, start & 0x1F, 32, value, _initialValue, overwrite);
			start = num2;
		}
		int num3 = num & 0x1F;
		num &= -32;
		int num4 = ((value != _initialValue) ? (-1) : _dataNullOffset);
		while (start < num)
		{
			bool flag = false;
			if (value == _initialValue && IsInNullBlock(start, forLSCP: true))
			{
				start += 32;
				continue;
			}
			int index2Block = GetIndex2Block(start, forLSCP: true);
			index2Block += (start >> 5) & 0x3F;
			int num5 = _index2[index2Block];
			if (IsWritableBlock(num5))
			{
				if (overwrite && num5 >= 2176)
				{
					flag = true;
				}
				else
				{
					FillBlock(num5, 0, 32, value, _initialValue, overwrite);
				}
			}
			else if (_data[num5] != value && (overwrite || num5 == _dataNullOffset))
			{
				flag = true;
			}
			if (flag)
			{
				if (num4 >= 0)
				{
					SetIndex2Entry(index2Block, num4);
				}
				else
				{
					num4 = GetDataBlock(start, forLSCP: true);
					WriteBlock(num4, value);
				}
			}
			start += 32;
		}
		if (num3 > 0)
		{
			int dataBlock2 = GetDataBlock(start, forLSCP: true);
			FillBlock(dataBlock2, 0, num3, value, _initialValue, overwrite);
		}
		return this;
	}

	public uint Get(int c, bool fromLSCP = true)
	{
		if (c < 0 || c > 1114111)
		{
			return _errorValue;
		}
		if (c >= _highStart && ((c < 55296 || c >= 56320) | fromLSCP))
		{
			return _data[_dataLength - 4];
		}
		int num = ((!((c >= 55296 && c < 56320) & fromLSCP)) ? (_index1[c >> 11] + ((c >> 5) & 0x3F)) : (320 + (c >> 5)));
		int num2 = _index2[num];
		return _data[num2 + (c & 0x1F)];
	}

	public UnicodeTrie Freeze()
	{
		if (!_isCompacted)
		{
			Compact();
		}
		int num = ((_highStart > 65536) ? _index2Length : 2112);
		int num2 = num;
		if (num > 65535 || num2 + _dataNullOffset > 65535 || num2 + 2176 > 65535 || num2 + _dataLength > 262140)
		{
			throw new InvalidOperationException("Trie data is too large.");
		}
		uint[] array = new uint[num + _dataLength];
		int num3 = 0;
		int i;
		for (i = 0; i < 2080; i++)
		{
			array[num3++] = (uint)(_index2[i] + num2 >> 2);
		}
		for (i = 0; i < 2; i++)
		{
			array[num3++] = (uint)(num2 + 128);
		}
		for (; i < 32; i++)
		{
			array[num3++] = (uint)(num2 + _index2[i << 1]);
		}
		if (_highStart > 65536)
		{
			int num4 = _highStart - 65536 >> 11;
			int num5 = 2112 + num4;
			for (i = 0; i < num4; i++)
			{
				array[num3++] = (uint)_index1[i + 32];
			}
			for (i = 0; i < _index2Length - num5; i++)
			{
				array[num3++] = (uint)(num2 + _index2[num5 + i] >> 2);
			}
		}
		for (i = 0; i < _dataLength; i++)
		{
			array[num3++] = _data[i];
		}
		return new UnicodeTrie(array, _highStart, _errorValue);
	}

	private bool IsInNullBlock(int c, bool forLSCP)
	{
		int num = ((!(((c & 0xFFFFFC00u) == 55296) & forLSCP)) ? (_index1[c >> 11] + ((c >> 5) & 0x3F)) : (320 + (c >> 5)));
		return _index2[num] == _dataNullOffset;
	}

	private int AllocIndex2Block()
	{
		int index2Length = _index2Length;
		int num = index2Length + 64;
		if (num > _index2.Length)
		{
			throw new InvalidOperationException("Internal error in Trie2 creation.");
		}
		_index2Length = num;
		Array.Copy(_index2, _index2NullOffset, _index2, index2Length, 64);
		return index2Length;
	}

	private int GetIndex2Block(int c, bool forLSCP)
	{
		if ((c >= 55296 && c < 56320) & forLSCP)
		{
			return 2048;
		}
		int num = c >> 11;
		int num2 = _index1[num];
		if (num2 == _index2NullOffset)
		{
			num2 = AllocIndex2Block();
			_index1[num] = num2;
		}
		return num2;
	}

	private bool IsWritableBlock(int block)
	{
		if (block != _dataNullOffset)
		{
			return _map[block >> 5] == 1;
		}
		return false;
	}

	private int AllocDataBlock(int copyBlock)
	{
		int num;
		if (_firstFreeBlock != 0)
		{
			num = _firstFreeBlock;
			_firstFreeBlock = -_map[num >> 5];
		}
		else
		{
			num = _dataLength;
			int num2 = num + 32;
			if (num2 > _dataCapacity)
			{
				int num3;
				if (_dataCapacity < 131072)
				{
					num3 = 131072;
				}
				else
				{
					if (_dataCapacity >= 1115264)
					{
						throw new InvalidOperationException("Internal error in Trie2 creation.");
					}
					num3 = 1115264;
				}
				uint[] array = new uint[num3];
				Array.Copy(_data, array, _dataLength);
				_data = array;
				_dataCapacity = num3;
			}
			_dataLength = num2;
		}
		Array.Copy(_data, copyBlock, _data, num, 32);
		_map[num >> 5] = 0;
		return num;
	}

	private void ReleaseDataBlock(int block)
	{
		_map[block >> 5] = -_firstFreeBlock;
		_firstFreeBlock = block;
	}

	private void SetIndex2Entry(int i2, int block)
	{
		_map[block >> 5]++;
		int num = _index2[i2];
		if (--_map[num >> 5] == 0)
		{
			ReleaseDataBlock(num);
		}
		_index2[i2] = block;
	}

	private int GetDataBlock(int c, bool forLSCP)
	{
		int index2Block = GetIndex2Block(c, forLSCP);
		index2Block += (c >> 5) & 0x3F;
		int num = _index2[index2Block];
		if (IsWritableBlock(num))
		{
			return num;
		}
		int num2 = AllocDataBlock(num);
		SetIndex2Entry(index2Block, num2);
		return num2;
	}

	private void FillBlock(int block, int start, int limit, uint value, uint initialValue, bool overwrite)
	{
		if (overwrite)
		{
			for (int i = block + start; i < block + limit; i++)
			{
				_data[i] = value;
			}
			return;
		}
		for (int i = block + start; i < block + limit; i++)
		{
			if (_data[i] == initialValue)
			{
				_data[i] = value;
			}
		}
	}

	private void WriteBlock(int block, uint value)
	{
		int num = block + 32;
		while (block < num)
		{
			_data[block++] = value;
		}
	}

	private int FindHighStart(uint highValue)
	{
		int num;
		int num2;
		if (highValue == _initialValue)
		{
			num = _index2NullOffset;
			num2 = _dataNullOffset;
		}
		else
		{
			num = -1;
			num2 = -1;
		}
		int num3 = 544;
		int num4 = 1114112;
		while (num4 > 0)
		{
			int num5 = _index1[--num3];
			if (num5 == num)
			{
				num4 -= 2048;
				continue;
			}
			num = num5;
			if (num5 == _index2NullOffset)
			{
				if (highValue != _initialValue)
				{
					return num4;
				}
				num4 -= 2048;
				continue;
			}
			int num6 = 64;
			while (num6 > 0)
			{
				int num7 = _index2[num5 + --num6];
				if (num7 == num2)
				{
					num4 -= 32;
					continue;
				}
				num2 = num7;
				if (num7 == _dataNullOffset)
				{
					if (highValue != _initialValue)
					{
						return num4;
					}
					num4 -= 32;
					continue;
				}
				int num8 = 32;
				while (num8 > 0)
				{
					if (_data[num7 + --num8] != highValue)
					{
						return num4;
					}
					num4--;
				}
			}
		}
		return 0;
	}

	private int FindSameDataBlock(int dataLength, int otherBlock, int blockLength)
	{
		dataLength -= blockLength;
		for (int i = 0; i <= dataLength; i += 4)
		{
			if (EqualSequence(_data, i, otherBlock, blockLength))
			{
				return i;
			}
		}
		return -1;
	}

	private int FindSameIndex2Block(int index2Length, int otherBlock)
	{
		index2Length -= 64;
		for (int i = 0; i <= index2Length; i++)
		{
			if (EqualSequence(_index2, i, otherBlock, 64))
			{
				return i;
			}
		}
		return -1;
	}

	private void CompactData()
	{
		int num = 192;
		int i = 0;
		int num2 = 0;
		for (; i < num; i += 32)
		{
			_map[num2++] = i;
		}
		int num3 = 64;
		int num4 = num3 >> 5;
		i = num;
		while (i < _dataLength)
		{
			if (i == 2176)
			{
				num3 = 32;
				num4 = 1;
			}
			if (_map[i >> 5] <= 0)
			{
				i += num3;
				continue;
			}
			int num5;
			if ((num5 = FindSameDataBlock(num, i, num3)) >= 0)
			{
				int num6 = i >> 5;
				for (num2 = num4; num2 > 0; num2--)
				{
					_map[num6++] = num5;
					num5 += 32;
				}
				i += num3;
				continue;
			}
			int num7 = num3 - 4;
			while (num7 > 0 && !EqualSequence(_data, num - num7, i, num7))
			{
				num7 -= 4;
			}
			if (num7 > 0 || num < i)
			{
				num5 = num - num7;
				int num6 = i >> 5;
				for (num2 = num4; num2 > 0; num2--)
				{
					_map[num6++] = num5;
					num5 += 32;
				}
				i += num7;
				for (num2 = num3 - num7; num2 > 0; num2--)
				{
					_data[num++] = _data[i++];
				}
			}
			else
			{
				int num6 = i >> 5;
				for (num2 = num4; num2 > 0; num2--)
				{
					_map[num6++] = i;
					i += 32;
				}
				num = i;
			}
		}
		for (num2 = 0; num2 < _index2Length; num2++)
		{
			if (num2 == 2080)
			{
				num2 += 576;
			}
			_index2[num2] = _map[_index2[num2] >> 5];
		}
		_dataNullOffset = _map[_dataNullOffset >> 5];
		while ((num & 3) != 0)
		{
			_data[num++] = _initialValue;
		}
		_dataLength = num;
	}

	private void CompactIndex2()
	{
		int num = 2080;
		int i = 0;
		int num2 = 0;
		for (; i < num; i += 64)
		{
			_map[num2++] = i;
		}
		num += 32 + (_highStart - 65536 >> 11);
		i = 2656;
		while (i < _index2Length)
		{
			int num3;
			if ((num3 = FindSameIndex2Block(num, i)) >= 0)
			{
				_map[i >> 6] = num3;
				i += 64;
				continue;
			}
			int num4 = 63;
			while (num4 > 0 && !EqualSequence(_index2, num - num4, i, num4))
			{
				num4--;
			}
			if (num4 > 0 || num < i)
			{
				_map[i >> 6] = num - num4;
				i += num4;
				for (num2 = 64 - num4; num2 > 0; num2--)
				{
					_index2[num++] = _index2[i++];
				}
			}
			else
			{
				_map[i >> 6] = i;
				i += 64;
				num = i;
			}
		}
		for (num2 = 0; num2 < 544; num2++)
		{
			_index1[num2] = _map[_index1[num2] >> 6];
		}
		_index2NullOffset = _map[_index2NullOffset >> 6];
		while ((num & 3) != 0)
		{
			_index2[num++] = 262140;
		}
		_index2Length = num;
	}

	private void Compact()
	{
		uint num = Get(1114111);
		int num2 = FindHighStart(num);
		num2 = (num2 + 2047) & -2048;
		if (num2 == 1114112)
		{
			num = _errorValue;
		}
		_highStart = num2;
		if (_highStart < 1114112)
		{
			int start = ((_highStart <= 65536) ? 65536 : _highStart);
			SetRange(start, 1114111, _initialValue);
		}
		CompactData();
		if (_highStart > 65536)
		{
			CompactIndex2();
		}
		_data[_dataLength++] = num;
		while ((_dataLength & 3) != 0)
		{
			_data[_dataLength++] = _initialValue;
		}
		_isCompacted = true;
	}

	private static bool EqualSequence(IReadOnlyList<uint> a, int s, int t, int length)
	{
		for (int i = 0; i < length; i++)
		{
			if (a[s + i] != a[t + i])
			{
				return false;
			}
		}
		return true;
	}

	private static bool EqualSequence(IReadOnlyList<int> a, int s, int t, int length)
	{
		for (int i = 0; i < length; i++)
		{
			if (a[s + i] != a[t + i])
			{
				return false;
			}
		}
		return true;
	}
}
