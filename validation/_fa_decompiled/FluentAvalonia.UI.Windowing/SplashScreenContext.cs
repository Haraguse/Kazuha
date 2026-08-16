using System.Threading;
using System.Threading.Tasks;

namespace FluentAvalonia.UI.Windowing;

internal class SplashScreenContext
{
	private FAAppSplashScreen _splashHost;

	private CancellationTokenSource _splashCTS;

	public IFAApplicationSplashScreen SplashScreen { get; }

	public bool HasShownSplashScreen { get; set; }

	public FAAppSplashScreen Host
	{
		get
		{
			return _splashHost;
		}
		set
		{
			_splashHost = value;
			_splashHost.SplashScreen = SplashScreen;
		}
	}

	public SplashScreenContext(IFAApplicationSplashScreen splash)
	{
		SplashScreen = splash;
	}

	public async Task RunJobs()
	{
		_splashCTS = new CancellationTokenSource();
		await SplashScreen.RunTasks(_splashCTS.Token);
		_splashCTS?.Dispose();
		_splashCTS = null;
	}

	public void TryCancel()
	{
		_splashCTS?.Cancel();
		_splashCTS?.Dispose();
		_splashCTS = null;
	}
}
