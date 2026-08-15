using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.App.Services;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public sealed partial class PasswordDialogViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;

    public PasswordDialogViewModel(ILocalizationService localization, PasswordDialogMode mode)
    {
        _localization = localization;
        Mode = mode;
    }

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string confirmation = string.Empty;

    public PasswordDialogMode Mode { get; }

    public string Title => _localization[Mode switch
    {
        PasswordDialogMode.Unlock => "Settings.Password.UnlockTitle",
        PasswordDialogMode.Enable => "Settings.Password.EnableTitle",
        _ => "Settings.Password.ChangeTitle",
    }];

    public string Description => _localization[Mode switch
    {
        PasswordDialogMode.Unlock => "Settings.Password.UnlockDescription",
        PasswordDialogMode.Enable => "Settings.Password.EnableDescription",
        _ => "Settings.Password.ChangeDescription",
    }];

    public string PasswordLabel => _localization["Settings.Password.PasswordLabel"];

    public string ConfirmationLabel => _localization["Settings.Password.ConfirmationLabel"];

    public string PrimaryButtonText => _localization[Mode switch
    {
        PasswordDialogMode.Unlock => "Settings.Password.UnlockButton",
        PasswordDialogMode.Enable => "Settings.Password.EnableButton",
        _ => "Settings.Password.ChangeButton",
    }];

    public string CloseButtonText => _localization["Dialog.Button.Cancel"];

    public bool RequiresConfirmation => Mode is not PasswordDialogMode.Unlock;

    public PasswordDialogResult Submit() => PasswordDialogResult.Submitted(Password, Confirmation);
}
