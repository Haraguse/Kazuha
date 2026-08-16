using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Metadata;
using Avalonia.Reactive;

namespace Avalonia.Markup.Xaml.Templates;

public class TreeDataTemplate : ITreeDataTemplate, IDataTemplate, ITemplate<object?, Control?>, ITypedDataTemplate
{
	[DataType]
	public Type? DataType { get; set; }

	[Content]
	[TemplateContent]
	public object? Content { get; set; }

	[AssignBinding]
	public BindingBase? ItemsSource { get; set; }

	public bool Match(object? data)
	{
		if (DataType == null)
		{
			return true;
		}
		return DataType.IsInstanceOfType(data);
	}

	public IDisposable BindChildren(AvaloniaObject target, AvaloniaProperty targetProperty, object item)
	{
		if (ItemsSource == null)
		{
			return Disposable.Empty;
		}
		return target.Bind(targetProperty, ItemsSource);
	}

	public Control? Build(object? data)
	{
		Control control = TemplateContent.Load(Content)?.Result;
		if (control != null)
		{
			control.DataContext = data;
		}
		return control;
	}
}
