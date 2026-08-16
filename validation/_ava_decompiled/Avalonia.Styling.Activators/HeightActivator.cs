using Avalonia.Layout;
using Avalonia.Platform;

namespace Avalonia.Styling.Activators;

internal sealed class HeightActivator : ContainerQueryActivatorBase
{
	private readonly (StyleQueryComparisonOperator @operator, double value) _argument;

	public HeightActivator(Visual visual, (StyleQueryComparisonOperator @operator, double value) argument, string? containerName = null)
		: base(visual, containerName)
	{
		_argument = argument;
	}

	protected override bool EvaluateIsActive()
	{
		Layoutable currentContainer = base.CurrentContainer;
		ContainerSizing sizing = default(ContainerSizing);
		VisualQueryProvider queryProvider = default(VisualQueryProvider);
		int num;
		if (currentContainer != null)
		{
			sizing = Container.GetSizing(currentContainer);
			queryProvider = Container.GetQueryProvider(currentContainer);
			num = ((queryProvider != null) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
		if (flag)
		{
			bool flag2 = (uint)(sizing - 2) <= 1u;
			flag = flag2;
		}
		if (flag)
		{
			return HeightQuery.Evaluate(queryProvider, _argument).IsMatch;
		}
		return false;
	}
}
