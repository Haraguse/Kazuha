using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Reactive;

namespace Avalonia.Data.Core.Plugins;

/// <summary>
/// Handles binding to <see cref="T:System.Threading.Tasks.Task" />s for the '^' stream binding operator.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "This method is not supported by NativeAOT.")]
[RequiresUnreferencedCode("StreamPlugin might require unreferenced code.")]
internal class TaskStreamPlugin : IStreamPlugin
{
	/// <summary>
	/// Checks whether this plugin handles the specified value.
	/// </summary>
	/// <param name="reference">A weak reference to the value.</param>
	/// <returns>True if the plugin can handle the value; otherwise false.</returns>
	public virtual bool Match(WeakReference<object?> reference)
	{
		reference.TryGetTarget(out object target);
		return target is Task;
	}

	/// <summary>
	/// Starts producing output based on the specified value.
	/// </summary>
	/// <param name="reference">A weak reference to the object.</param>
	/// <returns>
	/// An observable that produces the output for the value.
	/// </returns>
	public virtual IObservable<object?> Start(WeakReference<object?> reference)
	{
		reference.TryGetTarget(out object target);
		Task task = target as Task;
		if (task != null && task.GetType().GetRuntimeProperty("Result") != null)
		{
			TaskStatus status = task.Status;
			if (status == TaskStatus.RanToCompletion || status == TaskStatus.Faulted)
			{
				return HandleCompleted(task);
			}
			LightweightSubject<object?> subject = new LightweightSubject<object>();
			task.ContinueWith((Task x) => HandleCompleted(task).Subscribe(subject), TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(continueOnCapturedContext: false);
			return subject;
		}
		return Observable.Empty<object>();
	}

	private static IObservable<object?> HandleCompleted(Task task)
	{
		PropertyInfo propertyInfo = GetTaskResult(task);
		if (propertyInfo != null)
		{
			return task.Status switch
			{
				TaskStatus.RanToCompletion => Observable.Return(propertyInfo.GetValue(task)), 
				TaskStatus.Faulted => Observable.Return(new BindingNotification(task.Exception, BindingErrorType.Error)), 
				_ => throw new AvaloniaInternalException("HandleCompleted called for non-completed Task."), 
			};
		}
		return Observable.Empty<object>();
		[DynamicDependency("Result", typeof(Task<>))]
		[UnconditionalSuppressMessage("Trimming", "IL2070")]
		[UnconditionalSuppressMessage("Trimming", "IL2075")]
		static PropertyInfo? GetTaskResult(Task obj)
		{
			return obj.GetType().GetProperty("Result");
		}
	}
}
internal class TaskStreamPlugin<T> : IStreamPlugin
{
	public bool Match(WeakReference<object?> reference)
	{
		if (reference.TryGetTarget(out object target))
		{
			return target is Task<T>;
		}
		return false;
	}

	public IObservable<object?> Start(WeakReference<object?> reference)
	{
		if (reference.TryGetTarget(out object target))
		{
			Task<T> task = target as Task<T>;
			if (task != null)
			{
				TaskStatus status = task.Status;
				if (status == TaskStatus.RanToCompletion || status == TaskStatus.Faulted)
				{
					return HandleCompleted(task);
				}
				LightweightSubject<object?> subject = new LightweightSubject<object>();
				task.ContinueWith((Task<T> _) => HandleCompleted(task).Subscribe(subject), TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(continueOnCapturedContext: false);
				return subject;
			}
		}
		return Observable.Empty<object>();
	}

	private static IObservable<object?> HandleCompleted(Task<T> task)
	{
		return task.Status switch
		{
			TaskStatus.RanToCompletion => Observable.Return((object)task.Result), 
			TaskStatus.Faulted => Observable.Return(new BindingNotification(task.Exception, BindingErrorType.Error)), 
			_ => throw new AvaloniaInternalException("HandleCompleted called for non-completed Task."), 
		};
	}
}
