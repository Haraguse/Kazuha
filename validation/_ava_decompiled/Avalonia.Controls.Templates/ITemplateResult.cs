namespace Avalonia.Controls.Templates;

public interface ITemplateResult
{
	object? Result { get; }

	INameScope NameScope { get; }
}
