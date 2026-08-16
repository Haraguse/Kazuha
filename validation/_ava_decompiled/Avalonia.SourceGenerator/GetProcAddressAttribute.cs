using System;

namespace Avalonia.SourceGenerator;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class GetProcAddressAttribute : Attribute
{
	public GetProcAddressAttribute(string proc)
	{
	}

	public GetProcAddressAttribute(string proc, bool optional = false)
	{
	}

	public GetProcAddressAttribute(bool optional)
	{
	}

	public GetProcAddressAttribute()
	{
	}
}
