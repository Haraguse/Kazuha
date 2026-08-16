using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.Metadata;

namespace Avalonia.Diagnostics;

[PrivateApi]
[NotClientImplementable]
public interface IValueFrameDiagnostic
{
	public enum FrameType
	{
		Unknown,
		Local,
		Theme,
		Style,
		Template
	}

	object? Source { get; }

	FrameType Type { get; }

	bool IsActive { get; }

	BindingPriority Priority { get; }

	IEnumerable<ValueEntryDiagnostic> Values { get; }
}
