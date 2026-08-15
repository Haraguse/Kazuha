using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.App.Services;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public sealed partial class LogsViewModel : ShellPageViewModel
{
    private readonly LocalLogService _logService;
    private IReadOnlyList<LogEntry> _allEntries = [];

    [ObservableProperty]
    private LogSeverity? selectedSeverity;

    [ObservableProperty]
    private IReadOnlyList<LogEntry> entries = [];

    [ObservableProperty]
    private string statusText = string.Empty;

    public LogsViewModel(LocalLogService logService, ILocalizationService localization)
        : base("plugin:logs", "Plugin.logs.Name", localization)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        SeverityOptions = [null, .. Enum.GetValues<LogSeverity>().Cast<LogSeverity?>()];
        RefreshCommand = new RelayCommand(Refresh);
        ExportCommand = new RelayCommand<string?>(Export);
        Refresh();
    }

    public IReadOnlyList<LogSeverity?> SeverityOptions { get; }

    public IRelayCommand RefreshCommand { get; }

    public IRelayCommand<string?> ExportCommand { get; }

    public string Description => Localization["Logs.Description"];

    public string FilterLabel => Localization["Logs.Filter.Label"];

    public string AllSeveritiesText => Localization["Logs.Filter.All"];

    public string RefreshText => Localization["Logs.Refresh"];

    public string ExportText => Localization["Logs.Export"];

    public string ExportHelpText => Localization["Logs.Export.HelpText"];

    partial void OnSelectedSeverityChanged(LogSeverity? value) => ApplyFilter();

    private void Refresh()
    {
        _allEntries = _logService.Read();
        ApplyFilter();
        StatusText = Entries.Count == 0 ? Localization["Logs.Empty"] : string.Empty;
    }

    private void ApplyFilter() => Entries = _logService.Filter(_allEntries, SelectedSeverity);

    private void Export(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        StatusText = _logService.TryExport(Entries, path)
            ? Localization["Logs.Export.Success"]
            : Localization["Logs.Export.Failed"];
    }

    protected override void RefreshLocalizedText()
    {
        base.RefreshLocalizedText();
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(FilterLabel));
        OnPropertyChanged(nameof(AllSeveritiesText));
        OnPropertyChanged(nameof(RefreshText));
        OnPropertyChanged(nameof(ExportText));
        OnPropertyChanged(nameof(ExportHelpText));
        if (Entries.Count == 0)
        {
            StatusText = Localization["Logs.Empty"];
        }
    }
}
