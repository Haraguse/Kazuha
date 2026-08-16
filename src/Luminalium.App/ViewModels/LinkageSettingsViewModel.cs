using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.App.Services;
using Luminalium.Core.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Luminalium.App.ViewModels;

[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Settings labels are bound to the UI as instance members so compiled bindings resolve consistently.")]
public sealed partial class LinkageSettingsViewModel : ObservableObject
{
    private readonly SettingsCoordinator? _coordinator;
    private bool _initializing;

    public LinkageSettingsViewModel(LuminaliumConfig? config = null, SettingsCoordinator? coordinator = null)
    {
        _coordinator = coordinator;
        var settings = config?.Linkage ?? new LinkageSettings();
        _initializing = true;
        secRandomEnabled = settings.SecRandomEnabled;
        _initializing = false;
    }

    [ObservableProperty]
    private bool secRandomEnabled;

    public string PageTitle => "联动";

    public string RandomLinkageSectionTitle => "随机联动";

    public string RandomLinkageLabel => "随机联动模式";

    public string RandomLinkageDescription => "启用后以随机顺序联动触发幻灯片翻页与 Overlay 动作，避免每次操作顺序过于固定";

    partial void OnSecRandomEnabledChanged(bool value) => Persist(c => c.Linkage.SecRandomEnabled = value);

    private void Persist(Action<LuminaliumConfig> update)
    {
        if (!_initializing && _coordinator is not null)
        {
            _ = _coordinator.UpdateAsync(update);
        }
    }
}