using Luminalium.Core.Media;

namespace Luminalium.Smtc.Windows;

public static class SmtcWindowsFactory
{
    /// <summary>
    /// Creates the optional WinRT SMTC adapter when the Windows projection can be loaded.
    /// The App loads this assembly optionally at runtime; consumer wiring is deferred.
    /// </summary>
    public static ISmtcSessionAdapter? TryCreateAdapter()
    {
        try
        {
            return new WinRtSmtcSessionAdapter();
        }
        catch (PlatformNotSupportedException)
        {
            return null;
        }
        catch (TypeInitializationException)
        {
            return null;
        }
    }
}
