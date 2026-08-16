using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Avalonia.Data.Core.Plugins;

/// <summary>
/// Validates properties on that have <see cref="T:System.ComponentModel.DataAnnotations.ValidationAttribute" />s.
/// </summary>
[RequiresUnreferencedCode("DataValidationPlugin might require unreferenced code.")]
public class DataAnnotationsValidationPlugin : IDataValidationPlugin
{
	[RequiresUnreferencedCode("DataValidationPlugin might require unreferenced code.")]
	private sealed class Accessor : DataValidationBase
	{
		private readonly ValidationContext? _context;

		public Accessor(WeakReference<object?> reference, string name, IPropertyAccessor inner)
			: base(inner)
		{
			if (reference.TryGetTarget(out object target))
			{
				_context = new ValidationContext(target);
				_context.MemberName = name;
			}
		}

		protected override void InnerValueChanged(object? value)
		{
			if (_context != null)
			{
				List<ValidationResult> list = new List<ValidationResult>();
				if (Validator.TryValidateProperty(value, _context, list))
				{
					base.InnerValueChanged(value);
				}
				else
				{
					base.InnerValueChanged(new BindingNotification(CreateException(list), BindingErrorType.DataValidationError, value));
				}
			}
		}

		private static Exception CreateException(IList<ValidationResult> errors)
		{
			if (errors.Count == 1)
			{
				return new DataValidationException(errors[0].ErrorMessage);
			}
			return new AggregateException(errors.Select((ValidationResult x) => new DataValidationException(x.ErrorMessage)));
		}
	}

	/// <inheritdoc />
	public bool Match(WeakReference<object?> reference, string memberName)
	{
		reference.TryGetTarget(out object target);
		return target?.GetType().GetRuntimeProperty(memberName)?.GetCustomAttributes<ValidationAttribute>().Any() == true;
	}

	/// <inheritdoc />
	public IPropertyAccessor Start(WeakReference<object?> reference, string name, IPropertyAccessor inner)
	{
		return new Accessor(reference, name, inner);
	}
}
