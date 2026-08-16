using System;
using Avalonia.Styling;

namespace Avalonia.Platform;

internal class VisualQueryProvider
{
	private readonly Visual _visual;

	public double Width { get; private set; } = double.PositiveInfinity;

	public double Height { get; private set; } = double.PositiveInfinity;

	public event EventHandler? WidthChanged;

	public event EventHandler? HeightChanged;

	public VisualQueryProvider(Visual visual)
	{
		_visual = visual;
	}

	public virtual void SetSize(double width, double height, ContainerSizing containerType)
	{
		double width2 = Width;
		double height2 = Height;
		Width = width;
		Height = height;
		if (width2 != Width && (containerType == ContainerSizing.Width || containerType == ContainerSizing.WidthAndHeight))
		{
			WidthChanged?.Invoke(this, EventArgs.Empty);
		}
		if (height2 != Height && (containerType == ContainerSizing.Height || containerType == ContainerSizing.WidthAndHeight))
		{
			HeightChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
