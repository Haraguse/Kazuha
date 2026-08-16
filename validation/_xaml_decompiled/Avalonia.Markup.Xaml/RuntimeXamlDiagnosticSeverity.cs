namespace Avalonia.Markup.Xaml;

public enum RuntimeXamlDiagnosticSeverity
{
	/// <summary>
	/// Something that is an issue, as determined by some authority,
	/// but is not surfaced through normal means.
	/// There may be different mechanisms that act on these issues.
	/// </summary>
	Info = 1,
	/// <summary>
	/// Diagnostic is reported as a warning.
	/// </summary>
	Warning,
	/// <summary>
	/// Diagnostic is reported as an error.
	/// Compilation process is continued until the end of the parsing and transforming stage, throwing an aggregated exception of all errors.
	/// </summary>
	Error,
	/// <summary>
	/// Diagnostic is reported as an fatal error.
	/// Compilation process is stopped right after this error.
	/// </summary>
	Fatal
}
