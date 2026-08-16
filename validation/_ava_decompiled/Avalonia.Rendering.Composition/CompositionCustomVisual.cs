using System.Collections.Generic;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Threading;

namespace Avalonia.Rendering.Composition;

public sealed class CompositionCustomVisual : CompositionContainerVisual
{
	private static readonly ThreadSafeObjectPool<List<object>> s_messageListPool = new ThreadSafeObjectPool<List<object>>();

	private List<object>? _messages;

	internal CompositionCustomVisual(Compositor compositor, CompositionCustomVisualHandler handler)
		: base(compositor, new ServerCompositionCustomVisual(compositor.Server, handler))
	{
	}

	public void SendHandlerMessage(object message)
	{
		if (_messages == null)
		{
			_messages = s_messageListPool.Get();
			base.Compositor.RequestCompositionUpdate(OnCompositionUpdate);
		}
		_messages.Add(message);
	}

	private void OnCompositionUpdate()
	{
		if (_messages != null)
		{
			List<object> messages = _messages;
			_messages = null;
			base.Compositor.PostServerJob(delegate
			{
				((ServerCompositionCustomVisual)base.Server).DispatchMessages(messages);
				messages.Clear();
				s_messageListPool.ReturnAndSetNull(ref messages);
			});
		}
	}
}
