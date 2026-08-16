using System;

namespace Avalonia.SourceGenerator;

/// <summary>
/// When applied to a partial class or struct that declares a primary constructor,
/// generates a partial part exposing a getter-only property for every primary
/// constructor parameter, each initialized from that parameter
/// (e.g. <c>[DefinitelyNotARecord] partial class C(int Foo)</c> generates
/// <c>public int Foo { get; } = Foo;</c>). Unlike a record, this produces no
/// equality, <c>ToString</c>, deconstruction or copy members, keeping IL small.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal sealed class DefinitelyNotARecordAttribute : Attribute
{
}
