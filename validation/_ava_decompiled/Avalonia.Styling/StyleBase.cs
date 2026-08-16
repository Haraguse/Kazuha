using System;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.PropertyStore;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling;

/// <summary>
/// Base class for <see cref="T:Avalonia.Styling.Style" /> and <see cref="T:Avalonia.Styling.ControlTheme" />.
/// </summary>
public abstract class StyleBase : AvaloniaObject, IStyle, IResourceNode, IResourceProvider, IAddChild
{
	private IResourceHost? _owner;

	private StyleChildren? _children;

	private IResourceDictionary? _resources;

	private List<SetterBase>? _setters;

	private List<IAnimation>? _animations;

	private StyleInstance? _sharedInstance;

	public IList<IStyle> Children => _children ?? (_children = new StyleChildren(this));

	public IResourceHost? Owner
	{
		get
		{
			return _owner;
		}
		private set
		{
			if (_owner != value)
			{
				_owner = value;
				OwnerChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public IStyle? Parent { get; private set; }

	public IResourceDictionary Resources
	{
		get
		{
			return _resources ?? (Resources = new ResourceDictionary());
		}
		set
		{
			value = value ?? throw new ArgumentNullException("value");
			bool flag = _resources?.HasResources ?? false;
			_resources = value;
			if (Owner != null)
			{
				_resources.AddOwner(Owner);
				if (flag || _resources.HasResources)
				{
					Owner.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Create());
				}
			}
		}
	}

	public IList<SetterBase> Setters => _setters ?? (_setters = new List<SetterBase>());

	public IList<IAnimation> Animations => _animations ?? (_animations = new List<IAnimation>());

	bool IResourceNode.HasResources
	{
		get
		{
			IResourceDictionary? resources = _resources;
			if (resources == null)
			{
				return false;
			}
			return resources.Count > 0;
		}
	}

	IReadOnlyList<IStyle> IStyle.Children
	{
		get
		{
			IReadOnlyList<IStyle> children = _children;
			return children ?? Array.Empty<IStyle>();
		}
	}

	internal bool HasChildren
	{
		get
		{
			StyleChildren? children = _children;
			if (children == null)
			{
				return false;
			}
			return children.Count > 0;
		}
	}

	internal bool HasSettersOrAnimations
	{
		get
		{
			List<SetterBase>? setters = _setters;
			if (setters == null || setters.Count <= 0)
			{
				List<IAnimation>? animations = _animations;
				if (animations == null)
				{
					return false;
				}
				return animations.Count > 0;
			}
			return true;
		}
	}

	public event EventHandler? OwnerChanged;

	public void Add(SetterBase setter)
	{
		Setters.Add(setter);
	}

	public void Add(IStyle style)
	{
		Children.Add(style);
	}

	void IAddChild.AddChild(object child)
	{
		if (!(child is SetterBase item))
		{
			if (!(child is IStyle item2))
			{
				throw new InvalidOperationException($"Cannot add {child.GetType()} to a style.");
			}
			Children.Add(item2);
		}
		else
		{
			Setters.Add(item);
		}
	}

	public bool TryGetResource(object key, ThemeVariant? themeVariant, out object? result)
	{
		if (_resources != null && _resources.TryGetResource(key, themeVariant, out result))
		{
			return true;
		}
		if (_children != null)
		{
			for (int i = 0; i < _children.Count; i++)
			{
				if (_children[i].TryGetResource(key, themeVariant, out result))
				{
					return true;
				}
			}
		}
		result = null;
		return false;
	}

	internal ValueFrame Attach(StyledElement target, IStyleActivator? activator, FrameType type, bool canShareInstance)
	{
		if (target == null)
		{
			throw new InvalidOperationException("Styles can only be applied to AvaloniaObjects.");
		}
		StyleInstance styleInstance;
		if ((_sharedInstance != null) & canShareInstance)
		{
			styleInstance = _sharedInstance;
		}
		else
		{
			canShareInstance &= activator == null;
			styleInstance = new StyleInstance(this, activator, type);
			if (_setters != null)
			{
				foreach (SetterBase setter in _setters)
				{
					ISetterInstance setterInstance = setter.Instance(styleInstance, target);
					styleInstance.Add(setterInstance);
					canShareInstance &= setterInstance == setter;
				}
			}
			if (_animations != null)
			{
				styleInstance.Add(_animations);
			}
			if (canShareInstance)
			{
				styleInstance.MakeShared();
				_sharedInstance = styleInstance;
			}
		}
		target.GetValueStore().AddFrame(styleInstance);
		styleInstance.ApplyAnimations(target);
		return styleInstance;
	}

	internal virtual void SetParent(StyleBase? parent)
	{
		Parent = parent;
	}

	void IResourceProvider.AddOwner(IResourceHost owner)
	{
		owner = owner ?? throw new ArgumentNullException("owner");
		if (Owner != null)
		{
			throw new InvalidOperationException("The Style already has a parent.");
		}
		Owner = owner;
		_resources?.AddOwner(owner);
	}

	void IResourceProvider.RemoveOwner(IResourceHost owner)
	{
		owner = owner ?? throw new ArgumentNullException("owner");
		if (Owner == owner)
		{
			Owner = null;
			_resources?.RemoveOwner(owner);
		}
	}
}
