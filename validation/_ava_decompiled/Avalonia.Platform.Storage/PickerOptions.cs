namespace Avalonia.Platform.Storage;

/// <summary>
/// Common options for <see cref="M:Avalonia.Platform.Storage.IStorageProvider.OpenFolderPickerAsync(Avalonia.Platform.Storage.FolderPickerOpenOptions)" />, <see cref="M:Avalonia.Platform.Storage.IStorageProvider.OpenFilePickerAsync(Avalonia.Platform.Storage.FilePickerOpenOptions)" /> and <see cref="M:Avalonia.Platform.Storage.IStorageProvider.SaveFilePickerAsync(Avalonia.Platform.Storage.FilePickerSaveOptions)" /> methods. 
/// </summary>
public class PickerOptions
{
	/// <summary>
	/// Gets or sets the text that appears in the title bar of a picker.
	/// </summary>
	public string? Title { get; set; }

	/// <summary>
	/// Gets or sets the initial location where the file open picker looks for files to present to the user.
	/// Can be obtained from previously picked folder or using <see cref="M:Avalonia.Platform.Storage.IStorageProvider.TryGetFolderFromPathAsync(System.Uri)" />
	/// or <see cref="M:Avalonia.Platform.Storage.IStorageProvider.TryGetWellKnownFolderAsync(Avalonia.Platform.Storage.WellKnownFolder)" />.
	/// </summary>
	public IStorageFolder? SuggestedStartLocation { get; set; }

	/// <summary>
	/// Gets or sets the file name that the file picker suggests to the user.
	/// </summary>
	public string? SuggestedFileName { get; set; }
}
