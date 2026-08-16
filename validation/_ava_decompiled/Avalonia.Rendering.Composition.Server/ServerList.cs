using System;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// A server-side list container capable of receiving changes from the UI thread
/// Right now it's quite dumb since it always receives the full list
/// </summary>
internal class ServerList<T> : ServerObject where T : ServerObject
{
	public List<T> List { get; } = new List<T>();

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		if (reader.Read<byte>() == 1)
		{
			List.Clear();
			int num = reader.Read<int>();
			for (int i = 0; i < num; i++)
			{
				List.Add(reader.ReadObject<T>());
			}
		}
		base.DeserializeChangesCore(reader, committedAt);
	}

	public List<T>.Enumerator GetEnumerator()
	{
		return List.GetEnumerator();
	}

	public ServerList(ServerCompositor compositor)
		: base(compositor)
	{
	}
}
