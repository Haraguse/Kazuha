using System;
using Avalonia.Styling;

namespace Avalonia.Controls;

/// <summary>
/// Base implementation for IResourceProvider interface.
/// Includes Owner property management.
/// </summary>
public abstract class ResourceProvider : AvaloniaObject, IResourceProvider, IResourceNode
{
	private IResourceHost? _owner;

	/// <inheritdoc />
	public abstract bool HasResources { get; }

	/// <inheritdoc />
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

	/// <inheritdoc />
	public event EventHandler? OwnerChanged;

	public ResourceProvider()
	{
	}

	public ResourceProvider(IResourceHost owner)
	{
		_owner = owner;
	}

	/// <inheritdoc />
	public abstract bool TryGetResource(object key, ThemeVariant? theme, out object? value);

	protected void RaiseResourcesChanged()
	{
		Owner?.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Create());
	}

	/// <summary>
	/// Handles when owner was added.
	/// Base method implementation raises <see cref="M:Avalonia.Controls.IResourceHost.NotifyHostedResourcesChanged(Avalonia.Controls.ResourcesChangedEventArgs)" />, if this provider has any resources.
	/// </summary>
	/// <param name="owner">New owner.</param>
	protected virtual void OnAddOwner(IResourceHost owner)
	{
		if (HasResources)
		{
			owner.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Create());
		}
	}

	/// <summary>
	/// Handles when owner was removed.
	/// Base method implementation raises <see cref="M:Avalonia.Controls.IResourceHost.NotifyHostedResourcesChanged(Avalonia.Controls.ResourcesChangedEventArgs)" />, if this provider has any resources.
	/// </summary>
	/// <param name="owner">Old owner.</param>
	protected virtual void OnRemoveOwner(IResourceHost owner)
	{
		if (HasResources)
		{
			owner.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Create());
		}
	}

	void IResourceProvider.AddOwner(IResourceHost owner)
	{
		owner = owner ?? throw new ArgumentNullException("owner");
		if (Owner != null)
		{
			throw new InvalidOperationException("The ResourceDictionary already has a parent.");
		}
		Owner = owner;
		OnAddOwner(owner);
	}

	void IResourceProvider.RemoveOwner(IResourceHost owner)
	{
		owner = owner ?? throw new ArgumentNullException("owner");
		if (Owner == owner)
		{
			Owner = null;
			OnRemoveOwner(owner);
		}
	}
}
