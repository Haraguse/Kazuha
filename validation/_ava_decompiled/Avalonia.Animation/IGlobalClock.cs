using System;

namespace Avalonia.Animation;

internal interface IGlobalClock : IClock, IObservable<TimeSpan>
{
}
