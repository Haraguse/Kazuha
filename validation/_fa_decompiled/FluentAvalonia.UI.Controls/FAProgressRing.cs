using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using FluentAvalonia.UI.Controls.Primitives;

namespace FluentAvalonia.UI.Controls;

[TemplatePart("AnimatedVisual", typeof(FAProgressRingAnimatedVisual))]
public class FAProgressRing : RangeBase
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAProgressRing.IsActive" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsActiveProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAProgressRing.IsIndeterminate" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsIndeterminateProperty;

	private FAProgressRingAnimatedVisual _animatedVisualSource;

	private const string _tpAnimatedVisual = "AnimatedVisual";

	/// <summary>
	/// Gets or sets a value that indicates whether the ProgressRing is showing progress
	/// </summary>
	public bool IsActive
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsActiveProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsActiveProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the progress ring reports generic progress 
	/// with a repeating pattern or reports progress based on the Value property.
	/// </summary>
	public bool IsIndeterminate
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsIndeterminateProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsIndeterminateProperty, value, (BindingPriority)0);
		}
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		((TemplatedControl)this).OnApplyTemplate(e);
		_animatedVisualSource = NameScopeExtensions.Get<FAProgressRingAnimatedVisual>(e.NameScope, "AnimatedVisual");
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Expected O, but got Unknown
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Expected O, but got Unknown
		((RangeBase)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)RangeBase.ValueProperty)
		{
			_animatedVisualSource?.SetValue(AvaloniaPropertyChangedExtensions.GetNewValue<double>(change));
		}
		else if (change.Property == (AvaloniaProperty)(object)RangeBase.MinimumProperty)
		{
			_animatedVisualSource?.SetMinimum(AvaloniaPropertyChangedExtensions.GetNewValue<double>(change));
		}
		else if (change.Property == (AvaloniaProperty)(object)RangeBase.MaximumProperty)
		{
			_animatedVisualSource?.SetMaximum(AvaloniaPropertyChangedExtensions.GetNewValue<double>(change));
		}
		else if (change.Property == (AvaloniaProperty)(object)IsIndeterminateProperty)
		{
			_animatedVisualSource?.SetIndeterminate(AvaloniaPropertyChangedExtensions.GetNewValue<bool>(change));
		}
		else if (change.Property == (AvaloniaProperty)(object)IsActiveProperty)
		{
			_animatedVisualSource?.SetActive(AvaloniaPropertyChangedExtensions.GetNewValue<bool>(change));
		}
		else if (change.Property == (AvaloniaProperty)(object)TemplatedControl.ForegroundProperty)
		{
			_animatedVisualSource?.SetForeground((IBrush)change.NewValue);
		}
		else if (change.Property == (AvaloniaProperty)(object)TemplatedControl.BackgroundProperty)
		{
			_animatedVisualSource?.SetBackground((IBrush)change.NewValue);
		}
	}

	static FAProgressRing()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		IsActiveProperty = AvaloniaProperty.Register<FAProgressRing, bool>("IsActive", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		IsIndeterminateProperty = ProgressBar.IsIndeterminateProperty.AddOwner<FAProgressRing>(new StyledPropertyMetadata<bool>(Optional<bool>.op_Implicit(true), (BindingMode)0, (Func<AvaloniaObject, bool, bool>)null, false));
	}
}
