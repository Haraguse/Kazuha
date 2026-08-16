using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation;

public abstract class InterpolatingAnimator<T> : ICustomAnimator
{
	internal class AnimatorWrapper : Animator<T>
	{
		private readonly InterpolatingAnimator<T> _parent;

		public AnimatorWrapper(InterpolatingAnimator<T> parent)
		{
			_parent = parent;
		}

		public override T Interpolate(double progress, T oldValue, T newValue)
		{
			return _parent.Interpolate(progress, oldValue, newValue);
		}
	}

	Type ICustomAnimator.WrapperType => typeof(AnimatorWrapper);

	public abstract T Interpolate(double progress, T oldValue, T newValue);

	IAnimator ICustomAnimator.CreateWrapper()
	{
		return new AnimatorWrapper(this);
	}

	internal IAnimator CreateWrapper()
	{
		return new AnimatorWrapper(this);
	}
}
