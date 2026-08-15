using Luminalium.Core.Configuration;
using Luminalium.Core.Security;
using Xunit;

namespace Luminalium.Tests;

public sealed class PasswordHashServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"Luminalium-PasswordHash-{Guid.NewGuid():N}");

    public PasswordHashServiceTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void HashesUseDocumentedFormatAndVerifyWithOnlyTheOriginalPassword()
    {
        var service = new PasswordHashService();

        var hash = service.HashPassword("correct-password", PasswordHashService.MinimumIterations);

        Assert.NotNull(hash);
        Assert.StartsWith("pbkdf2$sha256$100000$", hash, StringComparison.Ordinal);
        Assert.DoesNotContain("correct-password", hash, StringComparison.Ordinal);
        Assert.DoesNotContain(" ", hash, StringComparison.Ordinal);
        Assert.True(service.VerifyPassword("correct-password", hash));
        Assert.False(service.VerifyPassword("wrong-password", hash));
    }

    [Fact]
    public void EachHashUsesAnIndependentRandomSalt()
    {
        var service = new PasswordHashService();

        var first = service.HashPassword("same-password", PasswordHashService.MinimumIterations);
        var second = service.HashPassword("same-password", PasswordHashService.MinimumIterations);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first, second);
        Assert.True(service.VerifyPassword("same-password", first));
        Assert.True(service.VerifyPassword("same-password", second));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("pbkdf2$sha256$100000$bad$bad")]
    [InlineData("pbkdf2$sha1$100000$AA$AA")]
    [InlineData("scrypt$N=16384$r=8$p=1$salt$hash")]
    [InlineData("pbkdf2$sha256$0$AA$AA")]
    [InlineData("pbkdf2$sha256$100000$AA$AA$extra")]
    [InlineData("pbkdf2$sha256$100000$AA AA$AA")]
    public void MalformedUnsupportedAndEmptyInputsReturnFalse(string? passwordHash)
    {
        var service = new PasswordHashService();

        Assert.False(service.VerifyPassword("candidate", passwordHash));
        Assert.False(service.VerifyPassword("", passwordHash));
        Assert.False(service.VerifyPassword(null, passwordHash));
    }

    [Fact]
    public void InvalidHashLengthsReturnFalseWithoutThrowing()
    {
        var service = new PasswordHashService();

        var invalidSalt = "pbkdf2$sha256$100000$AA$" + Base64Url(new byte[PasswordHashService.DerivedKeyLength]);
        var invalidDerived = "pbkdf2$sha256$100000$" + Base64Url(new byte[PasswordHashService.SaltLength]) + "$AA";
        var invalidIteration = "pbkdf2$sha256$10000001$" + Base64Url(new byte[PasswordHashService.SaltLength]) + "$" + Base64Url(new byte[PasswordHashService.DerivedKeyLength]);

        Assert.False(service.VerifyPassword("candidate", invalidSalt));
        Assert.False(service.VerifyPassword("candidate", invalidDerived));
        Assert.False(service.VerifyPassword("candidate", invalidIteration));
    }

    [Fact]
    public void EmptyPasswordCannotBeHashed()
    {
        var service = new PasswordHashService();

        Assert.Null(service.HashPassword(string.Empty));
        Assert.Null(service.HashPassword(null));
    }

    [Fact]
    public void InvalidIterationCountsCannotBeHashed()
    {
        var service = new PasswordHashService();

        Assert.Null(service.HashPassword("candidate", PasswordHashService.MinimumIterations - 1));
        Assert.Null(service.HashPassword("candidate", PasswordHashService.MaximumIterations + 1));
    }

    [Fact]
    public void GeneratedHashCanBeSavedAndReloadedWithoutPersistingPlaintext()
    {
        var service = new PasswordHashService();
        var password = "configuration-password";
        var hash = service.HashPassword(password, PasswordHashService.MinimumIterations);
        var configuration = LuminaliumConfig.CreateDefault();
        configuration.Security.PasswordProtectionEnabled = true;
        configuration.Security.PasswordHash = hash!;

        var saveResult = new ConfigurationService(_directory).Save(configuration);
        var loadResult = new ConfigurationService(_directory).Load();
        var persisted = File.ReadAllText(loadResult.Path);

        Assert.True(saveResult.IsSuccess);
        Assert.True(loadResult.IsSuccess);
        Assert.Equal(hash, loadResult.Config.Security.PasswordHash);
        Assert.True(service.VerifyPassword(password, loadResult.Config.Security.PasswordHash));
        Assert.DoesNotContain(password, persisted, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
