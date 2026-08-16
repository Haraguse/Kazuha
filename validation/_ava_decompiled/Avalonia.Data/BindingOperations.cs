namespace Avalonia.Data;

public static class BindingOperations
{
	public static readonly object DoNothing = new DoNothingType();

	/// <summary>
	/// Retrieves the <see cref="T:Avalonia.Data.BindingExpressionBase" /> that is currently active on the
	/// specified property.
	/// </summary>
	/// <param name="target">
	/// The <see cref="T:Avalonia.AvaloniaObject" /> from which to retrieve the binding expression.
	/// </param>
	/// <param name="property">
	/// The binding target property from which to retrieve the binding expression.
	/// </param>
	/// <returns>
	/// The <see cref="T:Avalonia.Data.BindingExpressionBase" /> object that is active on the given property or
	/// null if no binding expression is active on the given property.
	/// </returns>
	public static BindingExpressionBase? GetBindingExpressionBase(AvaloniaObject target, AvaloniaProperty property)
	{
		return target.GetValueStore().GetExpression(property);
	}
}
