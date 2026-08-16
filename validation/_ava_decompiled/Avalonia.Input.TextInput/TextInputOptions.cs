namespace Avalonia.Input.TextInput;

public class TextInputOptions
{
	public static readonly TextInputOptions Default = new TextInputOptions();

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.TextInput.TextInputOptions.ContentType" /> property.
	/// </summary>
	public static readonly AttachedProperty<TextInputContentType> ContentTypeProperty = AvaloniaProperty.RegisterAttached<TextInputOptions, StyledElement, TextInputContentType>("ContentType", TextInputContentType.Normal, inherits: true);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.TextInput.TextInputOptions.ReturnKeyType" /> property.
	/// </summary>
	public static readonly AttachedProperty<TextInputReturnKeyType> ReturnKeyTypeProperty = AvaloniaProperty.RegisterAttached<TextInputOptions, StyledElement, TextInputReturnKeyType>("ReturnKeyType", TextInputReturnKeyType.Default, inherits: true);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.TextInput.TextInputOptions.Multiline" /> property.
	/// </summary>
	public static readonly AttachedProperty<bool> MultilineProperty = AvaloniaProperty.RegisterAttached<TextInputOptions, StyledElement, bool>("Multiline", defaultValue: false, inherits: true);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.TextInput.TextInputOptions.Lowercase" /> property.
	/// </summary>
	public static readonly AttachedProperty<bool> LowercaseProperty = AvaloniaProperty.RegisterAttached<TextInputOptions, StyledElement, bool>("Lowercase", defaultValue: false, inherits: true);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.TextInput.TextInputOptions.Uppercase" /> property.
	/// </summary>
	public static readonly AttachedProperty<bool> UppercaseProperty = AvaloniaProperty.RegisterAttached<TextInputOptions, StyledElement, bool>("Uppercase", defaultValue: false, inherits: true);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.TextInput.TextInputOptions.AutoCapitalization" /> property.
	/// </summary>
	public static readonly AttachedProperty<bool> AutoCapitalizationProperty = AvaloniaProperty.RegisterAttached<TextInputOptions, StyledElement, bool>("AutoCapitalization", defaultValue: false, inherits: true);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.TextInput.TextInputOptions.IsSensitive" /> property.
	/// </summary>
	public static readonly AttachedProperty<bool> IsSensitiveProperty = AvaloniaProperty.RegisterAttached<TextInputOptions, StyledElement, bool>("IsSensitive", defaultValue: false, inherits: true);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Input.TextInput.TextInputOptions.ShowSuggestions" /> property.
	/// </summary>
	public static readonly AttachedProperty<bool?> ShowSuggestionsProperty = AvaloniaProperty.RegisterAttached<TextInputOptions, StyledElement, bool?>("ShowSuggestions", null, inherits: true);

	/// <summary>
	/// The content type (mostly for determining the shape of the virtual keyboard)
	/// </summary>
	public TextInputContentType ContentType { get; set; }

	/// <summary>
	/// Determines what the Return key says and how it behaves.
	/// </summary>
	public TextInputReturnKeyType ReturnKeyType { get; set; }

	/// <summary>
	/// Text is multiline
	/// </summary>
	public bool Multiline { get; set; }

	/// <summary>
	/// Text is in lower case
	/// </summary>
	public bool Lowercase { get; set; }

	/// <summary>
	/// Text is in upper case
	/// </summary>
	public bool Uppercase { get; set; }

	/// <summary>
	/// Automatically capitalize letters at the start of the sentence
	/// </summary>
	public bool AutoCapitalization { get; set; }

	/// <summary>
	/// Text contains sensitive data like card numbers and should not be stored  
	/// </summary>
	public bool IsSensitive { get; set; }

	/// <summary>
	/// Show virtual keyboard suggestions
	/// </summary>
	public bool? ShowSuggestions { get; set; }

	public static TextInputOptions FromStyledElement(StyledElement avaloniaObject)
	{
		return new TextInputOptions
		{
			ContentType = GetContentType(avaloniaObject),
			ReturnKeyType = GetReturnKeyType(avaloniaObject),
			Multiline = GetMultiline(avaloniaObject),
			AutoCapitalization = GetAutoCapitalization(avaloniaObject),
			IsSensitive = GetIsSensitive(avaloniaObject),
			Lowercase = GetLowercase(avaloniaObject),
			Uppercase = GetUppercase(avaloniaObject),
			ShowSuggestions = GetShowSuggestions(avaloniaObject)
		};
	}

	/// <summary>
	/// Sets the value of the attached <see cref="F:Avalonia.Input.TextInput.TextInputOptions.ContentTypeProperty" /> on a control.
	/// </summary>
	/// <param name="avaloniaObject">The control.</param>
	/// <param name="value">The property value to set.</param>
	public static void SetContentType(StyledElement avaloniaObject, TextInputContentType value)
	{
		avaloniaObject.SetValue(ContentTypeProperty, value);
	}

	/// <summary>
	/// Gets the value of the attached <see cref="F:Avalonia.Input.TextInput.TextInputOptions.ContentTypeProperty" />.
	/// </summary>
	/// <param name="avaloniaObject">The target.</param>
	/// <returns>TextInputContentType</returns>
	public static TextInputContentType GetContentType(StyledElement avaloniaObject)
	{
		return avaloniaObject.GetValue(ContentTypeProperty);
	}

	/// <summary>
	/// Sets the value of the attached <see cref="F:Avalonia.Input.TextInput.TextInputOptions.ReturnKeyTypeProperty" /> on a control.
	/// </summary>
	/// <param name="avaloniaObject">The control.</param>
	/// <param name="value">The property value to set.</param>
	public static void SetReturnKeyType(StyledElement avaloniaObject, TextInputReturnKeyType value)
	{
		avaloniaObject.SetValue(ReturnKeyTypeProperty, value);
	}

	/// <summary>
	/// Gets the value of the attached <see cref="F:Avalonia.Input.TextInput.TextInputOptions.ReturnKeyTypeProperty" />.
	/// </summary>
	/// <param name="avaloniaObject">The target.</param>
	/// <returns>TextInputReturnKeyType</returns>
	public static TextInputReturnKeyType GetReturnKeyType(StyledElement avaloniaObject)
	{
		return avaloniaObject.GetValue(ReturnKeyTypeProperty);
	}

	/// <summary>
	/// Sets the value of the attached <see cref="F:Avalonia.Input.TextInput.TextInputOptions.MultilineProperty" /> on a control.
	/// </summary>
	/// <param name="avaloniaObject">The control.</param>
	/// <param name="value">The property value to set.</param>
	public static void SetMultiline(StyledElement avaloniaObject, bool value)
	{
		avaloniaObject.SetValue(MultilineProperty, value);
	}

	/// <summary>
	/// Gets the value of the attached <see cref="F:Avalonia.Input.TextInput.TextInputOptions.MultilineProperty" />.
	/// </summary>
	/// <param name="avaloniaObject">The target.</param>
	/// <returns>true if multiline</returns>
	public static bool GetMultiline(StyledElement avaloniaObject)
	{
		return avaloniaObject.GetValue(MultilineProperty);
	}

	/// <summary>
	/// Sets the value of the attached <see cref="F:Avalonia.Input.TextInput.TextInputOptions.LowercaseProperty" /> on a control.
	/// </summary>
	/// <param name="avaloniaObject">The control.</param>
	/// <param name="value">The property value to set.</param>
	public static void SetLowercase(StyledElement avaloniaObject, bool value)
	{
		avaloniaObject.SetValue(LowercaseProperty, value);
	}

	/// <summary>
	/// Gets the value of the attached <see cref="F:Avalonia.Input.TextInput.TextInputOptions.LowercaseProperty" />.
	/// </summary>
	/// <param name="avaloniaObject">The target.</param>
	/// <returns>true if Lowercase</returns>
	public static bool GetLowercase(StyledElement avaloniaObject)
	{
		return avaloniaObject.GetValue(LowercaseProperty);
	}

	/// <summary>
	/// Sets the value of the attached <see cref="F:Avalonia.Input.TextInput.TextInputOptions.UppercaseProperty" /> on a control.
	/// </summary>
	/// <param name="avaloniaObject">The control.</param>
	/// <param name="value">The property value to set.</param>
	public static void SetUppercase(StyledElement avaloniaObject, bool value)
	{
		avaloniaObject.SetValue(UppercaseProperty, value);
	}

	/// <summary>
	/// Gets the value of the attached <see cref="F:Avalonia.Input.TextInput.TextInputOptions.UppercaseProperty" />.
	/// </summary>
	/// <param name="avaloniaObject">The target.</param>
	/// <returns>true if Uppercase</returns>
	public static bool GetUppercase(StyledElement avaloniaObject)
	{
		return avaloniaObject.GetValue(UppercaseProperty);
	}

	/// <summary>
	/// Sets the value of the attached <see cref="F:Avalonia.Input.TextInput.TextInputOptions.AutoCapitalizationProperty" /> on a control.
	/// </summary>
	/// <param name="avaloniaObject">The control.</param>
	/// <param name="value">The property value to set.</param>
	public static void SetAutoCapitalization(StyledElement avaloniaObject, bool value)
	{
		avaloniaObject.SetValue(AutoCapitalizationProperty, value);
	}

	/// <summary>
	/// Gets the value of the attached <see cref="F:Avalonia.Input.TextInput.TextInputOptions.AutoCapitalizationProperty" />.
	/// </summary>
	/// <param name="avaloniaObject">The target.</param>
	/// <returns>true if AutoCapitalization</returns>
	public static bool GetAutoCapitalization(StyledElement avaloniaObject)
	{
		return avaloniaObject.GetValue(AutoCapitalizationProperty);
	}

	/// <summary>
	/// Sets the value of the attached <see cref="F:Avalonia.Input.TextInput.TextInputOptions.IsSensitiveProperty" /> on a control.
	/// </summary>
	/// <param name="avaloniaObject">The control.</param>
	/// <param name="value">The property value to set.</param>
	public static void SetIsSensitive(StyledElement avaloniaObject, bool value)
	{
		avaloniaObject.SetValue(IsSensitiveProperty, value);
	}

	/// <summary>
	/// Gets the value of the attached <see cref="F:Avalonia.Input.TextInput.TextInputOptions.IsSensitiveProperty" />.
	/// </summary>
	/// <param name="avaloniaObject">The target.</param>
	/// <returns>true if IsSensitive</returns>
	public static bool GetIsSensitive(StyledElement avaloniaObject)
	{
		return avaloniaObject.GetValue(IsSensitiveProperty);
	}

	/// <summary>
	/// Sets the value of the attached <see cref="F:Avalonia.Input.TextInput.TextInputOptions.ShowSuggestionsProperty" /> on a control.
	/// </summary>
	/// <param name="avaloniaObject">The control.</param>
	/// <param name="value">The property value to set.</param>
	public static void SetShowSuggestions(StyledElement avaloniaObject, bool? value)
	{
		avaloniaObject.SetValue(ShowSuggestionsProperty, value);
	}

	/// <summary>
	/// Gets the value of the attached <see cref="F:Avalonia.Input.TextInput.TextInputOptions.ShowSuggestionsProperty" />.
	/// </summary>
	/// <param name="avaloniaObject">The target.</param>
	/// <returns>true if ShowSuggestions</returns>
	public static bool? GetShowSuggestions(StyledElement avaloniaObject)
	{
		return avaloniaObject.GetValue(ShowSuggestionsProperty);
	}
}
