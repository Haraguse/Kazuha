using System;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

public class FAItemCollectionTransitionCompletedEventArgs : EventArgs
{
	public FAItemCollectionTransition Transition { get; }

	public Control Element { get; }

	public FAItemCollectionTransitionCompletedEventArgs(FAItemCollectionTransition transition)
	{
	}
}
