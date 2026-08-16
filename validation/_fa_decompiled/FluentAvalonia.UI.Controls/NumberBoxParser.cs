using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace FluentAvalonia.UI.Controls;

internal static class NumberBoxParser
{
	public const string numberBoxOperators = "+-*/^";

	public static IList<MathToken> GetTokens(ReadOnlySpan<char> input)
	{
		List<MathToken> list = new List<MathToken>();
		bool flag = true;
		while (input.Length > 0)
		{
			char c = input[0];
			if (c != ' ')
			{
				if (flag)
				{
					if (c == '(')
					{
						list.Add(new MathToken(MathTokenType.Parenthesis, c));
					}
					else
					{
						var (d, num) = GetNextNumber(input);
						if (num <= 0)
						{
							return null;
						}
						list.Add(new MathToken(MathTokenType.Numeric, d));
						input = input.Slice(num - 1);
						flag = false;
					}
				}
				else if ("+-*/^".IndexOf(c) != -1)
				{
					list.Add(new MathToken(MathTokenType.Operator, c));
					flag = true;
				}
				else
				{
					if (c != ')')
					{
						return null;
					}
					list.Add(new MathToken(MathTokenType.Parenthesis, c));
				}
			}
			input = input.Slice(1);
		}
		return list;
	}

	public static (double value, int charLen) GetNextNumber(ReadOnlySpan<char> input)
	{
		Regex.ValueMatchEnumerator valueMatchEnumerator = NumberRegex().EnumerateMatches(input);
		if (valueMatchEnumerator.MoveNext())
		{
			int length = valueMatchEnumerator.Current.Length;
			if (double.TryParse(input.Slice(0, length), NumberStyles.Any, CultureInfo.CurrentCulture, out var result))
			{
				return (value: result, charLen: length);
			}
		}
		return (value: double.NaN, charLen: 0);
	}

	public static int GetPrecedenceValue(char c)
	{
		switch (c)
		{
		case '*':
		case '/':
			return 1;
		case '^':
			return 2;
		default:
			return 0;
		}
	}

	public static IList<MathToken> ConvertInfixToPostfix(IList<MathToken> infixTokens)
	{
		List<MathToken> list = new List<MathToken>();
		Stack<MathToken> stack = new Stack<MathToken>();
		foreach (MathToken infixToken in infixTokens)
		{
			if (infixToken.Type == MathTokenType.Numeric)
			{
				list.Add(infixToken);
			}
			else if (infixToken.Type == MathTokenType.Operator)
			{
				while (stack.Count != 0)
				{
					MathToken item = stack.Peek();
					if (item.Type == MathTokenType.Parenthesis || GetPrecedenceValue(item.Char) < GetPrecedenceValue(infixToken.Char))
					{
						break;
					}
					list.Add(item);
					stack.Pop();
				}
				stack.Push(infixToken);
			}
			else
			{
				if (infixToken.Type != MathTokenType.Parenthesis)
				{
					continue;
				}
				if (infixToken.Char == '(')
				{
					stack.Push(infixToken);
					continue;
				}
				while (stack.Count != 0 && stack.Peek().Char != '(')
				{
					list.Add(stack.Peek());
					stack.Pop();
				}
				if (stack.Count == 0)
				{
					return null;
				}
				stack.Pop();
			}
		}
		while (stack.Count != 0)
		{
			if (stack.Peek().Type == MathTokenType.Parenthesis)
			{
				return null;
			}
			list.Add(stack.Pop());
		}
		return list;
	}

	public static double? ComputePostfixExpression(IList<MathToken> tokens)
	{
		Stack<double?> stack = new Stack<double?>();
		foreach (MathToken token in tokens)
		{
			if (token.Type == MathTokenType.Operator)
			{
				if (stack.Count < 2)
				{
					return null;
				}
				double value = stack.Pop().Value;
				double value2 = stack.Pop().Value;
				double? num = 0.0;
				switch (token.Char)
				{
				case '-':
					num = value2 - value;
					break;
				case '+':
					num = value + value2;
					break;
				case '*':
					num = value * value2;
					break;
				case '/':
					if (value == 0.0)
					{
						return double.NaN;
					}
					num = value2 / value;
					break;
				case '^':
					num = double.Pow(value2, value);
					break;
				default:
					num = null;
					break;
				}
				stack.Push(num);
			}
			else if (token.Type == MathTokenType.Numeric)
			{
				stack.Push(token.Value);
			}
		}
		if (stack.Count != 1)
		{
			return null;
		}
		return stack.Pop();
	}

	public static double? Compute(string expr)
	{
		IList<MathToken> tokens = GetTokens(expr.AsSpan());
		if (tokens != null && tokens.Count > 0)
		{
			IList<MathToken> list = ConvertInfixToPostfix(tokens);
			if (list != null && list.Count > 0)
			{
				return ComputePostfixExpression(list);
			}
		}
		return null;
	}

	/// <remarks>
	/// Pattern:<br />
	/// <code>^-?([^-+/*\\(\\)\\^\\s]+)</code><br />
	/// Explanation:<br />
	/// <code>
	/// ○ Match if at the beginning of the string.<br />
	/// ○ Match '-' atomically, optionally.<br />
	/// ○ 1st capture group.<br />
	///     ○ Match a character in the set [^(-+\-/^\s] atomically at least once.<br />
	/// </code>
	/// </remarks>
	[GeneratedRegex("^-?([^-+/*\\(\\)\\^\\s]+)")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.27113")]
	private static Regex NumberRegex()
	{
		return _003CRegexGenerator_g_003EFCA25BC748B01F694B9CFC64A6E47120476D5E3869795D7414BC8C722E863331E__NumberRegex_0.Instance;
	}
}
