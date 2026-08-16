using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Converters;
using Avalonia.Controls.Notifications;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Dialogs.Internal;
using Avalonia.Styling;

namespace CompiledAvaloniaXaml;

[CompilerGenerated]
internal class XamlIlHelpers
{
	private static IPropertyInfo Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Field;

	private static IPropertyInfo Avalonia_002ERect_002CAvalonia_002EBase_002EWidth_0021Field;

	private static IPropertyInfo Avalonia_002EStyling_002EControlTheme_002CAvalonia_002EBase_002EBasedOn_0021Field;

	private static IPropertyInfo Avalonia_002EData_002ETemplateBinding_002CAvalonia_002EBase_002EConverter_0021Field;

	private static IPropertyInfo Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EBindings_0021Field;

	private static IPropertyInfo Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002EConverter_0021Field;

	private static IPropertyInfo Avalonia_002ERect_002CAvalonia_002EBase_002EHeight_0021Field;

	private static IPropertyInfo Avalonia_002EControls_002ECommandBar_002CAvalonia_002EControls_002EVisiblePrimaryCommands_0021Field;

	private static IPropertyInfo Avalonia_002EControls_002ECommandBar_002CAvalonia_002EControls_002EOverflowItems_0021Field;

	private static IPropertyInfo Avalonia_002ECollections_002EDataGridCollectionViewGroup_002CAvalonia_002EControls_002EDataGrid_002EKey_0021Field;

	private static IPropertyInfo Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EConverter_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinks_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinksSelectedIndex_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EIconKey_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EDisplayName_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ELocation_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowFilters_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFilters_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedFilter_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFileName_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectingFolder_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowHiddenFiles_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EItems_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectionMode_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedItems_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EModified_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EType_0021Field;

	private static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002ESize_0021Field;

	private static IPropertyInfo Avalonia_002EControls_002EProgressBar_002CAvalonia_002EControls_002ETemplateSettings_0021Field;

	private static IPropertyInfo Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002ESource_0021Field;

	private static IPropertyInfo Avalonia_002EControls_002EConverters_002EMarginMultiplierConverter_002CAvalonia_002EControls_002EIndent_0021Field;

	private static IPropertyInfo Avalonia_002EControls_002ENotifications_002EINotification_002CAvalonia_002EControls_002ETitle_0021Field;

	private static IPropertyInfo Avalonia_002EControls_002ENotifications_002EINotification_002CAvalonia_002EControls_002EMessage_0021Field;

	private static object Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((Setter)P_0).Value;
	}

	private static void Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((Setter)P_0).Value = P_1;
	}

	public static IPropertyInfo Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Field != null)
		{
			return Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Field;
		}
		Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Field = (IPropertyInfo)new ClrPropertyInfo("Value", (Func<object, object>)Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Getter, (Action<object, object>)Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Setter, typeof(object));
		return Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Field;
	}

	private static object Avalonia_002ERect_002CAvalonia_002EBase_002EWidth_0021Getter(object P_0)
	{
		return ((Rect)(ref (Rect)P_0)).Width;
	}

	public static IPropertyInfo Avalonia_002ERect_002CAvalonia_002EBase_002EWidth_0021Property()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (Avalonia_002ERect_002CAvalonia_002EBase_002EWidth_0021Field != null)
		{
			return Avalonia_002ERect_002CAvalonia_002EBase_002EWidth_0021Field;
		}
		Avalonia_002ERect_002CAvalonia_002EBase_002EWidth_0021Field = (IPropertyInfo)new ClrPropertyInfo("Width", (Func<object, object>)Avalonia_002ERect_002CAvalonia_002EBase_002EWidth_0021Getter, (Action<object, object>)null, typeof(double));
		return Avalonia_002ERect_002CAvalonia_002EBase_002EWidth_0021Field;
	}

	private static object Avalonia_002EStyling_002EControlTheme_002CAvalonia_002EBase_002EBasedOn_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ControlTheme)P_0).BasedOn;
	}

	private static void Avalonia_002EStyling_002EControlTheme_002CAvalonia_002EBase_002EBasedOn_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		((ControlTheme)P_0).BasedOn = (ControlTheme)P_1;
	}

	public static IPropertyInfo Avalonia_002EStyling_002EControlTheme_002CAvalonia_002EBase_002EBasedOn_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EStyling_002EControlTheme_002CAvalonia_002EBase_002EBasedOn_0021Field != null)
		{
			return Avalonia_002EStyling_002EControlTheme_002CAvalonia_002EBase_002EBasedOn_0021Field;
		}
		Avalonia_002EStyling_002EControlTheme_002CAvalonia_002EBase_002EBasedOn_0021Field = (IPropertyInfo)new ClrPropertyInfo("BasedOn", (Func<object, object>)Avalonia_002EStyling_002EControlTheme_002CAvalonia_002EBase_002EBasedOn_0021Getter, (Action<object, object>)Avalonia_002EStyling_002EControlTheme_002CAvalonia_002EBase_002EBasedOn_0021Setter, typeof(ControlTheme));
		return Avalonia_002EStyling_002EControlTheme_002CAvalonia_002EBase_002EBasedOn_0021Field;
	}

	private static object Avalonia_002EData_002ETemplateBinding_002CAvalonia_002EBase_002EConverter_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((TemplateBinding)P_0).Converter;
	}

	private static void Avalonia_002EData_002ETemplateBinding_002CAvalonia_002EBase_002EConverter_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		((TemplateBinding)P_0).Converter = (IValueConverter)P_1;
	}

	public static IPropertyInfo Avalonia_002EData_002ETemplateBinding_002CAvalonia_002EBase_002EConverter_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EData_002ETemplateBinding_002CAvalonia_002EBase_002EConverter_0021Field != null)
		{
			return Avalonia_002EData_002ETemplateBinding_002CAvalonia_002EBase_002EConverter_0021Field;
		}
		Avalonia_002EData_002ETemplateBinding_002CAvalonia_002EBase_002EConverter_0021Field = (IPropertyInfo)new ClrPropertyInfo("Converter", (Func<object, object>)Avalonia_002EData_002ETemplateBinding_002CAvalonia_002EBase_002EConverter_0021Getter, (Action<object, object>)Avalonia_002EData_002ETemplateBinding_002CAvalonia_002EBase_002EConverter_0021Setter, typeof(IValueConverter));
		return Avalonia_002EData_002ETemplateBinding_002CAvalonia_002EBase_002EConverter_0021Field;
	}

	private static object Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EBindings_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((MultiBinding)P_0).Bindings;
	}

	private static void Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EBindings_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((MultiBinding)P_0).Bindings = (IList<BindingBase>)P_1;
	}

	public static IPropertyInfo Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EBindings_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EBindings_0021Field != null)
		{
			return Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EBindings_0021Field;
		}
		Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EBindings_0021Field = (IPropertyInfo)new ClrPropertyInfo("Bindings", (Func<object, object>)Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EBindings_0021Getter, (Action<object, object>)Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EBindings_0021Setter, typeof(IList<BindingBase>));
		return Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EBindings_0021Field;
	}

	private static object Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002EConverter_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((CompiledBinding)P_0).Converter;
	}

	private static void Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002EConverter_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		((CompiledBinding)P_0).Converter = (IValueConverter)P_1;
	}

	public static IPropertyInfo Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002EConverter_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002EConverter_0021Field != null)
		{
			return Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002EConverter_0021Field;
		}
		Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002EConverter_0021Field = (IPropertyInfo)new ClrPropertyInfo("Converter", (Func<object, object>)Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002EConverter_0021Getter, (Action<object, object>)Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002EConverter_0021Setter, typeof(IValueConverter));
		return Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002EConverter_0021Field;
	}

	private static object Avalonia_002ERect_002CAvalonia_002EBase_002EHeight_0021Getter(object P_0)
	{
		return ((Rect)(ref (Rect)P_0)).Height;
	}

	public static IPropertyInfo Avalonia_002ERect_002CAvalonia_002EBase_002EHeight_0021Property()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (Avalonia_002ERect_002CAvalonia_002EBase_002EHeight_0021Field != null)
		{
			return Avalonia_002ERect_002CAvalonia_002EBase_002EHeight_0021Field;
		}
		Avalonia_002ERect_002CAvalonia_002EBase_002EHeight_0021Field = (IPropertyInfo)new ClrPropertyInfo("Height", (Func<object, object>)Avalonia_002ERect_002CAvalonia_002EBase_002EHeight_0021Getter, (Action<object, object>)null, typeof(double));
		return Avalonia_002ERect_002CAvalonia_002EBase_002EHeight_0021Field;
	}

	private static object Avalonia_002EControls_002ECommandBar_002CAvalonia_002EControls_002EVisiblePrimaryCommands_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((CommandBar)P_0).VisiblePrimaryCommands;
	}

	public static IPropertyInfo Avalonia_002EControls_002ECommandBar_002CAvalonia_002EControls_002EVisiblePrimaryCommands_0021Property()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (Avalonia_002EControls_002ECommandBar_002CAvalonia_002EControls_002EVisiblePrimaryCommands_0021Field != null)
		{
			return Avalonia_002EControls_002ECommandBar_002CAvalonia_002EControls_002EVisiblePrimaryCommands_0021Field;
		}
		Avalonia_002EControls_002ECommandBar_002CAvalonia_002EControls_002EVisiblePrimaryCommands_0021Field = (IPropertyInfo)new ClrPropertyInfo("VisiblePrimaryCommands", (Func<object, object>)Avalonia_002EControls_002ECommandBar_002CAvalonia_002EControls_002EVisiblePrimaryCommands_0021Getter, (Action<object, object>)null, typeof(ReadOnlyObservableCollection<ICommandBarElement>));
		return Avalonia_002EControls_002ECommandBar_002CAvalonia_002EControls_002EVisiblePrimaryCommands_0021Field;
	}

	private static object Avalonia_002EControls_002ECommandBar_002CAvalonia_002EControls_002EOverflowItems_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((CommandBar)P_0).OverflowItems;
	}

	public static IPropertyInfo Avalonia_002EControls_002ECommandBar_002CAvalonia_002EControls_002EOverflowItems_0021Property()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (Avalonia_002EControls_002ECommandBar_002CAvalonia_002EControls_002EOverflowItems_0021Field != null)
		{
			return Avalonia_002EControls_002ECommandBar_002CAvalonia_002EControls_002EOverflowItems_0021Field;
		}
		Avalonia_002EControls_002ECommandBar_002CAvalonia_002EControls_002EOverflowItems_0021Field = (IPropertyInfo)new ClrPropertyInfo("OverflowItems", (Func<object, object>)Avalonia_002EControls_002ECommandBar_002CAvalonia_002EControls_002EOverflowItems_0021Getter, (Action<object, object>)null, typeof(ReadOnlyObservableCollection<ICommandBarElement>));
		return Avalonia_002EControls_002ECommandBar_002CAvalonia_002EControls_002EOverflowItems_0021Field;
	}

	private static object Avalonia_002ECollections_002EDataGridCollectionViewGroup_002CAvalonia_002EControls_002EDataGrid_002EKey_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((DataGridCollectionViewGroup)P_0).Key;
	}

	public static IPropertyInfo Avalonia_002ECollections_002EDataGridCollectionViewGroup_002CAvalonia_002EControls_002EDataGrid_002EKey_0021Property()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (Avalonia_002ECollections_002EDataGridCollectionViewGroup_002CAvalonia_002EControls_002EDataGrid_002EKey_0021Field != null)
		{
			return Avalonia_002ECollections_002EDataGridCollectionViewGroup_002CAvalonia_002EControls_002EDataGrid_002EKey_0021Field;
		}
		Avalonia_002ECollections_002EDataGridCollectionViewGroup_002CAvalonia_002EControls_002EDataGrid_002EKey_0021Field = (IPropertyInfo)new ClrPropertyInfo("Key", (Func<object, object>)Avalonia_002ECollections_002EDataGridCollectionViewGroup_002CAvalonia_002EControls_002EDataGrid_002EKey_0021Getter, (Action<object, object>)null, typeof(object));
		return Avalonia_002ECollections_002EDataGridCollectionViewGroup_002CAvalonia_002EControls_002EDataGrid_002EKey_0021Field;
	}

	private static object Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EConverter_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((MultiBinding)P_0).Converter;
	}

	private static void Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EConverter_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		((MultiBinding)P_0).Converter = (IMultiValueConverter)P_1;
	}

	public static IPropertyInfo Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EConverter_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EConverter_0021Field != null)
		{
			return Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EConverter_0021Field;
		}
		Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EConverter_0021Field = (IPropertyInfo)new ClrPropertyInfo("Converter", (Func<object, object>)Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EConverter_0021Getter, (Action<object, object>)Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EConverter_0021Setter, typeof(IMultiValueConverter));
		return Avalonia_002EData_002EMultiBinding_002CAvalonia_002EBase_002EConverter_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinks_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserViewModel)P_0).QuickLinks;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinks_0021Property()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinks_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinks_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinks_0021Field = (IPropertyInfo)new ClrPropertyInfo("QuickLinks", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinks_0021Getter, (Action<object, object>)null, typeof(AvaloniaList<ManagedFileChooserItemViewModel>));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinks_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinksSelectedIndex_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserViewModel)P_0).QuickLinksSelectedIndex;
	}

	private static void Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinksSelectedIndex_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((ManagedFileChooserViewModel)P_0).QuickLinksSelectedIndex = (int)P_1;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinksSelectedIndex_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinksSelectedIndex_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinksSelectedIndex_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinksSelectedIndex_0021Field = (IPropertyInfo)new ClrPropertyInfo("QuickLinksSelectedIndex", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinksSelectedIndex_0021Getter, (Action<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinksSelectedIndex_0021Setter, typeof(int));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EQuickLinksSelectedIndex_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EIconKey_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserItemViewModel)P_0).IconKey;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EIconKey_0021Property()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EIconKey_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EIconKey_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EIconKey_0021Field = (IPropertyInfo)new ClrPropertyInfo("IconKey", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EIconKey_0021Getter, (Action<object, object>)null, typeof(string));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EIconKey_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EDisplayName_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserItemViewModel)P_0).DisplayName;
	}

	private static void Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EDisplayName_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((ManagedFileChooserItemViewModel)P_0).DisplayName = (string)P_1;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EDisplayName_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EDisplayName_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EDisplayName_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EDisplayName_0021Field = (IPropertyInfo)new ClrPropertyInfo("DisplayName", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EDisplayName_0021Getter, (Action<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EDisplayName_0021Setter, typeof(string));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EDisplayName_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ELocation_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserViewModel)P_0).Location;
	}

	private static void Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ELocation_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((ManagedFileChooserViewModel)P_0).Location = (string)P_1;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ELocation_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ELocation_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ELocation_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ELocation_0021Field = (IPropertyInfo)new ClrPropertyInfo("Location", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ELocation_0021Getter, (Action<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ELocation_0021Setter, typeof(string));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ELocation_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowFilters_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserViewModel)P_0).ShowFilters;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowFilters_0021Property()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowFilters_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowFilters_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowFilters_0021Field = (IPropertyInfo)new ClrPropertyInfo("ShowFilters", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowFilters_0021Getter, (Action<object, object>)null, typeof(bool));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowFilters_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFilters_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserViewModel)P_0).Filters;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFilters_0021Property()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFilters_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFilters_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFilters_0021Field = (IPropertyInfo)new ClrPropertyInfo("Filters", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFilters_0021Getter, (Action<object, object>)null, typeof(AvaloniaList<ManagedFileChooserFilterViewModel>));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFilters_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedFilter_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserViewModel)P_0).SelectedFilter;
	}

	private static void Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedFilter_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		((ManagedFileChooserViewModel)P_0).SelectedFilter = (ManagedFileChooserFilterViewModel)P_1;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedFilter_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedFilter_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedFilter_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedFilter_0021Field = (IPropertyInfo)new ClrPropertyInfo("SelectedFilter", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedFilter_0021Getter, (Action<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedFilter_0021Setter, typeof(ManagedFileChooserFilterViewModel));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedFilter_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFileName_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserViewModel)P_0).FileName;
	}

	private static void Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFileName_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((ManagedFileChooserViewModel)P_0).FileName = (string)P_1;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFileName_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFileName_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFileName_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFileName_0021Field = (IPropertyInfo)new ClrPropertyInfo("FileName", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFileName_0021Getter, (Action<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFileName_0021Setter, typeof(string));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EFileName_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectingFolder_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserViewModel)P_0).SelectingFolder;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectingFolder_0021Property()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectingFolder_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectingFolder_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectingFolder_0021Field = (IPropertyInfo)new ClrPropertyInfo("SelectingFolder", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectingFolder_0021Getter, (Action<object, object>)null, typeof(bool));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectingFolder_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowHiddenFiles_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserViewModel)P_0).ShowHiddenFiles;
	}

	private static void Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowHiddenFiles_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((ManagedFileChooserViewModel)P_0).ShowHiddenFiles = (bool)P_1;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowHiddenFiles_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowHiddenFiles_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowHiddenFiles_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowHiddenFiles_0021Field = (IPropertyInfo)new ClrPropertyInfo("ShowHiddenFiles", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowHiddenFiles_0021Getter, (Action<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowHiddenFiles_0021Setter, typeof(bool));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EShowHiddenFiles_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EItems_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserViewModel)P_0).Items;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EItems_0021Property()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EItems_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EItems_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EItems_0021Field = (IPropertyInfo)new ClrPropertyInfo("Items", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EItems_0021Getter, (Action<object, object>)null, typeof(AvaloniaList<ManagedFileChooserItemViewModel>));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002EItems_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectionMode_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserViewModel)P_0).SelectionMode;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectionMode_0021Property()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectionMode_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectionMode_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectionMode_0021Field = (IPropertyInfo)new ClrPropertyInfo("SelectionMode", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectionMode_0021Getter, (Action<object, object>)null, typeof(SelectionMode));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectionMode_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedItems_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserViewModel)P_0).SelectedItems;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedItems_0021Property()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedItems_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedItems_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedItems_0021Field = (IPropertyInfo)new ClrPropertyInfo("SelectedItems", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedItems_0021Getter, (Action<object, object>)null, typeof(AvaloniaList<ManagedFileChooserItemViewModel>));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserViewModel_002CAvalonia_002EDialogs_002ESelectedItems_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EModified_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserItemViewModel)P_0).Modified;
	}

	private static void Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EModified_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((ManagedFileChooserItemViewModel)P_0).Modified = (DateTime)P_1;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EModified_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EModified_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EModified_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EModified_0021Field = (IPropertyInfo)new ClrPropertyInfo("Modified", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EModified_0021Getter, (Action<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EModified_0021Setter, typeof(DateTime));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EModified_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EType_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserItemViewModel)P_0).Type;
	}

	private static void Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EType_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((ManagedFileChooserItemViewModel)P_0).Type = (string)P_1;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EType_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EType_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EType_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EType_0021Field = (IPropertyInfo)new ClrPropertyInfo("Type", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EType_0021Getter, (Action<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EType_0021Setter, typeof(string));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002EType_0021Field;
	}

	private static object Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002ESize_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ManagedFileChooserItemViewModel)P_0).Size;
	}

	private static void Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002ESize_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((ManagedFileChooserItemViewModel)P_0).Size = (long)P_1;
	}

	public static IPropertyInfo Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002ESize_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002ESize_0021Field != null)
		{
			return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002ESize_0021Field;
		}
		Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002ESize_0021Field = (IPropertyInfo)new ClrPropertyInfo("Size", (Func<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002ESize_0021Getter, (Action<object, object>)Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002ESize_0021Setter, typeof(long));
		return Avalonia_002EDialogs_002EInternal_002EManagedFileChooserItemViewModel_002CAvalonia_002EDialogs_002ESize_0021Field;
	}

	private static object Avalonia_002EControls_002EProgressBar_002CAvalonia_002EControls_002ETemplateSettings_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((ProgressBar)P_0).TemplateSettings;
	}

	public static IPropertyInfo Avalonia_002EControls_002EProgressBar_002CAvalonia_002EControls_002ETemplateSettings_0021Property()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (Avalonia_002EControls_002EProgressBar_002CAvalonia_002EControls_002ETemplateSettings_0021Field != null)
		{
			return Avalonia_002EControls_002EProgressBar_002CAvalonia_002EControls_002ETemplateSettings_0021Field;
		}
		Avalonia_002EControls_002EProgressBar_002CAvalonia_002EControls_002ETemplateSettings_0021Field = (IPropertyInfo)new ClrPropertyInfo("TemplateSettings", (Func<object, object>)Avalonia_002EControls_002EProgressBar_002CAvalonia_002EControls_002ETemplateSettings_0021Getter, (Action<object, object>)null, typeof(ProgressBarTemplateSettings));
		return Avalonia_002EControls_002EProgressBar_002CAvalonia_002EControls_002ETemplateSettings_0021Field;
	}

	private static object Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002ESource_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((CompiledBinding)P_0).Source;
	}

	private static void Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002ESource_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((CompiledBinding)P_0).Source = P_1;
	}

	public static IPropertyInfo Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002ESource_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002ESource_0021Field != null)
		{
			return Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002ESource_0021Field;
		}
		Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002ESource_0021Field = (IPropertyInfo)new ClrPropertyInfo("Source", (Func<object, object>)Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002ESource_0021Getter, (Action<object, object>)Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002ESource_0021Setter, typeof(object));
		return Avalonia_002EData_002ECompiledBinding_002CAvalonia_002EBase_002ESource_0021Field;
	}

	private static object Avalonia_002EControls_002EConverters_002EMarginMultiplierConverter_002CAvalonia_002EControls_002EIndent_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((MarginMultiplierConverter)P_0).Indent;
	}

	private static void Avalonia_002EControls_002EConverters_002EMarginMultiplierConverter_002CAvalonia_002EControls_002EIndent_0021Setter(object P_0, object P_1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((MarginMultiplierConverter)P_0).Indent = (double)P_1;
	}

	public static IPropertyInfo Avalonia_002EControls_002EConverters_002EMarginMultiplierConverter_002CAvalonia_002EControls_002EIndent_0021Property()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (Avalonia_002EControls_002EConverters_002EMarginMultiplierConverter_002CAvalonia_002EControls_002EIndent_0021Field != null)
		{
			return Avalonia_002EControls_002EConverters_002EMarginMultiplierConverter_002CAvalonia_002EControls_002EIndent_0021Field;
		}
		Avalonia_002EControls_002EConverters_002EMarginMultiplierConverter_002CAvalonia_002EControls_002EIndent_0021Field = (IPropertyInfo)new ClrPropertyInfo("Indent", (Func<object, object>)Avalonia_002EControls_002EConverters_002EMarginMultiplierConverter_002CAvalonia_002EControls_002EIndent_0021Getter, (Action<object, object>)Avalonia_002EControls_002EConverters_002EMarginMultiplierConverter_002CAvalonia_002EControls_002EIndent_0021Setter, typeof(double));
		return Avalonia_002EControls_002EConverters_002EMarginMultiplierConverter_002CAvalonia_002EControls_002EIndent_0021Field;
	}

	private static object Avalonia_002EControls_002ENotifications_002EINotification_002CAvalonia_002EControls_002ETitle_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((INotification)P_0).Title;
	}

	public static IPropertyInfo Avalonia_002EControls_002ENotifications_002EINotification_002CAvalonia_002EControls_002ETitle_0021Property()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (Avalonia_002EControls_002ENotifications_002EINotification_002CAvalonia_002EControls_002ETitle_0021Field != null)
		{
			return Avalonia_002EControls_002ENotifications_002EINotification_002CAvalonia_002EControls_002ETitle_0021Field;
		}
		Avalonia_002EControls_002ENotifications_002EINotification_002CAvalonia_002EControls_002ETitle_0021Field = (IPropertyInfo)new ClrPropertyInfo("Title", (Func<object, object>)Avalonia_002EControls_002ENotifications_002EINotification_002CAvalonia_002EControls_002ETitle_0021Getter, (Action<object, object>)null, typeof(string));
		return Avalonia_002EControls_002ENotifications_002EINotification_002CAvalonia_002EControls_002ETitle_0021Field;
	}

	private static object Avalonia_002EControls_002ENotifications_002EINotification_002CAvalonia_002EControls_002EMessage_0021Getter(object P_0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((INotification)P_0).Message;
	}

	public static IPropertyInfo Avalonia_002EControls_002ENotifications_002EINotification_002CAvalonia_002EControls_002EMessage_0021Property()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (Avalonia_002EControls_002ENotifications_002EINotification_002CAvalonia_002EControls_002EMessage_0021Field != null)
		{
			return Avalonia_002EControls_002ENotifications_002EINotification_002CAvalonia_002EControls_002EMessage_0021Field;
		}
		Avalonia_002EControls_002ENotifications_002EINotification_002CAvalonia_002EControls_002EMessage_0021Field = (IPropertyInfo)new ClrPropertyInfo("Message", (Func<object, object>)Avalonia_002EControls_002ENotifications_002EINotification_002CAvalonia_002EControls_002EMessage_0021Getter, (Action<object, object>)null, typeof(string));
		return Avalonia_002EControls_002ENotifications_002EINotification_002CAvalonia_002EControls_002EMessage_0021Field;
	}
}
