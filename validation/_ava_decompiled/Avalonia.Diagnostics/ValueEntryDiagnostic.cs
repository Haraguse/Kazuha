using Avalonia.Metadata;

namespace Avalonia.Diagnostics;

[PrivateApi]
public record ValueEntryDiagnostic(AvaloniaProperty Property, object? Value);
