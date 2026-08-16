using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

/// <summary>
/// Provides form factor-specific value for T for the current target device.
/// This extension defines "form-factor" as a "device type" rather than "screen type".
/// </summary>
public abstract class OnFormFactorExtensionBase<TReturn, TOn> : IAddChild<TOn> where TOn : On<TReturn>
{
	/// <summary>
	/// Gets or sets the value applied by default.
	/// If not set, default(TReturn) is assigned to the value.
	/// </summary>
	[MarkupExtensionDefaultOption]
	public TReturn? Default { get; set; }

	/// <summary>
	/// Gets or sets the value applied on desktop systems.
	/// </summary>
	[MarkupExtensionOption(FormFactorType.Desktop)]
	public TReturn? Desktop { get; set; }

	/// <summary>
	/// Gets or sets the value applied on mobile systems.
	/// </summary>
	[MarkupExtensionOption(FormFactorType.Mobile)]
	public TReturn? Mobile { get; set; }

	/// <summary>
	/// Gets or sets the value applied on TV systems.
	/// </summary>
	[MarkupExtensionOption(FormFactorType.TV)]
	public TReturn? TV { get; set; }

	public object ProvideValue()
	{
		return this;
	}

	void IAddChild<TOn>.AddChild(TOn child)
	{
	}
}
