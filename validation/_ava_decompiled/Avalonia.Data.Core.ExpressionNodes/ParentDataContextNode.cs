using System;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in an <see cref="T:Avalonia.Data.Core.BindingExpression" /> which selects the value of the visual
/// parent's DataContext.
/// </summary>
internal sealed class ParentDataContextNode : DataContextNodeBase
{
	private static readonly AvaloniaObject s_unset = new AvaloniaObject();

	private AvaloniaObject? _parent = s_unset;

	protected override void OnSourceChanged(object? source, Exception? dataValidationError)
	{
		if (ValidateNonNullSource(source))
		{
			if (source is AvaloniaObject avaloniaObject)
			{
				avaloniaObject.PropertyChanged += OnPropertyChanged;
			}
			if (source is Visual visual)
			{
				SetParent(visual.GetValue(Visual.VisualParentProperty));
			}
			else
			{
				SetParent(null);
			}
		}
	}

	protected override void Unsubscribe(object oldSource)
	{
		if (oldSource is AvaloniaObject avaloniaObject)
		{
			avaloniaObject.PropertyChanged -= OnPropertyChanged;
		}
	}

	private void SetParent(AvaloniaObject? parent)
	{
		if (parent != _parent)
		{
			Unsubscribe();
			_parent = parent;
			if (_parent is IDataContextProvider)
			{
				_parent.PropertyChanged += OnParentPropertyChanged;
				SetValue(_parent.GetValue(StyledElement.DataContextProperty));
			}
			else
			{
				SetValue(null);
			}
		}
	}

	private void Unsubscribe()
	{
		if (_parent != null)
		{
			_parent.PropertyChanged -= OnParentPropertyChanged;
		}
	}

	private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == Visual.VisualParentProperty)
		{
			SetParent(e.NewValue as AvaloniaObject);
		}
	}

	private void OnParentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == StyledElement.DataContextProperty)
		{
			SetValue(e.NewValue);
		}
	}
}
