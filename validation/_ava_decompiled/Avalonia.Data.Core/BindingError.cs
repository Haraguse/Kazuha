using System;

namespace Avalonia.Data.Core;

internal class BindingError
{
	public Exception Exception { get; }

	public BindingErrorType ErrorType { get; }

	public BindingError(Exception exception, BindingErrorType errorType)
	{
		Exception = exception;
		ErrorType = errorType;
	}
}
