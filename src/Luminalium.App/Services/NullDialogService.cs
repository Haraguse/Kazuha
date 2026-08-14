using Luminalium.App.ViewModels;

namespace Luminalium.App.Services;

public sealed class NullDialogService : IDialogService
{
    public static NullDialogService Instance { get; } = new();

    private NullDialogService()
    {
    }

    public Task ShowAboutAsync(ShellViewModel shellViewModel) => Task.CompletedTask;
}
