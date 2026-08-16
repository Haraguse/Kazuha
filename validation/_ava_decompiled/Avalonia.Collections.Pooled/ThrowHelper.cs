using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;

namespace Avalonia.Collections.Pooled;

internal static class ThrowHelper
{
	[DoesNotReturn]
	internal static void ThrowArrayTypeMismatchException()
	{
		throw new ArrayTypeMismatchException();
	}

	[DoesNotReturn]
	internal static void ThrowIndexOutOfRangeException()
	{
		throw new IndexOutOfRangeException();
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRangeException()
	{
		throw new ArgumentOutOfRangeException();
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException_DestinationTooShort()
	{
		throw new ArgumentException("Destination too short.");
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException_OverlapAlignmentMismatch()
	{
		throw new ArgumentException("Overlap alignment mismatch.");
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRange_IndexException()
	{
		throw GetArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
	}

	[DoesNotReturn]
	internal static void ThrowIndexArgumentOutOfRange_NeedNonNegNumException()
	{
		throw GetArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
	}

	[DoesNotReturn]
	internal static void ThrowValueArgumentOutOfRange_NeedNonNegNumException()
	{
		throw GetArgumentOutOfRangeException(ExceptionArgument.value, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
	}

	[DoesNotReturn]
	internal static void ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum()
	{
		throw GetArgumentOutOfRangeException(ExceptionArgument.length, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
	}

	[DoesNotReturn]
	internal static void ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index()
	{
		throw GetArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
	}

	[DoesNotReturn]
	internal static void ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count()
	{
		throw GetArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
	}

	[DoesNotReturn]
	internal static void ThrowWrongKeyTypeArgumentException<T>(T key, Type targetType)
	{
		throw GetWrongKeyTypeArgumentException(key, targetType);
	}

	[DoesNotReturn]
	internal static void ThrowWrongValueTypeArgumentException<T>(T value, Type targetType)
	{
		throw GetWrongValueTypeArgumentException(value, targetType);
	}

	private static ArgumentException GetAddingDuplicateWithKeyArgumentException(object? key)
	{
		return new ArgumentException($"Error adding duplicate with key: {key}.");
	}

	[DoesNotReturn]
	internal static void ThrowAddingDuplicateWithKeyArgumentException<T>(T key)
	{
		throw GetAddingDuplicateWithKeyArgumentException(key);
	}

	[DoesNotReturn]
	internal static void ThrowKeyNotFoundException<T>(T key)
	{
		throw GetKeyNotFoundException(key);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException(ExceptionResource resource)
	{
		throw GetArgumentException(resource);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException(ExceptionResource resource, ExceptionArgument argument)
	{
		throw GetArgumentException(resource, argument);
	}

	private static ArgumentNullException GetArgumentNullException(ExceptionArgument argument)
	{
		return new ArgumentNullException(GetArgumentName(argument));
	}

	[DoesNotReturn]
	internal static void ThrowArgumentNullException(ExceptionArgument argument)
	{
		throw GetArgumentNullException(argument);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentNullException(ExceptionResource resource)
	{
		throw new ArgumentNullException(GetResourceString(resource));
	}

	[DoesNotReturn]
	internal static void ThrowArgumentNullException(ExceptionArgument argument, ExceptionResource resource)
	{
		throw new ArgumentNullException(GetArgumentName(argument), GetResourceString(resource));
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument)
	{
		throw new ArgumentOutOfRangeException(GetArgumentName(argument));
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument, ExceptionResource resource)
	{
		throw GetArgumentOutOfRangeException(argument, resource);
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument, int paramNumber, ExceptionResource resource)
	{
		throw GetArgumentOutOfRangeException(argument, paramNumber, resource);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException(ExceptionResource resource)
	{
		throw GetInvalidOperationException(resource);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException(ExceptionResource resource, Exception e)
	{
		throw new InvalidOperationException(GetResourceString(resource), e);
	}

	[DoesNotReturn]
	internal static void ThrowSerializationException(ExceptionResource resource)
	{
		throw new SerializationException(GetResourceString(resource));
	}

	[DoesNotReturn]
	internal static void ThrowSecurityException(ExceptionResource resource)
	{
		throw new SecurityException(GetResourceString(resource));
	}

	[DoesNotReturn]
	internal static void ThrowRankException(ExceptionResource resource)
	{
		throw new RankException(GetResourceString(resource));
	}

	[DoesNotReturn]
	internal static void ThrowNotSupportedException(ExceptionResource resource)
	{
		throw new NotSupportedException(GetResourceString(resource));
	}

	[DoesNotReturn]
	internal static void ThrowUnauthorizedAccessException(ExceptionResource resource)
	{
		throw new UnauthorizedAccessException(GetResourceString(resource));
	}

	[DoesNotReturn]
	internal static void ThrowObjectDisposedException(string objectName, ExceptionResource resource)
	{
		throw new ObjectDisposedException(objectName, GetResourceString(resource));
	}

	[DoesNotReturn]
	internal static void ThrowObjectDisposedException(ExceptionResource resource)
	{
		throw new ObjectDisposedException(null, GetResourceString(resource));
	}

	[DoesNotReturn]
	internal static void ThrowNotSupportedException()
	{
		throw new NotSupportedException();
	}

	[DoesNotReturn]
	internal static void ThrowAggregateException(List<Exception> exceptions)
	{
		throw new AggregateException(exceptions);
	}

	[DoesNotReturn]
	internal static void ThrowOutOfMemoryException()
	{
		throw new OutOfMemoryException();
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException_Argument_InvalidArrayType()
	{
		throw new ArgumentException("Invalid array type.");
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_InvalidOperation_EnumNotStarted()
	{
		throw new InvalidOperationException("Enumeration has not started.");
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_InvalidOperation_EnumEnded()
	{
		throw new InvalidOperationException("Enumeration has ended.");
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_EnumCurrent(int index)
	{
		throw GetInvalidOperationException_EnumCurrent(index);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion()
	{
		throw new InvalidOperationException("Collection was modified during enumeration.");
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen()
	{
		throw new InvalidOperationException("Invalid enumerator state: enumeration cannot proceed.");
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_InvalidOperation_NoValue()
	{
		throw new InvalidOperationException("No value provided.");
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_ConcurrentOperationsNotSupported()
	{
		throw new InvalidOperationException("Concurrent operations are not supported.");
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperationException_HandleIsNotInitialized()
	{
		throw new InvalidOperationException("Handle is not initialized.");
	}

	[DoesNotReturn]
	internal static void ThrowFormatException_BadFormatSpecifier()
	{
		throw new FormatException("Bad format specifier.");
	}

	private static ArgumentException GetArgumentException(ExceptionResource resource)
	{
		return new ArgumentException(GetResourceString(resource));
	}

	private static InvalidOperationException GetInvalidOperationException(ExceptionResource resource)
	{
		return new InvalidOperationException(GetResourceString(resource));
	}

	private static ArgumentException GetWrongKeyTypeArgumentException(object? key, Type targetType)
	{
		return new ArgumentException($"Wrong key type. Expected {targetType}, got: '{key}'.", "key");
	}

	private static ArgumentException GetWrongValueTypeArgumentException(object? value, Type targetType)
	{
		return new ArgumentException($"Wrong value type. Expected {targetType}, got: '{value}'.", "value");
	}

	private static KeyNotFoundException GetKeyNotFoundException(object? key)
	{
		return new KeyNotFoundException($"Key not found: {key}");
	}

	private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument, ExceptionResource resource)
	{
		return new ArgumentOutOfRangeException(GetArgumentName(argument), GetResourceString(resource));
	}

	private static ArgumentException GetArgumentException(ExceptionResource resource, ExceptionArgument argument)
	{
		return new ArgumentException(GetResourceString(resource), GetArgumentName(argument));
	}

	private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument, int paramNumber, ExceptionResource resource)
	{
		return new ArgumentOutOfRangeException(GetArgumentName(argument) + "[" + paramNumber + "]", GetResourceString(resource));
	}

	private static InvalidOperationException GetInvalidOperationException_EnumCurrent(int index)
	{
		return new InvalidOperationException((index < 0) ? "Enumeration has not started" : "Enumeration has ended");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void IfNullAndNullsAreIllegalThenThrow<T>(object? value, ExceptionArgument argName)
	{
		if (default(T) != null && value == null)
		{
			ThrowArgumentNullException(argName);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ThrowForUnsupportedVectorBaseType<T>() where T : struct
	{
		if (typeof(T) != typeof(byte) && typeof(T) != typeof(sbyte) && typeof(T) != typeof(short) && typeof(T) != typeof(ushort) && typeof(T) != typeof(int) && typeof(T) != typeof(uint) && typeof(T) != typeof(long) && typeof(T) != typeof(ulong) && typeof(T) != typeof(float) && typeof(T) != typeof(double))
		{
			ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
		}
	}

	private static string GetArgumentName(ExceptionArgument argument)
	{
		return argument switch
		{
			ExceptionArgument.obj => "obj", 
			ExceptionArgument.dictionary => "dictionary", 
			ExceptionArgument.array => "array", 
			ExceptionArgument.info => "info", 
			ExceptionArgument.key => "key", 
			ExceptionArgument.text => "text", 
			ExceptionArgument.values => "values", 
			ExceptionArgument.value => "value", 
			ExceptionArgument.startIndex => "startIndex", 
			ExceptionArgument.task => "task", 
			ExceptionArgument.ch => "ch", 
			ExceptionArgument.s => "s", 
			ExceptionArgument.input => "input", 
			ExceptionArgument.list => "list", 
			ExceptionArgument.index => "index", 
			ExceptionArgument.capacity => "capacity", 
			ExceptionArgument.collection => "collection", 
			ExceptionArgument.item => "item", 
			ExceptionArgument.converter => "converter", 
			ExceptionArgument.match => "match", 
			ExceptionArgument.count => "count", 
			ExceptionArgument.action => "action", 
			ExceptionArgument.comparison => "comparison", 
			ExceptionArgument.exceptions => "exceptions", 
			ExceptionArgument.exception => "exception", 
			ExceptionArgument.enumerable => "enumerable", 
			ExceptionArgument.start => "start", 
			ExceptionArgument.format => "format", 
			ExceptionArgument.culture => "culture", 
			ExceptionArgument.comparer => "comparer", 
			ExceptionArgument.comparable => "comparable", 
			ExceptionArgument.source => "source", 
			ExceptionArgument.state => "state", 
			ExceptionArgument.length => "length", 
			ExceptionArgument.comparisonType => "comparisonType", 
			ExceptionArgument.manager => "manager", 
			ExceptionArgument.sourceBytesToCopy => "sourceBytesToCopy", 
			ExceptionArgument.callBack => "callBack", 
			ExceptionArgument.creationOptions => "creationOptions", 
			ExceptionArgument.function => "function", 
			ExceptionArgument.delay => "delay", 
			ExceptionArgument.millisecondsDelay => "millisecondsDelay", 
			ExceptionArgument.millisecondsTimeout => "millisecondsTimeout", 
			ExceptionArgument.timeout => "timeout", 
			ExceptionArgument.type => "type", 
			ExceptionArgument.sourceIndex => "sourceIndex", 
			ExceptionArgument.sourceArray => "sourceArray", 
			ExceptionArgument.destinationIndex => "destinationIndex", 
			ExceptionArgument.destinationArray => "destinationArray", 
			ExceptionArgument.other => "other", 
			ExceptionArgument.newSize => "newSize", 
			ExceptionArgument.lowerBounds => "lowerBounds", 
			ExceptionArgument.lengths => "lengths", 
			ExceptionArgument.len => "len", 
			ExceptionArgument.keys => "keys", 
			ExceptionArgument.indices => "indices", 
			ExceptionArgument.endIndex => "endIndex", 
			ExceptionArgument.elementType => "elementType", 
			ExceptionArgument.arrayIndex => "arrayIndex", 
			_ => argument.ToString(), 
		};
	}

	private static string GetResourceString(ExceptionResource resource)
	{
		return resource switch
		{
			ExceptionResource.ArgumentOutOfRange_Index => "Argument 'index' was out of the range of valid values.", 
			ExceptionResource.ArgumentOutOfRange_Count => "Argument 'count' was out of the range of valid values.", 
			ExceptionResource.Arg_ArrayPlusOffTooSmall => "Array plus offset too small.", 
			ExceptionResource.NotSupported_ReadOnlyCollection => "This operation is not supported on a read-only collection.", 
			ExceptionResource.Arg_RankMultiDimNotSupported => "Multi-dimensional arrays are not supported.", 
			ExceptionResource.Arg_NonZeroLowerBound => "Arrays with a non-zero lower bound are not supported.", 
			ExceptionResource.ArgumentOutOfRange_ListInsert => "Insertion index was out of the range of valid values.", 
			ExceptionResource.ArgumentOutOfRange_NeedNonNegNum => "The number must be non-negative.", 
			ExceptionResource.ArgumentOutOfRange_SmallCapacity => "The capacity cannot be set below the current Count.", 
			ExceptionResource.Argument_InvalidOffLen => "Invalid offset length.", 
			ExceptionResource.ArgumentOutOfRange_BiggerThanCollection => "The given value was larger than the size of the collection.", 
			ExceptionResource.Serialization_MissingKeys => "Serialization error: missing keys.", 
			ExceptionResource.Serialization_NullKey => "Serialization error: null key.", 
			ExceptionResource.NotSupported_KeyCollectionSet => "The KeyCollection does not support modification.", 
			ExceptionResource.NotSupported_ValueCollectionSet => "The ValueCollection does not support modification.", 
			ExceptionResource.InvalidOperation_NullArray => "Null arrays are not supported.", 
			ExceptionResource.InvalidOperation_HSCapacityOverflow => "Set hash capacity overflow. Cannot increase size.", 
			ExceptionResource.NotSupported_StringComparison => "String comparison not supported.", 
			ExceptionResource.ConcurrentCollection_SyncRoot_NotSupported => "SyncRoot not supported.", 
			ExceptionResource.ArgumentException_OtherNotArrayOfCorrectLength => "The other array is not of the correct length.", 
			ExceptionResource.ArgumentOutOfRange_EndIndexStartIndex => "The end index does not come after the start index.", 
			ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported => "Huge arrays are not supported.", 
			ExceptionResource.Argument_AddingDuplicate => "Duplicate item added.", 
			ExceptionResource.Argument_InvalidArgumentForComparison => "Invalid argument for comparison.", 
			ExceptionResource.Arg_LowerBoundsMustMatch => "Array lower bounds must match.", 
			ExceptionResource.Arg_MustBeType => "Argument must be of type: ", 
			ExceptionResource.InvalidOperation_IComparerFailed => "IComparer failed.", 
			ExceptionResource.NotSupported_FixedSizeCollection => "This operation is not suppored on a fixed-size collection.", 
			ExceptionResource.Rank_MultiDimNotSupported => "Multi-dimensional arrays are not supported.", 
			ExceptionResource.Arg_TypeNotSupported => "Type not supported.", 
			_ => resource.ToString(), 
		};
	}
}
