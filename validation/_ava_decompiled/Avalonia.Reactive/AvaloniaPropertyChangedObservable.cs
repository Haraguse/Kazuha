using System;

namespace Avalonia.Reactive;

internal class AvaloniaPropertyChangedObservable : LightweightObservableBase<AvaloniaPropertyChangedEventArgs>, IDescription
{
	private readonly WeakReference<AvaloniaObject> _target;

	private readonly AvaloniaProperty _property;

	public string Description => _target.GetType().Name + "." + _property.Name;

	public AvaloniaPropertyChangedObservable(AvaloniaObject target, AvaloniaProperty property)
	{
		_target = new WeakReference<AvaloniaObject>(target);
		_property = property;
	}

	protected override void Initialize()
	{
		if (_target.TryGetTarget(out AvaloniaObject target))
		{
			target.PropertyChanged += PropertyChanged;
		}
	}

	protected override void Deinitialize()
	{
		if (_target.TryGetTarget(out AvaloniaObject target))
		{
			target.PropertyChanged -= PropertyChanged;
		}
	}

	private void PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == _property)
		{
			PublishNext(e);
		}
	}
}
