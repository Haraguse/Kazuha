namespace Avalonia;

/// <summary>
/// Provides extensions for <see cref="T:Avalonia.AvaloniaPropertyChangedEventArgs" />.
/// </summary>
public static class AvaloniaPropertyChangedExtensions
{
	/// <summary>
	/// Gets a typed value from <see cref="P:Avalonia.AvaloniaPropertyChangedEventArgs.OldValue" />.
	/// </summary>
	/// <typeparam name="T">The value type.</typeparam>
	/// <param name="e">The event args.</param>
	/// <returns>The value.</returns>
	public static T GetOldValue<T>(this AvaloniaPropertyChangedEventArgs e)
	{
		return ((AvaloniaPropertyChangedEventArgs<T>)e).OldValue.GetValueOrDefault();
	}

	/// <summary>
	/// Gets a typed value from <see cref="P:Avalonia.AvaloniaPropertyChangedEventArgs.NewValue" />.
	/// </summary>
	/// <typeparam name="T">The value type.</typeparam>
	/// <param name="e">The event args.</param>
	/// <returns>The value.</returns>
	public static T GetNewValue<T>(this AvaloniaPropertyChangedEventArgs e)
	{
		return ((AvaloniaPropertyChangedEventArgs<T>)e).NewValue.GetValueOrDefault();
	}

	/// <summary>
	/// Gets a typed value from <see cref="P:Avalonia.AvaloniaPropertyChangedEventArgs.OldValue" /> and
	/// <see cref="P:Avalonia.AvaloniaPropertyChangedEventArgs.NewValue" />.
	/// </summary>
	/// <typeparam name="T">The value type.</typeparam>
	/// <param name="e">The event args.</param>
	/// <returns>The value.</returns>
	public static (T oldValue, T newValue) GetOldAndNewValue<T>(this AvaloniaPropertyChangedEventArgs e)
	{
		AvaloniaPropertyChangedEventArgs<T> e2 = (AvaloniaPropertyChangedEventArgs<T>)e;
		return (oldValue: e2.OldValue.GetValueOrDefault(), newValue: e2.NewValue.GetValueOrDefault());
	}
}
