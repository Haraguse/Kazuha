using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a header for a group of menu items in a NavigationMenu.
/// </summary>
[PseudoClasses(new string[] { ":headertextcollapsed", ":headertextvisible" })]
[PseudoClasses(new string[] { ":topmode" })]
[TemplatePart("RootGrid", typeof(Grid))]
public class FANavigationViewItemHeader : FANavigationViewItemBase
{
	private IDisposable _splitViewRevokers;

	private Grid _rootGrid;

	private bool _isClosedCompact;

	private const string s_tpRootGrid = "RootGrid";

	private const string s_pcTopMode = ":topmode";

	private const string s_pcHeaderTextVisible = ":headertextvisible";

	private const string s_pcHeaderTextCollapsed = ":headertextcollapsed";

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		_splitViewRevokers?.Dispose();
		((TemplatedControl)this).OnApplyTemplate(e);
		SplitView getSplitView = base.GetSplitView;
		if (getSplitView != null)
		{
			_splitViewRevokers = new FACompositeDisposable(AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)getSplitView, (AvaloniaProperty)(object)SplitView.IsPaneOpenProperty).Subscribe(OnSplitViewPropertyChanged), AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)getSplitView, (AvaloniaProperty)(object)SplitView.DisplayModeProperty).Subscribe(OnSplitViewPropertyChanged));
			UpdateIsClosedCompact();
		}
		_rootGrid = NameScopeExtensions.Find<Grid>(e.NameScope, "RootGrid");
		UpdateVisualState();
		UpdateItemIndentation();
	}

	protected override void OnNavigationViewItemBaseDepthChanged()
	{
		UpdateItemIndentation();
	}

	private void OnSplitViewPropertyChanged(AvaloniaPropertyChangedEventArgs args)
	{
		if (args.Property == (AvaloniaProperty)(object)SplitView.IsPaneOpenProperty || args.Property == (AvaloniaProperty)(object)SplitView.DisplayModeProperty)
		{
			UpdateIsClosedCompact();
		}
	}

	private void UpdateIsClosedCompact()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		SplitView getSplitView = base.GetSplitView;
		if (getSplitView != null)
		{
			_isClosedCompact = !getSplitView.IsPaneOpen && ((int)getSplitView.DisplayMode == 3 || (int)getSplitView.DisplayMode == 1);
			UpdateVisualState();
		}
	}

	private void UpdateVisualState()
	{
		bool flag = _isClosedCompact && base.IsTopLevelItem;
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":headertextcollapsed", flag);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":headertextvisible", !flag);
		FANavigationView getNavigationView = base.GetNavigationView;
		if (getNavigationView != null)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topmode", getNavigationView.PaneDisplayMode == FANavigationViewPaneDisplayMode.Top);
		}
	}

	private void UpdateItemIndentation()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if (_rootGrid != null)
		{
			Thickness margin = ((Layoutable)_rootGrid).Margin;
			int num = base.Depth * _itemIndentation;
			((Layoutable)_rootGrid).Margin = new Thickness((double)num, ((Thickness)(ref margin)).Top, ((Thickness)(ref margin)).Right, ((Thickness)(ref margin)).Bottom);
		}
	}
}
