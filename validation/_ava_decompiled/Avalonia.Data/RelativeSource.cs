using System;

namespace Avalonia.Data;

/// <summary>
/// Describes the location of a binding source, relative to the binding target.
/// </summary>
public class RelativeSource
{
	private int _ancestorLevel = 1;

	/// <summary>
	/// Gets the level of ancestor to look for when in <see cref="F:Avalonia.Data.RelativeSourceMode.FindAncestor" />  mode.
	/// </summary>
	/// <remarks>
	/// Use the default value of 1 to look for the first ancestor of the specified type.
	/// </remarks>
	public int AncestorLevel
	{
		get
		{
			return _ancestorLevel;
		}
		set
		{
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException("value", "AncestorLevel may not be set to less than 1.");
			}
			_ancestorLevel = value;
		}
	}

	/// <summary>
	/// Gets the type of ancestor to look for when in <see cref="F:Avalonia.Data.RelativeSourceMode.FindAncestor" />  mode.
	/// </summary>
	public Type? AncestorType { get; set; }

	/// <summary>
	/// Gets or sets a value that describes the type of relative source lookup.
	/// </summary>
	public RelativeSourceMode Mode { get; set; }

	public TreeType Tree { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.RelativeSource" /> class.
	/// </summary>
	/// <remarks>
	/// This constructor initializes <see cref="P:Avalonia.Data.RelativeSource.Mode" /> to <see cref="F:Avalonia.Data.RelativeSourceMode.FindAncestor" />.
	/// </remarks>
	public RelativeSource()
	{
		Mode = RelativeSourceMode.FindAncestor;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.RelativeSource" /> class.
	/// </summary>
	/// <param name="mode">The relative source mode.</param>
	public RelativeSource(RelativeSourceMode mode)
	{
		Mode = mode;
	}
}
