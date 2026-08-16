using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.Plugins;

/// <summary>
/// Validates properties on objects that implement <see cref="T:System.ComponentModel.INotifyDataErrorInfo" />.
/// </summary>
internal class IndeiValidationPlugin : IDataValidationPlugin
{
	private class Validator : DataValidationBase, IWeakEventSubscriber<DataErrorsChangedEventArgs>
	{
		private readonly WeakReference<object?> _reference;

		private readonly string _name;

		public Validator(WeakReference<object?> reference, string name, IPropertyAccessor inner)
			: base(inner)
		{
			_reference = reference;
			_name = name;
		}

		void IWeakEventSubscriber<DataErrorsChangedEventArgs>.OnEvent(object? notifyDataErrorInfo, WeakEvent ev, DataErrorsChangedEventArgs e)
		{
			if (e.PropertyName == _name || string.IsNullOrEmpty(e.PropertyName))
			{
				PublishValue(CreateBindingNotification(Value));
			}
		}

		protected override void SubscribeCore()
		{
			if (GetReferenceTarget() is INotifyDataErrorInfo target)
			{
				ErrorsChangedWeakEvent.Subscribe(target, this);
			}
			base.SubscribeCore();
		}

		protected override void UnsubscribeCore()
		{
			if (GetReferenceTarget() is INotifyDataErrorInfo target)
			{
				ErrorsChangedWeakEvent.Unsubscribe(target, this);
			}
			base.UnsubscribeCore();
		}

		protected override void InnerValueChanged(object? value)
		{
			PublishValue(CreateBindingNotification(value));
		}

		private BindingNotification CreateBindingNotification(object? value)
		{
			if (GetReferenceTarget() is INotifyDataErrorInfo notifyDataErrorInfo)
			{
				List<object> list = notifyDataErrorInfo.GetErrors(_name)?.Cast<object>().Where((object x) => x != null).ToList();
				if (list != null && list.Count > 0)
				{
					return new BindingNotification(GenerateException(list), BindingErrorType.DataValidationError, value);
				}
			}
			return new BindingNotification(value);
		}

		private object? GetReferenceTarget()
		{
			_reference.TryGetTarget(out object target);
			return target;
		}

		private static Exception GenerateException(IList<object> errors)
		{
			if (errors.Count == 1)
			{
				return new DataValidationException(errors[0]);
			}
			return new AggregateException(errors.Select((object x) => new DataValidationException(x)));
		}
	}

	private static readonly WeakEvent<INotifyDataErrorInfo, DataErrorsChangedEventArgs> ErrorsChangedWeakEvent = WeakEvent.Register(delegate(INotifyDataErrorInfo s, EventHandler<DataErrorsChangedEventArgs> h)
	{
		s.ErrorsChanged += h;
	}, delegate(INotifyDataErrorInfo s, EventHandler<DataErrorsChangedEventArgs> h)
	{
		s.ErrorsChanged -= h;
	});

	/// <inheritdoc />
	public bool Match(WeakReference<object?> reference, string memberName)
	{
		reference.TryGetTarget(out object target);
		return target is INotifyDataErrorInfo;
	}

	/// <inheritdoc />
	public IPropertyAccessor Start(WeakReference<object?> reference, string name, IPropertyAccessor accessor)
	{
		return new Validator(reference, name, accessor);
	}
}
