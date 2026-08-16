using System.Reflection;

namespace Avalonia.Markup.Xaml;

public class RuntimeXamlLoaderConfiguration
{
	/// <summary>
	/// Delegate for <see cref="P:Avalonia.Markup.Xaml.RuntimeXamlLoaderConfiguration.DiagnosticHandler" /> property.
	/// </summary>
	public delegate RuntimeXamlDiagnosticSeverity XamlDiagnosticFunc(RuntimeXamlDiagnostic diagnostic);

	/// <summary>
	/// Default assembly for clr-namespace:.
	/// </summary>
	public Assembly? LocalAssembly { get; set; }

	/// <summary>
	/// Defines is CompiledBinding should be used by default.
	/// Default is 'false'.
	/// </summary>
	public bool UseCompiledBindingsByDefault { get; set; }

	/// <summary>
	/// Indicates whether the XAML is being loaded in design mode.
	/// Default is 'false'.
	/// </summary>
	public bool DesignMode { get; set; }

	/// <summary>
	/// When enabled, the XAML compiler embeds SourceInfo metadata (file path, line, and column) into generated code.
	/// Default is 'false'.
	/// </summary>
	public bool CreateSourceInfo { get; set; }

	/// <summary>
	/// XAML diagnostics handler.
	/// </summary>
	/// <returns>
	/// Defines if any diagnostic severity should be overriden.
	/// Note, severity cannot be set lower than minimal for specific diagnostic.
	/// </returns>
	public XamlDiagnosticFunc? DiagnosticHandler { get; set; }
}
