using System;

namespace Avalonia.Animation;

public interface ICustomAnimator
{
	internal Type WrapperType { get; }

	internal IAnimator CreateWrapper();
}
