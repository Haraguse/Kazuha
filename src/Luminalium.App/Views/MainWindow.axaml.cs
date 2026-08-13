using Avalonia.Controls;
using Luminalium.Core.Identity;

namespace Luminalium.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var result = VersionMetadataReader.Load(
            Path.Combine(AppContext.BaseDirectory, ProductIdentity.VersionMetadataFileName));
        VersionText.Text = result.IsSuccess
            ? $"{result.Metadata!.VersionName} | build {result.Metadata.Build}"
            : "Version metadata unavailable";
    }
}
