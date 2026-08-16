using System;
using Avalonia.Metadata;

namespace Avalonia.Platform.Storage;

[NotClientImplementable]
public interface IStorageBookmarkFolder : IStorageFolder, IStorageItem, IDisposable, IStorageBookmarkItem
{
}
