using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.PropertyStore;
using Avalonia.Styling;

namespace Avalonia;

/// <summary>
/// Extends an <see cref="T:Avalonia.Animation.Animatable" /> with the following features:
///
/// - An inherited <see cref="P:Avalonia.StyledElement.DataContext" />.
/// - Implements <see cref="T:Avalonia.LogicalTree.ILogical" /> to form part of a logical tree.
/// - A collection of class strings for custom styling.
/// </summary>
public class StyledElement : Animatable, IDataContextProvider, ILogical, IThemeVariantHost, IResourceHost, IResourceNode, IStyleHost, ISetLogicalParent, ISetInheritanceParent, ISupportInitialize, INamed, IAvaloniaListItemValidator<ILogical>
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.StyledElement.DataContext" /> property.
	/// </summary>
	public static readonly StyledProperty<object?> DataContextProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.StyledElement.Name" /> property.
	/// </summary>
	public static readonly DirectProperty<StyledElement, string?> NameProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.StyledElement.Parent" /> property.
	/// </summary>
	public static readonly DirectProperty<StyledElement, StyledElement?> ParentProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.StyledElement.TemplatedParent" /> property.
	/// </summary>
	public static readonly DirectProperty<StyledElement, AvaloniaObject?> TemplatedParentProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.StyledElement.Theme" /> property.
	/// </summary>
	public static readonly StyledProperty<ControlTheme?> ThemeProperty;

	[ThreadStatic]
	private static ControlTheme? s_invalidTheme;

	private int _initCount;

	private string? _name;

	private Classes? _classes;

	private ILogicalRoot? _logicalRoot;

	private AvaloniaList<ILogical>? _logicalChildren;

	private IResourceDictionary? _resources;

	private Styles? _styles;

	private bool _stylesApplied;

	private bool _themeApplied;

	private bool _templatedParentThemeApplied;

	private AvaloniaObject? _templatedParent;

	private bool _dataContextUpdating;

	private ControlTheme? _implicitTheme;

	private ResourcesChangedEventArgs _lastResourcesChangedEventArgs;

	/// <summary>
	/// Gets or sets the name of the styled element.
	/// </summary>
	/// <remarks>
	/// An element's name is used to uniquely identify an element within the element's name
	/// scope. Once the element is added to a logical tree, its name cannot be changed.
	/// </remarks>
	public string? Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (_stylesApplied)
			{
				throw new InvalidOperationException("Cannot set Name : styled element already styled.");
			}
			_name = value;
		}
	}

	/// <summary>
	/// Gets or sets the styled element's classes.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Classes can be used to apply user-defined styling to styled elements, or to allow styled elements
	/// that share a common purpose to be easily selected.
	/// </para>
	/// </remarks>
	public Classes Classes => _classes ?? (_classes = new Classes());

	/// <summary>
	/// Gets or sets the control's data context.
	/// </summary>
	/// <remarks>
	/// The data context is an inherited property that specifies the default object that will
	/// be used for data binding.
	/// </remarks>
	public object? DataContext
	{
		get
		{
			return GetValue(DataContextProperty);
		}
		set
		{
			SetValue(DataContextProperty, value);
		}
	}

	/// <summary>
	/// Gets a value that indicates whether the element has finished initialization.
	/// </summary>
	/// <remarks>
	/// For more information about when IsInitialized is set, see the <see cref="E:Avalonia.StyledElement.Initialized" />
	/// event.
	/// </remarks>
	public bool IsInitialized { get; private set; }

	/// <summary>
	/// Gets the styles for the styled element.
	/// </summary>
	/// <remarks>
	/// Styles for the entire application are added to the Application.Styles collection, but
	/// each styled element may in addition define its own styles which are applied to the styled element
	/// itself and its children.
	/// </remarks>
	public Styles Styles => _styles ?? (_styles = new Styles(this));

	/// <summary>
	/// Gets the type by which the element is styled.
	/// </summary>
	/// <remarks>
	/// Usually controls are styled by their own type, but there are instances where you want
	/// an element to be styled by its base type, e.g. creating SpecialButton that
	/// derives from Button and adds extra functionality but is still styled as a regular
	/// Button. To change the style for a control class, override the <see cref="P:Avalonia.StyledElement.StyleKeyOverride" />
	/// property
	/// </remarks>
	public Type StyleKey => StyleKeyOverride;

	/// <summary>
	/// Gets or sets the styled element's resource dictionary.
	/// </summary>
	public IResourceDictionary Resources
	{
		get
		{
			return _resources ?? (_resources = new ResourceDictionary(this));
		}
		set
		{
			value = value ?? throw new ArgumentNullException("value");
			_resources?.RemoveOwner(this);
			_resources = value;
			_resources.AddOwner(this);
		}
	}

	/// <summary>
	/// Gets the styled element whose lookless template this styled element is part of.
	/// </summary>
	public AvaloniaObject? TemplatedParent
	{
		get
		{
			return _templatedParent;
		}
		internal set
		{
			SetAndRaise(TemplatedParentProperty, ref _templatedParent, value);
		}
	}

	/// <summary>
	/// Gets or sets the theme to be applied to the element.
	/// </summary>
	public ControlTheme? Theme
	{
		get
		{
			return GetValue(ThemeProperty);
		}
		set
		{
			SetValue(ThemeProperty, value);
		}
	}

	/// <summary>
	/// Gets the styled element's logical children.
	/// </summary>
	protected internal IAvaloniaList<ILogical> LogicalChildren
	{
		get
		{
			if (_logicalChildren == null)
			{
				AvaloniaList<ILogical> avaloniaList = new AvaloniaList<ILogical>
				{
					ResetBehavior = ResetBehavior.Remove,
					Validator = this
				};
				avaloniaList.CollectionChanged += LogicalChildrenCollectionChanged;
				_logicalChildren = avaloniaList;
			}
			return _logicalChildren;
		}
	}

	/// <summary>
	/// Gets the <see cref="P:Avalonia.StyledElement.Classes" /> collection in a form that allows adding and removing
	/// pseudoclasses.
	/// </summary>
	protected IPseudoClasses PseudoClasses => Classes;

	/// <summary>
	/// Gets the type by which the element is styled.
	/// </summary>
	/// <remarks>
	/// Usually controls are styled by their own type, but there are instances where you want
	/// an element to be styled by its base type, e.g. creating SpecialButton that
	/// derives from Button and adds extra functionality but is still styled as a regular
	/// Button. Override this property to change the style for a control class, returning the
	/// type that you wish the elements to be styled as.
	/// </remarks>
	protected virtual Type StyleKeyOverride => GetType();

	/// <summary>
	/// Gets a value indicating whether the element is attached to a rooted logical tree.
	/// </summary>
	bool ILogical.IsAttachedToLogicalTree => _logicalRoot != null;

	/// <summary>
	/// Gets the styled element's logical parent.
	/// </summary>
	public StyledElement? Parent { get; private set; }

	/// <inheritdoc />
	public ThemeVariant ActualThemeVariant => GetValue(ThemeVariant.ActualThemeVariantProperty);

	/// <summary>
	/// Gets the styled element's logical parent.
	/// </summary>
	ILogical? ILogical.LogicalParent => Parent;

	/// <summary>
	/// Gets the styled element's logical children.
	/// </summary>
	IAvaloniaReadOnlyList<ILogical> ILogical.LogicalChildren => LogicalChildren;

	/// <inheritdoc />
	bool IResourceNode.HasResources
	{
		get
		{
			IResourceDictionary? resources = _resources;
			if (resources == null || !resources.HasResources)
			{
				return ((IResourceNode)_styles)?.HasResources ?? false;
			}
			return true;
		}
	}

	/// <inheritdoc />
	bool IStyleHost.IsStylesInitialized => _styles != null;

	/// <inheritdoc />
	IStyleHost? IStyleHost.StylingParent => (IStyleHost)base.InheritanceParent;

	internal static ControlTheme InvalidTheme => s_invalidTheme ?? (s_invalidTheme = new ControlTheme());

	/// <summary>
	/// Raised when the styled element is attached to a rooted logical tree.
	/// </summary>
	public event EventHandler<LogicalTreeAttachmentEventArgs>? AttachedToLogicalTree;

	/// <summary>
	/// Raised when the styled element is detached from a rooted logical tree.
	/// </summary>
	public event EventHandler<LogicalTreeAttachmentEventArgs>? DetachedFromLogicalTree;

	/// <summary>
	/// Occurs when the <see cref="P:Avalonia.StyledElement.DataContext" /> property changes.
	/// </summary>
	/// <remarks>
	/// This event will be raised when the <see cref="P:Avalonia.StyledElement.DataContext" /> property has changed and
	/// all subscribers to that change have been notified.
	/// </remarks>
	public event EventHandler? DataContextChanged;

	/// <summary>
	/// Occurs when the styled element has finished initialization.
	/// </summary>
	/// <remarks>
	/// The Initialized event indicates that all property values on the styled element have been set.
	/// When loading the styled element from markup, it occurs when 
	/// <see cref="M:System.ComponentModel.ISupportInitialize.EndInit" /> is called *and* the styled element
	/// is attached to a rooted logical tree. When the styled element is created by code and
	/// <see cref="T:System.ComponentModel.ISupportInitialize" /> is not used, it is called when the styled element is attached
	/// to the visual tree.
	/// </remarks>
	public event EventHandler? Initialized;

	/// <summary>
	/// Occurs when a resource in this styled element or a parent styled element has changed.
	/// </summary>
	public event EventHandler<ResourcesChangedEventArgs>? ResourcesChanged;

	/// <inheritdoc />
	public event EventHandler? ActualThemeVariantChanged;

	/// <summary>
	/// Initializes static members of the <see cref="T:Avalonia.StyledElement" /> class.
	/// </summary>
	static StyledElement()
	{
		DataContextProperty = AvaloniaProperty.Register<StyledElement, object>("DataContext", null, inherits: true, BindingMode.OneWay, null, null, enableDataValidation: false, DataContextNotifying);
		NameProperty = AvaloniaProperty.RegisterDirect("Name", (StyledElement o) => o.Name, delegate(StyledElement o, string? v)
		{
			o.Name = v;
		});
		ParentProperty = AvaloniaProperty.RegisterDirect("Parent", (StyledElement o) => o.Parent);
		TemplatedParentProperty = AvaloniaProperty.RegisterDirect("TemplatedParent", (StyledElement o) => o.TemplatedParent);
		ThemeProperty = AvaloniaProperty.Register<StyledElement, ControlTheme>("Theme");
		DataContextProperty.Changed.AddClassHandler(delegate(StyledElement x, AvaloniaPropertyChangedEventArgs e)
		{
			x.OnDataContextChangedCore(e);
		});
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.StyledElement" /> class.
	/// </summary>
	public StyledElement()
	{
		_logicalRoot = this as ILogicalRoot;
	}

	/// <inheritdoc />
	public virtual void BeginInit()
	{
		_initCount++;
	}

	/// <inheritdoc />
	public virtual void EndInit()
	{
		if (_initCount == 0)
		{
			throw new InvalidOperationException("BeginInit was not called.");
		}
		if (--_initCount == 0 && _logicalRoot != null)
		{
			ApplyStyling();
			InitializeIfNeeded();
		}
	}

	/// <summary>
	/// Applies styling to the control if the control is initialized and styling is not
	/// already applied.
	/// </summary>
	/// <remarks>
	/// The styling system will automatically apply styling when required, so it should not
	/// usually be necessary to call this method manually.
	/// </remarks>
	/// <returns>
	/// A value indicating whether styling is now applied to the control.
	/// </returns>
	public bool ApplyStyling()
	{
		if (_initCount == 0 && (!_stylesApplied || !_themeApplied || !_templatedParentThemeApplied))
		{
			GetValueStore().BeginStyling();
			try
			{
				if (!_themeApplied)
				{
					ApplyControlTheme();
					_themeApplied = true;
				}
				if (!_templatedParentThemeApplied)
				{
					ApplyTemplatedParentControlTheme();
					_templatedParentThemeApplied = true;
				}
				if (!_stylesApplied)
				{
					ApplyStyles(this);
					_stylesApplied = true;
				}
			}
			finally
			{
				GetValueStore().EndStyling();
			}
		}
		return _stylesApplied;
	}

	protected void InitializeIfNeeded()
	{
		if (_initCount == 0 && !IsInitialized)
		{
			IsInitialized = true;
			OnInitialized();
			Initialized?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <inheritdoc />
	void ILogical.NotifyAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		OnAttachedToLogicalTreeCore(e);
	}

	/// <inheritdoc />
	void ILogical.NotifyDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		OnDetachedFromLogicalTreeCore(e);
	}

	/// <inheritdoc />
	void ILogical.NotifyResourcesChanged(ResourcesChangedEventArgs e)
	{
		NotifyResourcesChanged(e);
	}

	/// <inheritdoc />
	void IResourceHost.NotifyHostedResourcesChanged(ResourcesChangedEventArgs e)
	{
		NotifyResourcesChanged(e);
	}

	/// <inheritdoc />
	public bool TryGetResource(object key, ThemeVariant? theme, out object? value)
	{
		value = null;
		IResourceDictionary? resources = _resources;
		if (resources == null || !resources.TryGetResource(key, theme, out value))
		{
			return _styles?.TryGetResource(key, theme, out value) ?? false;
		}
		return true;
	}

	/// <summary>
	/// Sets the styled element's logical parent.
	/// </summary>
	/// <param name="parent">The parent.</param>
	void ISetLogicalParent.SetParent(ILogical? parent)
	{
		StyledElement parent2 = Parent;
		if (parent != parent2)
		{
			if (parent2 != null && parent != null)
			{
				throw new InvalidOperationException("The Control already has a parent.");
			}
			if (base.InheritanceParent == null || parent == null)
			{
				base.InheritanceParent = parent as AvaloniaObject;
			}
			Parent = (StyledElement)parent;
			if (_logicalRoot != null)
			{
				LogicalTreeAttachmentEventArgs e = new LogicalTreeAttachmentEventArgs(_logicalRoot, this, parent2);
				OnDetachedFromLogicalTreeCore(e);
			}
			ILogicalRoot logicalRoot = FindLogicalRoot(this);
			if (logicalRoot != null)
			{
				LogicalTreeAttachmentEventArgs e2 = new LogicalTreeAttachmentEventArgs(logicalRoot, this, parent);
				OnAttachedToLogicalTreeCore(e2);
			}
			else if (parent == null)
			{
				NotifyResourcesChanged(ResourcesChangedEventArgs.Create());
			}
			RaisePropertyChanged(ParentProperty, parent2, Parent);
		}
	}

	/// <summary>
	/// Sets the styled element's inheritance parent.
	/// </summary>
	/// <param name="parent">The parent.</param>
	void ISetInheritanceParent.SetParent(AvaloniaObject? parent)
	{
		base.InheritanceParent = parent;
	}

	void IStyleHost.StylesAdded(IReadOnlyList<IStyle> styles)
	{
		if (HasSettersOrAnimations(styles))
		{
			InvalidateStyles(recurse: true);
		}
	}

	void IStyleHost.StylesRemoved(IReadOnlyList<IStyle> styles)
	{
		IReadOnlyList<Style> readOnlyList = FlattenStyles(styles);
		if (readOnlyList != null)
		{
			DetachStyles(readOnlyList);
		}
	}

	protected virtual void LogicalChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		switch (e.Action)
		{
		case NotifyCollectionChangedAction.Add:
			SetLogicalParent(e.NewItems);
			break;
		case NotifyCollectionChangedAction.Remove:
			ClearLogicalParent(e.OldItems);
			break;
		case NotifyCollectionChangedAction.Replace:
			ClearLogicalParent(e.OldItems);
			SetLogicalParent(e.NewItems);
			break;
		case NotifyCollectionChangedAction.Reset:
			throw new NotSupportedException("Reset should not be signaled on LogicalChildren collection");
		case NotifyCollectionChangedAction.Move:
			break;
		}
	}

	/// <summary>
	/// Notifies child controls that a change has been made to resources that apply to them.
	/// </summary>
	/// <param name="e">The change token.</param>
	internal virtual void NotifyChildResourcesChanged(ResourcesChangedEventArgs e)
	{
		if (_logicalChildren == null)
		{
			return;
		}
		int count = _logicalChildren.Count;
		if (count > 0)
		{
			for (int i = 0; i < count; i++)
			{
				_logicalChildren[i].NotifyResourcesChanged(e);
			}
		}
	}

	/// <summary>
	/// Called when the styled element is added to a rooted logical tree.
	/// </summary>
	/// <param name="e">The event args.</param>
	protected virtual void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
	}

	/// <summary>
	/// Called when the styled element is removed from a rooted logical tree.
	/// </summary>
	/// <param name="e">The event args.</param>
	protected virtual void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
	}

	/// <summary>
	/// Called when the <see cref="P:Avalonia.StyledElement.DataContext" /> property changes.
	/// </summary>
	/// <param name="e">The event args.</param>
	protected virtual void OnDataContextChanged(EventArgs e)
	{
		DataContextChanged?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Called when the <see cref="P:Avalonia.StyledElement.DataContext" /> begins updating.
	/// </summary>
	protected virtual void OnDataContextBeginUpdate()
	{
	}

	/// <summary>
	/// Called when the <see cref="P:Avalonia.StyledElement.DataContext" /> finishes updating.
	/// </summary>
	protected virtual void OnDataContextEndUpdate()
	{
	}

	/// <summary>
	/// Called when the control finishes initialization.
	/// </summary>
	protected virtual void OnInitialized()
	{
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == ThemeProperty)
		{
			OnControlThemeChanged();
		}
		else if (change.Property == ThemeVariant.RequestedThemeVariantProperty)
		{
			ThemeVariant newValue = change.GetNewValue<ThemeVariant>();
			if ((object)newValue != null && newValue != ThemeVariant.Default)
			{
				SetValue(ThemeVariant.ActualThemeVariantProperty, newValue);
			}
			else
			{
				ClearValue(ThemeVariant.ActualThemeVariantProperty);
			}
		}
		else if (change.Property == ThemeVariant.ActualThemeVariantProperty)
		{
			ActualThemeVariantChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private protected virtual void OnControlThemeChanged()
	{
		ValueStore valueStore = GetValueStore();
		valueStore.BeginStyling();
		try
		{
			valueStore.RemoveFrames(FrameType.Theme);
		}
		finally
		{
			valueStore.EndStyling();
			_themeApplied = false;
		}
	}

	internal virtual void OnTemplatedParentControlThemeChanged()
	{
		ValueStore valueStore = GetValueStore();
		valueStore.BeginStyling();
		try
		{
			valueStore.RemoveFrames(FrameType.TemplatedParentTheme);
		}
		finally
		{
			valueStore.EndStyling();
			_templatedParentThemeApplied = false;
		}
	}

	internal ControlTheme? GetEffectiveTheme()
	{
		ControlTheme theme = Theme;
		if (theme != null)
		{
			return theme;
		}
		if (_implicitTheme == null)
		{
			Type styleKey = StyleKey;
			if (this.TryFindResource(styleKey, out object value) && value is ControlTheme implicitTheme)
			{
				_implicitTheme = implicitTheme;
			}
			else
			{
				_implicitTheme = InvalidTheme;
			}
		}
		if (_implicitTheme != InvalidTheme)
		{
			return _implicitTheme;
		}
		return null;
	}

	internal virtual void InvalidateStyles(bool recurse)
	{
		ValueStore valueStore = GetValueStore();
		valueStore.BeginStyling();
		try
		{
			valueStore.RemoveFrames(FrameType.Style);
		}
		finally
		{
			valueStore.EndStyling();
		}
		_stylesApplied = false;
		if (!recurse)
		{
			return;
		}
		IReadOnlyList<AvaloniaObject> inheritanceChildren = GetInheritanceChildren();
		if (inheritanceChildren != null)
		{
			int count = inheritanceChildren.Count;
			for (int i = 0; i < count; i++)
			{
				(inheritanceChildren[i] as StyledElement)?.InvalidateStyles(recurse);
			}
		}
	}

	private static void DataContextNotifying(AvaloniaObject o, bool updateStarted)
	{
		if (o is StyledElement element)
		{
			DataContextNotifying(element, updateStarted);
		}
	}

	private static void DataContextNotifying(StyledElement element, bool updateStarted)
	{
		if (updateStarted)
		{
			if (element._dataContextUpdating)
			{
				return;
			}
			element._dataContextUpdating = true;
			element.OnDataContextBeginUpdate();
			int count = element.LogicalChildren.Count;
			for (int i = 0; i < count; i++)
			{
				if (element.LogicalChildren[i] is StyledElement styledElement && styledElement.InheritanceParent == element && !styledElement.IsSet(DataContextProperty))
				{
					DataContextNotifying(styledElement, updateStarted);
				}
			}
		}
		else if (element._dataContextUpdating)
		{
			element.OnDataContextEndUpdate();
			element._dataContextUpdating = false;
		}
	}

	private static ILogicalRoot? FindLogicalRoot(IStyleHost? e)
	{
		while (e != null)
		{
			if (e is ILogicalRoot result)
			{
				return result;
			}
			e = e.StylingParent;
		}
		return null;
	}

	void IAvaloniaListItemValidator<ILogical>.Validate(ILogical item)
	{
		if (item == null)
		{
			throw new ArgumentException("Cannot add null to LogicalChildren.");
		}
	}

	private void ApplyControlTheme()
	{
		ControlTheme effectiveTheme = GetEffectiveTheme();
		if (effectiveTheme != null)
		{
			ApplyControlTheme(effectiveTheme, FrameType.Theme);
		}
	}

	private void ApplyTemplatedParentControlTheme()
	{
		ControlTheme controlTheme = (TemplatedParent as StyledElement)?.GetEffectiveTheme();
		if (controlTheme != null)
		{
			ApplyControlTheme(controlTheme, FrameType.TemplatedParentTheme);
		}
	}

	private void ApplyControlTheme(ControlTheme theme, FrameType type)
	{
		ControlTheme basedOn = theme.BasedOn;
		if (basedOn != null)
		{
			ApplyControlTheme(basedOn, type);
		}
		theme.TryAttach(this, type);
		if (theme.HasChildren)
		{
			IList<IStyle> children = theme.Children;
			for (int i = 0; i < children.Count; i++)
			{
				ApplyStyle(children[i], null, type);
			}
		}
	}

	private void ApplyStyles(IStyleHost host)
	{
		IStyleHost stylingParent = host.StylingParent;
		if (stylingParent != null)
		{
			ApplyStyles(stylingParent);
		}
		if (host.IsStylesInitialized)
		{
			Styles styles = host.Styles;
			for (int i = 0; i < styles.Count; i++)
			{
				ApplyStyle(styles[i], host, FrameType.Style);
			}
		}
	}

	private void ApplyStyle(IStyle style, IStyleHost? host, FrameType type)
	{
		if (style is ContainerQuery containerQuery)
		{
			containerQuery.TryAttach(this, host, type);
		}
		if (style is Style style2)
		{
			style2.TryAttach(this, host, type);
		}
		IReadOnlyList<IStyle> children = style.Children;
		for (int i = 0; i < children.Count; i++)
		{
			ApplyStyle(children[i], host, type);
		}
	}

	private void ReevaluateImplicitTheme()
	{
		if (Theme == null)
		{
			ControlTheme controlTheme = ((_implicitTheme == InvalidTheme) ? null : _implicitTheme);
			_implicitTheme = null;
			GetEffectiveTheme();
			if (((_implicitTheme == InvalidTheme) ? null : _implicitTheme) != controlTheme)
			{
				OnControlThemeChanged();
				_themeApplied = false;
			}
		}
	}

	private void OnAttachedToLogicalTreeCore(LogicalTreeAttachmentEventArgs e)
	{
		if (this.GetLogicalParent() == null && !(this is ILogicalRoot))
		{
			throw new InvalidOperationException("AttachedToLogicalTreeCore called for '" + GetType().Name + "' but control has no logical parent.");
		}
		if (_logicalRoot == null)
		{
			_logicalRoot = e.Root;
			ReevaluateImplicitTheme();
			ApplyStyling();
			NotifyResourcesChanged(ResourcesChangedEventArgs.Create(), propagate: false);
			OnAttachedToLogicalTree(e);
			AttachedToLogicalTree?.Invoke(this, e);
		}
		IAvaloniaList<ILogical> logicalChildren = LogicalChildren;
		int count = logicalChildren.Count;
		for (int i = 0; i < count; i++)
		{
			if (logicalChildren[i] is StyledElement styledElement && styledElement._logicalRoot != e.Root)
			{
				styledElement.OnAttachedToLogicalTreeCore(e);
			}
		}
	}

	private void OnDetachedFromLogicalTreeCore(LogicalTreeAttachmentEventArgs e)
	{
		if (_logicalRoot == null)
		{
			return;
		}
		_logicalRoot = null;
		InvalidateStyles(recurse: false);
		OnDetachedFromLogicalTree(e);
		DetachedFromLogicalTree?.Invoke(this, e);
		IAvaloniaList<ILogical> logicalChildren = LogicalChildren;
		int count = logicalChildren.Count;
		for (int i = 0; i < count; i++)
		{
			if (logicalChildren[i] is StyledElement styledElement)
			{
				styledElement.OnDetachedFromLogicalTreeCore(e);
			}
		}
	}

	private void OnDataContextChangedCore(AvaloniaPropertyChangedEventArgs e)
	{
		OnDataContextChanged(EventArgs.Empty);
	}

	private void SetLogicalParent(IList children)
	{
		int count = children.Count;
		for (int i = 0; i < count; i++)
		{
			ILogical logical = (ILogical)children[i];
			if (logical.LogicalParent == null)
			{
				((ISetLogicalParent)logical).SetParent(this);
			}
		}
	}

	private void ClearLogicalParent(IList children)
	{
		int count = children.Count;
		for (int i = 0; i < count; i++)
		{
			ILogical logical = (ILogical)children[i];
			if (logical.LogicalParent == this)
			{
				((ISetLogicalParent)logical).SetParent(null);
			}
		}
	}

	private void DetachStyles(IReadOnlyList<Style> styles)
	{
		ValueStore valueStore = GetValueStore();
		valueStore.BeginStyling();
		try
		{
			valueStore.RemoveFrames(styles);
		}
		finally
		{
			valueStore.EndStyling();
		}
		if (_logicalChildren != null)
		{
			int count = _logicalChildren.Count;
			for (int i = 0; i < count; i++)
			{
				(_logicalChildren[i] as StyledElement)?.DetachStyles(styles);
			}
		}
	}

	internal void NotifyResourcesChanged(ResourcesChangedEventArgs e, bool propagate = true)
	{
		if (!e.Equals(_lastResourcesChangedEventArgs))
		{
			_lastResourcesChangedEventArgs = e;
			ResourcesChanged?.Invoke(this, e);
			if (propagate)
			{
				NotifyChildResourcesChanged(e);
			}
		}
	}

	internal override void BuildDebugDisplay(StringBuilder builder, bool includeContent)
	{
		base.BuildDebugDisplay(builder, includeContent);
		DebugDisplayHelper.AppendOptionalValue(builder, "Name", Name, includeContent);
	}

	private static IReadOnlyList<Style>? FlattenStyles(IReadOnlyList<IStyle> styles)
	{
		List<Style> result = null;
		FlattenStyles(styles, ref result);
		return result;
		static void FlattenStyle(IStyle style, ref List<Style>? reference)
		{
			if (style is Style item)
			{
				(reference ?? (reference = new List<Style>())).Add(item);
			}
			FlattenStyles(style.Children, ref reference);
		}
		static void FlattenStyles(IReadOnlyList<IStyle> readOnlyList, ref List<Style>? result2)
		{
			int count = readOnlyList.Count;
			for (int i = 0; i < count; i++)
			{
				FlattenStyle(readOnlyList[i], ref result2);
			}
		}
	}

	private static bool HasSettersOrAnimations(IReadOnlyList<IStyle> styles)
	{
		int count = styles.Count;
		for (int i = 0; i < count; i++)
		{
			if (StyleHasSettersOrAnimations(styles[i]))
			{
				return true;
			}
		}
		return false;
		static bool StyleHasSettersOrAnimations(IStyle style)
		{
			if (style is StyleBase { HasSettersOrAnimations: not false })
			{
				return true;
			}
			return HasSettersOrAnimations(style.Children);
		}
	}

	private static IReadOnlyList<StyleBase> RecurseStyles(IReadOnlyList<IStyle> styles)
	{
		List<StyleBase> result = new List<StyleBase>();
		RecurseStyles(styles, result);
		return result;
	}

	private static void RecurseStyles(IReadOnlyList<IStyle> styles, List<StyleBase> result)
	{
		int count = styles.Count;
		for (int i = 0; i < count; i++)
		{
			IStyle style = styles[i];
			if (style is StyleBase item)
			{
				result.Add(item);
			}
			else if (style is IReadOnlyList<IStyle> styles2)
			{
				RecurseStyles(styles2, result);
			}
		}
	}
}
