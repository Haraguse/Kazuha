using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Luminalium.Models;

public sealed partial class WorkspaceColorOption : ObservableObject
{
    public WorkspaceColorOption(string hex, string name)
    {
        Hex = hex;
        Name = name;
        Brush = new SolidColorBrush(Color.Parse(hex));
    }

    public string Hex { get; }
    public string Name { get; }
    public IBrush Brush { get; }

    [ObservableProperty]
    private bool isSelected;
}
