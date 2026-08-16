using System;
using Avalonia;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents the base class for all items in a <see cref="T:FluentAvalonia.UI.Controls.FATaskDialog" />
/// </summary>
public abstract class FATaskDialogControl : AvaloniaObject
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialogControl.Text" /> property
	/// </summary>
	public static readonly DirectProperty<FATaskDialogControl, string> TextProperty = AvaloniaProperty.RegisterDirect<FATaskDialogControl, string>("Text", (Func<FATaskDialogControl, string>)((FATaskDialogControl x) => x.Text), (Action<FATaskDialogControl, string>)delegate(FATaskDialogControl x, string v)
	{
		x.Text = v;
	}, (string)null, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialogControl.DialogResult" /> property
	/// </summary>
	public static readonly DirectProperty<FATaskDialogControl, object> DialogResultProperty = AvaloniaProperty.RegisterDirect<FATaskDialogControl, object>("DialogResult", (Func<FATaskDialogControl, object>)((FATaskDialogControl x) => x.DialogResult), (Action<FATaskDialogControl, object>)delegate(FATaskDialogControl x, object v)
	{
		x.DialogResult = v;
	}, (object)null, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialogControl.IsEnabled" /> property
	/// </summary>
	public static readonly DirectProperty<FATaskDialogControl, bool> IsEnabledProperty = AvaloniaProperty.RegisterDirect<FATaskDialogControl, bool>("IsEnabled", (Func<FATaskDialogControl, bool>)((FATaskDialogControl x) => x.IsEnabled), (Action<FATaskDialogControl, bool>)delegate(FATaskDialogControl x, bool v)
	{
		x.IsEnabled = v;
	}, false, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialogControl.IsDefault" /> property
	/// </summary>
	public static readonly DirectProperty<FATaskDialogControl, bool> IsDefaultProperty = AvaloniaProperty.RegisterDirect<FATaskDialogControl, bool>("IsDefault", (Func<FATaskDialogControl, bool>)((FATaskDialogControl x) => x.IsDefault), (Action<FATaskDialogControl, bool>)delegate(FATaskDialogControl x, bool v)
	{
		x.IsDefault = v;
	}, false, (BindingMode)1, false);

	private string _text;

	private object _dialogResult = FATaskDialogStandardResult.None;

	private bool _isEnabled = true;

	private bool _isDefault;

	/// <summary>
	/// Gets or sets the Text associated with the TaskDialog control
	/// </summary>
	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<string>((DirectPropertyBase<string>)(object)TextProperty, ref _text, value);
		}
	}

	/// <summary>
	/// Gets or sets the dialog result associated with the control
	/// </summary>
	/// <remarks>
	/// Can be any of <see cref="T:FluentAvalonia.UI.Controls.FATaskDialogStandardResult" /> or any custom name. NOTE:
	/// that 'null' is not a valid result and will be converted to <see cref="F:FluentAvalonia.UI.Controls.FATaskDialogStandardResult.None" />
	/// </remarks>
	public object DialogResult
	{
		get
		{
			return _dialogResult;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<object>((DirectPropertyBase<object>)(object)DialogResultProperty, ref _dialogResult, value);
		}
	}

	/// <summary>
	/// Gets or sets whether the control is enabled
	/// </summary>
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<bool>((DirectPropertyBase<bool>)(object)IsEnabledProperty, ref _isEnabled, value);
		}
	}

	/// <summary>
	/// Gets or sets whether the control is the default button/command.
	/// </summary>
	/// <remarks>
	/// This can only be set on one button/command in a TaskDialog or an error will be thrown.
	/// The desired button will be invoked upon 'Enter' key press and receives first focus
	/// </remarks>
	public bool IsDefault
	{
		get
		{
			return _isDefault;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<bool>((DirectPropertyBase<bool>)(object)IsDefaultProperty, ref _isDefault, value);
		}
	}

	public FATaskDialogControl()
	{
	}

	/// <summary>
	/// Creates a new TaskDialog control with the specified text and dialog result
	/// </summary>
	/// <param name="text">The text the button/command should display</param>
	/// <param name="result">The dialog result the button should return if invoked. Can 
	/// be any of <see cref="T:FluentAvalonia.UI.Controls.FATaskDialogStandardResult" /> or any custom name.
	/// </param>
	public FATaskDialogControl(string text, object result)
	{
		_text = text;
		_dialogResult = result;
	}
}
