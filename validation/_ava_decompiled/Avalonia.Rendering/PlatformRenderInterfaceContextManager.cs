using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Reactive;

namespace Avalonia.Rendering;

internal class PlatformRenderInterfaceContextManager
{
	private readonly IPlatformGraphics? _graphics;

	private IPlatformRenderInterfaceContext? _backend;

	private OwnedDisposable<IPlatformGraphicsContext>? _gpuContext;

	private readonly IPlatformGraphicsReadyStateFeature? _readyStateFeature;

	public bool IsReady => _readyStateFeature?.IsReady ?? true;

	public IPlatformRenderInterfaceContext Value
	{
		get
		{
			EnsureValidBackendContext();
			return _backend;
		}
	}

	internal IPlatformGraphicsContext? GpuContext => _gpuContext?.Value;

	public event Action? ContextDisposed;

	public event Action<IPlatformRenderInterfaceContext>? ContextCreated;

	public PlatformRenderInterfaceContextManager(IPlatformGraphics? graphics)
	{
		_graphics = graphics;
		_readyStateFeature = (_graphics as IPlatformGraphicsWithFeatures)?.TryGetFeature<IPlatformGraphicsReadyStateFeature>();
	}

	public void EnsureValidBackendContext()
	{
		if (!IsReady)
		{
			throw new InvalidOperationException("Platform graphics isn't ready yet");
		}
		if (_backend != null)
		{
			ref OwnedDisposable<IPlatformGraphicsContext>? gpuContext = ref _gpuContext;
			if (!gpuContext.HasValue || !gpuContext.GetValueOrDefault().Value.IsLost)
			{
				return;
			}
		}
		_backend?.Dispose();
		_backend = null;
		if (_gpuContext.HasValue)
		{
			_gpuContext?.Dispose();
			_gpuContext = null;
			ContextDisposed?.Invoke();
		}
		if (_graphics != null)
		{
			IPlatformGraphicsReadyStateFeature? readyStateFeature = _readyStateFeature;
			if (readyStateFeature == null || readyStateFeature.UsesContexts)
			{
				if (_graphics.UsesSharedContext)
				{
					_gpuContext = new OwnedDisposable<IPlatformGraphicsContext>(_graphics.GetSharedContext(), owns: false);
				}
				else
				{
					_gpuContext = new OwnedDisposable<IPlatformGraphicsContext>(_graphics.CreateContext(), owns: true);
				}
			}
		}
		_backend = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>().CreateBackendContext(_gpuContext?.Value);
		ContextCreated?.Invoke(_backend);
	}

	public IDisposable EnsureCurrent()
	{
		EnsureValidBackendContext();
		if (_gpuContext.HasValue)
		{
			return _gpuContext.Value.Value.EnsureCurrent();
		}
		return Disposable.Empty;
	}

	public IRenderTarget CreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces)
	{
		EnsureValidBackendContext();
		return _backend.CreateRenderTarget(surfaces);
	}

	public bool IsReadyToCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces)
	{
		if (_backend == null)
		{
			return IsReady;
		}
		return _backend.IsReadyToCreateRenderTarget(surfaces);
	}

	public void Reset()
	{
		_backend?.Dispose();
		_backend = null;
	}
}
