using System;
using Avalonia;
using Avalonia.Data;
using Avalonia.Input;
using FluentAvalonia.UI.Controls;

namespace FluentAvalonia.UI.Input;

/// <summary>
/// Derives from XamlUICommand, adding a set of standard platform commands with pre-defined properties.
/// </summary>
public class FAStandardUICommand : FAXamlUICommand
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Input.FAStandardUICommand.Kind" /> property
	/// </summary>
	public static readonly StyledProperty<FAStandardUICommandKind> KindProperty = AvaloniaProperty.Register<FAStandardUICommand, FAStandardUICommandKind>("Kind", FAStandardUICommandKind.None, false, (BindingMode)1, (Func<FAStandardUICommandKind, bool>)null, (Func<AvaloniaObject, FAStandardUICommandKind, FAStandardUICommandKind>)null, false);

	/// <summary>
	/// Gets the platform command (with pre-defined properties such as icon, keyboard accelerator, 
	/// and description) that can be used with a StandardUICommand.
	/// </summary>
	public FAStandardUICommandKind Kind
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAStandardUICommandKind>(KindProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAStandardUICommandKind>(KindProperty, value, (BindingPriority)0);
		}
	}

	public FAStandardUICommand()
	{
	}

	public FAStandardUICommand(FAStandardUICommandKind kind)
	{
		Kind = kind;
		SetupCommand();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((AvaloniaObject)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)KindProperty)
		{
			SetupCommand();
		}
	}

	private void SetupCommand()
	{
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Expected O, but got Unknown
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Expected O, but got Unknown
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Expected O, but got Unknown
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Expected O, but got Unknown
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Expected O, but got Unknown
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Expected O, but got Unknown
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Expected O, but got Unknown
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Expected O, but got Unknown
		//IL_0387: Unknown result type (might be due to invalid IL or missing references)
		//IL_0391: Expected O, but got Unknown
		//IL_03c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cc: Expected O, but got Unknown
		switch (Kind)
		{
		case FAStandardUICommandKind.None:
			base.Label = string.Empty;
			base.IconSource = null;
			base.Description = string.Empty;
			base.HotKey = null;
			break;
		case FAStandardUICommandKind.Cut:
			base.Label = "Cut";
			base.IconSource = new FASymbolIconSource
			{
				Symbol = FASymbol.Cut
			};
			base.Description = "Remove the selected content and put it on the clipboard";
			base.HotKey = new KeyGesture((Key)67, (KeyModifiers)2);
			break;
		case FAStandardUICommandKind.Copy:
			base.Label = "Copy";
			base.IconSource = new FASymbolIconSource
			{
				Symbol = FASymbol.Copy
			};
			base.Description = "Copy the selected content to the clipboard";
			base.HotKey = new KeyGesture((Key)46, (KeyModifiers)2);
			break;
		case FAStandardUICommandKind.Paste:
			base.Label = "Paste";
			base.IconSource = new FASymbolIconSource
			{
				Symbol = FASymbol.Paste
			};
			base.Description = "Insert the contents of the clipboard at the current location";
			base.HotKey = new KeyGesture((Key)65, (KeyModifiers)2);
			break;
		case FAStandardUICommandKind.SelectAll:
			base.Label = "Select All";
			base.IconSource = new FASymbolIconSource
			{
				Symbol = FASymbol.SelectAll
			};
			base.Description = "Select all content";
			base.HotKey = new KeyGesture((Key)44, (KeyModifiers)2);
			break;
		case FAStandardUICommandKind.Delete:
			base.Label = "Delete";
			base.IconSource = new FASymbolIconSource
			{
				Symbol = FASymbol.Delete
			};
			base.Description = "Delete the selected content";
			base.HotKey = new KeyGesture((Key)32, (KeyModifiers)0);
			break;
		case FAStandardUICommandKind.Share:
			base.Label = "Share";
			base.IconSource = new FASymbolIconSource
			{
				Symbol = FASymbol.Share
			};
			base.Description = "Share the selected content";
			break;
		case FAStandardUICommandKind.Save:
			base.Label = "Save";
			base.IconSource = new FASymbolIconSource
			{
				Symbol = FASymbol.Save
			};
			base.Description = "Save your changes";
			base.HotKey = new KeyGesture((Key)62, (KeyModifiers)2);
			break;
		case FAStandardUICommandKind.Open:
		{
			string label = (base.Description = "Open");
			base.Label = label;
			base.IconSource = new FASymbolIconSource
			{
				Symbol = FASymbol.Open
			};
			base.HotKey = new KeyGesture((Key)58, (KeyModifiers)2);
			break;
		}
		case FAStandardUICommandKind.Close:
		{
			string label = (base.Description = "Close");
			base.Label = label;
			base.IconSource = new FASymbolIconSource
			{
				Symbol = FASymbol.Dismiss
			};
			base.HotKey = new KeyGesture((Key)66, (KeyModifiers)2);
			break;
		}
		case FAStandardUICommandKind.Pause:
		{
			string label = (base.Description = "Pause");
			base.Label = label;
			base.IconSource = new FASymbolIconSource
			{
				Symbol = FASymbol.Pause
			};
			break;
		}
		case FAStandardUICommandKind.Play:
		{
			string label = (base.Description = "Play");
			base.Label = label;
			base.IconSource = new FASymbolIconSource
			{
				Symbol = FASymbol.Play
			};
			break;
		}
		case FAStandardUICommandKind.Stop:
		{
			string label = (base.Description = "Stop");
			base.Label = label;
			base.IconSource = new FASymbolIconSource
			{
				Symbol = FASymbol.Stop
			};
			break;
		}
		case FAStandardUICommandKind.Forward:
			base.Label = "Forward";
			base.IconSource = new FASymbolIconSource
			{
				Symbol = FASymbol.Forward
			};
			base.Description = "Go to the next item";
			break;
		case FAStandardUICommandKind.Backward:
			base.Label = "Backward";
			base.IconSource = new FASymbolIconSource
			{
				Symbol = FASymbol.Back
			};
			base.Description = "Back";
			break;
		case FAStandardUICommandKind.Undo:
			base.Label = "Undo";
			base.IconSource = new FASymbolIconSource
			{
				Symbol = FASymbol.Undo
			};
			base.Description = "Reverse the most recent action";
			base.HotKey = new KeyGesture((Key)69, (KeyModifiers)2);
			break;
		case FAStandardUICommandKind.Redo:
			base.Label = "Redo";
			base.IconSource = new FASymbolIconSource
			{
				Symbol = FASymbol.Redo
			};
			base.Description = "Repeat the most recently undone action";
			base.HotKey = new KeyGesture((Key)68, (KeyModifiers)2);
			break;
		default:
			throw new NotImplementedException();
		}
	}
}
