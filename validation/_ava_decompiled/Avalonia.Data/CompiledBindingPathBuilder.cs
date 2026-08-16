using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data;

public class CompiledBindingPathBuilder
{
	private readonly List<ICompiledBindingPathElement> _elements = new List<ICompiledBindingPathElement>();

	public CompiledBindingPathBuilder Not()
	{
		_elements.Add(new NotExpressionPathElement());
		return this;
	}

	public CompiledBindingPathBuilder Property(IPropertyInfo info, Func<WeakReference<object?>, IPropertyInfo, IPropertyAccessor> accessorFactory)
	{
		return Property(info, accessorFactory, acceptsNull: false);
	}

	public CompiledBindingPathBuilder Property(IPropertyInfo info, Func<WeakReference<object?>, IPropertyInfo, IPropertyAccessor> accessorFactory, bool acceptsNull)
	{
		_elements.Add(new PropertyElement(info, accessorFactory, _elements.Count == 0, acceptsNull));
		return this;
	}

	public CompiledBindingPathBuilder Method(RuntimeMethodHandle handle, RuntimeTypeHandle delegateType)
	{
		Method(handle, delegateType, acceptsNull: false);
		return this;
	}

	public CompiledBindingPathBuilder Method(RuntimeMethodHandle handle, RuntimeTypeHandle delegateType, bool acceptsNull)
	{
		_elements.Add(new MethodAsDelegateElement(handle, delegateType, acceptsNull));
		return this;
	}

	public CompiledBindingPathBuilder Command(string methodName, Action<object, object?> executeHelper, Func<object, object?, bool>? canExecuteHelper, string[]? dependsOnProperties)
	{
		_elements.Add(new MethodAsCommandElement(methodName, executeHelper, canExecuteHelper, dependsOnProperties ?? Array.Empty<string>()));
		return this;
	}

	public CompiledBindingPathBuilder StreamTask<T>()
	{
		_elements.Add(new TaskStreamPathElement<T>());
		return this;
	}

	[RequiresUnreferencedCode("StreamPlugin might require unreferenced code.")]
	public CompiledBindingPathBuilder StreamTask()
	{
		_elements.Add(new TaskStreamPathElement());
		return this;
	}

	public CompiledBindingPathBuilder StreamObservable<T>()
	{
		_elements.Add(new ObservableStreamPathElement<T>());
		return this;
	}

	[RequiresUnreferencedCode("StreamPlugin might require unreferenced code.")]
	public CompiledBindingPathBuilder StreamObservable()
	{
		_elements.Add(new ObservableStreamPathElement());
		return this;
	}

	public CompiledBindingPathBuilder Self()
	{
		_elements.Add(new SelfPathElement());
		return this;
	}

	public CompiledBindingPathBuilder Ancestor(Type ancestorType, int level)
	{
		_elements.Add(new AncestorPathElement(ancestorType, level));
		return this;
	}

	public CompiledBindingPathBuilder VisualAncestor(Type ancestorType, int level)
	{
		_elements.Add(new VisualAncestorPathElement(ancestorType, level));
		return this;
	}

	public CompiledBindingPathBuilder ElementName(INameScope nameScope, string name)
	{
		_elements.Add(new ElementNameElement(nameScope, name));
		return this;
	}

	public CompiledBindingPathBuilder ArrayElement(int[] indices, Type elementType)
	{
		_elements.Add(new ArrayElementPathElement(indices, elementType));
		return this;
	}

	public CompiledBindingPathBuilder TypeCast<T>()
	{
		_elements.Add(new TypeCastPathElement<T>());
		return this;
	}

	public CompiledBindingPathBuilder TypeCast(Type targetType)
	{
		_elements.Add(new TypeCastPathElement(targetType));
		return this;
	}

	public CompiledBindingPathBuilder TemplatedParent()
	{
		_elements.Add(new TemplatedParentPathElement());
		return this;
	}

	public CompiledBindingPath Build()
	{
		return new CompiledBindingPath(_elements.ToArray());
	}
}
