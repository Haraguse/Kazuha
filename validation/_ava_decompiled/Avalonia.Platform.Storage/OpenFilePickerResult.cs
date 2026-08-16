using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Avalonia.Platform.Storage;

/// <summary>
/// Represents the result of the <see cref="M:Avalonia.Platform.Storage.IStorageProvider.OpenFilePickerWithResultAsync(Avalonia.Platform.Storage.FilePickerOpenOptions)" /> operation.
/// </summary>
public readonly record struct OpenFilePickerResult
{
	/// <summary>
	/// Gets the list of files selected by the user, or empty if the user canceled the dialog.
	/// </summary>
	public IReadOnlyList<IStorageFile> Files
	{
		get
		{
			return _003CFiles_003Ek__BackingField ?? Array.Empty<IStorageFile>();
		}
		[CompilerGenerated]
		init
		{
			_003CFiles_003Ek__BackingField = value;
		}
	}

	/// <summary>
	/// Gets the file type selected by the user, or null if the platform does not support this feature.
	/// </summary>
	public FilePickerFileType? SelectedFileType { get; init; }

	[CompilerGenerated]
	private readonly IReadOnlyList<IStorageFile> _003CFiles_003Ek__BackingField;

	[CompilerGenerated]
	public override int GetHashCode()
	{
		return EqualityComparer<IReadOnlyList<IStorageFile>>.Default.GetHashCode(_003CFiles_003Ek__BackingField) * -1521134295 + EqualityComparer<FilePickerFileType>.Default.GetHashCode(SelectedFileType);
	}

	[CompilerGenerated]
	public bool Equals(OpenFilePickerResult other)
	{
		if (EqualityComparer<IReadOnlyList<IStorageFile>>.Default.Equals(_003CFiles_003Ek__BackingField, other._003CFiles_003Ek__BackingField))
		{
			return EqualityComparer<FilePickerFileType>.Default.Equals(SelectedFileType, other.SelectedFileType);
		}
		return false;
	}
}
