using CommunityToolkit.Mvvm.ComponentModel;

namespace Luminalium.App.ViewModels;

/// <summary>
/// A single toolbar tool in the "工具可用性" editor. Backs an <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/>
/// so each toggle maps to a slot in <c>ToolbarSettings.DisabledTools</c>.
/// </summary>
public sealed partial class ToolAvailabilityItem : ObservableObject
{
    private readonly ToolbarSettingsViewModel _owner;

    public ToolAvailabilityItem(ToolbarSettingsViewModel owner, string id, string label, bool isDisabled)
    {
        _owner = owner;
        Id = id;
        Label = label;
        IsDisabled = isDisabled;
    }

    public string Id { get; }

    public string Label { get; }

    public bool IsVisible
    {
        get => !IsDisabled;
        set => IsDisabled = !value;
    }

    [ObservableProperty]
    private bool isDisabled;

    partial void OnIsDisabledChanged(bool value)
    {
        OnPropertyChanged(nameof(IsVisible));
        _owner.SetToolDisabled(Id, value);
    }
}

/// <summary>
/// An entry in the "工具顺序" editor. Pure display carrier; the owning view model
/// moves entries and persists the resulting <c>ToolbarSettings.ToolbarOrder</c>.
/// </summary>
public sealed record ToolOrderItem(string Id, string Label);
