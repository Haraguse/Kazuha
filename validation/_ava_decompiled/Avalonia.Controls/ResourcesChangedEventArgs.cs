using System.Threading;

namespace Avalonia.Controls;

/// <summary>
/// Represents the event arguments of <see cref="E:Avalonia.Controls.IResourceHost.ResourcesChanged" />.
/// The <see cref="P:Avalonia.Controls.ResourcesChangedEventArgs.SequenceNumber" /> identifies the changes.
/// </summary>
/// <param name="SequenceNumber">The sequence number used to identify the changes.</param>
/// <remarks>
/// For performance reasons, this type is a struct.
/// Avoid using a default instance of this type or its default constructor, call <see cref="M:Avalonia.Controls.ResourcesChangedEventArgs.Create" /> instead.
/// </remarks>
public readonly record struct ResourcesChangedEventArgs(int SequenceNumber)
{
	private static int s_lastSequenceNumber;

	/// <summary>
	/// Creates a new instance of <see cref="T:Avalonia.Controls.ResourcesChangedEventArgs" /> with an auto-incremented sequence number.
	/// </summary>
	/// <returns></returns>
	public static ResourcesChangedEventArgs Create()
	{
		return new ResourcesChangedEventArgs(Interlocked.Increment(ref s_lastSequenceNumber));
	}
}
