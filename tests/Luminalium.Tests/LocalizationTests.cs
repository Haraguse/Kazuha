using System.Text;
using System.Text.Json;
using Luminalium.App.Overlay;
using Luminalium.App.ViewModels;
using Luminalium.Core.Configuration;
using Luminalium.Core.Localization;
using Luminalium.Presentation;
using Xunit;

namespace Luminalium.Tests;

public sealed class LocalizationTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"Luminalium-Localization-{Guid.NewGuid():N}");

    public LocalizationTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Theory]
    [InlineData(AppLanguage.ZhCn, "设置")]
    [InlineData(AppLanguage.ZhTw, "設定")]
    [InlineData(AppLanguage.YueHk, "設定")]
    [InlineData(AppLanguage.JaJp, "設定")]
    [InlineData(AppLanguage.EnUs, "Settings")]
    [InlineData(AppLanguage.UgCn, "تەڭشەكلەر")]
    public void LookupReturnsLocalizedNavigationLabelForEveryLanguage(AppLanguage language, string expected)
    {
        var localization = new LocalizationService();

        localization.SetLanguage(language);

        Assert.Equal(expected, localization["Navigation.Settings"]);
    }

    [Fact]
    public void MissingNonDefaultKeyFallsBackToZhCnAndUnknownKeyReturnsKey()
    {
        var localization = new LocalizationService();

        localization.SetLanguage(AppLanguage.JaJp);

        Assert.Equal("默认中文回退文本", localization["Localization.FallbackProbe"]);
        Assert.Equal("Missing.Unknown.Key", localization["Missing.Unknown.Key"]);
    }

    [Fact]
    public void SetLanguageRaisesChangedOnceWithSelectedLanguage()
    {
        var localization = new LocalizationService();
        var observed = new List<AppLanguage>();
        localization.LanguageChanged += (_, args) => observed.Add(args.Language);

        var result = localization.SetLanguage(AppLanguage.EnUs);

        Assert.True(result.IsSuccess);
        Assert.Equal(AppLanguage.EnUs, result.Language);
        Assert.Equal([AppLanguage.EnUs], observed);
    }

    [Fact]
    public void InvalidLanguageCodeIsRejected()
    {
        var localization = new LocalizationService();

        var result = localization.SetLanguageCode("fr-FR");

        Assert.False(result.IsSuccess);
        Assert.Equal("fr-FR", result.Code);
        Assert.Equal(AppLanguage.ZhCn, localization.CurrentLanguage);
    }

    [Fact]
    public void ConfigRoundTripPersistsLanguageAndDefaultsToZhCn()
    {
        var service = new ConfigurationService(_directory);
        var created = service.Load();
        created.Config.General.Language = "en-US";

        var saveResult = service.Save(created.Config);
        var reloaded = new ConfigurationService(_directory).Load();
        var json = File.ReadAllText(Path.Combine(_directory, ConfigurationService.BaseSettingsFileName), new UTF8Encoding(false));
        var persisted = JsonSerializer.Deserialize<LuminaliumConfig>(json, ConfigurationJson.Options);

        Assert.Equal("zh-CN", LuminaliumConfig.CreateDefault().General.Language);
        Assert.True(saveResult.IsSuccess);
        Assert.True(reloaded.IsSuccess);
        Assert.Equal("en-US", reloaded.Config.General.Language);
        Assert.Equal("en-US", persisted!.General.Language);
    }

    [Fact]
    public void InvalidConfigLanguageIsRejected()
    {
        var service = new ConfigurationService(_directory);
        var config = LuminaliumConfig.CreateDefault();
        config.General.Language = "fr-FR";

        var result = service.Save(config);

        Assert.False(result.IsSuccess);
        Assert.Equal(nameof(GeneralSettings.Language), result.Error!.Field);
    }

    [Fact]
    public void ViewModelLabelChangesWhenLanguageSwitches()
    {
        var localization = new LocalizationService();
        var viewModel = new ShellViewModel(localizationService: localization);

        Assert.Equal("设置", viewModel.Settings.Title);

        localization.SetLanguage(AppLanguage.EnUs);

        Assert.Equal("Settings", viewModel.Settings.Title);
        Assert.Equal("Open overlay", viewModel.OpenOverlayText);
    }

    [Fact]
    public async Task OverlayStatusChangesWhenLanguageSwitches()
    {
        var localization = new LocalizationService();
        var viewModel = new OverlayViewModel(
            new PresentationMonitor([new FakePresentationHost()], new AlwaysAllowCommandTimer()),
            new FixedScreenProvider([new OverlayScreenBounds(0, 0, 1280, 720, 1.0)]),
            localizationService: localization);

        await viewModel.InitializeAsync();
        Assert.Equal("幻灯片 1 / 5", viewModel.StatusText);

        localization.SetLanguage(AppLanguage.EnUs);

        Assert.Equal("Slide 1 / 5", viewModel.StatusText);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private sealed class FakePresentationHost : IPresentationHost
    {
        public PresentationHostKind HostKind => PresentationHostKind.PowerPoint;

        public PresentationState State { get; private set; } = new(
            1,
            5,
            true,
            PresentationPointerType.Arrow,
            "#FF0000",
            PresentationHostKind.PowerPoint,
            false,
            false);

        public Task<PresentationOperationResult> ConnectAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Success());

        public Task<PresentationOperationResult<PresentationState>> GetStateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperation.Success(State));

        public Task<PresentationOperationResult> NavigateNextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Success());

        public Task<PresentationOperationResult> NavigatePreviousAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Success());

        public Task<PresentationOperationResult> GotoSlideAsync(int slideIndex, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Success());

        public Task<PresentationOperationResult> ApplyPenColorAsync(string colorHex, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Success());

        public Task<PresentationOperationResult> SetPointerTypeAsync(PresentationPointerType pointerType, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Success());

        public Task<PresentationOperationResult> ZoomAsync(double zoomFactor, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresentationOperationResult.Success());
    }

    private sealed class AlwaysAllowCommandTimer : ICommandTimer
    {
        public PresentationOperationResult TryConsumePageTurn() => PresentationOperationResult.Success();
    }

    private sealed class FixedScreenProvider(IReadOnlyList<OverlayScreenBounds> screens) : IOverlayScreenProvider
    {
        public IReadOnlyList<OverlayScreenBounds> GetScreens() => screens;

        public OverlayScreenBounds GetScreen(int? screenIndex = null) => screens[screenIndex ?? 0];
    }
}
