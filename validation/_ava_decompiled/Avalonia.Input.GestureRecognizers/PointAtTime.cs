using System;

namespace Avalonia.Input.GestureRecognizers;

internal record struct PointAtTime(bool Valid, Vector Point, TimeSpan Time);
