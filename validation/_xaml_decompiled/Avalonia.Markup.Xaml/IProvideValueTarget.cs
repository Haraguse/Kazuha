namespace Avalonia.Markup.Xaml;

public interface IProvideValueTarget
{
	object TargetObject { get; }

	object TargetProperty { get; }
}
