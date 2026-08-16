namespace Avalonia.Markup.Xaml;

public record RuntimeXamlDiagnostic(string Id, RuntimeXamlDiagnosticSeverity Severity, string Title, int? LineNumber, int? LinePosition)
{
	public string? Document { get; set; }
}
