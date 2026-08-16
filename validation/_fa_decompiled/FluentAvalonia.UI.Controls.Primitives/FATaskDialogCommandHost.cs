using System;
using Avalonia;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls.Primitives;

/// <summary>
/// Represents a command button in a TaskDialog
/// </summary>
/// <remarks>
/// This type should not be used directly and is generated automatically
/// by a TaskDialog
/// </remarks>
public class FATaskDialogCommandHost : FATaskDialogButtonHost
{
	public static readonly StyledProperty<string> DescriptionProperty = AvaloniaProperty.Register<FATaskDialogCommandHost, string>("Description", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);

	public string Description
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(DescriptionProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(DescriptionProperty, value, (BindingPriority)0);
		}
	}
}
