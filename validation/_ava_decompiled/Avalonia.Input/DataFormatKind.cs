namespace Avalonia.Input;

/// <summary>
/// Represents the kind of a <see cref="T:Avalonia.Input.DataFormat" />.
/// </summary>
public enum DataFormatKind
{
	/// <summary>
	/// <para>
	/// The data format is specific to the application.
	/// The exact format name used internally by Avalonia will vary depending on the platform.
	/// </para>
	/// <para>
	/// Such a format is created using <see cref="M:Avalonia.Input.DataFormat.CreateBytesApplicationFormat(System.String)" />
	/// or <see cref="M:Avalonia.Input.DataFormat.CreateStringApplicationFormat(System.String)" />.
	/// </para>
	/// </summary>
	/// <seealso cref="M:Avalonia.Input.DataFormat.CreateBytesApplicationFormat(System.String)" />
	/// <seealso cref="M:Avalonia.Input.DataFormat.CreateStringApplicationFormat(System.String)" />
	Application,
	/// <summary>
	/// <para>
	/// The data format is specific to the current platform.
	/// Any other application using the same identifier will be able to access it.
	/// </para>
	/// <para>
	/// Such a format is created using <see cref="M:Avalonia.Input.DataFormat.CreateBytesPlatformFormat(System.String)" />
	/// or <see cref="M:Avalonia.Input.DataFormat.CreateStringPlatformFormat(System.String)" />.
	/// </para>
	/// </summary>
	/// <seealso cref="M:Avalonia.Input.DataFormat.CreateBytesPlatformFormat(System.String)" />
	/// <seealso cref="M:Avalonia.Input.DataFormat.CreateStringPlatformFormat(System.String)" />
	Platform,
	/// <summary>
	/// <para>
	/// The data format is cross-platform and supported directly by Avalonia.
	/// Such formats include <see cref="P:Avalonia.Input.DataFormat.Text" /> and <see cref="P:Avalonia.Input.DataFormat.File" />.
	/// </para>
	/// <para>
	/// It is not possible to create such a format directly.
	/// </para>
	/// </summary>
	Universal,
	/// <summary>
	/// <para>
	/// The data format is only usable within the current process.
	/// It never crosses process or serialization boundaries.
	/// </para>
	/// <para>
	/// Such a format is created using <see cref="M:Avalonia.Input.DataFormat.CreateInProcessFormat``1(System.String)" />.
	/// </para>
	/// </summary>
	/// <seealso cref="M:Avalonia.Input.DataFormat.CreateInProcessFormat``1(System.String)" />
	InProcess
}
