using System;
using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Rendering;

[PrivateApi]
internal interface IHitTester
{
	/// <summary>
	/// Hit tests a location to find the visuals at the specified point.
	/// </summary>
	/// <remarks>
	/// <para>⚠️ This method is low-level and <b>DOES NOT respect <see cref="P:Avalonia.Input.InputElement.IsHitTestVisible" /></b>.</para>
	/// <para>Use  <see cref="T:Avalonia.Input.InputExtensions" /> to perform input hit testing, or provide your own <paramref name="filter" /> function.</para>
	/// </remarks>
	/// <param name="p">The point, in coordinates relative to <paramref name="root" />.</param>
	/// <param name="root">The root of the subtree to search.</param>
	/// <param name="filter">
	/// A filter predicate. If the predicate returns false then the visual and all its
	/// children will be excluded from the results.
	/// </param>
	/// <returns>The visuals at the specified point, topmost first.</returns>
	IEnumerable<Visual> HitTest(Point p, Visual root, Func<Visual, bool>? filter);

	/// <summary>
	/// Hit tests a location to find first visual at the specified point.
	/// </summary>
	/// <inheritdoc cref="M:Avalonia.Rendering.IHitTester.HitTest(Avalonia.Point,Avalonia.Visual,System.Func{Avalonia.Visual,System.Boolean})" />
	/// <returns>The visual at the specified point, topmost first.</returns>
	Visual? HitTestFirst(Point p, Visual root, Func<Visual, bool>? filter);
}
