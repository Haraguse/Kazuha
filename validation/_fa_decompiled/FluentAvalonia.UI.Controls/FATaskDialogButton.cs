using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Data;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a button in <see cref="T:FluentAvalonia.UI.Controls.FATaskDialog" />
/// </summary>
/// <remarks>
/// This type is an abstraction of a button and is used to create the actual button
/// hosted in the TaskDialog.
/// </remarks>
public class FATaskDialogButton : FATaskDialogControl
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialogButton.IconSource" /> property
	/// </summary>
	public static readonly DirectProperty<FATaskDialogButton, FAIconSource> IconSourceProperty = AvaloniaProperty.RegisterDirect<FATaskDialogButton, FAIconSource>("IconSource", (Func<FATaskDialogButton, FAIconSource>)((FATaskDialogButton x) => x.IconSource), (Action<FATaskDialogButton, FAIconSource>)delegate(FATaskDialogButton x, FAIconSource v)
	{
		x.IconSource = v;
	}, (FAIconSource)null, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialogButton.Command" /> property
	/// </summary>
	public static readonly DirectProperty<FATaskDialogButton, ICommand> CommandProperty = AvaloniaProperty.RegisterDirect<FATaskDialogButton, ICommand>("Command", (Func<FATaskDialogButton, ICommand>)((FATaskDialogButton x) => x.Command), (Action<FATaskDialogButton, ICommand>)delegate(FATaskDialogButton x, ICommand v)
	{
		x.Command = v;
	}, (ICommand)null, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialogButton.CommandParameter" /> property
	/// </summary>
	public static readonly DirectProperty<FATaskDialogButton, object> CommandParameterProperty = AvaloniaProperty.RegisterDirect<FATaskDialogButton, object>("CommandParameter", (Func<FATaskDialogButton, object>)((FATaskDialogButton x) => x.CommandParameter), (Action<FATaskDialogButton, object>)delegate(FATaskDialogButton x, object v)
	{
		x.CommandParameter = v;
	}, (object)null, (BindingMode)1, false);

	private FAIconSource _iconSource;

	private ICommand _command;

	private object _commandParameter;

	private bool _isStandard;

	/// <summary>
	/// Predefined button for 'OK'. Note that predefined buttons cannot have command, icons,
	/// or click handlers attached - they are meant for simple purposes
	/// </summary>
	public static readonly FATaskDialogButton OKButton = new FATaskDialogButton(FATaskDialogStandardResult.OK);

	/// <summary>
	/// Predefined button for 'Cancel'. Note that predefined buttons cannot have command, icons,
	/// or click handlers attached - they are meant for simple purposes
	/// </summary>
	public static readonly FATaskDialogButton CancelButton = new FATaskDialogButton(FATaskDialogStandardResult.Cancel);

	/// <summary>
	/// Predefined button for 'Yes'. Note that predefined buttons cannot have command, icons,
	/// or click handlers attached - they are meant for simple purposes
	/// </summary>
	public static readonly FATaskDialogButton YesButton = new FATaskDialogButton(FATaskDialogStandardResult.Yes);

	/// <summary>
	/// Predefined button for 'No'. Note that predefined buttons cannot have command, icons,
	/// or click handlers attached - they are meant for simple purposes
	/// </summary>
	public static readonly FATaskDialogButton NoButton = new FATaskDialogButton(FATaskDialogStandardResult.No);

	/// <summary>
	/// Predefined button for 'Retry'. Note that predefined buttons cannot have command, icons,
	/// or click handlers attached - they are meant for simple purposes
	/// </summary>
	public static readonly FATaskDialogButton RetryButton = new FATaskDialogButton(FATaskDialogStandardResult.Retry);

	/// <summary>
	/// Predefined button for 'Close'. Note that predefined buttons cannot have command, icons,
	/// or click handlers attached - they are meant for simple purposes
	/// </summary>
	public static readonly FATaskDialogButton CloseButton = new FATaskDialogButton(FATaskDialogStandardResult.Close);

	/// <summary>
	/// Gets or sets the icon displayed in the button
	/// </summary>
	public FAIconSource IconSource
	{
		get
		{
			return _iconSource;
		}
		set
		{
			if (_isStandard)
			{
				throw new InvalidOperationException("Cannot add icon to a predefined TaskDialogButton");
			}
			((AvaloniaObject)this).SetAndRaise<FAIconSource>((DirectPropertyBase<FAIconSource>)(object)IconSourceProperty, ref _iconSource, value);
		}
	}

	/// <summary>
	/// Gets or sets the command that is invoked when the button is clicked
	/// </summary>
	public ICommand Command
	{
		get
		{
			return _command;
		}
		set
		{
			if (_isStandard)
			{
				throw new InvalidOperationException("Cannot add Command to a predefined TaskDialogButton");
			}
			((AvaloniaObject)this).SetAndRaise<ICommand>((DirectPropertyBase<ICommand>)(object)CommandProperty, ref _command, value);
		}
	}

	/// <summary>
	/// Gets or sets the command parameter for the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialogButton.Command" />
	/// </summary>
	public object CommandParameter
	{
		get
		{
			return _commandParameter;
		}
		set
		{
			if (_isStandard)
			{
				throw new InvalidOperationException("Cannot add icon to a predefined TaskDialogButton");
			}
			((AvaloniaObject)this).SetAndRaise<object>((DirectPropertyBase<object>)(object)CommandParameterProperty, ref _commandParameter, value);
		}
	}

	/// <summary>
	/// Raised when the button is clicked
	/// </summary>
	public event TypedEventHandler<FATaskDialogButton, EventArgs> Click;

	public FATaskDialogButton()
	{
	}

	public FATaskDialogButton(string text, object result)
		: base(text, result)
	{
	}

	private FATaskDialogButton(FATaskDialogStandardResult result)
		: this(result.ToString(), result)
	{
		_isStandard = true;
	}

	internal void RaiseClick()
	{
		if (!_isStandard)
		{
			Click?.Invoke(this, EventArgs.Empty);
		}
	}
}
