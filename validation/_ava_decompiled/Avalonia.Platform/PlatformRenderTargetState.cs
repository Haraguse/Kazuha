using Avalonia.Metadata;

namespace Avalonia.Platform;

/// <summary>
/// Describes the current readiness state of a platform render target.
/// Flows through the entire rendering pipeline from platform to compositor.
/// </summary>
[PrivateApi]
public readonly struct PlatformRenderTargetState
{
	/// <summary>
	/// Indicates if the render target is currently ready to be rendered to.
	/// </summary>
	public bool IsReady { get; init; }

	/// <summary>
	/// Indicates that the render target is currently not ready but will wake up the render loop
	/// when it becomes ready (e.g. waiting for a compositor frame callback).
	/// When true, the compositor should not keep polling for readiness.
	/// </summary>
	public bool WillWakeUpRenderLoopWhenReady { get; init; }

	/// <summary>
	/// Indicates if the render target is no longer usable and needs to be recreated
	/// </summary>
	public bool IsCorrupted { get; init; }

	/// <summary>
	/// A readiness state indicating the target is ready to render.
	/// </summary>
	public static PlatformRenderTargetState Ready => new PlatformRenderTargetState
	{
		IsReady = true
	};

	public static PlatformRenderTargetState NotReadyTryLater => default(PlatformRenderTargetState);

	public static PlatformRenderTargetState Corrupted => new PlatformRenderTargetState
	{
		IsCorrupted = true,
		IsReady = true
	};

	public static PlatformRenderTargetState Disposed => new PlatformRenderTargetState
	{
		IsCorrupted = true
	};

	public static PlatformRenderTargetState NotReadyWillWakeupRenderLoop => new PlatformRenderTargetState
	{
		WillWakeUpRenderLoopWhenReady = true
	};
}
