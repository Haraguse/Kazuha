using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// Indicates that a <see cref="T:Avalonia.Data.Core.ExpressionNodes.ExpressionNode" /> accesses a property on an object.
/// </summary>
internal interface IPropertyAccessorNode
{
	string PropertyName { get; }

	IPropertyAccessor? Accessor { get; }

	void EnableDataValidation();
}
