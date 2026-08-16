using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.LogicalTree;
using FluentAvalonia.UI.Input;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a templated button control to be displayed in an <see cref="T:FluentAvalonia.UI.Controls.FACommandBar" />.
/// </summary>
/// <summary>
/// Represents a templated button control to be displayed in an <see cref="T:FluentAvalonia.UI.Controls.FACommandBar" />
/// </summary>
[PseudoClasses(new string[] { ":icon", ":label", ":compact" })]
[PseudoClasses(new string[] { ":flyout", ":submenuopen", ":overflow" })]
[PseudoClasses(new string[] { ":hotkey" })]
public class FACommandBarButton : Button, IFACommandBarElement
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarButton.IsInOverflow" /> property
	/// </summary>
	public static readonly DirectProperty<FACommandBarButton, bool> IsInOverflowProperty = AvaloniaProperty.RegisterDirect<FACommandBarButton, bool>("IsInOverflow", (Func<FACommandBarButton, bool>)((FACommandBarButton x) => x.IsInOverflow), (Action<FACommandBarButton, bool>)null, false, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarButton.IconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> IconSourceProperty = FASettingsExpander.IconSourceProperty.AddOwner<FACommandBarButton>((StyledPropertyMetadata<FAIconSource>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarButton.Label" /> property
	/// </summary>
	public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<FACommandBarButton, string>("Label", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarButton.DynamicOverflowOrder" /> property
	/// </summary>
	public static readonly DirectProperty<FACommandBarButton, int> DynamicOverflowOrderProperty = AvaloniaProperty.RegisterDirect<FACommandBarButton, int>("DynamicOverflowOrder", (Func<FACommandBarButton, int>)((FACommandBarButton x) => x.DynamicOverflowOrder), (Action<FACommandBarButton, int>)delegate(FACommandBarButton x, int v)
	{
		x.DynamicOverflowOrder = v;
	}, 0, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarButton.IsCompact" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsCompactProperty = AvaloniaProperty.Register<FACommandBarButton, bool>("IsCompact", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarButton.TemplateSettings" /> property
	/// </summary>
	public static readonly StyledProperty<FACommandBarButtonTemplateSettings> TemplateSettingsProperty = AvaloniaProperty.Register<FACommandBarButton, FACommandBarButtonTemplateSettings>("TemplateSettings", (FACommandBarButtonTemplateSettings)null, false, (BindingMode)1, (Func<FACommandBarButtonTemplateSettings, bool>)null, (Func<AvaloniaObject, FACommandBarButtonTemplateSettings, FACommandBarButtonTemplateSettings>)null, false);

	private bool _isInOverflow;

	private int _dynamicOverflowOrder;

	private const string s_pcSubmenuOpen = ":submenuopen";

	protected override Type StyleKeyOverride => typeof(FACommandBarButton);

	/// <summary>
	/// Gets or sets a value that indicates whether the button is shown with no label and reduced padding.
	/// </summary>
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

	/// <summary>
	/// Gets a value that indicates whether this item is in the overflow menu.
	/// </summary>
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
	/// Gets or sets the graphic content of the app bar toggle button.
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
	/// Gets or sets the text description displayed on the app bar toggle button.
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

	/// <inheritdoc />
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

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FluentAvalonia.UI.Controls.FACommandBarButton" /> class.
	/// </summary>
	public FACommandBarButton()
	{
		TemplateSettings = new FACommandBarButtonTemplateSettings();
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((Button)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)IconSourceProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":icon", change.NewValue != null);
			TemplateSettings.Icon = FAIconHelpers.CreateFromUnknown(AvaloniaPropertyChangedExtensions.GetNewValue<FAIconSource>(change));
		}
		else if (change.Property == (AvaloniaProperty)(object)LabelProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":label", change.NewValue != null);
		}
		else if (change.Property == (AvaloniaProperty)(object)Button.FlyoutProperty)
		{
			object oldValue = change.OldValue;
			FlyoutBase val = (FlyoutBase)((oldValue is FlyoutBase) ? oldValue : null);
			if (val != null)
			{
				val.Closed -= OnFlyoutClosed;
				val.Opened -= OnFlyoutOpened;
			}
			object newValue = change.NewValue;
			FlyoutBase val2 = (FlyoutBase)((newValue is FlyoutBase) ? newValue : null);
			if (val2 != null)
			{
				val2.Closed += OnFlyoutClosed;
				val2.Opened += OnFlyoutOpened;
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":flyout", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":submenuopen", val2.IsOpen);
			}
			else
			{
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":flyout", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":submenuopen", false);
			}
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
		((Button)this).OnClick();
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

	private void OnFlyoutOpened(object sender, EventArgs e)
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":submenuopen", true);
	}

	private void OnFlyoutClosed(object sender, EventArgs e)
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":submenuopen", false);
	}
}
