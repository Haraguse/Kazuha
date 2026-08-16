using System;

namespace FluentAvalonia.UI.Controls;

internal struct SelectedItemInfo(SelectionNode node, IndexPath path)
{
	public WeakReference<SelectionNode> Node = new WeakReference<SelectionNode>(node);

	public IndexPath Path = path;
}
