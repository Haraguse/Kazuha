using System;

namespace Avalonia.Data.Core.ExpressionNodes;

internal sealed class DataContextNode : DataContextNodeBase
{
	protected override void OnSourceChanged(object? source, Exception? dataValidationError)
	{
		if (ValidateNonNullSource(source))
		{
			if (source is IDataContextProvider && source is AvaloniaObject avaloniaObject)
			{
				avaloniaObject.PropertyChanged += OnPropertyChanged;
				SetValue(avaloniaObject.GetValue(StyledElement.DataContextProperty));
				return;
			}
			SetError($"Unable to read DataContext from '{source.GetType()}'.");
		}
	}

	protected override void Unsubscribe(object oldSource)
	{
		if (oldSource is StyledElement styledElement)
		{
			styledElement.PropertyChanged -= OnPropertyChanged;
		}
	}

	private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (sender == base.Source && e.Property == StyledElement.DataContextProperty)
		{
			SetValue(e.NewValue);
		}
	}
}
