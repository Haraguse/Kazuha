using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// CommandBar used for a <see cref="T:FluentAvalonia.UI.Controls.FACommandBarFlyout" />
/// </summary>
/// <remarks>
/// This class should be treated as internal to FluentAvalonia and not used outside of 
/// the CommandBarFlyout implementations.
/// </remarks>
[TemplatePart("MoreButton", typeof(Button))]
public class FACommandBarFlyoutCommandBar : FACommandBar
{
	private List<Control> _horizontallyAccessibleControls;

	private List<Control> _verticallyAccessibleControls;

	private Button _moreButton;

	private FACommandBarFlyout _owningFlyout;

	private const string s_tpMoreButton = "MoreButton";

	protected override Type StyleKeyOverride => typeof(FACommandBarFlyoutCommandBar);

	public FACommandBarFlyoutCommandBar()
	{
		((Visual)this).AttachedToVisualTree += delegate
		{
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			if (((base.PrimaryCommands.Count > 0) ? base.PrimaryCommands : ((base.SecondaryCommands.Count > 0) ? base.SecondaryCommands : null)) != null)
			{
				Dispatcher.UIThread.Post((Action)delegate
				{
					if (base.PrimaryCommands.Count > 0)
					{
						bool flag = false;
						for (int i = 0; i < base.PrimaryCommands.Count; i++)
						{
							IFACommandBarElement iFACommandBarElement = base.PrimaryCommands[i];
							if (IsControlFocusable((Control)((iFACommandBarElement is Control) ? iFACommandBarElement : null), checkTabStop: false))
							{
								IFACommandBarElement iFACommandBarElement2 = base.PrimaryCommands[i];
								((InputElement)((iFACommandBarElement2 is InputElement) ? iFACommandBarElement2 : null)).Focus((NavigationMethod)0, (KeyModifiers)0);
								flag = true;
								break;
							}
						}
						if (!flag && _moreButton != null && ((Visual)_moreButton).IsVisible)
						{
							((InputElement)_moreButton).Focus((NavigationMethod)0, (KeyModifiers)0);
						}
					}
					else if (_moreButton != null && ((Visual)_moreButton).IsVisible)
					{
						((InputElement)_moreButton).Focus((NavigationMethod)0, (KeyModifiers)0);
					}
				}, DispatcherPriority.Loaded);
			}
		};
		base.Closing += delegate
		{
			if (_owningFlyout != null && ((FlyoutBase)_owningFlyout).IsOpen && _owningFlyout.AlwaysExpanded)
			{
				base.IsOpen = true;
			}
		};
		((INotifyCollectionChanged)base.PrimaryCommands).CollectionChanged += delegate
		{
			PopulateAccessibleControls();
		};
		((INotifyCollectionChanged)base.SecondaryCommands).CollectionChanged += delegate
		{
			PopulateAccessibleControls();
		};
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		_moreButton = NameScopeExtensions.Find<Button>(e.NameScope, "MoreButton");
		PopulateAccessibleControls();
	}

	private void PopulateAccessibleControls()
	{
		if (_horizontallyAccessibleControls == null)
		{
			_horizontallyAccessibleControls = new List<Control>();
			_verticallyAccessibleControls = new List<Control>();
		}
		else
		{
			_horizontallyAccessibleControls.Clear();
			_verticallyAccessibleControls.Clear();
		}
		for (int i = 0; i < base.PrimaryCommands.Count; i++)
		{
			IFACommandBarElement iFACommandBarElement = base.PrimaryCommands[i];
			Control val = (Control)((iFACommandBarElement is Control) ? iFACommandBarElement : null);
			if (val != null)
			{
				_horizontallyAccessibleControls.Add(val);
				_verticallyAccessibleControls.Add(val);
			}
		}
		if (_moreButton != null)
		{
			_horizontallyAccessibleControls.Add((Control)(object)_moreButton);
			_verticallyAccessibleControls.Add((Control)(object)_moreButton);
		}
		for (int j = 0; j < base.SecondaryCommands.Count; j++)
		{
			IFACommandBarElement iFACommandBarElement2 = base.SecondaryCommands[j];
			Control val2 = (Control)((iFACommandBarElement2 is Control) ? iFACommandBarElement2 : null);
			if (val2 != null)
			{
				_verticallyAccessibleControls.Add(val2);
			}
		}
	}

	protected override void OnKeyDown(KeyEventArgs args)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Invalid comparison between Unknown and I4
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Invalid comparison between Unknown and I4
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Invalid comparison between Unknown and I4
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Invalid comparison between Unknown and I4
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		if (((RoutedEventArgs)args).Handled)
		{
			return;
		}
		Key key = args.Key;
		if ((int)key != 3)
		{
			if (key - 23 <= 3)
			{
				bool num = (int)args.Key == 23;
				_ = args.Key;
				bool flag = (int)args.Key == 24;
				bool flag2 = (int)args.Key == 26;
				List<Control> list = ((flag | flag2) ? _verticallyAccessibleControls : _horizontallyAccessibleControls);
				int num2 = ((num | flag) ? (list.Count - 1) : 0);
				int num3 = ((num | flag) ? (-1) : list.Count);
				int num4 = ((!(num | flag)) ? 1 : (-1));
				bool flag3 = flag | flag2;
				Control val = null;
				int num5 = -1;
				for (int i = num2; ((i != num3) | flag3) || (num5 > 0 && i == num5); i += num4)
				{
					if (i == num3)
					{
						if (val == null)
						{
							break;
						}
						i = num2;
					}
					Control val2 = list[i];
					if (val == null)
					{
						if (((InputElement)val2).IsFocused)
						{
							val = val2;
							num5 = i;
						}
					}
					else if (IsControlFocusable(val2, checkTabStop: false))
					{
						if (val2 is IFACommandBarElement item && ((ICollection<IFACommandBarElement>)base.SecondaryCommands).Contains(item) && !base.IsOpen)
						{
							base.IsOpen = true;
						}
						((InputElement)val2).Focus((NavigationMethod)2, (KeyModifiers)0);
						((RoutedEventArgs)args).Handled = true;
						break;
					}
				}
				if (!((RoutedEventArgs)args).Handled)
				{
					((RoutedEventArgs)args).Handled = true;
				}
			}
		}
		else
		{
			IInputElement focusedElement = TopLevel.GetTopLevel((Visual)(object)((FlyoutBase)_owningFlyout).Target).FocusManager.GetFocusedElement();
			if ((object)focusedElement == _moreButton)
			{
				if (base.SecondaryCommands.Count > 0 && !base.IsOpen)
				{
					base.IsOpen = true;
				}
				for (int j = 0; j < base.SecondaryCommands.Count; j++)
				{
					IFACommandBarElement iFACommandBarElement = base.SecondaryCommands[j];
					if (IsControlFocusable((Control)((iFACommandBarElement is Control) ? iFACommandBarElement : null), checkTabStop: false))
					{
						IFACommandBarElement iFACommandBarElement2 = base.SecondaryCommands[j];
						((InputElement)((iFACommandBarElement2 is InputElement) ? iFACommandBarElement2 : null)).Focus((NavigationMethod)1, (KeyModifiers)0);
						((RoutedEventArgs)args).Handled = true;
						break;
					}
				}
			}
			if (!((RoutedEventArgs)args).Handled && focusedElement != null)
			{
				if (((ICollection<IFACommandBarElement>)base.PrimaryCommands).Contains(focusedElement as IFACommandBarElement))
				{
					bool num6 = !base.IsOpen;
					if (base.SecondaryCommands.Count > 0 && !base.IsOpen)
					{
						base.IsOpen = true;
					}
					if (num6)
					{
						Dispatcher.UIThread.Post((Action)FocusFirstSecondary, DispatcherPriority.Render);
					}
					else
					{
						FocusFirstSecondary();
					}
				}
				else if (((ICollection<IFACommandBarElement>)base.SecondaryCommands).Contains(focusedElement as IFACommandBarElement))
				{
					for (int k = 0; k < base.PrimaryCommands.Count; k++)
					{
						IFACommandBarElement iFACommandBarElement3 = base.PrimaryCommands[k];
						if (IsControlFocusable((Control)((iFACommandBarElement3 is Control) ? iFACommandBarElement3 : null), checkTabStop: false))
						{
							IFACommandBarElement iFACommandBarElement4 = base.PrimaryCommands[k];
							((InputElement)((iFACommandBarElement4 is InputElement) ? iFACommandBarElement4 : null)).Focus((NavigationMethod)1, (KeyModifiers)0);
							((RoutedEventArgs)args).Handled = true;
							break;
						}
					}
					if (!((RoutedEventArgs)args).Handled && _moreButton != null && ((Visual)_moreButton).IsVisible)
					{
						((InputElement)_moreButton).Focus((NavigationMethod)1, (KeyModifiers)0);
						((RoutedEventArgs)args).Handled = true;
					}
				}
			}
		}
		((InputElement)this).OnKeyDown(args);
		void FocusFirstSecondary()
		{
			for (int l = 0; l < base.SecondaryCommands.Count; l++)
			{
				FACommandBarFlyoutCommandBar fACommandBarFlyoutCommandBar = this;
				IFACommandBarElement iFACommandBarElement5 = base.SecondaryCommands[l];
				if (fACommandBarFlyoutCommandBar.IsControlFocusable((Control)((iFACommandBarElement5 is Control) ? iFACommandBarElement5 : null), checkTabStop: false))
				{
					IFACommandBarElement iFACommandBarElement6 = base.SecondaryCommands[l];
					((InputElement)((iFACommandBarElement6 is InputElement) ? iFACommandBarElement6 : null)).Focus((NavigationMethod)1, (KeyModifiers)0);
					((RoutedEventArgs)args).Handled = true;
					break;
				}
			}
		}
	}

	private bool IsControlFocusable(Control control, bool checkTabStop)
	{
		if (control != null && ((Visual)control).IsVisible && ((InputElement)control).IsEnabled)
		{
			return ((InputElement)control).Focusable;
		}
		return false;
	}

	internal void SetOwningFlyout(FACommandBarFlyout f)
	{
		_owningFlyout = f;
	}
}
