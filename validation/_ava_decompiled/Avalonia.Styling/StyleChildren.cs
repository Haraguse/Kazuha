using System.Collections.ObjectModel;
using Avalonia.Controls;

namespace Avalonia.Styling;

internal class StyleChildren : Collection<IStyle>
{
	private readonly StyleBase _owner;

	public StyleChildren(StyleBase owner)
	{
		_owner = owner;
	}

	protected override void InsertItem(int index, IStyle item)
	{
		(item as StyleBase)?.SetParent(_owner);
		base.InsertItem(index, item);
	}

	protected override void RemoveItem(int index)
	{
		IStyle style = base.Items[index];
		(style as StyleBase)?.SetParent(null);
		IResourceHost owner = _owner.Owner;
		if (owner != null)
		{
			(style as IResourceProvider)?.RemoveOwner(owner);
		}
		base.RemoveItem(index);
	}

	protected override void SetItem(int index, IStyle item)
	{
		(item as StyleBase)?.SetParent(_owner);
		base.SetItem(index, item);
		IResourceHost owner = _owner.Owner;
		if (owner != null)
		{
			(item as IResourceProvider)?.AddOwner(owner);
		}
	}
}
