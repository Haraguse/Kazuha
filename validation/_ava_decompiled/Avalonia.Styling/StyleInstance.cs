using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Animation;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.PropertyStore;
using Avalonia.Reactive;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling;

/// <summary>
/// Stores state for a <see cref="T:Avalonia.Styling.Style" /> that has been instanced on a control.
/// </summary>
/// <remarks>
/// <see cref="T:Avalonia.Styling.StyleInstance" /> is based on <see cref="T:Avalonia.PropertyStore.ValueFrame" /> meaning that it is 
/// injected directly into the value store of an <see cref="T:Avalonia.AvaloniaObject" />. Depending on
/// the setters present on the style, it may be possible to share a single style instance
/// among all controls that the style is applied to, meaning that a single style instance can
/// apply to multiple controls.
/// </remarks>
internal class StyleInstance : ValueFrame, IStyleInstance, IStyleActivatorSink, IDisposable
{
	private readonly IStyleActivator? _activator;

	private bool _isActive;

	private List<ISetterInstance>? _setters;

	private List<IAnimation>? _animations;

	private List<IDisposable>? _animationApplyDisposables;

	private LightweightSubject<bool>? _animationTrigger;

	public bool HasActivator => _activator != null;

	public IStyle Source { get; }

	bool IStyleInstance.IsActive => _isActive;

	public StyleInstance(IStyle style, IStyleActivator? activator, FrameType type)
		: base(GetPriority(activator), type)
	{
		_activator = activator;
		Source = style;
	}

	public void Add(ISetterInstance instance)
	{
		if (instance is IValueEntry valueEntry)
		{
			if (Contains(valueEntry.Property))
			{
				throw new InvalidOperationException($"Duplicate setter encountered for property '{valueEntry.Property}' in '{Source}'.");
			}
			Add(valueEntry);
		}
		else
		{
			(_setters ?? (_setters = new List<ISetterInstance>())).Add(instance);
		}
	}

	public void Add(IList<IAnimation> animations)
	{
		if (_animations == null)
		{
			_animations = new List<IAnimation>(animations);
		}
		else
		{
			_animations.AddRange(animations);
		}
	}

	public void ApplyAnimations(AvaloniaObject control)
	{
		if (_animations == null || !(control is Animatable control2))
		{
			return;
		}
		if (_animationTrigger == null)
		{
			_animationTrigger = new LightweightSubject<bool>();
		}
		if (_animationApplyDisposables == null)
		{
			_animationApplyDisposables = new List<IDisposable>();
		}
		foreach (IAnimation animation in _animations)
		{
			_animationApplyDisposables.Add(animation.Apply(control2, null, _animationTrigger));
		}
		if (_activator == null)
		{
			_animationTrigger.OnNext(value: true);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		_activator?.Dispose();
		if (_animationApplyDisposables == null)
		{
			return;
		}
		foreach (IDisposable animationApplyDisposable in _animationApplyDisposables)
		{
			animationApplyDisposable.Dispose();
		}
	}

	public new void MakeShared()
	{
		base.MakeShared();
	}

	void IStyleActivatorSink.OnNext(bool value)
	{
		base.Owner?.OnFrameActivationChanged(this);
		_animationTrigger?.OnNext(value);
	}

	protected override bool GetIsActive(out bool hasChanged)
	{
		bool isActive = _isActive;
		IStyleActivator? activator = _activator;
		if (activator != null && !activator.IsSubscribed)
		{
			_activator.Subscribe(this);
			_animationTrigger?.OnNext(_activator.GetIsActive());
		}
		using Activity activity = ((_activator == null) ? null : Diagnostic.EvaluatingStyle()?.AddTag("Activator", _activator).AddTag("Selector", (Source as Style)?.Selector).AddTag("Style", Source as StyleBase));
		_isActive = _activator?.GetIsActive() ?? true;
		hasChanged = _isActive != isActive;
		activity?.AddTag("IsActive", _isActive);
		return _isActive;
	}

	private static BindingPriority GetPriority(IStyleActivator? activator)
	{
		if (activator == null)
		{
			return BindingPriority.Style;
		}
		return BindingPriority.StyleTrigger;
	}
}
