namespace Avalonia.Media;

public interface IDropShadowEffect : IEffect
{
	double OffsetX { get; }

	double OffsetY { get; }

	double BlurRadius { get; }

	Color Color { get; }

	double Opacity { get; }
}
