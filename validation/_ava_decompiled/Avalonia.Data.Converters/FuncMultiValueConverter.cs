using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Avalonia.Data.Converters;

/// <summary>
/// A general purpose <see cref="T:Avalonia.Data.Converters.IValueConverter" /> that uses a <see cref="T:System.Func`2" />
/// to provide the converter logic.
/// </summary>
/// <typeparam name="TIn">The type of the inputs.</typeparam>
/// <typeparam name="TOut">The output type.</typeparam>
public class FuncMultiValueConverter<TIn, TOut> : IMultiValueConverter
{
	private readonly Func<IReadOnlyList<TIn?>, TOut> _convert;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.Converters.FuncMultiValueConverter`2" /> class.
	/// </summary>
	/// <param name="convert">The convert function.</param>
	public FuncMultiValueConverter(Func<IReadOnlyList<TIn?>, TOut> convert)
	{
		_convert = convert;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.Converters.FuncMultiValueConverter`2" /> class.
	/// </summary>
	/// <param name="convert">The convert function.</param>
	public FuncMultiValueConverter(Func<IEnumerable<TIn?>, TOut> convert)
		: this((Func<IReadOnlyList<TIn?>, TOut>)convert.Invoke)
	{
	}

	/// <inheritdoc />
	public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		List<TIn> list = OfTypeWithDefaultSupport(values).ToList();
		if (list.Count == values.Count)
		{
			return _convert(list);
		}
		return AvaloniaProperty.UnsetValue;
		static IEnumerable<TIn?> OfTypeWithDefaultSupport(IList<object?> list2)
		{
			foreach (object item in list2)
			{
				if (item is TIn)
				{
					yield return (TIn)item;
				}
				else if (object.Equals(item, default(TIn)))
				{
					yield return default(TIn);
				}
			}
		}
	}
}
