using Avalonia.Controls;

namespace Avalonia.Data;

internal class ElementNameElement : ICompiledBindingPathElement, IControlSourceBindingPathElement
{
	public INameScope NameScope { get; }

	public string Name { get; }

	public ElementNameElement(INameScope nameScope, string name)
	{
		NameScope = nameScope;
		Name = name;
	}

	public override string ToString()
	{
		return "#" + Name;
	}
}
