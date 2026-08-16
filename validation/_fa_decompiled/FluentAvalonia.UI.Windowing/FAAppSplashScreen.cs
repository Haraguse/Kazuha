using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;

namespace FluentAvalonia.UI.Windowing;

public class FAAppSplashScreen : TemplatedControl
{
	public IFAApplicationSplashScreen SplashScreen { get; set; }

	/// <inheritdoc />
	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		((TemplatedControl)this).OnApplyTemplate(e);
		if (SplashScreen != null)
		{
			if (SplashScreen.SplashScreenContent != null)
			{
				NameScopeExtensions.Find<ContentPresenter>(e.NameScope, "ContentHost").Content = SplashScreen.SplashScreenContent;
			}
			else if (SplashScreen.AppIcon != null)
			{
				NameScopeExtensions.Find<Image>(e.NameScope, "AppImageHost").Source = SplashScreen.AppIcon;
			}
			else if (!string.IsNullOrEmpty(SplashScreen.AppName))
			{
				NameScopeExtensions.Find<TextBlock>(e.NameScope, "AppNameText").Text = SplashScreen.AppName;
			}
		}
	}
}
