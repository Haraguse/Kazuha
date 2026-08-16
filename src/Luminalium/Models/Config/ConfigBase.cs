using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Luminalium.Models.Config;

public abstract class ConfigBase : ObservableObject
{
    [JsonIgnore] public abstract string ConfigFilePath { get; }
}