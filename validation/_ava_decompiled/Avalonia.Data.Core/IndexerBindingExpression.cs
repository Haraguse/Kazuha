namespace Avalonia.Data.Core;

internal class IndexerBindingExpression : UntypedBindingExpressionBase
{
	private readonly AvaloniaObject _source;

	private readonly AvaloniaProperty _sourceProperty;

	private readonly AvaloniaObject _target;

	private readonly AvaloniaProperty? _targetProperty;

	private readonly BindingMode _mode;

	public override string Description => $"IndexerBinding {_sourceProperty})";

	public IndexerBindingExpression(AvaloniaObject source, AvaloniaProperty sourceProperty, AvaloniaObject target, AvaloniaProperty? targetProperty, BindingMode mode)
		: base(BindingPriority.LocalValue)
	{
		_source = source;
		_sourceProperty = sourceProperty;
		_target = target;
		_targetProperty = targetProperty;
		_mode = mode;
	}

	internal override bool WriteValueToSource(object? value)
	{
		_source.SetValue(_sourceProperty, value);
		return true;
	}

	protected override void StartCore()
	{
		BindingMode mode = _mode;
		bool flag = ((mode == BindingMode.TwoWay || mode == BindingMode.OneWayToSource) ? true : false);
		if (flag && (object)_targetProperty != null)
		{
			_target.PropertyChanged += OnTargetPropertyChanged;
		}
		if (_mode != BindingMode.OneWayToSource)
		{
			_source.PropertyChanged += OnSourcePropertyChanged;
			PublishValue(_source.GetValue(_sourceProperty));
		}
		if (_mode == BindingMode.OneTime)
		{
			Stop();
		}
	}

	protected override void StopCore()
	{
		_source.PropertyChanged -= OnSourcePropertyChanged;
		_target.PropertyChanged -= OnTargetPropertyChanged;
	}

	private void OnSourcePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == _sourceProperty)
		{
			PublishValue(_source.GetValue(_sourceProperty));
		}
	}

	private void OnTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == _targetProperty)
		{
			WriteValueToSource(e.NewValue);
		}
	}
}
