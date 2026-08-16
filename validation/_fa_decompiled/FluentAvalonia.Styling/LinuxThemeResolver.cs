using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Styling;
using FluentAvalonia.UI.Media;

namespace FluentAvalonia.Styling;

internal static class LinuxThemeResolver
{
	private enum DesktopEnvironment
	{
		KDE,
		GNOME,
		LXQt,
		LXDE,
		Cinnamon,
		Other
	}

	private static string _config;

	private static readonly DesktopEnvironment _desktopEnvironment = GetDesktopEnvironment();

	public static Color2? TryLoadAccentColor()
	{
		if (_config == null)
		{
			TryLoadLinuxDesktopEnvironmentConfig();
		}
		Color2? result = null;
		if (_config != null)
		{
			switch (_desktopEnvironment)
			{
			case DesktopEnvironment.KDE:
			{
				Match match = new Regex("^AccentColor=(\\d+),(\\d+),(\\d+)$", RegexOptions.Multiline).Match(_config);
				if (!match.Success)
				{
					match = new Regex("^\\[Colors:Selection\\].*?BackgroundNormal=(\\d+),(\\d+),(\\d+)", RegexOptions.Multiline | RegexOptions.Singleline).Match(_config);
				}
				if (match.Success)
				{
					result = Color2.FromRGB(byte.Parse(match.Groups[1].Value), byte.Parse(match.Groups[2].Value), byte.Parse(match.Groups[3].Value));
				}
				break;
			}
			case DesktopEnvironment.LXQt:
			{
				Match match = new Regex("^highlight_color=#([\\da-f]{2})([\\da-f]{2})([\\da-f]{2})$", RegexOptions.Multiline).Match(_config);
				if (match.Success)
				{
					result = Color2.FromRGB(Convert.ToByte(match.Groups[1].Value, 16), Convert.ToByte(match.Groups[2].Value, 16), Convert.ToByte(match.Groups[3].Value, 16));
				}
				break;
			}
			case DesktopEnvironment.LXDE:
			{
				Match match = new Regex("selected_bg_color:#([\\da-f]{2}).{2}([\\da-f]{2}).{2}([\\da-f]{2}).{2}").Match(_config);
				if (match.Success)
				{
					result = Color2.FromRGB(Convert.ToByte(match.Groups[1].Value, 16), Convert.ToByte(match.Groups[2].Value, 16), Convert.ToByte(match.Groups[3].Value, 16));
				}
				break;
			}
			}
		}
		return result;
	}

	public static ThemeVariant TryLoadSystemTheme()
	{
		if (_config == null)
		{
			TryLoadLinuxDesktopEnvironmentConfig();
		}
		if (_config != null)
		{
			switch (_desktopEnvironment)
			{
			case DesktopEnvironment.KDE:
			{
				Match match2 = new Regex("^ColorScheme=(.*)$", RegexOptions.Multiline).Match(_config);
				if (match2.Success)
				{
					return GetThemeFromName(match2.Groups[1].Value);
				}
				break;
			}
			case DesktopEnvironment.LXDE:
			{
				Match match3 = new Regex("^sNet\\/ThemeName=(.*)$", RegexOptions.Multiline).Match(_config);
				if (match3.Success)
				{
					return GetThemeFromName(match3.Groups[1].Value);
				}
				break;
			}
			case DesktopEnvironment.LXQt:
			{
				Match match = new Regex("^theme=(.*)$", RegexOptions.Multiline).Match(_config);
				if (match.Success)
				{
					return GetThemeFromName(match.Groups[1].Value);
				}
				break;
			}
			}
		}
		else
		{
			switch (_desktopEnvironment)
			{
			case DesktopEnvironment.Cinnamon:
				return GetThemeFromName(ReadGsettingsKey("org.cinnamon.desktop.interface", "gtk-theme"));
			case DesktopEnvironment.Other:
			{
				string text = ReadGsettingsKey("org.gnome.desktop.interface", "color-scheme");
				if (!(text == "prefer-light"))
				{
					if (text == "prefer-dark")
					{
						return ThemeVariant.Dark;
					}
					return GetThemeFromName(ReadGsettingsKey("org.gnome.desktop.interface", "gtk-theme"));
				}
				return ThemeVariant.Light;
			}
			}
		}
		return ThemeVariant.Light;
	}

	private static void TryLoadLinuxDesktopEnvironmentConfig()
	{
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		string text = _desktopEnvironment switch
		{
			DesktopEnvironment.KDE => Path.Combine(folderPath, "kdeglobals"), 
			DesktopEnvironment.LXDE => Path.Combine(folderPath, "lxsession/LXDE/desktop.conf"), 
			DesktopEnvironment.LXQt => Path.Combine(folderPath, "lxqt/lxqt.conf"), 
			_ => null, 
		};
		if (text != null)
		{
			try
			{
				_config = File.ReadAllText(text);
			}
			catch
			{
			}
		}
	}

	private static ThemeVariant GetThemeFromName(string name)
	{
		if (name == null || name.IndexOf("dark", StringComparison.OrdinalIgnoreCase) == -1)
		{
			return ThemeVariant.Light;
		}
		return ThemeVariant.Dark;
	}

	private static string ReadGsettingsKey(string schema, string key)
	{
		Process process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				WindowStyle = ProcessWindowStyle.Hidden,
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				FileName = "gsettings",
				Arguments = "get " + schema + " " + key
			}
		};
		process.Start();
		process.WaitForExit();
		if (process.ExitCode == 0)
		{
			string text = process.StandardOutput.ReadToEnd().Trim().Replace("'", string.Empty);
			if (!text.Contains("No such"))
			{
				return text;
			}
			return null;
		}
		return null;
	}

	private static DesktopEnvironment GetDesktopEnvironment()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP");
		if (environmentVariable == null)
		{
			return DesktopEnvironment.Other;
		}
		if (environmentVariable.Contains("KDE"))
		{
			return DesktopEnvironment.KDE;
		}
		if (environmentVariable.Contains("GNOME"))
		{
			return DesktopEnvironment.GNOME;
		}
		if (environmentVariable.Contains("LXDE"))
		{
			return DesktopEnvironment.LXDE;
		}
		if (environmentVariable.Contains("Cinnamon"))
		{
			return DesktopEnvironment.Cinnamon;
		}
		if (!environmentVariable.Contains("LXQt"))
		{
			return DesktopEnvironment.Other;
		}
		return DesktopEnvironment.LXQt;
	}
}
