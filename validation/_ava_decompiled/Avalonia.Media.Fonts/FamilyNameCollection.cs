using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Media.Fonts;

public sealed class FamilyNameCollection : IReadOnlyList<string>, IEnumerable<string>, IEnumerable, IReadOnlyCollection<string>
{
	private readonly string[] _names;

	/// <summary>
	/// Gets the primary family name.
	/// </summary>
	/// <value>
	/// The primary family name.
	/// </value>
	public string PrimaryFamilyName { get; }

	/// <summary>
	/// Gets a value indicating whether fallbacks are defined.
	/// </summary>
	/// <value>
	///   <c>true</c> if fallbacks are defined; otherwise, <c>false</c>.
	/// </value>
	public bool HasFallbacks { get; }

	public int Count => _names.Length;

	public string this[int index] => _names[index];

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Fonts.FamilyNameCollection" /> class.
	/// </summary>
	/// <param name="familyNames">The family names.</param>
	/// <exception cref="T:System.ArgumentException">familyNames</exception>
	public FamilyNameCollection(string familyNames)
	{
		if (familyNames == null)
		{
			throw new ArgumentNullException("familyNames");
		}
		_names = SplitNames(familyNames);
		PrimaryFamilyName = _names[0];
		HasFallbacks = _names.Length > 1;
	}

	internal FamilyNameCollection(FrugalStructList<FontSourceIdentifier> fontSources)
	{
		_names = new string[fontSources.Count];
		for (int i = 0; i < fontSources.Count; i++)
		{
			_names[i] = fontSources[i].Name;
		}
		PrimaryFamilyName = _names[0];
		HasFallbacks = _names.Length > 1;
	}

	private static string[] SplitNames(string names)
	{
		return names.Split(',', StringSplitOptions.TrimEntries);
	}

	/// <summary>
	/// Returns an enumerator for the name collection.
	/// </summary>
	public ImmutableReadOnlyListStructEnumerator<string> GetEnumerator()
	{
		return new ImmutableReadOnlyListStructEnumerator<string>(this);
	}

	IEnumerator<string> IEnumerable<string>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	/// <summary>
	/// Returns a <see cref="T:System.String" /> that represents this instance.
	/// </summary>
	/// <returns>
	/// A <see cref="T:System.String" /> that represents this instance.
	/// </returns>
	public override string ToString()
	{
		return string.Join(", ", _names);
	}

	/// <summary>
	/// Returns a hash code for this instance.
	/// </summary>
	/// <returns>
	/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
	/// </returns>
	public override int GetHashCode()
	{
		if (_names.Length == 0)
		{
			return 0;
		}
		int num = 17;
		for (int i = 0; i < _names.Length; i++)
		{
			string text = _names[i];
			num = num * 23 + text.GetHashCode();
		}
		return num;
	}

	public static bool operator !=(FamilyNameCollection? a, FamilyNameCollection? b)
	{
		return !(a == b);
	}

	public static bool operator ==(FamilyNameCollection? a, FamilyNameCollection? b)
	{
		if ((object)a == b)
		{
			return true;
		}
		return a?.Equals(b) ?? false;
	}

	/// <summary>
	/// Determines whether the specified <see cref="T:System.Object" />, is equal to this instance.
	/// </summary>
	/// <param name="obj">The <see cref="T:System.Object" /> to compare with this instance.</param>
	/// <returns>
	///   <c>true</c> if the specified <see cref="T:System.Object" /> is equal to this instance; otherwise, <c>false</c>.
	/// </returns>
	public override bool Equals(object? obj)
	{
		if (obj is FamilyNameCollection familyNameCollection)
		{
			return ((ReadOnlySpan<string>)_names.AsSpan()).SequenceEqual((ReadOnlySpan<string>)familyNameCollection._names);
		}
		return false;
	}
}
