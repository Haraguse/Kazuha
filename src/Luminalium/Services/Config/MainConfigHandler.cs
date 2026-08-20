using Luminalium.Models.Config;

namespace Luminalium.Services.Config;

public sealed class MainConfigHandler : ConfigHandlerBase<MainConfigModel>
{
    public MainConfigHandler() : base(MainConfigModel.CreateDefault)
    {
    }
}
