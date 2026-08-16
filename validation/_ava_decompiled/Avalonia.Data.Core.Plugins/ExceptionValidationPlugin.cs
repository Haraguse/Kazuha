using System;
using System.Reflection;

namespace Avalonia.Data.Core.Plugins;

/// <summary>
/// Validates properties that report errors by throwing exceptions.
/// </summary>
internal class ExceptionValidationPlugin : IDataValidationPlugin
{
	private sealed class Validator : DataValidationBase
	{
		public Validator(WeakReference<object?> reference, string name, IPropertyAccessor inner)
			: base(inner)
		{
		}

		public override bool SetValue(object? value, BindingPriority priority)
		{
			try
			{
				return base.SetValue(value, priority);
			}
			catch (TargetInvocationException ex) when (ex.InnerException != null)
			{
				PublishValue(new BindingNotification(ex.InnerException, BindingErrorType.DataValidationError));
			}
			catch (Exception error)
			{
				PublishValue(new BindingNotification(error, BindingErrorType.DataValidationError));
			}
			return false;
		}
	}

	/// <inheritdoc />
	public bool Match(WeakReference<object?> reference, string memberName)
	{
		return true;
	}

	/// <inheritdoc />
	public IPropertyAccessor Start(WeakReference<object?> reference, string name, IPropertyAccessor inner)
	{
		return new Validator(reference, name, inner);
	}
}
