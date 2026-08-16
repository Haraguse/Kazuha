using System;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Platform.Storage;

[NotClientImplementable]
public interface IStorageBookmarkItem : IStorageItem, IDisposable
{
	Task ReleaseBookmarkAsync();
}
