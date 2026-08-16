using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Metadata;

namespace FluentAvalonia.UI.Data;

/// <summary>
/// Provides a data source that adds grouping and current-item support to collection classes.
/// </summary>
public class FACollectionViewSource : AvaloniaObject, ISupportInitialize
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Data.FACollectionViewSource.IsSourceGrouped" /> property
	/// </summary>
	public static readonly DirectProperty<FACollectionViewSource, bool> IsSourceGroupedProperty = AvaloniaProperty.RegisterDirect<FACollectionViewSource, bool>("IsSourceGrouped", (Func<FACollectionViewSource, bool>)((FACollectionViewSource x) => x.IsSourceGrouped), (Action<FACollectionViewSource, bool>)delegate(FACollectionViewSource x, bool v)
	{
		x.IsSourceGrouped = v;
	}, false, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Data.FACollectionViewSource.ItemsBinding" /> property
	/// </summary>
	public static readonly DirectProperty<FACollectionViewSource, BindingBase> ItemsBindingProperty = AvaloniaProperty.RegisterDirect<FACollectionViewSource, BindingBase>("ItemsBinding", (Func<FACollectionViewSource, BindingBase>)((FACollectionViewSource x) => x.ItemsBinding), (Action<FACollectionViewSource, BindingBase>)delegate(FACollectionViewSource x, BindingBase v)
	{
		x.ItemsBinding = v;
	}, (BindingBase)null, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Data.FACollectionViewSource.Source" /> property
	/// </summary>
	public static readonly DirectProperty<FACollectionViewSource, IEnumerable> SourceProperty = AvaloniaProperty.RegisterDirect<FACollectionViewSource, IEnumerable>("Source", (Func<FACollectionViewSource, IEnumerable>)((FACollectionViewSource x) => x.Source), (Action<FACollectionViewSource, IEnumerable>)delegate(FACollectionViewSource x, IEnumerable v)
	{
		x.Source = v;
	}, (IEnumerable)null, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Data.FACollectionViewSource.View" /> property
	/// </summary>
	public static readonly DirectProperty<FACollectionViewSource, IFACollectionView> ViewProperty = AvaloniaProperty.RegisterDirect<FACollectionViewSource, IFACollectionView>("View", (Func<FACollectionViewSource, IFACollectionView>)((FACollectionViewSource x) => x.View), (Action<FACollectionViewSource, IFACollectionView>)null, (IFACollectionView)null, (BindingMode)1, false);

	public static readonly DirectProperty<FACollectionViewSource, Predicate<object>> FilterProperty = AvaloniaProperty.RegisterDirect<FACollectionViewSource, Predicate<object>>("Filter", (Func<FACollectionViewSource, Predicate<object>>)((FACollectionViewSource x) => x.Filter), (Action<FACollectionViewSource, Predicate<object>>)delegate(FACollectionViewSource x, Predicate<object> v)
	{
		x.Filter = v;
	}, (Predicate<object>)null, (BindingMode)1, false);

	public static readonly DirectProperty<FACollectionViewSource, IList<string>> LiveFilterPropertiesProperty = AvaloniaProperty.RegisterDirect<FACollectionViewSource, IList<string>>("LiveFilterProperties", (Func<FACollectionViewSource, IList<string>>)((FACollectionViewSource x) => (IList<string>)x.LiveFilterProperties), (Action<FACollectionViewSource, IList<string>>)null, (IList<string>)null, (BindingMode)1, false);

	public static readonly DirectProperty<FACollectionViewSource, IList<FASortDescription>> SortDescriptionsProperty = AvaloniaProperty.RegisterDirect<FACollectionViewSource, IList<FASortDescription>>("SortDescriptions", (Func<FACollectionViewSource, IList<FASortDescription>>)((FACollectionViewSource x) => (IList<FASortDescription>)x.SortDescriptions), (Action<FACollectionViewSource, IList<FASortDescription>>)null, (IList<FASortDescription>)null, (BindingMode)1, false);

	public static readonly DirectProperty<FACollectionViewSource, bool> IsLiveShapingEnabledProperty = AvaloniaProperty.RegisterDirect<FACollectionViewSource, bool>("IsLiveShapingEnabled", (Func<FACollectionViewSource, bool>)((FACollectionViewSource x) => x.IsLiveShapingEnabled), (Action<FACollectionViewSource, bool>)delegate(FACollectionViewSource x, bool v)
	{
		x.IsLiveShapingEnabled = v;
	}, false, (BindingMode)1, false);

	private bool _isInitializing;

	private bool _isSourceGrouped;

	private BindingBase _itemsBinding;

	private IEnumerable _source;

	private IFACollectionView _view;

	private AvaloniaList<string> _liveFilterProperties;

	private AvaloniaList<FASortDescription> _sortDescriptions;

	private bool _isLiveShapingEnabled;

	private Predicate<object> _filter;

	/// <summary>
	/// Gets or sets a value that indicates whether source data is grouped.
	/// </summary>
	public bool IsSourceGrouped
	{
		get
		{
			return _isSourceGrouped;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<bool>((DirectPropertyBase<bool>)(object)IsSourceGroupedProperty, ref _isSourceGrouped, value);
		}
	}

	/// <summary>
	/// Gets or sets the property path to follow from the top level item to find groups within the CollectionViewSource.
	/// </summary>
	[AssignBinding]
	[InheritDataTypeFromItems("Source")]
	public BindingBase ItemsBinding
	{
		get
		{
			return _itemsBinding;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<BindingBase>((DirectPropertyBase<BindingBase>)(object)ItemsBindingProperty, ref _itemsBinding, value);
		}
	}

	/// <summary>
	/// Gets or sets the collection object from which to create this view.
	/// </summary>
	public IEnumerable Source
	{
		get
		{
			return _source;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<IEnumerable>((DirectPropertyBase<IEnumerable>)(object)SourceProperty, ref _source, value);
		}
	}

	/// <summary>
	/// Gets the view object that is currently associated with this instance of CollectionViewSource.
	/// </summary>
	public IFACollectionView View
	{
		get
		{
			return _view;
		}
		private set
		{
			((AvaloniaObject)this).SetAndRaise<IFACollectionView>((DirectPropertyBase<IFACollectionView>)(object)ViewProperty, ref _view, value);
		}
	}

	public Predicate<object> Filter
	{
		get
		{
			return _filter;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<Predicate<object>>((DirectPropertyBase<Predicate<object>>)(object)FilterProperty, ref _filter, value);
			UpdateView();
		}
	}

	/// <summary>
	/// Gets a list (comma separated) of properties that should be used for live filtering
	/// of the CollectionView
	/// </summary>
	/// <remarks>
	/// In order to use this property, <see cref="P:FluentAvalonia.UI.Data.FACollectionViewSource.IsLiveShapingEnabled" /> must be set to true
	/// or an error will be thrown when creating the ICollectionView
	/// </remarks>
	public AvaloniaList<string> LiveFilterProperties
	{
		get
		{
			if (_liveFilterProperties == null)
			{
				_liveFilterProperties = new AvaloniaList<string>();
				_liveFilterProperties.CollectionChanged += SortOrFilterListChanged;
			}
			return _liveFilterProperties;
		}
	}

	/// <summary>
	/// Gets a list of <see cref="T:FluentAvalonia.UI.Data.FASortDescription" /> that is used for sorting the ICollectionView
	/// </summary>
	public AvaloniaList<FASortDescription> SortDescriptions
	{
		get
		{
			if (_sortDescriptions == null)
			{
				_sortDescriptions = new AvaloniaList<FASortDescription>();
				_sortDescriptions.CollectionChanged += SortOrFilterListChanged;
			}
			return _sortDescriptions;
		}
	}

	/// <summary>
	/// Gets or sets whether the ICollectionView should respond to changes of the properties 
	/// specified in <see cref="P:FluentAvalonia.UI.Data.FACollectionViewSource.LiveFilterProperties" /> or <see cref="T:FluentAvalonia.UI.Data.FASortDescription" />
	/// </summary>
	public bool IsLiveShapingEnabled
	{
		get
		{
			return _isLiveShapingEnabled;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<bool>((DirectPropertyBase<bool>)(object)IsLiveShapingEnabledProperty, ref _isLiveShapingEnabled, value);
		}
	}

	public void BeginInit()
	{
		_isInitializing = true;
	}

	public void EndInit()
	{
		_isInitializing = false;
		UpdateView();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((AvaloniaObject)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)SourceProperty || change.Property == (AvaloniaProperty)(object)ItemsBindingProperty || change.Property == (AvaloniaProperty)(object)IsSourceGroupedProperty || change.Property == (AvaloniaProperty)(object)IsLiveShapingEnabledProperty)
		{
			UpdateView();
		}
	}

	private void SortOrFilterListChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		UpdateView();
	}

	private void UpdateView()
	{
		if (_isInitializing)
		{
			return;
		}
		if (Source is IFACollectionViewFactory iFACollectionViewFactory)
		{
			View = iFACollectionViewFactory.CreateView();
			return;
		}
		IEnumerable source = Source;
		if (source != null)
		{
			if (_isSourceGrouped)
			{
				if (_view is FAGroupedDataCollectionView fAGroupedDataCollectionView && fAGroupedDataCollectionView.Source == _source && fAGroupedDataCollectionView.IsLiveShapingEnabled == _isLiveShapingEnabled && fAGroupedDataCollectionView.ItemsBinding == _itemsBinding)
				{
					fAGroupedDataCollectionView.UpdateViewFromCollectionViewSource(_filter, (IList<string>)_liveFilterProperties, (IList<FASortDescription>)_sortDescriptions);
				}
				else
				{
					View = new FAGroupedDataCollectionView(source, _itemsBinding, _isLiveShapingEnabled, _filter, (IList<string>)_liveFilterProperties, (IList<FASortDescription>)_sortDescriptions);
				}
			}
			else if (_view is FAIterableCollectionView fAIterableCollectionView && fAIterableCollectionView.Source == _source && fAIterableCollectionView.IsLiveShapingEnabled == _isLiveShapingEnabled)
			{
				fAIterableCollectionView.UpdateViewFromCollectionViewSource(_filter, (IList<string>)_liveFilterProperties, (IList<FASortDescription>)_sortDescriptions);
			}
			else
			{
				View = new FAIterableCollectionView(source, _isLiveShapingEnabled, _filter, (IList<string>)_liveFilterProperties, (IList<FASortDescription>)_sortDescriptions);
			}
		}
		else
		{
			View = null;
		}
	}
}
