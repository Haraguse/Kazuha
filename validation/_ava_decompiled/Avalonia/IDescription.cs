namespace Avalonia;

/// <summary>
/// Interface for objects with a <see cref="P:Avalonia.IDescription.Description" />.
/// </summary>
public interface IDescription
{
	/// <summary>
	/// Gets the description of the object.
	/// </summary>
	/// <value>
	/// The description of the object.
	/// </value>
	string? Description { get; }
}
