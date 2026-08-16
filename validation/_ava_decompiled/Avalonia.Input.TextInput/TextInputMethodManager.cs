using System;
using Avalonia.Interactivity;
using Avalonia.Reactive;

namespace Avalonia.Input.TextInput;

internal class TextInputMethodManager
{
	private ITextInputMethodImpl? _im;

	private IInputElement? _focusedElement;

	private Interactive? _visualRoot;

	private TextInputMethodClient? _client;

	private readonly TransformTrackingHelper _transformTracker = new TransformTrackingHelper(deferAfterRenderPass: true);

	private TextInputMethodClient? Client
	{
		get
		{
			return _client;
		}
		set
		{
			if (_client != value)
			{
				if (_client != null)
				{
					_client.CursorRectangleChanged -= OnCursorRectangleChanged;
					_client.TextViewVisualChanged -= OnTextViewVisualChanged;
					_client.ResetRequested -= OnResetRequested;
					_client = null;
					_im?.Reset();
				}
				_client = value;
				if (_client != null)
				{
					_client.CursorRectangleChanged += OnCursorRectangleChanged;
					_client.TextViewVisualChanged += OnTextViewVisualChanged;
					_client.ResetRequested += OnResetRequested;
					PopulateImWithInitialValues();
				}
				else
				{
					_im?.SetClient(null);
					_transformTracker.SetVisual(null);
				}
			}
		}
	}

	public TextInputMethodManager()
	{
		_transformTracker.MatrixChanged += UpdateCursorRect;
		InputMethod.IsInputMethodEnabledProperty.Changed.Subscribe(OnIsInputMethodEnabledChanged);
	}

	private void PopulateImWithInitialValues()
	{
		if (_focusedElement is StyledElement avaloniaObject)
		{
			_im?.SetOptions(TextInputOptions.FromStyledElement(avaloniaObject));
		}
		else
		{
			_im?.SetOptions(TextInputOptions.Default);
		}
		_transformTracker.SetVisual(_client?.TextViewVisual);
		_im?.SetClient(_client);
		UpdateCursorRect();
	}

	private void OnResetRequested(object? sender, EventArgs args)
	{
		if (_im != null && sender == _client)
		{
			_im.Reset();
			PopulateImWithInitialValues();
		}
	}

	private void OnIsInputMethodEnabledChanged(AvaloniaPropertyChangedEventArgs<bool> obj)
	{
		if (obj.Sender == _focusedElement)
		{
			TryFindAndApplyClient();
		}
	}

	private void OnTextViewVisualChanged(object? sender, EventArgs e)
	{
		_transformTracker.SetVisual(_client?.TextViewVisual);
	}

	private void UpdateCursorRect()
	{
		if (_im == null || _client == null || !(_focusedElement is Visual visual))
		{
			return;
		}
		Visual visualRoot = visual.VisualRoot;
		if (visualRoot != null)
		{
			Matrix? matrix = visual.TransformToVisual(visualRoot);
			if (!matrix.HasValue)
			{
				_im.SetCursorRect(default(Rect));
			}
			else
			{
				_im.SetCursorRect(_client.CursorRectangle.TransformToAABB(matrix.Value));
			}
		}
	}

	private void OnCursorRectangleChanged(object? sender, EventArgs e)
	{
		if (sender == _client)
		{
			UpdateCursorRect();
		}
	}

	public void SetFocusedElement(IInputElement? element)
	{
		if (_focusedElement != element)
		{
			if (_visualRoot != null)
			{
				InputMethod.RemoveTextInputMethodClientRequeryRequestedHandler(_visualRoot, TextInputMethodClientRequeryRequested);
			}
			_focusedElement = element;
			_visualRoot = (element as Visual)?.VisualRoot as Interactive;
			if (_visualRoot != null)
			{
				InputMethod.AddTextInputMethodClientRequeryRequestedHandler(_visualRoot, TextInputMethodClientRequeryRequested);
			}
			ITextInputMethodImpl textInputMethodImpl = ((element as Visual)?.GetInputRoot())?.InputMethod;
			if (_im != textInputMethodImpl)
			{
				_im?.SetClient(null);
			}
			_im = textInputMethodImpl;
			TryFindAndApplyClient();
		}
	}

	private void TextInputMethodClientRequeryRequested(object? sender, RoutedEventArgs e)
	{
		if (_im != null)
		{
			TryFindAndApplyClient();
		}
	}

	private void TryFindAndApplyClient()
	{
		if (!(_focusedElement is InputElement target) || _im == null || !InputMethod.GetIsInputMethodEnabled(target))
		{
			Client = null;
			return;
		}
		TextInputMethodClientRequestedEventArgs e = new TextInputMethodClientRequestedEventArgs
		{
			RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
		};
		_focusedElement.RaiseEvent(e);
		Client = e.Client;
	}
}
