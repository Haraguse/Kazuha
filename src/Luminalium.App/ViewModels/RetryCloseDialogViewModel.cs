using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Luminalium.App.ViewModels;

public sealed partial class RetryCloseDialogViewModel : ObservableObject
{
    private readonly Func<Task<bool>> _retryAsync;
    private readonly Action _close;
    private int _closeCalled;

    public RetryCloseDialogViewModel(string message, Func<Task<bool>> retryAsync, Action close)
    {
        Message = message;
        _retryAsync = retryAsync;
        _close = close;
        RetryCommand = new AsyncRelayCommand(RetryAsync);
        CloseCommand = new RelayCommand(Close);
    }

    public string Message { get; }

    [ObservableProperty]
    private string retryError = string.Empty;

    partial void OnRetryErrorChanged(string value) => OnPropertyChanged(nameof(HasRetryError));

    public bool HasRetryError => !string.IsNullOrWhiteSpace(RetryError);

    public IAsyncRelayCommand RetryCommand { get; }

    public IRelayCommand CloseCommand { get; }

    public event EventHandler<bool>? RetryCompleted;

    public async Task<bool> RetryAsync()
    {
        RetryError = string.Empty;
        var succeeded = await _retryAsync().ConfigureAwait(true);
        if (!succeeded)
        {
            RetryError = "Retry failed.";
        }

        RetryCompleted?.Invoke(this, succeeded);
        return succeeded;
    }

    private void Close()
    {
        if (Interlocked.Exchange(ref _closeCalled, 1) == 0)
        {
            _close();
        }
    }
}
