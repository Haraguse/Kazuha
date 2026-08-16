using System;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.Templates;

[ControlTemplateScope]
public class WindowDrawnDecorationsTemplate : IWindowDrawnDecorationsTemplate, ITemplate
{
	[Content]
	[TemplateContent(TemplateResultType = typeof(WindowDrawnDecorationsContent))]
	public object? Content { get; set; }

	public TemplateResult<WindowDrawnDecorationsContent> Build()
	{
		return TemplateContent.Load<WindowDrawnDecorationsContent>(Content) ?? throw new InvalidOperationException();
	}

	object? ITemplate.Build()
	{
		return Build().Result;
	}
}
