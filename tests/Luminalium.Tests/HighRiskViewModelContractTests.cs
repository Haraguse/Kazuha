using Luminalium.App.Services;
using Luminalium.App.ViewModels;
using Luminalium.Core.Identity;
using Luminalium.Core.Localization;
using Luminalium.Core.Platform;
using Xunit;

namespace Luminalium.Tests;

/// <summary>
/// T21 contract fixtures for high-risk shell ViewModels and the process-launch
/// service. These assert typed behavior without any Office/WPS or UI host, so
/// the suite stays deterministic on a bare CI runner.
/// </summary>
public sealed class HighRiskViewModelContractTests
{
    [Fact]
    public void AboutDialogExposesProductIdentityAndVersionDisplay()
    {
        var viewModel = new AboutDialogViewModel("1.4.400.1-nightly");

        Assert.Equal(ProductIdentity.DisplayName, viewModel.ProductName);
        Assert.Equal(ProductIdentity.WindowsAppUserModelId, viewModel.AppUserModelId);
        Assert.Equal("1.4.400.1-nightly", viewModel.VersionDisplay);
    }

    [Fact]
    public void PasswordDialogTitlesDescriptionsAndConfirmationFollowMode()
    {
        var localization = new LocalizationService();

        var unlock = new PasswordDialogViewModel(localization, PasswordDialogMode.Unlock);
        Assert.Equal(localization["Settings.Password.UnlockTitle"], unlock.Title);
        Assert.Equal(localization["Settings.Password.UnlockDescription"], unlock.Description);
        Assert.Equal(localization["Settings.Password.UnlockButton"], unlock.PrimaryButtonText);
        Assert.False(unlock.RequiresConfirmation);

        var enable = new PasswordDialogViewModel(localization, PasswordDialogMode.Enable);
        Assert.Equal(localization["Settings.Password.EnableTitle"], enable.Title);
        Assert.Equal(localization["Settings.Password.EnableDescription"], enable.Description);
        Assert.True(enable.RequiresConfirmation);

        var change = new PasswordDialogViewModel(localization, PasswordDialogMode.Change);
        Assert.Equal(localization["Settings.Password.ChangeTitle"], change.Title);
        Assert.Equal(localization["Settings.Password.ChangeDescription"], change.Description);
        Assert.True(change.RequiresConfirmation);

        Assert.Equal(localization["Settings.Password.PasswordLabel"], unlock.PasswordLabel);
        Assert.Equal(localization["Settings.Password.ConfirmationLabel"], change.ConfirmationLabel);
        Assert.Equal(localization["Dialog.Button.Cancel"], unlock.CloseButtonText);
    }

    [Fact]
    public void PasswordDialogSubmitCarriesPasswordAndConfirmation()
    {
        var viewModel = new PasswordDialogViewModel(new LocalizationService(), PasswordDialogMode.Enable)
        {
            Password = "s3cret",
            Confirmation = "s3cret",
        };

        var result = viewModel.Submit();

        Assert.Equal(PasswordDialogOutcome.Submitted, result.Outcome);
        Assert.Equal("s3cret", result.Password);
        Assert.Equal("s3cret", result.Confirmation);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProcessLaunchRejectsEmptyOrWhitespacePathWithTypedNotFound(string? path)
    {
        var result = new ProcessLaunchService().Launch(path!);

        Assert.False(result.IsSuccess);
        Assert.Equal(PlatformOperationErrorCode.NotFound, result.Error!.Code);
    }

    [Fact]
    public void ProcessLaunchRejectsMissingFileWithTypedNotFound()
    {
        var missing = Path.Combine(Path.GetTempPath(), "Luminalium-DoesNotExist-" + Guid.NewGuid().ToString("N") + ".exe");

        var result = new ProcessLaunchService().Launch(missing);

        Assert.False(result.IsSuccess);
        Assert.Equal(PlatformOperationErrorCode.NotFound, result.Error!.Code);
        Assert.Contains("File not found", result.Error.Message, StringComparison.Ordinal);
    }
}
