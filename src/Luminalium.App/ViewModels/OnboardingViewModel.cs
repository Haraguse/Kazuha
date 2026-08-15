using CommunityToolkit.Mvvm.Input;
using Luminalium.Core.Configuration;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public sealed class OnboardingViewModel : ShellPageViewModel
{
    private readonly LuminaliumConfig _config;
    private readonly ConfigurationService? _configurationService;

    public OnboardingViewModel(
        LuminaliumConfig config,
        ConfigurationService? configurationService,
        ILocalizationService localization)
        : base("plugin:onboarding", "Plugin.onboarding.Name", localization)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _configurationService = configurationService;
        IsCompleted = config.General.OnboardingCompleted;
        CompleteCommand = new RelayCommand(Complete);
    }

    public bool IsCompleted { get; private set; }

    public string Description => Localization["Onboarding.Description"];

    public string CompletionText => IsCompleted
        ? Localization["Onboarding.Completed"]
        : Localization["Onboarding.Incomplete"];

    public string CompleteText => Localization["Onboarding.Complete"];

    public string CompleteHelpText => Localization["Onboarding.Complete.HelpText"];

    public IRelayCommand CompleteCommand { get; }

    private void Complete()
    {
        if (IsCompleted)
        {
            return;
        }

        _config.General.OnboardingCompleted = true;
        var result = _configurationService?.Save(_config);
        if (result is { IsSuccess: false })
        {
            _config.General.OnboardingCompleted = false;
            return;
        }

        IsCompleted = true;
        OnPropertyChanged(nameof(IsCompleted));
        OnPropertyChanged(nameof(CompletionText));
        CompleteCommand.NotifyCanExecuteChanged();
    }

    protected override void RefreshLocalizedText()
    {
        base.RefreshLocalizedText();
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(CompletionText));
        OnPropertyChanged(nameof(CompleteText));
        OnPropertyChanged(nameof(CompleteHelpText));
    }
}
