using System;
using System.ComponentModel;
using Avalonia.Metadata;

namespace Avalonia.Threading;

/// <summary>
/// Defines the priorities with which jobs can be invoked on a <see cref="T:Avalonia.Threading.Dispatcher" />.
/// </summary>
public readonly struct DispatcherPriority : IEquatable<DispatcherPriority>, IComparable<DispatcherPriority>
{
	/// <summary>
	/// The lowest foreground dispatcher priority
	/// </summary>
	public static readonly DispatcherPriority Default = new DispatcherPriority(0);

	internal static readonly DispatcherPriority MinimumForegroundPriority = Default;

	/// <summary>
	/// The job will be processed with the same priority as input.
	/// </summary>
	public static readonly DispatcherPriority Input = new DispatcherPriority((int)Default - 1);

	/// <summary>
	/// The job will be processed after other non-idle operations have completed.
	/// </summary>
	public static readonly DispatcherPriority Background = new DispatcherPriority((int)Input - 1);

	/// <summary>
	/// The job will be processed after background operations have completed.
	/// </summary>
	public static readonly DispatcherPriority ContextIdle = new DispatcherPriority((int)Background - 1);

	/// <summary>
	/// The job will be processed when the application is idle.
	/// </summary>
	public static readonly DispatcherPriority ApplicationIdle = new DispatcherPriority((int)ContextIdle - 1);

	/// <summary>
	/// The job will be processed when the system is idle.
	/// </summary>
	public static readonly DispatcherPriority SystemIdle = new DispatcherPriority((int)ApplicationIdle - 1);

	/// <summary>
	/// Minimum possible priority that's actually dispatched, default value
	/// </summary>
	internal static readonly DispatcherPriority MinimumActiveValue = new DispatcherPriority(SystemIdle);

	/// <summary>
	/// A dispatcher priority for jobs that shouldn't be executed yet
	/// </summary>
	public static readonly DispatcherPriority Inactive = new DispatcherPriority((int)MinimumActiveValue - 1);

	/// <summary>
	/// Minimum valid priority
	/// </summary>
	internal static readonly DispatcherPriority MinValue = new DispatcherPriority(Inactive);

	/// <summary>
	/// Used internally in dispatcher code
	/// </summary>
	public static readonly DispatcherPriority Invalid = new DispatcherPriority((int)MinimumActiveValue - 2);

	/// <summary>
	/// The job will be processed after layout and render but before input.
	/// </summary>
	public static readonly DispatcherPriority Loaded = new DispatcherPriority((int)Default + 1);

	/// <summary>
	/// A special priority for platforms with UI render timer or for forced full rasterization requests
	/// </summary>
	[PrivateApi]
	public static readonly DispatcherPriority UiThreadRender = new DispatcherPriority((int)Loaded + 1);

	/// <summary>
	/// A special priority to synchronize native control host positions, IME, etc
	/// We should probably have a better API for that, so the priority is internal
	/// </summary>
	internal static readonly DispatcherPriority AfterRender = new DispatcherPriority((int)UiThreadRender + 1);

	/// <summary>
	/// The job will be processed with the same priority as render.
	/// </summary>
	public static readonly DispatcherPriority Render = new DispatcherPriority((int)AfterRender + 1);

	/// <summary>
	/// A special platform hook for jobs to be executed before the normal render cycle
	/// </summary>
	[PrivateApi]
	public static readonly DispatcherPriority BeforeRender = new DispatcherPriority((int)Render + 1);

	/// <summary>
	/// A special priority for platforms that resize the render target in asynchronous-ish matter,
	/// should be changed into event grouping in the platform backend render
	/// </summary>
	[PrivateApi]
	public static readonly DispatcherPriority AsyncRenderTargetResize = new DispatcherPriority((int)BeforeRender + 1);

	/// <summary>
	/// The job will be processed with the same priority as data binding.
	/// </summary>
	[Obsolete("WPF compatibility")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static readonly DispatcherPriority DataBind = new DispatcherPriority((int)AsyncRenderTargetResize + 1);

	/// <summary>
	/// The job will be processed with normal priority.
	/// </summary>
	public static readonly DispatcherPriority Normal = new DispatcherPriority((int)DataBind + 1);

	/// <summary>
	/// The job will be processed before other asynchronous operations.
	/// </summary>
	public static readonly DispatcherPriority Send = new DispatcherPriority((int)Normal + 1);

	/// <summary>
	/// Maximum possible priority
	/// </summary>
	public static readonly DispatcherPriority MaxValue = Send;

	/// <summary>
	/// The integer value of the priority
	/// </summary>
	public int Value { get; }

	private DispatcherPriority(int value)
	{
		Value = value;
	}

	public static DispatcherPriority FromValue(int value)
	{
		if (value < MinValue.Value || value > MaxValue.Value)
		{
			throw new ArgumentOutOfRangeException("value");
		}
		return new DispatcherPriority(value);
	}

	public static implicit operator int(DispatcherPriority priority)
	{
		return priority.Value;
	}

	public static implicit operator DispatcherPriority(int value)
	{
		return FromValue(value);
	}

	/// <inheritdoc />
	public bool Equals(DispatcherPriority other)
	{
		return Value == other.Value;
	}

	/// <inheritdoc />
	public override bool Equals(object? obj)
	{
		if (obj is DispatcherPriority other)
		{
			return Equals(other);
		}
		return false;
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return Value.GetHashCode();
	}

	public static bool operator ==(DispatcherPriority left, DispatcherPriority right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(DispatcherPriority left, DispatcherPriority right)
	{
		return left.Value != right.Value;
	}

	public static bool operator <(DispatcherPriority left, DispatcherPriority right)
	{
		return left.Value < right.Value;
	}

	public static bool operator >(DispatcherPriority left, DispatcherPriority right)
	{
		return left.Value > right.Value;
	}

	public static bool operator <=(DispatcherPriority left, DispatcherPriority right)
	{
		return left.Value <= right.Value;
	}

	public static bool operator >=(DispatcherPriority left, DispatcherPriority right)
	{
		return left.Value >= right.Value;
	}

	/// <inheritdoc />
	public int CompareTo(DispatcherPriority other)
	{
		return Value.CompareTo(other.Value);
	}

	public static void Validate(DispatcherPriority priority, string parameterName)
	{
		if (priority < Inactive || priority > MaxValue)
		{
			throw new ArgumentException("Invalid DispatcherPriority value", parameterName);
		}
	}

	public override string ToString()
	{
		if (this == Invalid)
		{
			return "Invalid";
		}
		if (this == Inactive)
		{
			return "Inactive";
		}
		if (this == SystemIdle)
		{
			return "SystemIdle";
		}
		if (this == ContextIdle)
		{
			return "ContextIdle";
		}
		if (this == ApplicationIdle)
		{
			return "ApplicationIdle";
		}
		if (this == Background)
		{
			return "Background";
		}
		if (this == Input)
		{
			return "Input";
		}
		if (this == Default)
		{
			return "Default";
		}
		if (this == Loaded)
		{
			return "Loaded";
		}
		if (this == UiThreadRender)
		{
			return "UiThreadRender";
		}
		if (this == AfterRender)
		{
			return "AfterRender";
		}
		if (this == Render)
		{
			return "Render";
		}
		if (this == BeforeRender)
		{
			return "BeforeRender";
		}
		if (this == AsyncRenderTargetResize)
		{
			return "AsyncRenderTargetResize";
		}
		if (this == DataBind)
		{
			return "DataBind";
		}
		if (this == Normal)
		{
			return "Normal";
		}
		if (this == Send)
		{
			return "Send";
		}
		return Value.ToString();
	}
}
