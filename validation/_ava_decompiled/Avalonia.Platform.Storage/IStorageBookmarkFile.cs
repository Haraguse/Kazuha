using System;
using Avalonia.Metadata;

namespace Avalonia.Platform.Storage;

[NotClientImplementable]
public interface IStorageBookmarkFile : IStorageFile, IStorageItem, IDisposable, IStorageBookmarkItem
{
}
