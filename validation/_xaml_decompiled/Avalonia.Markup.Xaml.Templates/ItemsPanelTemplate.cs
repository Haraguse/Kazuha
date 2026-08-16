using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.Templates;

[ControlTemplateScope]
public class ItemsPanelTemplate : ITemplate<Panel?>, ITemplate
{
	[Content]
	[TemplateContent]
	public object? Content { get; set; }

	public Panel? Build()
	{
		return (Panel)(TemplateContent.Load(Content)?.Result);
	}

	object? ITemplate.Build()
	{
		return Build();
	}
}
