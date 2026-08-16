using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Threading;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Navigation;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Displays <see cref="T:Avalonia.Controls.UserControl" /> instances (Pages in WinUI), supports navigation to new pages, 
/// and maintains a navigation history to support forward and backward navigation.
/// </summary>
[TemplatePart("ContentPresenter", typeof(ContentPresenter))]
public class FAFrame : ContentControl
{
	private class NavigationCacheItem
	{
		public Type PageSrcType;

		public object Context;

		public Control Page;

		public NavigationCacheItem(Type pageType, object context, Control page)
		{
			if (pageType != null && context != null)
			{
				throw new InvalidOperationException("PageType and Context cannot both be set");
			}
			PageSrcType = pageType;
			Context = context;
			Page = page;
		}
	}

	private CancellationTokenSource _cts;

	private ContentPresenter _presenter;

	private readonly List<NavigationCacheItem> _pageCache = new List<NavigationCacheItem>(10);

	private bool _isNavigating;

	private const string s_tpContentPresenter = "ContentPresenter";

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFrame.SourcePageType" /> property
	/// </summary>
	/// <remarks>
	/// When concerned about trimming/aot, do not set the <see cref="P:FluentAvalonia.UI.Controls.FAFrame.SourcePageType" /> using the property!
	/// Use <see cref="M:FluentAvalonia.UI.Controls.FAFrame.Navigate(System.Type)" /> instead.
	/// </remarks>
	public static readonly StyledProperty<Type> SourcePageTypeProperty = AvaloniaProperty.Register<FAFrame, Type>("SourcePageType", (Type)null, false, (BindingMode)1, (Func<Type, bool>)null, (Func<AvaloniaObject, Type, Type>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFrame.CacheSize" /> property
	/// </summary>
	public static readonly StyledProperty<int> CacheSizeProperty = AvaloniaProperty.Register<FAFrame, int>("CacheSize", 10, false, (BindingMode)1, (Func<int, bool>)null, (Func<AvaloniaObject, int, int>)((AvaloniaObject x, int v) => (v >= 0) ? v : 0), false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFrame.BackStackDepth" /> property
	/// </summary>
	public static readonly DirectProperty<FAFrame, int> BackStackDepthProperty = AvaloniaProperty.RegisterDirect<FAFrame, int>("BackStackDepth", (Func<FAFrame, int>)((FAFrame x) => x.BackStackDepth), (Action<FAFrame, int>)null, 0, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFrame.CanGoBack" /> property
	/// </summary>
	public static readonly DirectProperty<FAFrame, bool> CanGoBackProperty = AvaloniaProperty.RegisterDirect<FAFrame, bool>("CanGoBack", (Func<FAFrame, bool>)((FAFrame x) => x.CanGoBack), (Action<FAFrame, bool>)null, false, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFrame.CanGoForward" /> property
	/// </summary>
	public static readonly DirectProperty<FAFrame, bool> CanGoForwardProperty = AvaloniaProperty.RegisterDirect<FAFrame, bool>("CanGoForward", (Func<FAFrame, bool>)((FAFrame x) => x.CanGoForward), (Action<FAFrame, bool>)null, false, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFrame.CurrentSourcePageType" /> property
	/// </summary>
	public static readonly DirectProperty<FAFrame, Type> CurrentSourcePageTypeProperty = AvaloniaProperty.RegisterDirect<FAFrame, Type>("CurrentSourcePageType", (Func<FAFrame, Type>)((FAFrame x) => x.CurrentSourcePageType), (Action<FAFrame, Type>)null, (Type)null, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFrame.BackStack" /> property
	/// </summary>
	public static readonly DirectProperty<FAFrame, IList<FAPageStackEntry>> BackStackProperty = AvaloniaProperty.RegisterDirect<FAFrame, IList<FAPageStackEntry>>("BackStack", (Func<FAFrame, IList<FAPageStackEntry>>)((FAFrame x) => x.BackStack), (Action<FAFrame, IList<FAPageStackEntry>>)null, (IList<FAPageStackEntry>)null, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFrame.ForwardStack" /> property
	/// </summary>
	public static readonly DirectProperty<FAFrame, IList<FAPageStackEntry>> ForwardStackProperty = AvaloniaProperty.RegisterDirect<FAFrame, IList<FAPageStackEntry>>("ForwardStack", (Func<FAFrame, IList<FAPageStackEntry>>)((FAFrame x) => x.ForwardStack), (Action<FAFrame, IList<FAPageStackEntry>>)null, (IList<FAPageStackEntry>)null, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFrame.IsNavigationStackEnabled" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsNavigationStackEnabledProperty = AvaloniaProperty.Register<FAFrame, bool>("IsNavigationStackEnabled", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFrame.NavigationPageFactory" /> property
	/// </summary>
	public static readonly DirectProperty<FAFrame, IFANavigationPageFactory> NavigationPageFactoryProperty = AvaloniaProperty.RegisterDirect<FAFrame, IFANavigationPageFactory>("NavigationPageFactory", (Func<FAFrame, IFANavigationPageFactory>)((FAFrame x) => x.NavigationPageFactory), (Action<FAFrame, IFANavigationPageFactory>)delegate(FAFrame x, IFANavigationPageFactory v)
	{
		x.NavigationPageFactory = v;
	}, (IFANavigationPageFactory)null, (BindingMode)1, false);

	/// <summary>
	/// Indicates to a page that it is being navigated away from. Takes the place of 
	/// Microsoft.UI.Xaml.Controls.Page.OnNavigatingFrom() method
	/// </summary>
	public static readonly RoutedEvent<FANavigatingCancelEventArgs> NavigatingFromEvent = RoutedEvent.Register<Control, FANavigatingCancelEventArgs>("NavigatingFrom", (RoutingStrategies)1);

	/// <summary>
	/// Indiates to a page that it has been navigated away from. Takes the place of
	/// Microsoft.UI.Xaml.Controls.Page.OnNavigatedFrom() method
	/// </summary>
	public static readonly RoutedEvent<FANavigationEventArgs> NavigatedFromEvent = RoutedEvent.Register<Control, FANavigationEventArgs>("NavigatedFrom", (RoutingStrategies)1);

	/// <summary>
	/// Indiates to a page that it is being navigated to. Takes the place of
	/// Microsoft.UI.Xaml.Controls.Page.OnNavigatedTo() method
	/// </summary>
	public static readonly RoutedEvent<FANavigationEventArgs> NavigatedToEvent = RoutedEvent.Register<Control, FANavigationEventArgs>("NavigatedTo", (RoutingStrategies)1);

	private IList<FAPageStackEntry> _backStack;

	private IList<FAPageStackEntry> _forwardStack;

	private IFANavigationPageFactory _pageFactory;

	/// <summary>
	/// Gets or sets a type reference of the current content, or the content that should be navigated to.
	/// </summary>
	/// <remarks>
	/// Do not use this method with trimming/aot! Use <see cref="M:FluentAvalonia.UI.Controls.FAFrame.Navigate(System.Type)" /> instead
	/// </remarks>
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	public Type SourcePageType
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<Type>(SourcePageTypeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<Type>(SourcePageTypeProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the number of pages in the navigation history that can be cached for the frame.
	/// </summary>
	public int CacheSize
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<int>(CacheSizeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<int>(CacheSizeProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the number of entries in the navigation back stack.
	/// </summary>
	public int BackStackDepth => _backStack.Count;

	/// <summary>
	/// Gets a value that indicates whether there is at least one entry in back navigation history.
	/// </summary>
	public bool CanGoBack => _backStack.Count > 0;

	/// <summary>
	/// Gets a value that indicates whether there is at least one entry in forward navigation history.
	/// </summary>
	public bool CanGoForward => _forwardStack.Count > 0;

	/// <summary>
	/// Gets a type reference for the content that is currently displayed.
	/// </summary>
	public Type CurrentSourcePageType => ((ContentControl)this).Content?.GetType();

	/// <summary>
	/// Gets a collection of <see cref="T:FluentAvalonia.UI.Navigation.FAPageStackEntry" /> instances representing the 
	/// backward navigation history of the Frame.
	/// </summary>
	public IList<FAPageStackEntry> BackStack
	{
		get
		{
			return _backStack;
		}
		private set
		{
			((AvaloniaObject)this).SetAndRaise<IList<FAPageStackEntry>>((DirectPropertyBase<IList<FAPageStackEntry>>)(object)BackStackProperty, ref _backStack, value);
		}
	}

	/// <summary>
	/// Gets a collection of <see cref="T:FluentAvalonia.UI.Navigation.FAPageStackEntry" /> instances representing the 
	/// forward navigation history of the Frame.
	/// </summary>
	public IList<FAPageStackEntry> ForwardStack
	{
		get
		{
			return _forwardStack;
		}
		private set
		{
			((AvaloniaObject)this).SetAndRaise<IList<FAPageStackEntry>>((DirectPropertyBase<IList<FAPageStackEntry>>)(object)ForwardStackProperty, ref _forwardStack, value);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether navigation is recorded in the Frame's 
	/// <see cref="P:FluentAvalonia.UI.Controls.FAFrame.ForwardStack" /> or <see cref="P:FluentAvalonia.UI.Controls.FAFrame.BackStack" />.
	/// </summary>
	public bool IsNavigationStackEnabled
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsNavigationStackEnabledProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsNavigationStackEnabledProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the user specified factory that should be use for resolving pages
	/// when types are not controls or from object instances directly
	/// </summary>
	public IFANavigationPageFactory NavigationPageFactory
	{
		get
		{
			return _pageFactory;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<IFANavigationPageFactory>((DirectPropertyBase<IFANavigationPageFactory>)(object)NavigationPageFactoryProperty, ref _pageFactory, value);
		}
	}

	internal FAPageStackEntry CurrentEntry { get; set; }

	/// <summary>
	/// Occurs when the content that is being navigated to has been found and is available 
	/// from the Content property, although it may not have completed loading.
	/// </summary>
	public event FANavigatedEventHandler Navigated;

	/// <summary>
	/// Occurs when a new navigation is requested.
	/// </summary>
	public event FANavigatingCancelEventHandler Navigating;

	/// <summary>
	/// Occurs when an error is raised while navigating to the requested content.
	/// </summary>
	public event FANavigationFailedEventHandler NavigationFailed;

	/// <summary>
	/// Occurs when a new navigation is requested while a current navigation is in progress.
	/// </summary>
	public event FANavigationStoppedEventHandler NavigationStopped;

	public FAFrame()
	{
		AvaloniaList<FAPageStackEntry> val = new AvaloniaList<FAPageStackEntry>();
		AvaloniaList<FAPageStackEntry> val2 = new AvaloniaList<FAPageStackEntry>();
		val.CollectionChanged += OnBackStackChanged;
		val2.CollectionChanged += OnForwardStackChanged;
		BackStack = (IList<FAPageStackEntry>)val;
		ForwardStack = (IList<FAPageStackEntry>)val2;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((ContentControl)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)ContentControl.ContentProperty)
		{
			if (change.NewValue == null)
			{
				CurrentEntry = null;
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)SourcePageTypeProperty)
		{
			if (!_isNavigating)
			{
				Type sourcePageType = SourcePageType;
				if ((object)sourcePageType == null)
				{
					throw new InvalidOperationException("SourcePageType cannot be null. Use Content instead.");
				}
				Navigate(sourcePageType);
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)IsNavigationStackEnabledProperty && !AvaloniaPropertyChangedExtensions.GetNewValue<bool>(change))
		{
			_backStack.Clear();
			_forwardStack.Clear();
			_pageCache.Clear();
		}
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		((TemplatedControl)this).OnApplyTemplate(e);
		_presenter = NameScopeExtensions.Find<ContentPresenter>(e.NameScope, "ContentPresenter");
	}

	protected override bool RegisterContentPresenter(ContentPresenter presenter)
	{
		if (((StyledElement)presenter).Name == "ContentPresenter")
		{
			return true;
		}
		return ((ContentControl)this).RegisterContentPresenter(presenter);
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		((Visual)this).OnAttachedToVisualTree(e);
		TopLevel topLevel = TopLevel.GetTopLevel((Visual)(object)this);
		if (topLevel != null)
		{
			topLevel.BackRequested += OnTopLevelBackRequested;
		}
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		((Visual)this).OnDetachedFromVisualTree(e);
		TopLevel topLevel = TopLevel.GetTopLevel((Visual)(object)this);
		if (topLevel != null)
		{
			topLevel.BackRequested -= OnTopLevelBackRequested;
		}
	}

	/// <summary>
	/// Navigates to the most recent item in back navigation history, if a Frame manages its own navigation history.
	/// </summary>
	public void GoBack()
	{
		GoBack(null);
	}

	/// <summary>
	/// Navigates to the most recent item in back navigation history, if a Frame manages its own navigation history, 
	/// and specifies the animated transition to use.
	/// </summary>
	/// <param name="infoOverride">Info about the animated transition to use.</param>
	public void GoBack(FANavigationTransitionInfo infoOverride)
	{
		if (CanGoBack)
		{
			FAPageStackEntry fAPageStackEntry = _backStack[_backStack.Count - 1];
			if (infoOverride != null)
			{
				fAPageStackEntry.NavigationTransitionInfo = infoOverride;
			}
			else
			{
				fAPageStackEntry.NavigationTransitionInfo = CurrentEntry?.NavigationTransitionInfo ?? null;
			}
			NavigateCore(fAPageStackEntry, FANavigationMode.Back);
		}
	}

	/// <summary>
	/// Navigates to the most recent item in forward navigation history, if a Frame manages its own navigation history.
	/// </summary>
	public void GoForward()
	{
		if (CanGoForward)
		{
			NavigateCore(_forwardStack[_forwardStack.Count - 1], FANavigationMode.Forward);
		}
	}

	/// <summary>
	/// Causes the Frame to load content represented by the specified Page.
	/// </summary>
	/// <param name="sourcePageType">The page (IControl) to navigate to, specified as a type reference to its class type, or 
	/// if a <see cref="P:FluentAvalonia.UI.Controls.FAFrame.NavigationPageFactory" /> this can be any type (e.g., a ViewModel)</param>
	/// <returns><c>false</c> if a <see cref="E:FluentAvalonia.UI.Controls.FAFrame.NavigationFailed" /> event handler has set Handled to true; 
	/// otherwise, <c>true</c>.</returns>
	public bool Navigate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type sourcePageType)
	{
		return Navigate(sourcePageType, null, null);
	}

	/// <summary>
	/// Causes the Frame to load content represented by the specified Page, also passing a parameter to be 
	/// interpreted by the target of the navigation.
	/// </summary>
	/// <param name="sourcePageType">The page (IControl) to navigate to, specified as a type reference to its class type, or 
	/// if a <see cref="P:FluentAvalonia.UI.Controls.FAFrame.NavigationPageFactory" /> this can be any type (e.g., a ViewModel)</param>
	/// <param name="parameter">The navigation parameter to pass to the target page; 
	/// must have a basic type (string, char, numeric, or GUID) to support parameter serialization
	/// using GetNavigationState.</param>
	/// <returns><c>false</c> if a <see cref="E:FluentAvalonia.UI.Controls.FAFrame.NavigationFailed" /> event handler has set Handled to true; 
	/// otherwise, <c>true</c>.</returns>
	public bool Navigate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type sourcePageType, object parameter)
	{
		return Navigate(sourcePageType, parameter, null);
	}

	/// <summary>
	/// Causes the Frame to load content represented by the specified Page -derived data type, 
	/// also passing a parameter to be interpreted by the target of the navigation, and a value 
	/// indicating the animated transition to use.
	/// </summary>
	/// <param name="sourcePageType">The page (IControl) to navigate to, specified as a type reference to its class type, or 
	/// if a <see cref="P:FluentAvalonia.UI.Controls.FAFrame.NavigationPageFactory" /> this can be any type (e.g., a ViewModel)</param>
	/// <param name="parameter">The navigation parameter to pass to the target page; must have a 
	/// basic type (string, char, numeric, or GUID) to support parameter serialization using 
	/// GetNavigationState.</param>
	/// <param name="infoOverride">Info about the animated transition.</param>
	/// <returns><c>false</c> if a <see cref="E:FluentAvalonia.UI.Controls.FAFrame.NavigationFailed" /> event handler has set Handled to true; 
	/// otherwise, <c>true</c>.</returns>
	public bool Navigate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type sourcePageType, object parameter, FANavigationTransitionInfo infoOverride)
	{
		return NavigateCore(new FAPageStackEntry(sourcePageType, parameter, infoOverride), FANavigationMode.New);
	}

	/// <summary>
	/// Causes the Frame to load content represented by the specified Page, also passing a parameter to be 
	/// interpreted by the target of the navigation.
	/// </summary>
	/// <param name="sourcePageType">The page (IControl) to navigate to, specified as a type reference to its class type, or 
	/// if a <see cref="P:FluentAvalonia.UI.Controls.FAFrame.NavigationPageFactory" /> this can be any type (e.g., a ViewModel)</param>
	/// <param name="parameter">The navigation parameter to pass to the target page; must have a basic type 
	/// (string, char, numeric, or GUID) to support parameter serialization using GetNavigationState.</param>
	/// <param name="navOptions">Options for the navigation, including whether it is recorded in the navigation stack 
	/// and what transition animation is used.</param>
	/// <returns><c>false</c> if a <see cref="E:FluentAvalonia.UI.Controls.FAFrame.NavigationFailed" /> event handler has set Handled to true; 
	/// otherwise, <c>true</c>.</returns>
	public bool NavigateToType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type sourcePageType, object parameter, FAFrameNavigationOptions navOptions)
	{
		return NavigateCore(new FAPageStackEntry(sourcePageType, parameter, navOptions?.TransitionInfoOverride), FANavigationMode.New, navOptions);
	}

	/// <summary>
	/// Causes the frame to load content represented by the specified target property with the
	/// specified navigation options
	/// </summary>
	/// <remarks>
	/// You must specify a <see cref="P:FluentAvalonia.UI.Controls.FAFrame.NavigationPageFactory" /> for this method to succeed
	/// </remarks>
	/// <param name="target">An existing object for which page creation should be based (e.g., A ViewModel instance)</param>
	/// <param name="navOptions">Options for the navigation, including whether it is recorded in the navigation stack 
	/// and what transition animation is used.</param>
	/// <returns><c>false</c> if a <see cref="E:FluentAvalonia.UI.Controls.FAFrame.NavigationFailed" /> event handler has set Handled to true or
	/// if <see cref="P:FluentAvalonia.UI.Controls.FAFrame.NavigationPageFactory" /> is not specified; otherwise, <c>true</c>.</returns>
	public bool NavigateFromObject<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(T target, FAFrameNavigationOptions navOptions = null)
	{
		Control val = CheckCacheAndGetPage(null, target);
		if (val == null)
		{
			val = NavigationPageFactory.GetPageFromObject(target);
			if (val == null)
			{
				return false;
			}
		}
		FAPageStackEntry entry = new FAPageStackEntry(typeof(T), null, navOptions?.TransitionInfoOverride)
		{
			Instance = val,
			Context = target
		};
		return NavigateCore(entry, FANavigationMode.New, navOptions);
	}

	/// <summary>
	/// Serializes the Frame navigation history into a string
	/// </summary>
	/// <returns></returns>
	public string GetNavigationState()
	{
		if (!IsNavigationStackEnabled)
		{
			throw new InvalidOperationException("Cannot retreive navigation stack when IsNavigationStackEnabled is false");
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (CurrentEntry != null)
		{
			AppendEntry(stringBuilder, CurrentEntry);
		}
		stringBuilder.AppendLine(BackStackDepth.ToString());
		for (int i = 0; i < BackStackDepth; i++)
		{
			AppendEntry(stringBuilder, BackStack[i]);
		}
		stringBuilder.AppendLine(ForwardStack.Count.ToString());
		for (int j = 0; j < ForwardStack.Count; j++)
		{
			AppendEntry(stringBuilder, ForwardStack[j]);
		}
		return stringBuilder.ToString();
		static void AppendEntry(StringBuilder sb, FAPageStackEntry entry)
		{
			sb.Append(entry.SourcePageType.AssemblyQualifiedName);
			sb.Append('|');
			if (entry.Parameter != null)
			{
				sb.Append(entry.Parameter.ToString());
			}
			sb.AppendLine();
		}
	}

	/// <summary>
	/// Reads and restores the navigation history of a Frame from a provided serialization string.
	/// </summary>
	/// <param name="navState">The serialization string that supplies the restore point for navigation history.</param>
	[RequiresUnreferencedCode("Resolves navigation targets from the navState string.")]
	public void SetNavigationState(string navState)
	{
		SetNavigationState(navState, suppressNavigate: false);
	}

	/// <summary>
	/// Reads and restores the navigation history of a Frame from a provided serialization string,
	/// and optionally supresses navigation to the last page type
	/// </summary>
	/// <param name="navState">The serialization string that supplies the restore point for navigation history.</param>
	/// <param name="suppressNavigate">true to restore navigation history without navigating to the current page; otherwise, false.</param>
	/// <remarks>
	/// Calling SetNavigationState with suppressNavigate set to true, OnNavigatedTo is not called and the current page is placed into
	/// the BackStack
	/// </remarks>
	[RequiresUnreferencedCode("Resolves navigation targets from the navState string.")]
	public void SetNavigationState(string navState, bool suppressNavigate)
	{
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		if (!IsNavigationStackEnabled)
		{
			throw new InvalidOperationException("Cannot set navigation stack when IsNavigationStackEnabled is false");
		}
		BackStack.Clear();
		ForwardStack.Clear();
		CurrentEntry = null;
		((ContentControl)this).Content = null;
		_pageCache.Clear();
		using StringReader stringReader = new StringReader(navState);
		string text = stringReader.ReadLine();
		bool flag = false;
		if (text[0] != '|')
		{
			int num = text.IndexOf('|');
			Type type = Type.GetType(text.Substring(0, num));
			string text2 = text.Substring(num + 1);
			CurrentEntry = new FAPageStackEntry(type, text2, null);
			if (!suppressNavigate)
			{
				Control val = CreatePageAndCacheIfNecessary(type);
				CurrentEntry.Instance = val;
				SetContentAndAnimate(CurrentEntry);
				FANavigationEventArgs e = new FANavigationEventArgs(val, FANavigationMode.New, null, text2, type);
				((RoutedEventArgs)e).RoutedEvent = (RoutedEvent)(object)NavigatedToEvent;
				((Interactive)val).RaiseEvent((RoutedEventArgs)(object)e);
			}
			else
			{
				flag = true;
			}
		}
		int num2 = int.Parse(stringReader.ReadLine());
		ParametrizedLogger valueOrDefault;
		for (int i = 0; i < num2; i++)
		{
			string text3 = stringReader.ReadLine();
			int num3 = text3.IndexOf('|');
			Type type2 = Type.GetType(text3.Substring(0, num3));
			if (type2 == null)
			{
				ParametrizedLogger? val2 = Logger.TryGet((LogEventLevel)4, "Frame");
				if (val2.HasValue)
				{
					valueOrDefault = val2.GetValueOrDefault();
					((ParametrizedLogger)(ref valueOrDefault)).Log((object)"Frame", "Attempting to parse the type '" + text3.Substring(0, num3) + "' failed. Page was skipped");
				}
			}
			else
			{
				string parameter = text3.Substring(num3 + 1);
				FAPageStackEntry item = new FAPageStackEntry(type2, parameter, null);
				BackStack.Add(item);
			}
		}
		if (flag)
		{
			CurrentEntry.Instance = null;
			BackStack.Add(CurrentEntry);
			CurrentEntry = null;
		}
		int num4 = int.Parse(stringReader.ReadLine());
		for (int j = 0; j < num4; j++)
		{
			string text4 = stringReader.ReadLine();
			int num5 = text4.IndexOf('|');
			Type type3 = Type.GetType(text4.Substring(0, num5));
			string parameter2 = text4.Substring(num5 + 1);
			if (type3 == null)
			{
				ParametrizedLogger? val2 = Logger.TryGet((LogEventLevel)4, "Frame");
				if (val2.HasValue)
				{
					valueOrDefault = val2.GetValueOrDefault();
					((ParametrizedLogger)(ref valueOrDefault)).Log((object)"Frame", "Attempting to parse the type '" + text4.Substring(0, num5) + "' failed. Page was skipped");
				}
			}
			else
			{
				FAPageStackEntry item2 = new FAPageStackEntry(type3, parameter2, null);
				ForwardStack.Add(item2);
			}
		}
	}

	private bool NavigateCore(FAPageStackEntry entry, FANavigationMode mode, FAFrameNavigationOptions options = null)
	{
		//IL_02e8: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			_isNavigating = true;
			FANavigatingCancelEventArgs e = new FANavigatingCancelEventArgs(mode, entry.NavigationTransitionInfo, entry.Parameter, entry.SourcePageType);
			Navigating?.Invoke(this, e);
			if (e.Cancel)
			{
				OnNavigationStopped(entry, mode);
				return false;
			}
			Control val = CurrentEntry?.Instance;
			if (val != null)
			{
				((RoutedEventArgs)e).RoutedEvent = (RoutedEvent)(object)NavigatingFromEvent;
				((Interactive)val).RaiseEvent((RoutedEventArgs)(object)e);
				if (e.Cancel)
				{
					OnNavigationStopped(entry, mode);
					return false;
				}
			}
			bool flag = entry.Instance != null;
			if (!flag)
			{
				if (entry.Context != null)
				{
					entry.Instance = CheckCacheAndGetPage(null, entry.Context);
				}
				else
				{
					entry.Instance = CheckCacheAndGetPage(entry.SourcePageType);
				}
			}
			if (entry.Instance == null)
			{
				Control val2 = CreatePageAndCacheIfNecessary(entry.SourcePageType);
				if (val2 == null)
				{
					throw new ArgumentException($"The type {entry.SourcePageType} is not a valid page type.");
				}
				entry.Instance = val2;
			}
			else if (flag)
			{
				TryAddToCache(entry.Context, entry.Instance);
			}
			FAPageStackEntry currentEntry = CurrentEntry;
			CurrentEntry = entry;
			FANavigationEventArgs navEA = new FANavigationEventArgs(entry.Instance, mode, entry.NavigationTransitionInfo, entry.Parameter, entry.SourcePageType);
			if (currentEntry != null)
			{
				((RoutedEventArgs)navEA).RoutedEvent = (RoutedEvent)(object)NavigatedFromEvent;
				((Interactive)currentEntry.Instance).RaiseEvent((RoutedEventArgs)(object)navEA);
				currentEntry.Instance = null;
			}
			SetContentAndAnimate(entry);
			if (options?.IsNavigationStackEnabled ?? IsNavigationStackEnabled)
			{
				switch (mode)
				{
				case FANavigationMode.New:
					ForwardStack.Clear();
					if (currentEntry != null)
					{
						BackStack.Add(currentEntry);
					}
					break;
				case FANavigationMode.Back:
					ForwardStack.Add(currentEntry);
					BackStack.Remove(entry);
					break;
				case FANavigationMode.Forward:
					BackStack.Add(currentEntry);
					ForwardStack.Remove(entry);
					break;
				}
			}
			SourcePageType = entry.SourcePageType;
			Navigated?.Invoke(this, navEA);
			Dispatcher.UIThread.Post((Action)delegate
			{
				Control instance = entry.Instance;
				if (instance != null)
				{
					((RoutedEventArgs)navEA).RoutedEvent = (RoutedEvent)(object)NavigatedToEvent;
					((Interactive)instance).RaiseEvent((RoutedEventArgs)(object)navEA);
				}
			}, DispatcherPriority.Render);
			return true;
		}
		catch (Exception ex)
		{
			NavigationFailed?.Invoke(this, new FANavigationFailedEventArgs(ex, entry.SourcePageType));
			return false;
		}
		finally
		{
			_isNavigating = false;
		}
	}

	private void OnNavigationStopped(FAPageStackEntry entry, FANavigationMode mode)
	{
		NavigationStopped?.Invoke(this, new FANavigationEventArgs(entry.Instance, mode, entry.NavigationTransitionInfo, entry.Parameter, entry.SourcePageType));
	}

	private void OnForwardStackChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		bool flag = _forwardStack.Count - (e.NewItems?.Count ?? 0) + (e.OldItems?.Count ?? 0) > 0;
		bool flag2 = _forwardStack.Count > 0;
		((AvaloniaObject)this).RaisePropertyChanged<bool>((DirectPropertyBase<bool>)(object)CanGoForwardProperty, flag, flag2);
	}

	private void OnBackStackChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		int num = _backStack.Count - (e.NewItems?.Count ?? 0) + (e.OldItems?.Count ?? 0);
		bool flag = num > 0;
		bool flag2 = _backStack.Count > 0;
		((AvaloniaObject)this).RaisePropertyChanged<bool>((DirectPropertyBase<bool>)(object)CanGoBackProperty, flag, flag2);
		((AvaloniaObject)this).RaisePropertyChanged<int>((DirectPropertyBase<int>)(object)BackStackDepthProperty, num, _backStack.Count);
	}

	private Control CreatePageAndCacheIfNecessary([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type srcPageType)
	{
		if (CacheSize == 0)
		{
			object obj = NavigationPageFactory?.GetPage(srcPageType);
			if (obj == null)
			{
				object? obj2 = Activator.CreateInstance(srcPageType);
				obj = ((obj2 is Control) ? obj2 : null);
			}
			return (Control)obj;
		}
		for (int i = 0; i < _pageCache.Count; i++)
		{
			if (_pageCache[i].PageSrcType == srcPageType)
			{
				throw new Exception($"An object of type {srcPageType} has already been added to the Navigation Stack");
			}
		}
		object obj3 = NavigationPageFactory?.GetPage(srcPageType);
		if (obj3 == null)
		{
			object? obj4 = Activator.CreateInstance(srcPageType);
			obj3 = ((obj4 is Control) ? obj4 : null);
		}
		Control val = (Control)obj3;
		_pageCache.Add(new NavigationCacheItem(srcPageType, null, val));
		if (_pageCache.Count > CacheSize)
		{
			_pageCache.RemoveAt(0);
		}
		return val;
	}

	private Control CheckCacheAndGetPage(Type srcPageType = null, object target = null)
	{
		if (CacheSize == 0)
		{
			return null;
		}
		for (int num = _pageCache.Count - 1; num >= 0; num--)
		{
			NavigationCacheItem navigationCacheItem = _pageCache[num];
			if (srcPageType != null && navigationCacheItem.PageSrcType == srcPageType)
			{
				return navigationCacheItem.Page;
			}
			if (target != null && navigationCacheItem.Context == target)
			{
				return navigationCacheItem.Page;
			}
		}
		return null;
	}

	private void TryAddToCache(object context, Control page)
	{
		for (int num = _pageCache.Count - 1; num >= 0; num--)
		{
			NavigationCacheItem navigationCacheItem = _pageCache[num];
			if (context != null && navigationCacheItem.Context == context)
			{
				return;
			}
		}
		_pageCache.Add(new NavigationCacheItem(null, context, page));
		if (_pageCache.Count > CacheSize)
		{
			_pageCache.RemoveAt(0);
		}
	}

	private void SetContentAndAnimate(FAPageStackEntry entry)
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		if (entry == null)
		{
			return;
		}
		((ContentControl)this).Content = entry.Instance;
		if (_presenter != null)
		{
			entry.NavigationTransitionInfo = entry.NavigationTransitionInfo ?? new FAEntranceNavigationTransitionInfo();
			((Visual)_presenter).Opacity = 0.0;
			_cts?.Cancel();
			_cts = new CancellationTokenSource();
			Dispatcher.UIThread.Post((Action)delegate
			{
				entry.NavigationTransitionInfo.RunAnimation((Animatable)(object)_presenter, _cts.Token);
			}, DispatcherPriority.Render);
		}
	}

	private void OnTopLevelBackRequested(object sender, RoutedEventArgs e)
	{
		if (!e.Handled && IsNavigationStackEnabled && CanGoBack)
		{
			GoBack();
			e.Handled = true;
		}
	}
}
