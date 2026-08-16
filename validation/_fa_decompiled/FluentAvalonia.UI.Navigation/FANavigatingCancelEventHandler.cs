namespace FluentAvalonia.UI.Navigation;

/// <summary>
/// Represents the method to use as the OnNavigatingFrom callback override.
/// </summary>
/// <param name="sender">The object where the method is implemented.</param>
/// <param name="e">Event data that is passed through the callback.</param>
public delegate void FANavigatingCancelEventHandler(object sender, FANavigatingCancelEventArgs e);
