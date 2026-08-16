using System;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Avalonia.Media;

public sealed class TransformGroup : Transform
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TransformGroup.Children" /> property.
	/// </summary>
	public static readonly StyledProperty<Transforms> ChildrenProperty = AvaloniaProperty.Register<TransformGroup, Transforms>("Children");

	private IDisposable? _childrenNotificationSubscription;

	private readonly EventHandler _childTransformChangedHandler;

	private Matrix? _lastMatrix;

	/// <summary>
	/// Gets or sets the children.
	/// </summary>
	/// <value>
	/// The children.
	/// </value>
	[Content]
	public Transforms Children
	{
		get
		{
			return GetValue(ChildrenProperty);
		}
		set
		{
			SetValue(ChildrenProperty, value);
		}
	}

	/// <summary>
	/// Gets the transform's <see cref="T:Avalonia.Matrix" />.
	/// </summary>
	public override Matrix Value
	{
		get
		{
			Matrix? lastMatrix = _lastMatrix;
			if (!lastMatrix.HasValue)
			{
				Matrix identity = Matrix.Identity;
				foreach (Transform child in Children)
				{
					identity *= child.Value;
				}
				_lastMatrix = identity;
			}
			return _lastMatrix.Value;
		}
	}

	public TransformGroup()
	{
		_childTransformChangedHandler = delegate
		{
			OnTransformInvalidated();
		};
		Children = new Transforms();
	}

	private void OnTransformInvalidated()
	{
		_lastMatrix = null;
		RaiseChanged();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (!(change.Property == ChildrenProperty))
		{
			return;
		}
		_childrenNotificationSubscription?.Dispose();
		if (change.OldValue is Transforms transforms)
		{
			foreach (Transform item in transforms)
			{
				item.Changed -= _childTransformChangedHandler;
			}
		}
		if (change.NewValue is Transforms transforms2)
		{
			transforms2.ResetBehavior = ResetBehavior.Remove;
			_childrenNotificationSubscription = transforms2.ForEachItem(delegate(Transform tr)
			{
				tr.Changed += _childTransformChangedHandler;
				OnTransformInvalidated();
			}, delegate(Transform tr)
			{
				tr.Changed -= _childTransformChangedHandler;
				OnTransformInvalidated();
			}, delegate
			{
			});
		}
		OnTransformInvalidated();
	}
}
