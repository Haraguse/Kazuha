namespace Avalonia.Data;

internal static class BindingErrorTypeExtensions
{
	public static BindingValueType ToBindingValueType(this BindingErrorType type)
	{
		return type switch
		{
			BindingErrorType.Error => BindingValueType.BindingError, 
			BindingErrorType.DataValidationError => BindingValueType.DataValidationError, 
			_ => BindingValueType.Value, 
		};
	}
}
