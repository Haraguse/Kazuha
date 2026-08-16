using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Avalonia.Rendering.Composition.Expressions;

internal class ExpressionParser
{
	private struct ExpressionOperatorGroup
	{
		private List<Expression> _expressions;

		private List<ExpressionType> _operators;

		private Expression? _first;

		private static readonly ExpressionType[][] OperatorPrecedenceGroups = new ExpressionType[6][]
		{
			new ExpressionType[3]
			{
				ExpressionType.Multiply,
				ExpressionType.Divide,
				ExpressionType.Remainder
			},
			new ExpressionType[2]
			{
				ExpressionType.Add,
				ExpressionType.Subtract
			},
			new ExpressionType[4]
			{
				ExpressionType.MoreThan,
				ExpressionType.MoreThanOrEqual,
				ExpressionType.LessThan,
				ExpressionType.LessThanOrEqual
			},
			new ExpressionType[2]
			{
				ExpressionType.Equals,
				ExpressionType.NotEquals
			},
			new ExpressionType[1] { ExpressionType.LogicalAnd },
			new ExpressionType[1] { ExpressionType.LogicalOr }
		};

		private static readonly ExpressionType[][] OperatorPrecedenceGroupsReversed = OperatorPrecedenceGroups.AsEnumerable().Reverse().ToArray();

		public bool NotEmpty => !Empty;

		public bool Empty
		{
			get
			{
				if (_expressions == null)
				{
					return _first == null;
				}
				return false;
			}
		}

		public void AppendFirst(Expression expr)
		{
			if (NotEmpty)
			{
				throw new InvalidOperationException();
			}
			_first = expr;
		}

		public void AppendWithOperator(Expression expr, ExpressionType op)
		{
			if (_expressions == null)
			{
				if (_first == null)
				{
					throw new InvalidOperationException();
				}
				_expressions = new List<Expression>();
				_expressions.Add(_first);
				_first = null;
				_operators = new List<ExpressionType>();
			}
			_expressions.Add(expr);
			_operators.Add(op);
		}

		private Expression ToExpression(int from, int to)
		{
			if (to - from == 0)
			{
				return _expressions[from];
			}
			if (to - from == 1)
			{
				return new BinaryExpression(_expressions[from], _expressions[to], _operators[from]);
			}
			ExpressionType[][] operatorPrecedenceGroupsReversed = OperatorPrecedenceGroupsReversed;
			foreach (ExpressionType[] array in operatorPrecedenceGroupsReversed)
			{
				for (int j = from; j < to; j++)
				{
					ExpressionType expressionType = _operators[j];
					ExpressionType[] array2 = array;
					foreach (ExpressionType expressionType2 in array2)
					{
						if (expressionType == expressionType2)
						{
							Expression left = ToExpression(from, j);
							Expression right = ToExpression(j + 1, to);
							return new BinaryExpression(left, right, expressionType);
						}
					}
				}
			}
			throw new ExpressionParseException("Expression parsing algorithm bug in ToExpression", 0);
		}

		public Expression ToExpression()
		{
			if (_expressions == null)
			{
				return _first ?? throw new InvalidOperationException();
			}
			return ToExpression(0, _expressions.Count - 1);
		}
	}

	private static ReadOnlySpan<char> Dot => ".".AsSpan();

	public static Expression Parse(ReadOnlySpan<char> s)
	{
		TokenParser parser = new TokenParser(s);
		Expression result = ParseTillTerminator(ref parser, "", throwOnTerminator: false, throwOnEnd: false, out var _);
		parser.SkipWhitespace();
		if (parser.Length != 0)
		{
			throw new ExpressionParseException("Unexpected data ", parser.Position);
		}
		return result;
	}

	private static bool TryParseAtomic(ref TokenParser parser, [MaybeNullWhen(false)] out Expression expr)
	{
		expr = null;
		if (parser.TryParseKeywordLowerCase("this.startingvalue"))
		{
			expr = new KeywordExpression(ExpressionKeyword.StartingValue);
		}
		else if (parser.TryParseKeywordLowerCase("this.currentvalue"))
		{
			expr = new KeywordExpression(ExpressionKeyword.CurrentValue);
		}
		else if (parser.TryParseKeywordLowerCase("this.finalvalue"))
		{
			expr = new KeywordExpression(ExpressionKeyword.FinalValue);
		}
		else if (parser.TryParseKeywordLowerCase("pi"))
		{
			expr = new KeywordExpression(ExpressionKeyword.Pi);
		}
		else if (parser.TryParseKeywordLowerCase("true"))
		{
			expr = new KeywordExpression(ExpressionKeyword.True);
		}
		else if (parser.TryParseKeywordLowerCase("false"))
		{
			expr = new KeywordExpression(ExpressionKeyword.False);
		}
		else if (parser.TryParseKeywordLowerCase("this.target"))
		{
			expr = new KeywordExpression(ExpressionKeyword.Target);
		}
		if (expr != null)
		{
			return true;
		}
		if (parser.TryParseIdentifier(out var res))
		{
			expr = new ParameterExpression(res.ToString());
			return true;
		}
		if (parser.TryParseFloat(out var res2))
		{
			expr = new ConstantExpression(res2);
			return true;
		}
		return false;
	}

	private static bool TryParseOperator(ref TokenParser parser, out ExpressionType op)
	{
		op = (ExpressionType)(-1);
		if (parser.TryConsume("||"))
		{
			op = ExpressionType.LogicalOr;
		}
		else if (parser.TryConsume("&&"))
		{
			op = ExpressionType.LogicalAnd;
		}
		else if (parser.TryConsume(">="))
		{
			op = ExpressionType.MoreThanOrEqual;
		}
		else if (parser.TryConsume("<="))
		{
			op = ExpressionType.LessThanOrEqual;
		}
		else if (parser.TryConsume("=="))
		{
			op = ExpressionType.Equals;
		}
		else if (parser.TryConsume("!="))
		{
			op = ExpressionType.NotEquals;
		}
		else
		{
			if (!parser.TryConsumeAny("+-/*><%".AsSpan(), out var token))
			{
				return false;
			}
			op = token switch
			{
				'+' => ExpressionType.Add, 
				'-' => ExpressionType.Subtract, 
				'/' => ExpressionType.Divide, 
				'*' => ExpressionType.Multiply, 
				'<' => ExpressionType.LessThan, 
				'>' => ExpressionType.MoreThan, 
				'%' => ExpressionType.Remainder, 
			};
		}
		return true;
	}

	private static Expression ParseTillTerminator(ref TokenParser parser, string terminatorChars, bool throwOnTerminator, bool throwOnEnd, out char? token)
	{
		ExpressionOperatorGroup expressionOperatorGroup = default(ExpressionOperatorGroup);
		token = null;
		while (true)
		{
			if (parser.TryConsumeAny(terminatorChars.AsSpan(), out var token2))
			{
				if (throwOnTerminator || expressionOperatorGroup.Empty)
				{
					throw new ExpressionParseException($"Unexpected '{token}'", parser.Position - 1);
				}
				token = token2;
				return expressionOperatorGroup.ToExpression();
			}
			parser.SkipWhitespace();
			if (parser.Length == 0)
			{
				if (throwOnEnd || expressionOperatorGroup.Empty)
				{
					throw new ExpressionParseException("Unexpected end of  expression", parser.Position);
				}
				return expressionOperatorGroup.ToExpression();
			}
			ExpressionType? expressionType = null;
			if (expressionOperatorGroup.NotEmpty)
			{
				if (parser.TryConsume('?'))
				{
					Expression truePart = ParseTillTerminator(ref parser, ":", throwOnTerminator: false, throwOnEnd: true, out var _);
					Expression falsePart = ParseTillTerminator(ref parser, terminatorChars, throwOnTerminator, throwOnEnd, out token);
					return new ConditionalExpression(expressionOperatorGroup.ToExpression(), truePart, falsePart);
				}
				if (!TryParseOperator(ref parser, out var op))
				{
					throw new ExpressionParseException("Unexpected token", parser.Position);
				}
				expressionType = op;
			}
			bool flag = false;
			while (parser.TryConsume('!'))
			{
				flag = !flag;
			}
			bool flag2 = false;
			while (parser.TryConsume('-'))
			{
				flag2 = !flag2;
			}
			Expression expr;
			ReadOnlySpan<char> res;
			if (parser.TryConsume('('))
			{
				expr = ParseTillTerminator(ref parser, ")", throwOnTerminator: false, throwOnEnd: true, out var _);
			}
			else if (parser.TryParseCall(out res))
			{
				List<Expression> list = new List<Expression>();
				while (true)
				{
					list.Add(ParseTillTerminator(ref parser, ",)", throwOnTerminator: false, throwOnEnd: true, out var token5));
					if (token5 == ')')
					{
						break;
					}
					if (token5 != ',')
					{
						throw new ExpressionParseException("Unexpected end of the expression", parser.Position);
					}
				}
				expr = new FunctionCallExpression(res.ToString(), list);
			}
			else if (!TryParseAtomic(ref parser, out expr))
			{
				break;
			}
			while (parser.TryConsume('.'))
			{
				if (!parser.TryParseIdentifier(out var res2))
				{
					throw new ExpressionParseException("Unexpected token", parser.Position);
				}
				expr = new MemberAccessExpression(expr, res2.ToString());
			}
			if (flag)
			{
				expr = new UnaryExpression(expr, ExpressionType.Not);
			}
			if (flag2)
			{
				expr = ((!(expr is ConstantExpression constantExpression)) ? ((Expression)new UnaryExpression(expr, ExpressionType.UnaryMinus)) : ((Expression)new ConstantExpression(0f - constantExpression.Constant)));
			}
			if (expressionOperatorGroup.Empty)
			{
				expressionOperatorGroup.AppendFirst(expr);
			}
			else
			{
				expressionOperatorGroup.AppendWithOperator(expr, expressionType.Value);
			}
		}
		throw new ExpressionParseException("Unexpected token", parser.Position);
	}
}
