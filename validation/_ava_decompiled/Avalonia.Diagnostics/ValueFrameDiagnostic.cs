using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.PropertyStore;

namespace Avalonia.Diagnostics;

internal sealed class ValueFrameDiagnostic : IValueFrameDiagnostic
{
	private readonly ValueFrame _valueFrame;

	public object? Source => _valueFrame.Owner?.Owner;

	public IValueFrameDiagnostic.FrameType Type => IValueFrameDiagnostic.FrameType.Template;

	public bool IsActive => _valueFrame.IsActive();

	public BindingPriority Priority => _valueFrame.FramePriority.ToBindingPriority();

	public IEnumerable<ValueEntryDiagnostic> Values
	{
		get
		{
			for (int i = 0; i < _valueFrame.EntryCount; i++)
			{
				IValueEntry entry = _valueFrame.GetEntry(i);
				if (entry.HasValue())
				{
					yield return new ValueEntryDiagnostic(entry.Property, entry.GetValue());
				}
			}
		}
	}

	internal ValueFrameDiagnostic(ValueFrame valueFrame)
	{
		_valueFrame = valueFrame;
	}
}
