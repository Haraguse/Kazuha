using System;

namespace Avalonia.Metadata;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface)]
public sealed class PrivateApiAttribute : Attribute
{
}
