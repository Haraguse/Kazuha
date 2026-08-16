using System.Text.Json;
using Luminalium.Core.Configuration;
using Xunit;

namespace Luminalium.Tests;

public sealed class SettingsBatchTwoTests
{
    [Fact]
    public void NewSectionsHaveExpectedDefaults()
    {
        var config = LuminaliumConfig.CreateDefault();

        Assert.Equal(BoardInBoardPosition.BottomRight, config.BoardInBoard.WindowPosition);
        Assert.Equal("#FFFFFF", config.BoardInBoard.BackgroundColor);
        Assert.Equal(BoardEraserMode.Stroke, config.BoardInBoard.EraserMode);
        Assert.Equal(BoardPenEffect.Limited, config.BoardInBoard.PenEffect);
        Assert.True(config.BoardInBoard.WindowEnterAnimation);

        Assert.Equal(TimerFullscreenBehavior.Normal, config.Timer.FullscreenBehavior);
        Assert.True(config.Timer.EnableSoundEffects);
        Assert.Empty(config.Timer.QuickAddPresets);

        Assert.False(config.Fonts.CustomFont);
        Assert.Equal(string.Empty, config.Fonts.Family);
        Assert.Equal("Regular", config.Fonts.Weight);
        Assert.Empty(config.Fonts.PerLanguageFonts);
        Assert.Equal(string.Empty, config.Fonts.PreviewSample);
    }

    [Fact]
    public void BoardInBoardRoundTripsThroughJson()
    {
        var config = LuminaliumConfig.CreateDefault();
        config.BoardInBoard.WindowPosition = BoardInBoardPosition.TopLeft;
        config.BoardInBoard.BackgroundColor = "#1E1E1E";
        config.BoardInBoard.EraserMode = BoardEraserMode.Point;
        config.BoardInBoard.PenEffect = BoardPenEffect.Full;
        config.BoardInBoard.WindowEnterAnimation = false;

        var json = JsonSerializer.Serialize(config, ConfigurationJson.Options);
        var reloaded = JsonSerializer.Deserialize<LuminaliumConfig>(json, ConfigurationJson.Options);

        Assert.NotNull(reloaded);
        Assert.Equal(BoardInBoardPosition.TopLeft, reloaded.BoardInBoard.WindowPosition);
        Assert.Equal("#1E1E1E", reloaded.BoardInBoard.BackgroundColor);
        Assert.Equal(BoardEraserMode.Point, reloaded.BoardInBoard.EraserMode);
        Assert.Equal(BoardPenEffect.Full, reloaded.BoardInBoard.PenEffect);
        Assert.False(reloaded.BoardInBoard.WindowEnterAnimation);
        Assert.Contains("\"WindowPosition\": \"TopLeft\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void TimerAndFontsRoundTripThroughJson()
    {
        var config = LuminaliumConfig.CreateDefault();
        config.Timer.FullscreenBehavior = TimerFullscreenBehavior.Fullscreen;
        config.Timer.EnableSoundEffects = false;
        config.Timer.QuickAddPresets = [1, 3, 5];
        config.Fonts.CustomFont = true;
        config.Fonts.Family = "Segoe UI";
        config.Fonts.Weight = "SemiBold";
        config.Fonts.PreviewSample = "样例";
        config.Fonts.PerLanguageFonts =
        [
            new LanguageFont { LanguageCode = "zh-CN", Family = "Microsoft YaHei UI" },
            new LanguageFont { LanguageCode = "ja-JP", Family = "Yu Gothic UI" },
        ];

        var json = JsonSerializer.Serialize(config, ConfigurationJson.Options);
        var reloaded = JsonSerializer.Deserialize<LuminaliumConfig>(json, ConfigurationJson.Options);

        Assert.NotNull(reloaded);
        Assert.Equal(TimerFullscreenBehavior.Fullscreen, reloaded.Timer.FullscreenBehavior);
        Assert.False(reloaded.Timer.EnableSoundEffects);
        Assert.Equal([1, 3, 5], reloaded.Timer.QuickAddPresets);
        Assert.True(reloaded.Fonts.CustomFont);
        Assert.Equal("Segoe UI", reloaded.Fonts.Family);
        Assert.Equal("SemiBold", reloaded.Fonts.Weight);
        Assert.Equal("样例", reloaded.Fonts.PreviewSample);
        Assert.Equal(2, reloaded.Fonts.PerLanguageFonts.Count);
        Assert.Equal("Microsoft YaHei UI", reloaded.Fonts.PerLanguageFonts.Single(f => f.LanguageCode == "zh-CN").Family);
        Assert.Equal("Yu Gothic UI", reloaded.Fonts.PerLanguageFonts.Single(f => f.LanguageCode == "ja-JP").Family);
        Assert.Contains("\"FullscreenBehavior\": \"Fullscreen\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void QuickAddPresetsRejectsMoreThanFourPresets()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"Luminalium-BatchTwo-{Guid.NewGuid():N}");
        try
        {
            var service = new ConfigurationService(directory);
            var config = LuminaliumConfig.CreateDefault();
            config.Timer.QuickAddPresets = [1, 2, 3, 4, 5];

            var result = service.Save(config);

            Assert.False(result.IsSuccess);
            Assert.Equal(nameof(TimerSettings.QuickAddPresets), result.Error!.Field);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public void QuickAddPresetsRejectsSubMinutePreset()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"Luminalium-BatchTwo-{Guid.NewGuid():N}");
        try
        {
            var service = new ConfigurationService(directory);
            var config = LuminaliumConfig.CreateDefault();
            config.Timer.QuickAddPresets = [0];

            var result = service.Save(config);

            Assert.False(result.IsSuccess);
            Assert.Equal(nameof(TimerSettings.QuickAddPresets), result.Error!.Field);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public void QuickAddPresetsAcceptsUpToFourValidPresets()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"Luminalium-BatchTwo-{Guid.NewGuid():N}");
        try
        {
            var service = new ConfigurationService(directory);
            var config = LuminaliumConfig.CreateDefault();
            config.Timer.QuickAddPresets = [1, 2, 3, 4];

            var result = service.Save(config);

            Assert.True(result.IsSuccess);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}