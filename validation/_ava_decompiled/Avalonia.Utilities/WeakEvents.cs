using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using Avalonia.Threading;

namespace Avalonia.Utilities;

public class WeakEvents
{
	/// <summary>
	/// Represents CollectionChanged event from <see cref="T:System.Collections.Specialized.INotifyCollectionChanged" />
	/// </summary>
	public static readonly WeakEvent<INotifyCollectionChanged, NotifyCollectionChangedEventArgs> CollectionChanged = WeakEvent.Register(delegate(INotifyCollectionChanged c, EventHandler<NotifyCollectionChangedEventArgs> s)
	{
		NotifyCollectionChangedEventHandler handler = delegate(object? _, NotifyCollectionChangedEventArgs e)
		{
			s(c, e);
		};
		c.CollectionChanged += handler;
		return delegate
		{
			c.CollectionChanged -= handler;
		};
	});

	/// <summary>
	/// Represents PropertyChanged event from <see cref="T:System.ComponentModel.INotifyPropertyChanged" /> with auto-dispatching to the UI thread
	/// </summary>
	public static readonly WeakEvent<INotifyPropertyChanged, PropertyChangedEventArgs> ThreadSafePropertyChanged = WeakEvent.Register(delegate(INotifyPropertyChanged s, EventHandler<PropertyChangedEventArgs> h)
	{
		bool unsubscribed = false;
		PropertyChangedEventHandler handler = delegate(object? _, PropertyChangedEventArgs e)
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				h(s, e);
			}
			else
			{
				Dispatcher.UIThread.Post(delegate
				{
					if (!unsubscribed)
					{
						h(s, e);
					}
				});
			}
		};
		s.PropertyChanged += handler;
		return delegate
		{
			unsubscribed = true;
			s.PropertyChanged -= handler;
		};
	});

	/// <summary>
	/// Represents PropertyChanged event from <see cref="T:Avalonia.AvaloniaObject" />
	/// </summary>
	public static readonly WeakEvent<AvaloniaObject, AvaloniaPropertyChangedEventArgs> AvaloniaPropertyChanged = WeakEvent.Register(delegate(AvaloniaObject s, EventHandler<AvaloniaPropertyChangedEventArgs> h)
	{
		EventHandler<AvaloniaPropertyChangedEventArgs> handler = delegate(object? _, AvaloniaPropertyChangedEventArgs e)
		{
			h(s, e);
		};
		s.PropertyChanged += handler;
		return delegate
		{
			s.PropertyChanged -= handler;
		};
	});

	/// <summary>
	/// Represents CanExecuteChanged event from <see cref="T:System.Windows.Input.ICommand" />
	/// </summary>
	public static readonly WeakEvent<ICommand, EventArgs> CommandCanExecuteChanged = WeakEvent.Register(delegate(ICommand s, EventHandler h)
	{
		s.CanExecuteChanged += h;
	}, delegate(ICommand s, EventHandler h)
	{
		s.CanExecuteChanged -= h;
	});
}
