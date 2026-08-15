using Luminalium.App.ViewModels;

namespace Luminalium.App.Services;

public sealed class StartupErrorCoordinator
{
    private readonly IDialogService _dialogService;
    private int _dialogRequested;

    public StartupErrorCoordinator(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task<RetryCloseDialogResult> PresentAsync(
        ShellViewModel shellViewModel,
        Exception exception,
        Func<Task<bool>> retryAsync,
        Action close)
    {
        if (Interlocked.Exchange(ref _dialogRequested, 1) != 0)
        {
            shellViewModel.ReportLocalizedError(ShellErrorKind.DialogAlreadyOpen, "Dialog.Error.AlreadyOpen");
            return RetryCloseDialogResult.AlreadyOpen;
        }

        try
        {
            var message = FailureTextSanitizer.CreateMessage(shellViewModel, exception);
            var closeCalled = 0;
            void CloseOnce()
            {
                if (Interlocked.Exchange(ref closeCalled, 1) == 0)
                {
                    close();
                }
            }
            return await _dialogService.ShowRetryCloseAsync(
                shellViewModel,
                new RetryCloseDialogRequest(
                    shellViewModel.Localization["Dialog.RetryClose.Title"],
                    message,
                    retryAsync,
                    CloseOnce)).ConfigureAwait(true);
        }
        finally
        {
            Volatile.Write(ref _dialogRequested, 0);
        }
    }
}

public static class FailureTextSanitizer
{
    private static readonly string[] SensitiveMarkers =
    [
        "password",
        "passwordhash",
        "config",
        "secret",
        "token",
        "credential",
    ];

    public static string CreateMessage(ShellViewModel shellViewModel, Exception exception)
    {
        var stable = shellViewModel.Localization["Dialog.RetryClose.Message"];
        var typeName = exception.GetType().Name;
        var detail = exception.Message.Trim();
        if (detail.Length == 0 || ContainsSensitiveText(typeName) || ContainsSensitiveText(detail))
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                shellViewModel.Localization["Dialog.RetryClose.MessageWithType"],
                stable,
                typeName);
        }

        return string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            shellViewModel.Localization["Dialog.RetryClose.MessageWithDetail"],
            stable,
            typeName,
            detail);
    }

    private static bool ContainsSensitiveText(string value) =>
        SensitiveMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
