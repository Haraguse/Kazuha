using System;
using System.Collections;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;

namespace FluentAvalonia.UI.Data;

/// <summary>
/// Provides data on how a CollectionView should sort its items
/// </summary>
public class FASortDescription
{
	private class ObjectComparer : IComparer
	{
		public static readonly IComparer Instance = new ObjectComparer();

		private ObjectComparer()
		{
		}

		public int Compare(object x, object y)
		{
			IComparable comparable = x as IComparable;
			IComparable comparable2 = y as IComparable;
			if (comparable != comparable2)
			{
				if (comparable != null)
				{
					if (comparable2 != null)
					{
						return comparable.CompareTo(comparable2);
					}
					return 1;
				}
				return -1;
			}
			return 0;
		}
	}

	private string _propertyName;

	/// <summary>
	/// Gets or sets the property this sort description uses to sort.
	/// </summary>
	[AssignBinding]
	public BindingBase Property { get; set; }

	/// <summary>
	/// Gets or sets the name of the property used for sorting. If the name hasn't been set,
	/// it will be set from the binding, if possible. Note that nested properties are not supported
	/// </summary>
	public string PropertyName
	{
		get
		{
			if (_propertyName == null)
			{
				BindingBase property = Property;
				Binding val = (Binding)(object)((property is Binding) ? property : null);
				if (val != null)
				{
					_propertyName = ((ReflectionBinding)val).Path;
				}
				else
				{
					BindingBase property2 = Property;
					CompiledBindingExtension val2 = (CompiledBindingExtension)(object)((property2 is CompiledBindingExtension) ? property2 : null);
					if (val2 != null)
					{
						_propertyName = ((object)((CompiledBinding)val2).Path).ToString();
					}
				}
			}
			return _propertyName;
		}
		set
		{
			_propertyName = value;
		}
	}

	/// <summary>
	/// Gets or sets the direction the sort description should sort
	/// </summary>
	public FASortDirection Direction { get; set; }

	/// <summary>
	/// Gets or sets a custom IComparer implementation used to compare items for sorting
	/// </summary>
	public IComparer Comparer { get; set; }

	/// <summary>
	/// Creates a default sort description, that sorts in Ascending order using the default
	/// object comparer (which compares the items themselves)
	/// </summary>
	public FASortDescription()
		: this(null, null, FASortDirection.Ascending, ObjectComparer.Instance)
	{
	}

	/// <summary>
	/// Creates a sort description with the specified SortDirection and comparer
	/// </summary>
	/// <param name="direction">The direction this description should sort items</param>
	/// <param name="comparer">The custom IComparer implementation</param>
	public FASortDescription(FASortDirection direction, IComparer comparer = null)
		: this(null, null, direction, comparer)
	{
	}

	/// <summary>
	/// Creates a sort description that sorts using a custom binding expression
	/// </summary>
	/// <param name="property">The property that should be used for sorting</param>
	/// <param name="propertyName">The name of the property. If this is unset, the name is retrieved
	/// from the binding, if applicable</param>
	/// <param name="comparer">The custom IComparer implementation</param>
	/// <remarks>Note: nested properties are not supported, particularly for live shaping</remarks>
	public FASortDescription(BindingBase property, string propertyName = null, IComparer comparer = null)
		: this(property, propertyName, FASortDirection.Ascending, comparer)
	{
	}

	/// <summary>
	/// Creates a custom sort description with a specified property binding, direction, and IComparer
	/// </summary>
	/// <param name="property">The property that should be used for sorting</param>
	/// <param name="propertyName">The name of the property. If this is null, the name is retrieved
	/// from the binding, if applicable&gt;</param>
	/// <param name="direction">The direction this description should sort items</param>
	/// <param name="comparer">The custom IComparer implementation</param>
	/// <remarks>Note: nested properties are not supported, particularly for live shaping</remarks>
	public FASortDescription(BindingBase property, string propertyName, FASortDirection direction, IComparer comparer)
	{
		Property = property;
		_propertyName = propertyName;
		Direction = direction;
		Comparer = comparer ?? ObjectComparer.Instance;
	}

	public static FASortDescription CreateCompiled(string propertyName, Func<object, object> getter, Type propertyType, FASortDirection direction)
	{
		return CreateCompiled(propertyName, getter, propertyType, direction, null);
	}

	public static FASortDescription CreateCompiled(string propertyName, Func<object, object> getter, Type propertyType, FASortDirection direction, IComparer comparer)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		return new FASortDescription((BindingBase)new CompiledBindingExtension(new CompiledBindingPathBuilder().Property((IPropertyInfo)new ClrPropertyInfo(propertyName, getter, (Action<object, object>)null, propertyType), (Func<WeakReference<object>, IPropertyInfo, IPropertyAccessor>)PropertyInfoAccessorFactory.CreateInpcPropertyAccessor).Build()), propertyName, direction, comparer);
	}
}
