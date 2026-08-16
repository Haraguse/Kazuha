namespace Avalonia.Markup.Xaml;

public interface IRootObjectProvider
{
	/// <summary>
	/// The root object of the xaml file
	/// </summary>
	object RootObject { get; }

	/// <summary>
	/// The "current" root object, contains either the root of the xaml file
	/// or the root object of the control/data template 
	/// </summary>
	object IntermediateRootObject { get; }
}
