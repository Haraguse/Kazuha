using System;

namespace Avalonia.SourceGenerator;

/// <summary>
/// When applied to a void-returning method on an interface marked with
/// <see cref="T:Avalonia.SourceGenerator.GenerateCrossThreadProxyAttribute" />, the generated proxy
/// returns <see cref="T:System.Threading.Tasks.Task" /> instead of being
/// fire-and-forget. Has no effect on non-void methods (which are always
/// wrapped into <see cref="T:System.Threading.Tasks.Task`1" />).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class GenerateCrossThreadProxyReturnTaskAttribute : Attribute
{
}
