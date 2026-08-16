using System;
using Avalonia.Animation;
using Avalonia.Animation.Animators;
using Avalonia.Reactive;
using Avalonia.Rendering.Composition.Expressions;

namespace Avalonia.Media;

public class Effect : Animatable, IAffectsRender
{
	/// <inheritdoc />
	public event EventHandler? Invalidated;

	/// <summary>
	/// Marks a property as affecting the brush's visual representation.
	/// </summary>
	/// <param name="properties">The properties.</param>
	/// <remarks>
	/// After a call to this method in a brush's static constructor, any change to the
	/// property will cause the <see cref="E:Avalonia.Media.Effect.Invalidated" /> event to be raised on the brush.
	/// </remarks>
	protected static void AffectsRender<T>(params AvaloniaProperty[] properties) where T : Effect
	{
		AnonymousObserver<AvaloniaPropertyChangedEventArgs> observer = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(delegate(AvaloniaPropertyChangedEventArgs e)
		{
			(e.Sender as T)?.RaiseInvalidated(EventArgs.Empty);
		});
		for (int num = 0; num < properties.Length; num++)
		{
			properties[num].Changed.Subscribe(observer);
		}
	}

	/// <summary>
	/// Raises the <see cref="E:Avalonia.Media.Effect.Invalidated" /> event.
	/// </summary>
	/// <param name="e">The event args.</param>
	protected void RaiseInvalidated(EventArgs e)
	{
		Invalidated?.Invoke(this, e);
	}

	private static Exception ParseError(string s)
	{
		throw new ArgumentException("Unable to parse effect: " + s);
	}

	public static IEffect Parse(string s)
	{
		ReadOnlySpan<char> s2 = s.AsSpan();
		TokenParser tokenParser = new TokenParser(s2);
		if (tokenParser.TryConsume("blur"))
		{
			if (!tokenParser.TryConsume('(') || !tokenParser.TryParseDouble(out var res) || !tokenParser.TryConsume(')') || !tokenParser.IsEofWithWhitespace())
			{
				throw ParseError(s);
			}
			return new ImmutableBlurEffect(res);
		}
		if (tokenParser.TryConsume("drop-shadow"))
		{
			if (!tokenParser.TryConsume('(') || !tokenParser.TryParseDouble(out var res2) || !tokenParser.TryParseDouble(out var res3))
			{
				throw ParseError(s);
			}
			double res4 = 0.0;
			Color color = Colors.Black;
			if (!tokenParser.TryConsume(')'))
			{
				if (!tokenParser.TryParseDouble(out res4) || res4 < 0.0)
				{
					throw ParseError(s);
				}
				if (!tokenParser.TryConsume(')'))
				{
					int num = s.LastIndexOf(")", StringComparison.Ordinal);
					if (num == -1)
					{
						throw ParseError(s);
					}
					if (!new TokenParser(s2.Slice(num + 1)).IsEofWithWhitespace())
					{
						throw ParseError(s);
					}
					if (!Color.TryParse(s2.Slice(tokenParser.Position, num - tokenParser.Position).TrimEnd(), out color))
					{
						throw ParseError(s);
					}
					return new ImmutableDropShadowEffect(res2, res3, res4, color, 1.0);
				}
			}
			if (!tokenParser.IsEofWithWhitespace())
			{
				throw ParseError(s);
			}
			return new ImmutableDropShadowEffect(res2, res3, res4, color, 1.0);
		}
		throw ParseError(s);
	}

	static Effect()
	{
		EffectAnimator.EnsureRegistered();
	}

	internal Effect()
	{
	}
}
