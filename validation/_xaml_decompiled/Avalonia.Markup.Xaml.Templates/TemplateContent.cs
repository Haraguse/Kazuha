using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Avalonia.Markup.Xaml.Templates;

public static class TemplateContent
{
	public static TemplateResult<Control>? Load(object? templateContent)
	{
		return Load<Control>(templateContent);
	}

	public static TemplateResult<T>? Load<T>(object? templateContent)
	{
		if (!(templateContent is IDeferredContent deferredContent))
		{
			if (!(templateContent is Func<IServiceProvider, object> func))
			{
				if (templateContent == null)
				{
					return null;
				}
				throw new ArgumentException($"Unexpected content {templateContent.GetType()}", "templateContent");
			}
			return (TemplateResult<T>)func(null);
		}
		return (TemplateResult<T>)deferredContent.Build(null);
	}
}
