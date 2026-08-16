using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.Templates;

public class Template : ITemplate<Control?>, ITemplate
{
	[Content]
	[TemplateContent]
	public object? Content { get; set; }

	public Control? Build()
	{
		return TemplateContent.Load(Content)?.Result;
	}

	object? ITemplate.Build()
	{
		return Build();
	}
}
