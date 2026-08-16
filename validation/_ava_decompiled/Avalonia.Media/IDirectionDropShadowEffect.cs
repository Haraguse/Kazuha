namespace Avalonia.Media;

internal interface IDirectionDropShadowEffect : IDropShadowEffect, IEffect
{
	double Direction { get; }

	double ShadowDepth { get; }
}
