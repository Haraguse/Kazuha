using Luminalium.App.Services;
using Luminalium.Presentation;
using Xunit;

namespace Luminalium.Tests;

public sealed class PresentationHostFactoryTests
{
    [Fact]
    public void TryCreatePowerPointHostReturnsPowerPointHost()
    {
        var host = PresentationHostFactory.TryCreatePowerPointHost();

        Assert.NotNull(host);
        Assert.Equal(PresentationHostKind.PowerPoint, host.HostKind);
    }

    [Fact]
    public void TryCreateWindowAdapterReturnsAdapter()
    {
        Assert.NotNull(PresentationHostFactory.TryCreateWindowAdapter());
    }

    [Fact]
    public void TryCreateComAdapterReturnsAdapter()
    {
        Assert.NotNull(PresentationHostFactory.TryCreateComAdapter());
    }
}
