using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a line that separates menu items in a NavigationView.
/// </summary>
[PseudoClasses(new string[] { ":horizontal", ":horizontalcompact", ":vertical" })]
[TemplatePart("RootGrid", typeof(Panel))]
public class FANavigationViewItemSeparator : FANavigationViewItemBase
{
	private FACompositeDisposable _splitViewRevokers;

	private bool _appliedTemplate;

	private bool _isClosedCompact;

	private Panel _rootGrid;

	private const string s_tpRootGrid = "RootGrid";

	private const string s_pcHorizontal = ":horizontal";

	private const string s_pcHorizontalCompact = ":horizontalcompact";

	private const string s_pcVertical = ":vertical";

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		_appliedTemplate = false;
		_splitViewRevokers?.Dispose();
		((TemplatedControl)this).OnApplyTemplate(e);
		_rootGrid = NameScopeExtensions.Find<Panel>(e.NameScope, "RootGrid");
		SplitView getSplitView = base.GetSplitView;
		if (getSplitView != null)
		{
			_splitViewRevokers = new FACompositeDisposable(AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)getSplitView, (AvaloniaProperty)(object)SplitView.IsPaneOpenProperty).Subscribe(OnSplitViewPropertyChanged), AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)getSplitView, (AvaloniaProperty)(object)SplitView.DisplayModeProperty).Subscribe(OnSplitViewPropertyChanged));
			UpdateIsClosedCompact(updateVisState: false);
		}
		_appliedTemplate = true;
		UpdateVisualState();
		UpdateItemIndentation();
	}

	protected override void OnNavigationViewItemBaseDepthChanged()
	{
		UpdateItemIndentation();
	}

	protected override void OnNavigationViewItemBasePositionChanged()
	{
		UpdateVisualState();
	}

	private void OnSplitViewPropertyChanged(AvaloniaPropertyChangedEventArgs args)
	{
		UpdateIsClosedCompact(updateVisState: true);
	}

	private void UpdateVisualState()
	{
		if (_appliedTemplate)
		{
			bool flag = base.Position == NavigationViewRepeaterPosition.TopFooter || base.Position == NavigationViewRepeaterPosition.TopPrimary;
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":horizontal", !flag && !_isClosedCompact);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":horizontalcompact", !flag && _isClosedCompact);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":vertical", flag);
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

	private void UpdateIsClosedCompact(bool updateVisState)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		SplitView getSplitView = base.GetSplitView;
		if (getSplitView != null)
		{
			_isClosedCompact = !getSplitView.IsPaneOpen && ((int)getSplitView.DisplayMode == 1 || (int)getSplitView.DisplayMode == 3);
			if (updateVisState)
			{
				UpdateVisualState();
			}
		}
	}
}
