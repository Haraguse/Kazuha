using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Avalonia.Data.Core.Plugins;

/// <summary>
/// Holds a registry of plugins used for bindings.
/// </summary>
[RequiresUnreferencedCode("PropertyAccessors might require unreferenced code.")]
internal static class BindingPlugins
{
	internal static readonly List<IPropertyAccessorPlugin> s_propertyAccessors;

	internal static readonly List<IDataValidationPlugin> s_dataValidators;

	internal static readonly List<IStreamPlugin> s_streamHandlers;

	/// <summary>
	/// An ordered collection of property accessor plugins that can be used to customize
	/// the reading and subscription of property values on a type.
	/// </summary>
	public static IList<IPropertyAccessorPlugin> PropertyAccessors => s_propertyAccessors;

	/// <summary>
	/// An ordered collection of validation checker plugins that can be used to customize
	/// the validation of view model and model data.
	/// </summary>
	public static IList<IDataValidationPlugin> DataValidators => s_dataValidators;

	/// <summary>
	/// An ordered collection of stream plugins that can be used to customize the behavior
	/// of the '^' stream binding operator.
	/// </summary>
	public static IList<IStreamPlugin> StreamHandlers => s_streamHandlers;

	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "We're checking if dynamic code is supported.")]
	static BindingPlugins()
	{
		s_propertyAccessors = new List<IPropertyAccessorPlugin>
		{
			new AvaloniaPropertyAccessorPlugin(),
			new InpcPropertyAccessorPlugin()
		};
		s_dataValidators = new List<IDataValidationPlugin>
		{
			new IndeiValidationPlugin(),
			new ExceptionValidationPlugin()
		};
		s_streamHandlers = new List<IStreamPlugin>
		{
			new TaskStreamPlugin(),
			new ObservableStreamPlugin()
		};
		if (RuntimeFeature.IsDynamicCodeSupported)
		{
			s_propertyAccessors.Insert(1, new ReflectionMethodAccessorPlugin());
		}
	}
}
