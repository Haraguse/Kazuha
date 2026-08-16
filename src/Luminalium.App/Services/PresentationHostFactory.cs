using System.Reflection;
using Luminalium.Presentation;

namespace Luminalium.App.Services;

/// <summary>
/// Runtime factory for PowerPoint presentation components. The App is a plain
/// net10.0 project and must not reference the net10.0-windows project
/// Luminalium.Presentation.Windows (ForbiddenReferenceCheck rule 5), so the
/// concrete adapters are loaded by assembly-qualified name at runtime and the
/// plain-project PowerPointPresentationHost is constructed here. Any load
/// failure degrades to null so callers keep their existing no-host fallback.
/// </summary>
public static class PresentationHostFactory
{
    private const string ComAdapterTypeName =
        "Luminalium.Presentation.Windows.PowerPointComAdapter, Luminalium.Presentation.Windows";
    private const string WindowAdapterTypeName =
        "Luminalium.Presentation.Windows.Win32SlideshowWindowAdapter, Luminalium.Presentation.Windows";

    public static IOfficeComAdapter? TryCreateComAdapter()
    {
        try
        {
            var type = Type.GetType(ComAdapterTypeName, throwOnError: false);
            if (type is null || Activator.CreateInstance(type) is not IOfficeComAdapter adapter)
            {
                return null;
            }

            return adapter;
        }
        catch (Exception exception) when (exception is FileNotFoundException or FileLoadException or TypeLoadException or MissingMethodException or MemberAccessException or TargetInvocationException)
        {
            return null;
        }
    }

    public static ISlideshowWindowAdapter? TryCreateWindowAdapter()
    {
        try
        {
            var type = Type.GetType(WindowAdapterTypeName, throwOnError: false);
            if (type is null || Activator.CreateInstance(type) is not ISlideshowWindowAdapter adapter)
            {
                return null;
            }

            return adapter;
        }
        catch (Exception exception) when (exception is FileNotFoundException or FileLoadException or TypeLoadException or MissingMethodException or MemberAccessException or TargetInvocationException)
        {
            return null;
        }
    }

    public static IPresentationHost? TryCreatePowerPointHost()
    {
        var comAdapter = TryCreateComAdapter();
        var windowAdapter = TryCreateWindowAdapter();
        if (comAdapter is null || windowAdapter is null)
        {
            return null;
        }

        return new PowerPointPresentationHost(comAdapter, windowAdapter);
    }
}
