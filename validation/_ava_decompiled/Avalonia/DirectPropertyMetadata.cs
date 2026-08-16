using Avalonia.Data;

namespace Avalonia;

/// <summary>
/// Metadata for direct avalonia properties.
/// </summary>
public class DirectPropertyMetadata<TValue> : AvaloniaPropertyMetadata, IDirectPropertyMetadata
{
	/// <summary>
	/// Gets the value to use when the property is set to <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" />.
	/// </summary>
	public TValue UnsetValue { get; private set; }

	/// <inheritdoc />
	object? IDirectPropertyMetadata.UnsetValue => UnsetValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.StyledPropertyMetadata`1" /> class.
	/// </summary>
	/// <param name="unsetValue">
	/// The value to use when the property is set to <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" />
	/// </param>
	/// <param name="defaultBindingMode">The default binding mode.</param>
	/// <param name="enableDataValidation">
	/// Whether the property is interested in data validation.
	/// </param>
	public DirectPropertyMetadata(TValue unsetValue = default(TValue), BindingMode defaultBindingMode = BindingMode.Default, bool? enableDataValidation = null)
		: base(defaultBindingMode, enableDataValidation)
	{
		UnsetValue = unsetValue;
	}

	/// <inheritdoc />
	public override void Merge(AvaloniaPropertyMetadata baseMetadata, AvaloniaProperty property)
	{
		base.Merge(baseMetadata, property);
		if (baseMetadata is DirectPropertyMetadata<TValue> directPropertyMetadata)
		{
			TValue unsetValue = UnsetValue;
			if (unsetValue == null)
			{
				TValue val = (UnsetValue = directPropertyMetadata.UnsetValue);
			}
		}
	}

	/// <inheritdoc />
	public override AvaloniaPropertyMetadata GenerateTypeSafeMetadata()
	{
		if (base.IsReadOnly)
		{
			return this;
		}
		DirectPropertyMetadata<TValue> directPropertyMetadata = new DirectPropertyMetadata<TValue>(UnsetValue, base.DefaultBindingMode, base.EnableDataValidation);
		directPropertyMetadata.Freeze();
		return directPropertyMetadata;
	}
}
