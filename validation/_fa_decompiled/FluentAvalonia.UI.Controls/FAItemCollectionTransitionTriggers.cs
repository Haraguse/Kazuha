using System;

namespace FluentAvalonia.UI.Controls;

[Flags]
public enum FAItemCollectionTransitionTriggers
{
	CollectionChangeAdd = 1,
	CollectionChangeRemove = 2,
	CollectionChangeReset = 4,
	LayoutTransition = 8
}
