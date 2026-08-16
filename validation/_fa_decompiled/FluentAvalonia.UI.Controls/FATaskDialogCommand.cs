using System;
using Avalonia;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a command item in a <see cref="T:FluentAvalonia.UI.Controls.FATaskDialog" />
/// </summary>
public class FATaskDialogCommand : FATaskDialogButton
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialogCommand.Description" /> property
	/// </summary>
	public static readonly DirectProperty<FATaskDialogCommand, string> DescriptionProperty = AvaloniaProperty.RegisterDirect<FATaskDialogCommand, string>("Description", (Func<FATaskDialogCommand, string>)((FATaskDialogCommand x) => x.Description), (Action<FATaskDialogCommand, string>)delegate(FATaskDialogCommand x, string v)
	{
		x.Description = v;
	}, (string)null, (BindingMode)1, false);

	private string _description;

	/// <summary>
	/// Gets or sets whether invoking this command should also close the dialog
	/// </summary>
	public bool ClosesOnInvoked { get; set; } = true;

	/// <summary>
	/// Gets or sets the description of the TaskDialogCommand
	/// </summary>
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<string>((DirectPropertyBase<string>)(object)DescriptionProperty, ref _description, value);
		}
	}
}
