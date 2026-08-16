using System;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Animations;

/// <summary>
/// Server-side counterpart of <see cref="T:Avalonia.Rendering.Composition.Animations.ExpressionAnimation" /> with values baked-in.
/// </summary>
internal class ExpressionAnimationInstance : AnimationInstanceBase, IAnimationInstance, IServerClockItem
{
	private readonly Expression _expression;

	private ExpressionVariant _startingValue;

	private readonly ExpressionVariant? _finalValue;

	protected override ExpressionVariant EvaluateCore(TimeSpan now, ExpressionVariant currentValue)
	{
		ExpressionEvaluationContext context = new ExpressionEvaluationContext
		{
			Parameters = base.Parameters,
			Target = base.TargetObject,
			ForeignFunctionInterface = BuiltInExpressionFfi.Instance,
			StartingValue = _startingValue,
			FinalValue = (_finalValue ?? _startingValue),
			CurrentValue = currentValue
		};
		return _expression.Evaluate(ref context);
	}

	public override void Initialize(TimeSpan startedAt, ExpressionVariant startingValue, CompositionProperty property)
	{
		_startingValue = startingValue;
		HashSet<(string, string)> hashSet = new HashSet<(string, string)>();
		_expression.CollectReferences(hashSet);
		Initialize(property, hashSet);
	}

	public ExpressionAnimationInstance(Expression expression, ServerObject target, ExpressionVariant? finalValue, PropertySetSnapshot parameters)
		: base(target, parameters)
	{
		_expression = expression;
		_finalValue = finalValue;
	}
}
