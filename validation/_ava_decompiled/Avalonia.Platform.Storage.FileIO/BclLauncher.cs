using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Logging;

namespace Avalonia.Platform.Storage.FileIO;

internal class BclLauncher : ILauncher
{
	public virtual Task<bool> LaunchUriAsync(Uri uri)
	{
		if ((object)uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		if (uri.IsAbsoluteUri)
		{
			return Task.FromResult(Exec(uri.AbsoluteUri));
		}
		return Task.FromResult(result: false);
	}

	/// <summary>
	/// This Process based implementation doesn't handle the case, when there is no app to handle link.
	/// It will still return true in this case.
	/// </summary>
	public virtual Task<bool> LaunchFileAsync(IStorageItem storageItem)
	{
		if (storageItem == null)
		{
			throw new ArgumentNullException("storageItem");
		}
		string text = storageItem.TryGetLocalPath();
		if (text != null && CanOpenFileOrDirectory(text))
		{
			return Task.FromResult(Exec(text));
		}
		return Task.FromResult(result: false);
	}

	protected virtual bool CanOpenFileOrDirectory(string localPath)
	{
		return true;
	}

	private static bool Exec(string urlOrFile)
	{
		try
		{
			if (OperatingSystem.IsLinux())
			{
				string text = EscapeForShell(urlOrFile);
				ShellExecRaw("xdg-open \\\"" + text + "\\\"", waitForExit: false);
				return true;
			}
			if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo
				{
					FileName = (OperatingSystem.IsWindows() ? urlOrFile : "open"),
					CreateNoWindow = true,
					UseShellExecute = OperatingSystem.IsWindows()
				};
				if (OperatingSystem.IsMacOS())
				{
					processStartInfo.ArgumentList.Add(urlOrFile);
				}
				using (Process.Start(processStartInfo))
				{
					return true;
				}
			}
			return false;
		}
		catch (Exception propertyValue)
		{
			Logger.TryGet(LogEventLevel.Error, "BclLauncher")?.Log(null, "Exception during BclLauncher.Exec: {Error}", propertyValue);
			return false;
		}
	}

	private static string EscapeForShell(string input)
	{
		return Regex.Replace(input, "(?=[`~!#&*()|;'<>])", "\\").Replace("\"", "\\\\\\\"");
	}

	private static void ShellExecRaw(string cmd, bool waitForExit = true)
	{
		using Process process = Process.Start(new ProcessStartInfo
		{
			FileName = "/bin/sh",
			Arguments = "-c \"" + cmd + "\"",
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WindowStyle = ProcessWindowStyle.Hidden
		});
		if (waitForExit)
		{
			process?.WaitForExit();
		}
	}
}
