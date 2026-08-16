using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Data.Core.Plugins;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.Parsers;

/// <summary>
/// Visits and processes a LINQ expression to build a compiled binding path.
/// </summary>
/// <typeparam name="TIn">The input parameter type for the binding expression.</typeparam>
/// <remarks>
/// This visitor traverses lambda expressions used in compiled bindings and uses
/// <see cref="T:Avalonia.Data.CompiledBindingPathBuilder" /> to construct a <see cref="T:Avalonia.Data.CompiledBindingPath" />, which
/// can then be converted into <see cref="T:Avalonia.Data.Core.ExpressionNodes.ExpressionNode" /> instances. It supports property access,
/// indexers, AvaloniaProperty access, stream bindings, type casts, and logical operators.
/// </remarks>
[RequiresUnreferencedCode("ExpressionNode might require unreferenced code.")]
internal class BindingExpressionVisitor<TIn>(LambdaExpression expression) : ExpressionVisitor
{
	private class AvaloniaPropertyAccessor : PropertyAccessorBase, IWeakEventSubscriber<AvaloniaPropertyChangedEventArgs>
	{
		private readonly WeakReference<AvaloniaObject?> _reference;

		private readonly AvaloniaProperty _property;

		public override Type PropertyType => _property.PropertyType;

		public override object? Value
		{
			get
			{
				if (!_reference.TryGetTarget(out AvaloniaObject target))
				{
					return null;
				}
				return target?.GetValue(_property);
			}
		}

		public AvaloniaPropertyAccessor(WeakReference<AvaloniaObject?> reference, AvaloniaProperty property)
		{
			_reference = reference ?? throw new ArgumentNullException("reference");
			_property = property ?? throw new ArgumentNullException("property");
		}

		public override bool SetValue(object? value, BindingPriority priority)
		{
			if (!_property.IsReadOnly && _reference.TryGetTarget(out AvaloniaObject target))
			{
				target.SetValue(_property, value, priority);
				return true;
			}
			return false;
		}

		public void OnEvent(object? sender, WeakEvent ev, AvaloniaPropertyChangedEventArgs e)
		{
			if (e.Property == _property)
			{
				PublishValue(Value);
			}
		}

		protected override void SubscribeCore()
		{
			if (_reference.TryGetTarget(out AvaloniaObject target) && target != null)
			{
				PublishValue(target.GetValue(_property));
				WeakEvents.AvaloniaPropertyChanged.Subscribe(target, this);
			}
		}

		protected override void UnsubscribeCore()
		{
			if (_reference.TryGetTarget(out AvaloniaObject target) && target != null)
			{
				WeakEvents.AvaloniaPropertyChanged.Unsubscribe(target, this);
			}
		}
	}

	private class InpcPropertyAccessor : PropertyAccessorBase, IWeakEventSubscriber<PropertyChangedEventArgs>
	{
		protected readonly WeakReference<object?> _reference;

		private readonly IPropertyInfo _property;

		public override Type PropertyType => _property.PropertyType;

		public override object? Value
		{
			get
			{
				if (!_reference.TryGetTarget(out object target))
				{
					return null;
				}
				return _property.Get(target);
			}
		}

		public InpcPropertyAccessor(WeakReference<object?> reference, IPropertyInfo property)
		{
			_reference = reference ?? throw new ArgumentNullException("reference");
			_property = property ?? throw new ArgumentNullException("property");
		}

		public override bool SetValue(object? value, BindingPriority priority)
		{
			if (_property.CanSet && _reference.TryGetTarget(out object target))
			{
				_property.Set(target, value);
				SendCurrentValue();
				return true;
			}
			return false;
		}

		public void OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == _property.Name || string.IsNullOrEmpty(e.PropertyName))
			{
				SendCurrentValue();
			}
		}

		protected override void SubscribeCore()
		{
			SendCurrentValue();
			if (_reference.TryGetTarget(out object target) && target is INotifyPropertyChanged target2)
			{
				WeakEvents.ThreadSafePropertyChanged.Subscribe(target2, this);
			}
		}

		protected override void UnsubscribeCore()
		{
			if (_reference.TryGetTarget(out object target) && target is INotifyPropertyChanged target2)
			{
				WeakEvents.ThreadSafePropertyChanged.Unsubscribe(target2, this);
			}
		}

		protected void SendCurrentValue()
		{
			try
			{
				PublishValue(Value);
			}
			catch (Exception error)
			{
				PublishValue(new BindingNotification(error, BindingErrorType.Error));
			}
		}
	}

	private class IndexerAccessor : InpcPropertyAccessor, IWeakEventSubscriber<NotifyCollectionChangedEventArgs>
	{
		private readonly int _index;

		public IndexerAccessor(WeakReference<object?> target, IPropertyInfo basePropertyInfo, int argument)
			: base(target, basePropertyInfo)
		{
			_index = argument;
		}

		protected override void SubscribeCore()
		{
			base.SubscribeCore();
			if (_reference.TryGetTarget(out object target) && target is INotifyCollectionChanged target2)
			{
				WeakEvents.CollectionChanged.Subscribe(target2, this);
			}
		}

		protected override void UnsubscribeCore()
		{
			base.UnsubscribeCore();
			if (_reference.TryGetTarget(out object target) && target is INotifyCollectionChanged target2)
			{
				WeakEvents.CollectionChanged.Unsubscribe(target2, this);
			}
		}

		public void OnEvent(object? sender, WeakEvent ev, NotifyCollectionChangedEventArgs args)
		{
			if (ShouldNotifyListeners(args))
			{
				SendCurrentValue();
			}
		}

		private bool ShouldNotifyListeners(NotifyCollectionChangedEventArgs e)
		{
			return e.Action switch
			{
				NotifyCollectionChangedAction.Add => _index >= e.NewStartingIndex, 
				NotifyCollectionChangedAction.Remove => _index >= e.OldStartingIndex, 
				NotifyCollectionChangedAction.Replace => _index >= e.NewStartingIndex && _index < e.NewStartingIndex + e.NewItems.Count, 
				NotifyCollectionChangedAction.Move => (_index >= e.NewStartingIndex && _index < e.NewStartingIndex + e.NewItems.Count) || (_index >= e.OldStartingIndex && _index < e.OldStartingIndex + e.OldItems.Count), 
				NotifyCollectionChangedAction.Reset => true, 
				_ => false, 
			};
		}
	}

	private const string IndexerGetterName = "get_Item";

	private const string MultiDimensionalArrayGetterMethodName = "Get";

	private readonly LambdaExpression _rootExpression = expression;

	private readonly CompiledBindingPathBuilder _builder = new CompiledBindingPathBuilder();

	private Expression? _head;

	/// <summary>
	/// Builds a compiled binding path from a lambda expression.
	/// </summary>
	/// <typeparam name="TOut">The output type of the binding expression.</typeparam>
	/// <param name="expression">
	/// The lambda expression to parse and convert into a binding path.
	/// </param>
	/// <returns>
	/// A <see cref="T:Avalonia.Data.CompiledBindingPath" /> representing the binding path.
	/// </returns>
	/// <exception cref="T:Avalonia.Data.Core.ExpressionParseException">
	/// Thrown when the expression contains unsupported operations or invalid syntax for binding
	/// expressions.
	/// </exception>
	public static CompiledBindingPath BuildPath<TOut>(Expression<Func<TIn, TOut>> expression)
	{
		BindingExpressionVisitor<TIn> bindingExpressionVisitor = new BindingExpressionVisitor<TIn>(expression);
		bindingExpressionVisitor.Visit(expression);
		return bindingExpressionVisitor._builder.Build();
	}

	protected override Expression VisitBinary(BinaryExpression node)
	{
		if (node.NodeType == ExpressionType.ArrayIndex)
		{
			return Visit(Expression.MakeIndex(node.Left, null, new _003C_003Ez__ReadOnlySingleElementList<Expression>(node.Right)));
		}
		throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
	}

	protected override Expression VisitIndex(IndexExpression node)
	{
		if (node.Indexer == BindingExpressionVisitorMembers.AvaloniaObjectIndexer)
		{
			AvaloniaProperty property = GetValue<AvaloniaProperty>(node.Arguments[0]);
			return Add(node.Object, node, delegate(CompiledBindingPathBuilder x)
			{
				x.Property(property, CreateAvaloniaPropertyAccessor);
			});
		}
		Expression? expression = node.Object;
		if (expression != null && expression.Type.IsArray)
		{
			int[] indexes = node.Arguments.Select(GetValue<int>).ToArray();
			return Add(node.Object, node, delegate(CompiledBindingPathBuilder x)
			{
				x.ArrayElement(indexes, node.Type);
			});
		}
		if ((object)node.Indexer?.GetMethod != null && node.Arguments.Count == 1 && node.Arguments[0].Type == typeof(int))
		{
			MethodInfo getMethod = node.Indexer.GetMethod;
			MethodInfo setMethod = node.Indexer.SetMethod;
			int index = GetValue<int>(node.Arguments[0]);
			ClrPropertyInfo info = new ClrPropertyInfo("Item", (object x) => getMethod.Invoke(x, new object[1] { index }), ((object)setMethod != null) ? ((Action<object, object>)delegate(object o, object? v)
			{
				setMethod.Invoke(o, new object[2] { index, v });
			}) : null, getMethod.ReturnType);
			return Add(node.Object, node, delegate(CompiledBindingPathBuilder x)
			{
				x.Property(info, (WeakReference<object?> weakRef, IPropertyInfo propInfo) => CreateIndexerPropertyAccessor(weakRef, propInfo, index));
			});
		}
		if ((object)node.Indexer?.GetMethod != null)
		{
			MethodInfo getMethod2 = node.Indexer.GetMethod;
			MethodInfo setMethod2 = node.Indexer?.SetMethod;
			object[] indexes2 = node.Arguments.Select(GetValue<object>).ToArray();
			ClrPropertyInfo info2 = new ClrPropertyInfo("Item", (object x) => getMethod2.Invoke(x, indexes2), ((object)setMethod2 != null) ? ((Action<object, object>)delegate(object o, object? v)
			{
				setMethod2.Invoke(o, indexes2.Append<object>(v).ToArray());
			}) : null, getMethod2.ReturnType);
			return Add(node.Object, node, delegate(CompiledBindingPathBuilder x)
			{
				x.Property(info2, CreateInpcPropertyAccessor);
			});
		}
		throw new ExpressionParseException(0, $"Invalid indexer in binding expression: {node.NodeType}.");
	}

	protected override Expression VisitMember(MemberExpression node)
	{
		if (node.Member.MemberType == MemberTypes.Property)
		{
			return AddPropertyNode(node);
		}
		throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
	}

	protected override Expression VisitMethodCall(MethodCallExpression node)
	{
		MethodInfo method = node.Method;
		if (method.Name == "get_Item" && node.Object != null)
		{
			PropertyInfo indexer = TryGetPropertyFromMethod(method);
			return Visit(Expression.MakeIndex(node.Object, indexer, node.Arguments));
		}
		if (method.Name == "Get" && node.Object != null)
		{
			int[] indexes = node.Arguments.Select(GetValue<int>).ToArray();
			return Add(node.Object, node, delegate(CompiledBindingPathBuilder x)
			{
				x.ArrayElement(indexes, node.Type);
			});
		}
		if (method.Name.StartsWith(StreamBindingExtensions.StreamBindingName) && method.DeclaringType == typeof(StreamBindingExtensions))
		{
			Expression expression = (node.Method.IsStatic ? node.Arguments[0] : node.Object);
			Type type = expression?.Type;
			Type[] genericArguments = method.GetGenericArguments();
			Type type2 = ((genericArguments.Length != 0) ? genericArguments[0] : typeof(object));
			if (type == typeof(Task) || ((object)type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>) && type2.IsAssignableFrom(type.GetGenericArguments()[0])))
			{
				return Add(expression, node, delegate(CompiledBindingPathBuilder x)
				{
					x.StreamTask();
				});
			}
			if ((object)type != null && ObservableStreamPlugin.MatchesType(type))
			{
				return Add(expression, node, delegate(CompiledBindingPathBuilder x)
				{
					x.StreamObservable();
				});
			}
		}
		else if (method == BindingExpressionVisitorMembers.CreateDelegateMethod)
		{
			MethodInfo methodInfo = GetValue<MethodInfo>(node.Object);
			Type delegateType = GetValue<Type>(node.Arguments[0]);
			return Add(node.Arguments[1], node, delegate(CompiledBindingPathBuilder x)
			{
				x.Method(methodInfo.MethodHandle, delegateType.TypeHandle, acceptsNull: false);
			});
		}
		throw new ExpressionParseException(0, $"Invalid method call in binding expression: '{node.Method.DeclaringType}.{node.Method.Name}'.");
	}

	protected override Expression VisitParameter(ParameterExpression node)
	{
		if (node == _rootExpression.Parameters[0] && _head == null)
		{
			_head = node;
		}
		return base.VisitParameter(node);
	}

	protected override Expression VisitUnary(UnaryExpression node)
	{
		if (node.NodeType == ExpressionType.Not && node.Type == typeof(bool))
		{
			return Add(node.Operand, node, delegate(CompiledBindingPathBuilder x)
			{
				x.Not();
			});
		}
		if (node.NodeType == ExpressionType.Convert)
		{
			if (!node.Type.IsValueType && !node.Operand.Type.IsValueType && (node.Type.IsAssignableFrom(node.Operand.Type) || node.Operand.Type.IsAssignableFrom(node.Type)))
			{
				return Add(node.Operand, node, delegate(CompiledBindingPathBuilder x)
				{
					x.TypeCast(node.Type);
				});
			}
		}
		else if (node.NodeType == ExpressionType.TypeAs)
		{
			return Add(node.Operand, node, delegate(CompiledBindingPathBuilder x)
			{
				x.TypeCast(node.Type);
			});
		}
		throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
	}

	protected override Expression VisitBlock(BlockExpression node)
	{
		throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
	}

	protected override CatchBlock VisitCatchBlock(CatchBlock node)
	{
		throw new ExpressionParseException(0, "Catch blocks are not allowed in binding expressions.");
	}

	protected override Expression VisitConditional(ConditionalExpression node)
	{
		throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
	}

	protected override Expression VisitDynamic(DynamicExpression node)
	{
		throw new ExpressionParseException(0, "Dynamic expressions are not allowed in binding expressions.");
	}

	protected override ElementInit VisitElementInit(ElementInit node)
	{
		throw new ExpressionParseException(0, "Element init expressions are not valid in a binding expression.");
	}

	protected override Expression VisitGoto(GotoExpression node)
	{
		throw new ExpressionParseException(0, "Goto expressions are not supported in binding expressions.");
	}

	protected override Expression VisitInvocation(InvocationExpression node)
	{
		throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
	}

	protected override Expression VisitLabel(LabelExpression node)
	{
		throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
	}

	protected override Expression VisitListInit(ListInitExpression node)
	{
		throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
	}

	protected override Expression VisitLoop(LoopExpression node)
	{
		throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
	}

	protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
	{
		throw new ExpressionParseException(0, "Member assignments not supported in binding expressions.");
	}

	protected override Expression VisitSwitch(SwitchExpression node)
	{
		throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
	}

	protected override Expression VisitTry(TryExpression node)
	{
		throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
	}

	protected override Expression VisitTypeBinary(TypeBinaryExpression node)
	{
		throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
	}

	private Expression Add(Expression? instance, Expression expression, Action<CompiledBindingPathBuilder> build)
	{
		Expression expression2 = Visit(instance);
		if (expression2 != _head)
		{
			throw new ExpressionParseException(0, $"Unable to parse '{expression}': expected an instance of '{_head}' but got '{expression2}'.");
		}
		build(_builder);
		return _head = expression;
	}

	private Expression AddPropertyNode(MemberExpression node)
	{
		if (typeof(AvaloniaObject).IsAssignableFrom(node.Expression?.Type))
		{
			AvaloniaProperty avaloniaProperty = AvaloniaPropertyRegistry.Instance.FindRegistered(node.Expression.Type, node.Member.Name);
			if ((object)avaloniaProperty != null)
			{
				return Add(node.Expression, node, delegate(CompiledBindingPathBuilder x)
				{
					x.Property(avaloniaProperty, CreateAvaloniaPropertyAccessor);
				});
			}
		}
		PropertyInfo propertyInfo = (PropertyInfo)node.Member;
		ClrPropertyInfo info = new ClrPropertyInfo(propertyInfo.Name, CreateGetter(propertyInfo), CreateSetter(propertyInfo), propertyInfo.PropertyType);
		return Add(node.Expression, node, delegate(CompiledBindingPathBuilder x)
		{
			x.Property(info, CreateInpcPropertyAccessor);
		});
	}

	private static Func<object, object?>? CreateGetter(PropertyInfo info)
	{
		if (!info.CanRead)
		{
			return null;
		}
		return info.GetValue;
	}

	private static Action<object, object?>? CreateSetter(PropertyInfo info)
	{
		if (!info.CanWrite)
		{
			return null;
		}
		return info.SetValue;
	}

	private static T GetValue<T>(Expression expr)
	{
		if (expr is ConstantExpression constantExpression)
		{
			return (T)constantExpression.Value;
		}
		return Expression.Lambda<Func<T>>(expr, Array.Empty<ParameterExpression>()).Compile(preferInterpretation: true)();
	}

	private static PropertyInfo? TryGetPropertyFromMethod(MethodInfo method)
	{
		return method.DeclaringType?.GetRuntimeProperties().FirstOrDefault((PropertyInfo prop) => prop.GetMethod == method);
	}

	private static IPropertyAccessor CreateInpcPropertyAccessor(WeakReference<object?> target, IPropertyInfo property)
	{
		return new InpcPropertyAccessor(target, property);
	}

	private static IPropertyAccessor CreateAvaloniaPropertyAccessor(WeakReference<object?> target, IPropertyInfo property)
	{
		object target2;
		return new AvaloniaPropertyAccessor(new WeakReference<AvaloniaObject>((AvaloniaObject)(target.TryGetTarget(out target2) ? target2 : null)), (AvaloniaProperty)property);
	}

	private static IPropertyAccessor CreateIndexerPropertyAccessor(WeakReference<object?> target, IPropertyInfo property, int argument)
	{
		return new IndexerAccessor(target, property, argument);
	}
}
