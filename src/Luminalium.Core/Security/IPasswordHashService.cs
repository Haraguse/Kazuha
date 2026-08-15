namespace Luminalium.Core.Security;

public interface IPasswordHashService
{
    string? HashPassword(string? password, int iterations = PasswordHashService.DefaultIterations);

    bool VerifyPassword(string? password, string? passwordHash);
}
