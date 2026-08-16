using System;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

public class FASelectTemplateEventArgs : EventArgs
{
	public string TemplateKey { get; set; }

	public object DataContext { get; internal set; }

	public Control Owner { get; internal set; }

	internal FASelectTemplateEventArgs()
	{
	}

	internal FASelectTemplateEventArgs(object dataContext, Control owner)
	{
		DataContext = dataContext;
		Owner = owner;
	}
}
