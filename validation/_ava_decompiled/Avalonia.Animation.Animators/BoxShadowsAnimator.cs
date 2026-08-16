using Avalonia.Media;

namespace Avalonia.Animation.Animators;

internal class BoxShadowsAnimator : Animator<BoxShadows>
{
	private static readonly BoxShadowAnimator s_boxShadowAnimator = new BoxShadowAnimator();

	public override BoxShadows Interpolate(double progress, BoxShadows oldValue, BoxShadows newValue)
	{
		int num = ((progress >= 1.0) ? newValue.Count : oldValue.Count);
		if (num == 0)
		{
			return default(BoxShadows);
		}
		BoxShadow boxShadow = ((oldValue.Count > 0 && newValue.Count > 0) ? s_boxShadowAnimator.Interpolate(progress, oldValue[0], newValue[0]) : ((oldValue.Count <= 0) ? newValue[0] : oldValue[0]));
		if (num == 1)
		{
			return new BoxShadows(boxShadow);
		}
		BoxShadow[] array = new BoxShadow[num - 1];
		for (int i = 0; i < array.Length; i++)
		{
			int num2 = i + 1;
			if (oldValue.Count > num2 && newValue.Count > num2)
			{
				array[i] = s_boxShadowAnimator.Interpolate(progress, oldValue[num2], newValue[num2]);
			}
			else if (oldValue.Count > num2)
			{
				array[i] = oldValue[num2];
			}
			else
			{
				array[i] = newValue[num2];
			}
		}
		return new BoxShadows(boxShadow, array);
	}
}
