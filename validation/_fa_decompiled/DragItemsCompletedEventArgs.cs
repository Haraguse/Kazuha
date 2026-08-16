using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Input;

/// <summary>
/// Provides event data for the DragItemsCompleted event
/// </summary>
public class DragItemsCompletedEventArgs
{
	[CompilerGenerated]
	private readonly DragDropEffects _003CDropResult_003Ek__BackingField;

	/// <summary>
	/// Gets a value that indicates what operation was performed on the dragged data, and
	/// whether it was successful
	/// </summary>
	public DragDropEffects DropResult
	{
		[CompilerGenerated]
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return _003CDropResult_003Ek__BackingField;
		}
		[CompilerGenerated]
		internal init
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			_003CDropResult_003Ek__BackingField = value;
		}
	}

	/// <summary>
	/// Gets a loosely typed collection of objects that are selected for item drag action
	/// </summary>
	public IReadOnlyList<object> Items { get; internal init; }

	internal DragItemsCompletedEventArgs(DragDropEffects result, IList<object> items)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		base._002Ector();
		DropResult = result;
		Items = new List<object>(items);
	}
}
