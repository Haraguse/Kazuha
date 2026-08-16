using System;
using System.Collections.Generic;

namespace Avalonia.Data;

internal class MethodAsCommandElement : ICompiledBindingPathElement
{
	public string MethodName { get; }

	public Action<object, object?> ExecuteMethod { get; }

	public Func<object, object?, bool>? CanExecuteMethod { get; }

	public HashSet<string> DependsOnProperties { get; }

	public MethodAsCommandElement(string methodName, Action<object, object?> executeHelper, Func<object, object?, bool>? canExecuteHelper, string[] dependsOnElements)
	{
		MethodName = methodName;
		ExecuteMethod = executeHelper;
		CanExecuteMethod = canExecuteHelper;
		DependsOnProperties = new HashSet<string>(dependsOnElements);
	}
}
