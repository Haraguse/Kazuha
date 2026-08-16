namespace Avalonia.Platform;

/// <summary>
/// Describes various GPU semaphore handle types that are currently supported by Avalonia graphics backends
/// </summary>
public static class KnownPlatformGraphicsExternalSemaphoreHandleTypes
{
	/// <summary>
	/// A POSIX file descriptor that's been exported by Vulkan using VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_FD_BIT or in a compatible way
	/// </summary>
	public const string VulkanOpaquePosixFileDescriptor = "VulkanOpaquePosixFileDescriptor";

	/// <summary>
	/// A NT handle that's been exported by Vulkan using VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_WIN32_BIT or in a compatible way
	/// </summary>
	public const string VulkanOpaqueNtHandle = "VulkanOpaqueNtHandle";

	public const string VulkanOpaqueKmtHandle = "VulkanOpaqueKmtHandle";

	/// A DXGI NT handle returned by ID3D12Device::CreateSharedHandle or ID3D11Fence::CreateSharedHandle
	public const string Direct3D12FenceNtHandle = "Direct3D12FenceNtHandle";

	/// <summary>
	/// A pointer to MTLSharedEvent object
	/// </summary>
	public const string MetalSharedEvent = "MetalSharedEvent";
}
