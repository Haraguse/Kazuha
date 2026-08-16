using System;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Styling;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Special helper class to enable WinUI like animations on the Expander control
/// </summary>
public sealed class FAExpanderExt : AvaloniaObject
{
	private class ExpanderInfo : IDisposable
	{
		private readonly Expander _expander;

		private Border _expanderContent;

		private Size _contentSize;

		private IDisposable _expandedChangedNotice;

		public ExpanderInfo(Expander expander)
		{
			_expander = expander;
			((TemplatedControl)expander).TemplateApplied += HandleExpanderTemplateApplied;
			_expandedChangedNotice = AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)expander, (AvaloniaProperty)(object)Expander.IsExpandedProperty).Subscribe(new SimpleObserver<AvaloniaPropertyChangedEventArgs>(HandleIsExpandedChanged));
		}

		private void HandleExpanderTemplateApplied(object sender, TemplateAppliedEventArgs e)
		{
			if (_expanderContent != null)
			{
				((Control)_expanderContent).SizeChanged -= HandleContentSizeChanged;
			}
			ElementComposition.GetElementVisual((Visual)(object)NameScopeExtensions.Get<Border>(e.NameScope, "ExpanderContentClip")).ClipToBounds = true;
			Border expanderContent = NameScopeExtensions.Get<Border>(e.NameScope, "ExpanderContent");
			SetExpanderContent(expanderContent);
			UpdateExpandState(useTransitions: false);
		}

		private void SetExpanderContent(Border expanderContent)
		{
			_expanderContent = expanderContent;
			((Control)expanderContent).SizeChanged += HandleContentSizeChanged;
		}

		private void HandleContentSizeChanged(object sender, SizeChangedEventArgs e)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			_contentSize = e.NewSize;
		}

		private void HandleIsExpandedChanged(AvaloniaPropertyChangedEventArgs args)
		{
			UpdateExpandState(useTransitions: true);
		}

		private void UpdateExpandState(bool useTransitions)
		{
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Invalid comparison between Unknown and I4
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Invalid comparison between Unknown and I4
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Invalid comparison between Unknown and I4
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Invalid comparison between Unknown and I4
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Invalid comparison between Unknown and I4
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Invalid comparison between Unknown and I4
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Invalid comparison between Unknown and I4
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Invalid comparison between Unknown and I4
			bool isExpanded = _expander.IsExpanded;
			if (useTransitions && _expanderContent == null)
			{
				useTransitions = false;
			}
			Classes classes = ((StyledElement)_expander).Classes;
			PseudoClassesExtensions.Set((IPseudoClasses)(object)classes, ":noAnimation", !useTransitions);
			PseudoClassesExtensions.Set((IPseudoClasses)(object)classes, ":expanded", isExpanded);
			if (!useTransitions)
			{
				return;
			}
			ExpandDirection expandDirection = _expander.ExpandDirection;
			if (isExpanded)
			{
				if ((int)expandDirection > 1)
				{
					if (expandDirection - 2 <= 1)
					{
						RunExpandLeftRightAnimation((int)expandDirection == 3);
					}
				}
				else
				{
					RunExpandDownUpAnimation((int)expandDirection == 0);
				}
			}
			else if ((int)expandDirection > 1)
			{
				if (expandDirection - 2 <= 1)
				{
					RunCollapseLeftRightAnimation((int)expandDirection == 3);
				}
			}
			else
			{
				RunCollapseDownUpAnimation((int)expandDirection == 0);
			}
		}

		private async void RunExpandDownUpAnimation(bool down)
		{
			((AvaloniaObject)_expanderContent).SetCurrentValue<bool>(Visual.IsVisibleProperty, true);
			if (((StyledElement)_expander).Parent is FASettingsExpander fASettingsExpander && ((ItemsControl)fASettingsExpander).Presenter != null)
			{
				((Layoutable)((ItemsControl)fASettingsExpander).Presenter).Measure(Size.Infinity);
				_contentSize = ((Layoutable)((ItemsControl)fASettingsExpander).Presenter).DesiredSize;
			}
			else
			{
				((Layoutable)_expanderContent).Measure(Size.Infinity);
				_contentSize = ((Layoutable)_expanderContent).DesiredSize;
			}
			double num = (down ? (0.0 - ((Size)(ref _contentSize)).Height) : ((Size)(ref _contentSize)).Height);
			Animation val = new Animation
			{
				Duration = TimeSpan.FromMilliseconds(333L),
				FillMode = (FillMode)1
			};
			KeyFrames children = val.Children;
			KeyFrame val2 = new KeyFrame
			{
				KeyTime = TimeSpan.Zero
			};
			val2.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)Visual.IsVisibleProperty, (object)true));
			val2.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)TranslateTransform.YProperty, (object)num));
			((AvaloniaList<KeyFrame>)(object)children).Add(val2);
			KeyFrames children2 = val.Children;
			KeyFrame val3 = new KeyFrame
			{
				KeyTime = TimeSpan.FromMilliseconds(333L)
			};
			val3.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)TranslateTransform.YProperty, (object)0.0));
			val3.KeySpline = new KeySpline(0.0, 0.0, 0.0, 1.0);
			((AvaloniaList<KeyFrame>)(object)children2).Add(val3);
			await val.RunAsync((Animatable)(object)_expanderContent, default(CancellationToken));
		}

		private async void RunCollapseDownUpAnimation(bool down)
		{
			double num = (down ? (0.0 - ((Size)(ref _contentSize)).Height) : ((Size)(ref _contentSize)).Height);
			Animation val = new Animation
			{
				Duration = TimeSpan.FromMilliseconds(167L),
				FillMode = (FillMode)1
			};
			KeyFrames children = val.Children;
			KeyFrame val2 = new KeyFrame
			{
				KeyTime = TimeSpan.Zero
			};
			val2.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)TranslateTransform.YProperty, (object)0.0));
			((AvaloniaList<KeyFrame>)(object)children).Add(val2);
			KeyFrames children2 = val.Children;
			KeyFrame val3 = new KeyFrame
			{
				KeyTime = TimeSpan.FromMilliseconds(167L)
			};
			val3.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)TranslateTransform.YProperty, (object)num));
			val3.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)Visual.IsVisibleProperty, (object)false));
			val3.KeySpline = new KeySpline(1.0, 1.0, 0.0, 1.0);
			((AvaloniaList<KeyFrame>)(object)children2).Add(val3);
			await val.RunAsync((Animatable)(object)_expanderContent, default(CancellationToken));
			((AvaloniaObject)_expanderContent).SetValue<bool>(Visual.IsVisibleProperty, false, (BindingPriority)0);
		}

		private async void RunExpandLeftRightAnimation(bool right)
		{
			((AvaloniaObject)_expanderContent).SetCurrentValue<bool>(Visual.IsVisibleProperty, true);
			((Layoutable)_expanderContent).Measure(Size.Infinity);
			_contentSize = ((Layoutable)_expanderContent).DesiredSize;
			double num = (right ? (0.0 - ((Size)(ref _contentSize)).Width) : ((Size)(ref _contentSize)).Width);
			Animation val = new Animation
			{
				Duration = TimeSpan.FromMilliseconds(333L),
				FillMode = (FillMode)1
			};
			KeyFrames children = val.Children;
			KeyFrame val2 = new KeyFrame
			{
				KeyTime = TimeSpan.Zero
			};
			val2.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)Visual.IsVisibleProperty, (object)true));
			val2.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)TranslateTransform.XProperty, (object)num));
			((AvaloniaList<KeyFrame>)(object)children).Add(val2);
			KeyFrames children2 = val.Children;
			KeyFrame val3 = new KeyFrame
			{
				KeyTime = TimeSpan.FromMilliseconds(333L)
			};
			val3.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)TranslateTransform.XProperty, (object)0.0));
			val3.KeySpline = new KeySpline(0.0, 0.0, 0.0, 1.0);
			((AvaloniaList<KeyFrame>)(object)children2).Add(val3);
			await val.RunAsync((Animatable)(object)_expanderContent, default(CancellationToken));
		}

		private async void RunCollapseLeftRightAnimation(bool right)
		{
			double num = (right ? (0.0 - ((Size)(ref _contentSize)).Width) : ((Size)(ref _contentSize)).Width);
			Animation val = new Animation
			{
				Duration = TimeSpan.FromMilliseconds(167L),
				FillMode = (FillMode)1
			};
			KeyFrames children = val.Children;
			KeyFrame val2 = new KeyFrame
			{
				KeyTime = TimeSpan.Zero
			};
			val2.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)TranslateTransform.XProperty, (object)0.0));
			((AvaloniaList<KeyFrame>)(object)children).Add(val2);
			KeyFrames children2 = val.Children;
			KeyFrame val3 = new KeyFrame
			{
				KeyTime = TimeSpan.FromMilliseconds(167L)
			};
			val3.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)TranslateTransform.XProperty, (object)num));
			val3.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)Visual.IsVisibleProperty, (object)false));
			val3.KeySpline = new KeySpline(1.0, 1.0, 0.0, 1.0);
			((AvaloniaList<KeyFrame>)(object)children2).Add(val3);
			await val.RunAsync((Animatable)(object)_expanderContent, default(CancellationToken));
			((AvaloniaObject)_expanderContent).SetValue<bool>(Visual.IsVisibleProperty, false, (BindingPriority)0);
		}

		public void Dispose()
		{
			_expandedChangedNotice?.Dispose();
			((TemplatedControl)_expander).TemplateApplied -= HandleExpanderTemplateApplied;
			if (_expanderContent != null)
			{
				((Control)_expanderContent).SizeChanged -= HandleContentSizeChanged;
			}
		}
	}

	/// <summary>
	/// Defines the ExpanderAnimationType attached property
	/// </summary>
	public static readonly AttachedProperty<string> ExpanderAnimationTypeProperty;

	private static readonly AttachedProperty<ExpanderInfo> ExpanderAnimationInfoProperty;

	private static readonly string s_Fluentv2;

	static FAExpanderExt()
	{
		ExpanderAnimationTypeProperty = AvaloniaProperty.RegisterAttached<FAExpanderExt, Expander, string>("ExpanderAnimationType", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null);
		ExpanderAnimationInfoProperty = AvaloniaProperty.RegisterAttached<FAExpanderExt, Expander, ExpanderInfo>("ExpanderAnimationInfo", (ExpanderInfo)null, false, (BindingMode)1, (Func<ExpanderInfo, bool>)null, (Func<AvaloniaObject, ExpanderInfo, ExpanderInfo>)null);
		s_Fluentv2 = "FluentV2";
		((AvaloniaProperty<string>)(object)ExpanderAnimationTypeProperty).Changed.Subscribe((IObserver<AvaloniaPropertyChangedEventArgs<string>>)new SimpleObserver<AvaloniaPropertyChangedEventArgs>(HandleExpanderAnimationTypeChanged));
	}

	/// <summary>
	/// Gets the current value of the <see cref="F:FluentAvalonia.UI.Controls.FAExpanderExt.ExpanderAnimationTypeProperty" />
	/// </summary>
	public static string GetExpanderAnimationType(Expander exp)
	{
		return ((AvaloniaObject)exp).GetValue<string>((StyledProperty<string>)(object)ExpanderAnimationTypeProperty);
	}

	/// <summary>
	/// Sets the current value of the <see cref="F:FluentAvalonia.UI.Controls.FAExpanderExt.ExpanderAnimationTypeProperty" />
	/// </summary>
	public static void SetExpanderAnimationType(Expander exp, string value)
	{
		((AvaloniaObject)exp).SetValue<string>((StyledProperty<string>)(object)ExpanderAnimationTypeProperty, value, (BindingPriority)0);
	}

	private static void HandleExpanderAnimationTypeChanged(AvaloniaPropertyChangedEventArgs args)
	{
		string newValue = AvaloniaPropertyChangedExtensions.GetNewValue<string>(args);
		AvaloniaObject sender = args.Sender;
		Expander val = (Expander)(object)((sender is Expander) ? sender : null);
		if (val != null)
		{
			if (newValue != null && newValue.Equals(s_Fluentv2, StringComparison.OrdinalIgnoreCase))
			{
				((AvaloniaObject)val).SetValue<ExpanderInfo>((StyledProperty<ExpanderInfo>)(object)ExpanderAnimationInfoProperty, new ExpanderInfo(val), (BindingPriority)0);
				return;
			}
			((AvaloniaObject)val).GetValue<ExpanderInfo>((StyledProperty<ExpanderInfo>)(object)ExpanderAnimationInfoProperty)?.Dispose();
			((AvaloniaObject)val).ClearValue<ExpanderInfo>((StyledProperty<ExpanderInfo>)(object)ExpanderAnimationInfoProperty);
		}
	}
}
