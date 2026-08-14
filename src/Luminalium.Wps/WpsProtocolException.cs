namespace Luminalium.Wps;

public sealed class WpsProtocolException : Exception
{
    public WpsProtocolException(string message)
        : base(message)
    {
    }
}
