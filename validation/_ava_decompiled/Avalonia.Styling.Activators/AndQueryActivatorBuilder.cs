namespace Avalonia.Styling.Activators;

/// <summary>
/// Builds an <see cref="T:Avalonia.Styling.Activators.AndActivator" />.
/// </summary>
/// <remarks>
/// When ANDing style activators, if there is more than one input then creates an instance of
/// <see cref="T:Avalonia.Styling.Activators.AndActivator" />. If there is only one input, returns the input directly.
/// </remarks>
internal struct AndQueryActivatorBuilder
{
	private readonly Visual _visual;

	private IStyleActivator? _single;

	private AndQueryActivator? _multiple;

	public int Count => _multiple?.Count ?? ((_single != null) ? 1 : 0);

	public AndQueryActivatorBuilder(Visual visual)
	{
		this = default(AndQueryActivatorBuilder);
		_visual = visual;
	}

	public void Add(IStyleActivator? activator)
	{
		if (activator == null)
		{
			return;
		}
		if (_single == null && _multiple == null)
		{
			_single = activator;
			return;
		}
		if (_multiple == null)
		{
			_multiple = new AndQueryActivator(_visual);
			_multiple.Add(_single);
			_single = null;
		}
		_multiple.Add(activator);
	}

	public IStyleActivator Get()
	{
		return _single ?? _multiple;
	}
}
