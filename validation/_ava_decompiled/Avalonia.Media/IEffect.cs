using System.ComponentModel;
using Avalonia.Metadata;

namespace Avalonia.Media;

[TypeConverter(typeof(EffectConverter))]
[NotClientImplementable]
public interface IEffect
{
}
