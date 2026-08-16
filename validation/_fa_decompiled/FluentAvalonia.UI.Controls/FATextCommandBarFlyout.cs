using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using FluentAvalonia.UI.Input;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a specialized command bar flyout that contains commands for editing text.
/// </summary>
public class FATextCommandBarFlyout : FACommandBarFlyout
{
	private Dictionary<TextControlButtons, IFACommandBarElement> _buttons;

	private WeakReference<Control> _targetLocal;

	public FATextCommandBarFlyout()
	{
		((PopupFlyoutBase)this).Opening += delegate
		{
			UpdateButtons();
		};
		((FlyoutBase)this).Opened += delegate
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Invalid comparison between Unknown and I4
			_targetLocal = new WeakReference<Control>(((FlyoutBase)this).Target);
			bool flag = (int)((PopupFlyoutBase)this).ShowMode == 0;
			if ((base.PrimaryCommands.Count == 0 && base.SecondaryCommands.Count == 0) || (!_commandBar.IsOpen && !flag))
			{
				((FlyoutBase)this).Hide();
			}
		};
	}

	private void InitializeButtonWithUICommand(Button b, FAXamlUICommand command, Action executeFunc)
	{
		command.ExecuteRequested += delegate
		{
			executeFunc();
		};
		b.Command = command;
	}

	private void UpdateButtons()
	{
		((ICollection<IFACommandBarElement>)base.PrimaryCommands).Clear();
		((ICollection<IFACommandBarElement>)base.SecondaryCommands).Clear();
		TextControlButtons buttonsToAdd = GetButtonsToAdd();
		addButtonToCommandsIfPresent(TextControlButtons.Cut, (IList<IFACommandBarElement>)base.SecondaryCommands);
		addButtonToCommandsIfPresent(TextControlButtons.Copy, (IList<IFACommandBarElement>)base.SecondaryCommands);
		addButtonToCommandsIfPresent(TextControlButtons.Paste, (IList<IFACommandBarElement>)base.SecondaryCommands);
		addButtonToCommandsIfPresent(TextControlButtons.Undo, (IList<IFACommandBarElement>)base.SecondaryCommands);
		addButtonToCommandsIfPresent(TextControlButtons.Redo, (IList<IFACommandBarElement>)base.SecondaryCommands);
		addButtonToCommandsIfPresent(TextControlButtons.SelectAll, (IList<IFACommandBarElement>)base.SecondaryCommands);
		void addButtonToCommandsIfPresent(TextControlButtons buttonType, IList<IFACommandBarElement> commandsList)
		{
			if ((buttonsToAdd & buttonType) != TextControlButtons.None)
			{
				commandsList.Add(GetButton(buttonType));
			}
		}
	}

	private TextControlButtons GetButtonsToAdd()
	{
		TextControlButtons result = TextControlButtons.None;
		Control target = ((FlyoutBase)this).Target;
		TextBox val = (TextBox)(object)((target is TextBox) ? target : null);
		if (val != null)
		{
			result = ((val.PasswordChar == '\0') ? GetTextBoxButtonsToAdd(val) : GetPasswordBoxButtonsToAdd(val));
		}
		else
		{
			TextBlock val2 = (TextBlock)(object)((target is TextBlock) ? target : null);
			if (val2 != null)
			{
				result = GetTextBlockButtonsToAdd(val2);
			}
		}
		return result;
	}

	private TextControlButtons GetTextBoxButtonsToAdd(TextBox textBox)
	{
		TextControlButtons textControlButtons = TextControlButtons.None;
		int num = Math.Abs(textBox.SelectionEnd - textBox.SelectionStart);
		if (!textBox.IsReadOnly)
		{
			if (num > 0)
			{
				textControlButtons |= TextControlButtons.Cut;
			}
			if (textBox.CanPaste)
			{
				textControlButtons |= TextControlButtons.Paste;
			}
			if (textBox.CanUndo)
			{
				textControlButtons |= TextControlButtons.Undo;
			}
			if (textBox.CanRedo)
			{
				textControlButtons |= TextControlButtons.Undo;
			}
		}
		if (num > 0)
		{
			textControlButtons |= TextControlButtons.Copy;
		}
		if (!string.IsNullOrEmpty(textBox.Text) && textBox.Text.Length > 0)
		{
			textControlButtons |= TextControlButtons.SelectAll;
		}
		return textControlButtons;
	}

	private TextControlButtons GetTextBlockButtonsToAdd(TextBlock tb)
	{
		TextControlButtons textControlButtons = TextControlButtons.None;
		SelectableTextBlock val = (SelectableTextBlock)(object)((tb is SelectableTextBlock) ? tb : null);
		if (val != null)
		{
			if (Math.Abs(val.SelectionEnd - val.SelectionStart) > 0)
			{
				textControlButtons |= TextControlButtons.Copy;
			}
			if (!string.IsNullOrEmpty(((TextBlock)val).Text) && ((TextBlock)val).Text.Length > 0)
			{
				textControlButtons |= TextControlButtons.SelectAll;
			}
		}
		else
		{
			textControlButtons |= TextControlButtons.Copy;
		}
		return textControlButtons;
	}

	private TextControlButtons GetPasswordBoxButtonsToAdd(TextBox textBox)
	{
		TextControlButtons textControlButtons = TextControlButtons.None;
		if (textBox.CanPaste)
		{
			textControlButtons |= TextControlButtons.Paste;
		}
		if (!string.IsNullOrEmpty(textBox.Text) && textBox.Text.Length > 0)
		{
			textControlButtons |= TextControlButtons.SelectAll;
		}
		return textControlButtons;
	}

	private bool IsButtonInPrimaryCommands(TextControlButtons button)
	{
		return ((ICollection<IFACommandBarElement>)base.PrimaryCommands).Contains(GetButton(button));
	}

	private void ExecuteCutCommand()
	{
		if (!_targetLocal.TryGetTarget(out var target))
		{
			return;
		}
		try
		{
			TextBox val = (TextBox)(object)((target is TextBox) ? target : null);
			if (val != null)
			{
				val.Cut();
			}
		}
		catch
		{
		}
		if (IsButtonInPrimaryCommands(TextControlButtons.Cut))
		{
			UpdateButtons();
		}
	}

	private async void ExecuteCopyCommand()
	{
		if (!_targetLocal.TryGetTarget(out var target))
		{
			return;
		}
		try
		{
			TextBox val = (TextBox)(object)((target is TextBox) ? target : null);
			if (val != null)
			{
				val.Copy();
			}
			else
			{
				SelectableTextBlock val2 = (SelectableTextBlock)(object)((target is SelectableTextBlock) ? target : null);
				if (val2 != null)
				{
					val2.Copy();
				}
				else
				{
					TextBlock val3 = (TextBlock)(object)((target is TextBlock) ? target : null);
					if (val3 != null)
					{
						await ClipboardExtensions.SetTextAsync(TopLevel.GetTopLevel((Visual)(object)((FlyoutBase)this).Target).Clipboard, val3.Text);
					}
				}
			}
		}
		catch
		{
		}
		if (IsButtonInPrimaryCommands(TextControlButtons.Copy))
		{
			UpdateButtons();
		}
	}

	private async void ExecutePasteCommand()
	{
		if (!_targetLocal.TryGetTarget(out var target))
		{
			return;
		}
		try
		{
			TextBox val = (TextBox)(object)((target is TextBox) ? target : null);
			if (val != null)
			{
				val.Paste();
			}
			else
			{
				TextBlock txtB = (TextBlock)(object)((target is TextBlock) ? target : null);
				if (txtB != null)
				{
					string text = await ClipboardExtensions.TryGetTextAsync(TopLevel.GetTopLevel((Visual)(object)target).Clipboard);
					if (text != null)
					{
						txtB.Text = text;
					}
				}
			}
		}
		catch
		{
		}
		if (IsButtonInPrimaryCommands(TextControlButtons.Paste))
		{
			UpdateButtons();
		}
	}

	private void ExecuteBoldCommand()
	{
	}

	private void ExecuteItalicCommand()
	{
	}

	private void ExecuteUnderlineCommand()
	{
	}

	private void ExecuteUndoCommand()
	{
		if (_targetLocal.TryGetTarget(out var target))
		{
			TextBox val = (TextBox)(object)((target is TextBox) ? target : null);
			if (val != null)
			{
				val.Undo();
			}
		}
		if (IsButtonInPrimaryCommands(TextControlButtons.Undo))
		{
			UpdateButtons();
		}
	}

	private void ExecuteRedoCommand()
	{
		if (_targetLocal.TryGetTarget(out var target))
		{
			TextBox val = (TextBox)(object)((target is TextBox) ? target : null);
			if (val != null)
			{
				val.Redo();
			}
		}
		if (IsButtonInPrimaryCommands(TextControlButtons.Redo))
		{
			UpdateButtons();
		}
	}

	private void ExecuteSelectAllCommand()
	{
		if (_targetLocal.TryGetTarget(out var target))
		{
			TextBox val = (TextBox)(object)((target is TextBox) ? target : null);
			if (val != null)
			{
				val.SelectAll();
				goto IL_0031;
			}
		}
		SelectableTextBlock val2 = (SelectableTextBlock)(object)((target is SelectableTextBlock) ? target : null);
		if (val2 != null)
		{
			val2.SelectAll();
		}
		goto IL_0031;
		IL_0031:
		if (IsButtonInPrimaryCommands(TextControlButtons.SelectAll))
		{
			UpdateButtons();
		}
	}

	private IFACommandBarElement GetButton(TextControlButtons textControlButton)
	{
		if (_buttons == null)
		{
			_buttons = new Dictionary<TextControlButtons, IFACommandBarElement>();
		}
		if (_buttons.ContainsKey(textControlButton))
		{
			return _buttons[textControlButton];
		}
		switch (textControlButton)
		{
		case TextControlButtons.Cut:
		{
			FACommandBarButton fACommandBarButton6 = new FACommandBarButton();
			InitializeButtonWithUICommand((Button)(object)fACommandBarButton6, new FAStandardUICommand(FAStandardUICommandKind.Cut), ExecuteCutCommand);
			_buttons.Add(TextControlButtons.Cut, fACommandBarButton6);
			return fACommandBarButton6;
		}
		case TextControlButtons.Copy:
		{
			FACommandBarButton fACommandBarButton5 = new FACommandBarButton();
			InitializeButtonWithUICommand((Button)(object)fACommandBarButton5, new FAStandardUICommand(FAStandardUICommandKind.Copy), ExecuteCopyCommand);
			_buttons.Add(TextControlButtons.Copy, fACommandBarButton5);
			return fACommandBarButton5;
		}
		case TextControlButtons.Paste:
		{
			FACommandBarButton fACommandBarButton4 = new FACommandBarButton();
			InitializeButtonWithUICommand((Button)(object)fACommandBarButton4, new FAStandardUICommand(FAStandardUICommandKind.Paste), ExecutePasteCommand);
			_buttons.Add(TextControlButtons.Paste, fACommandBarButton4);
			return fACommandBarButton4;
		}
		case TextControlButtons.Bold:
		case TextControlButtons.Italic:
		case TextControlButtons.Underline:
			return null;
		case TextControlButtons.Undo:
		{
			FACommandBarButton fACommandBarButton3 = new FACommandBarButton();
			InitializeButtonWithUICommand((Button)(object)fACommandBarButton3, new FAStandardUICommand(FAStandardUICommandKind.Undo), ExecuteUndoCommand);
			_buttons.Add(TextControlButtons.Undo, fACommandBarButton3);
			return fACommandBarButton3;
		}
		case TextControlButtons.Redo:
		{
			FACommandBarButton fACommandBarButton2 = new FACommandBarButton();
			InitializeButtonWithUICommand((Button)(object)fACommandBarButton2, new FAStandardUICommand(FAStandardUICommandKind.Redo), ExecuteRedoCommand);
			_buttons.Add(TextControlButtons.Redo, fACommandBarButton2);
			return fACommandBarButton2;
		}
		case TextControlButtons.SelectAll:
		{
			FACommandBarButton fACommandBarButton = new FACommandBarButton();
			InitializeButtonWithUICommand((Button)(object)fACommandBarButton, new FAStandardUICommand(FAStandardUICommandKind.SelectAll), ExecuteSelectAllCommand);
			_buttons.Add(TextControlButtons.SelectAll, fACommandBarButton);
			return fACommandBarButton;
		}
		default:
			throw new NotSupportedException("Invalid TextControlButtons");
		}
	}
}
