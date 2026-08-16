namespace Avalonia.Platform.Storage;

/// <summary>
/// Options class for <see cref="M:Avalonia.Platform.Storage.IStorageProvider.OpenFolderPickerAsync(Avalonia.Platform.Storage.FolderPickerOpenOptions)" /> method.
/// </summary>
public class FolderPickerOpenOptions : PickerOptions
{
	/// <summary>
	/// Gets or sets an option indicating whether open picker allows users to select multiple folders.
	/// </summary>
	public bool AllowMultiple { get; set; }
}
