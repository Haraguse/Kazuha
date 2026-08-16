using System;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using Avalonia.Reactive;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;

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
