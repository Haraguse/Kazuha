using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Avalonia.Data;
using Avalonia.Reactive;

namespace Avalonia.Animation;

/// <summary>
/// Defines a KeyFrame that is used for
/// <see cref="T:Avalonia.Animation.Animators.Animator`1" /> objects.
/// </summary>
internal class AnimatorKeyFrame : AvaloniaObject
{
	public static readonly DirectProperty<AnimatorKeyFrame, object?> ValueProperty = AvaloniaProperty.RegisterDirect("Value", (AnimatorKeyFrame k) => k.Value, delegate(AnimatorKeyFrame k, object? v)
	{
		k.Value = v;
	});

	private object? _value;

	public Type? AnimatorType { get; }

	public Func<IAnimator>? AnimatorFactory { get; }

	public Cue Cue { get; }

	public KeySpline? KeySpline { get; }

	public bool FillBefore { get; set; }

	public bool FillAfter { get; set; }

	public AvaloniaProperty? Property { get; private set; }

	public object? Value
	{
		get
		{
			return _value;
		}
		set
		{
			SetAndRaise(ValueProperty, ref _value, value);
		}
	}

	public AnimatorKeyFrame(Type? animatorType, Func<IAnimator>? animatorFactory, Cue cue, KeySpline? keySpline)
	{
		AnimatorType = animatorType;
		AnimatorFactory = animatorFactory;
		Cue = cue;
		KeySpline = keySpline;
	}

	public IDisposable BindSetter(IAnimationSetter setter, Animatable targetControl)
	{
		Property = setter.Property;
		object value = setter.Value;
		if (value is BindingBase binding)
		{
			return Bind(ValueProperty, binding, targetControl);
		}
		return Bind(ValueProperty, Observable.SingleValue(value).ToBinding(), targetControl);
	}

	[RequiresUnreferencedCode("Conversion methods are required for type conversion, including op_Implicit, op_Explicit, Parse and TypeConverter.")]
	public T GetTypedValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
	{
		TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
		if (Value == null)
		{
			throw new ArgumentNullException("KeyFrame value can't be null.");
		}
		object value = Value;
		if (value is T)
		{
			return (T)value;
		}
		if (!converter.CanConvertTo(Value.GetType()))
		{
			throw new InvalidCastException("KeyFrame value doesnt match property type.");
		}
		return (T)converter.ConvertTo(Value, typeof(T));
	}

	internal override void BuildDebugDisplay(StringBuilder builder, bool includeContent)
	{
		base.BuildDebugDisplay(builder, includeContent);
		DebugDisplayHelper.AppendOptionalValue(builder, "Property", Property, includeContent);
		DebugDisplayHelper.AppendOptionalValue(builder, "Cue", Cue, includeContent);
		DebugDisplayHelper.AppendOptionalValue(builder, "Value", Value, includeContent);
	}
}
