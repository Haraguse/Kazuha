namespace Luminalium.App.Services;

public enum PasswordDialogMode
{
    Unlock,
    Enable,
    Change,
}

public enum PasswordDialogOutcome
{
    Cancelled,
    Submitted,
}

public sealed record PasswordDialogResult(
    PasswordDialogOutcome Outcome,
    string? Password = null,
    string? Confirmation = null)
{
    public static PasswordDialogResult Cancelled { get; } = new(PasswordDialogOutcome.Cancelled);

    public static PasswordDialogResult Submitted(string password, string? confirmation = null) =>
        new(PasswordDialogOutcome.Submitted, password, confirmation);
}
