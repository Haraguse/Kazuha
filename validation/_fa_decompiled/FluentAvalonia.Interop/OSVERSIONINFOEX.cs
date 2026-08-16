namespace FluentAvalonia.Interop;

internal struct OSVERSIONINFOEX
{
	public uint OSVersionInfoSize;

	public uint MajorVersion;

	public uint MinorVersion;

	public uint BuildNumber;

	public uint PlatformId;

	public unsafe fixed ushort CSDVersion[128];

	public ushort ServicePackMajor;

	public ushort ServicePackMinor;

	public ushort SuiteMask;

	public byte ProductType;

	public byte Reserved;
}
