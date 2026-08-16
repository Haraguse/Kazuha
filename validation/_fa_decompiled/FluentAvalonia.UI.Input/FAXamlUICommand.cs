using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Data;
using Avalonia.Input;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;

namespace FluentAvalonia.UI.Input;

/// <summary>
/// Provides a base class for defining the command behavior of an interactive UI element that 
/// performs an action when invoked (such as sending an email, deleting an item, or submitting a form).
/// </summary>
/// <summary>
/// Provides a base class for defining the command behavior of an interactive UI element that 
/// performs an action when invoked (such as sending an email, deleting an item, or submitting a form).
/// </summary>
public class FAXamlUICommand : AvaloniaObject, ICommand
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Input.FAXamlUICommand.Command" /> property
	/// </summary>
	public static readonly StyledProperty<ICommand> CommandProperty = AvaloniaProperty.Register<FAXamlUICommand, ICommand>("Command", (ICommand)null, false, (BindingMode)1, (Func<ICommand, bool>)null, (Func<AvaloniaObject, ICommand, ICommand>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Input.FAXamlUICommand.Description" /> property
	/// </summary>
	public static readonly StyledProperty<string> DescriptionProperty = AvaloniaProperty.Register<FAXamlUICommand, string>("Description", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Input.FAXamlUICommand.IconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> IconSourceProperty = AvaloniaProperty.Register<FAXamlUICommand, FAIconSource>("IconSource", (FAIconSource)null, false, (BindingMode)1, (Func<FAIconSource, bool>)null, (Func<AvaloniaObject, FAIconSource, FAIconSource>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Input.FAXamlUICommand.HotKey" /> property
	/// </summary>
	public static readonly StyledProperty<KeyGesture> HotKeyProperty = AvaloniaProperty.Register<FAXamlUICommand, KeyGesture>("HotKey", (KeyGesture)null, false, (BindingMode)1, (Func<KeyGesture, bool>)null, (Func<AvaloniaObject, KeyGesture, KeyGesture>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Input.FAXamlUICommand.Label" /> property
	/// </summary>
	public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<FAXamlUICommand, string>("Label", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);

	/// <summary>
	/// Gets or sets the command behavior of an interactive UI element that performs an action when invoked, 
	/// such as sending an email, deleting an item, or submitting a form.
	/// </summary>
	public ICommand Command
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<ICommand>(CommandProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<ICommand>(CommandProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a description for this element.
	/// </summary>
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

	/// <summary>
	/// Gets or sets an IconSource for this element.
	/// </summary>
	public FAIconSource IconSource
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAIconSource>(IconSourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAIconSource>(IconSourceProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a KeyGesture used to invoke this XamlUICommand
	/// </summary>
	public KeyGesture HotKey
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<KeyGesture>(HotKeyProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<KeyGesture>(HotKeyProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the label for this element.
	/// </summary>
	public string Label
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(LabelProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(LabelProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Occurs whenever something happens that affects whether the command can execute.
	/// </summary>
	public event EventHandler CanExecuteChanged;

	/// <summary>
	/// Occurs when a CanExecute call is made.
	/// </summary>
	public event TypedEventHandler<FAXamlUICommand, FACanExecuteRequestedEventArgs> CanExecuteRequested;

	/// <summary>
	/// Occurs when an Execute call is made.
	/// </summary>
	public event TypedEventHandler<FAXamlUICommand, FAExecuteRequestedEventArgs> ExecuteRequested;

	public void NotifyCanExecuteChanged()
	{
		CanExecuteChanged?.Invoke(this, null);
	}

	public bool CanExecute(object param)
	{
		bool flag = false;
		FACanExecuteRequestedEventArgs e = new FACanExecuteRequestedEventArgs(param);
		CanExecuteRequested?.Invoke(this, e);
		flag = e.CanExecute;
		ICommand command = Command;
		if (command != null)
		{
			bool flag2 = command.CanExecute(param);
			flag &= flag2;
		}
		return flag;
	}

	public void Execute(object param)
	{
		FAExecuteRequestedEventArgs args = new FAExecuteRequestedEventArgs(param);
		ExecuteRequested?.Invoke(this, args);
		Command?.Execute(param);
	}
}
