using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Dialogs.Internal;

namespace CompiledAvaloniaXaml;

[CompilerGenerated]
internal class XamlIlTrampolines
{
	public static void Avalonia_002EDialogs_003AAvalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002BEnterPressed_0_0021CommandExecuteTrampoline(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((ManagedFileChooserViewModel)P_0).EnterPressed();
	}

	public static void Avalonia_002EDialogs_003AAvalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002BOk_0_0021CommandExecuteTrampoline(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((ManagedFileChooserViewModel)P_0).Ok();
	}

	public static bool Avalonia_002EDialogs_003AAvalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002BCanOk_0021CommandCanExecuteTrampoline(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return /*tail.*/((ManagedFileChooserViewModel)P_0).CanOk(P_1);
	}

	public static void Avalonia_002EDialogs_003AAvalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002BCancel_0_0021CommandExecuteTrampoline(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((ManagedFileChooserViewModel)P_0).Cancel();
	}

	public static void Avalonia_002EControls_003AAvalonia_002EControls_002ETextBox_002BClear_0_0021CommandExecuteTrampoline(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((TextBox)P_0).Clear();
	}
}
