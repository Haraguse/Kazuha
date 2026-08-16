namespace Avalonia.Data;

internal class TemplatedParentPathElement : ICompiledBindingPathElement, IControlSourceBindingPathElement
{
	public override string ToString()
	{
		return "$templatedParent";
	}
}
