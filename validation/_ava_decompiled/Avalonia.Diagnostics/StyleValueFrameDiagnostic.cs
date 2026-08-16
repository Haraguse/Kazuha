using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.PropertyStore;
using Avalonia.Styling;

namespace Avalonia.Diagnostics;

internal class StyleValueFrameDiagnostic : IValueFrameDiagnostic
{
	private readonly StyleInstance _styleInstance;

	public object? Source => _styleInstance.Source;

	public IValueFrameDiagnostic.FrameType Type
	{
		get
		{
			IStyle source = _styleInstance.Source;
			if (!(source is Style))
			{
				if (source is ControlTheme)
				{
					return IValueFrameDiagnostic.FrameType.Theme;
				}
				return IValueFrameDiagnostic.FrameType.Unknown;
			}
			return IValueFrameDiagnostic.FrameType.Style;
		}
	}

	public bool IsActive => _styleInstance.IsActive();

	public BindingPriority Priority => _styleInstance.FramePriority.ToBindingPriority();

	public IEnumerable<ValueEntryDiagnostic> Values
	{
		get
		{
			foreach (SetterBase setter2 in ((StyleBase)_styleInstance.Source).Setters)
			{
				if (setter2 is Setter { Property: not null } setter)
				{
					yield return new ValueEntryDiagnostic(setter.Property, setter.Value);
				}
			}
		}
	}

	internal StyleValueFrameDiagnostic(StyleInstance styleInstance)
	{
		_styleInstance = styleInstance;
	}
}
