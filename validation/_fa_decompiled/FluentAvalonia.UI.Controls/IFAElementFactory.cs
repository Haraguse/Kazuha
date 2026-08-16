using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Supports the creation and recycling of <see cref="T:Avalonia.Controls.Control" /> objects
/// </summary>
public interface IFAElementFactory : IDataTemplate, ITemplate<object?, Control?>
{
	/// <summary>
	/// Gets a <see cref="T:Avalonia.Controls.Control" /> object
	/// </summary>
	Control GetElement(FAElementFactoryGetArgs args);

	/// <summary>
	/// Recycles a <see cref="T:Avalonia.Controls.Control" /> that was previously retreived using <see cref="M:FluentAvalonia.UI.Controls.IFAElementFactory.GetElement(FluentAvalonia.UI.Controls.FAElementFactoryGetArgs)" />
	/// </summary>
	void RecycleElement(FAElementFactoryRecycleArgs args);
}
