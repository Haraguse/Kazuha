using Luminalium.Core.Configuration;
using Luminalium.App.ViewModels;
using Luminalium.Core.Localization;
using Xunit;

namespace Luminalium.Tests;

public sealed class FontAndSplashPolicyTests
{
    [Fact]
    public void EmptyAndUnknownFontFamiliesUseTheCjkFallbackStack()
    {
        Assert.Equal(
            FontFamilyResolver.DefaultFontFamilyStack,
            FontFamilyResolver.Resolve(null));
        Assert.Equal(
            FontFamilyResolver.DefaultFontFamilyStack,
            FontFamilyResolver.Resolve("missing-font"));
    }

    [Fact]
    public void KnownConfiguredFontIsPrependedToTheFallbackStack()
    {
        var resolved = FontFamilyResolver.Resolve(
            "Noto Sans CJK",
            ["Noto Sans CJK"]);

        Assert.Equal(
            $"Noto Sans CJK, {FontFamilyResolver.DefaultFontFamilyStack}",
            resolved);
    }

    [Theory]
    [InlineData("font, injected")]
    [InlineData("font; injected")]
    [InlineData("font\"injected")]
    [InlineData("font\ninjected")]
    public void UnsafeConfiguredFontNamesUseTheCjkFallbackStack(string configuredFamily)
    {
        Assert.Equal(
            FontFamilyResolver.DefaultFontFamilyStack,
            FontFamilyResolver.Resolve(configuredFamily, [configuredFamily]));
    }

    [Fact]
    public void OversizedConfiguredFontNamesUseTheCjkFallbackStack()
    {
        var configuredFamily = new string('f', FontFamilyResolver.MaximumFamilyNameLength + 1);

        Assert.Equal(
            FontFamilyResolver.DefaultFontFamilyStack,
            FontFamilyResolver.Resolve(configuredFamily, [configuredFamily]));
    }

    [Theory]
    [InlineData(SplashMode.Always, false, true)]
    [InlineData(SplashMode.Always, true, true)]
    [InlineData(SplashMode.Never, false, false)]
    [InlineData(SplashMode.Never, true, false)]
    [InlineData(SplashMode.HideOnAutoStart, false, true)]
    [InlineData(SplashMode.HideOnAutoStart, true, false)]
    public void FixedSplashModesRespectAutoStart(
        SplashMode mode,
        bool isAutoStart,
        bool expectedVisibility)
    {
        var result = Evaluate(mode, isAutoStart: isAutoStart);

        Assert.Equal(expectedVisibility, result.IsVisible);
    }

    [Fact]
    public void TimeRangeIncludesBothBoundariesAndDetailedStateOnlyWhenVisible()
    {
        var service = new SplashPolicyService();
        var atStart = service.Evaluate(
            SplashMode.TimeRange,
            "default",
            true,
            "08:00",
            "20:00",
            false,
            new TimeOnly(8, 0));
        var atEnd = service.Evaluate(
            SplashMode.TimeRange,
            "default",
            true,
            "08:00",
            "20:00",
            false,
            new TimeOnly(20, 0));
        var outside = service.Evaluate(
            SplashMode.TimeRange,
            "default",
            true,
            "08:00",
            "20:00",
            false,
            new TimeOnly(20, 1));

        Assert.True(atStart.IsVisible);
        Assert.True(atStart.ShowDetailedSplash);
        Assert.True(atEnd.IsVisible);
        Assert.False(outside.IsVisible);
        Assert.False(outside.ShowDetailedSplash);
    }

    [Fact]
    public void GeneralSettingsOverloadUsesTheCallerProvidedTimeAndAutoStartState()
    {
        var settings = new GeneralSettings
        {
            SplashMode = SplashMode.HideOnAutoStart,
            SplashStyle = "default",
            ShowDetailedSplash = true,
            SplashStartTime = "08:00",
            SplashEndTime = "20:00",
        };
        var service = new SplashPolicyService();

        var autoStartResult = service.Evaluate(settings, true, new TimeOnly(12, 0));
        var manualStartResult = service.Evaluate(settings, false, new TimeOnly(12, 0));

        Assert.False(autoStartResult.IsVisible);
        Assert.False(autoStartResult.ShowDetailedSplash);
        Assert.True(manualStartResult.IsVisible);
        Assert.True(manualStartResult.ShowDetailedSplash);
    }

    [Fact]
    public void OvernightTimeRangeSpansMidnight()
    {
        Assert.True(IsVisible(new TimeOnly(22, 0)));
        Assert.True(IsVisible(new TimeOnly(2, 0)));
        Assert.False(IsVisible(new TimeOnly(12, 0)));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("not-a-time", "20:00")]
    [InlineData("08:00", "not-a-time")]
    [InlineData("8:00", "20:00")]
    public void InvalidTimeRangeInputsUseTheAlwaysVisibleFallback(string? startTime, string? endTime)
    {
        var service = new SplashPolicyService();
        var result = service.Evaluate(
            SplashMode.TimeRange,
            "default",
            true,
            startTime,
            endTime,
            false,
            new TimeOnly(12, 0));

        Assert.True(result.IsVisible);
        Assert.True(result.ShowDetailedSplash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown-style")]
    public void UnknownSplashStylesUseTheDefaultStyle(string? style)
    {
        var result = Evaluate(SplashMode.Always, style: style);

        Assert.Equal(SplashPolicyService.DefaultStyle, result.Style);
    }

    [Fact]
    public void SplashViewModelUsesDetailedPolicyToSelectStartupContent()
    {
        var localization = new LocalizationService();
        var detailed = new SplashViewModel(
            "1.0",
            localization,
            new SplashPolicyResult(true, "nina_iseri_1_2", true));
        var brief = new SplashViewModel(
            "1.0",
            localization,
            new SplashPolicyResult(true, SplashPolicyService.DefaultStyle, false));

        Assert.True(detailed.ShowDetailedSplash);
        Assert.Equal("nina_iseri_1_2", detailed.Style);
        Assert.Equal(localization["Splash.DetailText"], detailed.DetailText);
        Assert.Equal(localization["Splash.Style.NinaIseri"], detailed.StyleText);
        Assert.False(brief.ShowDetailedSplash);
        Assert.Equal(localization["Splash.BriefText"], brief.DetailText);

        localization.SetLanguage(AppLanguage.EnUs);
        Assert.Equal("Loading the shell, theme, and presentation tools", detailed.DetailText);
    }

    private static SplashPolicyResult Evaluate(
        SplashMode mode,
        bool isAutoStart = false,
        string? style = "default") =>
        new SplashPolicyService().Evaluate(
            mode,
            style,
            true,
            "08:00",
            "20:00",
            isAutoStart,
            new TimeOnly(12, 0));

    private static bool IsVisible(TimeOnly currentTime) =>
        new SplashPolicyService().Evaluate(
            SplashMode.TimeRange,
            "default",
            false,
            "22:00",
            "06:00",
            false,
            currentTime).IsVisible;
}
