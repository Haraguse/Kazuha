namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that handles <see cref="T:Avalonia.CornerRadius" /> properties.
/// </summary>
internal class CornerRadiusAnimator : Animator<CornerRadius>
{
	public override CornerRadius Interpolate(double progress, CornerRadius oldValue, CornerRadius newValue)
	{
		double num = newValue.TopLeft - oldValue.TopLeft;
		double num2 = newValue.TopRight - oldValue.TopRight;
		double num3 = newValue.BottomRight - oldValue.BottomRight;
		double num4 = newValue.BottomLeft - oldValue.BottomLeft;
		double topLeft = progress * num + oldValue.TopLeft;
		double topRight = progress * num2 + oldValue.TopRight;
		double bottomRight = progress * num3 + oldValue.BottomRight;
		double bottomLeft = progress * num4 + oldValue.BottomLeft;
		return new CornerRadius(topLeft, topRight, bottomRight, bottomLeft);
	}
}
