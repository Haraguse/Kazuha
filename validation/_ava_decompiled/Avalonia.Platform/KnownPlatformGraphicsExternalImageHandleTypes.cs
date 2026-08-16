namespace Avalonia.Platform;

/// <summary>
/// Describes various GPU memory handle types that are currently supported by Avalonia graphics backends
/// </summary>
public static class KnownPlatformGraphicsExternalImageHandleTypes
{
	/// <summary>
	/// An DXGI global shared handle returned by IDXGIResource::GetSharedHandle D3D11_RESOURCE_MISC_SHARED or D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX flag.
	/// The handle does not own the reference to the underlying video memory, so the provider should make sure that the resource is valid until
	/// the handle has been successfully imported
	/// </summary>
	public const string D3D11TextureGlobalSharedHandle = "D3D11TextureGlobalSharedHandle";

	/// <summary>
	/// A DXGI NT handle returned by IDXGIResource1::CreateSharedHandle for a texture created with D3D11_RESOURCE_MISC_SHARED_NTHANDLE or flag
	/// </summary>
	public const string D3D11TextureNtHandle = "D3D11TextureNtHandle";

	/// <summary>
	/// A POSIX file descriptor that's exported by Vulkan using VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_FD_BIT or in a compatible way
	/// </summary>
	public const string VulkanOpaquePosixFileDescriptor = "VulkanOpaquePosixFileDescriptor";

	/// <summary>
	/// A NT handle that's been exported by Vulkan using VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_WIN32_BIT or in a compatible way
	/// </summary>
	public const string VulkanOpaqueNtHandle = "VulkanOpaqueNtHandle";

	public const string VulkanOpaqueKmtHandle = "VulkanOpaqueKmtHandle";

	/// <summary>
	/// A reference to IOSurface
	/// </summary>
	public const string IOSurfaceRef = "IOSurfaceRef";
}
