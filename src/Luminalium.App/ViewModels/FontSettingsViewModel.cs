using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.App.Services;
using Luminalium.Core.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Luminalium.App.ViewModels;

[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Settings labels are bound to the UI as instance members so compiled bindings resolve consistently.")]
public sealed partial class FontSettingsViewModel : ObservableObject
{
    private readonly SettingsCoordinator? _coordinator;
    private bool _initializing;

    private static readonly string[] WeightValues = ["Regular", "Medium", "Bold", "Light", "SemiBold"];
    private static readonly string[] WeightDisplays = ["常规", "中等", "粗体", "细体", "半粗"];

    private static readonly (string Code, string Display)[] LanguageOverrides =
    [
        ("zh-CN", "简体中文"),
        ("zh-TW", "繁體中文"),
        ("yue-HK", "粤语"),
        ("ja-JP", "日本語"),
        ("en-US", "English"),
        ("ug-CN", "维吾尔语"),
    ];

    public FontSettingsViewModel(LuminaliumConfig? config = null, SettingsCoordinator? coordinator = null)
    {
        _coordinator = coordinator;
        var fonts = config?.Fonts ?? new FontSettings();
        _initializing = true;
        customFont = fonts.CustomFont;
        selectedFamilyIndex = ResolveFamilyIndex(fonts.Family);
        selectedWeightIndex = Array.IndexOf(WeightValues, fonts.Weight);
        if (selectedWeightIndex < 0) selectedWeightIndex = 0;
        previewSample = fonts.PreviewSample;

        var configured = fonts.PerLanguageFonts
            .GroupBy(item => item.LanguageCode, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Family, StringComparer.Ordinal);
        var rows = LanguageOverrides
            .Select(language => new FontOverrideItemViewModel(
                language.Code,
                language.Display,
                ResolveFamilyIndex(configured.TryGetValue(language.Code, out var family) ? family : string.Empty),
                FontFamilies,
                () => PersistLanguageFonts()))
            .ToList();
        languageOverrideItems = rows;
        _initializing = false;
    }

    [ObservableProperty]
    private bool customFont;

    [ObservableProperty]
    private int selectedFamilyIndex;

    [ObservableProperty]
    private int selectedWeightIndex;

    [ObservableProperty]
    private string previewSample = string.Empty;

    [ObservableProperty]
    private List<FontOverrideItemViewModel> languageOverrideItems = [];

    public string PageTitle => "全局应用字体";

    public string CustomFontLabel => "启用自定义字体";

    public string CustomFontDescription => "开启后可按需指定软件界面使用的字体，并可为不同语言单独覆盖";

    public string FontSelectionSectionTitle => "字体选择";

    public string FamilyLabel => "字体";

    public string WeightLabel => "字重";

    public string LanguageOverrideSectionTitle => "按语言覆盖字体";

    public string LanguageOverrideDescription => "为特定语言单独指定字体，未指定的语言使用全局字体";

    public string PreviewSectionTitle => "字体预览";

    public string PreviewSampleLabel => "预览文本";

    public string PreviewHint => "此区域会以所选字体显示下面的文本";

    public IReadOnlyList<string> FontFamilies { get; } =
        new[] { "系统默认" }.Concat(FontFamilyResolver.DefaultFontFamilies).ToArray();

    public IReadOnlyList<string> Weights { get; } = WeightDisplays;

    public string CustomFontStatusText => CustomFont ? "开" : "关";

    public bool FontSelectionEnabled => CustomFont;

    public bool LanguageOverridesEnabled => CustomFont;

    partial void OnCustomFontChanged(bool value)
    {
        OnPropertyChanged(nameof(CustomFontStatusText));
        OnPropertyChanged(nameof(FontSelectionEnabled));
        OnPropertyChanged(nameof(LanguageOverridesEnabled));
        Persist(c => c.Fonts.CustomFont = value);
    }

    partial void OnSelectedFamilyIndexChanged(int value)
    {
        Persist(c => c.Fonts.Family = ResolveFamilyValue(value));
    }

    partial void OnSelectedWeightIndexChanged(int value)
    {
        if (value >= 0 && value < WeightValues.Length) Persist(c => c.Fonts.Weight = WeightValues[value]);
    }

    partial void OnPreviewSampleChanged(string value) => Persist(c => c.Fonts.PreviewSample = value);

    private void PersistLanguageFonts()
    {
        var langFonts = LanguageOverrideItems
            .Select(row => new LanguageFont
            {
                LanguageCode = row.LanguageCode,
                Family = ResolveFamilyValue(row.SelectedFamilyIndex),
            })
            .ToList();
        Persist(c => c.Fonts.PerLanguageFonts = langFonts);
    }

    private static int ResolveFamilyIndex(string family)
    {
        if (string.IsNullOrWhiteSpace(family))
        {
            return 0;
        }

        var index = Array.FindIndex(
            FontFamilyResolver.DefaultFontFamilies.ToArray(),
            known => known.Equals(family, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index + 1 : 0;
    }

    private static string ResolveFamilyValue(int index) =>
        index > 0 && index <= FontFamilyResolver.DefaultFontFamilies.Count
            ? FontFamilyResolver.DefaultFontFamilies[index - 1]
            : string.Empty;

    private void Persist(Action<LuminaliumConfig> update)
    {
        if (!_initializing && _coordinator is not null)
        {
            _ = _coordinator.UpdateAsync(update);
        }
    }
}

public sealed partial class FontOverrideItemViewModel : ObservableObject
{
    private readonly Action _persist;

    public FontOverrideItemViewModel(
        string languageCode,
        string languageDisplay,
        int familyIndex,
        IReadOnlyList<string> fontFamilies,
        Action persist)
    {
        LanguageCode = languageCode;
        LanguageDisplay = languageDisplay;
        FontFamilies = fontFamilies;
        selectedFamilyIndex = familyIndex;
        _persist = persist;
    }

    public string LanguageCode { get; }

    public string LanguageDisplay { get; }

    public IReadOnlyList<string> FontFamilies { get; }

    [ObservableProperty]
    private int selectedFamilyIndex;

    partial void OnSelectedFamilyIndexChanged(int value) => _persist();
}