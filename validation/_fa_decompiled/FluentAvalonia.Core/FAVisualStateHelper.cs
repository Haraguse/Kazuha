using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using FluentAvalonia.Core.Internal;

namespace FluentAvalonia.Core;

/// <summary>
/// Visual State Helper
/// </summary>
public sealed class FAVisualStateHelper
{
	/// <summary>
	/// Forced Classes 
	/// </summary>
	public static readonly AttachedProperty<string> ForcedClassesProperty;

	static FAVisualStateHelper()
	{
		ForcedClassesProperty = AvaloniaProperty.RegisterAttached<FAVisualStateHelper, StyledElement, string>("ForcedClasses", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null);
		((AvaloniaProperty<string>)(object)ForcedClassesProperty).Changed.Subscribe(OnForcedClassesPropertyChanged);
	}

	/// <summary>
	/// Get value of <see cref="F:FluentAvalonia.Core.FAVisualStateHelper.ForcedClassesProperty" /> property.
	/// </summary>
	/// <param name="element"></param>
	/// <returns></returns>
	public static string GetForcedClassesProperty(StyledElement element)
	{
		return ((AvaloniaObject)element).GetValue<string>((StyledProperty<string>)(object)ForcedClassesProperty);
	}

	/// <summary>
	/// Set value of <see cref="F:FluentAvalonia.Core.FAVisualStateHelper.ForcedClassesProperty" /> property.
	/// </summary>
	/// <param name="element"></param>
	/// <param name="classes"></param>
	public static void SetForcedClassesProperty(StyledElement element, string classes)
	{
		((AvaloniaObject)element).SetValue<string>((StyledProperty<string>)(object)ForcedClassesProperty, classes, (BindingPriority)0);
	}

	private static void OnForcedClassesPropertyChanged(AvaloniaPropertyChangedEventArgs<string> args)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		AvaloniaObject sender = ((AvaloniaPropertyChangedEventArgs)args).Sender;
		StyledElement val = (StyledElement)(object)((sender is StyledElement) ? sender : null);
		if (val != null)
		{
			SetClasses(val, args.OldValue.GetValueOrDefault<string>(), set: false);
			SetClasses(val, args.NewValue.GetValueOrDefault<string>(), set: true);
		}
	}

	private static void SetClasses(StyledElement element, string classes, bool set)
	{
		if (string.IsNullOrEmpty(classes))
		{
			return;
		}
		CharacterReader characterReader = new CharacterReader(classes.AsSpan());
		while (!characterReader.End)
		{
			ReadOnlySpan<char> readOnlySpan = characterReader.TakeUntil(',');
			if (readOnlySpan[readOnlySpan.Length - 1] == '!')
			{
				readOnlySpan = readOnlySpan.Slice(0, readOnlySpan.Length - 1);
				PseudoClassesExtensions.Set((IPseudoClasses)(object)element.Classes, readOnlySpan.ToString(), false);
			}
			else
			{
				PseudoClassesExtensions.Set((IPseudoClasses)(object)element.Classes, readOnlySpan.ToString(), set);
			}
			if (!characterReader.End)
			{
				characterReader.Skip(1);
			}
		}
	}
}
