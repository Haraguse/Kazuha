using Avalonia.Controls;

namespace Luminalium.Plugins;

public interface IPluginCommand
{
    /// <summary>
    /// Runs plugin command behavior without requiring a UI view.
    /// </summary>
    Task<PluginExecutionResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Stops plugin command behavior. Implementations must tolerate repeated termination requests.
    /// </summary>
    Task TerminateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Creates an Avalonia view for UI hosts. Non-UI consumers can use metadata and commands without this surface.
/// </summary>
public interface IPluginViewFactory
{
    Control CreateView();
}
