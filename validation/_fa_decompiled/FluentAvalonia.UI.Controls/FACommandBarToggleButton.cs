using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.LogicalTree;
using FluentAvalonia.UI.Input;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a button control that can switch states and be displayed in a CommandBar.
/// </summary>
public class FACommandBarToggleButton : ToggleButton, IFACommandBarElement
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarToggleButton.IsInOverflow" /> property
	/// </summary>
	public static readonly DirectProperty<FACommandBarToggleButton, bool> IsInOverflowProperty = AvaloniaProperty.RegisterDirect<FACommandBarToggleButton, bool>("IsInOverflow", (Func<FACommandBarToggleButton, bool>)((FACommandBarToggleButton x) => x.IsInOverflow), (Action<FACommandBarToggleButton, bool>)null, false, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarToggleButton.IconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> IconSourceProperty = FASettingsExpander.IconSourceProperty.AddOwner<FACommandBarButton>((StyledPropertyMetadata<FAIconSource>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarToggleButton.Label" /> property
	/// </summary>
	public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<FACommandBarToggleButton, string>("Label", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarToggleButton.DynamicOverflowOrder" /> property
	/// </summary>
	public static readonly DirectProperty<FACommandBarToggleButton, int> DynamicOverflowOrderProperty = AvaloniaProperty.RegisterDirect<FACommandBarToggleButton, int>("DynamicOverflowOrder", (Func<FACommandBarToggleButton, int>)((FACommandBarToggleButton x) => x.DynamicOverflowOrder), (Action<FACommandBarToggleButton, int>)delegate(FACommandBarToggleButton x, int v)
	{
		x.DynamicOverflowOrder = v;
	}, 0, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarToggleButton.IsCompact" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsCompactProperty = AvaloniaProperty.Register<FACommandBarToggleButton, bool>("IsCompact", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarToggleButton.TemplateSettings" /> property
	/// </summary>
	public static readonly StyledProperty<FACommandBarButtonTemplateSettings> TemplateSettingsProperty = AvaloniaProperty.Register<FACommandBarToggleButton, FACommandBarButtonTemplateSettings>("TemplateSettings", (FACommandBarButtonTemplateSettings)null, false, (BindingMode)1, (Func<FACommandBarButtonTemplateSettings, bool>)null, (Func<AvaloniaObject, FACommandBarButtonTemplateSettings, FACommandBarButtonTemplateSettings>)null, false);

	private bool _isInOverflow;

	private int _dynamicOverflowOrder;

	public bool IsCompact
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsCompactProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsCompactProperty, value, (BindingPriority)0);
		}
	}

	public bool IsInOverflow
	{
		get
		{
			return _isInOverflow;
		}
		internal set
		{
			if (((AvaloniaObject)this).SetAndRaise<bool>((DirectPropertyBase<bool>)(object)IsInOverflowProperty, ref _isInOverflow, value))
			{
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":overflow", value);
			}
		}
	}

	/// <summary>
	/// Gets or sets the graphic content of the command bar toggle button.
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
	/// Gets or sets the text description displayed on the command bar toggle button.
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

	public int DynamicOverflowOrder
	{
		get
		{
			return _dynamicOverflowOrder;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<int>((DirectPropertyBase<int>)(object)DynamicOverflowOrderProperty, ref _dynamicOverflowOrder, value);
		}
	}

	/// <summary>
	/// Gets the template settings for this CommandBarButton
	/// </summary>
	public FACommandBarButtonTemplateSettings TemplateSettings
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FACommandBarButtonTemplateSettings>(TemplateSettingsProperty);
		}
		private set
		{
			((AvaloniaObject)this).SetValue<FACommandBarButtonTemplateSettings>(TemplateSettingsProperty, value, (BindingPriority)0);
		}
	}

	protected override Type StyleKeyOverride => typeof(FACommandBarToggleButton);

	public FACommandBarToggleButton()
	{
		TemplateSettings = new FACommandBarButtonTemplateSettings();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((ToggleButton)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)IconSourceProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":icon", change.NewValue != null);
			TemplateSettings.Icon = FAIconHelpers.CreateFromUnknown(AvaloniaPropertyChangedExtensions.GetNewValue<FAIconSource>(change));
		}
		else if (change.Property == (AvaloniaProperty)(object)LabelProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":label", change.NewValue != null);
		}
		else if (change.Property == (AvaloniaProperty)(object)Button.HotKeyProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":hotkey", change.NewValue != null);
		}
		else if (change.Property == (AvaloniaProperty)(object)IsCompactProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":compact", AvaloniaPropertyChangedExtensions.GetNewValue<bool>(change));
		}
		else
		{
			if (!(change.Property == (AvaloniaProperty)(object)Button.CommandProperty))
			{
				return;
			}
			if (change.OldValue is FAXamlUICommand fAXamlUICommand)
			{
				if (Label == fAXamlUICommand.Label)
				{
					Label = null;
				}
				if (((Button)this).HotKey == fAXamlUICommand.HotKey)
				{
					((Button)this).HotKey = null;
				}
				if (ToolTip.GetTip((Control)(object)this).ToString() == fAXamlUICommand.Description)
				{
					ToolTip.SetTip((Control)(object)this, (object)null);
				}
			}
			if (change.NewValue is FAXamlUICommand fAXamlUICommand2)
			{
				if (string.IsNullOrEmpty(Label))
				{
					Label = fAXamlUICommand2.Label;
				}
				IconSource = fAXamlUICommand2.IconSource;
				if (((Button)this).HotKey == (KeyGesture)null)
				{
					((Button)this).HotKey = fAXamlUICommand2.HotKey;
				}
				if (ToolTip.GetTip((Control)(object)this) == null)
				{
					ToolTip.SetTip((Control)(object)this, (object)fAXamlUICommand2.Description);
				}
			}
		}
	}

	protected override void OnClick()
	{
		((ToggleButton)this).OnClick();
		if (IsInOverflow)
		{
			FACommandBar fACommandBar = LogicalExtensions.FindLogicalAncestorOfType<FACommandBar>((ILogical)(object)this, false);
			if (fACommandBar != null)
			{
				fACommandBar.IsOpen = false;
			}
		}
	}

	protected override bool RegisterContentPresenter(ContentPresenter presenter)
	{
		if (((StyledElement)presenter).Name == "ContentPresenter")
		{
			return true;
		}
		return ((ContentControl)this).RegisterContentPresenter(presenter);
	}
}
