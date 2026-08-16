using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Displays the content of a <see cref="T:FluentAvalonia.UI.Controls.FAMenuFlyout" /> control.
/// </summary>
[PseudoClasses(new string[] { ":icons", ":toggle" })]
public class FAMenuFlyoutPresenter : ItemsControl
{
	private FAMenuFlyoutItemBase _openingItem;

	private FAMenuFlyoutSubItem _openedItem;

	private IDisposable _closingCancelDisp;

	private int _iconCount;

	private int _toggleCount;

	private const string s_pcIcons = ":icons";

	private const string s_pcToggle = ":toggle";

	internal AvaloniaObject InternalParent { get; set; }

	public FAMenuFlyoutPresenter()
	{
		KeyboardNavigation.SetTabNavigation((InputElement)(object)this, (KeyboardNavigationMode)1);
	}

	protected override bool NeedsContainerOverride(object item, int index, out object recycleKey)
	{
		recycleKey = typeof(FAMenuFlyoutItem);
		return !(item is FAMenuFlyoutItemBase);
	}

	protected override Control CreateContainerForItemOverride(object item, int index, object recycleKey)
	{
		if (((ITemplate<object, Control>)(object)DataTemplateExtensions.FindDataTemplate((Control)(object)this, item, ((ItemsControl)this).ItemTemplate))?.Build(item) is FAMenuFlyoutItemBase fAMenuFlyoutItemBase)
		{
			fAMenuFlyoutItemBase.IsContainerFromTemplate = true;
			return (Control)(object)fAMenuFlyoutItemBase;
		}
		return (Control)(object)new FAMenuFlyoutItem
		{
			Text = item.ToString()
		};
	}

	protected override void PrepareContainerForItemOverride(Control element, object item, int index)
	{
		FAMenuFlyoutItemBase obj = element as FAMenuFlyoutItemBase;
		if (!obj.IsContainerFromTemplate)
		{
			((ItemsControl)this).PrepareContainerForItemOverride(element, item, index);
		}
		obj.InternalParent = this;
		int num = _iconCount;
		int num2 = _toggleCount;
		if (element is FAToggleMenuFlyoutItem fAToggleMenuFlyoutItem)
		{
			if (fAToggleMenuFlyoutItem.IconSource != null)
			{
				num++;
			}
			num2++;
		}
		else if (element is FARadioMenuFlyoutItem fARadioMenuFlyoutItem)
		{
			if (fARadioMenuFlyoutItem.IconSource != null)
			{
				num++;
			}
			num2++;
		}
		else if (element is FAMenuFlyoutItem fAMenuFlyoutItem)
		{
			if (fAMenuFlyoutItem.IconSource != null)
			{
				num++;
			}
		}
		else if (element is FAMenuFlyoutSubItem { IconSource: not null })
		{
			num++;
		}
		if (num != _iconCount || _toggleCount != num2)
		{
			_iconCount = num;
			_toggleCount = num2;
			UpdateVisualState();
		}
		PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)element).Classes, ":icons", num != 0);
		PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)element).Classes, ":toggle", num2 != 0);
	}

	protected override void ClearContainerForItemOverride(Control element)
	{
		((ItemsControl)this).ClearContainerForItemOverride(element);
		int num = _iconCount;
		int num2 = _toggleCount;
		if (element is FAToggleMenuFlyoutItem fAToggleMenuFlyoutItem)
		{
			if (fAToggleMenuFlyoutItem.IconSource != null)
			{
				num--;
			}
			num2--;
		}
		else if (element is FARadioMenuFlyoutItem fARadioMenuFlyoutItem)
		{
			if (fARadioMenuFlyoutItem.IconSource != null)
			{
				num--;
			}
			num2--;
		}
		else if (element is FAMenuFlyoutItem fAMenuFlyoutItem)
		{
			if (fAMenuFlyoutItem.IconSource != null)
			{
				num--;
			}
		}
		else if (element is FAMenuFlyoutSubItem { IconSource: not null })
		{
			num--;
		}
		if (num != _iconCount || _toggleCount != num2)
		{
			_iconCount = num;
			_toggleCount = num2;
			UpdateVisualState();
		}
	}

	protected override void OnKeyDown(KeyEventArgs args)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected I4, but got Unknown
		if (((RoutedEventArgs)args).Handled)
		{
			return;
		}
		FAMenuFlyoutItemBase menuItem = GetMenuItem(((RoutedEventArgs)args).Source);
		Key key = args.Key;
		if ((int)key != 6)
		{
			if ((int)key != 13)
			{
				switch (key - 23)
				{
				case 3:
				{
					if (!(TopLevel.GetTopLevel((Visual)(object)this).FocusManager.GetFocusedElement() is FAMenuFlyoutItemBase fAMenuFlyoutItemBase2))
					{
						break;
					}
					int num2 = ((ItemsControl)this).IndexFromContainer((Control)(object)fAMenuFlyoutItemBase2);
					if (num2 == -1)
					{
						return;
					}
					Control val2;
					do
					{
						num2++;
						if (num2 >= ((ItemsControl)this).ItemCount)
						{
							num2 = 0;
						}
						val2 = ((ItemsControl)this).ContainerFromIndex(num2);
						if (val2 != null && !(val2 is FAMenuFlyoutSeparator) && ((InputElement)val2).Focusable && ((InputElement)val2).IsEffectivelyEnabled)
						{
							((InputElement)val2).Focus((NavigationMethod)2, (KeyModifiers)0);
							((RoutedEventArgs)args).Handled = true;
							break;
						}
					}
					while ((object)val2 != menuItem);
					break;
				}
				case 1:
				{
					if (!(TopLevel.GetTopLevel((Visual)(object)this).FocusManager.GetFocusedElement() is FAMenuFlyoutItemBase fAMenuFlyoutItemBase))
					{
						break;
					}
					int num = ((ItemsControl)this).IndexFromContainer((Control)(object)fAMenuFlyoutItemBase);
					if (num == -1)
					{
						return;
					}
					Control val;
					do
					{
						num--;
						if (num < 0)
						{
							num = ((ItemsControl)this).ItemCount - 1;
						}
						val = ((ItemsControl)this).ContainerFromIndex(num);
						if (val != null && !(val is FAMenuFlyoutSeparator) && ((InputElement)val).Focusable && ((InputElement)val).IsEffectivelyEnabled)
						{
							((InputElement)val).Focus((NavigationMethod)2, (KeyModifiers)0);
							((RoutedEventArgs)args).Handled = true;
							break;
						}
					}
					while ((object)val != menuItem);
					break;
				}
				case 2:
					if (menuItem is FAMenuFlyoutSubItem fAMenuFlyoutSubItem2)
					{
						fAMenuFlyoutSubItem2.Open(fromKeyboard: true);
						((RoutedEventArgs)args).Handled = true;
					}
					break;
				case 0:
					if (InternalParent is FAMenuFlyoutSubItem fAMenuFlyoutSubItem)
					{
						((InputElement)fAMenuFlyoutSubItem).Focus((NavigationMethod)2, (KeyModifiers)0);
						fAMenuFlyoutSubItem.Close();
						((RoutedEventArgs)args).Handled = true;
					}
					break;
				}
			}
			else if (InternalParent is FAMenuFlyoutSubItem fAMenuFlyoutSubItem3)
			{
				((InputElement)fAMenuFlyoutSubItem3).Focus((NavigationMethod)2, (KeyModifiers)0);
				fAMenuFlyoutSubItem3.Close();
				((RoutedEventArgs)args).Handled = true;
			}
			else
			{
				CloseMenu();
			}
		}
		else if (TopLevel.GetTopLevel((Visual)(object)this).FocusManager.GetFocusedElement() is FAMenuFlyoutItemBase fAMenuFlyoutItemBase3 && ((InputElement)fAMenuFlyoutItemBase3).Focusable && ((InputElement)fAMenuFlyoutItemBase3).IsEffectivelyEnabled)
		{
			if (fAMenuFlyoutItemBase3 is FAMenuFlyoutSubItem fAMenuFlyoutSubItem4)
			{
				fAMenuFlyoutSubItem4.Open(fromKeyboard: true);
			}
			else
			{
				(fAMenuFlyoutItemBase3 as FAMenuFlyoutItem)?.RaiseClick();
				CloseMenu();
			}
			((RoutedEventArgs)args).Handled = true;
		}
		((ItemsControl)this).OnKeyDown(args);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs args)
	{
		((Control)this).OnPointerReleased(args);
		if (GetMenuItem(((RoutedEventArgs)args).Source) is FAMenuFlyoutItem fAMenuFlyoutItem)
		{
			fAMenuFlyoutItem.RaiseClick();
			CloseMenu();
			((RoutedEventArgs)args).Handled = true;
		}
	}

	internal void PointerEnteredItem(FAMenuFlyoutItemBase item)
	{
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		FAMenuFlyoutSubItem mfsi = item as FAMenuFlyoutSubItem;
		if (mfsi != null)
		{
			if (mfsi == _openedItem)
			{
				_closingCancelDisp?.Dispose();
				return;
			}
			if (_openedItem != null)
			{
				_openedItem.Close();
				_openedItem = null;
			}
			_openingItem = mfsi;
			DispatcherTimer.RunOnce((Action)delegate
			{
				if (_openingItem == mfsi)
				{
					_openingItem = null;
					mfsi.Open();
					_openedItem = mfsi;
				}
			}, TimeSpan.FromMilliseconds(400L), default(DispatcherPriority));
		}
		else if (_openedItem != null)
		{
			_closingCancelDisp = DispatcherTimer.RunOnce((Action)delegate
			{
				_openedItem?.Close();
				_openedItem = null;
			}, TimeSpan.FromMilliseconds(400L), default(DispatcherPriority));
		}
	}

	internal void PointerExitedItem(FAMenuFlyoutItemBase item)
	{
		if (_openingItem == item)
		{
			_openingItem = null;
		}
	}

	internal void MenuOpened(bool fromKeyboard = false)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		Dispatcher.UIThread.Post((Action)delegate
		{
			Control val = (from x in ((ItemsControl)this).GetRealizedContainers()
				where ((InputElement)x).Focusable && ((InputElement)x).IsEffectivelyEnabled
				select x).FirstOrDefault();
			if (val != null)
			{
				((InputElement)val).Focus((NavigationMethod)(fromKeyboard ? 2 : 0), (KeyModifiers)0);
			}
		}, DispatcherPriority.Render);
	}

	internal void MenuClosed()
	{
		_openedItem = null;
		_openingItem = null;
	}

	private FAMenuFlyoutItemBase GetMenuItem(object src)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		return VisualExtensions.FindAncestorOfType<FAMenuFlyoutItemBase>((Visual)src, true);
	}

	internal void CloseMenu()
	{
		if (InternalParent is FAMenuFlyoutSubItem fAMenuFlyoutSubItem)
		{
			fAMenuFlyoutSubItem.Close(isFullClose: true);
		}
		else if (InternalParent is FAMenuFlyout fAMenuFlyout)
		{
			fAMenuFlyout.Close();
		}
	}

	private void UpdateVisualState()
	{
		bool flag = _iconCount > 0;
		bool flag2 = _toggleCount > 0;
		foreach (Control realizedContainer in ((ItemsControl)this).GetRealizedContainers())
		{
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)realizedContainer).Classes, ":icons", flag);
			PseudoClassesExtensions.Set((IPseudoClasses)(object)((StyledElement)realizedContainer).Classes, ":toggle", flag2);
		}
	}
}
