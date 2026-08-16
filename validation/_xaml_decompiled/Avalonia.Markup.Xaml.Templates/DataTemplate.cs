using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Avalonia.Markup.Xaml.Templates;

public class DataTemplate : IRecyclingDataTemplate, IDataTemplate, ITemplate<object?, Control?>, ITypedDataTemplate
{
	[DataType]
	public Type? DataType { get; set; }

	[Content]
	[TemplateContent]
	public object? Content { get; set; }

	public bool Match(object? data)
	{
		if (DataType == null)
		{
			return true;
		}
		return DataType.IsInstanceOfType(data);
	}

	public Control? Build(object? data)
	{
		return Build(data, null);
	}

	public Control? Build(object? data, Control? existing)
	{
		Control control = existing;
		if (control == null)
		{
			TemplateResult<Control>? templateResult = TemplateContent.Load(Content);
			if (templateResult == null)
			{
				return null;
			}
			control = templateResult.Result;
		}
		return control;
	}
}
