using System;
using Avalonia.PropertyStore;
using Avalonia.Styling;

namespace Avalonia.Data;

public abstract class BindingExpressionBase : IDisposable, ISetterInstance
{
	private protected BindingExpressionBase()
	{
	}

	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Sends the current binding target value to the binding source property in 
	/// <see cref="F:Avalonia.Data.BindingMode.TwoWay" /> or <see cref="F:Avalonia.Data.BindingMode.OneWayToSource" /> bindings.
	/// </summary>
	/// <remarks>
	/// This method does nothing when the Mode of the binding is not 
	/// <see cref="F:Avalonia.Data.BindingMode.TwoWay" /> or <see cref="F:Avalonia.Data.BindingMode.OneWayToSource" />.
	///
	/// If the UpdateSourceTrigger value of your binding is set to
	/// <see cref="F:Avalonia.Data.UpdateSourceTrigger.Explicit" />, you must call the 
	/// <see cref="M:Avalonia.Data.BindingExpressionBase.UpdateSource" /> method or the changes will not propagate back to the
	/// source.
	/// </remarks>
	public virtual void UpdateSource()
	{
	}

	/// <summary>
	/// Forces a data transfer from the binding source to the binding target.
	/// </summary>
	public virtual void UpdateTarget()
	{
	}

	/// <summary>
	/// When overridden in a derived class, attaches the binding expression to a value store but
	/// does not start it.
	/// </summary>
	/// <param name="valueStore">The value store to attach to.</param>
	/// <param name="frame">The immediate value frame to attach to, if any.</param>
	/// <param name="target">The target object.</param>
	/// <param name="targetProperty">The target property.</param>
	/// <param name="priority">The priority of the binding.</param>
	internal abstract void Attach(ValueStore valueStore, ImmediateValueFrame? frame, AvaloniaObject target, AvaloniaProperty targetProperty, BindingPriority priority);
}
