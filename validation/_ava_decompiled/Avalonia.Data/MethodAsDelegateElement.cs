using System;
using System.Reflection;

namespace Avalonia.Data;

internal class MethodAsDelegateElement : ICompiledBindingPathElement
{
	public MethodInfo Method { get; }

	public Type DelegateType { get; }

	public bool AcceptsNull { get; }

	public MethodAsDelegateElement(RuntimeMethodHandle method, RuntimeTypeHandle delegateType, bool acceptsNull)
	{
		Method = (MethodBase.GetMethodFromHandle(method) as MethodInfo) ?? throw new ArgumentException("Invalid method handle", "method");
		DelegateType = Type.GetTypeFromHandle(delegateType) ?? throw new ArgumentException("Unexpected null returned from Type.GetTypeFromHandle in MethodAsDelegateElement");
		AcceptsNull = acceptsNull;
	}
}
