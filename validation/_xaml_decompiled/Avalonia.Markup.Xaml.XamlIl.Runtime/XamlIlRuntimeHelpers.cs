using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Platform;

namespace Avalonia.Markup.Xaml.XamlIl.Runtime;

public static class XamlIlRuntimeHelpers
{
	private sealed class LastParentStack
	{
		private readonly WeakReference<IAvaloniaXamlIlParentStackProvider?> _parentStackProvider = new WeakReference<IAvaloniaXamlIlParentStackProvider>(null);

		private readonly WeakReference<IResourceNode[]?> _resourceNodes = new WeakReference<IResourceNode[]>(null);

		public void Set(IAvaloniaXamlIlParentStackProvider parentStackProvider, IResourceNode[] resourceNodes)
		{
			_parentStackProvider.SetTarget(parentStackProvider);
			_resourceNodes.SetTarget(resourceNodes);
		}

		public bool IsEquivalentTo(IAvaloniaXamlIlParentStackProvider parentStackProvider, List<IResourceNode> resourceNodes, [NotNullWhen(true)] out IResourceNode[]? cachedResourceNodes)
		{
			if (!_parentStackProvider.TryGetTarget(out IAvaloniaXamlIlParentStackProvider target) || !_resourceNodes.TryGetTarget(out IResourceNode[] target2) || parentStackProvider != target || resourceNodes.Count != target2.Length)
			{
				cachedResourceNodes = null;
				return false;
			}
			if (!((ReadOnlySpan<IResourceNode>)CollectionsMarshal.AsSpan(resourceNodes)).SequenceEqual((ReadOnlySpan<IResourceNode>)target2, (IEqualityComparer<IResourceNode>?)null))
			{
				cachedResourceNodes = null;
				return false;
			}
			cachedResourceNodes = target2;
			return true;
		}
	}

	private abstract class DeferredContent<T> : IDeferredContent
	{
		private readonly INameScope? _parentNameScope;

		private readonly object _rootObject;

		private readonly IResourceNode[] _parentResourceNodes;

		protected DeferredContent(IResourceNode[] parentResourceNodes, object rootObject, INameScope? parentNameScope)
		{
			_parentNameScope = parentNameScope;
			_parentResourceNodes = parentResourceNodes;
			_rootObject = rootObject;
		}

		public object Build(IServiceProvider? serviceProvider)
		{
			INameScope nameScope2;
			if (_parentNameScope != null)
			{
				INameScope nameScope = new ChildNameScope(_parentNameScope);
				nameScope2 = nameScope;
			}
			else
			{
				INameScope nameScope = new NameScope();
				nameScope2 = nameScope;
			}
			INameScope nameScope3 = nameScope2;
			object obj = InvokeBuilder(new DeferredParentServiceProvider(serviceProvider, _parentResourceNodes, _rootObject, nameScope3));
			nameScope3.Complete();
			return new TemplateResult<T>((T)obj, nameScope3);
		}

		protected abstract object InvokeBuilder(IServiceProvider serviceProvider);
	}

	private sealed class PointerDeferredContent<T> : DeferredContent<T>
	{
		private unsafe readonly delegate*<IServiceProvider, object> _builder;

		public unsafe PointerDeferredContent(IResourceNode[] parentResourceNodes, object rootObject, INameScope? parentNameScope, delegate*<IServiceProvider, object> builder)
			: base(parentResourceNodes, rootObject, parentNameScope)
		{
			_builder = builder;
		}

		protected unsafe override object InvokeBuilder(IServiceProvider serviceProvider)
		{
			return _builder(serviceProvider);
		}
	}

	private sealed class DelegateDeferredContent<T> : DeferredContent<T>
	{
		private readonly Func<IServiceProvider, object> _builder;

		public DelegateDeferredContent(IResourceNode[] parentResourceNodes, object rootObject, INameScope? parentNameScope, Func<IServiceProvider, object> builder)
			: base(parentResourceNodes, rootObject, parentNameScope)
		{
			_builder = builder;
		}

		protected override object InvokeBuilder(IServiceProvider serviceProvider)
		{
			return _builder(serviceProvider);
		}
	}

	private sealed class DeferredParentServiceProvider : IAvaloniaXamlIlEagerParentStackProvider, IAvaloniaXamlIlParentStackProvider, IServiceProvider, IRootObjectProvider, IAvaloniaXamlIlControlTemplateProvider
	{
		private readonly IServiceProvider? _parentProvider;

		private readonly IResourceNode[] _parentResourceNodes;

		private readonly INameScope _nameScope;

		private IRuntimePlatform? _runtimePlatform;

		private Optional<IAvaloniaXamlIlEagerParentStackProvider?> _parentStackProvider;

		public object RootObject { get; }

		public object IntermediateRootObject => RootObject;

		public IEnumerable<object> Parents => _parentResourceNodes.AsEnumerable().Reverse();

		public IReadOnlyList<object> DirectParentsStack => _parentResourceNodes;

		public IAvaloniaXamlIlEagerParentStackProvider? ParentProvider
		{
			get
			{
				if (!_parentStackProvider.HasValue)
				{
					_parentStackProvider = new Optional<IAvaloniaXamlIlEagerParentStackProvider>(GetParentStackProviderFromServices());
				}
				return _parentStackProvider.GetValueOrDefault();
			}
		}

		public DeferredParentServiceProvider(IServiceProvider? parentProvider, IResourceNode[] parentResourceNodes, object rootObject, INameScope nameScope)
		{
			_parentProvider = parentProvider;
			_parentResourceNodes = parentResourceNodes;
			_nameScope = nameScope;
			RootObject = rootObject;
		}

		private IAvaloniaXamlIlEagerParentStackProvider? GetParentStackProviderFromServices()
		{
			return _parentProvider?.GetService<IAvaloniaXamlIlParentStackProvider>() as IAvaloniaXamlIlEagerParentStackProvider;
		}

		public object? GetService(Type serviceType)
		{
			if (serviceType == typeof(INameScope))
			{
				return _nameScope;
			}
			if (serviceType == typeof(IAvaloniaXamlIlParentStackProvider))
			{
				return this;
			}
			if (serviceType == typeof(IRootObjectProvider))
			{
				return this;
			}
			if (serviceType == typeof(IAvaloniaXamlIlControlTemplateProvider))
			{
				return this;
			}
			if (serviceType == typeof(IRuntimePlatform))
			{
				return _runtimePlatform ?? (_runtimePlatform = AvaloniaLocator.Current.GetService<IRuntimePlatform>());
			}
			return _parentProvider?.GetService(serviceType);
		}
	}

	private sealed class InnerServiceProvider : IServiceProvider
	{
		private readonly IServiceProvider _compiledProvider;

		private XamlTypeResolver? _resolver;

		public InnerServiceProvider(IServiceProvider compiledProvider)
		{
			_compiledProvider = compiledProvider;
		}

		public object? GetService(Type serviceType)
		{
			if (serviceType == typeof(IXamlTypeResolver))
			{
				return _resolver ?? (_resolver = new XamlTypeResolver(_compiledProvider.GetRequiredService<IAvaloniaXamlIlXmlNamespaceInfoProvider>()));
			}
			return null;
		}
	}

	private sealed class XamlTypeResolver : IXamlTypeResolver
	{
		private readonly IAvaloniaXamlIlXmlNamespaceInfoProvider _nsInfo;

		public XamlTypeResolver(IAvaloniaXamlIlXmlNamespaceInfoProvider nsInfo)
		{
			_nsInfo = nsInfo;
		}

		[RequiresUnreferencedCode("XamlTypeResolver might require unreferenced code.")]
		public Type Resolve(string qualifiedTypeName)
		{
			string[] array = qualifiedTypeName.Split(new char[1] { ':' }, 2);
			string text3;
			string key;
			if (array.Length != 1)
			{
				string text = array[0];
				string text2 = array[1];
				text3 = text2;
				key = text;
			}
			else
			{
				string text2 = qualifiedTypeName;
				text3 = text2;
				key = "";
			}
			if (!_nsInfo.XmlNamespaces.TryGetValue(key, out IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo> value))
			{
				throw new ArgumentException("Unable to resolve namespace for type " + qualifiedTypeName);
			}
			IEnumerable<AvaloniaXamlIlXmlNamespaceInfo> enumerable = value.Where(delegate(AvaloniaXamlIlXmlNamespaceInfo e)
			{
				string clrAssemblyName = e.ClrAssemblyName;
				return clrAssemblyName != null && clrAssemblyName.Length > 0;
			});
			foreach (AvaloniaXamlIlXmlNamespaceInfo item in enumerable)
			{
				Type type = Assembly.Load(new AssemblyName(item.ClrAssemblyName)).GetType(item.ClrNamespace + "." + text3);
				if (type != null)
				{
					return type;
				}
			}
			throw new ArgumentException("Unable to resolve type " + qualifiedTypeName + " from any of the following locations: " + string.Join(",", enumerable.Select((AvaloniaXamlIlXmlNamespaceInfo e) => $"`clr-namespace:{e.ClrNamespace};assembly={e.ClrAssemblyName}`")))
			{
				HelpLink = "https://docs.avaloniaui.net/docs/basics/user-interface/introduction-to-xaml#xml-namespaces"
			};
		}
	}

	private sealed class RootServiceProvider : IServiceProvider
	{
		private sealed class ApplicationAvaloniaXamlIlParentStackProvider : IAvaloniaXamlIlEagerParentStackProvider, IAvaloniaXamlIlParentStackProvider, IReadOnlyList<object>, IEnumerable<object>, IEnumerable, IReadOnlyCollection<object>
		{
			private static ApplicationAvaloniaXamlIlParentStackProvider? s_lastProvider;

			private static Application? s_lastApplication;

			private readonly Application _application;

			private IEnumerable<object>? _parents;

			public IEnumerable<object> Parents
			{
				get
				{
					IEnumerable<object> enumerable = _parents;
					if (enumerable == null)
					{
						object[] obj = new object[1] { _application };
						IEnumerable<object> enumerable2 = obj;
						_parents = obj;
						enumerable = enumerable2;
					}
					return enumerable;
				}
			}

			public IReadOnlyList<object> DirectParentsStack => this;

			public int Count => 1;

			public object this[int index]
			{
				get
				{
					if (index != 0)
					{
						ThrowArgumentOutOfRangeException();
					}
					return _application;
				}
			}

			public IAvaloniaXamlIlEagerParentStackProvider? ParentProvider => null;

			public static ApplicationAvaloniaXamlIlParentStackProvider GetForApplication(Application application)
			{
				if (application != s_lastApplication)
				{
					s_lastProvider = new ApplicationAvaloniaXamlIlParentStackProvider(application);
					s_lastApplication = application;
				}
				return s_lastProvider;
			}

			public ApplicationAvaloniaXamlIlParentStackProvider(Application application)
			{
				_application = application;
			}

			public IEnumerator<object> GetEnumerator()
			{
				return Parents.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			[DoesNotReturn]
			private static void ThrowArgumentOutOfRangeException()
			{
				throw new ArgumentOutOfRangeException("index");
			}
		}

		private sealed class EmptyAvaloniaXamlIlParentStackProvider : IAvaloniaXamlIlEagerParentStackProvider, IAvaloniaXamlIlParentStackProvider
		{
			public static EmptyAvaloniaXamlIlParentStackProvider Instance { get; } = new EmptyAvaloniaXamlIlParentStackProvider();

			public IEnumerable<object> Parents => Array.Empty<object>();

			public IReadOnlyList<object> DirectParentsStack => Array.Empty<object>();

			public IAvaloniaXamlIlEagerParentStackProvider? ParentProvider => null;

			private EmptyAvaloniaXamlIlParentStackProvider()
			{
			}
		}

		private readonly INameScope _nameScope;

		private readonly IServiceProvider? _parentServiceProvider;

		private readonly IRuntimePlatform? _runtimePlatform;

		private IAvaloniaXamlIlParentStackProvider? _parentStackProvider;

		public RootServiceProvider(INameScope nameScope, IServiceProvider? parentServiceProvider)
		{
			_nameScope = nameScope;
			_parentServiceProvider = parentServiceProvider;
			_runtimePlatform = AvaloniaLocator.Current.GetService<IRuntimePlatform>();
		}

		public object? GetService(Type serviceType)
		{
			if (serviceType == typeof(INameScope))
			{
				return _nameScope;
			}
			if (serviceType == typeof(IAvaloniaXamlIlParentStackProvider))
			{
				return _parentStackProvider ?? (_parentStackProvider = CreateParentStackProvider());
			}
			if (serviceType == typeof(IRuntimePlatform))
			{
				return _runtimePlatform ?? throw new KeyNotFoundException("IRuntimePlatform was not registered");
			}
			return null;
		}

		private IAvaloniaXamlIlParentStackProvider CreateParentStackProvider()
		{
			return _parentServiceProvider?.GetService<IAvaloniaXamlIlParentStackProvider>() ?? GetParentStackProviderForApplication(Application.Current);
		}

		private static IAvaloniaXamlIlEagerParentStackProvider GetParentStackProviderForApplication(Application? application)
		{
			if (application != null)
			{
				return ApplicationAvaloniaXamlIlParentStackProvider.GetForApplication(application);
			}
			return EmptyAvaloniaXamlIlParentStackProvider.Instance;
		}
	}

	[ThreadStatic]
	private static List<IResourceNode>? s_resourceNodeBuffer;

	[ThreadStatic]
	private static LastParentStack? s_lastParentStack;

	public static Func<IServiceProvider, object> DeferredTransformationFactoryV1(Func<IServiceProvider, object> builder, IServiceProvider provider)
	{
		return DeferredTransformationFactoryV2<Control>(builder, provider);
	}

	public static Func<IServiceProvider, object> DeferredTransformationFactoryV2<T>(Func<IServiceProvider, object> builder, IServiceProvider provider)
	{
		IResourceNode[] parentResourceNodes = AsResourceNodesStack(provider.GetRequiredService<IAvaloniaXamlIlParentStackProvider>());
		object rootObject = provider.GetRequiredService<IRootObjectProvider>().RootObject;
		INameScope service = provider.GetService<INameScope>();
		return new DelegateDeferredContent<T>(parentResourceNodes, rootObject, service, builder).Build;
	}

	public unsafe static IDeferredContent DeferredTransformationFactoryV3<T>(nint builder, IServiceProvider provider)
	{
		IResourceNode[] parentResourceNodes = AsResourceNodesStack(provider.GetRequiredService<IAvaloniaXamlIlParentStackProvider>());
		object rootObject = provider.GetRequiredService<IRootObjectProvider>().RootObject;
		INameScope service = provider.GetService<INameScope>();
		return new PointerDeferredContent<T>(parentResourceNodes, rootObject, service, (delegate*<IServiceProvider, object>)builder);
	}

	private static IResourceNode[] AsResourceNodesStack(IAvaloniaXamlIlParentStackProvider provider)
	{
		List<IResourceNode> list = s_resourceNodeBuffer ?? (s_resourceNodeBuffer = new List<IResourceNode>(8));
		list.Clear();
		if (provider is IAvaloniaXamlIlEagerParentStackProvider provider2)
		{
			EagerParentStackEnumerator eagerParentStackEnumerator = new EagerParentStackEnumerator(provider2);
			while (true)
			{
				IResourceNode resourceNode = eagerParentStackEnumerator.TryGetNextOfType<IResourceNode>();
				if (resourceNode == null)
				{
					break;
				}
				list.Add(resourceNode);
			}
		}
		else
		{
			foreach (object parent in provider.Parents)
			{
				if (parent is IResourceNode item)
				{
					list.Add(item);
				}
			}
		}
		list.Reverse();
		LastParentStack lastParentStack = s_lastParentStack;
		if (lastParentStack == null || !lastParentStack.IsEquivalentTo(provider, list, out IResourceNode[] cachedResourceNodes))
		{
			cachedResourceNodes = list.ToArray();
			if (lastParentStack == null)
			{
				lastParentStack = (s_lastParentStack = new LastParentStack());
			}
			lastParentStack.Set(provider, cachedResourceNodes);
		}
		list.Clear();
		return cachedResourceNodes;
	}

	/// <summary>
	/// Converts a <see cref="T:Avalonia.Markup.Xaml.XamlIl.Runtime.IAvaloniaXamlIlParentStackProvider" /> into a
	/// <see cref="T:Avalonia.Markup.Xaml.XamlIl.Runtime.IAvaloniaXamlIlEagerParentStackProvider" />.
	/// </summary>
	public static IAvaloniaXamlIlEagerParentStackProvider AsEagerParentStackProvider(this IAvaloniaXamlIlParentStackProvider provider)
	{
		return (provider as IAvaloniaXamlIlEagerParentStackProvider) ?? new XamlIlParentStackProviderWrapper(provider);
	}

	public static void ApplyNonMatchingMarkupExtensionV1(object target, object property, IServiceProvider prov, object value)
	{
		if (value is BindingBase binding)
		{
			if (property is AvaloniaProperty property2)
			{
				((AvaloniaObject)target).Bind(property2, binding);
				return;
			}
			throw new ArgumentException("Attempt to apply binding to non-avalonia property " + property);
		}
		if (value is UnsetValueType value2)
		{
			if (property is AvaloniaProperty property3)
			{
				((AvaloniaObject)target).SetValue(property3, value2);
			}
			return;
		}
		throw new ArgumentException("Don't know what to do with " + value.GetType());
	}

	public static IServiceProvider CreateInnerServiceProviderV1(IServiceProvider compiled)
	{
		return new InnerServiceProvider(compiled);
	}

	public static IServiceProvider CreateRootServiceProviderV2()
	{
		return new RootServiceProvider(new NameScope(), null);
	}

	public static IServiceProvider CreateRootServiceProviderV3(IServiceProvider? parentServiceProvider)
	{
		return new RootServiceProvider(new NameScope(), parentServiceProvider);
	}
}
