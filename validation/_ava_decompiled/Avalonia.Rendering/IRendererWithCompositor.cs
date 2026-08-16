using System;
using Avalonia.Rendering.Composition;

namespace Avalonia.Rendering;

internal interface IRendererWithCompositor : IRenderer, IDisposable
{
	/// <summary>
	/// The associated <see cref="T:Avalonia.Rendering.Composition.Compositor" /> object
	/// </summary>
	Compositor Compositor { get; }
}
