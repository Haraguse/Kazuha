using System;
using System.Collections.Generic;
using Avalonia.Animation.Easings;
using Avalonia.Rendering.Composition.Expressions;

namespace Avalonia.Rendering.Composition.Animations;

/// <summary>
/// Collection of composition animation key frames
/// </summary>
/// <typeparam name="T"></typeparam>
internal class KeyFrames<T> : List<KeyFrame<T>>, IKeyFrames
{
	private void Validate(float key)
	{
		if (key < 0f || key > 1f)
		{
			throw new ArgumentException("Key frame key");
		}
		if (base.Count > 0 && base[base.Count - 1].NormalizedProgressKey > key)
		{
			throw new ArgumentException("Key frame key " + key + " is less than the previous one");
		}
	}

	public void InsertExpressionKeyFrame(float normalizedProgressKey, string value, IEasing easingFunction)
	{
		Validate(normalizedProgressKey);
		Add(new KeyFrame<T>
		{
			NormalizedProgressKey = normalizedProgressKey,
			Expression = Expression.Parse(value),
			EasingFunction = easingFunction
		});
	}

	public void Insert(float normalizedProgressKey, T value, IEasing easingFunction)
	{
		Validate(normalizedProgressKey);
		Add(new KeyFrame<T>
		{
			NormalizedProgressKey = normalizedProgressKey,
			Value = value,
			EasingFunction = easingFunction
		});
	}

	public ServerKeyFrame<T>[] Snapshot()
	{
		ServerKeyFrame<T>[] array = new ServerKeyFrame<T>[base.Count];
		for (int i = 0; i < base.Count; i++)
		{
			KeyFrame<T> keyFrame = base[i];
			array[i] = new ServerKeyFrame<T>
			{
				Expression = keyFrame.Expression,
				Value = keyFrame.Value,
				EasingFunction = keyFrame.EasingFunction,
				Key = keyFrame.NormalizedProgressKey
			};
		}
		return array;
	}
}
